using System.Globalization;
using System.Text;

namespace ATAG.Costing.Reporting.Quotations;

/// <summary>
/// Produces a self-contained, single-page A4 quotation. It consumes approved
/// values supplied by the application and does not perform costing rules.
/// </summary>
public sealed class A4QuotationPdfGenerator
{
    private const decimal PageWidth = 595m;
    private const decimal PageHeight = 842m;
    private static readonly CultureInfo NumberCulture =
        CultureInfo.GetCultureInfo("en-GB");
    private static readonly Encoding PdfEncoding = CreatePdfEncoding();

    public void Generate(Stream output, A4QuotationDocument document)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(document);
        if (!output.CanWrite)
        {
            throw new ArgumentException(
                "The quotation output stream must be writable.",
                nameof(output));
        }

        var content = ComposePage(document);
        var contentBytes = PdfEncoding.GetBytes(content);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(PageWidth)} {N(PageHeight)}] " +
            "/Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            $"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream",
        };

        using var documentStream = new MemoryStream();
        Write(documentStream, "%PDF-1.4\n%âãÏÓ\n");
        var offsets = new long[objects.Length + 1];
        for (var index = 0; index < objects.Length; index++)
        {
            offsets[index + 1] = documentStream.Position;
            Write(
                documentStream,
                $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var crossReferenceOffset = documentStream.Position;
        Write(documentStream, $"xref\n0 {objects.Length + 1}\n");
        Write(documentStream, "0000000000 65535 f \n");
        for (var index = 1; index < offsets.Length; index++)
        {
            Write(
                documentStream,
                $"{offsets[index]:0000000000} 00000 n \n");
        }

        Write(
            documentStream,
            $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\n" +
            $"startxref\n{crossReferenceOffset}\n%%EOF\n");
        documentStream.Position = 0;
        documentStream.CopyTo(output);
    }

    public byte[] GenerateBytes(A4QuotationDocument document)
    {
        using var stream = new MemoryStream();
        Generate(stream, document);
        return stream.ToArray();
    }

    private static string ComposePage(A4QuotationDocument document)
    {
        var page = new StringBuilder();
        SetStroke(page, 0.08m, 0.35m, 0.46m);
        SetFill(page, 0.08m, 0.35m, 0.46m);
        FillRectangle(page, 36m, 744m, 523m, 62m);
        Text(page, "Quotation", 30m, 50m, 770m, bold: true, white: true);
        Text(
            page,
            "ATAG Design Ltd",
            16m,
            390m,
            782m,
            bold: true,
            white: true);
        Text(
            page,
            "Unit 18, Longfield Road",
            7.5m,
            398m,
            769m,
            white: true);
        Text(
            page,
            "South Church Enterprise Park",
            7.5m,
            398m,
            758m,
            white: true);
        Text(
            page,
            "Bishop Auckland, DL14 6XB | 01325 314128",
            7.5m,
            398m,
            747m,
            white: true);

        SetFill(page, 0m, 0m, 0m);
        Text(page, "Customer", 10m, 42m, 728m, bold: true);
        Text(
            page,
            string.IsNullOrWhiteSpace(document.CustomerName)
                ? "Customer not selected"
                : document.CustomerName,
            12m,
            42m,
            712m,
            bold: true);
        var addressY = 698m;
        foreach (var line in document.CustomerAddressLines
                     .Where(line => !string.IsNullOrWhiteSpace(line))
                     .Take(5))
        {
            Text(page, line, 9m, 42m, addressY);
            addressY -= 12m;
        }

        Text(page, "Quotation #", 9m, 355m, 728m, bold: true);
        Text(page, document.QuoteNumber, 10m, 430m, 728m);
        Text(page, "Quote date", 9m, 355m, 711m, bold: true);
        Text(
            page,
            document.QuoteDate.ToString("dd MMM yyyy", NumberCulture),
            10m,
            430m,
            711m);
        Text(page, "Quoted by", 9m, 355m, 694m, bold: true);
        Text(
            page,
            string.IsNullOrWhiteSpace(document.QuotedBy)
                ? "Not selected"
                : document.QuotedBy,
            10m,
            430m,
            694m);
        Text(page, "Currency", 9m, 355m, 677m, bold: true);
        Text(page, document.CurrencyCode, 10m, 430m, 677m);

        SectionTitle(page, "Quoted item", 630m);
        TableHeader(page, 604m);
        Line(page, 42m, 583m, 553m, 583m);
        Text(page, document.Description, 9.5m, 43m, 590m, bold: true);
        Text(page, document.ItemCount.ToString("N2", NumberCulture), 9.5m, 306m, 590m);
        Text(page, "x", 9.5m, 342m, 590m);
        Text(
            page,
            document.LengthPerItemMetres.ToString("N2", NumberCulture),
            9.5m,
            365m,
            590m);
        Text(
            page,
            document.TotalQuantityMetres.ToString("N0", NumberCulture),
            9.5m,
            414m,
            590m);
        Text(
            page,
            Money(
                document.UnitPricePerMetre,
                document.CurrencySymbol),
            9.5m,
            466m,
            590m);
        Text(
            page,
            Money(
                document.GoodsTotal,
                document.CurrencySymbol),
            9.5m,
            514m,
            590m);

        SectionTitle(page, "Specifications", 550m);
        LabelValue(
            page,
            "Conductor",
            document.ConductorSpecification,
            42m,
            525m,
            245m);
        LabelValue(
            page,
            "Insulation",
            document.InsulationSpecification,
            304m,
            525m,
            247m);
        LabelValue(
            page,
            "Colour",
            document.ColourSpecification,
            42m,
            500m,
            245m);
        LabelValue(
            page,
            "Packaging",
            document.Packaging,
            42m,
            475m,
            245m);
        LabelValue(
            page,
            "Reel size",
            document.ReelSize,
            304m,
            475m,
            247m);
        LabelValue(
            page,
            "Est. delivery",
            document.EstimatedDelivery,
            42m,
            450m,
            245m);
        LabelValue(
            page,
            "Price per km",
            Money(
                document.UnitPricePerMetre * 1000m,
                document.CurrencySymbol),
            304m,
            450m,
            247m);

        SectionTitle(page, "Special notes and totals", 412m);
        Text(page, "Special notes", 9m, 42m, 386m, bold: true);
        DrawWrappedText(
            page,
            string.IsNullOrWhiteSpace(document.SpecialNotes)
                ? "No special notes recorded."
                : document.SpecialNotes,
            8.5m,
            42m,
            371m,
            56,
            4);
        Text(page, "Cost of goods", 9m, 378m, 386m);
        Text(
            page,
            Money(
                document.GoodsTotal,
                document.CurrencySymbol),
            10m,
            489m,
            386m,
            bold: true);
        Text(page, "Delivery charge", 9m, 378m, 368m);
        Text(
            page,
            Money(
                document.DeliveryCharge,
                document.CurrencySymbol),
            10m,
            489m,
            368m);
        Line(page, 378m, 354m, 553m, 354m);
        Text(page, "Total price", 11m, 378m, 336m, bold: true);
        Text(
            page,
            Money(
                document.TotalPrice,
                document.CurrencySymbol),
            12m,
            478m,
            336m,
            bold: true);

        SectionTitle(page, "Terms and conditions", 292m);
        DrawWrappedText(
            page,
            document.TermsAndConditions,
            8.5m,
            42m,
            267m,
            112,
            9);

        SetFill(page, 0.08m, 0.35m, 0.46m);
        FillRectangle(page, 36m, 34m, 523m, 24m);
        Text(
            page,
            $"ATAG Design Ltd | Quotation {document.QuoteNumber} | Page 1 of 1",
            8m,
            48m,
            43m,
            white: true);
        return page.ToString();
    }

    private static void TableHeader(StringBuilder page, decimal y)
    {
        SetFill(page, 0.92m, 0.95m, 0.96m);
        FillRectangle(page, 42m, y - 5m, 511m, 22m);
        SetFill(page, 0m, 0m, 0m);
        Text(page, "Description", 8.5m, 43m, y, bold: true);
        Text(page, "Items", 8.5m, 305m, y, bold: true);
        Text(page, "(m)/Ea", 8.5m, 360m, y, bold: true);
        Text(page, "Total qty", 8.5m, 410m, y, bold: true);
        Text(page, "Unit price", 8.5m, 464m, y, bold: true);
        Text(page, "Total", 8.5m, 514m, y, bold: true);
    }

    private static void LabelValue(
        StringBuilder page,
        string label,
        string value,
        decimal x,
        decimal y,
        decimal width)
    {
        Text(page, label, 8.5m, x, y, bold: true);
        DrawWrappedText(
            page,
            value,
            8.5m,
            x + 67m,
            y,
            Math.Max(12, (int)(width / 5.2m)),
            2);
    }

    private static void SectionTitle(
        StringBuilder page,
        string title,
        decimal y)
    {
        SetFill(page, 0.08m, 0.35m, 0.46m);
        FillRectangle(page, 36m, y - 6m, 523m, 22m);
        Text(page, title, 11m, 43m, y, bold: true, white: true);
        SetFill(page, 0m, 0m, 0m);
    }

    private static void DrawWrappedText(
        StringBuilder page,
        string text,
        decimal fontSize,
        decimal x,
        decimal startY,
        int maximumCharacters,
        int maximumLines)
    {
        var words = NormalizeText(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length > 0 &&
                current.Length + word.Length + 1 > maximumCharacters)
            {
                lines.Add(current.ToString());
                current.Clear();
                if (lines.Count == maximumLines)
                {
                    break;
                }
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }
            current.Append(word);
        }

        if (current.Length > 0 && lines.Count < maximumLines)
        {
            lines.Add(current.ToString());
        }

        for (var index = 0; index < lines.Count; index++)
        {
            Text(page, lines[index], fontSize, x, startY - index * 11m);
        }
    }

    private static string Money(
        decimal value,
        string symbol) =>
        $"{symbol}{value.ToString("N2", NumberCulture)}";

    private static void Text(
        StringBuilder page,
        string text,
        decimal fontSize,
        decimal x,
        decimal y,
        bool bold = false,
        bool white = false)
    {
        SetFill(page, white ? 1m : 0m, white ? 1m : 0m, white ? 1m : 0m);
        page.Append("BT /")
            .Append(bold ? "F2" : "F1")
            .Append(' ')
            .Append(N(fontSize))
            .Append(" Tf 1 0 0 1 ")
            .Append(N(x))
            .Append(' ')
            .Append(N(y))
            .Append(" Tm (")
            .Append(EscapePdfText(text))
            .Append(") Tj ET\n");
    }

    private static void Line(
        StringBuilder page,
        decimal x1,
        decimal y1,
        decimal x2,
        decimal y2) =>
        page.Append(N(x1))
            .Append(' ')
            .Append(N(y1))
            .Append(" m ")
            .Append(N(x2))
            .Append(' ')
            .Append(N(y2))
            .Append(" l S\n");

    private static void FillRectangle(
        StringBuilder page,
        decimal x,
        decimal y,
        decimal width,
        decimal height) =>
        page.Append(N(x))
            .Append(' ')
            .Append(N(y))
            .Append(' ')
            .Append(N(width))
            .Append(' ')
            .Append(N(height))
            .Append(" re f\n");

    private static void SetFill(
        StringBuilder page,
        decimal red,
        decimal green,
        decimal blue) =>
        page.Append(N(red))
            .Append(' ')
            .Append(N(green))
            .Append(' ')
            .Append(N(blue))
            .Append(" rg\n");

    private static void SetStroke(
        StringBuilder page,
        decimal red,
        decimal green,
        decimal blue) =>
        page.Append(N(red))
            .Append(' ')
            .Append(N(green))
            .Append(' ')
            .Append(N(blue))
            .Append(" RG 0.7 w\n");

    private static string EscapePdfText(string value) =>
        NormalizeText(value)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static string NormalizeText(string value)
    {
        var normalized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("–", "-", StringComparison.Ordinal)
            .Replace("—", "-", StringComparison.Ordinal)
            .Replace("−", "-", StringComparison.Ordinal);
        return new string(
            normalized.Select(character =>
                character <= byte.MaxValue || character == '€'
                    ? character
                    : '?').ToArray());
    }

    private static Encoding CreatePdfEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252);
    }

    private static string N(decimal value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void Write(Stream stream, string value)
    {
        var bytes = PdfEncoding.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
