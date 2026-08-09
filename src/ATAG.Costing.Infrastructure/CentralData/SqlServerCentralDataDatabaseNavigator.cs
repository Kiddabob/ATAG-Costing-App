using ATAG.Costing.Application.CentralData;
using Microsoft.Data.SqlClient;

namespace ATAG.Costing.Infrastructure.CentralData;

public sealed class SqlServerCentralDataDatabaseNavigator : ICentralDataDatabaseNavigator
{
    public CentralDataSourceKind Kind => CentralDataSourceKind.SqlDatabase;

    public async Task<IReadOnlyList<CentralDataSourceObject>> DiscoverAsync(
        CentralDataDatabaseConnection connection,
        CancellationToken cancellationToken = default)
    {
        Validate(connection);
        await using var database = CreateConnection(connection);
        await database.OpenAsync(cancellationToken);
        await using var command = database.CreateCommand();
        command.CommandText = """
            SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE IN ('BASE TABLE', 'VIEW')
            ORDER BY TABLE_SCHEMA, TABLE_NAME;
            """;

        var result = new List<CentralDataSourceObject>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            var type = reader.GetString(2);
            result.Add(new CentralDataSourceObject(
                name,
                schema,
                type.Equals("VIEW", StringComparison.OrdinalIgnoreCase)
                    ? CentralDataObjectKind.View
                    : CentralDataObjectKind.Table,
                $"{schema}.{name}"));
        }

        return result;
    }

    public async Task<CentralDataTablePreview> PreviewAsync(
        CentralDataDatabaseConnection connection,
        CentralDataSourceObject sourceObject,
        int rowLimit = 200,
        CancellationToken cancellationToken = default)
    {
        Validate(connection);
        ArgumentNullException.ThrowIfNull(sourceObject);
        rowLimit = rowLimit <= 0 ? 0 : Math.Clamp(rowLimit, 1, 1000);

        await using var database = CreateConnection(connection);
        await database.OpenAsync(cancellationToken);
        await using var command = database.CreateCommand();
        var sourceName = $"{Quote(sourceObject.SchemaName ?? "dbo")}.{Quote(sourceObject.Name)}";
        command.CommandText = rowLimit == 0
            ? $"SELECT * FROM {sourceName};"
            : $"SELECT TOP ({rowLimit}) * FROM {sourceName};";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await RelationalPreviewReader.ReadAsync(
            reader,
            sourceObject,
            rowLimit,
            cancellationToken);
    }

    private static SqlConnection CreateConnection(
        CentralDataDatabaseConnection connection)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = connection.SqlServer,
            InitialCatalog = connection.SqlDatabase,
            IntegratedSecurity = connection.UseWindowsAuthentication,
            ConnectTimeout = 10,
            Encrypt = true,
        };
        if (!connection.UseWindowsAuthentication)
        {
            builder.UserID = connection.SqlUserName;
            builder.Password = connection.SqlPassword;
        }

        return new SqlConnection(builder.ConnectionString);
    }

    private static string Quote(string identifier) =>
        $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static void Validate(CentralDataDatabaseConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.SourceKind != CentralDataSourceKind.SqlDatabase ||
            string.IsNullOrWhiteSpace(connection.SqlServer) ||
            string.IsNullOrWhiteSpace(connection.SqlDatabase))
        {
            throw new ArgumentException("A SQL Server and database are required.", nameof(connection));
        }

        if (!connection.UseWindowsAuthentication &&
            (string.IsNullOrWhiteSpace(connection.SqlUserName) || string.IsNullOrWhiteSpace(connection.SqlPassword)))
        {
            throw new ArgumentException("SQL sign-in requires a user name and password.", nameof(connection));
        }
    }
}
