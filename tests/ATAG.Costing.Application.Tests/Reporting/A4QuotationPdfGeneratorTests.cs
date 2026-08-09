using System.Text;
using ATAG.Costing.Reporting.Quotations;
using Xunit;

namespace ATAG.Costing.Application.Tests.Reporting;

public sealed class A4QuotationPdfGeneratorTests
{
    [Fact]
    public void Generate_ProducesASinglePageA4Quotation()
    {
        var document = CreateSampleDocument();
        var generator = new A4QuotationPdfGenerator();

        var bytes = generator.GenerateBytes(document);
        var pdfText = Encoding.Latin1.GetString(bytes);

        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("/MediaBox [0 0 595 842]", pdfText);
        Assert.Contains("/Count 1", pdfText);
        Assert.Contains("Costing App", pdfText);
        Assert.Contains("COST-V1-SAMPLE", pdfText);
        Assert.DoesNotContain("ATAG", pdfText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("South Church", pdfText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("01325", pdfText, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("%%EOF\n", pdfText);

        var samplePath = Environment.GetEnvironmentVariable(
            "COSTING_QUOTE_SAMPLE_PATH");
        if (!string.IsNullOrWhiteSpace(samplePath))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(samplePath)
                ?? throw new InvalidOperationException(
                    "The sample PDF path has no directory."));
            File.WriteAllBytes(samplePath, bytes);
        }
    }

    [Fact]
    public void Generate_PreservesTheEuroSymbolInWinAnsiOutput()
    {
        var document = CreateSampleDocument() with
        {
            CurrencyCode = "EUR",
            CurrencySymbol = "€",
        };

        var bytes = new A4QuotationPdfGenerator().GenerateBytes(document);

        Assert.Contains((byte)0x80, bytes);
    }

    private static A4QuotationDocument CreateSampleDocument() =>
        new(
            "COST-V1-SAMPLE",
            new DateOnly(2026, 7, 29),
            "Example Customer Ltd",
            ["Unit 1", "Example Industrial Estate", "Bishop Auckland", "DL14 6XB"],
            "Office Operator",
            "COR 0720 T T2 - single insulated core",
            10m,
            500m,
            5000m,
            "GBP",
            "£",
            0.0325m,
            162.50m,
            0m,
            "7/0.20 TCW - 0.21 mm² - 24 AWG",
            "PVC",
            "Red",
            "Returnable reels",
            "500 m per reel (10 reels)",
            "To be confirmed",
            "Customer wording can be reviewed before the final PDF is saved.",
            "Prices exclude VAT and remain subject to final technical and commercial approval.");
}
