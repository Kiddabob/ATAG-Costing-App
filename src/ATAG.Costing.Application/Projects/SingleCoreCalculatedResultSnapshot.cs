namespace ATAG.Costing.Application.Projects;

/// <summary>
/// Exact displayed outputs and raw quotation value saved with a revision.
/// Calculation engines remain authoritative for working copies; an approved
/// revision uses this snapshot to reproduce what was approved.
/// </summary>
public sealed record SingleCoreCalculatedResultSnapshot
{
    public decimal RecommendedQuotePrice { get; init; }
    public string EffectiveCoreName { get; init; } = "";
    public string GeneratedCoreName { get; init; } = "";
    public string ConductorCostPerMetre { get; init; } = "";
    public string CompoundCostPerMetre { get; init; } = "";
    public string MasterbatchCostPerMetre { get; init; } = "";
    public string CoreMaterialCostPerMetre { get; init; } = "";
    public string CoreMaterialQuote { get; init; } = "";
    public string RiskAdjustedCostPerMetre { get; init; } = "";
    public string RiskAdjustedQuote { get; init; } = "";
    public string MarkedUpCostPerMetre { get; init; } = "";
    public string MarkedUpQuote { get; init; } = "";
    public string RecommendedLineSpeed { get; init; } = "";
    public string EffectiveLineSpeed { get; init; } = "";
    public string ProductionRunningTime { get; init; } = "";
    public string TotalProductionTime { get; init; } = "";
    public string ChargeableLabourHours { get; init; } = "";
    public string LabourCost { get; init; } = "";
    public string LabourCostPerMetre { get; init; } = "";
    public string TotalEstimatedCost { get; init; } = "";
    public string RiskValue { get; init; } = "";
    public string MarkupValue { get; init; } = "";
    public string CombinedRatePrice { get; init; } = "";
    public string TargetMarginPrice { get; init; } = "";
    public string ConductorQuoteMass { get; init; } = "";
    public string CompoundQuoteMass { get; init; } = "";
    public string MasterbatchQuoteMass { get; init; } = "";
    public string ConductorQuotePrice { get; init; } = "";
    public string CompoundQuotePrice { get; init; } = "";
    public string MasterbatchQuotePrice { get; init; } = "";
    public IReadOnlyList<SavedCalculationSection> Trace { get; init; } = [];
}

public sealed record SavedCalculationSection(
    string Id,
    string Label,
    IReadOnlyList<SavedCalculationStep> Steps);

/// <summary>
/// Serializable calculation evidence. Inputs are recursive so dependency
/// meaning is retained rather than reduced to display-only rows.
/// </summary>
public sealed record SavedCalculationStep(
    string Id,
    string Label,
    string Expression,
    string SubstitutedExpression,
    decimal RawValue,
    string DisplayValue,
    string Unit,
    IReadOnlyList<SavedCalculationStep> Inputs,
    string? Warning,
    string? BusinessMeaning,
    string? RoundingRule,
    string? RuleVersion);
