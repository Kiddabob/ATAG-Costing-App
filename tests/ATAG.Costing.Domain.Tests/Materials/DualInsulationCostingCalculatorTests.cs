using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Materials;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Materials;

public sealed class DualInsulationCostingCalculatorTests
{
    [Fact]
    public void Calculate_ReproducesTheCorrectedDualInsulationReferenceCase()
    {
        var result = CalculateReferenceCase();

        Assert.Equal(10200m, result.CoreProductionLength.Value);
        Assert.Equal(10000m, result.SecondLayerProductionLength.Value);
        Assert.InRange(
            result.Conductor.QuotePrice,
            500.8581314242926m,
            500.8581314242929m);
        Assert.InRange(
            result.FirstLayerCompound.CompoundAreaSquareMillimetres,
            3.4581277483929m,
            3.4581277483932m);
        Assert.InRange(
            result.SecondLayerCompound.CompoundAreaSquareMillimetres,
            4.8375618126760m,
            4.8375618126763m);
        Assert.InRange(
            result.SecondLayerCompound.Material.QuoteMass.Value,
            65.7714904051447m,
            65.7714904051453m);
        Assert.InRange(
            result.SecondLayerMasterbatch.MasterbatchMassForQuote.Value,
            0.6577149040512m,
            0.6577149040517m);
        Assert.InRange(
            result.MaterialPriceForProductionRun,
            815.4229279099002m,
            815.4229279099012m);
        Assert.InRange(
            result.MaterialPricePerFinishedMetre.Value,
            0.0815422927908m,
            0.0815422927912m);
    }

    [Fact]
    public void Calculate_AppliesTheGeneralAllowanceExactlyOnceToEveryStream()
    {
        var result = CalculateReferenceCase();
        const decimal coreProductionLength = 10200m;
        const decimal secondLayerProductionLength = 10000m;
        const decimal allowanceMultiplier = 1.03m;

        Assert.Equal(
            result.Conductor.BaseKilogramsPerMetre.Value * allowanceMultiplier,
            result.Conductor.AdjustedKilogramsPerMetre.Value);
        Assert.Equal(
            result.FirstLayerCompound.Material.BaseKilogramsPerMetre.Value *
            allowanceMultiplier,
            result.FirstLayerCompound.Material.AdjustedKilogramsPerMetre.Value);
        Assert.Equal(
            result.SecondLayerCompound.Material.BaseKilogramsPerMetre.Value *
            allowanceMultiplier,
            result.SecondLayerCompound.Material.AdjustedKilogramsPerMetre.Value);

        Assert.Equal(
            result.FirstLayerCompound.Material.BaseKilogramsPerMetre.Value *
            coreProductionLength *
            allowanceMultiplier *
            0.01m,
            result.FirstLayerMasterbatch.MasterbatchMassForQuote.Value);
        Assert.Equal(
            result.SecondLayerCompound.Material.BaseKilogramsPerMetre.Value *
            secondLayerProductionLength *
            allowanceMultiplier *
            0.01m,
            result.SecondLayerMasterbatch.MasterbatchMassForQuote.Value);
    }

    [Fact]
    public void Calculate_AddsEveryMaterialPriceExactlyOnce()
    {
        var result = CalculateReferenceCase();
        var firstMasterbatchPrice =
            result.FirstLayerMasterbatch.MasterbatchPricePerMetre.Value *
            result.CoreProductionLength.Value;
        var secondMasterbatchPrice =
            result.SecondLayerMasterbatch.MasterbatchPricePerMetre.Value *
            result.SecondLayerProductionLength.Value;
        var expected =
            result.Conductor.QuotePrice +
            result.FirstLayerCompound.Material.QuotePrice +
            firstMasterbatchPrice +
            result.SecondLayerCompound.Material.QuotePrice +
            secondMasterbatchPrice;

        Assert.InRange(
            result.MaterialPriceForProductionRun,
            expected - 0.00000000000000000000001m,
            expected + 0.00000000000000000000001m);
    }

    [Fact]
    public void Calculate_KeepsMasterbatchAsMaterialCostOnly()
    {
        var result = CalculateReferenceCase();

        Assert.DoesNotContain(
            result.Steps,
            step =>
                (step.Id.Contains(
                     "masterbatch",
                     StringComparison.OrdinalIgnoreCase) ||
                 step.Label.Contains(
                     "masterbatch",
                     StringComparison.OrdinalIgnoreCase)) &&
                (step.Id.Contains(
                     "labour",
                     StringComparison.OrdinalIgnoreCase) ||
                 step.Id.Contains(
                     "line-speed",
                     StringComparison.OrdinalIgnoreCase) ||
                 step.Label.Contains(
                     "production time",
                     StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Calculate_UsesCoreStartupOnlyForTheConductorAndFirstLayer()
    {
        var result = CalculateReferenceCase();

        Assert.Equal(10200m, result.Conductor.QuoteMass.Value /
            result.Conductor.AdjustedKilogramsPerMetre.Value);
        Assert.Equal(10200m, result.FirstLayerCompound.Material.QuoteMass.Value /
            result.FirstLayerCompound.Material.AdjustedKilogramsPerMetre.Value);
        Assert.Equal(10000m, result.SecondLayerCompound.Material.QuoteMass.Value /
            result.SecondLayerCompound.Material.AdjustedKilogramsPerMetre.Value);

        var coreProductionLength = Assert.Single(
            result.Steps,
            step => step.Id == "dual-core-production-length");
        Assert.Equal(2, coreProductionLength.InputSteps.Count);
        var secondLayerProductionLength = Assert.Single(
            result.Steps,
            step => step.Id == "dual-second-layer-production-length");
        Assert.Single(secondLayerProductionLength.InputSteps);
    }

    [Fact]
    public void Calculate_ReturnsAuditableGeometryAndSubtotalFlow()
    {
        var result = CalculateReferenceCase();

        var secondLayerArea = Assert.Single(
            result.Steps,
            step => step.Id == "second-layer-compound-cross-sectional-area");
        Assert.Equal(2, secondLayerArea.InputSteps.Count);

        var secondLayerSubtotal = Assert.Single(
            result.Steps,
            step => step.Id == "dual-second-layer-price-per-finished-metre");
        Assert.Equal(2, secondLayerSubtotal.InputSteps.Count);
        var materialSubtotal = Assert.Single(
            result.Steps,
            step => step.Id == "dual-material-price-for-production-run");
        Assert.Equal(2, materialSubtotal.InputSteps.Count);

        var prefixedMasterbatchStep = Assert.Single(
            result.Steps,
            step => step.Id == "second-layer-masterbatch-mass-for-quote");
        Assert.All(
            prefixedMasterbatchStep.InputSteps,
            input => Assert.StartsWith("second-layer-", input.Id));
        Assert.All(
            result.Steps,
            step => Assert.False(string.IsNullOrWhiteSpace(step.RuleVersion)));
        Assert.All(
            result.Steps,
            step => Assert.False(string.IsNullOrWhiteSpace(step.RoundingRule)));
    }

    [Fact]
    public void Calculate_RejectsInvalidLayerGeometry()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DualInsulationCostingCalculator.Calculate(
                ReferenceInputs() with
                {
                    SecondLayer = ReferenceInputs().SecondLayer with
                    {
                        NominalFinishedOutsideDiameter =
                            new Millimetres(2.2m),
                    },
                }));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => DualInsulationCostingCalculator.Calculate(
                ReferenceInputs() with
                {
                    FirstLayer = ReferenceInputs().FirstLayer with
                    {
                        PositiveOutsideDiameterTolerance =
                            new Millimetres(2.2m),
                    },
                }));
    }

    [Fact]
    public void TypedInput_AcceptsZeroStartupAndRejectsNegativeStartup()
    {
        var result = DualInsulationCostingCalculator.Calculate(
            ReferenceInputs() with
            {
                CoreStartupLength = new AdditionalProductionLengthMetres(0m),
            });

        Assert.Equal(10000m, result.CoreProductionLength.Value);
        Assert.Equal(10000m, result.SecondLayerProductionLength.Value);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AdditionalProductionLengthMetres(-0.001m));
    }

    private static DualInsulationCostingResult CalculateReferenceCase() =>
        DualInsulationCostingCalculator.Calculate(ReferenceInputs());

    private static DualInsulationCostingInputs ReferenceInputs() =>
        new(
            "7/0.253 TCW (H) (0.35mm²)",
            new MaterialSupplierQuote(
                new SupplierQuoteTotal(15200m),
                new MassKilograms(1000m)),
            new YieldMetresPerKilogram(318.835195m),
            new Millimetres(0.74m),
            new DualInsulationLayerInputs(
                "SX 0612 S",
                new MaterialSupplierQuote(
                    new SupplierQuoteTotal(1116.5m),
                    new MassKilograms(350m)),
                new SpecificGravity(1.47m),
                new Millimetres(2.2m),
                new Millimetres(0.025m),
                "Natural",
                new MaterialSupplierQuote(
                    new SupplierQuoteTotal(0m),
                    new MassKilograms(1m)),
                new AdditionRateFraction(0.01m)),
            new DualInsulationLayerInputs(
                "FC1031CHT (PVT5TWLFX)",
                new MaterialSupplierQuote(
                    new SupplierQuoteTotal(2130m),
                    new MassKilograms(1000m)),
                new SpecificGravity(1.32m),
                new Millimetres(3.2m),
                new Millimetres(0.1m),
                "Bright White",
                new MaterialSupplierQuote(
                    new SupplierQuoteTotal(6.24m),
                    new MassKilograms(1m)),
                new AdditionRateFraction(0.01m)),
            new LengthMetres(10000m),
            new AdditionalProductionLengthMetres(200m),
            new UsageAllowanceRateFraction(0.03m));
}
