using ATAG.Costing.Application.Braiding;
using ATAG.Costing.Application.CentralData;
using Xunit;

namespace ATAG.Costing.Application.Tests.CentralData;

public sealed class BraidWireCatalogueTests
{
    [Fact]
    public void Create_UsesSimpleOneToTenEndCopperAndPreservesSupplierRows()
    {
        CopperReference[] source =
        [
            Copper("four-a", "4/0.10 TCW", "Supplier A"),
            Copper("four-b", "4/0.15 PCW", "Supplier B"),
            Copper("seven", "7/0.20 TCW", "Supplier C"),
            Copper("large-count", "16/0.20 TCW", "Supplier D"),
            Copper("large-wire", "4/0.32 TCW", "Supplier E"),
            Copper("rope", "7x19/0.10 TCW", "Supplier F"),
        ];

        var choices = BraidWireCatalogue.Create(source);

        Assert.Equal(3, choices.Count);
        Assert.Equal([4, 4, 7], choices.Select(item => item.EndsPerCarrier));
        Assert.Contains(choices, item =>
            item.StrandDiameterMillimetres == 0.15m &&
            item.Copper.Supplier == "Supplier B");
        Assert.DoesNotContain(choices, item => item.Copper.Id == "rope");
    }

    private static CopperReference Copper(
        string id,
        string description,
        string supplier) =>
        new(
            id,
            description,
            supplier,
            0m,
            1m,
            1m);
}
