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

    [Fact]
    public void SeparateClients_AddingDifferentLines_PreserveBothChanges()
    {
        var statePath = TemporaryStatePath();

        try
        {
            var firstClient = new JsonProductionSpeedLibraryStore(statePath);
            var secondClient = new JsonProductionSpeedLibraryStore(statePath);
            firstClient.Load();
            secondClient.Load();

            firstClient.Save(new ProductionSpeedLibraryState
            {
                Lines = [Line("line-1", "Line 1")],
            });
            secondClient.Save(new ProductionSpeedLibraryState
            {
                Lines = [Line("line-2", "Line 2")],
            });

            var reloaded = new JsonProductionSpeedLibraryStore(statePath).Load();
            Assert.Equal(2, reloaded.Lines.Count);
            Assert.Contains(reloaded.Lines, line => line.Id == "line-1");
            Assert.Contains(reloaded.Lines, line => line.Id == "line-2");
        }
        finally
        {
            DeleteTemporaryParent(statePath);
        }
    }

    private static ProductionLineDefinition Line(string id, string name) =>
        new()
        {
            Id = id,
            Name = name,
        };

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
