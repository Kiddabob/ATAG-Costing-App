using System.Text.Json;
using ATAG.Costing.Application.Projects;

namespace ATAG.Costing.Infrastructure.Projects;

/// <summary>
/// Portable, versioned JSON persistence for a manually chosen costing file.
/// Writes use a temporary sibling file followed by an atomic replacement.
/// </summary>
public sealed class JsonSingleCoreProjectDocumentStore :
    ISingleCoreProjectDocumentStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public async Task SaveAsync(
        string path,
        SingleCoreProjectDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);

        if (!document.IsSupportedSchema)
        {
            throw new JsonException(
                $"Costing schema {document.SchemaVersion} is not supported.");
        }

        var normalized = document.Upgrade();
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException(
                "The costing path has no parent directory.");
        Directory.CreateDirectory(directory);

        if (File.Exists(fullPath))
        {
            var existing = await LoadAsync(fullPath, cancellationToken);
            if (existing.RevisionState ==
                CostingRevisionState.ApprovedRevision)
            {
                throw new InvalidOperationException(
                    "Approved costing revisions are immutable and cannot be overwritten.");
            }

            if (normalized.RevisionState ==
                    CostingRevisionState.ApprovedRevision &&
                (normalized.ProjectId != existing.ProjectId ||
                 normalized.RevisionId != existing.RevisionId))
            {
                throw new InvalidOperationException(
                    "Approval can only replace the matching working revision.");
            }
        }

        var temporaryPath =
            Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 65536,
                             useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    normalized,
                    SerializerOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public async Task<SingleCoreProjectDocument> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(
            Path.GetFullPath(path),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 65536,
            useAsync: true);
        var document =
            await JsonSerializer.DeserializeAsync<SingleCoreProjectDocument>(
                stream,
                SerializerOptions,
                cancellationToken)
            ?? throw new JsonException(
                "The costing document contains no readable project.");
        if (!document.IsSupportedSchema)
        {
            throw new JsonException(
                $"Costing schema {document.SchemaVersion} is not supported.");
        }

        return document.Upgrade();
    }
}
