using System.Globalization;
using ATAG.Costing.Domain.Calculations;

namespace ATAG.Costing.Domain.Materials;

public readonly record struct AdditionalProductionLengthMetres
{
    public AdditionalProductionLengthMetres(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Additional production length cannot be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public sealed record DualInsulationLayerInputs(
    string CompoundReference,
    MaterialSupplierQuote CompoundQuote,
    SpecificGravity CompoundSpecificGravity,
    Millimetres NominalFinishedOutsideDiameter,
    Millimetres PositiveOutsideDiameterTolerance,
    string MasterbatchReference,
    MaterialSupplierQuote MasterbatchQuote,
    AdditionRateFraction MasterbatchAdditionRate);

public sealed record DualInsulationCostingInputs(
    string ConductorReference,
    MaterialSupplierQuote ConductorQuote,
    YieldMetresPerKilogram ConductorYield,
    Millimetres ConductorOutsideDiameter,
    DualInsulationLayerInputs FirstLayer,
    DualInsulationLayerInputs SecondLayer,
    LengthMetres FinishedQuoteLength,
    AdditionalProductionLengthMetres CoreStartupLength,
    UsageAllowanceRateFraction UsageAllowanceRate);

public sealed record DualInsulationCostingResult(
    LengthMetres CoreProductionLength,
    LengthMetres SecondLayerProductionLength,
    MaterialUsageCostResult Conductor,
    CompoundUsageCostResult FirstLayerCompound,
    MasterbatchUsageResult FirstLayerMasterbatch,
    CompoundUsageCostResult SecondLayerCompound,
    MasterbatchUsageResult SecondLayerMasterbatch,
    PricePerMetre CoreAndFirstLayerPricePerProductionMetre,
    PricePerMetre SecondLayerPricePerFinishedMetre,
    decimal MaterialPriceForProductionRun,
    PricePerMetre MaterialPricePerFinishedMetre,
    IReadOnlyList<CalculationStep> Steps);

/// <summary>
/// Calculates one conductor with two successive annular insulation layers.
/// The conductor and first layer cover the finished quote length plus the
/// one-off core start-up length. The second layer covers finished quote length
/// only. The confirmed general waste/start-up allowance is then applied once
/// to every material stream. Labour and commercial pricing continue to use
/// their existing shared calculators over this result.
/// </summary>
public static class DualInsulationCostingCalculator
{
    public const string RuleVersion = "dual-insulation-material-costing/v1";

    public static DualInsulationCostingResult Calculate(
        DualInsulationCostingInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ValidateReferences(inputs);

        var finishedLengthStep = InputStep(
            "dual-finished-quote-length",
            "Finished quote length",
            "The finished cable length supplied to the customer.",
            inputs.FinishedQuoteLength.Value,
            0,
            "m");
        var coreStartupLengthStep = InputStep(
            "dual-core-startup-length",
            "Additional core start-up length",
            "Extra conductor and first-layer length produced before the finished quote length.",
            inputs.CoreStartupLength.Value,
            0,
            "m");
        var coreProductionLengthValue =
            inputs.FinishedQuoteLength.Value + inputs.CoreStartupLength.Value;
        var coreProductionLength = new LengthMetres(coreProductionLengthValue);
        var coreProductionLengthStep = DerivedStep(
            "dual-core-production-length",
            "Core and first-layer production length",
            "Finished quote length plus the separately visible core start-up length.",
            "FinishedQuoteLength + CoreStartupLength",
            $"{Raw(inputs.FinishedQuoteLength.Value)} m + " +
            $"{Raw(inputs.CoreStartupLength.Value)} m",
            coreProductionLengthValue,
            0,
            "m",
            [finishedLengthStep, coreStartupLengthStep]);
        var secondLayerProductionLength = inputs.FinishedQuoteLength;
        var secondLayerProductionLengthStep = DerivedStep(
            "dual-second-layer-production-length",
            "Second-layer production length",
            "Only the finished core receives the second insulation layer.",
            "FinishedQuoteLength",
            $"{Raw(inputs.FinishedQuoteLength.Value)} m",
            secondLayerProductionLength.Value,
            0,
            "m",
            [finishedLengthStep]);

        var firstLayer = inputs.FirstLayer;
        var firstLayerResult = SingleCoreCostingCalculator.Calculate(
            new SingleCoreCostingInputs(
                inputs.ConductorReference,
                inputs.ConductorQuote,
                inputs.ConductorYield,
                inputs.ConductorOutsideDiameter,
                firstLayer.CompoundReference,
                firstLayer.CompoundQuote,
                firstLayer.CompoundSpecificGravity,
                firstLayer.NominalFinishedOutsideDiameter,
                firstLayer.PositiveOutsideDiameterTolerance,
                firstLayer.MasterbatchReference,
                firstLayer.MasterbatchQuote,
                firstLayer.MasterbatchAdditionRate,
                coreProductionLength,
                inputs.UsageAllowanceRate,
                new RiskRateFraction(0m),
                new MarkupRateFraction(0m)));

        var secondLayerCompound = CalculateSecondLayerCompound(
            inputs,
            secondLayerProductionLength);

        var secondLayerBaseMass =
            MaterialCostingFormulas.MassForLength(
                secondLayerCompound.Material.BaseKilogramsPerMetre.Value,
                secondLayerProductionLength);
        var secondLayerMasterbatchRaw = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(secondLayerBaseMass),
                inputs.UsageAllowanceRate,
                inputs.SecondLayer.MasterbatchAdditionRate,
                secondLayerProductionLength,
                inputs.SecondLayer.MasterbatchQuote.PricePerKilogram));
        var secondLayerMasterbatch = PrefixMasterbatchResult(
            secondLayerMasterbatchRaw,
            "second-layer-");
        var secondLayerMasterbatchSupplierSteps = SupplierQuoteSteps(
            "second-layer-masterbatch",
            "Second-layer masterbatch",
            inputs.SecondLayer.MasterbatchReference,
            inputs.SecondLayer.MasterbatchQuote);

        var firstMaterialPerMetre =
            firstLayerResult.CoreMaterialPricePerMetre.Value;
        var secondCompoundPerMetre =
            secondLayerCompound.Material.PricePerMetre.Value;
        var secondMasterbatchPerMetre =
            secondLayerMasterbatch.MasterbatchPricePerMetre.Value;
        var secondLayerPerFinishedMetre =
            secondCompoundPerMetre +
            secondMasterbatchPerMetre;

        var firstMaterialPerMetreStep = AssertStep(
            firstLayerResult.Steps,
            "core-material-price-per-metre");
        var firstMaterialForRunStep = AssertStep(
            firstLayerResult.Steps,
            "core-material-price-for-quote");
        var secondCompoundPerMetreStep = AssertStep(
            secondLayerCompound.Steps,
            "second-layer-compound-price-per-metre");
        var secondMasterbatchPerMetreStep =
            secondLayerMasterbatch.Steps[^1];
        var secondLayerPerFinishedMetreStep = DerivedStep(
            "dual-second-layer-price-per-finished-metre",
            "Second-layer material price per finished metre",
            "Second-layer compound and masterbatch cost for one finished metre.",
            "SecondLayerCompoundPricePerMetre + SecondLayerMasterbatchPricePerMetre",
            $"{Raw(secondCompoundPerMetre)} £/m + " +
            $"{Raw(secondMasterbatchPerMetre)} £/m",
            secondLayerPerFinishedMetre,
            4,
            "£/m",
            [
                secondCompoundPerMetreStep,
                secondMasterbatchPerMetreStep,
            ]);

        var secondLayerForRun =
            MaterialCostingFormulas.PriceForLength(
                secondLayerPerFinishedMetre,
                secondLayerProductionLength);
        var secondLayerForRunStep = DerivedStep(
            "dual-second-layer-price-for-production-run",
            "Second-layer material price for finished run",
            "Second-layer compound and masterbatch cost across finished quote length only.",
            "SecondLayerPricePerFinishedMetre × FinishedQuoteLength",
            $"{Raw(secondLayerPerFinishedMetre)} £/m × " +
            $"{Raw(secondLayerProductionLength.Value)} m",
            secondLayerForRun,
            2,
            "£",
            [
                secondLayerPerFinishedMetreStep,
                secondLayerProductionLengthStep,
            ]);

        var firstMaterialForRun = firstLayerResult.CoreMaterialPriceForQuote;
        var materialForProductionRun =
            firstMaterialForRun + secondLayerForRun;
        var materialForProductionRunStep = DerivedStep(
            "dual-material-price-for-production-run",
            "Dual-insulation material price for production run",
            "The core and first-layer run plus the second layer over finished length, with each subtotal added once.",
            "CoreAndFirstLayerPriceForRun + SecondLayerPriceForRun",
            $"{Raw(firstMaterialForRun)} £ + " +
            $"{Raw(secondLayerForRun)} £",
            materialForProductionRun,
            2,
            "£",
            [firstMaterialForRunStep, secondLayerForRunStep]);

        var materialPerFinishedMetre =
            materialForProductionRun / inputs.FinishedQuoteLength.Value;
        var materialPerFinishedMetreStep = DerivedStep(
            "dual-material-price-per-finished-metre",
            "Dual-insulation material price per finished metre",
            "The complete production-run material cost distributed only across customer-delivered metres.",
            "MaterialPriceForProductionRun ÷ FinishedQuoteLength",
            $"{Raw(materialForProductionRun)} £ ÷ " +
            $"{Raw(inputs.FinishedQuoteLength.Value)} m",
            materialPerFinishedMetre,
            4,
            "£/m",
            [materialForProductionRunStep, finishedLengthStep]);

        var materialSteps = firstLayerResult.Steps
            .Where(
                step =>
                    !step.Id.StartsWith("risk-", StringComparison.Ordinal) &&
                    !step.Id.StartsWith("markup-", StringComparison.Ordinal) &&
                    !step.Id.StartsWith("marked-up-", StringComparison.Ordinal))
            .ToArray();
        return new DualInsulationCostingResult(
            coreProductionLength,
            secondLayerProductionLength,
            firstLayerResult.Conductor,
            firstLayerResult.Compound,
            firstLayerResult.Masterbatch,
            secondLayerCompound,
            secondLayerMasterbatch,
            new PricePerMetre(firstMaterialPerMetre),
            new PricePerMetre(secondLayerPerFinishedMetre),
            materialForProductionRun,
            new PricePerMetre(materialPerFinishedMetre),
            [
                finishedLengthStep,
                coreStartupLengthStep,
                coreProductionLengthStep,
                secondLayerProductionLengthStep,
                .. materialSteps,
                .. secondLayerCompound.Steps,
                .. secondLayerMasterbatchSupplierSteps,
                .. secondLayerMasterbatch.Steps,
                secondLayerPerFinishedMetreStep,
                secondLayerForRunStep,
                materialForProductionRunStep,
                materialPerFinishedMetreStep,
            ]);
    }

    private static CompoundUsageCostResult CalculateSecondLayerCompound(
        DualInsulationCostingInputs inputs,
        LengthMetres secondLayerProductionLength)
    {
        var first = inputs.FirstLayer;
        var second = inputs.SecondLayer;
        var firstNominal = first.NominalFinishedOutsideDiameter.Value;
        var firstTolerance = first.PositiveOutsideDiameterTolerance.Value;
        var innerMinimum = firstNominal - firstTolerance;
        var secondNominal = second.NominalFinishedOutsideDiameter.Value;
        var secondTolerance = second.PositiveOutsideDiameterTolerance.Value;
        var outerMaximum = secondNominal + secondTolerance;

        if (innerMinimum <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "The first-layer minimum outside diameter must be greater than zero.");
        }

        if (secondNominal <= firstNominal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "The second-layer nominal outside diameter must exceed the first-layer nominal outside diameter.");
        }

        if (outerMaximum <= innerMinimum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "The second-layer maximum outside diameter must exceed the first-layer minimum outside diameter.");
        }

        var firstNominalStep = InputStep(
            "first-layer-nominal-outside-diameter",
            "First-layer nominal outside diameter",
            "The nominal first-layer diameter forming the inner boundary of layer two.",
            firstNominal,
            3,
            "mm");
        var firstToleranceStep = InputStep(
            "first-layer-outside-diameter-tolerance",
            "First-layer outside-diameter tolerance",
            "The first-layer tolerance subtracted to form the minimum inner boundary for maximum second-layer material usage.",
            firstTolerance,
            3,
            "mm");
        var innerMinimumStep = DerivedStep(
            "second-layer-minimum-inner-diameter",
            "Second-layer minimum inner diameter",
            "The smallest permitted first-layer diameter used as the inner boundary of the second-layer annulus.",
            "FirstLayerNominalOutsideDiameter - FirstLayerTolerance",
            $"{Raw(firstNominal)} mm - {Raw(firstTolerance)} mm",
            innerMinimum,
            3,
            "mm",
            [firstNominalStep, firstToleranceStep]);
        var secondNominalStep = InputStep(
            "second-layer-nominal-outside-diameter",
            "Second-layer nominal outside diameter",
            "The target finished diameter after the second insulation layer.",
            secondNominal,
            3,
            "mm");
        var secondToleranceStep = InputStep(
            "second-layer-outside-diameter-tolerance",
            "Second-layer outside-diameter tolerance",
            "The positive second-layer tolerance added for maximum material usage.",
            secondTolerance,
            3,
            "mm");
        var outerMaximumStep = DerivedStep(
            "second-layer-maximum-outside-diameter",
            "Second-layer maximum outside diameter",
            "Second-layer nominal diameter plus its positive tolerance.",
            "SecondLayerNominalOutsideDiameter + SecondLayerTolerance",
            $"{Raw(secondNominal)} mm + {Raw(secondTolerance)} mm",
            outerMaximum,
            3,
            "mm",
            [secondNominalStep, secondToleranceStep]);

        var innerArea =
            MaterialCostingFormulas.CircularAreaSquareMillimetres(
                innerMinimum);
        var innerAreaStep = DerivedStep(
            "second-layer-inner-cross-sectional-area",
            "Second-layer inner cross-sectional area",
            "The circular area inside the second insulation layer at the first-layer minimum diameter.",
            "π ÷ 4 × MinimumInnerDiameter²",
            $"π ÷ 4 × {Raw(innerMinimum)}²",
            innerArea,
            6,
            "mm²",
            [innerMinimumStep]);
        var outerArea =
            MaterialCostingFormulas.CircularAreaSquareMillimetres(
                outerMaximum);
        var outerAreaStep = DerivedStep(
            "second-layer-outer-cross-sectional-area",
            "Second-layer outer cross-sectional area",
            "The circular area at the second-layer maximum outside diameter.",
            "π ÷ 4 × MaximumOuterDiameter²",
            $"π ÷ 4 × {Raw(outerMaximum)}²",
            outerArea,
            6,
            "mm²",
            [outerMaximumStep]);
        var compoundArea =
            MaterialCostingFormulas.AnnularAreaSquareMillimetres(
                innerMinimum,
                outerMaximum);
        var compoundAreaStep = DerivedStep(
            "second-layer-compound-cross-sectional-area",
            "Second-layer compound cross-sectional area",
            "The maximum second-layer annulus after subtracting the minimum inner area from maximum outer area.",
            "SecondLayerOuterArea - SecondLayerInnerArea",
            $"{Raw(outerArea)} mm² - {Raw(innerArea)} mm²",
            compoundArea,
            6,
            "mm²",
            [outerAreaStep, innerAreaStep]);

        var specificGravity = second.CompoundSpecificGravity.Value;
        var specificGravityStep = InputStep(
            "second-layer-compound-specific-gravity",
            "Second-layer compound specific gravity",
            $"The density factor for {second.CompoundReference}.",
            specificGravity,
            4,
            "g/cm³");
        var baseKilogramsPerMetre =
            MaterialCostingFormulas.CompoundKilogramsPerMetre(
                compoundArea,
                specificGravity);
        var gramsPerMetre = baseKilogramsPerMetre * 1000m;
        var gramsPerMetreStep = DerivedStep(
            "second-layer-compound-grams-per-metre-before-allowance",
            "Second-layer compound base usage",
            "The second-layer mass per metre before the general usage allowance.",
            "SecondLayerCompoundArea × SpecificGravity",
            $"{Raw(compoundArea)} mm² × {Raw(specificGravity)} g/cm³",
            gramsPerMetre,
            6,
            "g/m",
            [compoundAreaStep, specificGravityStep]);
        var baseKilogramsStep = DerivedStep(
            "second-layer-compound-kilograms-per-metre-before-allowance",
            "Second-layer compound base usage",
            "The second-layer base usage converted to kilograms per metre.",
            "SecondLayerCompoundGramsPerMetre ÷ 1000",
            $"{Raw(gramsPerMetre)} g/m ÷ 1000 g/kg",
            baseKilogramsPerMetre,
            9,
            "kg/m",
            [gramsPerMetreStep]);

        var allowanceRateStep = PercentageStep(
            "second-layer-usage-allowance-rate",
            "Waste/start-up usage allowance",
            "The shared general material-usage boost applied once to layer two.",
            inputs.UsageAllowanceRate.Value);
        var allowanceMultiplier = 1m + inputs.UsageAllowanceRate.Value;
        var allowanceMultiplierStep = DerivedStep(
            "second-layer-usage-allowance-multiplier",
            "Usage allowance multiplier",
            "One plus the general waste/start-up allowance.",
            "1 + UsageAllowanceRate",
            $"1 + {Raw(inputs.UsageAllowanceRate.Value)}",
            allowanceMultiplier,
            4,
            "×",
            [allowanceRateStep]);
        var adjustedKilogramsPerMetre =
            MaterialCostingFormulas.ApplyUsageAllowance(
                baseKilogramsPerMetre,
                inputs.UsageAllowanceRate);
        var adjustedKilogramsStep = DerivedStep(
            "second-layer-compound-kilograms-per-metre-with-allowance",
            "Second-layer compound usage with allowance",
            "The second-layer usage after one application of the general allowance.",
            "SecondLayerBaseKilogramsPerMetre × UsageAllowanceMultiplier",
            $"{Raw(baseKilogramsPerMetre)} kg/m × {Raw(allowanceMultiplier)}",
            adjustedKilogramsPerMetre,
            9,
            "kg/m",
            [baseKilogramsStep, allowanceMultiplierStep]);
        var productionLengthStep = InputStep(
            "second-layer-production-length",
            "Second-layer production length",
            "The finished quote length; the core-only start-up does not receive layer two.",
            secondLayerProductionLength.Value,
            0,
            "m");
        var quoteMass = MaterialCostingFormulas.MassForLength(
            adjustedKilogramsPerMetre,
            secondLayerProductionLength);
        var quoteMassStep = DerivedStep(
            "second-layer-compound-quote-mass",
            "Second-layer compound mass for production run",
            "Adjusted second-layer usage across the total production length.",
            "SecondLayerKilogramsPerMetre × ProductionLength",
            $"{Raw(adjustedKilogramsPerMetre)} kg/m × " +
            $"{Raw(secondLayerProductionLength.Value)} m",
            quoteMass,
            6,
            "kg",
            [adjustedKilogramsStep, productionLengthStep]);

        var supplierQuoteSteps = SupplierQuoteSteps(
            "second-layer-compound",
            "Second-layer compound",
            second.CompoundReference,
            second.CompoundQuote);
        var pricePerMetre =
            MaterialCostingFormulas.PricePerMetre(
                adjustedKilogramsPerMetre,
                second.CompoundQuote.PricePerKilogram);
        var pricePerMetreStep = DerivedStep(
            "second-layer-compound-price-per-metre",
            "Second-layer compound price per production metre",
            "Adjusted second-layer usage multiplied by supplier price per kilogram.",
            "SecondLayerKilogramsPerMetre × CompoundPricePerKilogram",
            $"{Raw(adjustedKilogramsPerMetre)} kg/m × " +
            $"{Raw(second.CompoundQuote.PricePerKilogram.Value)} £/kg",
            pricePerMetre,
            4,
            "£/m",
            [adjustedKilogramsStep, supplierQuoteSteps[^1]]);
        var quotePrice = MaterialCostingFormulas.PriceForLength(
            pricePerMetre,
            secondLayerProductionLength);
        var quotePriceStep = DerivedStep(
            "second-layer-compound-price-for-production-run",
            "Second-layer compound price for production run",
            "Second-layer compound material cost across the production length.",
            "SecondLayerCompoundPricePerMetre × ProductionLength",
            $"{Raw(pricePerMetre)} £/m × " +
            $"{Raw(secondLayerProductionLength.Value)} m",
            quotePrice,
            2,
            "£",
            [pricePerMetreStep, productionLengthStep]);

        var material = new MaterialUsageCostResult(
            new KilogramsPerMetre(baseKilogramsPerMetre),
            new KilogramsPerMetre(adjustedKilogramsPerMetre),
            new MassKilograms(quoteMass),
            new PricePerMetre(pricePerMetre),
            quotePrice,
            [
                gramsPerMetreStep,
                baseKilogramsStep,
                allowanceRateStep,
                allowanceMultiplierStep,
                adjustedKilogramsStep,
                productionLengthStep,
                quoteMassStep,
                .. supplierQuoteSteps,
                pricePerMetreStep,
                quotePriceStep,
            ]);

        return new CompoundUsageCostResult(
            innerArea,
            outerArea,
            compoundArea,
            gramsPerMetre,
            material,
            [
                firstNominalStep,
                firstToleranceStep,
                innerMinimumStep,
                secondNominalStep,
                secondToleranceStep,
                outerMaximumStep,
                innerAreaStep,
                outerAreaStep,
                compoundAreaStep,
                specificGravityStep,
                .. material.Steps,
            ]);
    }

    private static void ValidateReferences(DualInsulationCostingInputs inputs)
    {
        if (string.IsNullOrWhiteSpace(inputs.ConductorReference))
        {
            throw new ArgumentException(
                "A conductor reference is required.",
                nameof(inputs));
        }

        if (string.IsNullOrWhiteSpace(inputs.FirstLayer.CompoundReference) ||
            string.IsNullOrWhiteSpace(inputs.SecondLayer.CompoundReference))
        {
            throw new ArgumentException(
                "Both insulation-layer compound references are required.",
                nameof(inputs));
        }

        if (string.IsNullOrWhiteSpace(inputs.FirstLayer.MasterbatchReference) ||
            string.IsNullOrWhiteSpace(inputs.SecondLayer.MasterbatchReference))
        {
            throw new ArgumentException(
                "Both insulation-layer masterbatch references are required.",
                nameof(inputs));
        }
    }

    private static IReadOnlyList<CalculationStep> SupplierQuoteSteps(
        string idPrefix,
        string labelPrefix,
        string materialReference,
        MaterialSupplierQuote quote)
    {
        var totalStep = InputStep(
            $"{idPrefix}-supplier-quote-total",
            $"{labelPrefix} supplier quote total",
            $"The supplier's total price for {materialReference}.",
            quote.Total.Value,
            2,
            "£");
        var massStep = InputStep(
            $"{idPrefix}-supplier-quoted-mass",
            $"{labelPrefix} supplier quoted mass",
            "The material mass covered by the supplier quote.",
            quote.QuotedMass.Value,
            3,
            "kg");
        var priceStep = DerivedStep(
            $"{idPrefix}-price-per-kilogram",
            $"{labelPrefix} calculated price per kilogram",
            "Supplier quote total divided by quoted material mass.",
            "SupplierQuoteTotal ÷ SupplierQuotedMass",
            $"{Raw(quote.Total.Value)} £ ÷ {Raw(quote.QuotedMass.Value)} kg",
            quote.PricePerKilogram.Value,
            5,
            "£/kg",
            [totalStep, massStep]);
        return [totalStep, massStep, priceStep];
    }

    private static MasterbatchUsageResult PrefixMasterbatchResult(
        MasterbatchUsageResult result,
        string prefix) =>
        result with
        {
            Steps = result.Steps
                .Select(step => PrefixStep(step, prefix))
                .ToArray(),
        };

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
