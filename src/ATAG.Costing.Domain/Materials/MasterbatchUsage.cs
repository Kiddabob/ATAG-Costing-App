using System.Globalization;
using ATAG.Costing.Domain.Calculations;

namespace ATAG.Costing.Domain.Materials;

public readonly record struct MassKilograms
{
    public MassKilograms(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Mass cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct LengthMetres
{
    public LengthMetres(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Length must be greater than zero.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct AdditionRateFraction
{
    public AdditionRateFraction(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "The addition-rate fraction cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct PricePerKilogram
{
    public PricePerKilogram(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Price cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct KilogramsPerMetre(decimal Value);

public readonly record struct GramsPerMetre(decimal Value);

public readonly record struct PricePerMetre(decimal Value);

public sealed record MasterbatchUsageInputs(
    MassKilograms BaseCompoundMassBeforeAllowanceForQuote,
    UsageAllowanceRateFraction UsageAllowanceRate,
    AdditionRateFraction AdditionRate,
    LengthMetres QuoteLength,
    PricePerKilogram MasterbatchPrice);

public sealed record MasterbatchUsageResult(
    MassKilograms MasterbatchMassForQuote,
    KilogramsPerMetre MasterbatchKilogramsPerMetre,
    GramsPerMetre MasterbatchGramsPerMetre,
    PricePerMetre MasterbatchPricePerMetre,
    IReadOnlyList<CalculationStep> Steps);

/// <summary>
/// Calculates masterbatch usage from an already-approved base-compound mass.
/// Workbook cell references belong in migration metadata and parity fixtures,
/// not in this reusable business rule.
/// </summary>
public static class MasterbatchUsageCalculator
{
    public const string RuleVersion = "masterbatch-usage-per-metre/v1";

    public static MasterbatchUsageResult Calculate(MasterbatchUsageInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var baseMassBeforeAllowance = inputs.BaseCompoundMassBeforeAllowanceForQuote.Value;
        var usageAllowanceRate = inputs.UsageAllowanceRate.Value;
        var additionRate = inputs.AdditionRate.Value;
        var quoteLength = inputs.QuoteLength.Value;
        var pricePerKilogram = inputs.MasterbatchPrice.Value;

        var baseMassStep = InputStep(
            "base-compound-mass-before-allowance",
            "Base compound mass before allowance",
            "The calculated compound mass for the quote before waste and start-up usage are added.",
            baseMassBeforeAllowance,
            6,
            "kg");
        var usageAllowanceRateStep = new CalculationStep(
            "usage-allowance-rate",
            "Waste/start-up usage allowance",
            "Input",
            $"{Raw(usageAllowanceRate)} fraction ({Raw(usageAllowanceRate * 100m)}%)",
            usageAllowanceRate,
            Display(usageAllowanceRate * 100m, 2),
            "%",
            BusinessMeaning:
                "The general usage boost that covers expected material waste and process start-up.",
            RoundingRule: "No calculation rounding; percentage display is rounded to 2 decimal places.",
            RuleVersion: UsageAllowanceCalculator.RuleVersion);
        var allowance = UsageAllowanceCalculator.Apply(
            baseMassBeforeAllowance,
            inputs.UsageAllowanceRate);
        var usageMultiplierStep = DerivedStep(
            "usage-allowance-multiplier",
            "Usage allowance multiplier",
            "The multiplier formed from one plus the waste/start-up allowance rate.",
            "1 + UsageAllowanceRate",
            $"1 + {Raw(usageAllowanceRate)}",
            allowance.Multiplier,
            4,
            "×",
            [usageAllowanceRateStep],
            UsageAllowanceCalculator.RuleVersion);
        var adjustedBaseMassStep = DerivedStep(
            "base-compound-mass-with-allowance",
            "Base compound mass with allowance",
            "The quote compound mass after the waste/start-up usage boost.",
            "BaseCompoundMassBeforeAllowance × UsageAllowanceMultiplier",
            $"{Raw(baseMassBeforeAllowance)} kg × {Raw(allowance.Multiplier)}",
            allowance.AdjustedUsage,
            6,
            "kg",
            [baseMassStep, usageMultiplierStep],
            UsageAllowanceCalculator.RuleVersion);
        var additionRateStep = new CalculationStep(
            "masterbatch-addition-rate",
            "Masterbatch addition rate",
            "Input",
            $"{Raw(additionRate)} fraction ({Raw(additionRate * 100m)}%)",
            additionRate,
            Display(additionRate * 100m, 2),
            "%",
            Warning: additionRate > 1m
                ? "The addition rate exceeds 100%; confirm that a percentage was not entered as a whole number."
                : null,
            BusinessMeaning: "The masterbatch proportion applied to the base compound mass.",
            RoundingRule: "No calculation rounding; percentage display is rounded to 2 decimal places.",
            RuleVersion: RuleVersion);
        var quoteLengthStep = InputStep(
            "quote-length",
            "Cable quote length",
            "The cable length over which the quote mass is distributed.",
            quoteLength,
            0,
            "m");
        var priceStep = InputStep(
            "masterbatch-price-per-kilogram",
            "Masterbatch supplier price",
            "The approved supplier price for one kilogram of masterbatch.",
            pricePerKilogram,
            2,
            "£/kg");

        var quoteMass = allowance.AdjustedUsage * additionRate;
        var quoteMassStep = DerivedStep(
            "masterbatch-mass-for-quote",
            "Masterbatch mass for quote",
            "The masterbatch mass required for the complete quote length.",
            "BaseCompoundMassWithAllowance × AdditionRate",
            $"{Raw(allowance.AdjustedUsage)} kg × {Raw(additionRate)}",
            quoteMass,
            6,
            "kg",
            [adjustedBaseMassStep, additionRateStep]);

        var kilogramsPerMetre = quoteMass / quoteLength;
        var kilogramsPerMetreStep = DerivedStep(
            "masterbatch-kilograms-per-metre",
            "Masterbatch mass per metre",
            "The masterbatch mass allocated to one metre of cable.",
            "MasterbatchMassForQuote ÷ QuoteLength",
            $"{Raw(quoteMass)} kg ÷ {Raw(quoteLength)} m",
            kilogramsPerMetre,
            9,
            "kg/m",
            [quoteMassStep, quoteLengthStep]);

        var gramsPerMetre = kilogramsPerMetre * 1000m;
        var gramsPerMetreStep = DerivedStep(
            "masterbatch-grams-per-metre",
            "Masterbatch mass per metre",
            "The per-metre masterbatch mass expressed in grams.",
            "MasterbatchKilogramsPerMetre × 1000",
            $"{Raw(kilogramsPerMetre)} kg/m × 1000 g/kg",
            gramsPerMetre,
            6,
            "g/m",
            [kilogramsPerMetreStep]);

        var pricePerMetre = kilogramsPerMetre * pricePerKilogram;
        var pricePerMetreStep = DerivedStep(
            "masterbatch-price-per-metre",
            "Masterbatch price per metre",
            "The raw masterbatch material price allocated to one metre of cable.",
            "MasterbatchKilogramsPerMetre × MasterbatchPricePerKilogram",
            $"{Raw(kilogramsPerMetre)} kg/m × {Raw(pricePerKilogram)} £/kg",
            pricePerMetre,
            2,
            "£/m",
            [kilogramsPerMetreStep, priceStep]);

        return new MasterbatchUsageResult(
            new MassKilograms(quoteMass),
            new KilogramsPerMetre(kilogramsPerMetre),
            new GramsPerMetre(gramsPerMetre),
            new PricePerMetre(pricePerMetre),
            [
                baseMassStep,
                usageAllowanceRateStep,
                usageMultiplierStep,
                adjustedBaseMassStep,
                additionRateStep,
                quoteLengthStep,
                priceStep,
                quoteMassStep,
                kilogramsPerMetreStep,
                gramsPerMetreStep,
                pricePerMetreStep,
            ]);
    }

    private static CalculationStep InputStep(
        string id,
        string label,
        string businessMeaning,
        decimal value,
        int displayDecimalPlaces,
        string unit) =>
        new(
            id,
            label,
            "Input",
            $"{Raw(value)} {unit}",
            value,
            Display(value, displayDecimalPlaces),
            unit,
            BusinessMeaning: businessMeaning,
            RoundingRule: DisplayRule(displayDecimalPlaces),
            RuleVersion: RuleVersion);

    private static CalculationStep DerivedStep(
        string id,
        string label,
        string businessMeaning,
        string expression,
        string substitutedExpression,
        decimal value,
        int displayDecimalPlaces,
        string unit,
        IReadOnlyList<CalculationStep> inputs,
        string ruleVersion = RuleVersion) =>
        new(
            id,
            label,
            expression,
            $"{substitutedExpression} = {Raw(value)} {unit}",
            value,
            Display(value, displayDecimalPlaces),
            unit,
            inputs,
            BusinessMeaning: businessMeaning,
            RoundingRule: DisplayRule(displayDecimalPlaces),
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
