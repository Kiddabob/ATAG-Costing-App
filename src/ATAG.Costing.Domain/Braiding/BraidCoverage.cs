using System.Globalization;
using ATAG.Costing.Domain.Calculations;

namespace ATAG.Costing.Domain.Braiding;

public sealed record BraidCoreLayout(
    int CoreCount,
    string Layup,
    double OutsideDiameterMultiplier)
{
    public string Display => string.IsNullOrWhiteSpace(Layup)
        ? $"{CoreCount} core{(CoreCount == 1 ? string.Empty : "s")} · ×{OutsideDiameterMultiplier:0.###} OD"
        : $"{CoreCount} cores · {Layup} lay-up · ×{OutsideDiameterMultiplier:0.###} OD";
}

public sealed record BuncherLaySetting(
    double LayLengthMillimetres,
    int GearA,
    int GearB,
    string BuncherSize)
{
    public string Display =>
        $"{LayLengthMillimetres:0.##} mm · {BuncherSize} buncher · gears {GearA} & {GearB}";
}

public sealed record BraidCoverageInputs(
    double TargetCoverageFraction,
    double CoreOutsideDiameterMillimetres,
    int CoreCount,
    int EndsPerCarrier,
    double EffectiveWireDiameterMillimetres,
    double CableLengthMetres);

public sealed record BraidCarrierResult(
    int CarrierCount,
    int TotalBraidStrands,
    double BaseFillFraction,
    double RecommendedPitchMillimetres,
    double LongitudinalAngleDegrees,
    double PerpendicularAngleDegrees,
    double CoverageAtReferencePitchFraction,
    double StrandLengthPerBobbinMetres);

public sealed record BraidCoverageResult(
    BraidCoreLayout CoreLayout,
    double MeanOutsideDiameterMillimetres,
    double TargetFillFraction,
    BraidCarrierResult SixteenCarrier,
    BraidCarrierResult TwentyFourCarrier,
    IReadOnlyList<CalculationStep> Steps);

/// <summary>
/// Reference tables transcribed from the workbook's Braid Coverage Calculator.
/// They are kept in the domain layer so every interface uses one versioned source.
/// </summary>
public static class BraidReferenceTables
{
    public static IReadOnlyList<int> EndsPerCarrierOptions { get; } =
        Enumerable.Range(1, 10).ToArray();

    public static IReadOnlyList<double> EffectiveWireDiameterOptionsMillimetres { get; } =
        [0.1d, 0.2d];

    public static IReadOnlyList<BraidCoreLayout> CoreLayouts { get; } =
    [
        new(1, "", 1.000),
        new(2, "", 2.000),
        new(3, "", 2.155),
        new(4, "", 2.414),
        new(5, "1-4", 2.701),
        new(6, "1-5", 3.000),
        new(7, "1-6", 3.000),
        new(8, "1-7", 3.350),
        new(9, "1-8", 3.660),
        new(10, "2-8", 4.000),
        new(11, "3-8", 4.155),
        new(12, "3-9", 4.155),
        new(13, "3-10", 4.300),
        new(14, "4-10", 4.414),
        new(15, "5-10", 4.700),
        new(16, "5-11", 4.700),
        new(17, "5-12", 5.000),
        new(18, "6-12", 5.000),
        new(19, "1-6-12", 5.000),
        new(20, "1-6-13", 5.350),
        new(21, "1-7-13", 5.350),
        new(22, "2-7-13", 5.700),
        new(23, "2-8-13", 6.000),
        new(24, "2-8-14", 6.000),
        new(25, "2-8-15", 6.000),
        new(26, "3-9-15", 6.155),
        new(27, "3-9-15", 6.155),
        new(28, "3-9-16", 6.250),
        new(29, "4-10-15", 6.414),
        new(30, "4-10-16", 6.414),
        new(31, "4-10-17", 6.600),
        new(32, "5-11-16", 6.700),
        new(33, "5-11-17", 6.700),
        new(34, "5-11-18", 7.000),
        new(35, "5-12-18", 7.000),
        new(36, "6-12-18", 7.000),
        new(37, "1-6-12-18", 7.000),
        new(38, "1-6-12-19", 7.350),
        new(39, "1-6-13-19", 7.350),
        new(40, "1-7-13-19", 7.350),
        new(41, "1-7-13-20", 7.700),
        new(42, "2-8-13-19", 8.000),
        new(43, "2-8-14-19", 8.000),
        new(44, "2-8-14-20", 8.000),
        new(45, "2-8-14-21", 8.000),
    ];

    public static IReadOnlyList<BuncherLaySetting> BuncherLaySettings { get; } =
    [
        new(120.00, 57, 20, "Large"),
        new(90.00, 58, 22, "Large"),
        new(70.00, 50, 25, "Large"),
        new(45.00, 41, 36, "Large"),
        new(96.43, 66, 19, "Small"),
        new(56.51, 57, 28, "Small"),
        new(50.89, 55, 30, "Small"),
        new(45.98, 53, 32, "Small"),
        new(39.66, 50, 35, "Small"),
        new(34.33, 47, 38, "Small"),
        new(31.23, 45, 40, "Small"),
        new(24.68, 40, 45, "Small"),
        new(22.44, 38, 47, "Small"),
        new(19.43, 35, 50, "Small"),
        new(16.76, 32, 53, "Small"),
        new(15.14, 30, 55, "Small"),
        new(13.64, 28, 57, "Small"),
        new(7.99, 19, 66, "Small"),
    ];

    public static BraidCoreLayout CoreLayoutFor(int coreCount) =>
        CoreLayouts.FirstOrDefault(layout => layout.CoreCount == coreCount)
        ?? throw new ArgumentOutOfRangeException(
            nameof(coreCount),
            "Choose a core count between 1 and 45.");
}

/// <summary>
/// Reproduces the formulas in the workbook's Braid Coverage Calculator sheet.
/// </summary>
public static class BraidCoverageCalculator
{
    public const string RuleVersion = "braid-coverage-workbook/v1";
    public const double CoverageReferencePitchMillimetres = 55d;

    public static BraidCoverageResult Calculate(BraidCoverageInputs inputs)
    {
        Validate(inputs);

        var layout = BraidReferenceTables.CoreLayoutFor(inputs.CoreCount);
        var meanOutsideDiameter =
            inputs.CoreOutsideDiameterMillimetres * layout.OutsideDiameterMultiplier;
        var targetFill = 1d - Math.Sqrt(1d - inputs.TargetCoverageFraction);
        var sixteen = CalculateCarrier(
            16,
            inputs,
            meanOutsideDiameter,
            targetFill,
            minimumPitch: 3d,
            maximumPitch: 90d);
        var twentyFour = CalculateCarrier(
            24,
            inputs,
            meanOutsideDiameter,
            targetFill,
            minimumPitch: 6d,
            maximumPitch: 120d);

        var steps = BuildSteps(
            inputs,
            layout,
            meanOutsideDiameter,
            targetFill,
            sixteen,
            twentyFour);
        return new(
            layout,
            meanOutsideDiameter,
            targetFill,
            sixteen,
            twentyFour,
            steps);
    }

    private static BraidCarrierResult CalculateCarrier(
        int carrierCount,
        BraidCoverageInputs inputs,
        double meanOutsideDiameter,
        double targetFill,
        double minimumPitch,
        double maximumPitch)
    {
        var totalBraidStrands = carrierCount * inputs.EndsPerCarrier;
        var baseFill =
            totalBraidStrands * inputs.EffectiveWireDiameterMillimetres /
            (2d * Math.PI * meanOutsideDiameter);
        var requiredPitch = targetFill <= baseFill
            ? maximumPitch
            : Math.PI * meanOutsideDiameter /
              Math.Sqrt(Math.Pow(targetFill / baseFill, 2d) - 1d);
        var recommendedPitch = Math.Clamp(
            requiredPitch,
            minimumPitch,
            maximumPitch);
        var longitudinalAngle = RadiansToDegrees(
            Math.Atan(Math.PI * meanOutsideDiameter / recommendedPitch));

        // This deliberately preserves the workbook formula, including its use
        // of the longitudinal-angle result as the denominator.
        var perpendicularAngle = 90d - RadiansToDegrees(
            Math.Atan(Math.PI * meanOutsideDiameter / longitudinalAngle));
        var referenceFill = baseFill * Math.Sqrt(
            1d + Math.Pow(
                Math.PI * meanOutsideDiameter / CoverageReferencePitchMillimetres,
                2d));
        var coverageAtReferencePitch = referenceFill * (2d - referenceFill);
        var strandLength = inputs.CableLengthMetres * Math.Sqrt(
            1d + Math.Pow(
                Math.PI * meanOutsideDiameter / recommendedPitch,
                2d));

        return new(
            carrierCount,
            totalBraidStrands,
            baseFill,
            recommendedPitch,
            longitudinalAngle,
            perpendicularAngle,
            coverageAtReferencePitch,
            strandLength);
    }

    private static IReadOnlyList<CalculationStep> BuildSteps(
        BraidCoverageInputs inputs,
        BraidCoreLayout layout,
        double meanOutsideDiameter,
        double targetFill,
        params BraidCarrierResult[] carriers)
    {
        var steps = new List<CalculationStep>
        {
            Step(
                "mean-od",
                "Mean cable OD",
                "Core OD multiplied by the selected core-layout factor.",
                "Core OD × OD multiplier",
                $"{Raw(inputs.CoreOutsideDiameterMillimetres)} × {Raw(layout.OutsideDiameterMultiplier)}",
                meanOutsideDiameter,
                "mm",
                "Shown to 3 decimal places."),
            Step(
                "target-fill",
                "Target fill",
                "The fill fraction needed to achieve the requested coverage.",
                "1 − √(1 − target coverage)",
                $"1 − √(1 − {Raw(inputs.TargetCoverageFraction)})",
                targetFill,
                "%",
                "Shown to 2 decimal places as a percentage.",
                percentage: true),
        };

        foreach (var carrier in carriers)
        {
            var prefix = carrier.CarrierCount.ToString(CultureInfo.InvariantCulture);
            steps.Add(Step(
                $"{prefix}-total-strands",
                $"{prefix}-carrier total strands",
                "Carrier count multiplied by the chosen ends per carrier.",
                "Carriers × ends per carrier",
                $"{carrier.CarrierCount} × {inputs.EndsPerCarrier}",
                carrier.TotalBraidStrands,
                "strands",
                "Whole strands."));
            steps.Add(Step(
                $"{prefix}-base-fill",
                $"{prefix}-carrier base fill",
                "The braid fill before the pitch-angle adjustment.",
                "(Total strands × effective wire diameter) ÷ (2π × mean OD)",
                $"({carrier.TotalBraidStrands} × {Raw(inputs.EffectiveWireDiameterMillimetres)}) ÷ (2π × {Raw(meanOutsideDiameter)})",
                carrier.BaseFillFraction,
                "%",
                "Shown to 2 decimal places as a percentage.",
                percentage: true));
            steps.Add(Step(
                $"{prefix}-pitch",
                $"Best {prefix}-carrier pitch",
                "Pitch required for the target fill, limited to the workbook's carrier range.",
                "Clamp(π × mean OD ÷ √((target fill ÷ base fill)² − 1))",
                $"Clamp(π × {Raw(meanOutsideDiameter)} ÷ √(({Raw(targetFill)} ÷ {Raw(carrier.BaseFillFraction)})² − 1))",
                carrier.RecommendedPitchMillimetres,
                "mm",
                "Shown to 2 decimal places."));
            steps.Add(Step(
                $"{prefix}-longitudinal-angle",
                $"Best {prefix}-carrier longitudinal angle",
                "The strand angle along the cable at the recommended pitch.",
                "degrees(atan(π × mean OD ÷ pitch))",
                $"degrees(atan(π × {Raw(meanOutsideDiameter)} ÷ {Raw(carrier.RecommendedPitchMillimetres)}))",
                carrier.LongitudinalAngleDegrees,
                "°",
                "Shown to 2 decimal places."));
            steps.Add(Step(
                $"{prefix}-perpendicular-angle",
                $"Workbook {prefix}-carrier perpendicular angle",
                "The perpendicular-angle result using the workbook's existing formula.",
                "90 − degrees(atan(π × mean OD ÷ longitudinal angle))",
                $"90 − degrees(atan(π × {Raw(meanOutsideDiameter)} ÷ {Raw(carrier.LongitudinalAngleDegrees)}))",
                carrier.PerpendicularAngleDegrees,
                "°",
                "Shown to 2 decimal places."));
            steps.Add(Step(
                $"{prefix}-reference-coverage",
                $"{prefix}-carrier coverage at 55 mm pitch",
                "Coverage at the workbook's fixed 55 mm comparison pitch.",
                "fill × (2 − fill), where fill = base fill × √(1 + (π × mean OD ÷ 55)²)",
                $"fill × (2 − fill), fill = {Raw(carrier.BaseFillFraction)} × √(1 + (π × {Raw(meanOutsideDiameter)} ÷ 55)²)",
                carrier.CoverageAtReferencePitchFraction,
                "%",
                "Shown to 2 decimal places as a percentage.",
                percentage: true));
            steps.Add(Step(
                $"{prefix}-strand-length",
                $"{prefix}-carrier length per strand/bobbin",
                "The braid-wire length needed for the entered cable length at the recommended pitch.",
                "Cable length × √(1 + (π × mean OD ÷ pitch)²)",
                $"{Raw(inputs.CableLengthMetres)} × √(1 + (π × {Raw(meanOutsideDiameter)} ÷ {Raw(carrier.RecommendedPitchMillimetres)})²)",
                carrier.StrandLengthPerBobbinMetres,
                "m",
                "Shown to 3 decimal places."));
        }

        return steps;
    }

    private static CalculationStep Step(
        string id,
        string label,
        string meaning,
        string expression,
        string substituted,
        double value,
        string unit,
        string rounding,
        bool percentage = false)
    {
        var rawValue = Convert.ToDecimal(value);
        var displayValue = percentage
            ? (value * 100d).ToString("0.00", CultureInfo.InvariantCulture)
            : unit == "strands"
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : unit == "m"
                    ? value.ToString("0.000", CultureInfo.InvariantCulture)
                    : value.ToString("0.00#", CultureInfo.InvariantCulture);
        return new(
            id,
            label,
            expression,
            substituted,
            rawValue,
            displayValue,
            unit,
            BusinessMeaning: meaning,
            RoundingRule: rounding,
            RuleVersion: RuleVersion);
    }

    private static void Validate(BraidCoverageInputs inputs)
    {
        if (!double.IsFinite(inputs.TargetCoverageFraction) ||
            inputs.TargetCoverageFraction <= 0d ||
            inputs.TargetCoverageFraction >= 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Target coverage must be greater than 0% and less than 100%.");
        }

        if (!double.IsFinite(inputs.CoreOutsideDiameterMillimetres) ||
            inputs.CoreOutsideDiameterMillimetres <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Core OD must be greater than zero.");
        }

        _ = BraidReferenceTables.CoreLayoutFor(inputs.CoreCount);
        if (!BraidReferenceTables.EndsPerCarrierOptions.Contains(inputs.EndsPerCarrier))
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Choose between 1 and 10 ends per carrier.");
        }

        if (!BraidReferenceTables.EffectiveWireDiameterOptionsMillimetres.Contains(
                inputs.EffectiveWireDiameterMillimetres))
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Choose an effective wire diameter of 0.1 mm or 0.2 mm.");
        }

        if (!double.IsFinite(inputs.CableLengthMetres) || inputs.CableLengthMetres <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Cable length must be greater than zero.");
        }
    }

    private static double RadiansToDegrees(double radians) =>
        radians * 180d / Math.PI;

    private static string Raw(double value) =>
        value.ToString("0.############", CultureInfo.InvariantCulture);
}
