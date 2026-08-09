using System.Globalization;
using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Materials;

namespace ATAG.Costing.Domain.Costing;

public sealed record ExtrusionProductionSettings(
    string ProcessName,
    Millimetres FinishedOutsideDiameter,
    ExtrusionLineSpeedProfile LineSpeedProfile,
    LineSpeedMetresPerHour? ManualLineSpeed,
    LabourHours SetupTime,
    OperatorCount Operators,
    HourlyLabourRate HourlyRate);

public sealed record DualInsulationProductionInputs(
    DualInsulationCostingResult MaterialCosting,
    ExtrusionProductionSettings FirstExtrusion,
    ExtrusionProductionSettings SecondExtrusion);

public sealed record DualInsulationProductionResult(
    ProductionLabourResult FirstExtrusion,
    ProductionLabourResult SecondExtrusion,
    LabourHours TotalProductionTime,
    LabourHours TotalChargeableLabourHours,
    decimal TotalLabourCost,
    PricePerMetre LabourCostPerFinishedMetre,
    IReadOnlyList<CalculationStep> Steps);

/// <summary>
/// Calculates the two extrusion processes independently. Each process owns its
/// own line-speed profile, set-up time, operators and labour rate. Masterbatch
/// is intentionally absent because it contributes material usage and cost, not
/// a separate production process.
/// </summary>
public static class DualInsulationProductionCalculator
{
    public const string RuleVersion = "dual-insulation-production/v1";

    public static DualInsulationProductionResult Calculate(
        DualInsulationProductionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(inputs.MaterialCosting);
        ArgumentNullException.ThrowIfNull(inputs.FirstExtrusion);
        ArgumentNullException.ThrowIfNull(inputs.SecondExtrusion);

        var first = CalculateExtrusion(
            inputs.FirstExtrusion,
            inputs.MaterialCosting.CoreProductionLength);
        var second = CalculateExtrusion(
            inputs.SecondExtrusion,
            inputs.MaterialCosting.SecondLayerProductionLength);

        var firstSteps = first.Steps
            .Select(step => PrefixStep(step, "first-extrusion-"))
            .ToArray();
        var secondSteps = second.Steps
            .Select(step => PrefixStep(step, "second-extrusion-"))
            .ToArray();

        var totalProductionTime =
            first.TotalProcessTime.Value +
            second.TotalProcessTime.Value;
        var totalChargeableHours =
            first.ChargeableLabourHours.Value +
            second.ChargeableLabourHours.Value;
        var totalLabourCost = first.LabourCost + second.LabourCost;
        var finishedLength =
            inputs.MaterialCosting.SecondLayerProductionLength.Value;
        var labourCostPerFinishedMetre = totalLabourCost / finishedLength;

        var firstCostStep = AssertStep(
            firstSteps,
            "first-extrusion-labour-cost-for-quote");
        var secondCostStep = AssertStep(
            secondSteps,
            "second-extrusion-labour-cost-for-quote");
        var totalCostStep = DerivedStep(
            "dual-extrusion-total-labour-cost",
            "Total dual-insulation extrusion labour cost",
            "The first- and second-extrusion labour costs added once each.",
            "FirstExtrusionLabourCost + SecondExtrusionLabourCost",
            $"{Raw(first.LabourCost)} £ + {Raw(second.LabourCost)} £",
            totalLabourCost,
            2,
            "£",
            [firstCostStep, secondCostStep]);
        var finishedLengthStep = new CalculationStep(
            "dual-extrusion-finished-length",
            "Finished quote length",
            "Input",
            $"{Raw(finishedLength)} m",
            finishedLength,
            Display(finishedLength, 0),
            "m",
            BusinessMeaning:
                "The customer quote length used to express total extrusion labour per finished metre.",
            RoundingRule: DisplayRule(0),
            RuleVersion: RuleVersion);
        var perMetreStep = DerivedStep(
            "dual-extrusion-labour-cost-per-finished-metre",
            "Dual-insulation labour cost per finished metre",
            "Both extrusion labour costs distributed across the customer quote length.",
            "TotalExtrusionLabourCost ÷ FinishedQuoteLength",
            $"{Raw(totalLabourCost)} £ ÷ {Raw(finishedLength)} m",
            labourCostPerFinishedMetre,
            4,
            "£/m",
            [totalCostStep, finishedLengthStep]);

        return new DualInsulationProductionResult(
            first,
            second,
            new LabourHours(totalProductionTime),
            new LabourHours(totalChargeableHours),
            totalLabourCost,
            new PricePerMetre(labourCostPerFinishedMetre),
            [
                .. firstSteps,
                .. secondSteps,
                totalCostStep,
                finishedLengthStep,
                perMetreStep,
            ]);
    }

    private static ProductionLabourResult CalculateExtrusion(
        ExtrusionProductionSettings settings,
        LengthMetres productionLength)
    {
        ArgumentNullException.ThrowIfNull(settings.LineSpeedProfile);
        return ProductionLabourCalculator.Calculate(
            new ProductionLabourInputs(
                settings.ProcessName,
                productionLength,
                settings.FinishedOutsideDiameter,
                settings.ManualLineSpeed,
                settings.SetupTime,
                settings.Operators,
                settings.HourlyRate,
                settings.LineSpeedProfile));
    }

    private static CalculationStep PrefixStep(
        CalculationStep step,
        string prefix) =>
        new(
            prefix + step.Id,
            step.Label,
            step.Expression,
            step.SubstitutedExpression,
            step.RawValue,
            step.DisplayValue,
            step.Unit,
            step.InputSteps
                .Select(input => PrefixStep(input, prefix))
                .ToArray(),
            step.Warning,
            step.BusinessMeaning,
            step.RoundingRule,
            step.RuleVersion);

    private static CalculationStep AssertStep(
        IReadOnlyList<CalculationStep> steps,
        string id) =>
        steps.FirstOrDefault(
            step => string.Equals(step.Id, id, StringComparison.Ordinal)) ??
        throw new InvalidOperationException(
            $"Required calculation step '{id}' was not produced.");

    private static CalculationStep DerivedStep(
        string id,
        string label,
        string businessMeaning,
        string expression,
        string substitutedExpression,
        decimal value,
        int decimalPlaces,
        string unit,
        IReadOnlyList<CalculationStep> inputSteps) =>
        new(
            id,
            label,
            expression,
            $"{substitutedExpression} = {Raw(value)} {unit}",
            value,
            Display(value, decimalPlaces),
            unit,
            inputSteps,
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
