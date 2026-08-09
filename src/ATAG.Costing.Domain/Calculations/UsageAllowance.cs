namespace ATAG.Costing.Domain.Calculations;

public readonly record struct UsageAllowanceRateFraction
{
    public UsageAllowanceRateFraction(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "The waste/start-up allowance rate cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public sealed record UsageAllowanceResult(decimal Multiplier, decimal AdjustedUsage);

/// <summary>
/// Applies the general waste/start-up allowance to a usage quantity. This rule
/// is shared by material calculations and is not risk, markup, or margin.
/// </summary>
public static class UsageAllowanceCalculator
{
    public const string RuleVersion = "usage-allowance/v1";

    public static UsageAllowanceResult Apply(
        decimal baseUsage,
        UsageAllowanceRateFraction allowanceRate)
    {
        if (baseUsage < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseUsage),
                "Base usage cannot be negative.");
        }

        var multiplier = 1m + allowanceRate.Value;
        return new UsageAllowanceResult(multiplier, baseUsage * multiplier);
    }
}
