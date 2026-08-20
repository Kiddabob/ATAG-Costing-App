using ATAG.Costing.Application.Visualisation;
using Xunit;

namespace ATAG.Costing.Application.Tests.Visualisation;

public sealed class BraidPreviewLayoutBuilderTests
{
    [Fact]
    public void SimplePreview_HasAConstantSmallSceneBudget()
    {
        var layout = BraidPreviewLayoutBuilder.Create(
            width: 760d,
            height: 88d,
            physicalPitchMillimetres: 55d,
            coreOutsideDiameterMillimetres: 10d,
            carrierCount: 24,
            endsPerCarrier: 10,
            wireDiameterMillimetres: 0.2d,
            detailed: false);

        Assert.Equal(24, layout.FullCurveCount);
        Assert.InRange(layout.SampleCount, 48, 128);
        Assert.InRange(layout.TotalPointCount, 1, 5_000);
        Assert.NotEmpty(layout.Clockwise.OverpassSegments);
        Assert.Empty(layout.CounterClockwise.OverpassSegments);
        AssertFinite(layout);
    }

    [Fact]
    public void DetailedPreview_IsBoundedAtMaximumSupportedEnds()
    {
        var layout = BraidPreviewLayoutBuilder.Create(
            width: 1_200d,
            height: 120d,
            physicalPitchMillimetres: 9.03d,
            coreOutsideDiameterMillimetres: 2.4d,
            carrierCount: 24,
            endsPerCarrier: 10,
            wireDiameterMillimetres: 0.25d,
            detailed: true);

        Assert.Equal(240, layout.FullCurveCount);
        Assert.InRange(layout.SampleCount, 48, 128);
        Assert.InRange(layout.TotalPointCount, 1, 50_000);
        AssertFinite(layout);
    }

    [Fact]
    public void SameInputs_ProduceTheSameGeometry()
    {
        var first = CreateRepresentativeLayout();
        var second = CreateRepresentativeLayout();

        Assert.Equal(first.VisualPitch, second.VisualPitch);
        Assert.Equal(first.FaceThickness, second.FaceThickness);
        Assert.Equal(first.SampleCount, second.SampleCount);
        Assert.Equal(first.FullCurveCount, second.FullCurveCount);
        Assert.Equal(first.TotalPointCount, second.TotalPointCount);
        Assert.Equal(
            Flatten(first).ToArray(),
            Flatten(second).ToArray());
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(25)]
    public void UnsupportedCarrierCounts_AreRejected(int carrierCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BraidPreviewLayoutBuilder.Create(
                760d,
                88d,
                55d,
                10d,
                carrierCount,
                6,
                0.2d,
                detailed: false));
    }

    private static BraidPreviewLayout CreateRepresentativeLayout() =>
        BraidPreviewLayoutBuilder.Create(
            width: 760d,
            height: 88d,
            physicalPitchMillimetres: 20.84d,
            coreOutsideDiameterMillimetres: 10d,
            carrierCount: 16,
            endsPerCarrier: 6,
            wireDiameterMillimetres: 0.2d,
            detailed: true);

    private static IEnumerable<BraidPreviewPoint> Flatten(
        BraidPreviewLayout layout) =>
        layout.Clockwise.Curves
            .Concat(layout.CounterClockwise.Curves)
            .Concat(layout.Clockwise.OverpassSegments)
            .Concat(layout.CounterClockwise.OverpassSegments)
            .SelectMany(curve => curve.Points);

    private static void AssertFinite(BraidPreviewLayout layout)
    {
        Assert.All(
            Flatten(layout),
            point =>
            {
                Assert.True(double.IsFinite(point.X));
                Assert.True(double.IsFinite(point.Y));
            });
    }
}
