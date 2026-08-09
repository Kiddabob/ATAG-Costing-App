using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using ATAG.Costing.Application.CentralData;

namespace ATAG.Costing.Infrastructure.CentralData;

/// <summary>
/// Reads the five replaceable central-data workbook tables without Excel,
/// recalculation, macros, or a machine-specific path. The selected workbook is
/// opened read-only and a new snapshot is returned only after all five tables
/// have produced records.
/// </summary>
public sealed class OpenXmlWorkbookCentralDataSourceReader : ICentralDataSourceReader
{
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelationshipsNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipsNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    public CentralDataSourceKind Kind => CentralDataSourceKind.LinkedWorkbook;

    public Task<CentralDataReadResult> ReadAsync(
        CentralDataSourceConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(configuration.WorkbookPath))
        {
            return Task.FromResult(
                CentralDataReadResult.Failure(
                    "Choose an Excel workbook before updating central data."));
        }

        if (!File.Exists(configuration.WorkbookPath))
        {
            return Task.FromResult(
                CentralDataReadResult.Failure(
                    "The linked workbook is not currently available."));
        }

        try
        {
            using var stream = new FileStream(
                configuration.WorkbookPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
            using var archive = new ZipArchive(
                stream,
                ZipArchiveMode.Read,
                leaveOpen: false);

            var sharedStrings = ReadSharedStrings(archive);
            var tables = ReadRequiredTables(
                archive,
                sharedStrings,
                cancellationToken);

            var copper = MapCopper(tables["Copper"]);
            var compounds = MapCompounds(tables["Compounds"]);
            var masterbatches = MapMasterbatches(tables["MasterbatchCodeList"]);
            var contacts = MapContacts(tables["Contacts"]);
            var operators = MapOperators(tables["Operators"]);

            if (copper.Count == 0 ||
                compounds.Count == 0 ||
                masterbatches.Count == 0 ||
                contacts.Count == 0 ||
                operators.Count == 0)
            {
                return Task.FromResult(
                    CentralDataReadResult.Failure(
                        "The workbook tables were found, but one or more contained no records."));
            }

            var snapshot = new CentralDataSnapshot(
                SchemaVersion: 2,
                Revision: $"workbook-refresh-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                CapturedAt: DateTimeOffset.UtcNow,
                SourceLabel: $"Linked workbook: {Path.GetFileName(configuration.WorkbookPath)}",
                copper,
                compounds,
                masterbatches,
                contacts,
                operators);

            return Task.FromResult(
                CentralDataReadResult.Success(
                    snapshot,
                    $"Central data updated from {Path.GetFileName(configuration.WorkbookPath)}."));
        }
        catch (InvalidDataException)
        {
            return Task.FromResult(
                CentralDataReadResult.Failure(
                    "The selected file is not a readable Excel Open XML workbook."));
        }
        catch (XmlException)
        {
            return Task.FromResult(
                CentralDataReadResult.Failure(
                    "The selected file contains unreadable Excel Open XML data."));
        }
        catch (IOException exception)
        {
            return Task.FromResult(
                CentralDataReadResult.Failure(
                    $"The linked workbook could not be read: {exception.Message}"));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(
                CentralDataReadResult.Failure(
                    "Windows denied access to the linked workbook."));
        }
        catch (FormatException exception)
        {
            return Task.FromResult(
                CentralDataReadResult.Failure(
                    $"The linked workbook has an unsupported central-data value: {exception.Message}"));
        }
    }

    private static Dictionary<string, WorkbookTable> ReadRequiredTables(
        ZipArchive archive,
        IReadOnlyList<string> sharedStrings,
        CancellationToken cancellationToken)
    {
        var requiredNames = new HashSet<string>(
            [
                "Copper",
                "Compounds",
                "MasterbatchCodeList",
                "Contacts",
                "Operators",
            ],
            StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, WorkbookTable>(
            StringComparer.OrdinalIgnoreCase);

        var workbook = LoadXml(archive, "xl/workbook.xml");
        var workbookRelationships = ReadRelationships(
            archive,
            "xl/workbook.xml",
            "/worksheet");

        foreach (var sheet in workbook
                     .Descendants(SpreadsheetNamespace + "sheet"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relationshipId =
                (string?)sheet.Attribute(OfficeRelationshipsNamespace + "id");
            if (string.IsNullOrWhiteSpace(relationshipId) ||
                !workbookRelationships.TryGetValue(
                    relationshipId,
                    out var sheetTarget))
            {
                continue;
            }

            var worksheetPart = ResolvePart("xl/workbook.xml", sheetTarget);
            var worksheetRelationships = ReadRelationships(
                archive,
                worksheetPart,
                "/table");

            foreach (var tableTarget in worksheetRelationships.Values)
            {
                var tablePart = ResolvePart(worksheetPart, tableTarget);
                var tableDocument = LoadXml(archive, tablePart);
                var tableElement = tableDocument.Root
                    ?? throw new InvalidDataException(
                        $"Table part {tablePart} has no root element.");
                var tableName =
                    (string?)tableElement.Attribute("name") ??
                    (string?)tableElement.Attribute("displayName");

                if (string.IsNullOrWhiteSpace(tableName) ||
                    !requiredNames.Contains(tableName))
                {
                    continue;
                }

                var worksheetDocument = LoadXml(archive, worksheetPart);
                result[tableName] = ReadTable(
                    tableElement,
                    worksheetDocument,
                    sharedStrings);
            }
        }

        var missing = requiredNames
            .Where(name => !result.ContainsKey(name))
            .OrderBy(name => name)
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                $"Required workbook table(s) not found: {string.Join(", ", missing)}.");
        }

        return result;
    }

    private static WorkbookTable ReadTable(
        XElement tableElement,
        XDocument worksheet,
        IReadOnlyList<string> sharedStrings)
    {
        var reference = (string?)tableElement.Attribute("ref")
            ?? throw new InvalidDataException("A workbook table has no range reference.");
        var bounds = ParseRange(reference);
        var totalsRowCount =
            ParseInteger((string?)tableElement.Attribute("totalsRowCount")) ?? 0;

        var headers = tableElement
            .Descendants(SpreadsheetNamespace + "tableColumn")
            .Select(column =>
                (string?)column.Attribute("name") ?? string.Empty)
            .ToArray();

        if (headers.Length != bounds.EndColumn - bounds.StartColumn + 1)
        {
            throw new InvalidDataException(
                $"Table {tableElement.Attribute("name")?.Value} has inconsistent columns.");
        }

        var cellValues = new Dictionary<(int Row, int Column), string?>();

        foreach (var cell in worksheet
                     .Descendants(SpreadsheetNamespace + "c"))
        {
            var address = (string?)cell.Attribute("r");
            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }

            var (row, column) = ParseCellAddress(address);
            if (row < bounds.StartRow ||
                row > bounds.EndRow ||
                column < bounds.StartColumn ||
                column > bounds.EndColumn)
            {
                continue;
            }

            cellValues[(row, column)] = ReadCellValue(cell, sharedStrings);
        }

        var dataEndRow = bounds.EndRow - totalsRowCount;
        var rows = new List<IReadOnlyDictionary<string, string?>>();

        for (var row = bounds.StartRow + 1; row <= dataEndRow; row++)
        {
            var values = new Dictionary<string, string?>(
                StringComparer.OrdinalIgnoreCase);
            var anyValue = false;

            for (var column = bounds.StartColumn; column <= bounds.EndColumn; column++)
            {
                cellValues.TryGetValue((row, column), out var value);
                values[headers[column - bounds.StartColumn]] = value;
                anyValue |= !string.IsNullOrWhiteSpace(value);
            }

            if (anyValue)
            {
                rows.Add(values);
            }
        }

        return new WorkbookTable(rows);
    }

    private static List<CopperReference> MapCopper(WorkbookTable table)
    {
        var result = new List<CopperReference>();

        foreach (var row in table.Rows)
        {
            var description = Text(row, "Description");
            var supplier = Text(row, "Company");
            var price = FirstPositiveDecimal(
                row,
                "Total Cost 2 (£/kg)",
                "Total Cost",
                "Copper Cost (£/kg)",
                "Manufature Cost") ?? 0m;
            var yield = FirstPositiveDecimal(
                row,
                "Yield (m/kg) Manual",
                "Yield (m/kg)") ?? 0m;
            var diameter = PositiveDecimal(row, "Nom OD (mm)") ?? 0m;

            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            result.Add(
                new CopperReference(
                    Text(row, "ID") ?? description,
                    description,
                    supplier ?? "Unknown supplier",
                    price,
                    yield,
                    diameter,
                    PositiveDecimal(row, "mm²") ?? 0m,
                    Text(row, "AWG")));
        }

        return result;
    }

    private static List<CompoundReference> MapCompounds(WorkbookTable table)
    {
        var result = new List<CompoundReference>();

        foreach (var row in table.Rows)
        {
            var compound = Text(row, "Compound");
            var price = PositiveDecimal(row, "Cost (£/kg)") ?? 0m;
            var specificGravity = PositiveDecimal(row, "Specific Gravity") ?? 0m;

            if (string.IsNullOrWhiteSpace(compound))
            {
                continue;
            }

            result.Add(
                new CompoundReference(
                    Text(row, "ID") ?? compound,
                    compound,
                    Text(row, "Company") ?? "Unknown supplier",
                    price,
                    specificGravity,
                    Text(row, "Type") ?? string.Empty,
                    Text(row, "Material Description") ?? string.Empty,
                    Truthy(row, "Data Sheet")));
        }

        return result;
    }

    private static List<MasterbatchReference> MapMasterbatches(WorkbookTable table)
    {
        var result = new List<MasterbatchReference>();

        foreach (var row in table.Rows)
        {
            var code = Text(row, "Colour Code");
            var colour = Text(row, "Colour");
            var price = PositiveDecimal(row, "£/kg") ?? 0m;

            if (string.IsNullOrWhiteSpace(code) &&
                string.IsNullOrWhiteSpace(colour))
            {
                continue;
            }

            result.Add(
                new MasterbatchReference(
                    code ?? string.Empty,
                    colour ?? string.Empty,
                    Text(row, "Colour Supplier") ?? "Unknown supplier",
                    price,
                    BuildCompatibility(row),
                    Text(row, "Colour Hex"),
                    Text(row, "Colour Type") ?? string.Empty,
                    Text(row, "RAL Number Equivalent"),
                    BuildTemperatureLimits(row)));
        }

        return result;
    }

    private static List<ContactReference> MapContacts(WorkbookTable table)
    {
        var result = new List<ContactReference>();

        foreach (var row in table.Rows)
        {
            var accountName = Text(row, "Account Name");
            if (string.IsNullOrWhiteSpace(accountName))
            {
                continue;
            }

            result.Add(
                new ContactReference(
                    Text(row, "UniqueCusRef") ?? accountName,
                    accountName,
                    Text(row, "Short Name") ?? string.Empty,
                    Text(row, "Address Line 1") ?? string.Empty,
                    Text(row, "Address Line 2") ?? string.Empty,
                    Text(row, "Address Line 3") ?? string.Empty,
                    Text(row, "Address Line 4") ?? string.Empty,
                    Text(row, "Post/Zip Code") ?? string.Empty,
                    Text(row, "Phone Number") ?? string.Empty,
                    Text(row, "PersonalEmail") ?? string.Empty,
                    Text(row, "SalesEmail") ?? string.Empty,
                    Text(row, "AccountsEmail") ?? string.Empty,
                    Truthy(row, "AccTypeAssemblyCust"),
                    Truthy(row, "AccTypeCableCust"),
                    Truthy(row, "AccTypeCompSupp"),
                    Truthy(row, "AccTypeCondSupp"),
                    Truthy(row, "AccTypePartSupp"),
                    Truthy(row, "AccTypeOtherSupp"),
                    Truthy(row, "AccTypeOtherCust")));
        }

        return result;
    }

    private static List<OperatorReference> MapOperators(WorkbookTable table)
    {
        var result = new List<OperatorReference>();

        foreach (var row in table.Rows)
        {
            var firstName = Text(row, "First Name") ?? string.Empty;
            var lastName = Text(row, "Last Name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstName) &&
                string.IsNullOrWhiteSpace(lastName))
            {
                continue;
            }

            result.Add(
                new OperatorReference(
                    Text(row, "ID") ?? $"{firstName}-{lastName}",
                    lastName,
                    Text(row, "Middle Name(s)") ?? string.Empty,
                    firstName,
                    Text(row, "Initials") ?? string.Empty,
                    Truthy(row, "Assembly"),
                    Truthy(row, "Production"),
                    Truthy(row, "Office"),
                    Truthy(row, "Other"),
                    Truthy(row, "Quality Control"),
                    Truthy(row, "GRN"),
                    Truthy(row, "Employee")));
        }

        return result;
    }

    private static string BuildCompatibility(
        IReadOnlyDictionary<string, string?> row)
    {
        var compatible = new List<string>();

        AddCompatibility(row, compatible, "PVC Use", "PVC");
        AddCompatibility(row, compatible, "PE/PP/PUR Use", "PE/PP/PUR");
        AddCompatibility(row, compatible, "PS Use", "PS");
        AddCompatibility(row, compatible, "ABS Use", "ABS");
        AddCompatibility(row, compatible, "ACETAL Use", "ACETAL");
        AddCompatibility(row, compatible, "PBT Use", "PBT");
        AddCompatibility(row, compatible, "Nylon Use", "Nylon");
        AddCompatibility(row, compatible, "PC/PES Use", "PC/PES");

        return compatible.Count == 0
            ? "Compatibility not recorded"
            : string.Join(", ", compatible);
    }

    private static string BuildTemperatureLimits(
        IReadOnlyDictionary<string, string?> row)
    {
        var limits = new List<string>();
        AddTemperatureLimit(row, limits, "PVC Use", "PVC Max Temp", "PVC");
        AddTemperatureLimit(
            row,
            limits,
            "PE/PP/PUR Use",
            "PE/PP/PUR Max Temp",
            "PE/PP/PUR");
        AddTemperatureLimit(row, limits, "PS Use", "PS Max Temp", "PS");
        AddTemperatureLimit(row, limits, "ABS Use", "ABS Max Temp", "ABS");
        AddTemperatureLimit(
            row,
            limits,
            "ACETAL Use",
            "ACETAL Max Temp",
            "ACETAL");
        AddTemperatureLimit(row, limits, "PBT Use", "PBT Max Temp", "PBT");
        AddTemperatureLimit(
            row,
            limits,
            "Nylon Use",
            "Nylon Max Temp",
            "Nylon");
        AddTemperatureLimit(
            row,
            limits,
            "PC/PES Use",
            "PC/PES Max Temp",
            "PC/PES");
        return string.Join(" · ", limits);
    }

    private static void AddTemperatureLimit(
        IReadOnlyDictionary<string, string?> row,
        ICollection<string> limits,
        string useHeader,
        string limitHeader,
        string label)
    {
        if (!Truthy(row, useHeader))
        {
            return;
        }

        var value = Text(row, limitHeader);
        limits.Add(string.IsNullOrWhiteSpace(value)
            ? $"{label} limit not recorded"
            : $"{label} {value} °C");
    }

    private static void AddCompatibility(
        IReadOnlyDictionary<string, string?> row,
        ICollection<string> compatible,
        string header,
        string label)
    {
        if (row.TryGetValue(header, out var raw) &&
            decimal.TryParse(
                raw,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value) &&
            value > 0)
        {
            compatible.Add(label);
        }
    }

    private static bool Truthy(
        IReadOnlyDictionary<string, string?> row,
        string header)
    {
        if (!row.TryGetValue(header, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               (decimal.TryParse(
                    raw,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var value) &&
                value > 0m);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return Array.Empty<string>();
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);

        return document
            .Descendants(SpreadsheetNamespace + "si")
            .Select(item =>
                string.Concat(
                    item.Descendants(SpreadsheetNamespace + "t")
                        .Select(text => text.Value)))
            .ToArray();
    }

    private static string? ReadCellValue(
        XElement cell,
        IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");

        if (type == "inlineStr")
        {
            return string.Concat(
                cell.Descendants(SpreadsheetNamespace + "t")
                    .Select(text => text.Value));
        }

        var raw = cell.Element(SpreadsheetNamespace + "v")?.Value;
        if (raw is null)
        {
            return null;
        }

        if (type == "s" &&
            int.TryParse(
                raw,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var index) &&
            index >= 0 &&
            index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return raw;
    }

    private static Dictionary<string, string> ReadRelationships(
        ZipArchive archive,
        string sourcePart,
        string relationshipTypeSuffix)
    {
        var relationshipsPart = GetRelationshipsPart(sourcePart);
        var entry = archive.GetEntry(relationshipsPart);
        if (entry is null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);

        return document
            .Descendants(PackageRelationshipsNamespace + "Relationship")
            .Where(relationship =>
                !string.Equals(
                    (string?)relationship.Attribute("TargetMode"),
                    "External",
                    StringComparison.OrdinalIgnoreCase) &&
                ((string?)relationship.Attribute("Type"))?.EndsWith(
                    relationshipTypeSuffix,
                    StringComparison.OrdinalIgnoreCase) == true)
            .ToDictionary(
                relationship =>
                    (string?)relationship.Attribute("Id") ?? string.Empty,
                relationship =>
                    (string?)relationship.Attribute("Target") ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static XDocument LoadXml(ZipArchive archive, string part)
    {
        var entry = archive.GetEntry(part)
            ?? throw new InvalidDataException(
                $"The workbook package is missing {part}.");
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static string GetRelationshipsPart(string sourcePart)
    {
        var separator = sourcePart.LastIndexOf('/');
        var directory = separator >= 0
            ? sourcePart[..(separator + 1)]
            : string.Empty;
        var fileName = separator >= 0
            ? sourcePart[(separator + 1)..]
            : sourcePart;
        return $"{directory}_rels/{fileName}.rels";
    }

    private static string ResolvePart(string sourcePart, string target)
    {
        if (target.StartsWith('/'))
        {
            return target.TrimStart('/');
        }

        var separator = sourcePart.LastIndexOf('/');
        var directory = separator >= 0
            ? sourcePart[..(separator + 1)]
            : string.Empty;
        var segments = new List<string>();

        foreach (var segment in $"{directory}{target}".Split('/'))
        {
            switch (segment)
            {
                case "":
                case ".":
                    continue;
                case "..":
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }

                    break;
                default:
                    segments.Add(segment);
                    break;
            }
        }

        return string.Join("/", segments);
    }

    private static CellRange ParseRange(string reference)
    {
        var parts = reference.Replace("$", string.Empty).Split(':');
        var start = ParseCellAddress(parts[0]);
        var end = parts.Length == 1
            ? start
            : ParseCellAddress(parts[1]);
        return new CellRange(start.Row, start.Column, end.Row, end.Column);
    }

    private static (int Row, int Column) ParseCellAddress(string address)
    {
        var column = 0;
        var index = 0;

        while (index < address.Length && char.IsLetter(address[index]))
        {
            column = (column * 26) +
                (char.ToUpperInvariant(address[index]) - 'A' + 1);
            index++;
        }

        if (column == 0 ||
            !int.TryParse(
                address[index..],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var row))
        {
            throw new FormatException($"Invalid worksheet cell address: {address}.");
        }

        return (row, column);
    }

    private static int? ParseInteger(string? value) =>
        int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;

    private static string? Text(
        IReadOnlyDictionary<string, string?> row,
        string header) =>
        row.TryGetValue(header, out var value) &&
        !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    private static decimal? PositiveDecimal(
        IReadOnlyDictionary<string, string?> row,
        string header)
    {
        var value = Text(row, header);

        return decimal.TryParse(
                   value,
                   NumberStyles.Number | NumberStyles.AllowExponent,
                   CultureInfo.InvariantCulture,
                   out var parsed) &&
               parsed > 0
            ? parsed
            : null;
    }

    private static decimal? FirstPositiveDecimal(
        IReadOnlyDictionary<string, string?> row,
        params string[] headers)
    {
        foreach (var header in headers)
        {
            var value = PositiveDecimal(row, header);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private sealed record WorkbookTable(
        IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows);

    private readonly record struct CellRange(
        int StartRow,
        int StartColumn,
        int EndRow,
        int EndColumn);
}
