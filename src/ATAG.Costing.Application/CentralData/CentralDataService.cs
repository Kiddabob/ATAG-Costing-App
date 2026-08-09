namespace ATAG.Costing.Application.CentralData;

/// <summary>
/// Owns central-data setup and refresh orchestration. A replacement snapshot is
/// committed only after a reader completes successfully; a broken or missing
/// link therefore never removes the last available material data.
/// </summary>
public sealed class CentralDataService
{
    private readonly ICentralDataStore _store;
    private readonly IReadOnlyDictionary<CentralDataSourceKind, ICentralDataSourceReader> _readers;
    private readonly IReadOnlyDictionary<CentralDataSourceKind, ICentralDataDatabaseNavigator> _databaseNavigators;

    public CentralDataService(
        ICentralDataStore store,
        IEnumerable<ICentralDataSourceReader> readers,
        IEnumerable<ICentralDataDatabaseNavigator>? databaseNavigators = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentNullException.ThrowIfNull(readers);

        _readers = readers.ToDictionary(reader => reader.Kind);
        _databaseNavigators = (databaseNavigators ?? [])
            .ToDictionary(navigator => navigator.Kind);
    }

    public CentralDataState Load() =>
        CentralDataDerivedProjection.Complete(_store.Load());

    public CentralDataState SaveConfiguration(
        CentralDataSourceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _store.SaveConfiguration(configuration);
        return Load();
    }

    public CentralDataState SaveTableLink(CentralDataTableLink link)
    {
        ArgumentNullException.ThrowIfNull(link);

        if (link.SourceKind is not
            (CentralDataSourceKind.AccessDatabase or
             CentralDataSourceKind.SqlDatabase))
        {
            throw new ArgumentException(
                "A table link must use Microsoft Access or SQL Server.",
                nameof(link));
        }

        if (string.IsNullOrWhiteSpace(link.TableName))
        {
            throw new ArgumentException(
                "A database table name is required.",
                nameof(link));
        }

        _store.SaveTableLink(link);
        return Load();
    }

    public CentralDataState RemoveTableLink(CentralDataArea area)
    {
        _store.RemoveTableLink(area);
        return Load();
    }

    public CentralDataTableImportResult ImportTable(
        CentralDataTableLink link,
        CentralDataTablePreview preview)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(preview);

        var current = Load();
        var steps = link.EffectiveQuerySteps.Count > 0
            ? link.EffectiveQuerySteps
            :
            [
                new CentralDataQueryStep(
                    CentralDataQueryStepKind.Source,
                    "Source",
                    link.DisplayName),
                new CentralDataQueryStep(
                    CentralDataQueryStepKind.Navigation,
                    "Navigation",
                    link.TableName),
                new CentralDataQueryStep(
                    CentralDataQueryStepKind.ReplaceDivisionByZeroWithNull,
                    "Ignored division-by-zero cells",
                    "#DIV/0! values are imported as blank cells."),
            ];
        var result = CentralDataTableImporter.Import(
            current.Snapshot,
            link.Area,
            preview,
            link.ColumnMappings,
            steps,
            link.DisplayName);
        if (!result.Succeeded || result.Snapshot is null || result.RetainedTable is null)
        {
            return result;
        }

        _store.SaveImportedTable(link, result.Snapshot, result.RetainedTable);
        return result;
    }

    public async Task<CentralDataRefreshResult> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        var current = Load();

        if (current.EffectiveTableLinks.Count > 0)
        {
            return await RefreshDatabaseTablesAsync(current, cancellationToken);
        }

        if (current.Configuration.Kind == CentralDataSourceKind.EmbeddedSnapshot)
        {
            return new CentralDataRefreshResult(
                current,
                Updated: false,
                UsedRetainedSnapshot: true,
                "No live source is configured. Import the required Access or SQL tables; any previously retained local tables remain available offline.");
        }

        if (!_readers.TryGetValue(current.Configuration.Kind, out var reader))
        {
            return new CentralDataRefreshResult(
                current,
                Updated: false,
                UsedRetainedSnapshot: true,
                $"No {current.Configuration.Kind} reader is available. The last successful snapshot was retained.");
        }

        CentralDataReadResult readResult;

        try
        {
            readResult = await reader.ReadAsync(
                current.Configuration,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            readResult = CentralDataReadResult.Failure(
                $"Central data could not be refreshed: {exception.Message}");
        }

        if (!readResult.Succeeded || readResult.Snapshot is null)
        {
            return new CentralDataRefreshResult(
                current,
                Updated: false,
                UsedRetainedSnapshot: true,
                $"{readResult.Message} The last successful snapshot was retained.");
        }

        _store.SaveSnapshot(readResult.Snapshot);
        var updated = Load();

        return new CentralDataRefreshResult(
            updated,
            Updated: true,
            UsedRetainedSnapshot: false,
            readResult.Message);
    }

    private async Task<CentralDataRefreshResult> RefreshDatabaseTablesAsync(
        CentralDataState original,
        CancellationToken cancellationToken)
    {
        var updatedAreas = new List<string>();
        var retainedAreas = new List<string>();
        var areaResults = new List<CentralDataAreaRefreshResult>();

        foreach (var link in original.EffectiveTableLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_databaseNavigators.TryGetValue(link.SourceKind, out var navigator))
            {
                retainedAreas.Add($"{link.Area}: provider unavailable");
                areaResults.Add(new CentralDataAreaRefreshResult(
                    link.Area,
                    Updated: false,
                    UsedRetainedSnapshot: true,
                    "Provider unavailable."));
                continue;
            }

            if (link.SourceKind == CentralDataSourceKind.SqlDatabase &&
                !link.UseWindowsAuthentication)
            {
                retainedAreas.Add($"{link.Area}: SQL sign-in must be re-entered from Set up data link");
                areaResults.Add(new CentralDataAreaRefreshResult(
                    link.Area,
                    Updated: false,
                    UsedRetainedSnapshot: true,
                    "SQL sign-in must be re-entered from Set up data link."));
                continue;
            }

            try
            {
                var connection = new CentralDataDatabaseConnection(
                    link.SourceKind,
                    link.DisplayName,
                    link.AccessDatabasePath,
                    link.SqlServer,
                    link.SqlDatabase,
                    link.UseWindowsAuthentication);
                var objectName = link.TableName;
                if (!string.IsNullOrWhiteSpace(link.SchemaName) &&
                    objectName.StartsWith(link.SchemaName + ".", StringComparison.OrdinalIgnoreCase))
                {
                    objectName = objectName[(link.SchemaName.Length + 1)..];
                }
                var sourceObject = new CentralDataSourceObject(
                    objectName,
                    link.SchemaName,
                    link.ObjectKind,
                    link.TableName);
                var table = await navigator.PreviewAsync(
                    connection,
                    sourceObject,
                    rowLimit: 0,
                    cancellationToken: cancellationToken);
                var import = ImportTable(link, table);
                if (import.Succeeded)
                {
                    updatedAreas.Add($"{link.Area} ({import.ImportedRows:N0} rows)");
                    areaResults.Add(new CentralDataAreaRefreshResult(
                        link.Area,
                        Updated: true,
                        UsedRetainedSnapshot: false,
                        $"Refreshed {import.ImportedRows:N0} rows."));
                }
                else
                {
                    retainedAreas.Add($"{link.Area}: {import.Message}");
                    areaResults.Add(new CentralDataAreaRefreshResult(
                        link.Area,
                        Updated: false,
                        UsedRetainedSnapshot: true,
                        import.Message));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                retainedAreas.Add($"{link.Area}: {exception.Message}");
                areaResults.Add(new CentralDataAreaRefreshResult(
                    link.Area,
                    Updated: false,
                    UsedRetainedSnapshot: true,
                    exception.Message));
            }
        }

        var state = Load();
        var updated = updatedAreas.Count > 0;
        var message = updated
            ? $"Updated {string.Join(", ", updatedAreas)}."
            : "No linked table could be updated.";
        if (retainedAreas.Count > 0)
        {
            message += $" Retained last successful data for {string.Join("; ", retainedAreas)}.";
        }

        return new CentralDataRefreshResult(
            state,
            Updated: updated,
            UsedRetainedSnapshot: retainedAreas.Count > 0 || !updated,
            message,
            areaResults);
    }
}
