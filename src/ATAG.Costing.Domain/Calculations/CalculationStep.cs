namespace ATAG.Costing.Domain.Calculations;

/// <summary>
/// One auditable stage in a costing calculation. Calculation engines return
/// these alongside their numerical results so the UI and reports can explain
/// exactly how every value was produced.
/// </summary>
public sealed record CalculationStep(
    string Id,
    string Label,
    string Expression,
    string SubstitutedExpression,
    decimal RawValue,
    string DisplayValue,
    string Unit,
    IReadOnlyList<CalculationStep>? Inputs = null,
    string? Warning = null,
    string? BusinessMeaning = null,
    string? RoundingRule = null,
    string? RuleVersion = null)
{
    public IReadOnlyList<CalculationStep> InputSteps { get; } =
        Inputs ?? Array.Empty<CalculationStep>();
}
