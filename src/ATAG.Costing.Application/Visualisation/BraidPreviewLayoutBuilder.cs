namespace ATAG.Costing.Application.Visualisation;

public sealed record BraidPreviewPoint(double X, double Y);

public sealed record BraidPreviewPolyline(
    IReadOnlyList<BraidPreviewPoint> Points);

public sealed record BraidPreviewFamilyLayout(
    IReadOnlyList<BraidPreviewPolyline> Curves,
    IReadOnlyList<BraidPreviewPolyline> OverpassSegments);

public sealed record BraidPreviewLayout(
    double VisualPitch,
    double FaceThickness,
    double ShadowThickness,
    int SampleCount,
    BraidPreviewFamilyLayout Clockwise,
    BraidPreviewFamilyLayout CounterClockwise)
{
    public int FullCurveCount =>
        Clockwise.Curves.Count + CounterClockwise.Curves.Count;

    public int TotalPointCount =>
        Clockwise.Curves.Sum(curve => curve.Points.Count) +
        CounterClockwise.Curves.Sum(curve => curve.Points.Count) +
        Clockwise.OverpassSegments.Sum(curve => curve.Points.Count) +
        CounterClockwise.OverpassSegments.Sum(curve => curve.Points.Count);
}

/// <summary>
/// Produces one bounded, reusable vector scene for the LIVE Braid Preview.
/// Carrier geometry is generated once per input revision; the WinUI renderer
/// reuses that geometry for the face, shadow and alternating overpass layers.
/// </summary>
public static class BraidPreviewLayoutBuilder
{
    public const string RuleVersion = "braid-preview-layout/v1";

    private const int MinimumSamples = 48;
    private const int MaximumSamples = 128;

    public static BraidPreviewLayout Create(
        double width,
        double height,
        double physicalPitchMillimetres,
        double coreOutsideDiameterMillimetres,
        int carrierCount,
        int endsPerCarrier,
        double wireDiameterMillimetres,
        bool detailed)
    {
        ValidatePositiveFinite(width, nameof(width));
        ValidatePositiveFinite(height, nameof(height));
        ValidatePositiveFinite(
            physicalPitchMillimetres,
            nameof(physicalPitchMillimetres));
        ValidatePositiveFinite(
            coreOutsideDiameterMillimetres,
            nameof(coreOutsideDiameterMillimetres));
        ValidatePositiveFinite(
            wireDiameterMillimetres,
            nameof(wireDiameterMillimetres));
        if (carrierCount is not 16 and not 24)
        {
            throw new ArgumentOutOfRangeException(nameof(carrierCount));
        }

        if (endsPerCarrier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endsPerCarrier));
        }

        var strandThickness = Math.Clamp(
            wireDiameterMillimetres /
            coreOutsideDiameterMillimetres * height,
            detailed ? 0.75d : 1.6d,
            detailed ? 2.2d : 5.4d);
        var visualPitch = Math.Clamp(
            physicalPitchMillimetres /
            coreOutsideDiameterMillimetres * height * 0.72d,
            34d,
            170d);
        var visibleCarriers = Math.Clamp(carrierCount / 2, 4, 12);
        var renderedEnds = detailed
            ? Math.Clamp(endsPerCarrier, 1, 10)
            : 1;
        var faceThickness = detailed
            ? strandThickness
            : Math.Clamp(
                strandThickness * Math.Sqrt(endsPerCarrier) * 1.65d,
                2.4d,
                9d);
        var sampleCount = Math.Clamp(
            (int)Math.Ceiling(width / visualPitch * 12d),
            MinimumSamples,
            MaximumSamples);
        var bandWidth = Math.Clamp(visualPitch / 4d, 8d, 24d);

        var clockwise = CreateFamily(
            width,
            height,
            visualPitch,
            bandWidth,
            visibleCarriers,
            renderedEnds,
            strandThickness,
            sampleCount,
            direction: 1d,
            includeOverpasses: true);
        var counterClockwise = CreateFamily(
            width,
            height,
            visualPitch,
            bandWidth,
            visibleCarriers,
            renderedEnds,
            strandThickness,
            sampleCount,
            direction: -1d,
            includeOverpasses: false);

        return new BraidPreviewLayout(
            visualPitch,
            faceThickness,
            faceThickness + (detailed ? 0.65d : 1.1d),
            sampleCount,
            clockwise,
            counterClockwise);
    }

    private static BraidPreviewFamilyLayout CreateFamily(
        double width,
        double height,
        double pitch,
        double bandWidth,
        int visibleCarriers,
        int renderedEnds,
        double strandThickness,
        int sampleCount,
        double direction,
        bool includeOverpasses)
    {
        var curves = new List<BraidPreviewPolyline>(
            visibleCarriers * renderedEnds);
        var overpasses = new List<BraidPreviewPolyline>();
        var phaseStep = Math.PI * 2d / visibleCarriers;

        for (var carrier = 0; carrier < visibleCarriers; carrier++)
        {
            var phase = carrier * phaseStep;
            for (var end = 0; end < renderedEnds; end++)
            {
                var verticalOffset = renderedEnds == 1
                    ? 0d
                    : (end - ((renderedEnds - 1d) / 2d)) *
                      strandThickness * 0.72d;
                var points = CreateCurve(
                    width,
                    height,
                    pitch,
                    phase,
                    direction,
                    verticalOffset,
                    sampleCount);
                curves.Add(new BraidPreviewPolyline(points));

                if (includeOverpasses)
                {
                    AddAlternatingOverpasses(
                        points,
                        bandWidth,
                        overpasses);
                }
            }
        }

        return new BraidPreviewFamilyLayout(curves, overpasses);
    }

    private static IReadOnlyList<BraidPreviewPoint> CreateCurve(
        double width,
        double height,
        double pitch,
        double phase,
        double direction,
        double verticalOffset,
        int sampleCount)
    {
        var points = new BraidPreviewPoint[sampleCount + 1];
        var centre = height / 2d;
        var amplitude = Math.Max((height / 2d) - 4d, 4d);
        for (var index = 0; index <= sampleCount; index++)
        {
            var x = width * index / sampleCount;
            var angle = direction * ((Math.PI * 2d * x / pitch) + phase);
            points[index] = new BraidPreviewPoint(
                x,
                centre + Math.Sin(angle) * amplitude + verticalOffset);
        }

        return points;
    }

    private static void AddAlternatingOverpasses(
        IReadOnlyList<BraidPreviewPoint> points,
        double bandWidth,
        ICollection<BraidPreviewPolyline> target)
    {
        List<BraidPreviewPoint>? segment = null;
        foreach (var point in points)
        {
            var isOverpass = ((int)Math.Floor(point.X / bandWidth) & 1) == 0;
            if (isOverpass)
            {
                segment ??= [];
                segment.Add(point);
                continue;
            }

            CommitSegment(segment, target);
            segment = null;
        }

        CommitSegment(segment, target);
    }

    private static void CommitSegment(
        IReadOnlyList<BraidPreviewPoint>? segment,
        ICollection<BraidPreviewPolyline> target)
    {
        if (segment is { Count: >= 2 })
        {
            target.Add(new BraidPreviewPolyline(segment));
        }
    }

    private static void ValidatePositiveFinite(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
