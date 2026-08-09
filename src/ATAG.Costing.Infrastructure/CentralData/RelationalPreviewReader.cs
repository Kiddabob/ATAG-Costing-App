using System.Data.Common;
using ATAG.Costing.Application.CentralData;

namespace ATAG.Costing.Infrastructure.CentralData;

internal static class RelationalPreviewReader
{
    public static async Task<CentralDataTablePreview> ReadAsync(
        DbDataReader reader,
        CentralDataSourceObject sourceObject,
        int rowLimit,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, CentralDataSourceColumnMetadata>? columnMetadata = null)
    {
        var schema = await reader.GetColumnSchemaAsync(cancellationToken);
        var columns = schema
            .Select((column, index) =>
            {
                var sourceName = column.ColumnName ?? $"Column {index + 1}";
                CentralDataSourceColumnMetadata? metadata = null;
                columnMetadata?.TryGetValue(sourceName, out metadata);
                return new CentralDataPreviewColumn(
                    sourceName,
                    column.DataTypeName ?? column.DataType?.Name ?? "Unknown",
                    index,
                    column.AllowDBNull ?? true,
                    sourceName,
                    metadata?.Caption,
                    metadata?.Description);
            })
            .ToArray();
        var rows = new List<CentralDataPreviewRow>();
        var issues = new List<CentralDataPreviewIssue>();

        try
        {
            while ((rowLimit == 0 || rows.Count < rowLimit) &&
                   await reader.ReadAsync(cancellationToken))
            {
                var rowNumber = rows.Count + 1;
                var cells = new Dictionary<string, CentralDataPreviewCell>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < columns.Length; index++)
                {
                    CentralDataPreviewCell cell;
                    try
                    {
                        cell = CentralDataPreviewCell.FromValue(reader.GetValue(index));
                    }
                    catch (Exception exception) when (LooksLikeDivisionByZero(exception))
                    {
                        cell = CentralDataPreviewCell.DivisionByZero(exception.Message);
                    }
                    catch (Exception exception)
                    {
                        cell = CentralDataPreviewCell.SourceError(exception.Message);
                    }

                    cells[columns[index].Name] = cell;
                    if (cell.HasError)
                    {
                        issues.Add(new CentralDataPreviewIssue(
                            rowNumber,
                            columns[index].Name,
                            cell.ErrorMessage ?? cell.DisplayValue));
                    }
                }

                rows.Add(new CentralDataPreviewRow(rowNumber, cells));
            }
        }
        catch (Exception exception) when (LooksLikeDivisionByZero(exception))
        {
            issues.Add(new CentralDataPreviewIssue(
                null,
                null,
                "The provider stopped the complete query at a division-by-zero result. Rows already read remain previewable, but this partial result cannot replace the retained table.",
                IsBlocking: true));
        }

        return new CentralDataTablePreview(sourceObject, columns, rows, issues, rowLimit);
    }

    private static bool LooksLikeDivisionByZero(Exception exception) =>
        exception.Message.Contains("divide by zero", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("division by zero", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("#DIV/0", StringComparison.OrdinalIgnoreCase);
}
