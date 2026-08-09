namespace ATAG.Costing.Application.Projects;

/// <summary>
/// Indexed revision storage rooted only in the business-data folder selected
/// by the user.
/// </summary>
public interface ISingleCoreProjectRepository
{
    Task<SingleCoreProjectSaveResult> SaveAsync(
        string storageRoot,
        SingleCoreProjectDocument document,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SingleCoreProjectIndexEntry>> ListAsync(
        string storageRoot,
        CancellationToken cancellationToken = default);

    Task<SingleCoreProjectDocument> LoadAsync(
        string storageRoot,
        SingleCoreProjectIndexEntry entry,
        CancellationToken cancellationToken = default);
}

public sealed record SingleCoreProjectSaveResult(
    SingleCoreProjectDocument Document,
    SingleCoreProjectIndexEntry IndexEntry,
    string FullPath);

public sealed record SingleCoreProjectIndexEntry(
    Guid ProjectId,
    Guid RevisionId,
    int RevisionNumber,
    CostingRevisionState RevisionState,
    string ProjectName,
    string CustomerName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    string RelativePath)
{
    public string DisplayName =>
        $"{ProjectName} · revision {RevisionNumber} · " +
        (RevisionState == CostingRevisionState.ApprovedRevision
            ? "Approved"
            : "Working copy");
}
