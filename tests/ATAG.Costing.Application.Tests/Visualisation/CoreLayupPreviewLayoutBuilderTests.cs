using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.Domain.Braiding;
using Xunit;

namespace ATAG.Costing.Application.Tests.Visualisation;

public sealed class CoreLayupPreviewLayoutBuilderTests
{
    [Fact]
    public void SixCoreGroup_IsOneEqualCoreInsideFiveEqualOuterCores()
    {
        var layout = CoreLayupPreviewLayoutBuilder.Create(
            BraidReferenceTables.CoreLayoutFor(6),
            width: 320d,
            height: 260d);

        Assert.Equal([1, 5], layout.LayerCounts);
        Assert.Equal(6, layout.CoreCount);
        Assert.Single(layout.Cores, core => core.IsCentral);
        Assert.Equal(5, layout.Cores.Count(core => core.LayerIndex == 1));
        Assert.Single(layout.Cores.Select(core => core.Radius).Distinct());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(19)]
    [InlineData(45)]
    public void RetainedGroup_ProducesExactlyTheRequestedCoreCount(int coreCount)
    {
        var layout = CoreLayupPreviewLayoutBuilder.Create(
            BraidReferenceTables.CoreLayoutFor(coreCount),
            width: 400d,
            height: 300d);

        Assert.Equal(coreCount, layout.CoreCount);
        Assert.All(layout.Cores, core => Assert.True(core.Radius > 0d));
    }
}
