using System.Globalization;
using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Materials;

namespace ATAG.Costing.Domain.Costing;

public readonly record struct TargetMarginRateFraction
{
    public TargetMarginRateFraction(decimal value)
    {
        if (value < 0 || value >= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Target gross margin must be at least zero and less than 100%.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public sealed record CommercialPricingInputs(
    decimal MaterialCost,
    decimal LabourCost,
    RiskRateFraction RiskRate,
    MarkupRateFraction MarkupRate,
    TargetMarginRateFraction TargetMarginRate);

public sealed record CommercialPricingResult(
    decimal EstimatedCost,
    decimal RiskValue,
    decimal RiskAdjustedCost,
    decimal MarkupValue,
    decimal SequentialRiskThenMarkupPrice,
    decimal CombinedRiskAndMarkupPrice,
    decimal TargetGrossMarginPrice,
    IReadOnlyList<CalculationStep> Steps);

/// <summary>
/// Applies commercial adjustments to the complete estimated cost. The
/// sequential risk-then-markup method is the working V1 result; the additive
/// and target-margin values are clearly labelled comparison methods.
/// </summary>
public static class CommercialPricingCalculator
{
    public const string RuleVersion = "commercial-pricing/v1";

    public static CommercialPricingResult Calculate(CommercialPricingInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        if (inputs.MaterialCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Material cost cannot be negative.");
        }

        if (inputs.LabourCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Labour cost cannot be negative.");
        }

        var materialStep = InputStep(
            "commercial-material-cost",
            "Material cost",
            "The complete pre-risk, pre-markup material cost.",
            inputs.MaterialCost,
            2,
            "£");
        var labourStep = InputStep(
            "commercial-labour-cost",
            "Labour cost",
            "The complete pre-risk, pre-markup production labour cost.",
            inputs.LabourCost,
            2,
            "£");
        var estimatedCost = inputs.MaterialCost + inputs.LabourCost;
        var estimatedCostStep = DerivedStep(
            "total-estimated-cost",
            "Total estimated cost",
            "Material and labour cost before commercial adjustments.",
            "MaterialCost + LabourCost",
            $"{Raw(inputs.MaterialCost)} £ + {Raw(inputs.LabourCost)} £",
            estimatedCost,
            2,
            "£",
            [materialStep, labourStep]);

        var riskRate = inputs.RiskRate.Value;
        var riskStep = PercentageStep(
            "commercial-risk-rate",
            "Risk",
            "The risk allowance applied to total estimated cost before markup.",
            riskRate);
        var riskValue = estimatedCost * riskRate;
        var riskValueStep = DerivedStep(
            "commercial-risk-value",
            "Risk value",
            "The monetary value of the risk allowance.",
            "EstimatedCost × RiskRate",
            $"{Raw(estimatedCost)} £ × {Raw(riskRate)}",
            riskValue,
            2,
            "£",
            [estimatedCostStep, riskStep]);
        var riskAdjusted = estimatedCost + riskValue;
        var riskAdjustedStep = DerivedStep(
            "commercial-risk-adjusted-cost",
            "Cost including risk",
            "Total estimated cost plus the separate risk value.",
            "EstimatedCost + RiskValue",
            $"{Raw(estimatedCost)} £ + {Raw(riskValue)} £",
            riskAdjusted,
            2,
            "£",
            [estimatedCostStep, riskValueStep]);

        var markupRate = inputs.MarkupRate.Value;
        var markupStep = PercentageStep(
            "commercial-markup-rate",
            "Markup",
            "The markup rate applied after risk.",
            markupRate);
        var markupValue = riskAdjusted * markupRate;
        var markupValueStep = DerivedStep(
            "commercial-markup-value",
            "Markup value",
            "The monetary markup applied to the risk-adjusted cost.",
            "RiskAdjustedCost × MarkupRate",
            $"{Raw(riskAdjusted)} £ × {Raw(markupRate)}",
            markupValue,
            2,
            "£",
            [riskAdjustedStep, markupStep]);
        var sequentialPrice = riskAdjusted + markupValue;
        var sequentialStep = DerivedStep(
            "sequential-risk-then-markup-price",
            "Recommended selling price: risk then markup",
            "The working V1 commercial method. Risk is applied first, followed by markup.",
            "EstimatedCost × (1 + RiskRate) × (1 + MarkupRate)",
            $"{Raw(estimatedCost)} £ × (1 + {Raw(riskRate)}) × (1 + {Raw(markupRate)})",
            sequentialPrice,
            2,
            "£",
            [riskAdjustedStep, markupValueStep]);

        var combinedPrice = estimatedCost * (1m + riskRate + markupRate);
        var combinedStep = DerivedStep(
            "combined-risk-and-markup-price",
            "Alternative: add risk and markup rates",
            "A comparison method that adds both rates before applying them once to estimated cost.",
            "EstimatedCost × (1 + RiskRate + MarkupRate)",
            $"{Raw(estimatedCost)} £ × (1 + {Raw(riskRate)} + {Raw(markupRate)})",
            combinedPrice,
            2,
            "£",
            [estimatedCostStep, riskStep, markupStep]);

        var targetMargin = inputs.TargetMarginRate.Value;
        var targetMarginStep = PercentageStep(
            "target-gross-margin-rate",
            "Target gross margin",
            "A selling-price target expressed as gross profit divided by selling price. It is not markup.",
            targetMargin);
        var marginPrice = riskAdjusted / (1m - targetMargin);
        var marginPriceStep = DerivedStep(
            "target-gross-margin-price",
            "Alternative: target gross margin price",
            "A comparison price that achieves the selected gross margin after risk.",
            "RiskAdjustedCost ÷ (1 - TargetMarginRate)",
            $"{Raw(riskAdjusted)} £ ÷ (1 - {Raw(targetMargin)})",
            marginPrice,
            2,
            "£",
            [riskAdjustedStep, targetMarginStep]);

        return new CommercialPricingResult(
            estimatedCost,
            riskValue,
            riskAdjusted,
            markupValue,
            sequentialPrice,
            combinedPrice,
            marginPrice,
            [
                materialStep,
                labourStep,
                estimatedCostStep,
                riskStep,
                riskValueStep,
                riskAdjustedStep,
                markupStep,
                markupValueStep,
                sequentialStep,
                combinedStep,
                targetMarginStep,
                marginPriceStep,
            ]);
    }

    private static CalculationStep PercentageStep(
        string id,
        string label,
        string businessMeaning,
        decimal value) =>
        new(
            id,
            label,
            "Input",
            $"{Raw(value)} fraction ({Raw(value * 100m)}%)",
            value,
            Display(value * 100m, 2),
            "%",
            BusinessMeaning: businessMeaning,
            RoundingRule: DisplayRule(2),
            RuleVersion: RuleVersion);

    private static CalculationStep InputStep(
        string id,
        string label,
        string businessMeaning,
        decimal value,
        int decimalPlaces,
        string unit) =>
        new(
            id,
            label,
            "Input",
            $"{Raw(value)} {unit}",
            value,
            Display(value, decimalPlaces),
            unit,
            BusinessMeaning: businessMeaning,
            RoundingRule: DisplayRule(decimalPlaces),
            RuleVersion: RuleVersion);

    private static CalculationStep DerivedStep(
        string id,
        string label,
        string businessMeaning,
        string expression,
        string substitutedExpression,
        decimal value,
        int decimalPlaces,
        string unit,
        IReadOnlyList<CalculationStep> inputs) =>
        new(
            id,
            label,
            expression,
            $"{substitutedExpression} = {Raw(value)} {unit}",
            value,
            Display(value, decimalPlaces),
            unit,
            inputs,
            BusinessMeaning: businessMeaning,
            RoundingRule: DisplayRule(decimalPlaces),
            RuleVersion: RuleVersion);

    private static string Display(decimal value, int decimalPlaces) =>
        decimal.Round(value, decimalPlaces, MidpointRounding.AwayFromZero)
            .ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);

    private static string DisplayRule(int decimalPlaces) =>
        $"No calculation rounding; display is rounded to {decimalPlaces} decimal places " +
        "using midpoint-away-from-zero.";

    private static string Raw(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
