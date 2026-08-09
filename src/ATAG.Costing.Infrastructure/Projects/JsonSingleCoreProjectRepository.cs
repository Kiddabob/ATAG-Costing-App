using System.Text.Json;
using ATAG.Costing.Application.Projects;

namespace ATAG.Costing.Infrastructure.Projects;

/// <summary>
/// Stores revision documents and a portable relative-path index beneath the
/// selected business-data folder. It never substitutes another location.
/// </summary>
public sealed class JsonSingleCoreProjectRepository :
    ISingleCoreProjectRepository
{
    public const string IndexFileName = "ATAG-Costing-Index.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ISingleCoreProjectDocumentStore _documentStore;

    public JsonSingleCoreProjectRepository(
        ISingleCoreProjectDocumentStore? documentStore = null)
    {
        _documentStore =
            documentStore ?? new JsonSingleCoreProjectDocumentStore();
    }

    public async Task<SingleCoreProjectSaveResult> SaveAsync(
        string storageRoot,
        SingleCoreProjectDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var root = RequireAvailableRoot(storageRoot);
        var normalized = document.Upgrade();
        var relativePath = Path.Combine(
            "Costings",
            normalized.ProjectId.ToString("N"),
            $"Revision-{normalized.RevisionNumber:D4}-{normalized.RevisionId:N}.atagcosting");
        var fullPath = ResolveContainedPath(root, relativePath);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await _documentStore.SaveAsync(
                fullPath,
                normalized,
                cancellationToken);

            var entry = ToIndexEntry(normalized, relativePath);
            var entries = (await LoadIndexCoreAsync(root, cancellationToken))
                .Where(item => item.RevisionId != normalized.RevisionId)
                .Append(entry)
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ToArray();
            await SaveIndexCoreAsync(root, entries, cancellationToken);
            return new SingleCoreProjectSaveResult(
                normalized,
                entry,
                fullPath);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<SingleCoreProjectIndexEntry>> ListAsync(
        string storageRoot,
        CancellationToken cancellationToken = default)
    {
        var root = RequireAvailableRoot(storageRoot);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadIndexCoreAsync(root, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<SingleCoreProjectDocument> LoadAsync(
        string storageRoot,
        SingleCoreProjectIndexEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var root = RequireAvailableRoot(storageRoot);
        var fullPath = ResolveContainedPath(root, entry.RelativePath);
        return _documentStore.LoadAsync(fullPath, cancellationToken);
    }

    private static string RequireAvailableRoot(string storageRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRoot);
        var root = Path.GetFullPath(storageRoot);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(
                "The selected business-data folder is unavailable.");
        }

        return root;
    }

    private static string ResolveContainedPath(
        string root,
        string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException(
                "Project index paths must be relative to the selected folder.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var relativeCheck = Path.GetRelativePath(root, fullPath);
        if (relativeCheck == ".." ||
            relativeCheck.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "The project index contains a path outside the selected folder.");
        }

        return fullPath;
    }

    private static async Task<IReadOnlyList<SingleCoreProjectIndexEntry>>
        LoadIndexCoreAsync(
            string root,
            CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, IndexFileName);
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 32768,
            useAsync: true);
        var index = await JsonSerializer.DeserializeAsync<ProjectIndexFile>(
            stream,
            SerializerOptions,
            cancellationToken);
        if (index is null || index.SchemaVersion != 1)
        {
            throw new JsonException(
                "The ATAG costing project index is not readable.");
        }

        return index.Entries
            .OrderByDescending(item => item.UpdatedAtUtc)
            .ToArray();
    }

    private static async Task SaveIndexCoreAsync(
        string root,
        IReadOnlyList<SingleCoreProjectIndexEntry> entries,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, IndexFileName);
        var temporaryPath =
            Path.Combine(root, $".{IndexFileName}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 32768,
                             useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    new ProjectIndexFile(1, entries),
                    SerializerOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static SingleCoreProjectIndexEntry ToIndexEntry(
        SingleCoreProjectDocument document,
        string relativePath) =>
        new(
            document.ProjectId,
            document.RevisionId,
            document.RevisionNumber,
            document.RevisionState,
            string.IsNullOrWhiteSpace(
                document.CalculatedResult?.EffectiveCoreName)
                ? "Single core costing"
                : document.CalculatedResult.EffectiveCoreName,
            document.CustomerName,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.ApprovedAtUtc,
            relativePath);

    private sealed record ProjectIndexFile(
        int SchemaVersion,
        IReadOnlyList<SingleCoreProjectIndexEntry> Entries);
}
