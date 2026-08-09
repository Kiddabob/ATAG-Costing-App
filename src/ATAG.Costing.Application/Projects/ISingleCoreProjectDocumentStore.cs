namespace ATAG.Costing.Application.Projects;

public interface ISingleCoreProjectDocumentStore
{
    Task SaveAsync(
        string path,
        SingleCoreProjectDocument document,
        CancellationToken cancellationToken = default);

    Task<SingleCoreProjectDocument> LoadAsync(
        string path,
        CancellationToken cancellationToken = default);
}
