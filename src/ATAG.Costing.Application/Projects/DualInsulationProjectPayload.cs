using ATAG.Costing.Domain.Costing;

namespace ATAG.Costing.Application.Projects;

public enum CostingConstructionKind
{
    SingleInsulatedCore = 0,
    DualInsulation = 1,
}

/// <summary>
/// Locked reference and quote values for one material stream. The live central
/// catalogue can change or be offline without changing an approved revision.
/// </summary>
public sealed record DualMaterialReferenceSnapshot
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Supplier { get; init; } = "";
    public double SupplierQuoteTotal { get; init; }
    public double SupplierQuotedKilograms { get; init; }
}

public sealed record DualInsulationLayerProjectPayload
{
    public DualMaterialReferenceSnapshot Compound { get; init; } = new();
    public double CompoundSpecificGravity { get; init; }
    public double NominalFinishedOutsideDiameterMillimetres { get; init; }
    public double PositiveOutsideDiameterToleranceMillimetres { get; init; }
    public DualMaterialReferenceSnapshot Masterbatch { get; init; } = new();
    public double MasterbatchAdditionPercent { get; init; }
}

public sealed record DualLineSpeedBandSnapshot
{
    public double MaximumOutsideDiameterMillimetres { get; init; }
    public double LineSpeedMetresPerHour { get; init; }
}

public sealed record DualExtrusionProjectPayload
{
    public string ProcessName { get; init; } = "";
    public string ProfileReference { get; init; } = "";
    public string ProfileRuleVersion { get; init; } = "";
    public IReadOnlyList<DualLineSpeedBandSnapshot> LineSpeedBands { get; init; } = [];
    public double AboveMaximumLineSpeedMetresPerHour { get; init; }
    public bool UseManualLineSpeed { get; init; }
    public double ManualLineSpeedMetresPerHour { get; init; }
    public double SetupTimeHours { get; init; }
    public double OperatorCount { get; init; }
    public double HourlyLabourRate { get; init; }
}

/// <summary>
/// Schema-v3 editable dual-insulation inputs. Optional modules are deliberately
/// retained in physical inside-to-outside order even though their costing
/// formulas are not part of this slice.
/// </summary>
public sealed record DualInsulationProjectPayload
{
    public string ProjectName { get; init; } = "";
    public DualMaterialReferenceSnapshot Conductor { get; init; } = new();
    public double ConductorYieldMetresPerKilogram { get; init; }
    public double ConductorOutsideDiameterMillimetres { get; init; }
    public DualInsulationLayerProjectPayload FirstLayer { get; init; } = new();
    public DualInsulationLayerProjectPayload SecondLayer { get; init; } = new();
    public double FinishedQuoteLengthMetres { get; init; }
    public double CoreStartupLengthMetres { get; init; }
    public double UsageAllowancePercent { get; init; }
    public double RiskPercent { get; init; }
    public double MarkupPercent { get; init; }
    public double TargetMarginPercent { get; init; }
    public DualExtrusionProjectPayload FirstExtrusion { get; init; } = new();
    public DualExtrusionProjectPayload SecondExtrusion { get; init; } = new();
    public IReadOnlyList<CableAddOnModule> AddOnModules { get; init; } = [];
}

/// <summary>
/// Exact dual-insulation evidence retained with an approved revision. Raw
/// values support later consumers while display strings reproduce the reviewed
/// result without silently applying newer rule or formatting versions.
/// </summary>
public sealed record DualInsulationCalculatedResultSnapshot
{
    public string ProjectName { get; init; } = "";
    public decimal CoreAndFirstLayerProductionLengthMetres { get; init; }
    public decimal SecondLayerProductionLengthMetres { get; init; }
    public decimal MaterialPriceForProductionRun { get; init; }
    public decimal MaterialPricePerFinishedMetre { get; init; }
    public decimal FirstExtrusionLabourCost { get; init; }
    public decimal SecondExtrusionLabourCost { get; init; }
    public decimal TotalLabourCost { get; init; }
    public decimal EstimatedCost { get; init; }
    public decimal RiskAdjustedCost { get; init; }
    public decimal SequentialRiskThenMarkupPrice { get; init; }
    public decimal CombinedRiskAndMarkupPrice { get; init; }
    public decimal TargetGrossMarginPrice { get; init; }
    public string MaterialPriceForProductionRunDisplay { get; init; } = "";
    public string MaterialPricePerFinishedMetreDisplay { get; init; } = "";
    public string FirstExtrusionLabourCostDisplay { get; init; } = "";
    public string SecondExtrusionLabourCostDisplay { get; init; } = "";
    public string FirstProductionTimeDisplay { get; init; } = "";
    public string SecondProductionTimeDisplay { get; init; } = "";
    public string TotalLabourCostDisplay { get; init; } = "";
    public string EstimatedCostDisplay { get; init; } = "";
    public string RiskAdjustedCostDisplay { get; init; } = "";
    public string RecommendedQuoteDisplay { get; init; } = "";
    public string CombinedRatePriceDisplay { get; init; } = "";
    public string TargetMarginPriceDisplay { get; init; } = "";
    public IReadOnlyList<SavedCalculationSection> Trace { get; init; } = [];
}
