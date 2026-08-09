namespace ATAG.Costing.Application.CentralData;

public interface ICentralDataSourceReader
{
    CentralDataSourceKind Kind { get; }

    Task<CentralDataReadResult> ReadAsync(
        CentralDataSourceConfiguration configuration,
        CancellationToken cancellationToken = default);
}
