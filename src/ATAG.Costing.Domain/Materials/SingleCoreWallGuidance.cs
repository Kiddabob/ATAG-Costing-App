namespace ATAG.Costing.Domain.Materials;

public enum WallReferenceKind
{
    PublishedMinimum = 0,
    PublishedNominal = 1,
}

public sealed record SingleCoreWallGuidanceResult(
    decimal CalculatedRadialWallMillimetres,
    decimal ReferenceWallMillimetres,
    decimal ReferenceNominalAreaSquareMillimetres,
    bool IsDirectNominalSizeMatch,
    bool IsGeometryValid,
    bool MeetsReferenceWall,
    WallReferenceKind ReferenceKind,
    string MaterialFamily,
    string SourceLabel,
    string SourceUrl,
    string Assessment,
    string RuleVersion);

/// <summary>
/// Compares the selected single-core geometry with published dimensions for
/// comparable H05/H07 flexible single-core cables. This is engineering
/// guidance only: it does not certify compliance or alter any costing result.
/// </summary>
public static class SingleCoreWallGuidance
{
    public const string RuleVersion = "single-core-wall-guidance/v1";

    private const string PvcSourceLabel =
        "Dee Cables H05V-K manufacturer data sheet";
    private const string PvcSourceUrl =
        "https://www.deecables.co.uk/wp-content/uploads/2025/04/Dee-Cables-PVC-insulated-single-core-cable-H05V-K-2025.pdf";
    private const string PvcNominalSourceLabel =
        "Clynder Cables 2491X H05V-K/H07V-K manufacturer data sheet";
    private const string PvcNominalSourceUrl =
        "https://clyndercables.co.uk/wp-content/uploads/2022/01/2491X-Spec-Sheet.pdf";
    private const string LszhSourceLabel =
        "Eland Cables H05Z-K/H07Z-K manufacturer data sheet";
    private const string LszhSourceUrl =
        "https://www.elandcables.com/media/eland/media/assets/product-pdf/h05v-k-h07z-k-bs-en-50525-3-41-cable.pdf";

    private static readonly IReadOnlyDictionary<decimal, decimal>
        PublishedReferenceWalls =
            new Dictionary<decimal, decimal>
            {
                [0.50m] = 0.60m,
                [0.75m] = 0.60m,
                [1.00m] = 0.60m,
                [1.50m] = 0.70m,
                [2.50m] = 0.80m,
                [4.00m] = 0.80m,
                [6.00m] = 0.80m,
                [10.00m] = 1.00m,
                [16.00m] = 1.00m,
                [25.00m] = 1.20m,
                [35.00m] = 1.20m,
                [50.00m] = 1.40m,
                [70.00m] = 1.40m,
                [95.00m] = 1.60m,
                [120.00m] = 1.60m,
                [150.00m] = 1.80m,
                [185.00m] = 2.00m,
                [240.00m] = 2.20m,
            };

    public static SingleCoreWallGuidanceResult Compare(
        decimal conductorOutsideDiameterMillimetres,
        decimal finishedOutsideDiameterMillimetres,
        decimal nominalAreaSquareMillimetres,
        string? compoundDescription)
    {
        var radialWall =
            (finishedOutsideDiameterMillimetres -
             conductorOutsideDiameterMillimetres) /
            2m;
        var geometryValid =
            conductorOutsideDiameterMillimetres > 0m &&
            finishedOutsideDiameterMillimetres >=
                conductorOutsideDiameterMillimetres;
        var nearestReference = PublishedReferenceWalls
            .OrderBy(item =>
                decimal.Abs(
                    item.Key -
                    Math.Max(0m, nominalAreaSquareMillimetres)))
            .First();
        var isDirectMatch =
            decimal.Abs(
                nearestReference.Key -
                nominalAreaSquareMillimetres) <=
            0.001m;
        var isLszh = IsLszhFamily(compoundDescription);
        var hasPublishedPvcMinimum =
            !isLszh &&
            nearestReference.Key <= 1.00m;
        var referenceKind =
            hasPublishedPvcMinimum
                ? WallReferenceKind.PublishedMinimum
                : WallReferenceKind.PublishedNominal;
        var sourceLabel = isLszh
            ? LszhSourceLabel
            : hasPublishedPvcMinimum
                ? PvcSourceLabel
                : PvcNominalSourceLabel;
        var sourceUrl = isLszh
            ? LszhSourceUrl
            : hasPublishedPvcMinimum
                ? PvcSourceUrl
                : PvcNominalSourceUrl;
        var meetsReference =
            geometryValid &&
            radialWall >= nearestReference.Value;

        var scope = isDirectMatch
            ? $"The selected {nominalAreaSquareMillimetres:0.###} mm² size matches the published comparator size."
            : $"No directly matching published size is available; {nearestReference.Key:0.###} mm² is shown only as the nearest comparator.";
        var assessment = !geometryValid
            ? "The finished OD is smaller than the conductor OD, so radial wall geometry is invalid."
            : $"{scope} The calculated radial wall is " +
              (meetsReference ? "at or above" : "below") +
              $" its {nearestReference.Value:0.000} mm " +
              (referenceKind == WallReferenceKind.PublishedMinimum
                  ? "published minimum."
                  : "published nominal reference.") +
              " Confirm the applicable cable standard and customer specification before approval.";

        return new SingleCoreWallGuidanceResult(
            radialWall,
            nearestReference.Value,
            nearestReference.Key,
            isDirectMatch,
            geometryValid,
            meetsReference,
            referenceKind,
            isLszh ? "LS0H/LSZH" : "PVC",
            sourceLabel,
            sourceUrl,
            assessment,
            RuleVersion);
    }

    private static bool IsLszhFamily(string? compoundDescription)
    {
        if (string.IsNullOrWhiteSpace(compoundDescription))
        {
            return false;
        }

        return compoundDescription.Contains(
                   "LSZH",
                   StringComparison.OrdinalIgnoreCase) ||
               compoundDescription.Contains(
                   "LS0H",
                   StringComparison.OrdinalIgnoreCase) ||
               compoundDescription.Contains(
                   "HFFR",
                   StringComparison.OrdinalIgnoreCase) ||
               compoundDescription.Contains(
                   "HALOGEN",
                   StringComparison.OrdinalIgnoreCase);
    }
}
