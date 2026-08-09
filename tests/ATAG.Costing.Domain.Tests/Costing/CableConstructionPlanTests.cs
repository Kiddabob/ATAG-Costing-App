using ATAG.Costing.Domain.Costing;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Costing;

public sealed class CableConstructionPlanTests
{
    [Fact]
    public void DualPlan_InsertsSelectedModulesBetweenTheTwoInsulationLayers()
    {
        var plan = CableConstructionPlan.Create(
            CableConstructionKind.DualInsulated,
            addOnModules:
            [
                CableAddOnModule.Foil,
                CableAddOnModule.DrainWire,
                CableAddOnModule.Braid,
            ]);

        Assert.Collection(
            plan.Stages,
            stage => Assert.Equal("conductor", stage.Reference),
            stage => Assert.Equal("first-insulation", stage.Reference),
            stage => Assert.Equal(CableAddOnModule.Foil, stage.Module),
            stage => Assert.Equal(CableAddOnModule.DrainWire, stage.Module),
            stage => Assert.Equal(CableAddOnModule.Braid, stage.Module),
            stage => Assert.Equal("second-insulation", stage.Reference));
    }

    [Theory]
    [InlineData(CableConstructionKind.Flat)]
    [InlineData(CableConstructionKind.DShape)]
    public void InLineFutureConstructions_SupportUpToTenCores(
        CableConstructionKind kind)
    {
        var plan = CableConstructionPlan.Create(kind, coreCount: 10);

        Assert.Equal(10, plan.CoreCount);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CableConstructionPlan.Create(kind, coreCount: 11));
    }

    [Fact]
    public void Plan_RejectsDuplicateModulesAndModulesOnSingleCore()
    {
        Assert.Throws<ArgumentException>(
            () => CableConstructionPlan.Create(
                CableConstructionKind.DualInsulated,
                addOnModules:
                [
                    CableAddOnModule.Tape,
                    CableAddOnModule.Tape,
                ]));
        Assert.Throws<ArgumentException>(
            () => CableConstructionPlan.Create(
                CableConstructionKind.CorSingleInsulatedCore,
                addOnModules:
                [
                    CableAddOnModule.Foil,
                ]));
    }
}
