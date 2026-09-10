using System.Globalization;
using System.Numerics;
using System.Text;
using ATAG.Costing.Application.Visualisation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace ATAG.Costing.WinUI;

/// <summary>
/// One native vector image per view preserves depth order without a XAML visual
/// per strand, triangle or turn. Ordered physical outlines are serialized off
/// the UI thread; WinUI handles vector decoding asynchronously.
/// </summary>
internal static class UnifiedPreviewDrawing
{
    public static byte[] Serialize(CablePreviewDrawing drawing, bool crossSection,
        CancellationToken cancellation)
    {
        const double width = 520, height = 220, margin = 14;
        var extent = drawing.MaximumMm - drawing.MinimumMm;
        var scale = Math.Min((width - 2 * margin) / Math.Max(0.01, extent.X),
            (height - 2 * margin) / Math.Max(0.01, extent.Y));
        var centre = (drawing.MaximumMm + drawing.MinimumMm) / 2;
        var svg = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"520\" height=\"220\" viewBox=\"0 0 520 220\">");
        foreach (var path in drawing.Paths)
        {
            cancellation.ThrowIfCancellationRequested();
            if (path.Points.Length < 3) continue;
            var colour = path.Colour;
            svg.Append(CultureInfo.InvariantCulture,
                $"<path fill=\"#{Channel(colour.X):X2}{Channel(colour.Y):X2}{Channel(colour.Z):X2}\" fill-opacity=\"{Math.Clamp(colour.W, 0, 1):0.###}\" fill-rule=\"evenodd\"");
            if (crossSection) svg.Append(" stroke=\"#18232d\" stroke-width=\"0.4\"");
            svg.Append(" d=\"");
            AppendFigure(path.Points);
            if (path.InnerPoints.Length >= 3) AppendFigure(path.InnerPoints);
            svg.Append("\"/>");
        }
        svg.Append("</svg>");
        return Encoding.UTF8.GetBytes(svg.ToString());

        void AppendFigure(IEnumerable<Vector2> points)
        {
            var first = true;
            foreach (var point in points)
            {
                svg.Append(first ? 'M' : 'L');
                svg.Append((width / 2 + (point.X - centre.X) * scale).ToString("0.###", CultureInfo.InvariantCulture));
                svg.Append(' ');
                svg.Append((height / 2 - (point.Y - centre.Y) * scale).ToString("0.###", CultureInfo.InvariantCulture));
                first = false;
            }
            svg.Append('Z');
        }
    }

    public static async Task<FrameworkElement> CreateAsync(byte[] bytes, string title,
        CancellationToken cancellation)
    {
        var source = new SvgImageSource { RasterizePixelWidth = 1040, RasterizePixelHeight = 440 };
        using var stream = new InMemoryRandomAccessStream();
        using var writer = new DataWriter(stream);
        writer.WriteBytes(bytes);
        await writer.StoreAsync();
        stream.Seek(0);
        var status = await source.SetSourceAsync(stream);
        cancellation.ThrowIfCancellationRequested();
        if (status != SvgImageSourceLoadStatus.Success)
            throw new InvalidOperationException($"The vector view could not load ({status}).");
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = title, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 225, 233, 239)),
            CornerRadius = new CornerRadius(8),
            Child = new Image
            {
                Source = source,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MaxHeight = 220,
            },
        });
        return panel;
    }

    private static byte Channel(float value) => (byte)(Math.Clamp(value, 0, 1) * 255);
}
