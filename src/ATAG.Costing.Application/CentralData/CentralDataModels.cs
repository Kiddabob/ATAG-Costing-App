using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ATAG.Costing.Domain.Conductors;

namespace ATAG.Costing.Application.CentralData;

public enum CentralDataSourceKind
{
    EmbeddedSnapshot = 0,
    LinkedWorkbook = 1,
    SqlDatabase = 2,
    AccessDatabase = 3,
}

public enum CentralDataArea
{
    Copper = 0,
    Compounds = 1,
    Masterbatch = 2,
    Contacts = 3,
    Operators = 4,
}

public sealed record CentralDataValueDerivation(
    string FieldKey,
    string DisplayName,
    string Formula,
    string SourceSummary,
    bool IsEstimate = false,
    string RuleVersion = "copper-reference-derivation/v1");

public sealed record CentralDataSourceConfiguration(
    CentralDataSourceKind Kind,
    string DisplayName,
    string? WorkbookPath = null,
    string? SqlServer = null,
    string? SqlDatabase = null,
    bool UseWindowsAuthentication = true)
{
    public static CentralDataSourceConfiguration Unconfigured { get; } = new(
        CentralDataSourceKind.EmbeddedSnapshot,
        "No central data linked");
}

public sealed record CopperReference(
    string Id,
    string Description,
    string Supplier,
    decimal PricePerKilogram,
    decimal YieldMetresPerKilogram,
    decimal NominalOutsideDiameterMillimetres,
    decimal NominalAreaSquareMillimetres = 0m,
    string? WorkbookAwg = null,
    IReadOnlyList<CentralDataValueDerivation>? DerivedValues = null)
{
    [JsonIgnore]
    public IReadOnlyList<CentralDataValueDerivation> EffectiveDerivedValues =>
        DerivedValues ?? [];

    [JsonIgnore]
    public bool HasDerivedValues => EffectiveDerivedValues.Count > 0;

    [JsonIgnore]
    public bool HasEstimatedValues =>
        EffectiveDerivedValues.Any(value => value.IsEstimate);

    [JsonIgnore]
    public string DerivationSummary => string.Join(
        " ",
        EffectiveDerivedValues.Select(value =>
            $"{value.DisplayName}: {value.Formula} ({value.SourceSummary}) " +
            $"[{value.RuleVersion}]."));

    [JsonIgnore]
    public string MaterialTypeCode => IdentifyMaterialType(Description).Code;

    [JsonIgnore]
    public string MaterialTypeDisplay => IdentifyMaterialType(Description).Display;

    [JsonIgnore]
    public bool IsSupplierDefinedConstruction => Construction is null;

    [JsonIgnore]
    public ConductorConstructionResult? Construction =>
        ConductorConstructionCalculator.TryCalculate(
            Description,
            NominalAreaSquareMillimetres);

    [JsonIgnore]
    public string DisplayDescription =>
        Construction?.NormalizedDescription ?? Description;

    [JsonIgnore]
    public bool IsCostingReady =>
        PricePerKilogram > 0m &&
        YieldMetresPerKilogram > 0m &&
        NominalOutsideDiameterMillimetres > 0m;

    [JsonIgnore]
    public bool IsSelectableForCosting =>
        !string.IsNullOrWhiteSpace(Description) &&
        !string.IsNullOrWhiteSpace(Supplier) &&
        YieldMetresPerKilogram > 0m &&
        (NominalOutsideDiameterMillimetres > 0m || Construction is not null);

    [JsonIgnore]
    public string NominalAreaDisplay =>
        NominalAreaSquareMillimetres > 0m
            ? $"{NominalAreaSquareMillimetres:0.###} mm²" +
              OriginSuffix("NominalAreaSquareMillimetres")
            : "—";

    [JsonIgnore]
    public string NominalOutsideDiameterDisplay =>
        NominalOutsideDiameterMillimetres > 0m
            ? $"{NominalOutsideDiameterMillimetres:0.###} mm" +
              OriginSuffix("NominalOutsideDiameterMillimetres")
            : "—";

    [JsonIgnore]
    public string CalculatedAreaDisplay =>
        Construction is null
            ? "—"
            : $"{Construction.CalculatedMetalAreaSquareMillimetres:0.###} mm²";

    [JsonIgnore]
    public string AwgDisplay =>
        Construction is null
            ? WorkbookAwg ?? "—"
            : Construction.NearestAwg;

    [JsonIgnore]
    public string ClassDisplay =>
        Construction?.ConductorClassDisplay ?? "Not established";

    [JsonIgnore]
    public string PriceDisplay =>
        PricePerKilogram > 0m
            ? $"£{PricePerKilogram:0.#####}/kg" +
              OriginSuffix("PricePerKilogram")
            : "No price";

    [JsonIgnore]
    public string YieldDisplay =>
        YieldMetresPerKilogram > 0m
            ? $"{YieldMetresPerKilogram:0.######} m/kg" +
              OriginSuffix("YieldMetresPerKilogram")
            : "No yield";

    private string OriginSuffix(string fieldKey)
    {
        var derivation = EffectiveDerivedValues.FirstOrDefault(value =>
            string.Equals(
                value.FieldKey,
                fieldKey,
                StringComparison.OrdinalIgnoreCase));
        return derivation is null
            ? string.Empty
            : derivation.IsEstimate
                ? " (estimated)"
                : " (calculated)";
    }

    private static (string Code, string Display) IdentifyMaterialType(
        string description)
    {
        if (Regex.IsMatch(description, @"\bTCW\b", RegexOptions.IgnoreCase))
        {
            return ("TCW", "TCW - Tinned copper wire");
        }

        if (Regex.IsMatch(description, @"\bPCW\b", RegexOptions.IgnoreCase))
        {
            return ("PCW", "PCW - Plain copper wire");
        }

        if (Regex.IsMatch(
                description,
                @"\b(?:TI|TITANIUM)\b",
                RegexOptions.IgnoreCase))
        {
            return ("TI", "TI - Titanium wire");
        }

        if (description.Contains("Tinsel", StringComparison.OrdinalIgnoreCase))
        {
            return ("TINSEL", "Tinsel wire");
        }

        if (description.Contains(
                "Silver plated",
                StringComparison.OrdinalIgnoreCase))
        {
            return ("SILVER", "Silver-plated wire");
        }

        if (description.Contains(
                "Stainless",
                StringComparison.OrdinalIgnoreCase))
        {
            return ("STAINLESS", "Stainless steel wire");
        }

        if (Regex.IsMatch(description, @"\bH72\b", RegexOptions.IgnoreCase))
        {
            return ("H72", "H72 bronze / strand");
        }

        if (Regex.IsMatch(description, @"\bTW\b", RegexOptions.IgnoreCase))
        {
            return ("TW", "TW wire");
        }

        if (description.Contains("Braid", StringComparison.OrdinalIgnoreCase))
        {
            return ("BRAID", "Braid wire");
        }

        if (description.Contains("Multi", StringComparison.OrdinalIgnoreCase))
        {
            return ("MULTI", "Multi wire");
        }

        if (description.Contains("Hystral", StringComparison.OrdinalIgnoreCase))
        {
            return ("HYSTRAL", "Hystral");
        }

        if (description.Contains("Simplex", StringComparison.OrdinalIgnoreCase))
        {
            return ("SIMPLEX", "Simplex");
        }

        return ("OTHER", "Unspecified / other");
    }
}

public sealed record CompoundReference(
    string Id,
    string CompoundName,
    string Supplier,
    decimal PricePerKilogram,
    decimal SpecificGravity,
    string MaterialType,
    string Description,
    bool HasDataSheet = false)
{
    [JsonIgnore]
    public bool IsCostingReady =>
        PricePerKilogram > 0m &&
        SpecificGravity > 0m;

    [JsonIgnore]
    public string PriceDisplay =>
        PricePerKilogram > 0m ? $"£{PricePerKilogram:0.#####}/kg" : "No price";
}

public sealed record MasterbatchReference(
    string ColourCode,
    string ColourName,
    string Supplier,
    decimal PricePerKilogram,
    string Compatibility,
    string? ColourHex = null,
    string ColourType = "",
    string? RalEquivalent = null,
    string TemperatureLimits = "",
    IReadOnlyList<MasterbatchMaterialLimit>? MaterialLimits = null)
{
    [JsonIgnore]
    public bool IsCostingReady => PricePerKilogram > 0m;

    [JsonIgnore]
    public string PriceDisplay =>
        PricePerKilogram > 0m ? $"£{PricePerKilogram:N5}/kg" : "No price";

    [JsonIgnore]
    public string ColourSearchDescription =>
        MasterbatchColourSearch.Describe(this);

    [JsonIgnore]
    public IReadOnlyList<MasterbatchCompatibilityCell> CompatibilityCells =>
        MasterbatchCompatibilityCell.Create(this);

    [JsonIgnore]
    public IReadOnlyList<MasterbatchMaterialLimit> EffectiveMaterialLimits =>
        MaterialLimits ?? [];

    public override string ToString() =>
        $"{ColourName} · {ColourCode} · {Supplier}";
}

public sealed record MasterbatchMaterialLimit(
    string MaterialFamily,
    bool? IsCompatible,
    string MaximumTemperature);

public sealed record MasterbatchCompatibilityCell(
    string MaterialFamily,
    string TemperatureDisplay,
    bool IsCompatible,
    bool IsRecorded = true)
{
    private static readonly string[] Families =
    [
        "PVC",
        "PE/PP/PUR",
        "PS",
        "ABS",
        "ACETAL",
        "PBT",
        "NYLON",
        "PC/PES",
    ];

    public string BackgroundHex =>
        !IsRecorded ? "#4A4029" : IsCompatible ? "#21402C" : "#342F30";

    public static IReadOnlyList<MasterbatchCompatibilityCell> Create(
        MasterbatchReference reference)
    {
        if (reference.EffectiveMaterialLimits.Count > 0)
        {
            return Families
                .Select(
                    family =>
                    {
                        var limit = reference.EffectiveMaterialLimits.FirstOrDefault(
                            item => string.Equals(
                                item.MaterialFamily,
                                family,
                                StringComparison.OrdinalIgnoreCase));
                        return new MasterbatchCompatibilityCell(
                            family,
                            limit?.IsCompatible == true
                                ? limit.MaximumTemperature
                                : string.Empty,
                            limit?.IsCompatible == true,
                            limit?.IsCompatible is not null);
                    })
                .ToArray();
        }

        var compatibleFamilies = reference.Compatibility
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
        var temperatureSegments = reference.TemperatureLimits
            .Split(
                ['·', ',', ';'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        return Families
            .Select(
                family =>
                {
                    var compatible = compatibleFamilies.Any(
                        item => string.Equals(
                            item,
                            family,
                            StringComparison.OrdinalIgnoreCase));
                    var temperature = compatible
                        ? temperatureSegments.FirstOrDefault(
                              segment => segment.StartsWith(
                                  family + " ",
                                  StringComparison.OrdinalIgnoreCase)) ?? ""
                        : "";
                    if (!string.IsNullOrWhiteSpace(temperature))
                    {
                        temperature = temperature[family.Length..].Trim();
                    }

                    return new MasterbatchCompatibilityCell(
                        family,
                        temperature,
                        compatible,
                        !reference.Compatibility.Contains(
                            "not recorded",
                            StringComparison.OrdinalIgnoreCase));
                })
            .ToArray();
    }
}

public sealed record ContactReference(
    string Id,
    string AccountName,
    string ShortName,
    string AddressLine1,
    string AddressLine2,
    string AddressLine3,
    string AddressLine4,
    string PostCode,
    string PhoneNumber,
    string PersonalEmail,
    string SalesEmail,
    string AccountsEmail,
    bool IsAssemblyCustomer,
    bool IsCableCustomer,
    bool IsCompoundSupplier,
    bool IsConductorSupplier,
    bool IsPartSupplier,
    bool IsOtherSupplier,
    bool IsOtherCustomer)
{
    public override string ToString() => AccountName;
}

public sealed record OperatorReference(
    string Id,
    string LastName,
    string MiddleNames,
    string FirstName,
    string Initials,
    bool Assembly,
    bool Production,
    bool Office,
    bool Other,
    bool QualityControl,
    bool Grn,
    bool Employee)
{
    [JsonIgnore]
    public string DisplayName =>
        string.Join(
            " ",
            new[] { FirstName, MiddleNames, LastName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
}

public sealed record CentralDataSnapshot(
    int SchemaVersion,
    string Revision,
    DateTimeOffset CapturedAt,
    string SourceLabel,
    IReadOnlyList<CopperReference> Copper,
    IReadOnlyList<CompoundReference> Compounds,
    IReadOnlyList<MasterbatchReference> Masterbatches,
    IReadOnlyList<ContactReference>? Contacts = null,
    IReadOnlyList<OperatorReference>? Operators = null)
{
    [JsonIgnore]
    public IReadOnlyList<ContactReference> EffectiveContacts =>
        Contacts ?? [];

    [JsonIgnore]
    public IReadOnlyList<OperatorReference> EffectiveOperators =>
        Operators ?? [];
}

public sealed record CentralDataTableLink(
    CentralDataArea Area,
    CentralDataSourceKind SourceKind,
    string DisplayName,
    string TableName,
    IReadOnlyDictionary<string, string> ColumnMappings,
    string? AccessDatabasePath = null,
    string? SqlServer = null,
    string? SqlDatabase = null,
    bool UseWindowsAuthentication = true,
    string? SchemaName = null,
    CentralDataObjectKind ObjectKind = CentralDataObjectKind.Table,
    IReadOnlyList<CentralDataQueryStep>? QuerySteps = null)
{
    [JsonIgnore]
    public IReadOnlyList<CentralDataQueryStep> EffectiveQuerySteps =>
        QuerySteps ?? [];
}

/// <summary>
/// The complete transformed database object retained after a successful import.
/// Costing-specific records are a validated projection of this table; columns
/// which are not currently used by a costing remain available for later mapping,
/// traceability, and future features.
/// </summary>
public sealed record CentralDataRetainedTable(
    CentralDataArea Area,
    string DisplayName,
    string TableName,
    string? SchemaName,
    CentralDataObjectKind ObjectKind,
    DateTimeOffset CapturedAt,
    IReadOnlyList<CentralDataPreviewColumn> Columns,
    IReadOnlyList<CentralDataPreviewRow> Rows,
    IReadOnlyList<CentralDataPreviewIssue>? Issues = null,
    IReadOnlyList<CentralDataQueryStep>? QuerySteps = null)
{
    [JsonIgnore]
    public IReadOnlyList<CentralDataPreviewIssue> EffectiveIssues => Issues ?? [];

    [JsonIgnore]
    public IReadOnlyList<CentralDataQueryStep> EffectiveQuerySteps => QuerySteps ?? [];
}

public sealed record CentralDataState(
    CentralDataSourceConfiguration Configuration,
    CentralDataSnapshot Snapshot,
    IReadOnlyList<CentralDataTableLink>? TableLinks = null,
    IReadOnlyList<CentralDataRetainedTable>? RetainedTables = null)
{
    [JsonIgnore]
    public IReadOnlyList<CentralDataTableLink> EffectiveTableLinks =>
        TableLinks ?? [];

    [JsonIgnore]
    public IReadOnlyList<CentralDataRetainedTable> EffectiveRetainedTables =>
        RetainedTables ?? [];
}

public sealed record CentralDataReadResult(
    bool Succeeded,
    CentralDataSnapshot? Snapshot,
    string Message)
{
    public static CentralDataReadResult Success(
        CentralDataSnapshot snapshot,
        string message) =>
        new(true, snapshot, message);

    public static CentralDataReadResult Failure(string message) =>
        new(false, null, message);
}

public sealed record CentralDataRefreshResult(
    CentralDataState State,
    bool Updated,
    bool UsedRetainedSnapshot,
    string Message,
    IReadOnlyList<CentralDataAreaRefreshResult>? AreaResults = null)
{
    [JsonIgnore]
    public IReadOnlyList<CentralDataAreaRefreshResult> EffectiveAreaResults =>
        AreaResults ?? [];
}

public sealed record CentralDataAreaRefreshResult(
    CentralDataArea Area,
    bool Updated,
    bool UsedRetainedSnapshot,
    string Message);
