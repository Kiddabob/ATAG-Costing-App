namespace ATAG.Costing.Domain.Costing;

public enum CableConstructionKind
{
    CorSingleInsulatedCore,
    DualInsulated,
    Flat,
    DShape,
}

public enum CableAddOnModule
{
    Tape,
    Chalk,
    Foil,
    Braid,
    Lapscreen,
    DrainWire,
}

public sealed record CableConstructionStage(
    string Reference,
    string DisplayName,
    CableAddOnModule? Module = null);

/// <summary>
/// Describes the physical inside-to-outside build order independently of the
/// UI and costing formula implementations.
/// </summary>
public sealed record CableConstructionPlan
{
    private CableConstructionPlan(
        CableConstructionKind kind,
        int coreCount,
        IReadOnlyList<CableAddOnModule> addOnModules,
        IReadOnlyList<CableConstructionStage> stages)
    {
        Kind = kind;
        CoreCount = coreCount;
        AddOnModules = addOnModules;
        Stages = stages;
    }

    public CableConstructionKind Kind { get; }

    public int CoreCount { get; }

    public IReadOnlyList<CableAddOnModule> AddOnModules { get; }

    public IReadOnlyList<CableConstructionStage> Stages { get; }

    public static CableConstructionPlan Create(
        CableConstructionKind kind,
        int coreCount = 1,
        IReadOnlyList<CableAddOnModule>? addOnModules = null)
    {
        var modules = (addOnModules ?? Array.Empty<CableAddOnModule>())
            .ToArray();
        if (modules.Distinct().Count() != modules.Length)
        {
            throw new ArgumentException(
                "A construction add-on module can only be selected once.",
                nameof(addOnModules));
        }

        switch (kind)
        {
            case CableConstructionKind.CorSingleInsulatedCore:
            case CableConstructionKind.DualInsulated:
                if (coreCount != 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(coreCount),
                        "COR and dual-insulated constructions contain one primary core.");
                }

                break;

            case CableConstructionKind.Flat:
            case CableConstructionKind.DShape:
                if (coreCount is < 1 or > 10)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(coreCount),
                        "Flat and D-shape constructions support one to ten in-line cores.");
                }

                break;
        }

        if (kind != CableConstructionKind.DualInsulated &&
            modules.Length > 0)
        {
            throw new ArgumentException(
                "Add-on modules are currently defined for the dual-insulated build only.",
                nameof(addOnModules));
        }

        IReadOnlyList<CableConstructionStage> stages = kind switch
        {
            CableConstructionKind.CorSingleInsulatedCore =>
            [
                new CableConstructionStage("conductor", "Conductor"),
                new CableConstructionStage(
                    "first-insulation",
                    "Single insulation compound and masterbatch"),
            ],
            CableConstructionKind.DualInsulated =>
            [
                new CableConstructionStage("conductor", "Conductor"),
                new CableConstructionStage(
                    "first-insulation",
                    "First insulation compound and masterbatch"),
                .. modules.Select(
                    module =>
                        new CableConstructionStage(
                            $"module-{module.ToString().ToLowerInvariant()}",
                            ModuleDisplayName(module),
                            module)),
                new CableConstructionStage(
                    "second-insulation",
                    "Second insulation compound and masterbatch"),
            ],
            CableConstructionKind.Flat =>
            [
                new CableConstructionStage(
                    "flat-cores",
                    $"{coreCount} in-line core{(coreCount == 1 ? string.Empty : "s")}"),
                new CableConstructionStage(
                    "flat-finish",
                    "Flat cable finishing layers"),
            ],
            CableConstructionKind.DShape =>
            [
                new CableConstructionStage(
                    "d-shape-cores",
                    $"{coreCount} in-line core{(coreCount == 1 ? string.Empty : "s")}"),
                new CableConstructionStage(
                    "d-shape-finish",
                    "D-shape cable finishing layers"),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        return new CableConstructionPlan(kind, coreCount, modules, stages);
    }

    private static string ModuleDisplayName(CableAddOnModule module) =>
        module switch
        {
            CableAddOnModule.Tape => "Tape",
            CableAddOnModule.Chalk => "Chalk",
            CableAddOnModule.Foil => "Foil",
            CableAddOnModule.Braid => "Braid",
            CableAddOnModule.Lapscreen => "Lapscreen",
            CableAddOnModule.DrainWire => "Drain wire",
            _ => throw new ArgumentOutOfRangeException(nameof(module)),
        };
}
