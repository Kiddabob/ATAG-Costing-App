using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Infrastructure.CentralData;
using System.Text.Json;
using Xunit;

namespace ATAG.Costing.Application.Tests.CentralData;

public sealed class CentralDataServiceTests
{
    [Fact]
    public async Task RefreshFailure_RetainsTheLastAvailableSnapshot()
    {
        var store = new InMemoryCentralDataStore(
            TestCentralDataSeed.Create() with
            {
                Configuration = new CentralDataSourceConfiguration(
                    CentralDataSourceKind.LinkedWorkbook,
                    "Unavailable workbook",
                    WorkbookPath: "missing.xlsm"),
            });
        var service = new CentralDataService(
            store,
            [new StubReader(CentralDataReadResult.Failure("Source unavailable."))]);
        var original = store.Load().Snapshot;

        var result = await service.RefreshAsync();

        Assert.False(result.Updated);
        Assert.True(result.UsedRetainedSnapshot);
        Assert.Same(original, result.State.Snapshot);
        Assert.Equal(0, store.SnapshotSaveCount);
        Assert.Contains("retained", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SuccessfulRefresh_CommitsTheReplacementSnapshot()
    {
        var originalState = TestCentralDataSeed.Create();
        var store = new InMemoryCentralDataStore(
            originalState with
            {
                Configuration = new CentralDataSourceConfiguration(
                    CentralDataSourceKind.LinkedWorkbook,
                    "Linked workbook",
                    WorkbookPath: "available.xlsm"),
            });
        var replacement = originalState.Snapshot with
        {
            Revision = "successful-refresh",
            CapturedAt = DateTimeOffset.UtcNow,
        };
        var service = new CentralDataService(
            store,
            [
                new StubReader(
                    CentralDataReadResult.Success(
                        replacement,
                        "Updated.")),
            ]);

        var result = await service.RefreshAsync();

        Assert.True(result.Updated);
        Assert.False(result.UsedRetainedSnapshot);
        Assert.Equal("successful-refresh", result.State.Snapshot.Revision);
        Assert.Equal(1, store.SnapshotSaveCount);
    }

    [Fact]
    public void JsonStore_StartsEmptyAndKeepsItEmptyWhenSetupChanges()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            "ATAG-Costing-tests",
            Guid.NewGuid().ToString("N"));
        var statePath = Path.Combine(testDirectory, "central-data-state.json");

        try
        {
            var store = new JsonCentralDataStore(statePath);
            var initial = store.Load();

            Assert.Equal("unconfigured", initial.Snapshot.Revision);
            Assert.Empty(initial.Snapshot.Copper);
            Assert.Empty(initial.Snapshot.Compounds);
            Assert.Empty(initial.Snapshot.Masterbatches);
            Assert.Empty(initial.Snapshot.EffectiveContacts);
            Assert.Empty(initial.Snapshot.EffectiveOperators);

            store.SaveConfiguration(
                new CentralDataSourceConfiguration(
                    CentralDataSourceKind.SqlDatabase,
                    "SQL database",
                    SqlServer: "ATAG-SQL",
                    SqlDatabase: "CentralData"));
            var reloaded = store.Load();

            Assert.Equal(
                CentralDataSourceKind.SqlDatabase,
                reloaded.Configuration.Kind);
            Assert.Equal(
                initial.Snapshot.Revision,
                reloaded.Snapshot.Revision);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void JsonStore_KeepsIndependentDatabaseTableLinksAndTheSnapshot()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"atag-central-data-links-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var statePath = Path.Combine(temporaryDirectory, "state.json");
            var store = new JsonCentralDataStore(statePath);
            var originalRevision = store.Load().Snapshot.Revision;

            store.SaveTableLink(
                new CentralDataTableLink(
                    CentralDataArea.Copper,
                    CentralDataSourceKind.AccessDatabase,
                    "Copper Access table",
                    "tblCopper",
                    new Dictionary<string, string>
                    {
                        ["Id"] = "CopperId",
                    },
                    AccessDatabasePath: "central.accdb"));
            store.SaveTableLink(
                new CentralDataTableLink(
                    CentralDataArea.Compounds,
                    CentralDataSourceKind.SqlDatabase,
                    "Compound SQL table",
                    "dbo.Compounds",
                    new Dictionary<string, string>
                    {
                        ["Id"] = "CompoundId",
                    },
                    SqlServer: "ATAG-SQL",
                    SqlDatabase: "CentralData"));

            var state = store.Load();

            Assert.Equal(originalRevision, state.Snapshot.Revision);
            Assert.Equal(2, state.EffectiveTableLinks.Count);
            Assert.Contains(
                state.EffectiveTableLinks,
                link => link.Area == CentralDataArea.Copper &&
                        link.TableName == "tblCopper");
            Assert.Contains(
                state.EffectiveTableLinks,
                link => link.Area == CentralDataArea.Compounds &&
                        link.TableName == "dbo.Compounds");
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void JsonStore_AllowsTheFirstValidatedTableOnACleanInstall()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"atag-central-data-first-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var store = new JsonCentralDataStore(
                Path.Combine(temporaryDirectory, "state.json"));
            var clean = store.Load();
            var copper = Assert.Single(
                TestCentralDataSeed.Create().Snapshot.Copper);
            var importedSnapshot = clean.Snapshot with
            {
                Revision = "first-copper-import",
                CapturedAt = DateTimeOffset.UtcNow,
                SourceLabel = "Copper test table",
                Copper = [copper],
            };
            var link = new CentralDataTableLink(
                CentralDataArea.Copper,
                CentralDataSourceKind.AccessDatabase,
                "Copper test table",
                "Copper",
                new Dictionary<string, string>(),
                AccessDatabasePath: "central.accdb");
            var retained = new CentralDataRetainedTable(
                CentralDataArea.Copper,
                link.DisplayName,
                link.TableName,
                null,
                CentralDataObjectKind.Table,
                DateTimeOffset.UtcNow,
                [new CentralDataPreviewColumn("Description", "Text", 0, true)],
                [new CentralDataPreviewRow(
                    1,
                    new Dictionary<string, CentralDataPreviewCell>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Description"] = CentralDataPreviewCell.FromValue(copper.Description),
                    })]);

            store.SaveImportedTable(link, importedSnapshot, retained);

            var reloaded = store.Load();
            Assert.Single(reloaded.Snapshot.Copper);
            Assert.Empty(reloaded.Snapshot.Compounds);
            Assert.Empty(reloaded.Snapshot.Masterbatches);
            Assert.Empty(reloaded.Snapshot.EffectiveContacts);
            Assert.Empty(reloaded.Snapshot.EffectiveOperators);
            Assert.Single(reloaded.EffectiveTableLinks);
            Assert.Single(reloaded.EffectiveRetainedTables);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void JsonStore_RoundTripsTheCompleteRetainedTableAndKeepsItWhenSetupChanges()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"atag-central-data-full-table-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var statePath = Path.Combine(temporaryDirectory, "state.json");
            var store = new JsonCentralDataStore(statePath);
            var seed = TestCentralDataSeed.Create();
            var columns = new[]
            {
                new CentralDataPreviewColumn(
                    "Nom OD (mm)",
                    "Double",
                    0,
                    true,
                    SourceName: "Nominal",
                    Description: "Nom OD (mm)"),
                new CentralDataPreviewColumn("Future source field", "Text", 1, true),
            };
            var retained = new CentralDataRetainedTable(
                CentralDataArea.Copper,
                "Copper · Access",
                "Copper",
                null,
                CentralDataObjectKind.Table,
                DateTimeOffset.UtcNow,
                columns,
                [new CentralDataPreviewRow(
                    1,
                    new Dictionary<string, CentralDataPreviewCell>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Nom OD (mm)"] = CentralDataPreviewCell.FromValue("0.58"),
                        ["Future source field"] = CentralDataPreviewCell.FromValue("retained"),
                    })]);
            var link = new CentralDataTableLink(
                CentralDataArea.Copper,
                CentralDataSourceKind.AccessDatabase,
                "Copper · Access",
                "Copper",
                new Dictionary<string, string>(),
                AccessDatabasePath: "central.accdb");

            store.SaveImportedTable(link, seed.Snapshot, retained);
            store.SaveConfiguration(new CentralDataSourceConfiguration(
                CentralDataSourceKind.SqlDatabase,
                "Later setup",
                SqlServer: "server",
                SqlDatabase: "database"));

            var reloaded = new JsonCentralDataStore(statePath).Load();
            var reloadedTable = Assert.Single(reloaded.EffectiveRetainedTables);
            Assert.Equal(2, reloadedTable.Columns.Count);
            Assert.Equal("Nominal", reloadedTable.Columns[0].EffectiveSourceName);
            Assert.Equal(
                "retained",
                Assert.Single(reloadedTable.Rows).Cell("Future source field").Value);
            Assert.Single(reloaded.EffectiveTableLinks);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void JsonStore_RemoveTableLink_KeepsRetainedTableAndValidatedSnapshot()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"atag-central-data-remove-link-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var store = new JsonCentralDataStore(Path.Combine(temporaryDirectory, "state.json"));
            var seed = TestCentralDataSeed.Create();
            var retained = new CentralDataRetainedTable(
                CentralDataArea.Copper,
                "Copper · Access",
                "Copper",
                null,
                CentralDataObjectKind.Table,
                DateTimeOffset.UtcNow,
                [new CentralDataPreviewColumn("Description", "Text", 0, true)],
                [new CentralDataPreviewRow(
                    1,
                    new Dictionary<string, CentralDataPreviewCell>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Description"] = CentralDataPreviewCell.FromValue("7/0.20 TCW"),
                    })]);
            var link = new CentralDataTableLink(
                CentralDataArea.Copper,
                CentralDataSourceKind.AccessDatabase,
                "Copper · Access",
                "Copper",
                new Dictionary<string, string>(),
                AccessDatabasePath: "central.accdb");
            store.SaveImportedTable(link, seed.Snapshot, retained);

            store.RemoveTableLink(CentralDataArea.Copper);

            var reloaded = store.Load();
            Assert.Empty(reloaded.EffectiveTableLinks);
            Assert.Single(reloaded.EffectiveRetainedTables);
            Assert.Equal(seed.Snapshot.Revision, reloaded.Snapshot.Revision);
            Assert.Equal(
                "7/0.20 TCW",
                reloaded.EffectiveRetainedTables[0].Rows[0].Cell("Description").Value);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void JsonStore_UpgradesTheOldThreeTableCacheWithoutLosingLinks()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"atag-central-data-upgrade-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var statePath = Path.Combine(temporaryDirectory, "state.json");
            var seed = TestCentralDataSeed.Create();
            var legacy = seed with
            {
                Snapshot = seed.Snapshot with
                {
                    SchemaVersion = 1,
                    Contacts = null,
                    Operators = null,
                },
                TableLinks =
                [
                    new CentralDataTableLink(
                        CentralDataArea.Copper,
                        CentralDataSourceKind.AccessDatabase,
                        "Copper link",
                        "tblCopper",
                        new Dictionary<string, string>
                        {
                            ["Id"] = "ID",
                        },
                        AccessDatabasePath: "central.accdb"),
                ],
            };
            File.WriteAllText(
                statePath,
                JsonSerializer.Serialize(legacy));

            var upgraded = new JsonCentralDataStore(statePath).Load();

            Assert.Equal(2, upgraded.Snapshot.SchemaVersion);
            Assert.Empty(upgraded.Snapshot.EffectiveContacts);
            Assert.Empty(upgraded.Snapshot.EffectiveOperators);
            Assert.Single(upgraded.EffectiveTableLinks);
            Assert.Equal(
                "tblCopper",
                upgraded.EffectiveTableLinks[0].TableName);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task LinkedWorkbookReader_ImportsTheReferenceTablesWhenPresent()
    {
        var workbookPath = FindReferenceWorkbook();
        if (workbookPath is null)
        {
            return;
        }

        var reader = new OpenXmlWorkbookCentralDataSourceReader();
        var result = await reader.ReadAsync(
            new CentralDataSourceConfiguration(
                CentralDataSourceKind.LinkedWorkbook,
                "Reference workbook",
                WorkbookPath: workbookPath));

        Assert.True(result.Succeeded, result.Message);
        Assert.NotNull(result.Snapshot);
        Assert.True(result.Snapshot.Copper.Count > 100);
        Assert.True(result.Snapshot.Compounds.Count > 30);
        Assert.True(result.Snapshot.Masterbatches.Count > 100);
        Assert.True(result.Snapshot.EffectiveContacts.Count > 500);
        Assert.Equal(5, result.Snapshot.EffectiveOperators.Count);
        Assert.Contains(
            result.Snapshot.Copper,
            item =>
                item.Id == "860" &&
                item.Construction?.NormalizedConstruction == "7/0.20");
        Assert.Contains(
            result.Snapshot.Compounds,
            item => item.CompoundName == "FC1530CSI (XN78927)");
        Assert.Contains(
            result.Snapshot.Masterbatches,
            item => item.ColourCode == "CUS3872");
    }

    private static string? FindReferenceWorkbook()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "(WIP Mitchell) Costing Sheet.xlsm");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private sealed class StubReader(
        CentralDataReadResult result) : ICentralDataSourceReader
    {
        public CentralDataSourceKind Kind =>
            CentralDataSourceKind.LinkedWorkbook;

        public Task<CentralDataReadResult> ReadAsync(
            CentralDataSourceConfiguration configuration,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    private sealed class InMemoryCentralDataStore(
        CentralDataState state) : ICentralDataStore
    {
        private CentralDataState _state = state;

        public int SnapshotSaveCount { get; private set; }

        public CentralDataState Load() => _state;

        public void SaveConfiguration(
            CentralDataSourceConfiguration configuration)
        {
            _state = _state with { Configuration = configuration };
        }

        public void SaveTableLink(CentralDataTableLink link)
        {
            var links = _state.EffectiveTableLinks
                .Where(existing => existing.Area != link.Area)
                .Append(link)
                .ToArray();
            _state = _state with { TableLinks = links };
        }

        public void RemoveTableLink(CentralDataArea area)
        {
            _state = _state with
            {
                TableLinks = _state.EffectiveTableLinks
                    .Where(existing => existing.Area != area)
                    .ToArray(),
            };
        }

        public void SaveSnapshot(CentralDataSnapshot snapshot)
        {
            SnapshotSaveCount++;
            _state = _state with { Snapshot = snapshot };
        }

        public void SaveImportedTable(
            CentralDataTableLink link,
            CentralDataSnapshot snapshot,
            CentralDataRetainedTable retainedTable)
        {
            SaveTableLink(link);
            SaveSnapshot(snapshot);
            _state = _state with
            {
                RetainedTables = _state.EffectiveRetainedTables
                    .Where(existing => existing.Area != retainedTable.Area)
                    .Append(retainedTable)
                    .ToArray(),
            };
        }
    }
}
