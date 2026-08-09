using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Costing;
using ATAG.Costing.Domain.Materials;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Costing;

public sealed class ProductionAndCommercialCostingTests
{
    [Theory]
    [InlineData(1.00, 15000)]
    [InlineData(1.20, 13000)]
    [InlineData(2.00, 8000)]
    [InlineData(2.50, 6000)]
    [InlineData(2.51, 700)]
    public void LineSpeedPolicy_UsesTheWorkbookDiameterBands(
        double diameter,
        double expectedSpeed)
    {
        var speed = InsulationLineSpeedPolicy.Select(
            new Millimetres((decimal)diameter));

        Assert.Equal((decimal)expectedSpeed, speed.Value);
    }

    [Fact]
    public void ProductionLabour_CalculatesTimeOperatorsAndCost()
    {
        var result = ProductionLabourCalculator.Calculate(
            new ProductionLabourInputs(
                "Insulation",
                new LengthMetres(5000m),
                new Millimetres(1.20m),
                ManualLineSpeed: null,
                new LabourHours(0.25m),
                new OperatorCount(2m),
                new HourlyLabourRate(35m)));

        Assert.Equal(13000m, result.EffectiveLineSpeed.Value);
        Assert.Equal(5000m / 13000m, result.RunningTime.Value);
        Assert.Equal((5000m / 13000m) + 0.25m, result.TotalProcessTime.Value);
        Assert.Equal(
            (((5000m / 13000m) + 0.25m) * 2m) * 35m,
            result.LabourCost);
        Assert.Contains(
            result.Steps,
            step => step.Id == "labour-cost-for-quote");
    }

    [Fact]
    public void DualProduction_UsesIndependentLineProfilesAndPhysicalRunLengths()
    {
        var materialCosting = DualInsulationCostingCalculator.Calculate(
            DualMaterialInputs());
        var firstProfile = Profile("Line 1", "line-1/v1", 5000m);
        var secondProfile = Profile("Line 2", "line-2/v1", 2500m);

        var result = DualInsulationProductionCalculator.Calculate(
            new DualInsulationProductionInputs(
                materialCosting,
                new ExtrusionProductionSettings(
                    "First insulation extrusion",
                    new Millimetres(2.2m),
                    firstProfile,
                    ManualLineSpeed: null,
                    new LabourHours(0.25m),
                    new OperatorCount(1m),
                    new HourlyLabourRate(35m)),
                new ExtrusionProductionSettings(
                    "Second insulation extrusion",
                    new Millimetres(3.2m),
                    secondProfile,
                    ManualLineSpeed: null,
                    new LabourHours(0.5m),
                    new OperatorCount(2m),
                    new HourlyLabourRate(40m))));

        Assert.Equal(5000m, result.FirstExtrusion.EffectiveLineSpeed.Value);
        Assert.Equal(2500m, result.SecondExtrusion.EffectiveLineSpeed.Value);
        Assert.Equal(
            10200m / 5000m,
            result.FirstExtrusion.RunningTime.Value);
        Assert.Equal(
            10000m / 2500m,
            result.SecondExtrusion.RunningTime.Value);
        Assert.Equal(
            result.FirstExtrusion.LabourCost +
            result.SecondExtrusion.LabourCost,
            result.TotalLabourCost);
        Assert.DoesNotContain(
            result.Steps,
            step =>
                step.Id.Contains("masterbatch", StringComparison.OrdinalIgnoreCase) ||
                step.Label.Contains("masterbatch", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProductionLabour_UsesTheSelectedExtrusionProfileInTheTrace()
    {
        var profile = Profile("Second extrusion", "second-extrusion/v3", 2700m);

        var result = ProductionLabourCalculator.Calculate(
            new ProductionLabourInputs(
                "Second insulation extrusion",
                new LengthMetres(10000m),
                new Millimetres(3.2m),
                ManualLineSpeed: null,
                new LabourHours(0m),
                new OperatorCount(1m),
                new HourlyLabourRate(35m),
                profile));

        Assert.Equal(2700m, result.RecommendedLineSpeed.Value);
        var step = Assert.Single(
            result.Steps,
            item => item.Id == "recommended-line-speed");
        Assert.Equal("second-extrusion/v3", step.RuleVersion);
        Assert.Contains("Second extrusion", step.BusinessMeaning);
    }

    [Fact]
    public void CommercialPricing_LabelsThreeDistinctMethods()
    {
        var result = CommercialPricingCalculator.Calculate(
            new CommercialPricingInputs(
                MaterialCost: 100m,
                LabourCost: 20m,
                new RiskRateFraction(0.10m),
                new MarkupRateFraction(0.45m),
                new TargetMarginRateFraction(0.45m)));

        Assert.Equal(120m, result.EstimatedCost);
        Assert.Equal(12m, result.RiskValue);
        Assert.Equal(132m, result.RiskAdjustedCost);
        Assert.Equal(59.4m, result.MarkupValue);
        Assert.Equal(191.4m, result.SequentialRiskThenMarkupPrice);
        Assert.Equal(186m, result.CombinedRiskAndMarkupPrice);
        Assert.Equal(240m, result.TargetGrossMarginPrice);
        Assert.Contains(
            result.Steps,
            step =>
                step.Id == "target-gross-margin-price" &&
                step.BusinessMeaning!.Contains("gross margin"));
    }

    [Fact]
    public void CoreNameGenerator_ReproducesTheVisibleSingleCoreConvention()
    {
        var result = CoreNameGenerator.Generate(
            new CoreNameInputs(
                "7/0.196 TCW (H) (0.21mm²)",
                "PVC2",
                IsCustomerSpecial: true,
                CustomerShortName: "Aflex Cables"));

        Assert.Equal("COR 0720 T T2 (Aflex Cables)", result.GeneratedName);
        Assert.Equal("07", result.StrandCountCode);
        Assert.Equal("20", result.StrandDiameterCode);
        Assert.Equal("T", result.WireCode);
        Assert.Equal("T2", result.MaterialTypeCode);
    }

    [Fact]
    public void NewTypedInputs_RejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LineSpeedMetresPerHour(0m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LabourHours(-0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HourlyLabourRate(-1m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OperatorCount(0m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TargetMarginRateFraction(1m));
    }

    private static ExtrusionLineSpeedProfile Profile(
        string reference,
        string ruleVersion,
        decimal speed) =>
        new(
            reference,
            ruleVersion,
            [
                new ExtrusionLineSpeedBand(
                    new Millimetres(3.5m),
                    new LineSpeedMetresPerHour(speed)),
            ],
            new LineSpeedMetresPerHour(speed / 2m));

    private static DualInsulationCostingInputs DualMaterialInputs() =>
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
