using ATAG.Costing.Application.Costing;
using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Costing;
using ATAG.Costing.Domain.Materials;
using Xunit;

namespace ATAG.Costing.Application.Tests.Costing;

public sealed class DualInsulationCostingApplicationServiceTests
{
    [Fact]
    public void Calculate_CoordinatesBothProductionScopesAndCommercialResult()
    {
        var service = new DualInsulationCostingApplicationService();

        var result = service.Calculate(
            new DualInsulationCostingRequest(
                MaterialInputs(),
                Production("Fictional line A", "line-a/v1", 5000m, 1m),
                Production("Fictional line B", "line-b/v1", 2500m, 2m),
                new RiskRateFraction(0.05m),
                new MarkupRateFraction(0.40m),
                new TargetMarginRateFraction(0.40m),
                [CableAddOnModule.Tape, CableAddOnModule.DrainWire]));

        Assert.Equal(10200m, result.Materials.CoreProductionLength.Value);
        Assert.Equal(10000m, result.Materials.SecondLayerProductionLength.Value);
        Assert.Equal(
            10200m / 5000m,
            result.Production.FirstExtrusion.RunningTime.Value);
        Assert.Equal(
            10000m / 2500m,
            result.Production.SecondExtrusion.RunningTime.Value);
        Assert.Equal(
            result.Materials.MaterialPriceForProductionRun +
            result.Production.TotalLabourCost,
            result.Commercial.EstimatedCost);
        Assert.Equal(
            [CableAddOnModule.Tape, CableAddOnModule.DrainWire],
            result.Construction.AddOnModules);
    }

    [Fact]
    public void Calculate_RejectsDuplicateOptionalModulesBeforeCosting()
    {
        var service = new DualInsulationCostingApplicationService();

        Assert.Throws<ArgumentException>(
            () => service.Calculate(
                new DualInsulationCostingRequest(
                    MaterialInputs(),
                    Production("Line A", "line-a/v1", 5000m, 1m),
                    Production("Line B", "line-b/v1", 2500m, 1m),
                    new RiskRateFraction(0m),
                    new MarkupRateFraction(0m),
                    new TargetMarginRateFraction(0m),
                    [CableAddOnModule.Foil, CableAddOnModule.Foil])));
    }

    private static ExtrusionProductionSettings Production(
        string name,
        string ruleVersion,
        decimal speed,
        decimal operators) =>
        new(
            name,
            new Millimetres(name.EndsWith('A') ? 2.2m : 3.2m),
            new ExtrusionLineSpeedProfile(
                name,
                ruleVersion,
                [
                    new ExtrusionLineSpeedBand(
                        new Millimetres(4m),
                        new LineSpeedMetresPerHour(speed)),
                ],
                new LineSpeedMetresPerHour(speed / 2m)),
            ManualLineSpeed: null,
            new LabourHours(0.25m),
            new OperatorCount(operators),
            new HourlyLabourRate(30m));

    private static DualInsulationCostingInputs MaterialInputs() =>
        new(
            "Fictional conductor",
            new MaterialSupplierQuote(
                new SupplierQuoteTotal(100m),
                new MassKilograms(10m)),
            new YieldMetresPerKilogram(300m),
            new Millimetres(0.8m),
            Layer("Fictional first compound", 2.2m, 1.4m, "Natural"),
            Layer("Fictional second compound", 3.2m, 1.3m, "Blue"),
            new LengthMetres(10000m),
            new AdditionalProductionLengthMetres(200m),
            new UsageAllowanceRateFraction(0.03m));

    private static DualInsulationLayerInputs Layer(
        string compound,
        decimal outsideDiameter,
        decimal specificGravity,
        string colour) =>
        new(
            compound,
            new MaterialSupplierQuote(
                new SupplierQuoteTotal(20m),
                new MassKilograms(10m)),
            new SpecificGravity(specificGravity),
            new Millimetres(outsideDiameter),
            new Millimetres(0.05m),
            colour,
            new MaterialSupplierQuote(
                new SupplierQuoteTotal(5m),
                new MassKilograms(1m)),
            new AdditionRateFraction(0.01m));
}
