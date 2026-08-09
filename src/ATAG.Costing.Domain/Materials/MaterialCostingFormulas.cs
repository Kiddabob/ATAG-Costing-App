using ATAG.Costing.Domain.Calculations;

namespace ATAG.Costing.Domain.Materials;

/// <summary>
/// Shared unrounded material formula primitives. Construction calculators own
/// validation and trace wording, while these methods keep conductor, annular
/// insulation, allowance, length, and price arithmetic in one place.
/// </summary>
internal static class MaterialCostingFormulas
{
    private const decimal Pi = 3.1415926535897932384626433833m;

    public static decimal ConductorKilogramsPerMetre(decimal yieldMetresPerKilogram) =>
        1m / yieldMetresPerKilogram;

    public static decimal CircularAreaSquareMillimetres(decimal diameterMillimetres) =>
        Pi / 4m * diameterMillimetres * diameterMillimetres;

    public static decimal AnnularAreaSquareMillimetres(
        decimal innerDiameterMillimetres,
        decimal outerDiameterMillimetres) =>
        CircularAreaSquareMillimetres(outerDiameterMillimetres) -
        CircularAreaSquareMillimetres(innerDiameterMillimetres);

    public static decimal CompoundKilogramsPerMetre(
        decimal annularAreaSquareMillimetres,
        decimal specificGravity) =>
        annularAreaSquareMillimetres * specificGravity / 1000m;

    public static decimal ApplyUsageAllowance(
        decimal baseUsage,
        UsageAllowanceRateFraction allowanceRate) =>
        baseUsage * (1m + allowanceRate.Value);

    public static decimal MassForLength(
        decimal kilogramsPerMetre,
        LengthMetres length) =>
        kilogramsPerMetre * length.Value;

    public static decimal PricePerMetre(
        decimal kilogramsPerMetre,
        PricePerKilogram pricePerKilogram) =>
        kilogramsPerMetre * pricePerKilogram.Value;

    public static decimal PriceForLength(
        decimal pricePerMetre,
        LengthMetres length) =>
        pricePerMetre * length.Value;
}
