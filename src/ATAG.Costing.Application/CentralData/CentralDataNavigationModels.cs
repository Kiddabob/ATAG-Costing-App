using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace ATAG.Costing.Application.CentralData;

public enum CentralDataObjectKind
{
    Table = 0,
    View = 1,
}

public enum CentralDataCellErrorKind
{
    None = 0,
    DivisionByZero = 1,
    SourceError = 2,
}

public enum CentralDataQueryStepKind
{
    Source = 0,
    Navigation = 1,
    ReplaceDivisionByZeroWithNull = 2,
    TrimText = 3,
    RemoveBlankRows = 4,
    RemoveColumn = 5,
    RenameColumn = 6,
    FilterRows = 7,
}

public enum CentralDataFilterOperator
{
    Equals = 0,
    DoesNotEqual = 1,
    Contains = 2,
    DoesNotContain = 3,
    StartsWith = 4,
    EndsWith = 5,
    IsBlank = 6,
    IsNotBlank = 7,
}

public sealed record CentralDataDatabaseConnection(
    CentralDataSourceKind SourceKind,
    string DisplayName,
    string? AccessDatabasePath = null,
    string? SqlServer = null,
    string? SqlDatabase = null,
    bool UseWindowsAuthentication = true,
    string? SqlUserName = null,
    string? SqlPassword = null);

public sealed record CentralDataSourceObject(
    string Name,
    string? SchemaName,
    CentralDataObjectKind Kind,
    string DisplayName)
{
    public string QualifiedName => string.IsNullOrWhiteSpace(SchemaName)
        ? Name
        : $"{SchemaName}.{Name}";
}

public sealed record CentralDataPreviewColumn(
    string Name,
    string DataType,
    int Ordinal,
    bool AllowsNull,
    string? SourceName = null,
    string? Caption = null,
    string? Description = null)
{
    [JsonIgnore]
    public string EffectiveSourceName =>
        string.IsNullOrWhiteSpace(SourceName) ? Name : SourceName;

    [JsonIgnore]
    public IReadOnlyList<string> MatchNames =>
        new[] { Name, EffectiveSourceName, Caption, Description }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    [JsonIgnore]
    public string MetadataDisplay
    {
        get
        {
            var details = new List<string>();
            if (!string.Equals(Name, EffectiveSourceName, StringComparison.OrdinalIgnoreCase))
            {
                details.Add($"Source: {EffectiveSourceName}");
            }

            if (!string.IsNullOrWhiteSpace(Caption) &&
                !string.Equals(Caption, Name, StringComparison.OrdinalIgnoreCase))
            {
                details.Add($"Caption: {Caption}");
            }

            if (!string.IsNullOrWhiteSpace(Description) &&
                !string.Equals(Description, Name, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Description, Caption, StringComparison.OrdinalIgnoreCase))
            {
                details.Add($"Description: {Description}");
            }

            details.Add(DataType);
            return string.Join("\n", details.Prepend(Name));
        }
    }
}

public sealed record CentralDataSourceColumnMetadata(
    string SourceName,
    string? Caption = null,
    string? Description = null);

public sealed record CentralDataPreviewCell(
    string? Value,
    string DisplayValue,
    CentralDataCellErrorKind ErrorKind = CentralDataCellErrorKind.None,
    string? ErrorMessage = null)
{
    public bool HasError => ErrorKind != CentralDataCellErrorKind.None;

    public static CentralDataPreviewCell FromValue(object? value)
    {
        if (value is null || value is DBNull)
        {
            return new(null, string.Empty);
        }

        var text = value switch
        {
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset offset => offset.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        } ?? string.Empty;

        return IsDivisionByZero(text)
            ? DivisionByZero()
            : new(text, text);
    }

    public static CentralDataPreviewCell DivisionByZero(
        string? message = null) =>
        new(
            null,
            "Ignored · division by zero",
            CentralDataCellErrorKind.DivisionByZero,
            message ?? "The source value is a division-by-zero error. It is treated as blank for import.");

    public static CentralDataPreviewCell SourceError(string message) =>
        new(
            null,
            "Ignored · source error",
            CentralDataCellErrorKind.SourceError,
            message);

    public static bool IsDivisionByZero(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return normalized is "#DIV/0!" or "#DIV0!" or "#DIV/0" or "DIVISIONBYZERO";
    }
}

public sealed record CentralDataPreviewRow(
    int SourceRowNumber,
    IReadOnlyDictionary<string, CentralDataPreviewCell> Cells)
{
    public CentralDataPreviewCell Cell(string columnName) =>
        Cells.TryGetValue(columnName, out var cell)
            ? cell
            : new CentralDataPreviewCell(null, string.Empty);
}

public sealed record CentralDataPreviewIssue(
    int? SourceRowNumber,
    string? ColumnName,
    string Message,
    bool IsBlocking = false);

public sealed record CentralDataTablePreview(
    CentralDataSourceObject SourceObject,
    IReadOnlyList<CentralDataPreviewColumn> Columns,
    IReadOnlyList<CentralDataPreviewRow> Rows,
    IReadOnlyList<CentralDataPreviewIssue> Issues,
    int PreviewLimit)
{
    public int IgnoredErrorCount => Rows.Sum(
        row => row.Cells.Values.Count(cell => cell.HasError));

    public CentralDataTablePreview Apply(
        IReadOnlyList<CentralDataQueryStep> steps)
    {
        var columns = Columns.ToList();
        var rows = Rows.ToArray();
        var issues = Issues.ToList();

        foreach (var step in steps.Where(step => step.IsEnabled))
        {
            switch (step.Kind)
            {
                case CentralDataQueryStepKind.RemoveColumn:
                {
                    if (string.IsNullOrWhiteSpace(step.SourceColumn))
                    {
                        issues.Add(new CentralDataPreviewIssue(
                            null,
                            null,
                            $"The '{step.Name}' remove-column step has no source column.",
                            IsBlocking: true));
                        break;
                    }

                    var removed = columns.FirstOrDefault(column =>
                        string.Equals(column.Name, step.SourceColumn, StringComparison.OrdinalIgnoreCase));
                    if (removed is null)
                    {
                        // A source schema may legitimately stop returning a column which
                        // was already configured for removal.
                        break;
                    }

                    columns.Remove(removed);
                    rows = rows.Select(row => row with
                    {
                        Cells = row.Cells
                            .Where(pair => !string.Equals(
                                pair.Key,
                                removed.Name,
                                StringComparison.OrdinalIgnoreCase))
                            .ToDictionary(
                                pair => pair.Key,
                                pair => pair.Value,
                                StringComparer.OrdinalIgnoreCase),
                    }).ToArray();
                    break;
                }
                case CentralDataQueryStepKind.RenameColumn:
                {
                    var source = step.SourceColumn?.Trim();
                    var target = step.TargetColumn?.Trim();
                    if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                    {
                        issues.Add(new CentralDataPreviewIssue(
                            null,
                            source,
                            $"The '{step.Name}' rename-column step needs both a source and a new name.",
                            IsBlocking: true));
                        break;
                    }

                    var index = columns.FindIndex(column =>
                        string.Equals(column.Name, source, StringComparison.OrdinalIgnoreCase));
                    if (index < 0)
                    {
                        issues.Add(new CentralDataPreviewIssue(
                            null,
                            source,
                            $"The saved rename source '{source}' is no longer present in this table.",
                            IsBlocking: true));
                        break;
                    }

                    if (columns.Where((_, columnIndex) => columnIndex != index).Any(column =>
                        string.Equals(column.Name, target, StringComparison.OrdinalIgnoreCase)))
                    {
                        issues.Add(new CentralDataPreviewIssue(
                            null,
                            source,
                            $"The new column name '{target}' is already in use.",
                            IsBlocking: true));
                        break;
                    }

                    var original = columns[index];
                    columns[index] = original with
                    {
                        Name = target,
                        SourceName = original.EffectiveSourceName,
                    };
                    rows = rows.Select(row => row with
                    {
                        Cells = row.Cells.ToDictionary(
                            pair => string.Equals(pair.Key, source, StringComparison.OrdinalIgnoreCase)
                                ? target
                                : pair.Key,
                            pair => pair.Value,
                            StringComparer.OrdinalIgnoreCase),
                    }).ToArray();
                    break;
                }
                case CentralDataQueryStepKind.TrimText:
                    rows = rows.Select(
                        row => row with
                        {
                            Cells = row.Cells.ToDictionary(
                                pair => pair.Key,
                                pair => pair.Value with
                                {
                                    Value = pair.Value.Value?.Trim(),
                                    DisplayValue = pair.Value.HasError
                                        ? pair.Value.DisplayValue
                                        : pair.Value.DisplayValue.Trim(),
                                },
                                StringComparer.OrdinalIgnoreCase),
                        }).ToArray();
                    break;
                case CentralDataQueryStepKind.RemoveBlankRows:
                    rows = rows.Where(
                        row => row.Cells.Values.Any(
                            cell => !cell.HasError && !string.IsNullOrWhiteSpace(cell.Value)))
                        .ToArray();
                    break;
                case CentralDataQueryStepKind.FilterRows:
                {
                    var columnName = step.SourceColumn?.Trim();
                    if (string.IsNullOrWhiteSpace(columnName))
                    {
                        issues.Add(new CentralDataPreviewIssue(
                            null,
                            null,
                            $"The '{step.Name}' filter has no source column.",
                            IsBlocking: true));
                        break;
                    }

                    if (!columns.Any(column => string.Equals(
                            column.Name,
                            columnName,
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        issues.Add(new CentralDataPreviewIssue(
                            null,
                            columnName,
                            $"The saved filter column '{columnName}' is no longer present in this table.",
                            IsBlocking: true));
                        break;
                    }

                    rows = rows.Where(row => FilterMatches(
                            row.Cell(columnName),
                            step.FilterOperator,
                            step.FilterValue))
                        .ToArray();
                    break;
                }
            }
        }

        return this with
        {
            Columns = columns,
            Rows = rows,
            Issues = issues,
        };
    }

    private static bool FilterMatches(
        CentralDataPreviewCell cell,
        CentralDataFilterOperator filterOperator,
        string? filterValue)
    {
        var actual = cell.HasError ? string.Empty : cell.Value?.Trim() ?? string.Empty;
        var expected = filterValue?.Trim() ?? string.Empty;
        return filterOperator switch
        {
            CentralDataFilterOperator.Equals =>
                string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            CentralDataFilterOperator.DoesNotEqual =>
                !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            CentralDataFilterOperator.Contains =>
                actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            CentralDataFilterOperator.DoesNotContain =>
                !actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            CentralDataFilterOperator.StartsWith =>
                actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
            CentralDataFilterOperator.EndsWith =>
                actual.EndsWith(expected, StringComparison.OrdinalIgnoreCase),
            CentralDataFilterOperator.IsBlank => string.IsNullOrWhiteSpace(actual),
            CentralDataFilterOperator.IsNotBlank => !string.IsNullOrWhiteSpace(actual),
            _ => false,
        };
    }
}

public sealed record CentralDataQueryStep(
    CentralDataQueryStepKind Kind,
    string Name,
    string Description,
    bool IsEnabled = true,
    bool CanDisable = false,
    string? SourceColumn = null,
    string? TargetColumn = null,
    CentralDataFilterOperator FilterOperator = CentralDataFilterOperator.Equals,
    string? FilterValue = null);

public sealed record CentralDataFieldDefinition(
    string Key,
    string Label,
    bool IsRequired,
    IReadOnlyList<string> Aliases);

public sealed record CentralDataFieldMatch(
    CentralDataFieldDefinition Field,
    string? SourceColumn,
    bool WasAutomaticallyMatched)
{
    public bool IsResolved => !string.IsNullOrWhiteSpace(SourceColumn);

    public string Status => IsResolved
        ? WasAutomaticallyMatched ? "Matched automatically" : "Selected"
        : Field.IsRequired ? "Required match missing" : "Optional field not present";
}

public sealed record CentralDataTableImportResult(
    bool Succeeded,
    CentralDataSnapshot? Snapshot,
    int ImportedRows,
    int SkippedRows,
    IReadOnlyList<string> Warnings,
    string Message,
    CentralDataRetainedTable? RetainedTable = null);

public interface ICentralDataDatabaseNavigator
{
    CentralDataSourceKind Kind { get; }

    Task<IReadOnlyList<CentralDataSourceObject>> DiscoverAsync(
        CentralDataDatabaseConnection connection,
        CancellationToken cancellationToken = default);

    Task<CentralDataTablePreview> PreviewAsync(
        CentralDataDatabaseConnection connection,
        CentralDataSourceObject sourceObject,
        int rowLimit = 200,
        CancellationToken cancellationToken = default);
}

public static class CentralDataImportSchema
{
    public static IReadOnlyList<CentralDataFieldDefinition> Fields(
        CentralDataArea area) => area switch
        {
            CentralDataArea.Copper =>
            [
                Field("Id", "Material ID", false, "ID", "CopperId", "MaterialId"),
                Field("Description", "Description", true, "Description", "Copper", "CopperType", "Conductor", "ConductorType"),
                Field("Supplier", "Supplier", true, "Company", "Supplier", "CopperSupplier", "ConductorSupplier"),
                Field("PricePerKilogram", "Price per kg", false, "Total Cost 2 (£/kg)", "Total Cost", "Copper Cost (£/kg)", "Cost (£/kg)", "Price", "PricePerKilogram"),
                Field("YieldMetresPerKilogram", "Yield in m/kg", false, "Yield (m/kg) Manual", "Yield (m/kg)", "Yield", "MetresPerKilogram", "m/kg"),
                Field("NominalOutsideDiameterMillimetres", "Outside diameter", false, "Nom OD (mm)", "OutsideDiameter", "Outside Diameter", "OD", "Diameter"),
                Field("NominalAreaSquareMillimetres", "Nominal area", false, "mm²", "mm2", "NominalArea", "CrossSectionalArea"),
                Field("WorkbookAwg", "AWG", false, "AWG", "Gauge"),
                Field("ManufacturingCostPerKilogram", "Manufacturing cost", false, "Manufature Cost", "Manufacture Cost", "Manufacturing Cost"),
                Field("CopperCostPerKilogram", "Copper cost", false, "Copper Cost (£/kg)", "Copper Cost", "CopperCost"),
                Field("CopperIncludingPremiumPerKilogram", "Copper including premium", false, "Copper inc Premium", "Copper Including Premium"),
                Field("NetWeightKilograms", "Reel net weight", false, "Net Weight", "Reel Net Weight", "NetWeight"),
                Field("LengthMetres", "Reel length", false, "Length", "Reel Length", "Conductor Length"),
                Field("VolumeCubicMillimetresPerMetre", "Volume per metre", false, "Volume (mm³/m)", "Volume (mm3/m)", "Volume per Metre", "Conductor Volume per Metre"),
            ],
            CentralDataArea.Compounds =>
            [
                Field("Id", "Material ID", false, "ID", "CompoundId", "MaterialId"),
                Field("CompoundName", "Compound", true, "Compound", "CompoundName", "Name"),
                Field("Supplier", "Supplier", true, "Company", "Supplier", "CompoundSupplier"),
                Field("PricePerKilogram", "Price per kg", true, "Cost (£/kg)", "£/kg", "Price", "PricePerKilogram", "CostPerKg"),
                Field("SpecificGravity", "Specific gravity", true, "Specific Gravity", "SpecificGravity", "SG"),
                Field("MaterialType", "Material type", false, "Type", "MaterialType", "PolymerType"),
                Field("Description", "Description", false, "Material Description", "Description"),
                Field("HasDataSheet", "Data sheet", false, "Data Sheet", "HasDataSheet"),
            ],
            CentralDataArea.Masterbatch =>
            [
                Field("ColourCode", "Colour code", false, "Colour Code", "ColourCode", "Code"),
                Field("ColourName", "Colour name", true, "Colour", "ColourName", "Name"),
                Field("Supplier", "Supplier", true, "Colour Supplier", "Supplier", "Company"),
                Field("PricePerKilogram", "Price per kg", true, "£/kg", "Cost (£/kg)", "Price", "PricePerKilogram"),
                Field("ColourHex", "Colour preview", false, "Colour Hex", "ColourHex", "Hex"),
                Field("ColourType", "Colour type", false, "Colour Type", "ColourType"),
                Field("RalEquivalent", "RAL equivalent", false, "RAL Number Equivalent", "RAL", "RalEquivalent"),
                Field("PvcUse", "PVC compatible", false, "PVC Use", "PVC Compatible", "PVC Compatibility"),
                Field("PvcMaxTemp", "PVC maximum temperature", false, "PVC Max Temp", "PVC Maximum Temperature", "PVC Temperature"),
                Field("PePpPurUse", "PE/PP/PUR compatible", false, "PE/PP/PUR Use", "PE PP PUR Use", "PE/PP/PUR Compatible"),
                Field("PePpPurMaxTemp", "PE/PP/PUR maximum temperature", false, "PE/PP/PUR Max Temp", "PE PP PUR Max Temp", "PE/PP/PUR Maximum Temperature"),
                Field("PsUse", "PS compatible", false, "PS Use", "PS Compatible", "PS Compatibility"),
                Field("PsMaxTemp", "PS maximum temperature", false, "PS Max Temp", "PS Maximum Temperature", "PS Temperature"),
                Field("AbsUse", "ABS compatible", false, "ABS Use", "ABS Compatible", "ABS Compatibility"),
                Field("AbsMaxTemp", "ABS maximum temperature", false, "ABS Max Temp", "ABS Maximum Temperature", "ABS Temperature"),
                Field("AcetalUse", "ACETAL compatible", false, "ACETAL Use", "Acetal Use", "ACETAL Compatible"),
                Field("AcetalMaxTemp", "ACETAL maximum temperature", false, "ACETAL Max Temp", "Acetal Max Temp", "ACETAL Maximum Temperature"),
                Field("PbtUse", "PBT compatible", false, "PBT Use", "PBT Compatible", "PBT Compatibility"),
                Field("PbtMaxTemp", "PBT maximum temperature", false, "PBT Max Temp", "PBT Maximum Temperature", "PBT Temperature"),
                Field("NylonUse", "Nylon compatible", false, "Nylon Use", "NYLON Use", "Nylon Compatible"),
                Field("NylonMaxTemp", "Nylon maximum temperature", false, "Nylon Max Temp", "NYLON Max Temp", "Nylon Maximum Temperature"),
                Field("PcPesUse", "PC/PES compatible", false, "PC/PES Use", "PC PES Use", "PC/PES Compatible"),
                Field("PcPesMaxTemp", "PC/PES maximum temperature", false, "PC/PES Max Temp", "PC PES Max Temp", "PC/PES Maximum Temperature"),
            ],
            CentralDataArea.Contacts =>
            [
                Field("Id", "Contact ID", false, "UniqueCusRef", "ID", "ContactId"),
                Field("AccountName", "Account name", true, "Account Name", "AccountName", "Company", "Customer"),
                Field("ShortName", "Short name", false, "Short Name", "ShortName"),
                Field("AddressLine1", "Address line 1", false, "Address Line 1", "AddressLine1"),
                Field("AddressLine2", "Address line 2", false, "Address Line 2", "AddressLine2"),
                Field("AddressLine3", "Address line 3", false, "Address Line 3", "AddressLine3"),
                Field("AddressLine4", "Address line 4", false, "Address Line 4", "AddressLine4"),
                Field("PostCode", "Post code", false, "Post/Zip Code", "PostCode", "PostalCode"),
                Field("PhoneNumber", "Phone number", false, "Phone Number", "PhoneNumber", "Telephone"),
                Field("PersonalEmail", "Personal email", false, "PersonalEmail", "Personal Email"),
                Field("SalesEmail", "Sales email", false, "SalesEmail", "Sales Email"),
                Field("AccountsEmail", "Accounts email", false, "AccountsEmail", "Accounts Email"),
                Field("IsAssemblyCustomer", "Assembly customer", false, "AccTypeAssemblyCust"),
                Field("IsCableCustomer", "Cable customer", false, "AccTypeCableCust"),
                Field("IsCompoundSupplier", "Compound supplier", false, "AccTypeCompSupp"),
                Field("IsConductorSupplier", "Conductor supplier", false, "AccTypeCondSupp"),
                Field("IsPartSupplier", "Part supplier", false, "AccTypePartSupp"),
                Field("IsOtherSupplier", "Other supplier", false, "AccTypeOtherSupp"),
                Field("IsOtherCustomer", "Other customer", false, "AccTypeOtherCust"),
            ],
            CentralDataArea.Operators =>
            [
                Field("Id", "Operator ID", false, "ID", "OperatorId"),
                Field("LastName", "Last name", false, "Last Name", "LastName", "Surname"),
                Field("MiddleNames", "Middle names", false, "Middle Name(s)", "MiddleNames"),
                Field("FirstName", "First name", true, "First Name", "FirstName", "Forename"),
                Field("Initials", "Initials", false, "Initials"),
                Field("Assembly", "Assembly", false, "Assembly"),
                Field("Production", "Production", false, "Production"),
                Field("Office", "Office", false, "Office"),
                Field("Other", "Other", false, "Other"),
                Field("QualityControl", "Quality control", false, "Quality Control", "QualityControl"),
                Field("Grn", "GRN", false, "GRN"),
                Field("Employee", "Current employee", false, "Employee"),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(area)),
        };

    public static IReadOnlyList<CentralDataFieldMatch> Match(
        CentralDataArea area,
        IEnumerable<string> sourceColumns,
        IReadOnlyDictionary<string, string>? existingMappings = null)
        => Match(
            area,
            sourceColumns.Select((name, index) =>
                new CentralDataPreviewColumn(name, "Unknown", index, true)),
            existingMappings);

    public static IReadOnlyList<CentralDataFieldMatch> Match(
        CentralDataArea area,
        IEnumerable<CentralDataPreviewColumn> sourceColumns,
        IReadOnlyDictionary<string, string>? existingMappings = null)
    {
        var columns = sourceColumns.ToArray();
        var byNormalizedName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
        {
            foreach (var candidate in column.MatchNames)
            {
                var normalized = NormalizeName(candidate);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    byNormalizedName.TryAdd(normalized, column.Name);
                }
            }
        }

        return Fields(area)
            .Select(
                field =>
                {
                    if (existingMappings?.TryGetValue(field.Key, out var existing) == true)
                    {
                        if (string.IsNullOrWhiteSpace(existing))
                        {
                            return new CentralDataFieldMatch(field, null, false);
                        }

                        if (columns.Any(column => string.Equals(column.Name, existing, StringComparison.OrdinalIgnoreCase)))
                        {
                            return new CentralDataFieldMatch(field, existing, false);
                        }
                    }

                    var match = field.Aliases
                        .Select(NormalizeName)
                        .Where(byNormalizedName.ContainsKey)
                        .Select(alias => byNormalizedName[alias])
                        .FirstOrDefault();
                    return new CentralDataFieldMatch(field, match, match is not null);
                })
            .ToArray();
    }

    public static bool HasAllRequiredMatches(
        IEnumerable<CentralDataFieldMatch> matches) =>
        matches.All(match => !match.Field.IsRequired || match.IsResolved);

    private static CentralDataFieldDefinition Field(
        string key,
        string label,
        bool required,
        params string[] aliases) =>
        new(key, label, required, aliases);

    private static string NormalizeName(string value)
    {
        var normalized = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(character))
            {
                normalized.Append(char.ToUpperInvariant(character));
            }
        }

        return normalized.ToString();
    }
}
