using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Runtime.Versioning;
using ATAG.Costing.Application.CentralData;

namespace ATAG.Costing.Infrastructure.CentralData;

[SupportedOSPlatform("windows")]
public sealed class AccessCentralDataDatabaseNavigator : ICentralDataDatabaseNavigator
{
    public CentralDataSourceKind Kind => CentralDataSourceKind.AccessDatabase;

    public async Task<IReadOnlyList<CentralDataSourceObject>> DiscoverAsync(
        CentralDataDatabaseConnection connection,
        CancellationToken cancellationToken = default)
    {
        Validate(connection);
        await using var database = await OpenAsync(connection, cancellationToken);
        var schema = database.GetOleDbSchemaTable(
            OleDbSchemaGuid.Tables,
            [null, null, null, null]);
        if (schema is null)
        {
            return [];
        }

        return schema.Rows
            .Cast<DataRow>()
            .Select(
                row => new
                {
                    Name = Convert.ToString(row["TABLE_NAME"]) ?? string.Empty,
                    Type = Convert.ToString(row["TABLE_TYPE"]) ?? string.Empty,
                })
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Name) &&
                !item.Name.StartsWith("MSys", StringComparison.OrdinalIgnoreCase) &&
                item.Type is "TABLE" or "VIEW")
            .Select(item => new CentralDataSourceObject(
                item.Name,
                SchemaName: null,
                item.Type == "VIEW" ? CentralDataObjectKind.View : CentralDataObjectKind.Table,
                item.Name))
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
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

        await using var database = await OpenAsync(connection, cancellationToken);
        var columnMetadata = ReadColumnMetadata(database, sourceObject.Name);
        await using var command = database.CreateCommand();
        command.CommandText = rowLimit == 0
            ? $"SELECT * FROM {Quote(sourceObject.Name)}"
            : $"SELECT TOP {rowLimit} * FROM {Quote(sourceObject.Name)}";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await RelationalPreviewReader.ReadAsync(
            reader,
            sourceObject,
            rowLimit,
            cancellationToken,
            columnMetadata);
    }

    private static IReadOnlyDictionary<string, CentralDataSourceColumnMetadata> ReadColumnMetadata(
        OleDbConnection database,
        string tableName)
    {
        try
        {
            var schema = database.GetOleDbSchemaTable(
                OleDbSchemaGuid.Columns,
                [null, null, tableName, null]);
            if (schema is null)
            {
                return new Dictionary<string, CentralDataSourceColumnMetadata>(
                    StringComparer.OrdinalIgnoreCase);
            }

            var result = new Dictionary<string, CentralDataSourceColumnMetadata>(
                StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in schema.Rows)
            {
                var sourceName = Text(row, "COLUMN_NAME");
                if (string.IsNullOrWhiteSpace(sourceName))
                {
                    continue;
                }

                // DESCRIPTION is part of the OLE DB COLUMNS schema rowset.
                // ACE versions may additionally expose a provider-specific
                // caption/display-name column, so accept those when present.
                var caption = FirstText(
                    row,
                    "CAPTION",
                    "COLUMN_CAPTION",
                    "DISPLAY_NAME");
                var description = FirstText(
                    row,
                    "DESCRIPTION",
                    "COLUMN_DESCRIPTION");
                result[sourceName] = new CentralDataSourceColumnMetadata(
                    sourceName,
                    caption,
                    description);
            }

            return result;
        }
        catch (OleDbException)
        {
            // Metadata improves matching but is not required to read the table.
            return new Dictionary<string, CentralDataSourceColumnMetadata>(
                StringComparer.OrdinalIgnoreCase);
        }
        catch (InvalidOperationException)
        {
            return new Dictionary<string, CentralDataSourceColumnMetadata>(
                StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? FirstText(DataRow row, params string[] columnNames) =>
        columnNames
            .Select(columnName => Text(row, columnName))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string? Text(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] is DBNull)
        {
            return null;
        }

        return Convert.ToString(row[columnName])?.Trim();
    }

    private static async Task<OleDbConnection> OpenAsync(
        CentralDataDatabaseConnection connection,
        CancellationToken cancellationToken)
    {
        Exception? firstFailure = null;
        foreach (var provider in new[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" })
        {
            var database = new OleDbConnection(
                $"Provider={provider};Data Source={connection.AccessDatabasePath};Persist Security Info=False;");
            try
            {
                await database.OpenAsync(cancellationToken);
                return database;
            }
            catch (Exception exception) when (exception is OleDbException or InvalidOperationException)
            {
                firstFailure ??= exception;
                await database.DisposeAsync();
            }
        }

        throw new InvalidOperationException(
            "The Access database could not be opened. Install the 64-bit Microsoft Access Database Engine or check that the file is available.",
            firstFailure);
    }

    private static string Quote(string identifier) =>
        $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static void Validate(CentralDataDatabaseConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.SourceKind != CentralDataSourceKind.AccessDatabase ||
            string.IsNullOrWhiteSpace(connection.AccessDatabasePath))
        {
            throw new ArgumentException("An Access database path is required.", nameof(connection));
        }
    }
}
