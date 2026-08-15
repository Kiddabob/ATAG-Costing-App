using ATAG.Costing.Application.CentralData;

namespace ATAG.Costing.Application.Braiding;

/// <summary>
/// Projects the retained Copper table into the loose, small-strand
/// constructions which can be used as braid ends. The source record remains
/// attached so supplier and material finish stay visible and traceable.
/// </summary>
public sealed class BraidWireReference
{
    public BraidWireReference(
        CopperReference copper,
        int endsPerCarrier,
        decimal strandDiameterMillimetres)
    {
        Copper = copper;
        EndsPerCarrier = endsPerCarrier;
        StrandDiameterMillimetres = strandDiameterMillimetres;
    }

    public CopperReference Copper { get; }

    public int EndsPerCarrier { get; }

    public decimal StrandDiameterMillimetres { get; }

    public string Id => Copper.Id;

    public string Display =>
        $"{Copper.Construction?.NormalizedConstruction ?? Copper.Description} · " +
        $"{Copper.MaterialTypeCode} · {Copper.Supplier}";

    public string Detail =>
        $"{EndsPerCarrier} end{(EndsPerCarrier == 1 ? string.Empty : "s")} per carrier · " +
        $"{StrandDiameterMillimetres:0.###} mm wire · " +
        $"{Copper.MaterialTypeDisplay} · {Copper.Supplier}";
}

public static class BraidWireCatalogue
{
    public const decimal MaximumStrandDiameterMillimetres = 0.25m;

    public static IReadOnlyList<BraidWireReference> Create(
        IEnumerable<CopperReference> copperRows)
    {
        ArgumentNullException.ThrowIfNull(copperRows);

        return copperRows
            .Select(
                copper => new
                {
                    Copper = copper,
                    Construction = copper.Construction,
                })
            .Where(item =>
                item.Construction is
                {
                    IsRopeLay: false,
                    TotalStrandCount: >= 1 and <= 10,
                    StrandDiameterMillimetres: > 0m and <= MaximumStrandDiameterMillimetres,
                })
            .Select(item => new BraidWireReference(
                item.Copper,
                item.Construction!.TotalStrandCount,
                item.Construction.StrandDiameterMillimetres))
            .OrderBy(item => item.EndsPerCarrier)
            .ThenBy(item => item.StrandDiameterMillimetres)
            .ThenBy(item => item.Copper.MaterialTypeCode)
            .ThenBy(item => item.Copper.Supplier)
            .ThenBy(item => item.Copper.Description)
            .ToArray();
    }
}
