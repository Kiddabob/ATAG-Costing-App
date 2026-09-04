using ATAG.Costing.Domain.Braiding;

namespace ATAG.Costing.Application.Visualisation;

public sealed record CoreLayupPreviewCore(
    double X,
    double Y,
    double Radius,
    int LayerIndex,
    double PhaseRadians,
    bool IsCentral);

public sealed record CoreLayupPreviewLayout(
    IReadOnlyList<CoreLayupPreviewCore> Cores,
    IReadOnlyList<int> LayerCounts,
    double CompositeRadius)
{
    public int CoreCount => Cores.Count;
}

/// <summary>
/// Builds a bounded equal-core visual layout from the retained workbook
/// bunch-group definition. It represents the cable cores themselves; it does
/// not invent a central carrier, former or sheath.
/// </summary>
public static class CoreLayupPreviewLayoutBuilder
{
    public static CoreLayupPreviewLayout Create(
        BraidCoreLayout definition,
        double width,
        double height)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!double.IsFinite(width) || width <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
        if (!double.IsFinite(height) || height <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var layerCounts = ParseLayerCounts(definition);
        var raw = new List<(double X, double Y, int Layer, double Phase, bool Centre)>();
        var startsWithCentre = layerCounts[0] == 1;
        for (var layerIndex = 0; layerIndex < layerCounts.Count; layerIndex++)
        {
            var count = layerCounts[layerIndex];
            var ringRadius = RingRadius(layerIndex, startsWithCentre, count);
            for (var coreIndex = 0; coreIndex < count; coreIndex++)
            {
                var phase = count == 1 && ringRadius == 0d
                    ? 0d
                    : (-Math.PI / 2d) + (Math.Tau * coreIndex / count);
                raw.Add((
                    ringRadius * Math.Cos(phase),
                    ringRadius * Math.Sin(phase),
                    layerIndex,
                    phase,
                    ringRadius == 0d));
            }
        }

        var rawExtent = raw.Count == 0
            ? 1d
            : raw.Max(core => Math.Sqrt((core.X * core.X) + (core.Y * core.Y))) + 1d;
        var compositeRadius = Math.Min(width, height) * 0.44d;
        var scale = compositeRadius / rawExtent;
        var centreX = width / 2d;
        var centreY = height / 2d;
        var cores = raw.Select(core => new CoreLayupPreviewCore(
            centreX + (core.X * scale),
            centreY + (core.Y * scale),
            scale,
            core.Layer,
            core.Phase,
            core.Centre)).ToArray();

        return new CoreLayupPreviewLayout(
            cores,
            layerCounts,
            compositeRadius);
    }

    private static IReadOnlyList<int> ParseLayerCounts(BraidCoreLayout definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Layup))
        {
            return [definition.CoreCount];
        }

        var parsed = definition.Layup
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var count) ? count : 0)
            .ToArray();
        if (parsed.Length == 0 ||
            parsed.Any(count => count <= 0) ||
            parsed.Sum() != definition.CoreCount)
        {
            throw new ArgumentException(
                "The retained core lay-up does not match its core count.",
                nameof(definition));
        }

        return parsed;
    }

    private static double RingRadius(
        int layerIndex,
        bool startsWithCentre,
        int count)
    {
        if (layerIndex == 0)
        {
            return count == 1 ? 0d : 1d;
        }

        return startsWithCentre
            ? layerIndex * 2d
            : 1d + (layerIndex * 2d);
    }
}
