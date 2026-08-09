using System.Globalization;
using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Materials;

namespace ATAG.Costing.Domain.Costing;

public readonly record struct LineSpeedMetresPerHour
{
    public LineSpeedMetresPerHour(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Line speed must be greater than zero.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct LabourHours
{
    public LabourHours(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Labour hours cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct HourlyLabourRate
{
    public HourlyLabourRate(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "The hourly labour rate cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct OperatorCount
{
    public OperatorCount(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Operator count must be greater than zero.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public sealed record ExtrusionLineSpeedBand(
    Millimetres MaximumOutsideDiameter,
    LineSpeedMetresPerHour LineSpeed);

/// <summary>
/// A line-specific set of OD-to-speed rules. Different extrusion processes can
/// therefore select different production speeds for the same finished size.
/// </summary>
public sealed class ExtrusionLineSpeedProfile
{
    public ExtrusionLineSpeedProfile(
        string reference,
        string ruleVersion,
        IReadOnlyList<ExtrusionLineSpeedBand> bands,
        LineSpeedMetresPerHour aboveMaximumLineSpeed)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException(
                "A line-speed profile reference is required.",
                nameof(reference));
        }

        if (string.IsNullOrWhiteSpace(ruleVersion))
        {
            throw new ArgumentException(
                "A line-speed profile rule version is required.",
                nameof(ruleVersion));
        }

        ArgumentNullException.ThrowIfNull(bands);
        if (bands.Count == 0)
        {
            throw new ArgumentException(
                "A line-speed profile must contain at least one OD band.",
                nameof(bands));
        }

        var copiedBands = bands.ToArray();
        for (var index = 0; index < copiedBands.Length; index++)
        {
            if (copiedBands[index].MaximumOutsideDiameter.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bands),
                    "Every maximum outside diameter must be greater than zero.");
            }

            if (index > 0 &&
                copiedBands[index].MaximumOutsideDiameter.Value <=
                copiedBands[index - 1].MaximumOutsideDiameter.Value)
            {
                throw new ArgumentException(
                    "Line-speed OD bands must be strictly increasing.",
                    nameof(bands));
            }
        }

        Reference = reference.Trim();
        RuleVersion = ruleVersion.Trim();
        Bands = copiedBands;
        AboveMaximumLineSpeed = aboveMaximumLineSpeed;
    }

    public string Reference { get; }

    public string RuleVersion { get; }

    public IReadOnlyList<ExtrusionLineSpeedBand> Bands { get; }

    public LineSpeedMetresPerHour AboveMaximumLineSpeed { get; }

    public LineSpeedMetresPerHour Select(Millimetres finishedOutsideDiameter) =>
        Bands
            .FirstOrDefault(
                band =>
                    finishedOutsideDiameter.Value <=
                    band.MaximumOutsideDiameter.Value)
            ?.LineSpeed ??
        AboveMaximumLineSpeed;

    public string BandLabel(Millimetres finishedOutsideDiameter)
    {
        var selectedBand = Bands.FirstOrDefault(
            band =>
                finishedOutsideDiameter.Value <=
                band.MaximumOutsideDiameter.Value);
        return selectedBand is null
            ? $"{Reference}: finished OD > " +
              $"{Bands[^1].MaximumOutsideDiameter.Value} mm"
            : $"{Reference}: finished OD ≤ " +
              $"{selectedBand.MaximumOutsideDiameter.Value} mm";
    }
}

public sealed record ProductionLabourInputs(
    string ProcessName,
    LengthMetres QuoteLength,
    Millimetres FinishedOutsideDiameter,
    LineSpeedMetresPerHour? ManualLineSpeed,
    LabourHours SetupTime,
    OperatorCount Operators,
    HourlyLabourRate HourlyRate,
    ExtrusionLineSpeedProfile? LineSpeedProfile = null);

public sealed record ProductionLabourResult(
    LineSpeedMetresPerHour RecommendedLineSpeed,
    LineSpeedMetresPerHour EffectiveLineSpeed,
    LabourHours RunningTime,
    LabourHours TotalProcessTime,
    LabourHours ChargeableLabourHours,
    decimal LabourCost,
    PricePerMetre LabourCostPerMetre,
    IReadOnlyList<CalculationStep> Steps);

public static class InsulationLineSpeedPolicy
{
    public const string RuleVersion = "insulation-line-speed/v1";

    public static LineSpeedMetresPerHour Select(Millimetres finishedOutsideDiameter)
    {
        var diameter = finishedOutsideDiameter.Value;
        var speed = diameter switch
        {
            <= 1.00m => 15000m,
            <= 1.20m => 13000m,
            <= 2.00m => 8000m,
            <= 2.50m => 6000m,
            _ => 700m,
        };

        return new LineSpeedMetresPerHour(speed);
    }

    public static string BandLabel(Millimetres finishedOutsideDiameter) =>
        finishedOutsideDiameter.Value switch
        {
            <= 1.00m => "Finished OD ≤ 1.00 mm",
            <= 1.20m => "Finished OD ≤ 1.20 mm",
            <= 2.00m => "Finished OD ≤ 2.00 mm",
            <= 2.50m => "Finished OD ≤ 2.50 mm",
            _ => "Finished OD > 2.50 mm",
        };
}

public static class ProductionLabourCalculator
{
    public const string RuleVersion = "production-labour/v1";

    public static ProductionLabourResult Calculate(ProductionLabourInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        if (string.IsNullOrWhiteSpace(inputs.ProcessName))
        {
            throw new ArgumentException(
                "A production process name is required.",
                nameof(inputs));
        }

        var quoteLength = inputs.QuoteLength.Value;
        var finishedDiameter = inputs.FinishedOutsideDiameter.Value;
        var speedRuleVersion =
            inputs.LineSpeedProfile?.RuleVersion ??
            InsulationLineSpeedPolicy.RuleVersion;
        var recommended =
            inputs.LineSpeedProfile?.Select(inputs.FinishedOutsideDiameter) ??
            InsulationLineSpeedPolicy.Select(inputs.FinishedOutsideDiameter);
        var effective = inputs.ManualLineSpeed ?? recommended;
        var setupHours = inputs.SetupTime.Value;
        var operators = inputs.Operators.Value;
        var hourlyRate = inputs.HourlyRate.Value;

        var diameterStep = InputStep(
            "labour-finished-outside-diameter",
            "Finished core outside diameter",
            "The finished diameter used to select the recommended insulation line-speed band.",
            finishedDiameter,
            3,
            "mm",
            speedRuleVersion);
        var recommendedSpeedStep = DerivedStep(
            "recommended-line-speed",
            $"Recommended {inputs.ProcessName} line speed",
            $"{(inputs.LineSpeedProfile?.BandLabel(inputs.FinishedOutsideDiameter) ??
                InsulationLineSpeedPolicy.BandLabel(inputs.FinishedOutsideDiameter))}.",
            "LineSpeedTable(FinishedOutsideDiameter)",
            $"LineSpeedTable({Raw(finishedDiameter)} mm)",
            recommended.Value,
            0,
            "m/h",
            [diameterStep],
            speedRuleVersion);
        var effectiveSpeedStep = inputs.ManualLineSpeed is null
            ? recommendedSpeedStep
            : InputStep(
                "manual-line-speed",
                "Manual line-speed override",
                "The explicit production speed used instead of the recommended OD-based speed.",
                effective.Value,
                0,
                "m/h");
        var quoteLengthStep = InputStep(
            "labour-quote-length",
            "Production length",
            "The cable length to be produced.",
            quoteLength,
            0,
            "m");

        var runningHours = quoteLength / effective.Value;
        var runningTimeStep = DerivedStep(
            "production-running-time",
            $"{inputs.ProcessName} running time",
            "The production length divided by the effective line speed.",
            "QuoteLength ÷ EffectiveLineSpeed",
            $"{Raw(quoteLength)} m ÷ {Raw(effective.Value)} m/h",
            runningHours,
            4,
            "h",
            [quoteLengthStep, effectiveSpeedStep]);
        var setupTimeStep = InputStep(
            "production-setup-time",
            "Set-up time",
            "Additional process preparation time charged once for this production run.",
            setupHours,
            2,
            "h");
        var processHours = runningHours + setupHours;
        var processTimeStep = DerivedStep(
            "total-production-time",
            "Total production time",
            "Running time plus one-off set-up time.",
            "RunningTime + SetupTime",
            $"{Raw(runningHours)} h + {Raw(setupHours)} h",
            processHours,
            4,
            "h",
            [runningTimeStep, setupTimeStep]);
        var operatorCountStep = InputStep(
            "operator-count",
            "Operators",
            "The number of operators charged for the production time.",
            operators,
            2,
            "operators");
        var chargeableHours = processHours * operators;
        var chargeableHoursStep = DerivedStep(
            "chargeable-labour-hours",
            "Chargeable labour hours",
            "Total production time multiplied by the number of operators.",
            "TotalProductionTime × Operators",
            $"{Raw(processHours)} h × {Raw(operators)}",
            chargeableHours,
            4,
            "operator h",
            [processTimeStep, operatorCountStep]);
        var hourlyRateStep = InputStep(
            "hourly-labour-rate",
            "Hourly labour rate",
            "The charge rate for each operator hour.",
            hourlyRate,
            2,
            "£/h");
        var labourCost = chargeableHours * hourlyRate;
        var labourCostStep = DerivedStep(
            "labour-cost-for-quote",
            "Labour cost for quote",
            "Chargeable labour hours multiplied by the hourly labour rate.",
            "ChargeableLabourHours × HourlyRate",
            $"{Raw(chargeableHours)} operator h × {Raw(hourlyRate)} £/h",
            labourCost,
            2,
            "£",
            [chargeableHoursStep, hourlyRateStep]);
        var costPerMetre = labourCost / quoteLength;
        var costPerMetreStep = DerivedStep(
            "labour-cost-per-metre",
            "Labour cost per metre",
            "The total labour cost distributed across the quote length.",
            "LabourCost ÷ QuoteLength",
            $"{Raw(labourCost)} £ ÷ {Raw(quoteLength)} m",
            costPerMetre,
            4,
            "£/m",
            [labourCostStep, quoteLengthStep]);

        return new ProductionLabourResult(
            recommended,
            effective,
            new LabourHours(runningHours),
            new LabourHours(processHours),
            new LabourHours(chargeableHours),
            labourCost,
            new PricePerMetre(costPerMetre),
            [
                diameterStep,
                recommendedSpeedStep,
                .. (inputs.ManualLineSpeed is null
                    ? Array.Empty<CalculationStep>()
                    : new[] { effectiveSpeedStep }),
                quoteLengthStep,
                runningTimeStep,
                setupTimeStep,
                processTimeStep,
                operatorCountStep,
                chargeableHoursStep,
                hourlyRateStep,
                labourCostStep,
                costPerMetreStep,
            ]);
    }

    private static CalculationStep InputStep(
        string id,
        string label,
        string businessMeaning,
        decimal value,
        int decimalPlaces,
        string unit,
        string ruleVersion = RuleVersion) =>
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
            RuleVersion: ruleVersion);

    private static CalculationStep DerivedStep(
        string id,
        string label,
        string businessMeaning,
        string expression,
        string substitutedExpression,
        decimal value,
        int decimalPlaces,
        string unit,
        IReadOnlyList<CalculationStep> inputs,
        string ruleVersion = RuleVersion) =>
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
            RuleVersion: ruleVersion);

    private static string Display(decimal value, int decimalPlaces) =>
        decimal.Round(value, decimalPlaces, MidpointRounding.AwayFromZero)
            .ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);

    private static string DisplayRule(int decimalPlaces) =>
        $"No calculation rounding; display is rounded to {decimalPlaces} decimal places " +
        "using midpoint-away-from-zero.";

    private static string Raw(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
