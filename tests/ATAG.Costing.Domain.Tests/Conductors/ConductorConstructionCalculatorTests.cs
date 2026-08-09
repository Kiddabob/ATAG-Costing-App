using ATAG.Costing.Domain.Conductors;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Conductors;

public sealed class ConductorConstructionCalculatorTests
{
    [Fact]
    public void SimpleConstruction_NormalizesDisplayWithoutChangingExactArea()
    {
        var result = ConductorConstructionCalculator.TryCalculate(
            "7/0.196 TCW (H) (0.21mm²)",
            0.21m);

        Assert.NotNull(result);
        Assert.Equal("7/0.20", result.NormalizedConstruction);
        Assert.Equal([7], result.PackingLevels);
        Assert.Equal(7, result.TotalStrandCount);
        Assert.Equal(0.211203m, result.CalculatedMetalAreaSquareMillimetres, 6);
        Assert.Equal("24", result.NearestAwg);
        Assert.Equal(ConductorClass.Class2Stranded, result.ConductorClass);
        Assert.False(result.RequiresAreaReview);
    }

    [Fact]
    public void RopeConstruction_MultipliesGroupsByStrandsPerGroup()
    {
        var result = ConductorConstructionCalculator.TryCalculate(
            "7x19/0.32 H72 Strand (25mm²)",
            25m);

        Assert.NotNull(result);
        Assert.True(result.IsRopeLay);
        Assert.Equal([7, 19], result.PackingLevels);
        Assert.Equal(7, result.GroupCount);
        Assert.Equal(19, result.StrandsPerGroup);
        Assert.Equal(133, result.TotalStrandCount);
        Assert.Equal(10.696495m, result.CalculatedMetalAreaSquareMillimetres, 6);
        Assert.Equal("7", result.NearestAwg);
        Assert.True(result.RequiresAreaReview);
        Assert.Contains(
            "Review required",
            result.AreaVerificationMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OneHundredAndThirtyThreeByPointThreeTwo_IsNotSilentlyRelabelled()
    {
        var result = ConductorConstructionCalculator.TryCalculate(
            "133/0.32 H72 Strand (11mm²)",
            11m);

        Assert.NotNull(result);
        Assert.True(result.IsRopeLay);
        Assert.True(result.WasRopeLayInferred);
        Assert.Equal(7, result.GroupCount);
        Assert.Equal(19, result.StrandsPerGroup);
        Assert.Equal(10.696495m, result.CalculatedMetalAreaSquareMillimetres, 6);
        Assert.Equal(11m, result.NominalAreaSquareMillimetres);
        Assert.False(result.RequiresAreaReview);
    }

    [Fact]
    public void CountTimesDiameter_ParsesBobbinsAsACompactStrandSet()
    {
        var result = ConductorConstructionCalculator.TryCalculate(
            "4 x 0.1 TCW (Bobbins) (0.03mm2)",
            0.03m);

        Assert.NotNull(result);
        Assert.False(result.IsRopeLay);
        Assert.Equal([4], result.PackingLevels);
        Assert.Equal(4, result.TotalStrandCount);
        Assert.Equal(0.1m, result.StrandDiameterMillimetres);
    }

    [Fact]
    public void MultiLevelRope_WithTrailingDiameter_PreservesEveryLevel()
    {
        var result = ConductorConstructionCalculator.TryCalculate(
            "183 x 7 0.10 TCW (10mm2)",
            10m);

        Assert.NotNull(result);
        Assert.True(result.IsRopeLay);
        Assert.False(result.WasStrandDiameterInferred);
        Assert.Equal([183, 7], result.PackingLevels);
        Assert.Equal(1_281, result.TotalStrandCount);
        Assert.Equal(0.1m, result.StrandDiameterMillimetres);
    }

    [Theory]
    [InlineData("130 x 7 x 7 TCW (50mm2)", 50, 6_370)]
    [InlineData("104 x 3 x 7 x 7 TCW (120mm2)", 120, 15_288)]
    [InlineData("97 x 3 x 7 TCW (16mm2)", 16, 2_037)]
    public void MultiLevelRope_InfersMissingDiameterFromStoredNominalArea(
        string description,
        int nominalArea,
        int expectedStrands)
    {
        var result = ConductorConstructionCalculator.TryCalculate(
            description,
            nominalArea);

        Assert.NotNull(result);
        Assert.True(result.IsRopeLay);
        Assert.True(result.WasStrandDiameterInferred);
        Assert.Equal(expectedStrands, result.TotalStrandCount);
        Assert.Equal(0.1m, result.StrandDiameterMillimetres, 3);
        Assert.Contains(
            "inferred",
            result.AreaVerificationMessage,
            StringComparison.OrdinalIgnoreCase);
    }
}
