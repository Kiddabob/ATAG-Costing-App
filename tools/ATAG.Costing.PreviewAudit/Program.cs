using System.Globalization;
using System.Text;
using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.Domain.Conductors;

namespace ATAG.Costing.PreviewAudit;

internal static class Program
{
    private const double Perspective = 0.3420201433256687d;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static readonly (string Description, decimal Area)[] Cases =
    [
        ("4/0.10 TCW", 0.031m),
        ("7/0.196 TCW", 0.21m),
        ("16/0.196 TCW", 0.50m),
        ("19/0.10 TCW", 0.15m),
        ("32/0.196 TCW", 0.965m),
        ("7x19/0.32 H72", 25m),
    ];

    public static int Main(string[] args)
    {
        var output = args.Length == 0
            ? Path.Combine(Environment.CurrentDirectory, "conductor-preview-audit.svg")
            : Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        const int tileWidth = 360;
        const int tileHeight = 390;
        const int columns = 3;
        var rows = (int)Math.Ceiling(Cases.Length / (double)columns);
        var svg = new StringBuilder();
        svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{tileWidth * columns}\" height=\"{tileHeight * rows}\" viewBox=\"0 0 {tileWidth * columns} {tileHeight * rows}\">");
        svg.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#211d1d\"/>");

        for (var index = 0; index < Cases.Length; index++)
        {
            var source = Cases[index];
            var construction = ConductorConstructionCalculator.TryCalculate(
                source.Description,
                source.Area) ?? throw new InvalidOperationException(source.Description);
            var layout = ConductorPreviewLayoutBuilder.Create(
                construction,
                centerX: 130d,
                centerY: 130d,
                envelopeRadius: 92d);
            var x = index % columns * tileWidth;
            var y = index / columns * tileHeight;
            AppendTile(svg, x, y, source.Description, construction, layout);
        }

        svg.AppendLine("</svg>");
        File.WriteAllText(output, svg.ToString(), Encoding.UTF8);
        Console.WriteLine(output);
        return 0;
    }

    private static void AppendTile(
        StringBuilder svg,
        double tileX,
        double tileY,
        string description,
        ConductorConstructionResult construction,
        ConductorPreviewLayout layout)
    {
        svg.AppendLine($"<g transform=\"translate({F(tileX)} {F(tileY)})\">");
        svg.AppendLine("<rect x=\"8\" y=\"8\" width=\"344\" height=\"374\" rx=\"16\" fill=\"#383232\" stroke=\"#655b5b\"/>");
        svg.AppendLine($"<text x=\"24\" y=\"36\" fill=\"white\" font-family=\"Segoe UI\" font-size=\"18\" font-weight=\"600\">{Escape(description)}</text>");
        svg.AppendLine($"<text x=\"24\" y=\"58\" fill=\"#c9c0c0\" font-family=\"Segoe UI\" font-size=\"12\">{construction.TotalStrandCount:N0} strands · {Escape(layout.PackingKind.ToString())}</text>");

        svg.AppendLine("<circle cx=\"130\" cy=\"130\" r=\"96\" fill=\"#241f1f\" stroke=\"#817575\"/>");
        foreach (var strand in layout.Strands)
        {
            var stroke = Math.Clamp(strand.Radius * 0.09d, 0.08d, 0.7d);
            svg.AppendLine($"<circle cx=\"{F(strand.X)}\" cy=\"{F(strand.Y)}\" r=\"{F(strand.Radius)}\" fill=\"#c7782e\" stroke=\"#f0d2b5\" stroke-width=\"{F(stroke)}\"/>");
        }

        foreach (var group in layout.Groups.Where(group => group.Level == 0))
        {
            svg.AppendLine($"<circle cx=\"{F(group.X)}\" cy=\"{F(group.Y)}\" r=\"{F(group.Radius)}\" fill=\"none\" stroke=\"#fff4df\" stroke-width=\"0.8\"/>");
        }

        AppendSideProfile(svg, layout, construction.IsRopeLay);
        svg.AppendLine("</g>");
    }

    private static void AppendSideProfile(
        StringBuilder svg,
        ConductorPreviewLayout layout,
        bool ropeLay)
    {
        const double startX = 24d;
        const double endX = 292d;
        const double centerY = 310d;
        const double yScale = 0.30d;
        var turns = ropeLay ? 0.18d : 0.28d;
        svg.AppendLine("<text x=\"24\" y=\"250\" fill=\"#c9c0c0\" font-family=\"Segoe UI\" font-size=\"12\">Exposed surface strands · compressed all-strand end face</text>");
        svg.AppendLine("<rect x=\"20\" y=\"278\" width=\"276\" height=\"64\" rx=\"4\" fill=\"#241f1f\" stroke=\"#817575\"/>");

        var surfaces = layout.SurfaceUnits
            .OrderBy(unit => Depth(unit, turns, 0.5d))
            .ToArray();
        foreach (var surface in surfaces)
        {
            var radius = Math.Max(0.55d, surface.Radius * 2d * yScale);
            var points = new StringBuilder();
            var phase = Math.Atan2(surface.Y - 130d, surface.X - 130d);
            var orbit = DistanceFromCenter(surface) * yScale;
            for (var segment = 0; segment <= 72; segment++)
            {
                var progress = segment / 72d;
                var px = startX + (endX - startX) * progress;
                var py = centerY + Math.Sin(phase + progress * turns * 2d * Math.PI) * orbit;
                points.Append(segment == 0 ? $"M {F(px)} {F(py)}" : $" L {F(px)} {F(py)}");
            }

            svg.AppendLine($"<path d=\"{points}\" fill=\"none\" stroke=\"#5d3018\" stroke-width=\"{F(radius + Math.Clamp(radius * 0.16d, 0.22d, 1.3d))}\"/>");
            svg.AppendLine($"<path d=\"{points}\" fill=\"none\" stroke=\"#c7782e\" stroke-width=\"{F(radius)}\"/>");
        }

        var rotation = turns * 2d * Math.PI;
        foreach (var strand in layout.Strands)
        {
            var relativeX = strand.X - 130d;
            var relativeY = strand.Y - 130d;
            var rotatedX = relativeX * Math.Cos(rotation) - relativeY * Math.Sin(rotation);
            var rotatedY = relativeX * Math.Sin(rotation) + relativeY * Math.Cos(rotation);
            var radiusY = Math.Max(0.2d, strand.Radius * yScale);
            var radiusX = Math.Max(0.14d, radiusY * Perspective);
            svg.AppendLine($"<ellipse cx=\"{F(endX + rotatedX * yScale * Perspective)}\" cy=\"{F(centerY + rotatedY * yScale)}\" rx=\"{F(radiusX)}\" ry=\"{F(radiusY)}\" fill=\"#bd6f2b\" stroke=\"#5d3018\" stroke-width=\"0.18\"/>");
        }
    }

    private static double Depth(
        ConductorPreviewCircle surface,
        double turns,
        double progress)
    {
        var phase = Math.Atan2(surface.Y - 130d, surface.X - 130d);
        return Math.Cos(phase + progress * turns * 2d * Math.PI);
    }

    private static double DistanceFromCenter(ConductorPreviewCircle circle) =>
        Math.Sqrt(Math.Pow(circle.X - 130d, 2d) + Math.Pow(circle.Y - 130d, 2d));

    private static string F(double value) => value.ToString("0.###", Invariant);

    private static string Escape(string value) =>
        System.Security.SecurityElement.Escape(value) ?? string.Empty;
}
