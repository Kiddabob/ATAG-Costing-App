using ATAG.Costing.Application.CentralData;
using Xunit;

namespace ATAG.Costing.Application.Tests.CentralData;

public sealed class MasterbatchColourSearchTests
{
    [Theory]
    [InlineData("red")]
    [InlineData("warm")]
    [InlineData("vivid")]
    [InlineData("rocket")]
    [InlineData("roket")]
    public void Matches_RedColour_BySemanticOrFuzzyText(string search)
    {
        var colour = Colour(
            "CUS3872",
            "Rocket Red",
            "#E90029",
            "Universal");

        Assert.True(MasterbatchColourSearch.Matches(colour, search));
    }

    [Fact]
    public void Matches_DoesNotTreatVividRedAsDull()
    {
        var colour = Colour(
            "CUS3872",
            "Rocket Red",
            "#E90029",
            "Universal");

        Assert.False(MasterbatchColourSearch.Matches(colour, "dull"));
    }

    [Fact]
    public void Matches_AllowsCombinedSemanticTerms()
    {
        var colour = Colour(
            "CUSNAVY",
            "Midnight",
            "#001F5B",
            "Universal");

        Assert.True(MasterbatchColourSearch.Matches(colour, "dark blue"));
        Assert.False(MasterbatchColourSearch.Matches(colour, "light blue"));
    }

    [Fact]
    public void Matches_AppliesGroupAndColourTypeFilters()
    {
        var colour = Colour(
            "CUS3872",
            "Rocket Red",
            "#E90029",
            "Universal");

        Assert.True(
            MasterbatchColourSearch.Matches(
                colour,
                "",
                "Red",
                "univ"));
        Assert.False(
            MasterbatchColourSearch.Matches(
                colour,
                "",
                "Blue",
                "univ"));
        Assert.False(
            MasterbatchColourSearch.Matches(
                colour,
                "",
                "Red",
                "special"));
    }

    [Fact]
    public void CompatibilityCells_PutFamilyAboveTemperature_AndBlankIncompatible()
    {
        var colour = Colour(
            "CUS3872",
            "Rocket Red",
            "#E90029",
            "Universal") with
        {
            Compatibility = "PVC, PS",
            TemperatureLimits = "PVC *200+ °C · PS 220 °C · ABS 260 °C",
        };

        var pvc = Assert.Single(
            colour.CompatibilityCells,
            cell => cell.MaterialFamily == "PVC");
        var abs = Assert.Single(
            colour.CompatibilityCells,
            cell => cell.MaterialFamily == "ABS");

        Assert.True(pvc.IsCompatible);
        Assert.Equal("*200+ °C", pvc.TemperatureDisplay);
        Assert.False(abs.IsCompatible);
        Assert.Equal("", abs.TemperatureDisplay);
    }

    private static MasterbatchReference Colour(
        string code,
        string name,
        string hex,
        string type) =>
        new(
            code,
            name,
            "Colourhouse Masterbatch",
            14.83m,
            "PVC, PS",
            hex,
            type,
            "RAL3020",
            "PVC *200+ °C · PS 220 °C");
}
