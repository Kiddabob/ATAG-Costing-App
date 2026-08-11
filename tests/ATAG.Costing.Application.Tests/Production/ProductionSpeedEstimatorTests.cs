using ATAG.Costing.Application.Production;
using Xunit;

namespace ATAG.Costing.Application.Tests.Production;

public sealed class ProductionSpeedEstimatorTests
{
    [Fact]
    public void ExplicitGeneralStarterProfile_KeepsTheAcceptedNewerWorkbookInsulationBands()
    {
        var line = ProductionSpeedLibraryDefaults.CreateGeneralInsulationStarterLine(
            "test-line");

        Assert.Equal("General insulation starter profile", line.Name);
        Assert.Collection(
            line.SpeedBands,
            band => AssertBand(band, 1.00m, 15000m),
            band => AssertBand(band, 1.20m, 13000m),
            band => AssertBand(band, 2.00m, 8000m),
            band => AssertBand(band, 2.50m, 6000m));
        Assert.Equal(700m, line.AboveMaximumLineSpeedMetresPerHour);
    }

    [Fact]
    public void NoMeasuredRunAndNoOdBands_RefusesToInventASpeed()
    {
        var line = new ProductionLineDefinition
        {
            Id = "empty-line",
            Name = "Empty line",
            AboveMaximumLineSpeedMetresPerHour = 700m,
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionSpeedEstimator.Estimate(
                line,
                new ProductionSpeedEstimateRequest(
                    "Insulation",
                    2.2m,
                    3.2m,
                    1000m)));

        Assert.Contains("no close measured run or usable OD speed bands", exception.Message);
    }

    [Fact]
    public void ExactKnownCableRun_UsesMeasuredSpeedAndCalculatesRuntime()
    {
        var line = Line(
            new ProductionRunObservation
            {
                Id = "example-run",
                CableReference = "Example 3.20 mm cable",
                ProcessName = "Insulation",
                CoreOutsideDiameterMillimetres = 2.2m,
                CoreOutsideDiameterToleranceMillimetres = 0.025m,
                FinishedOutsideDiameterMillimetres = 3.2m,
                FinishedOutsideDiameterToleranceMillimetres = 0.1m,
                CapstanSetting = 6.0m,
                ExtruderSetting = 2.15m,
                MeasuredLineSpeedMetresPerHour = 1200m,
            });

        var result = ProductionSpeedEstimator.Estimate(
            line,
            new ProductionSpeedEstimateRequest(
                "Insulation",
                2.2m,
                3.2m,
                6000m,
                6.0m,
                2.15m));

        Assert.Equal(1200m, result.RecommendedLineSpeedMetresPerHour);
        Assert.Equal(5m, result.RunningTimeHours);
        Assert.Equal("Known cable runs", result.Source);
        Assert.Equal("Medium", result.Confidence);
        var evidence = Assert.Single(result.Evidence);
        Assert.Equal("Example 3.20 mm cable", evidence.CableReference);
        Assert.Equal(0m, evidence.SimilarityScore);
    }

    [Fact]
    public void ProducedLengthAndMinutes_DeriveObservedSpeed()
    {
        var observation = new ProductionRunObservation
        {
            ProducedLengthMetres = 1000m,
            RunningTimeMinutes = 30m,
        };

        var speed = ProductionSpeedEstimator.EffectiveObservationSpeed(observation);

        Assert.Equal(2000m, speed);
    }

    [Fact]
    public void SettingsOnlyObservation_DoesNotPretendToBeLineSpeed()
    {
        var line = Line(
            new ProductionRunObservation
            {
                Id = "settings-only",
                CableReference = "Settings only",
                ProcessName = "Insulation",
                CoreOutsideDiameterMillimetres = 2.2m,
                CoreOutsideDiameterToleranceMillimetres = 0.025m,
                FinishedOutsideDiameterMillimetres = 3.2m,
                FinishedOutsideDiameterToleranceMillimetres = 0.1m,
                CapstanSetting = 6.0m,
                ExtruderSetting = 2.15m,
            });

        var result = ProductionSpeedEstimator.Estimate(
            line,
            new ProductionSpeedEstimateRequest(
                "Insulation",
                2.2m,
                3.2m,
                3500m,
                6.0m,
                2.15m));

        Assert.Equal(700m, result.RecommendedLineSpeedMetresPerHour);
        Assert.Equal(5m, result.RunningTimeHours);
        Assert.Equal("OD speed table", result.Source);
        Assert.Empty(result.Evidence);
    }

    [Fact]
    public void DistantMeasuredRun_FallsBackToTheOdTable()
    {
        var line = Line(
            new ProductionRunObservation
            {
                Id = "distant-run",
                CableReference = "Small cable",
                ProcessName = "Insulation",
                CoreOutsideDiameterMillimetres = 0.5m,
                CoreOutsideDiameterToleranceMillimetres = 0.025m,
                FinishedOutsideDiameterMillimetres = 0.8m,
                FinishedOutsideDiameterToleranceMillimetres = 0.05m,
                MeasuredLineSpeedMetresPerHour = 14000m,
            });

        var result = ProductionSpeedEstimator.Estimate(
            line,
            new ProductionSpeedEstimateRequest(
                "Insulation",
                2.2m,
                3.2m,
                700m));

        Assert.Equal(700m, result.RecommendedLineSpeedMetresPerHour);
        Assert.Equal(1m, result.RunningTimeHours);
        Assert.Equal("OD speed table", result.Source);
    }

    private static ProductionLineDefinition Line(
        params ProductionRunObservation[] observations) =>
        ProductionSpeedLibraryDefaults.CreateGeneralInsulationStarterLine(
            "test-line") with
        {
            Observations = observations,
        };

    private static void AssertBand(
        ProductionSpeedBandDefinition band,
        decimal diameter,
        decimal speed)
    {
        Assert.Equal(diameter, band.MaximumFinishedOutsideDiameterMillimetres);
        Assert.Equal(speed, band.LineSpeedMetresPerHour);
    }
}
