using ATAG.Costing.Application.CentralData;

namespace ATAG.Costing.Infrastructure.CentralData;

/// <summary>
/// Compatibility reader for the legacy whole-snapshot SQL configuration.
/// Current runtime SQL imports use per-area table links through
/// SqlServerCentralDataDatabaseNavigator. Failure here remains safe because the
/// last successful snapshot is retained.
/// </summary>
public sealed class SqlCentralDataSourceReader : ICentralDataSourceReader
{
    public CentralDataSourceKind Kind => CentralDataSourceKind.SqlDatabase;

    public Task<CentralDataReadResult> ReadAsync(
        CentralDataSourceConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var setupComplete =
            !string.IsNullOrWhiteSpace(configuration.SqlServer) &&
            !string.IsNullOrWhiteSpace(configuration.SqlDatabase);
        var message = setupComplete
            ? "This legacy SQL endpoint has no per-area Navigator query. Use Set up data link to choose and preview each required table."
            : "Complete the SQL server and database steps before attempting an update.";

        return Task.FromResult(CentralDataReadResult.Failure(message));
    }
}
