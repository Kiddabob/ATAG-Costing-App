using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Materials;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Materials;

public sealed class SingleCoreCostingCalculatorTests
{
    [Fact]
    public void Calculate_ReproducesTheWorkbookBasedOneCoreInputs()
    {
        var result = CalculateReferenceCase();

        Assert.InRange(
            result.Conductor.QuotePrice,
            100.8049521744023m,
            100.8049521744025m);
        Assert.InRange(
            result.Compound.CompoundAreaSquareMillimetres,
            0.9143801767813m,
            0.9143801767815m);
        Assert.InRange(
            result.Compound.Material.BaseKilogramsPerMetre.Value,
            0.0011704066262m,
            0.0011704066264m);
        Assert.InRange(
            result.Masterbatch.MasterbatchMassForQuote.Value,
            0.0602759412533m,
            0.0602759412536m);
    }

    [Fact]
    public void Calculate_AppliesTheThreePercentUsageAllowanceOncePerMaterialStream()
    {
        var result = CalculateReferenceCase();

        Assert.Equal(
            result.Conductor.BaseKilogramsPerMetre.Value * 1.03m,
            result.Conductor.AdjustedKilogramsPerMetre.Value);
        Assert.Equal(
            result.Compound.Material.BaseKilogramsPerMetre.Value * 1.03m,
            result.Compound.Material.AdjustedKilogramsPerMetre.Value);

        var baseCompoundQuoteMass =
            result.Compound.Material.BaseKilogramsPerMetre.Value * 5000m;
        Assert.Equal(
            baseCompoundQuoteMass * 1.03m * 0.01m,
            result.Masterbatch.MasterbatchMassForQuote.Value);

        var masterbatchQuotePrice =
            result.Masterbatch.MasterbatchPricePerMetre.Value * 5000m;
        var expectedMasterbatchQuotePrice =
            result.Masterbatch.MasterbatchMassForQuote.Value * 14.83m;
        Assert.InRange(
            masterbatchQuotePrice,
            expectedMasterbatchQuotePrice - 0.000000000000000001m,
            expectedMasterbatchQuotePrice + 0.000000000000000001m);
    }

    [Fact]
    public void Calculate_KeepsMarkupSeparateFromUsage()
    {
        var result = CalculateReferenceCase();

        Assert.Equal(
            result.CoreMaterialPricePerMetre.Value * 1.45m,
            result.MarkedUpPricePerMetre.Value);
        Assert.Equal(
            result.CoreMaterialPriceForQuote * 1.45m,
            result.MarkedUpPriceForQuote);

        var markupStep = Assert.Single(
            result.Steps,
            step => step.Id == "markup-rate");
        Assert.Contains(
            "does not alter material usage",
            Assert.Single(
                result.Steps,
                step => step.Id == "markup-multiplier").BusinessMeaning);
        Assert.Equal("45.00", markupStep.DisplayValue);
        Assert.Equal("%", markupStep.Unit);
    }

    [Fact]
    public void Calculate_AppliesRiskBeforeMarkupAndDoesNotDoubleAddMasterbatch()
    {
        var inputs = ReferenceInputs() with
        {
            RiskRate = new RiskRateFraction(0.10m),
        };
        var result = SingleCoreCostingCalculator.Calculate(inputs);

        Assert.Equal(
            result.CoreMaterialPriceForQuote * 1.10m,
            result.RiskAdjustedPriceForQuote);
        Assert.Equal(
            result.RiskAdjustedPriceForQuote * 1.45m,
            result.MarkedUpPriceForQuote);
        Assert.InRange(
            result.CoreMaterialPriceForQuote,
            111.5238228074997m,
            111.5238228074999m);
    }

    [Fact]
    public void Calculate_DerivesUnitPricesFromSupplierQuoteTotals()
    {
        var result = CalculateReferenceCase();

        var conductorUnitPrice = Assert.Single(
            result.Steps,
            step => step.Id == "conductor-price-per-kilogram");
        Assert.Equal(10.39841m, conductorUnitPrice.RawValue);
        Assert.Equal("£/kg", conductorUnitPrice.Unit);
        Assert.Equal(2, conductorUnitPrice.InputSteps.Count);
    }

    [Fact]
    public void Calculate_ReturnsGeometryUsageCostAndRoundingTrace()
    {
        var result = CalculateReferenceCase();

        Assert.Contains(
            result.Steps,
            step =>
                step.Id == "compound-cross-sectional-area" &&
                step.Expression.Contains("FinishedCoreArea"));
        Assert.Contains(
            result.Steps,
            step =>
                step.Id == "core-material-price-per-metre" &&
                step.InputSteps.Count == 3);
        Assert.All(
            result.Steps,
            step => Assert.False(string.IsNullOrWhiteSpace(step.RuleVersion)));
        Assert.All(
            result.Steps,
            step => Assert.False(string.IsNullOrWhiteSpace(step.RoundingRule)));
    }

    [Fact]
    public void Calculate_RejectsAFinishedCoreThatCannotContainTheConductor()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => SingleCoreCostingCalculator.Calculate(
                ReferenceInputs() with
                {
                    NominalFinishedCoreOutsideDiameter = new Millimetres(0.55m),
                    FinishedCoreOutsideDiameterTolerance = new Millimetres(0.02m),
                }));

        Assert.Contains(
            "must exceed the conductor",
            exception.Message);
    }

    [Fact]
    public void TypedInputs_RejectInvalidCoreValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new YieldMetresPerKilogram(0m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Millimetres(-0.001m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SpecificGravity(0m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MarkupRateFraction(-0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RiskRateFraction(-0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MaterialSupplierQuote(
                new SupplierQuoteTotal(10m),
                new MassKilograms(0m)));
    }

    private static SingleCoreCostingResult CalculateReferenceCase() =>
        SingleCoreCostingCalculator.Calculate(ReferenceInputs());

    private static SingleCoreCostingInputs ReferenceInputs() =>
        new(
            "7/0.196 TCW (H) (0.21mm²)",
            new MaterialSupplierQuote(
                new SupplierQuoteTotal(10398.41m),
                new MassKilograms(1000m)),
            new YieldMetresPerKilogram(531.241872m),
            new Millimetres(0.58m),
            "FC1530CSI (XN78927)",
            new MaterialSupplierQuote(
                new SupplierQuoteTotal(1.63m),
                new MassKilograms(1m)),
            new SpecificGravity(1.28m),
            new Millimetres(1.2m),
            new Millimetres(0.025m),
            "Rocket Red (CUS3872)",
            new MaterialSupplierQuote(
                new SupplierQuoteTotal(14.83m),
                new MassKilograms(1m)),
            new AdditionRateFraction(0.01m),
            new LengthMetres(5000m),
            new UsageAllowanceRateFraction(0.03m),
            new RiskRateFraction(0m),
            new MarkupRateFraction(0.45m));
}
