using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Domain.Costing;

namespace ATAG.Costing.Application.Costing;

/// <summary>
/// Search and ordering state shared by the dual-insulation presentation. It is
/// UI-framework independent so the five reference selectors and physical
/// module order can be tested without starting WinUI.
/// </summary>
public static class DualInsulationWorkspaceState
{
    private static readonly CableAddOnModule[] PhysicalModuleOrder =
    [
        CableAddOnModule.Tape,
        CableAddOnModule.Chalk,
        CableAddOnModule.Foil,
        CableAddOnModule.Braid,
        CableAddOnModule.Lapscreen,
        CableAddOnModule.DrainWire,
    ];

    public static IReadOnlyList<CopperReference> FilterCopper(
        IEnumerable<CopperReference> source,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source
            .Where(item =>
                Contains(item.Description, searchText) ||
                Contains(item.Supplier, searchText) ||
                Contains(item.Id, searchText))
            .ToArray();
    }

    public static IReadOnlyList<CompoundReference> FilterCompounds(
        IEnumerable<CompoundReference> source,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source
            .Where(item =>
                Contains(item.CompoundName, searchText) ||
                Contains(item.Supplier, searchText) ||
                Contains(item.MaterialType, searchText) ||
                Contains(item.Description, searchText))
            .ToArray();
    }

    public static IReadOnlyList<MasterbatchReference> FilterMasterbatches(
        IEnumerable<MasterbatchReference> source,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source
            .Where(item => MasterbatchColourSearch.Matches(item, searchText))
            .ToArray();
    }

    public static IReadOnlyList<CableAddOnModule> OrderModules(
        IEnumerable<CableAddOnModule> selectedModules)
    {
        ArgumentNullException.ThrowIfNull(selectedModules);
        var selected = selectedModules.ToHashSet();
        return PhysicalModuleOrder
            .Where(selected.Contains)
            .ToArray();
    }

    private static bool Contains(string? value, string? searchText) =>
        string.IsNullOrWhiteSpace(searchText) ||
        (!string.IsNullOrWhiteSpace(value) &&
         value.Contains(
             searchText.Trim(),
             StringComparison.OrdinalIgnoreCase));
}
