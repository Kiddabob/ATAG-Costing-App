namespace ATAG.Costing.Application.CentralData;

public interface ICentralDataStore
{
    CentralDataState Load();

    void SaveConfiguration(CentralDataSourceConfiguration configuration);

    void SaveTableLink(CentralDataTableLink link);

    void RemoveTableLink(CentralDataArea area);

    void SaveSnapshot(CentralDataSnapshot snapshot);

    void SaveImportedTable(
        CentralDataTableLink link,
        CentralDataSnapshot snapshot,
        CentralDataRetainedTable retainedTable);
}
