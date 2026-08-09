using ATAG.Costing.Domain.Materials;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Materials;

public sealed class SingleCoreWallGuidanceTests
{
    [Fact]
    public void Compare_CalculatesRadialWallAndKeepsSmallCoreAsComparatorOnly()
    {
        var result = SingleCoreWallGuidance.Compare(
            conductorOutsideDiameterMillimetres: 0.58m,
            finishedOutsideDiameterMillimetres: 1.20m,
            nominalAreaSquareMillimetres: 0.21m,
            compoundDescription: "PVC Type 2");

        Assert.Equal(0.31m, result.CalculatedRadialWallMillimetres);
        Assert.Equal(0.60m, result.ReferenceWallMillimetres);
        Assert.Equal(0.50m, result.ReferenceNominalAreaSquareMillimetres);
        Assert.False(result.IsDirectNominalSizeMatch);
        Assert.Contains("nearest comparator", result.Assessment);
    }

    [Fact]
    public void Compare_RecognisesPublishedH05vSizeAndMinimum()
    {
        var result = SingleCoreWallGuidance.Compare(
            conductorOutsideDiameterMillimetres: 0.90m,
            finishedOutsideDiameterMillimetres: 2.20m,
            nominalAreaSquareMillimetres: 0.50m,
            compoundDescription: "PVC");

        Assert.True(result.IsDirectNominalSizeMatch);
        Assert.True(result.MeetsReferenceWall);
        Assert.Equal(
            WallReferenceKind.PublishedMinimum,
            result.ReferenceKind);
    }

    [Fact]
    public void Compare_UsesLszhReferenceWithoutClaimingCertification()
    {
        var result = SingleCoreWallGuidance.Compare(
            conductorOutsideDiameterMillimetres: 1.0m,
            finishedOutsideDiameterMillimetres: 2.6m,
            nominalAreaSquareMillimetres: 1.5m,
            compoundDescription: "LSZH insulation");

        Assert.Equal("LS0H/LSZH", result.MaterialFamily);
        Assert.Equal(
            WallReferenceKind.PublishedNominal,
            result.ReferenceKind);
        Assert.Contains(
            "Confirm the applicable cable standard",
            result.Assessment);
    }
}
