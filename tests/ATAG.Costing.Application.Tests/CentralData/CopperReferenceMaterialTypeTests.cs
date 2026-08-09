using ATAG.Costing.Application.CentralData;
using Xunit;

namespace ATAG.Costing.Application.Tests.CentralData;

public sealed class CopperReferenceMaterialTypeTests
{
    [Theory]
    [InlineData("7/0.196 TCW (H)", "TCW", false)]
    [InlineData("19/0.20 PCW", "PCW", false)]
    [InlineData("0.50 TI wire", "TI", true)]
    [InlineData("Copper tinsel cord", "TINSEL", true)]
    [InlineData("Silver plated wire", "SILVER", true)]
    [InlineData("Stainless steel strand", "STAINLESS", true)]
    public void MaterialType_SeparatesFinishAndSupplierDefinedConstructions(
        string description,
        string expectedCode,
        bool expectedSupplierDefined)
    {
        var reference = new CopperReference(
            "test",
            description,
            "Supplier",
            1m,
            1m,
            1m);

        Assert.Equal(expectedCode, reference.MaterialTypeCode);
        Assert.Equal(
            expectedSupplierDefined,
            reference.IsSupplierDefinedConstruction);
    }

    [Fact]
    public void SelectableForCosting_KeepsParsedLinkedRowWithMissingNominalOd()
    {
        var reference = new CopperReference(
            "863",
            "32/0.20 TCW (H)",
            "Hayo Energi",
            PricePerKilogram: 0m,
            YieldMetresPerKilogram: 87.209302m,
            NominalOutsideDiameterMillimetres: 0m);

        Assert.False(reference.IsCostingReady);
        Assert.True(reference.IsSelectableForCosting);
    }
}
