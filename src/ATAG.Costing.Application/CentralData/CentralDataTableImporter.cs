using System.Globalization;

namespace ATAG.Costing.Application.CentralData;

public static class CentralDataTableImporter
{
    public static CentralDataTableImportResult Import(
        CentralDataSnapshot current,
        CentralDataArea area,
        CentralDataTablePreview preview,
        IReadOnlyDictionary<string, string> mappings,
        IReadOnlyList<CentralDataQueryStep> steps,
        string sourceLabel)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(steps);

        var transformed = preview.Apply(steps);
        var blockingIssue = transformed.Issues.FirstOrDefault(issue => issue.IsBlocking);
        if (blockingIssue is not null)
        {
            return Failure(
                $"The source stopped before the complete table was read: {blockingIssue.Message}");
        }

        var matches = CentralDataImportSchema.Match(area, transformed.Columns, mappings);
        var missing = matches
            .Where(match => match.Field.IsRequired && !match.IsResolved)
            .Select(match => match.Field.Label)
            .ToArray();
        if (missing.Length > 0)
        {
            return Failure($"Required data was not matched: {string.Join(", ", missing)}.");
        }

        mappings = matches
            .Where(match => match.IsResolved)
            .ToDictionary(
                match => match.Field.Key,
                match => match.SourceColumn!,
                StringComparer.OrdinalIgnoreCase);

        var warnings = new List<string>();
        if (transformed.IgnoredErrorCount > 0)
        {
            warnings.Add($"{transformed.IgnoredErrorCount} division-by-zero or source-error cell(s) were imported as blank values.");
        }

        var skipped = 0;
        var calculatedFieldCount = 0;
        var estimatedFieldCount = 0;
        CentralDataSnapshot replacement;
        int imported;

        switch (area)
        {
            case CentralDataArea.Copper:
            {
                var rows = new List<CopperReference>();
                foreach (var row in transformed.Rows)
                {
                    var description = Text(row, mappings, "Description");
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        skipped++;
                        continue;
                    }

                    var source = new CopperReference(
                        Text(row, mappings, "Id") ?? description,
                        description,
                        Text(row, mappings, "Supplier") ?? "Unknown supplier",
                        Decimal(row, mappings, "PricePerKilogram"),
                        Decimal(row, mappings, "YieldMetresPerKilogram"),
                        Decimal(row, mappings, "NominalOutsideDiameterMillimetres"),
                        Decimal(row, mappings, "NominalAreaSquareMillimetres"),
                        Text(row, mappings, "WorkbookAwg"));
                    var completed = CopperReferenceDeriver.FillMissing(
                        source,
                        row,
                        mappings);
                    calculatedFieldCount += completed.EffectiveDerivedValues.Count(
                        value => !value.IsEstimate);
                    estimatedFieldCount += completed.EffectiveDerivedValues.Count(
                        value => value.IsEstimate);
                    rows.Add(completed);
                }

                imported = rows.Count;
                replacement = current with { Copper = rows };
                break;
            }
            case CentralDataArea.Compounds:
            {
                var rows = new List<CompoundReference>();
                foreach (var row in transformed.Rows)
                {
                    var name = Text(row, mappings, "CompoundName");
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        skipped++;
                        continue;
                    }

                    rows.Add(new CompoundReference(
                        Text(row, mappings, "Id") ?? name,
                        name,
                        Text(row, mappings, "Supplier") ?? "Unknown supplier",
                        Decimal(row, mappings, "PricePerKilogram"),
                        Decimal(row, mappings, "SpecificGravity"),
                        Text(row, mappings, "MaterialType") ?? string.Empty,
                        Text(row, mappings, "Description") ?? string.Empty,
                        Boolean(row, mappings, "HasDataSheet")));
                }

                imported = rows.Count;
                replacement = current with { Compounds = rows };
                break;
            }
            case CentralDataArea.Masterbatch:
            {
                var rows = new List<MasterbatchReference>();
                foreach (var row in transformed.Rows)
                {
                    var name = Text(row, mappings, "ColourName");
                    var code = Text(row, mappings, "ColourCode");
                    if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code))
                    {
                        skipped++;
                        continue;
                    }

                    var materialLimits = ProjectMasterbatchMaterialLimits(
                        row,
                        mappings);
                    var hasMaterialColumns = materialLimits.Any(
                        limit => limit.IsCompatible is not null ||
                                 !string.IsNullOrWhiteSpace(limit.MaximumTemperature));
                    var compatibility = hasMaterialColumns
                        ? string.Join(
                            ", ",
                            materialLimits
                                .Where(limit => limit.IsCompatible == true)
                                .Select(limit => limit.MaterialFamily))
                        : Text(row, mappings, "Compatibility") ??
                          "Compatibility not recorded";
                    if (hasMaterialColumns && string.IsNullOrWhiteSpace(compatibility))
                    {
                        compatibility = "No compatible families listed";
                    }

                    var temperatureLimits = hasMaterialColumns
                        ? string.Join(
                            " · ",
                            materialLimits
                                .Where(limit => !string.IsNullOrWhiteSpace(
                                    limit.MaximumTemperature))
                                .Select(limit =>
                                    $"{limit.MaterialFamily} {limit.MaximumTemperature}"))
                        : Text(row, mappings, "TemperatureLimits") ?? string.Empty;

                    rows.Add(new MasterbatchReference(
                        code ?? string.Empty,
                        name ?? string.Empty,
                        Text(row, mappings, "Supplier") ?? "Unknown supplier",
                        Decimal(row, mappings, "PricePerKilogram"),
                        compatibility,
                        Text(row, mappings, "ColourHex"),
                        Text(row, mappings, "ColourType") ?? string.Empty,
                        Text(row, mappings, "RalEquivalent"),
                        temperatureLimits,
                        hasMaterialColumns ? materialLimits : null));
                }

                imported = rows.Count;
                replacement = current with { Masterbatches = rows };
                break;
            }
            case CentralDataArea.Contacts:
            {
                var rows = new List<ContactReference>();
                foreach (var row in transformed.Rows)
                {
                    var name = Text(row, mappings, "AccountName");
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        skipped++;
                        continue;
                    }

                    rows.Add(new ContactReference(
                        Text(row, mappings, "Id") ?? name,
                        name,
                        Text(row, mappings, "ShortName") ?? string.Empty,
                        Text(row, mappings, "AddressLine1") ?? string.Empty,
                        Text(row, mappings, "AddressLine2") ?? string.Empty,
                        Text(row, mappings, "AddressLine3") ?? string.Empty,
                        Text(row, mappings, "AddressLine4") ?? string.Empty,
                        Text(row, mappings, "PostCode") ?? string.Empty,
                        Text(row, mappings, "PhoneNumber") ?? string.Empty,
                        Text(row, mappings, "PersonalEmail") ?? string.Empty,
                        Text(row, mappings, "SalesEmail") ?? string.Empty,
                        Text(row, mappings, "AccountsEmail") ?? string.Empty,
                        Boolean(row, mappings, "IsAssemblyCustomer"),
                        Boolean(row, mappings, "IsCableCustomer"),
                        Boolean(row, mappings, "IsCompoundSupplier"),
                        Boolean(row, mappings, "IsConductorSupplier"),
                        Boolean(row, mappings, "IsPartSupplier"),
                        Boolean(row, mappings, "IsOtherSupplier"),
                        Boolean(row, mappings, "IsOtherCustomer")));
                }

                imported = rows.Count;
                replacement = current with { Contacts = rows };
                break;
            }
            case CentralDataArea.Operators:
            {
                var rows = new List<OperatorReference>();
                foreach (var row in transformed.Rows)
                {
                    var first = Text(row, mappings, "FirstName") ?? string.Empty;
                    var last = Text(row, mappings, "LastName") ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
                    {
                        skipped++;
                        continue;
                    }

                    rows.Add(new OperatorReference(
                        Text(row, mappings, "Id") ?? $"{first}-{last}",
                        last,
                        Text(row, mappings, "MiddleNames") ?? string.Empty,
                        first,
                        Text(row, mappings, "Initials") ?? string.Empty,
                        Boolean(row, mappings, "Assembly"),
                        Boolean(row, mappings, "Production"),
                        Boolean(row, mappings, "Office"),
                        Boolean(row, mappings, "Other"),
                        Boolean(row, mappings, "QualityControl"),
                        Boolean(row, mappings, "Grn"),
                        Boolean(row, mappings, "Employee")));
                }

                imported = rows.Count;
                replacement = current with { Operators = rows };
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(area));
        }

        if (imported == 0)
        {
            return Failure("No usable rows remained after the import steps.", skipped, warnings);
        }

        if (calculatedFieldCount > 0)
        {
            warnings.Add(
                $"Calculated {calculatedFieldCount:N0} missing {area} field(s) from retained source values; each value keeps its formula and source trace.");
        }

        if (estimatedFieldCount > 0)
        {
            warnings.Add(
                $"Estimated {estimatedFieldCount:N0} missing {area} field(s) from parsed construction geometry; estimated values remain visibly labelled.");
        }

        var capturedAt = DateTimeOffset.UtcNow;
        replacement = replacement with
        {
            Revision = $"database-import-{capturedAt:yyyyMMddHHmmss}",
            CapturedAt = capturedAt,
            SourceLabel = sourceLabel,
        };
        var retainedTable = new CentralDataRetainedTable(
            area,
            sourceLabel,
            transformed.SourceObject.QualifiedName,
            transformed.SourceObject.SchemaName,
            transformed.SourceObject.Kind,
            capturedAt,
            transformed.Columns,
            transformed.Rows,
            transformed.Issues,
            steps);
        return new CentralDataTableImportResult(
            true,
            replacement,
            imported,
            skipped,
            warnings,
            $"Retained the full {transformed.Rows.Count:N0}-row, {transformed.Columns.Count:N0}-column table and projected {imported:N0} usable {area} row(s); {skipped:N0} blank row(s) were skipped by the costing projection.",
            retainedTable);
    }

    private static CentralDataTableImportResult Failure(
        string message,
        int skipped = 0,
        IReadOnlyList<string>? warnings = null) =>
        new(false, null, 0, skipped, warnings ?? [], message);

    private static string? Text(
        CentralDataPreviewRow row,
        IReadOnlyDictionary<string, string> mappings,
        string field)
    {
        if (!mappings.TryGetValue(field, out var column) ||
            string.IsNullOrWhiteSpace(column))
        {
            return null;
        }

        var cell = row.Cell(column);
        return cell.HasError || string.IsNullOrWhiteSpace(cell.Value)
            ? null
            : cell.Value.Trim();
    }

    private static decimal Decimal(
        CentralDataPreviewRow row,
        IReadOnlyDictionary<string, string> mappings,
        string field)
    {
        var text = Text(row, mappings, field);
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0m;
        }

        var cleaned = text
            .Replace("£", string.Empty, StringComparison.Ordinal)
            .Replace("€", string.Empty, StringComparison.Ordinal)
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace("/kg", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("m/kg", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mm²", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mm2", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mm", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        return decimal.TryParse(
            cleaned,
            NumberStyles.Number | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture,
            out var invariant)
            ? invariant
            : decimal.TryParse(
                cleaned,
                NumberStyles.Number | NumberStyles.AllowExponent,
                CultureInfo.CurrentCulture,
                out var current)
                ? current
                : 0m;
    }

    private static bool Boolean(
        CentralDataPreviewRow row,
        IReadOnlyDictionary<string, string> mappings,
        string field)
    {
        var value = Text(row, mappings, field);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("y", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("x", StringComparison.OrdinalIgnoreCase) ||
               decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric) && numeric != 0m;
    }

    private static IReadOnlyList<MasterbatchMaterialLimit>
        ProjectMasterbatchMaterialLimits(
            CentralDataPreviewRow row,
            IReadOnlyDictionary<string, string> mappings) =>
        [
            MasterbatchMaterialLimit(row, mappings, "PVC", "PvcUse", "PvcMaxTemp"),
            MasterbatchMaterialLimit(row, mappings, "PE/PP/PUR", "PePpPurUse", "PePpPurMaxTemp"),
            MasterbatchMaterialLimit(row, mappings, "PS", "PsUse", "PsMaxTemp"),
            MasterbatchMaterialLimit(row, mappings, "ABS", "AbsUse", "AbsMaxTemp"),
            MasterbatchMaterialLimit(row, mappings, "ACETAL", "AcetalUse", "AcetalMaxTemp"),
            MasterbatchMaterialLimit(row, mappings, "PBT", "PbtUse", "PbtMaxTemp"),
            MasterbatchMaterialLimit(row, mappings, "NYLON", "NylonUse", "NylonMaxTemp"),
            MasterbatchMaterialLimit(row, mappings, "PC/PES", "PcPesUse", "PcPesMaxTemp"),
        ];

    private static MasterbatchMaterialLimit MasterbatchMaterialLimit(
        CentralDataPreviewRow row,
        IReadOnlyDictionary<string, string> mappings,
        string family,
        string useField,
        string temperatureField)
    {
        var useValue = Text(row, mappings, useField);
        bool? compatible = string.IsNullOrWhiteSpace(useValue)
            ? null
            : Boolean(row, mappings, useField);
        return new MasterbatchMaterialLimit(
            family,
            compatible,
            Text(row, mappings, temperatureField) ?? string.Empty);
    }
}
