using ATAG.Costing.Domain.Costing;
using ATAG.Costing.Domain.Materials;

namespace ATAG.Costing.Application.Costing;

public sealed record DualInsulationCostingRequest(
    DualInsulationCostingInputs MaterialInputs,
    ExtrusionProductionSettings FirstExtrusion,
    ExtrusionProductionSettings SecondExtrusion,
    RiskRateFraction RiskRate,
    MarkupRateFraction MarkupRate,
    TargetMarginRateFraction TargetMarginRate,
    IReadOnlyList<CableAddOnModule> AddOnModules);

public sealed record DualInsulationCostingApplicationResult(
    CableConstructionPlan Construction,
    DualInsulationCostingResult Materials,
    DualInsulationProductionResult Production,
    CommercialPricingResult Commercial);

/// <summary>
/// Coordinates the already-audited dual material, two-extrusion production,
/// and commercial domain rules. Presentation layers submit typed inputs and
/// consume the result; they do not reproduce any of these calculations.
/// </summary>
public sealed class DualInsulationCostingApplicationService
{
    public DualInsulationCostingApplicationResult Calculate(
        DualInsulationCostingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.MaterialInputs);
        ArgumentNullException.ThrowIfNull(request.FirstExtrusion);
        ArgumentNullException.ThrowIfNull(request.SecondExtrusion);
        ArgumentNullException.ThrowIfNull(request.AddOnModules);

        var construction = CableConstructionPlan.Create(
            CableConstructionKind.DualInsulated,
            addOnModules: request.AddOnModules);
        var materials = DualInsulationCostingCalculator.Calculate(
            request.MaterialInputs);
        var production = DualInsulationProductionCalculator.Calculate(
            new DualInsulationProductionInputs(
                materials,
                request.FirstExtrusion,
                request.SecondExtrusion));
        var commercial = CommercialPricingCalculator.Calculate(
            new CommercialPricingInputs(
                materials.MaterialPriceForProductionRun,
                production.TotalLabourCost,
                request.RiskRate,
                request.MarkupRate,
                request.TargetMarginRate));

        return new DualInsulationCostingApplicationResult(
            construction,
            materials,
            production,
            commercial);
    }
}
