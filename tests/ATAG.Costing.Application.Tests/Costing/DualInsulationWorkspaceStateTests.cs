using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Application.Costing;
using ATAG.Costing.Domain.Costing;
using Xunit;

namespace ATAG.Costing.Application.Tests.Costing;

public sealed class DualInsulationWorkspaceStateTests
{
    [Fact]
    public void Search_FiltersEachReferenceSelectorWithoutChangingSourceRows()
    {
        var copper = new[]
        {
            new CopperReference("c1", "Fictional 7 strand TCW", "Example Metals", 1m, 2m, 3m),
            new CopperReference("c2", "Fictional rope conductor", "Another Supplier", 1m, 2m, 3m),
        };
        var compounds = new[]
        {
            new CompoundReference("p1", "Example PVC", "Polymer One", 1m, 1.4m, "PVC", "Blue grade"),
            new CompoundReference("p2", "Example PE", "Polymer Two", 1m, 0.9m, "PE", "Natural grade"),
        };
        var colours = new[]
        {
            new MasterbatchReference("m1", "Deep Blue", "Colour One", 1m, "PVC", "#123B8A"),
            new MasterbatchReference("m2", "Warm Red", "Colour Two", 1m, "PE", "#C24038"),
        };

        Assert.Equal("c1", Assert.Single(
            DualInsulationWorkspaceState.FilterCopper(copper, "metals")).Id);
        Assert.Equal("p2", Assert.Single(
            DualInsulationWorkspaceState.FilterCompounds(compounds, "natural")).Id);
        Assert.Equal("m1", Assert.Single(
            DualInsulationWorkspaceState.FilterMasterbatches(colours, "dark blue")).ColourCode);
        Assert.Equal(2, copper.Length);
        Assert.Equal(2, compounds.Length);
        Assert.Equal(2, colours.Length);
    }

    [Fact]
    public void OptionalModules_AlwaysReturnInPhysicalInsideToOutsideOrder()
    {
        var ordered = DualInsulationWorkspaceState.OrderModules(
        [
            CableAddOnModule.DrainWire,
            CableAddOnModule.Tape,
            CableAddOnModule.Braid,
        ]);

        Assert.Equal(
            [
                CableAddOnModule.Tape,
                CableAddOnModule.Braid,
                CableAddOnModule.DrainWire,
            ],
            ordered);
    }
}
