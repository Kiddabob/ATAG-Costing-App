using ATAG.Costing.Domain.Conductors;

namespace ATAG.Costing.Application.Visualisation;

public enum ConductorPreviewPackingKind
{
    SingleStrand = 0,
    CompactHexagonal = 1,
    RopeLay = 2,
    FiveCoreElevenOuter = 3,
}

public sealed record ConductorPreviewCircle(
    double X,
    double Y,
    double Radius,
    bool IsBoundary,
    int Level);

public sealed record ConductorPreviewLayout(
    IReadOnlyList<ConductorPreviewCircle> Strands,
    IReadOnlyList<ConductorPreviewCircle> Groups,
    IReadOnlyList<ConductorPreviewCircle> SurfaceUnits,
    ConductorPreviewPackingKind PackingKind);

/// <summary>
/// Builds a close-packed triangular lattice rather than placing each layer on
/// a circular ring. The lattice keeps neighbouring equal strands touching and
/// gives incomplete layers a compact, approximately hexagonal envelope.
/// </summary>
public static class ConductorPreviewLayoutBuilder
{
    public const string RuleVersion = "conductor-preview-packing/v2";

    private const double PackingClearance = 0.97d;
    // Keep rope sub-bundles visually and physically close without allowing
    // adjacent group envelopes to overlap. The per-level lattice already
    // reserves a small 3% edge clearance.
    private const double NestedGroupFill = 0.985d;
    private const double RootThree = 1.7320508075688772d;
    private static readonly object PatternLock = new();
    private static readonly Dictionary<int, IReadOnlyList<PackedPoint>>
        PatternCache = [];
    private static readonly (double X, double Y)[] CandidateCentres =
    [
        (0d, 0d),
        (1d, 0d),
        (0.5d, RootThree / 2d),
        (1d, RootThree / 3d),
        (0d, 2d * RootThree / 3d),
    ];
    private static readonly (int Q, int R)[] Neighbours =
    [
        (1, 0),
        (1, -1),
        (0, -1),
        (-1, 0),
        (-1, 1),
        (0, 1),
    ];

    public static ConductorPreviewLayout Create(
        ConductorConstructionResult construction,
        double centerX,
        double centerY,
        double envelopeRadius)
    {
        ArgumentNullException.ThrowIfNull(construction);
        if (construction.PackingLevels.Count == 0 ||
            construction.PackingLevels.Any(level => level <= 0))
        {
            throw new ArgumentException(
                "The conductor construction has no valid packing hierarchy.",
                nameof(construction));
        }

        if (!double.IsFinite(envelopeRadius) || envelopeRadius <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(envelopeRadius));
        }

        var strands = new List<ConductorPreviewCircle>(
            construction.TotalStrandCount);
        var groups = new List<ConductorPreviewCircle>();
        var root = CreateLevelCircles(
            construction.PackingLevels[0],
            centerX,
            centerY,
            envelopeRadius,
            level: 0);

        if (construction.PackingLevels.Count == 1)
        {
            strands.AddRange(root);
        }
        else
        {
            groups.AddRange(root);
            foreach (var group in root)
            {
                BuildNestedLevel(
                    construction.PackingLevels,
                    level: 1,
                    group.X,
                    group.Y,
                    group.Radius * NestedGroupFill,
                    strands,
                    groups);
            }
        }

        return new ConductorPreviewLayout(
            strands,
            groups,
            root.Where(circle => circle.IsBoundary).ToArray(),
            construction.PackingLevels.Count > 1
                ? ConductorPreviewPackingKind.RopeLay
                : construction.TotalStrandCount == 1
                    ? ConductorPreviewPackingKind.SingleStrand
                    : construction.TotalStrandCount == 16
                        ? ConductorPreviewPackingKind.FiveCoreElevenOuter
                    : ConductorPreviewPackingKind.CompactHexagonal);
    }

    public static decimal EstimatePackedEnvelopeDiameterMillimetres(
        ConductorConstructionResult construction)
    {
        ArgumentNullException.ThrowIfNull(construction);
        var layout = Create(
            construction,
            centerX: 0d,
            centerY: 0d,
            envelopeRadius: 1d);
        var normalizedStrandRadius = layout.Strands
            .Select(strand => strand.Radius)
            .DefaultIfEmpty(0d)
            .Min();
        if (!double.IsFinite(normalizedStrandRadius) ||
            normalizedStrandRadius <= 0d)
        {
            throw new InvalidOperationException(
                "The conductor layout did not produce a usable strand radius.");
        }

        return construction.StrandDiameterMillimetres /
               (decimal)normalizedStrandRadius;
    }

    private static void BuildNestedLevel(
        IReadOnlyList<int> levels,
        int level,
        double centerX,
        double centerY,
        double envelopeRadius,
        ICollection<ConductorPreviewCircle> strands,
        ICollection<ConductorPreviewCircle> groups)
    {
        var circles = CreateLevelCircles(
            levels[level],
            centerX,
            centerY,
            envelopeRadius,
            level);
        if (level == levels.Count - 1)
        {
            foreach (var strand in circles)
            {
                strands.Add(strand);
            }

            return;
        }

        foreach (var group in circles)
        {
            groups.Add(group);
            BuildNestedLevel(
                levels,
                level + 1,
                group.X,
                group.Y,
                group.Radius * NestedGroupFill,
                strands,
                groups);
        }
    }

    private static IReadOnlyList<ConductorPreviewCircle> CreateLevelCircles(
        int count,
        double centerX,
        double centerY,
        double envelopeRadius,
        int level)
    {
        var pattern = GetPattern(count);
        return pattern
            .Select(point =>
                new ConductorPreviewCircle(
                    centerX + point.X * envelopeRadius,
                    centerY + point.Y * envelopeRadius,
                    point.Radius * envelopeRadius,
                    point.IsBoundary,
                    level))
            .ToArray();
    }

    private static IReadOnlyList<PackedPoint> GetPattern(int count)
    {
        lock (PatternLock)
        {
            if (!PatternCache.TryGetValue(count, out var pattern))
            {
                pattern = CreatePackedPattern(count);
                PatternCache.Add(count, pattern);
            }

            return pattern;
        }
    }

    private static IReadOnlyList<PackedPoint> CreatePackedPattern(int count)
    {
        if (count <= 0)
        {
            return [];
        }

        if (count == 1)
        {
            return [new PackedPoint(0d, 0d, 1d, true)];
        }

        if (count == 16)
        {
            return CreateFiveCoreElevenOuterPattern();
        }

        var approximateRing = (int)Math.Ceiling(
            (Math.Sqrt(12d * count - 3d) - 3d) / 6d);
        var extent = Math.Max(3, approximateRing + 4);
        var lattice = new List<LatticePoint>(
            (2 * extent + 1) * (2 * extent + 1));
        for (var q = -extent; q <= extent; q++)
        {
            for (var r = -extent; r <= extent; r++)
            {
                lattice.Add(
                    new LatticePoint(
                        q,
                        r,
                        2d * q + r,
                        RootThree * r));
            }
        }

        ClusterCandidate? best = null;
        foreach (var centre in CandidateCentres)
        {
            var candidate = CreateCandidateCluster(
                lattice,
                count,
                centre.X,
                centre.Y);
            if (best is null || candidate.Score < best.Score)
            {
                best = candidate;
            }
        }

        return best!.Points;
    }

    private static IReadOnlyList<PackedPoint>
        CreateFiveCoreElevenOuterPattern()
    {
        const double strandRadius = 1d;
        var innerRadius = strandRadius / Math.Sin(Math.PI / 5d);
        var minimumPhaseSeparation = Math.PI / 55d;
        var outerRadius =
            innerRadius * Math.Cos(minimumPhaseSeparation) +
            Math.Sqrt(
                4d * strandRadius * strandRadius -
                innerRadius * innerRadius *
                Math.Pow(Math.Sin(minimumPhaseSeparation), 2d)) +
            0.000001d;
        var scale = PackingClearance / (outerRadius + strandRadius);
        var points = new List<PackedPoint>(16);

        for (var index = 0; index < 5; index++)
        {
            var angle = -Math.PI / 2d + index * 2d * Math.PI / 5d;
            points.Add(
                new PackedPoint(
                    Math.Cos(angle) * innerRadius * scale,
                    Math.Sin(angle) * innerRadius * scale,
                    strandRadius * scale,
                    IsBoundary: false));
        }

        for (var index = 0; index < 11; index++)
        {
            var angle =
                -Math.PI / 2d +
                minimumPhaseSeparation +
                index * 2d * Math.PI / 11d;
            points.Add(
                new PackedPoint(
                    Math.Cos(angle) * outerRadius * scale,
                    Math.Sin(angle) * outerRadius * scale,
                    strandRadius * scale,
                    IsBoundary: true));
        }

        return points;
    }

    private static ClusterCandidate CreateCandidateCluster(
        IReadOnlyList<LatticePoint> lattice,
        int count,
        double candidateCenterX,
        double candidateCenterY)
    {
        var selected = new List<LatticePoint>(count);
        var distanceGroups = lattice
            .Select(point =>
                new
                {
                    Point = point,
                    Distance = Math.Round(
                        Math.Pow(point.X - candidateCenterX, 2d) +
                        Math.Pow(point.Y - candidateCenterY, 2d),
                        9),
                })
            .GroupBy(item => item.Distance)
            .OrderBy(group => group.Key);

        foreach (var distanceGroup in distanceGroups)
        {
            var points = distanceGroup
                .Select(item => item.Point)
                .OrderBy(point =>
                    Math.Atan2(
                        point.Y - candidateCenterY,
                        point.X - candidateCenterX))
                .ToArray();
            var remaining = count - selected.Count;
            if (remaining >= points.Length)
            {
                selected.AddRange(points);
            }
            else
            {
                selected.AddRange(SelectEvenly(points, remaining));
            }

            if (selected.Count == count)
            {
                break;
            }
        }

        var centroidX = selected.Average(point => point.X);
        var centroidY = selected.Average(point => point.Y);
        var selectedCoordinates = selected
            .Select(point => (point.Q, point.R))
            .ToHashSet();
        var centered = selected
            .Select(point =>
                new CenteredPoint(
                    point,
                    point.X - centroidX,
                    point.Y - centroidY,
                    Neighbours.Any(neighbour =>
                        !selectedCoordinates.Contains(
                            (point.Q + neighbour.Q,
                             point.R + neighbour.R)))))
            .ToArray();
        var maximumEnvelope = centered.Max(point =>
            Math.Sqrt(point.X * point.X + point.Y * point.Y) + 1d);
        var radius = PackingClearance / maximumEnvelope;
        var width = centered.Max(point => point.X) -
                    centered.Min(point => point.X) + 2d;
        var height = centered.Max(point => point.Y) -
                     centered.Min(point => point.Y) + 2d;
        var aspectPenalty = Math.Abs(Math.Log(width / height));
        var contactCount = selected.Sum(point =>
            Neighbours.Count(neighbour =>
                selectedCoordinates.Contains(
                    (point.Q + neighbour.Q,
                     point.R + neighbour.R)))) / 2;
        var score =
            maximumEnvelope * (1d + 0.06d * aspectPenalty) -
            contactCount * 0.00001d;
        var packed = centered
            .Select(point =>
                new PackedPoint(
                    point.X * radius,
                    point.Y * radius,
                    radius,
                    point.IsBoundary))
            .ToArray();
        return new ClusterCandidate(packed, score);
    }

    private static IEnumerable<LatticePoint> SelectEvenly(
        IReadOnlyList<LatticePoint> points,
        int count)
    {
        if (count <= 0)
        {
            yield break;
        }

        for (var index = 0; index < count; index++)
        {
            var selectedIndex = (int)Math.Floor(
                (index + 0.5d) * points.Count / count);
            yield return points[Math.Min(selectedIndex, points.Count - 1)];
        }
    }

    private sealed record LatticePoint(
        int Q,
        int R,
        double X,
        double Y);

    private sealed record CenteredPoint(
        LatticePoint Source,
        double X,
        double Y,
        bool IsBoundary);

    private sealed record PackedPoint(
        double X,
        double Y,
        double Radius,
        bool IsBoundary);

    private sealed record ClusterCandidate(
        IReadOnlyList<PackedPoint> Points,
        double Score);
}
