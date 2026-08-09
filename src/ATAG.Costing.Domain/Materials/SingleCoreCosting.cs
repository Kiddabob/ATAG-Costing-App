using System.Globalization;
using ATAG.Costing.Domain.Calculations;

namespace ATAG.Costing.Domain.Materials;

public readonly record struct YieldMetresPerKilogram
{
    public YieldMetresPerKilogram(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Conductor yield must be greater than zero.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct Millimetres
{
    public Millimetres(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "A diameter or tolerance cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct SpecificGravity
{
    public SpecificGravity(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Specific gravity must be greater than zero.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct MarkupRateFraction
{
    public MarkupRateFraction(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Markup cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct RiskRateFraction
{
    public RiskRateFraction(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Risk cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct SupplierQuoteTotal
{
    public SupplierQuoteTotal(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "A supplier quote total cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public sealed record MaterialSupplierQuote
{
    public MaterialSupplierQuote(
        SupplierQuoteTotal total,
        MassKilograms quotedMass)
    {
        if (quotedMass.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quotedMass),
                "The supplier quoted mass must be greater than zero.");
        }

        Total = total;
        QuotedMass = quotedMass;
    }

    public SupplierQuoteTotal Total { get; }

    public MassKilograms QuotedMass { get; }

    public PricePerKilogram PricePerKilogram =>
        new(Total.Value / QuotedMass.Value);
}

public sealed record SingleCoreCostingInputs(
    string ConductorReference,
    MaterialSupplierQuote ConductorQuote,
    YieldMetresPerKilogram ConductorYield,
    Millimetres ConductorOutsideDiameter,
    string CompoundReference,
    MaterialSupplierQuote CompoundQuote,
    SpecificGravity CompoundSpecificGravity,
    Millimetres NominalFinishedCoreOutsideDiameter,
    Millimetres FinishedCoreOutsideDiameterTolerance,
    string MasterbatchReference,
    MaterialSupplierQuote MasterbatchQuote,
    AdditionRateFraction MasterbatchAdditionRate,
    LengthMetres QuoteLength,
    UsageAllowanceRateFraction UsageAllowanceRate,
    RiskRateFraction RiskRate,
    MarkupRateFraction MarkupRate);

public sealed record MaterialUsageCostResult(
    KilogramsPerMetre BaseKilogramsPerMetre,
    KilogramsPerMetre AdjustedKilogramsPerMetre,
    MassKilograms QuoteMass,
    PricePerMetre PricePerMetre,
    decimal QuotePrice,
    IReadOnlyList<CalculationStep> Steps);

public sealed record CompoundUsageCostResult(
    decimal ConductorAreaSquareMillimetres,
    decimal FinishedCoreAreaSquareMillimetres,
    decimal CompoundAreaSquareMillimetres,
    decimal CompoundGramsPerMetreBeforeAllowance,
    MaterialUsageCostResult Material,
    IReadOnlyList<CalculationStep> Steps);

public sealed record SingleCoreCostingResult(
    MaterialUsageCostResult Conductor,
    CompoundUsageCostResult Compound,
    MasterbatchUsageResult Masterbatch,
    PricePerMetre CoreMaterialPricePerMetre,
    decimal CoreMaterialPriceForQuote,
    PricePerMetre RiskAdjustedPricePerMetre,
    decimal RiskAdjustedPriceForQuote,
    PricePerMetre MarkedUpPricePerMetre,
    decimal MarkedUpPriceForQuote,
    IReadOnlyList<CalculationStep> Steps);

/// <summary>
/// Calculates the material-only cost of one insulated conductor core. It
/// applies the confirmed general waste/start-up usage allowance exactly once
/// to each material stream, then applies risk and markup as separate,
/// sequential visible steps.
/// </summary>
public static class SingleCoreCostingCalculator
{
    public const string RuleVersion = "single-core-material-costing/v1";

    public static SingleCoreCostingResult Calculate(SingleCoreCostingInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        if (string.IsNullOrWhiteSpace(inputs.ConductorReference))
        {
            throw new ArgumentException(
                "A conductor reference is required.",
                nameof(inputs));
        }

        if (string.IsNullOrWhiteSpace(inputs.CompoundReference))
        {
            throw new ArgumentException(
                "A compound reference is required.",
                nameof(inputs));
        }

        if (string.IsNullOrWhiteSpace(inputs.MasterbatchReference))
        {
            throw new ArgumentException(
                "A masterbatch reference is required.",
                nameof(inputs));
        }

        var conductorDiameter = inputs.ConductorOutsideDiameter.Value;
        var nominalCoreDiameter = inputs.NominalFinishedCoreOutsideDiameter.Value;
        var tolerance = inputs.FinishedCoreOutsideDiameterTolerance.Value;
        var maximumCoreDiameter = nominalCoreDiameter + tolerance;

        if (conductorDiameter <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "The conductor outside diameter must be greater than zero.");
        }

        if (maximumCoreDiameter <= conductorDiameter)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "The finished core outside diameter, including tolerance, must exceed the conductor outside diameter.");
        }

        var allowanceRate = inputs.UsageAllowanceRate.Value;
        var quoteLength = inputs.QuoteLength.Value;

        var conductorSupplierQuoteSteps = SupplierQuoteSteps(
            "conductor",
            inputs.ConductorReference,
            inputs.ConductorQuote);
        var compoundSupplierQuoteSteps = SupplierQuoteSteps(
            "compound",
            inputs.CompoundReference,
            inputs.CompoundQuote);
        var masterbatchSupplierQuoteSteps = SupplierQuoteSteps(
            "masterbatch",
            inputs.MasterbatchReference,
            inputs.MasterbatchQuote);

        var allowanceRateStep = InputPercentageStep(
            "usage-allowance-rate",
            "Waste/start-up usage allowance",
            "The general material-usage boost for expected waste and process start-up.",
            allowanceRate,
            UsageAllowanceCalculator.RuleVersion);
        var allowance = UsageAllowanceCalculator.Apply(1m, inputs.UsageAllowanceRate);
        var allowanceMultiplierStep = DerivedStep(
            "usage-allowance-multiplier",
            "Usage allowance multiplier",
            "One plus the general waste/start-up usage allowance rate.",
            "1 + UsageAllowanceRate",
            $"1 + {Raw(allowanceRate)}",
            allowance.Multiplier,
            4,
            "×",
            [allowanceRateStep],
            UsageAllowanceCalculator.RuleVersion);
        var quoteLengthStep = InputStep(
            "quote-length",
            "Quote length",
            "The finished cable length being costed.",
            quoteLength,
            0,
            "m");

        var conductor = CalculateConductor(
            inputs,
            conductorSupplierQuoteSteps,
            allowanceMultiplierStep,
            quoteLengthStep);
        var compound = CalculateCompound(
            inputs,
            compoundSupplierQuoteSteps,
            allowanceMultiplierStep,
            quoteLengthStep,
            maximumCoreDiameter);

        var baseCompoundQuoteMass =
            compound.Material.BaseKilogramsPerMetre.Value * quoteLength;
        var masterbatch = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(baseCompoundQuoteMass),
                inputs.UsageAllowanceRate,
                inputs.MasterbatchAdditionRate,
                inputs.QuoteLength,
                inputs.MasterbatchQuote.PricePerKilogram));

        var corePricePerMetre =
            conductor.PricePerMetre.Value +
            compound.Material.PricePerMetre.Value +
            masterbatch.MasterbatchPricePerMetre.Value;
        var corePricePerMetreStep = DerivedStep(
            "core-material-price-per-metre",
            "Core material price per metre",
            "The conductor, compound, and masterbatch material cost for one metre of one core.",
            "ConductorPricePerMetre + CompoundPricePerMetre + MasterbatchPricePerMetre",
            $"{Raw(conductor.PricePerMetre.Value)} + " +
            $"{Raw(compound.Material.PricePerMetre.Value)} + " +
            $"{Raw(masterbatch.MasterbatchPricePerMetre.Value)}",
            corePricePerMetre,
            4,
            "£/m",
            [
                conductor.Steps[^1],
                compound.Material.Steps[^1],
                masterbatch.Steps[^1],
            ]);

        var quotePrice = corePricePerMetre * quoteLength;
        var quotePriceStep = DerivedStep(
            "core-material-price-for-quote",
            "Core material price for quote",
            "The material cost of one core across the complete quote length before markup.",
            "CoreMaterialPricePerMetre × QuoteLength",
            $"{Raw(corePricePerMetre)} £/m × {Raw(quoteLength)} m",
            quotePrice,
            2,
            "£",
            [corePricePerMetreStep, quoteLengthStep]);

        var riskRate = inputs.RiskRate.Value;
        var riskRateStep = InputPercentageStep(
            "risk-rate",
            "Risk",
            "The commercial risk allowance applied to the calculated material cost before markup.",
            riskRate,
            RuleVersion);
        var riskMultiplier = 1m + riskRate;
        var riskMultiplierStep = DerivedStep(
            "risk-multiplier",
            "Risk multiplier",
            "One plus the risk rate. Risk changes price, not material usage.",
            "1 + RiskRate",
            $"1 + {Raw(riskRate)}",
            riskMultiplier,
            4,
            "×",
            [riskRateStep]);
        var riskAdjustedPricePerMetre = corePricePerMetre * riskMultiplier;
        var riskAdjustedPricePerMetreStep = DerivedStep(
            "risk-adjusted-core-price-per-metre",
            "Core price per metre including risk",
            "The one-core material price per metre after risk and before markup.",
            "CoreMaterialPricePerMetre × RiskMultiplier",
            $"{Raw(corePricePerMetre)} £/m × {Raw(riskMultiplier)}",
            riskAdjustedPricePerMetre,
            4,
            "£/m",
            [corePricePerMetreStep, riskMultiplierStep]);
        var riskAdjustedQuotePrice = quotePrice * riskMultiplier;
        var riskAdjustedQuotePriceStep = DerivedStep(
            "risk-adjusted-core-price-for-quote",
            "Core price for quote including risk",
            "The complete one-core material quote after risk and before markup.",
            "CoreMaterialPriceForQuote × RiskMultiplier",
            $"{Raw(quotePrice)} £ × {Raw(riskMultiplier)}",
            riskAdjustedQuotePrice,
            2,
            "£",
            [quotePriceStep, riskMultiplierStep]);

        var markupRate = inputs.MarkupRate.Value;
        var markupRateStep = InputPercentageStep(
            "markup-rate",
            "Markup",
            "The commercial markup applied after the separate risk step.",
            markupRate,
            RuleVersion);
        var markupMultiplier = 1m + markupRate;
        var markupMultiplierStep = DerivedStep(
            "markup-multiplier",
            "Markup multiplier",
            "One plus the markup rate. Markup is not margin and does not alter material usage.",
            "1 + MarkupRate",
            $"1 + {Raw(markupRate)}",
            markupMultiplier,
            4,
            "×",
            [markupRateStep]);
        var markedUpPricePerMetre =
            riskAdjustedPricePerMetre * markupMultiplier;
        var markedUpPricePerMetreStep = DerivedStep(
            "marked-up-core-price-per-metre",
            "Core price per metre including risk and markup",
            "The risk-adjusted one-core price per metre after commercial markup.",
            "RiskAdjustedPricePerMetre × MarkupMultiplier",
            $"{Raw(riskAdjustedPricePerMetre)} £/m × {Raw(markupMultiplier)}",
            markedUpPricePerMetre,
            4,
            "£/m",
            [riskAdjustedPricePerMetreStep, markupMultiplierStep]);
        var markedUpQuotePrice =
            riskAdjustedQuotePrice * markupMultiplier;
        var markedUpQuotePriceStep = DerivedStep(
            "marked-up-core-price-for-quote",
            "Core price for quote including risk and markup",
            "The complete risk-adjusted one-core quote after commercial markup.",
            "RiskAdjustedPriceForQuote × MarkupMultiplier",
            $"{Raw(riskAdjustedQuotePrice)} £ × {Raw(markupMultiplier)}",
            markedUpQuotePrice,
            2,
            "£",
            [riskAdjustedQuotePriceStep, markupMultiplierStep]);

        var steps = new List<CalculationStep>();
        steps.Add(allowanceRateStep);
        steps.Add(allowanceMultiplierStep);
        steps.Add(quoteLengthStep);
        steps.AddRange(conductor.Steps);
        steps.AddRange(compound.Steps);
        steps.AddRange(masterbatchSupplierQuoteSteps);
        steps.AddRange(masterbatch.Steps);
        steps.Add(corePricePerMetreStep);
        steps.Add(quotePriceStep);
        steps.Add(riskRateStep);
        steps.Add(riskMultiplierStep);
        steps.Add(riskAdjustedPricePerMetreStep);
        steps.Add(riskAdjustedQuotePriceStep);
        steps.Add(markupRateStep);
        steps.Add(markupMultiplierStep);
        steps.Add(markedUpPricePerMetreStep);
        steps.Add(markedUpQuotePriceStep);

        return new SingleCoreCostingResult(
            conductor,
            compound,
            masterbatch,
            new PricePerMetre(corePricePerMetre),
            quotePrice,
            new PricePerMetre(riskAdjustedPricePerMetre),
            riskAdjustedQuotePrice,
            new PricePerMetre(markedUpPricePerMetre),
            markedUpQuotePrice,
            steps);
    }

    private static MaterialUsageCostResult CalculateConductor(
        SingleCoreCostingInputs inputs,
        IReadOnlyList<CalculationStep> supplierQuoteSteps,
        CalculationStep allowanceMultiplierStep,
        CalculationStep quoteLengthStep)
    {
        var pricePerKilogram =
            inputs.ConductorQuote.PricePerKilogram.Value;
        var yield = inputs.ConductorYield.Value;
        var quoteLength = inputs.QuoteLength.Value;
        var allowanceMultiplier = 1m + inputs.UsageAllowanceRate.Value;

        var priceStep = supplierQuoteSteps[^1];
        var yieldStep = InputStep(
            "conductor-yield",
            "Conductor yield",
            "The finished conductor length produced from one kilogram.",
            yield,
            6,
            "m/kg");
        var baseKilogramsPerMetre =
            MaterialCostingFormulas.ConductorKilogramsPerMetre(yield);
        var baseUsageStep = DerivedStep(
            "conductor-base-kilograms-per-metre",
            "Conductor base usage",
            "The conductor mass required for one metre before waste/start-up allowance.",
            "1 ÷ ConductorYield",
            $"1 kg ÷ {Raw(yield)} m",
            baseKilogramsPerMetre,
            9,
            "kg/m",
            [yieldStep]);
        var adjustedKilogramsPerMetre =
            MaterialCostingFormulas.ApplyUsageAllowance(
                baseKilogramsPerMetre,
                inputs.UsageAllowanceRate);
        var adjustedUsageStep = DerivedStep(
            "conductor-adjusted-kilograms-per-metre",
            "Conductor usage with allowance",
            "The conductor mass per metre after the general waste/start-up usage boost.",
            "ConductorBaseKilogramsPerMetre × UsageAllowanceMultiplier",
            $"{Raw(baseKilogramsPerMetre)} kg/m × {Raw(allowanceMultiplier)}",
            adjustedKilogramsPerMetre,
            9,
            "kg/m",
            [baseUsageStep, allowanceMultiplierStep]);
        var quoteMass = MaterialCostingFormulas.MassForLength(
            adjustedKilogramsPerMetre,
            inputs.QuoteLength);
        var quoteMassStep = DerivedStep(
            "conductor-quote-mass",
            "Conductor mass for quote",
            "The adjusted conductor mass required for the quote length.",
            "ConductorKilogramsPerMetre × QuoteLength",
            $"{Raw(adjustedKilogramsPerMetre)} kg/m × {Raw(quoteLength)} m",
            quoteMass,
            6,
            "kg",
            [adjustedUsageStep, quoteLengthStep]);
        var pricePerMetre = MaterialCostingFormulas.PricePerMetre(
            adjustedKilogramsPerMetre,
            inputs.ConductorQuote.PricePerKilogram);
        var pricePerMetreStep = DerivedStep(
            "conductor-price-per-metre",
            "Conductor price per metre",
            "The adjusted conductor usage multiplied by the supplier price per kilogram.",
            "ConductorKilogramsPerMetre × ConductorPricePerKilogram",
            $"{Raw(adjustedKilogramsPerMetre)} kg/m × {Raw(pricePerKilogram)} £/kg",
            pricePerMetre,
            4,
            "£/m",
            [adjustedUsageStep, priceStep]);
        var quotePrice = MaterialCostingFormulas.PriceForLength(
            pricePerMetre,
            inputs.QuoteLength);
        var quotePriceStep = DerivedStep(
            "conductor-price-for-quote",
            "Conductor price for quote",
            "The conductor material price across the quote length.",
            "ConductorPricePerMetre × QuoteLength",
            $"{Raw(pricePerMetre)} £/m × {Raw(quoteLength)} m",
            quotePrice,
            2,
            "£",
            [pricePerMetreStep, quoteLengthStep]);

        return new MaterialUsageCostResult(
            new KilogramsPerMetre(baseKilogramsPerMetre),
            new KilogramsPerMetre(adjustedKilogramsPerMetre),
            new MassKilograms(quoteMass),
            new PricePerMetre(pricePerMetre),
            quotePrice,
            [
                .. supplierQuoteSteps,
                yieldStep,
                baseUsageStep,
                adjustedUsageStep,
                quoteMassStep,
                pricePerMetreStep,
                quotePriceStep,
            ]);
    }

    private static CompoundUsageCostResult CalculateCompound(
        SingleCoreCostingInputs inputs,
        IReadOnlyList<CalculationStep> supplierQuoteSteps,
        CalculationStep allowanceMultiplierStep,
        CalculationStep quoteLengthStep,
        decimal maximumCoreDiameter)
    {
        var conductorDiameter = inputs.ConductorOutsideDiameter.Value;
        var nominalCoreDiameter = inputs.NominalFinishedCoreOutsideDiameter.Value;
        var tolerance = inputs.FinishedCoreOutsideDiameterTolerance.Value;
        var specificGravity = inputs.CompoundSpecificGravity.Value;
        var pricePerKilogram =
            inputs.CompoundQuote.PricePerKilogram.Value;
        var quoteLength = inputs.QuoteLength.Value;
        var allowanceMultiplier = 1m + inputs.UsageAllowanceRate.Value;

        var conductorDiameterStep = InputStep(
            "conductor-outside-diameter",
            "Conductor outside diameter",
            "The nominal outside diameter used as the inner boundary of the insulation annulus.",
            conductorDiameter,
            3,
            "mm");
        var nominalCoreDiameterStep = InputStep(
            "nominal-finished-core-outside-diameter",
            "Nominal finished core outside diameter",
            "The target outside diameter of the insulated core before tolerance.",
            nominalCoreDiameter,
            3,
            "mm");
        var toleranceStep = InputStep(
            "finished-core-outside-diameter-tolerance",
            "Finished core OD tolerance",
            "The positive diameter tolerance added for the material-usage calculation.",
            tolerance,
            3,
            "mm");
        var maximumCoreDiameterStep = DerivedStep(
            "maximum-finished-core-outside-diameter",
            "Maximum finished core outside diameter",
            "The nominal finished core diameter plus its positive tolerance.",
            "NominalFinishedCoreOutsideDiameter + Tolerance",
            $"{Raw(nominalCoreDiameter)} mm + {Raw(tolerance)} mm",
            maximumCoreDiameter,
            3,
            "mm",
            [nominalCoreDiameterStep, toleranceStep]);

        var conductorArea =
            MaterialCostingFormulas.CircularAreaSquareMillimetres(
                conductorDiameter);
        var conductorAreaStep = DerivedStep(
            "conductor-cross-sectional-area",
            "Conductor cross-sectional area",
            "The circular area inside the insulation geometry.",
            "π ÷ 4 × ConductorOutsideDiameter²",
            $"π ÷ 4 × {Raw(conductorDiameter)}²",
            conductorArea,
            6,
            "mm²",
            [conductorDiameterStep]);
        var coreArea =
            MaterialCostingFormulas.CircularAreaSquareMillimetres(
                maximumCoreDiameter);
        var coreAreaStep = DerivedStep(
            "finished-core-cross-sectional-area",
            "Finished core cross-sectional area",
            "The circular area at the maximum finished core outside diameter.",
            "π ÷ 4 × MaximumFinishedCoreOutsideDiameter²",
            $"π ÷ 4 × {Raw(maximumCoreDiameter)}²",
            coreArea,
            6,
            "mm²",
            [maximumCoreDiameterStep]);
        var compoundArea =
            MaterialCostingFormulas.AnnularAreaSquareMillimetres(
                conductorDiameter,
                maximumCoreDiameter);
        var compoundAreaStep = DerivedStep(
            "compound-cross-sectional-area",
            "Compound cross-sectional area",
            "The insulation annulus area after subtracting conductor area from finished core area.",
            "FinishedCoreArea - ConductorArea",
            $"{Raw(coreArea)} mm² - {Raw(conductorArea)} mm²",
            compoundArea,
            6,
            "mm²",
            [coreAreaStep, conductorAreaStep]);

        var specificGravityStep = InputStep(
            "compound-specific-gravity",
            "Compound specific gravity",
            $"The density factor for {inputs.CompoundReference}.",
            specificGravity,
            4,
            "g/cm³");
        var gramsPerMetre =
            MaterialCostingFormulas.CompoundKilogramsPerMetre(
                compoundArea,
                specificGravity) *
            1000m;
        var gramsPerMetreStep = DerivedStep(
            "compound-grams-per-metre-before-allowance",
            "Compound base usage",
            "The insulation mass per metre before waste/start-up allowance.",
            "CompoundArea × SpecificGravity",
            $"{Raw(compoundArea)} mm² × {Raw(specificGravity)} g/cm³",
            gramsPerMetre,
            6,
            "g/m",
            [compoundAreaStep, specificGravityStep]);
        var baseKilogramsPerMetre =
            MaterialCostingFormulas.CompoundKilogramsPerMetre(
                compoundArea,
                specificGravity);
        var baseKilogramsPerMetreStep = DerivedStep(
            "compound-kilograms-per-metre-before-allowance",
            "Compound base usage",
            "The unadjusted compound usage converted from grams to kilograms per metre.",
            "CompoundGramsPerMetre ÷ 1000",
            $"{Raw(gramsPerMetre)} g/m ÷ 1000 g/kg",
            baseKilogramsPerMetre,
            9,
            "kg/m",
            [gramsPerMetreStep]);
        var adjustedKilogramsPerMetre =
            MaterialCostingFormulas.ApplyUsageAllowance(
                baseKilogramsPerMetre,
                inputs.UsageAllowanceRate);
        var adjustedKilogramsPerMetreStep = DerivedStep(
            "compound-kilograms-per-metre-with-allowance",
            "Compound usage with allowance",
            "The compound usage after the general waste/start-up usage boost.",
            "CompoundBaseKilogramsPerMetre × UsageAllowanceMultiplier",
            $"{Raw(baseKilogramsPerMetre)} kg/m × {Raw(allowanceMultiplier)}",
            adjustedKilogramsPerMetre,
            9,
            "kg/m",
            [baseKilogramsPerMetreStep, allowanceMultiplierStep]);
        var quoteMass = MaterialCostingFormulas.MassForLength(
            adjustedKilogramsPerMetre,
            inputs.QuoteLength);
        var quoteMassStep = DerivedStep(
            "compound-quote-mass",
            "Compound mass for quote",
            "The adjusted insulation compound mass required for the quote length.",
            "CompoundKilogramsPerMetre × QuoteLength",
            $"{Raw(adjustedKilogramsPerMetre)} kg/m × {Raw(quoteLength)} m",
            quoteMass,
            6,
            "kg",
            [adjustedKilogramsPerMetreStep, quoteLengthStep]);
        var priceStep = supplierQuoteSteps[^1];
        var pricePerMetre = MaterialCostingFormulas.PricePerMetre(
            adjustedKilogramsPerMetre,
            inputs.CompoundQuote.PricePerKilogram);
        var pricePerMetreStep = DerivedStep(
            "compound-price-per-metre",
            "Compound price per metre",
            "The adjusted insulation usage multiplied by its supplier price.",
            "CompoundKilogramsPerMetre × CompoundPricePerKilogram",
            $"{Raw(adjustedKilogramsPerMetre)} kg/m × {Raw(pricePerKilogram)} £/kg",
            pricePerMetre,
            4,
            "£/m",
            [adjustedKilogramsPerMetreStep, priceStep]);
        var quotePrice = MaterialCostingFormulas.PriceForLength(
            pricePerMetre,
            inputs.QuoteLength);
        var quotePriceStep = DerivedStep(
            "compound-price-for-quote",
            "Compound price for quote",
            "The adjusted compound material price across the quote length.",
            "CompoundPricePerMetre × QuoteLength",
            $"{Raw(pricePerMetre)} £/m × {Raw(quoteLength)} m",
            quotePrice,
            2,
            "£",
            [pricePerMetreStep, quoteLengthStep]);

        var material = new MaterialUsageCostResult(
            new KilogramsPerMetre(baseKilogramsPerMetre),
            new KilogramsPerMetre(adjustedKilogramsPerMetre),
            new MassKilograms(quoteMass),
            new PricePerMetre(pricePerMetre),
            quotePrice,
            [
                gramsPerMetreStep,
                baseKilogramsPerMetreStep,
                adjustedKilogramsPerMetreStep,
                quoteMassStep,
                .. supplierQuoteSteps,
                pricePerMetreStep,
                quotePriceStep,
            ]);

        return new CompoundUsageCostResult(
            conductorArea,
            coreArea,
            compoundArea,
            gramsPerMetre,
            material,
            [
                conductorDiameterStep,
                nominalCoreDiameterStep,
                toleranceStep,
                maximumCoreDiameterStep,
                conductorAreaStep,
                coreAreaStep,
                compoundAreaStep,
                specificGravityStep,
                .. material.Steps,
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

    private static IReadOnlyList<CalculationStep> SupplierQuoteSteps(
        string idPrefix,
        string materialReference,
        MaterialSupplierQuote quote)
    {
        var totalStep = InputStep(
            $"{idPrefix}-supplier-quote-total",
            $"{Title(idPrefix)} supplier quote total",
            $"The total price quoted by the supplier for {materialReference}.",
            quote.Total.Value,
            2,
            "£");
        var massStep = InputStep(
            $"{idPrefix}-supplier-quoted-mass",
            $"{Title(idPrefix)} supplier quoted mass",
            "The material mass covered by the supplier's total quote.",
            quote.QuotedMass.Value,
            3,
            "kg");
        var pricePerKilogram = quote.PricePerKilogram.Value;
        var unitPriceStep = DerivedStep(
            $"{idPrefix}-price-per-kilogram",
            $"{Title(idPrefix)} calculated price per kilogram",
            "The supplier quote total divided by the quoted material mass.",
            "SupplierQuoteTotal ÷ SupplierQuotedMass",
            $"{Raw(quote.Total.Value)} £ ÷ {Raw(quote.QuotedMass.Value)} kg",
            pricePerKilogram,
            5,
            "£/kg",
            [totalStep, massStep]);

        return [totalStep, massStep, unitPriceStep];
    }

    private static CalculationStep InputPercentageStep(
        string id,
        string label,
        string businessMeaning,
        decimal value,
        string ruleVersion) =>
        new(
            id,
            label,
            "Input",
            $"{Raw(value)} fraction ({Raw(value * 100m)}%)",
            value,
            Display(value * 100m, 2),
            "%",
            BusinessMeaning: businessMeaning,
            RoundingRule:
                "No calculation rounding; percentage display is rounded to 2 decimal places.",
            RuleVersion: ruleVersion);

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

    private static string Title(string value) =>
        char.ToUpperInvariant(value[0]) + value[1..];
}
