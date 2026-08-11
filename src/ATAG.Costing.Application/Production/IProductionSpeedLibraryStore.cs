namespace ATAG.Costing.Application.Production;

public interface IProductionSpeedLibraryStore
{
    ProductionSpeedLibraryState Load();

    void Save(ProductionSpeedLibraryState state);
}
