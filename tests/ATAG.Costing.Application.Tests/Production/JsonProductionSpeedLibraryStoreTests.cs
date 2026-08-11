using ATAG.Costing.Application.Production;
using ATAG.Costing.Infrastructure.Production;
using Xunit;

namespace ATAG.Costing.Application.Tests.Production;

public sealed class JsonProductionSpeedLibraryStoreTests
{
    [Fact]
    public void MissingFile_StartsWithoutPrivateProductionRows()
    {
        var statePath = TemporaryStatePath();

        try
        {
            var state = new JsonProductionSpeedLibraryStore(statePath).Load();

            Assert.Equal(ProductionSpeedLibraryState.CurrentSchemaVersion, state.SchemaVersion);
            Assert.Empty(state.Lines);
            Assert.False(File.Exists(statePath));
        }
        finally
        {
            DeleteTemporaryParent(statePath);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsLinesBandsAndPrivateObservations()
    {
        var statePath = TemporaryStatePath();

        try
        {
            var store = new JsonProductionSpeedLibraryStore(statePath);
            var state = new ProductionSpeedLibraryState
            {
                Lines =
                [
                    new ProductionLineDefinition
                    {
                        Id = "line-1",
                        Name = "Line 1",
                        AboveMaximumLineSpeedMetresPerHour = 650m,
                        SpeedBands =
                        [
                            new ProductionSpeedBandDefinition
                            {
                                Id = "band-1",
                                MaximumFinishedOutsideDiameterMillimetres = 3.5m,
                                LineSpeedMetresPerHour = 1100m,
                            },
                        ],
                        Observations =
                        [
                            new ProductionRunObservation
                            {
                                Id = "run-1",
                                CableReference = "Fictional cable",
                                ProcessName = "Insulation",
                                CoreOutsideDiameterMillimetres = 2.2m,
                                CoreOutsideDiameterToleranceMillimetres = 0.025m,
                                FinishedOutsideDiameterMillimetres = 3.2m,
                                FinishedOutsideDiameterToleranceMillimetres = 0.1m,
                                CapstanSetting = 6.0m,
                                ExtruderSetting = 2.15m,
                                ProducedLengthMetres = 1000m,
                                RunningTimeMinutes = 50m,
                            },
                        ],
                    },
                ],
            };

            store.Save(state);
            var reloaded = store.Load();

            var line = Assert.Single(reloaded.Lines);
            Assert.Equal("Line 1", line.Name);
            Assert.Equal(650m, line.AboveMaximumLineSpeedMetresPerHour);
            Assert.Single(line.SpeedBands);
            var observation = Assert.Single(line.Observations);
            Assert.Equal(6.0m, observation.CapstanSetting);
            Assert.Equal(2.15m, observation.ExtruderSetting);
            Assert.Equal(
                1200m,
                ProductionSpeedEstimator.EffectiveObservationSpeed(observation));
        }
        finally
        {
            DeleteTemporaryParent(statePath);
        }
    }

    private static string TemporaryStatePath() =>
        Path.Combine(
            Path.GetTempPath(),
            "ATAG-Costing-tests",
            Guid.NewGuid().ToString("N"),
            "production-speed-library.json");

    private static void DeleteTemporaryParent(string statePath)
    {
        var directory = Path.GetDirectoryName(statePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
