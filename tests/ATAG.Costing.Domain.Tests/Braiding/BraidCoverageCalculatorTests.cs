using ATAG.Costing.Domain.Braiding;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Braiding;

public sealed class BraidCoverageCalculatorTests
{
    [Fact]
    public void Calculate_ReproducesWorkbookReferenceCase()
    {
        var result = BraidCoverageCalculator.Calculate(ReferenceInputs());

        Assert.Equal(10d, result.MeanOutsideDiameterMillimetres, 10);
        Assert.Equal(0.5527864045000421d, result.TargetFillFraction, 12);
        Assert.Equal(96, result.SixteenCarrier.TotalBraidStrands);
        Assert.Equal(144, result.TwentyFourCarrier.TotalBraidStrands);
        Assert.Equal(20.840275166895882d, result.SixteenCarrier.RecommendedPitchMillimetres, 10);
        Assert.Equal(46.60384472444385d, result.TwentyFourCarrier.RecommendedPitchMillimetres, 10);
        Assert.Equal(56.441034916338225d, result.SixteenCarrier.LongitudinalAngleDegrees, 10);
        Assert.Equal(60.89903159476365d, result.SixteenCarrier.PerpendicularAngleDegrees, 10);
        Assert.Equal(0.5799850064910369d, result.SixteenCarrier.CoverageAtReferencePitchFraction, 10);
        Assert.Equal(1.8089892785225496d, result.SixteenCarrier.StrandLengthPerBobbinMetres, 10);
        Assert.Equal(16, result.Steps.Count);
    }

    [Fact]
    public void ReferenceTables_ContainWorkbookRowsAndExpandedEndsList()
    {
        Assert.Equal(45, BraidReferenceTables.CoreLayouts.Count);
        Assert.Equal(
            new BraidCoreLayout(45, "2-8-14-21", 8d),
            BraidReferenceTables.CoreLayouts[^1]);
        Assert.Equal(Enumerable.Range(1, 10), BraidReferenceTables.EndsPerCarrierOptions);
        Assert.Contains(0.15d, BraidReferenceTables.EffectiveWireDiameterOptionsMillimetres);
        Assert.Equal(18, BraidReferenceTables.BuncherLaySettings.Count);
        Assert.Contains(
            new BuncherLaySetting(19.43d, 35, 50, "Small"),
            BraidReferenceTables.BuncherLaySettings);
        Assert.Contains(
            new BuncherLaySetting(120d, 57, 20, "Large"),
            BraidReferenceTables.BuncherLaySettings);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    public void Calculate_RejectsInvalidCoverage(double coverage)
    {
        var inputs = ReferenceInputs() with
        {
            TargetCoverageFraction = coverage,
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => BraidCoverageCalculator.Calculate(inputs));
    }

    [Fact]
    public void Calculate_RejectsValuesOutsideExpandedEndsList()
    {
        var inputs = ReferenceInputs() with
        {
            EndsPerCarrier = 11,
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => BraidCoverageCalculator.Calculate(inputs));
    }

    [Fact]
    public void Calculate_AcceptsRetainedWireDiameterUpToQuarterMillimetre()
    {
        var result = BraidCoverageCalculator.Calculate(
            ReferenceInputs() with
            {
                EffectiveWireDiameterMillimetres = 0.15d,
            });

        Assert.Equal(0.15d, result.SixteenCarrier.BaseFillFraction *
            (2d * Math.PI * result.MeanOutsideDiameterMillimetres) /
            result.SixteenCarrier.TotalBraidStrands, 10);
    }

    [Fact]
    public void Recommendation_UsesTargetResultAndKeepsReferencePitchIndependent()
    {
        var result = BraidCoverageCalculator.Calculate(ReferenceInputs());

        var recommendation = BraidCarrierRecommender.Select(result, 0.8d);

        Assert.Equal(16, recommendation.CarrierCount);
        Assert.Equal(0.8d, recommendation.SixteenCarrierCoverageFraction, 10);
        Assert.Equal(0.8d, recommendation.TwentyFourCarrierCoverageFraction, 10);
        Assert.Contains("tie-break", recommendation.Reason);
        Assert.NotEqual(
            result.SixteenCarrier.CoverageAtReferencePitchFraction,
            recommendation.SixteenCarrierCoverageFraction);
    }

    private static BraidCoverageInputs ReferenceInputs() =>
        new(
            TargetCoverageFraction: 0.8d,
            CoreOutsideDiameterMillimetres: 10d,
            CoreCount: 1,
            EndsPerCarrier: 6,
            EffectiveWireDiameterMillimetres: 0.2d,
            CableLengthMetres: 1d);
}
