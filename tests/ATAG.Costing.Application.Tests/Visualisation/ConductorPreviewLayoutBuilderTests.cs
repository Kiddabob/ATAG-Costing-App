using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.Domain.Conductors;
using Xunit;

namespace ATAG.Costing.Application.Tests.Visualisation;

public sealed class ConductorPreviewLayoutBuilderTests
{
    [Fact]
    public void SixteenEndCopper_UsesFiveCoreAndElevenOuterStrands()
    {
        var construction = ConductorConstructionCalculator.TryCalculate(
            "16/0.196 TCW (M)",
            0.5m);

        Assert.NotNull(construction);
        var layout = ConductorPreviewLayoutBuilder.Create(
            construction,
            centerX: 0d,
            centerY: 0d,
            envelopeRadius: 50d);

        Assert.Equal(
            ConductorPreviewPackingKind.FiveCoreElevenOuter,
            layout.PackingKind);
        Assert.Equal(16, layout.Strands.Count);
        Assert.Equal(5, layout.Strands.Count(strand => !strand.IsBoundary));
        Assert.Equal(11, layout.SurfaceUnits.Count);
        AssertEveryCircleHasNearbyNeighbour(layout.Strands, 1.05d);
        AssertNoOverlaps(layout.Strands);

        var width = layout.Strands.Max(strand => strand.X + strand.Radius) -
                    layout.Strands.Min(strand => strand.X - strand.Radius);
        var height = layout.Strands.Max(strand => strand.Y + strand.Radius) -
                     layout.Strands.Min(strand => strand.Y - strand.Radius);
        Assert.InRange(width / height, 0.75d, 1.35d);
    }

    [Fact]
    public void PackedEnvelopeEstimate_UsesTheParsedPhysicalStrandDiameter()
    {
        var construction = ConductorConstructionCalculator.TryCalculate(
            "32/0.196 TCW (H)",
            0m);

        Assert.NotNull(construction);
        var diameter =
            ConductorPreviewLayoutBuilder
                .EstimatePackedEnvelopeDiameterMillimetres(construction);

        Assert.InRange(diameter, 1.1m, 2m);
    }

    [Fact]
    public void SevenByNineteenRope_UsesSixExposedGroupsAndAllStrands()
    {
        var construction = ConductorConstructionCalculator.TryCalculate(
            "7x19/0.32 H72 Strand (25mm2)",
            25m);

        Assert.NotNull(construction);
        var layout = ConductorPreviewLayoutBuilder.Create(
            construction,
            centerX: 0d,
            centerY: 0d,
            envelopeRadius: 50d);

        Assert.Equal(ConductorPreviewPackingKind.RopeLay, layout.PackingKind);
        Assert.Equal(133, layout.Strands.Count);
        Assert.Equal(7, layout.Groups.Count);
        Assert.Equal(6, layout.SurfaceUnits.Count);
        AssertNoOverlaps(layout.Strands);
        AssertEveryCircleHasTouchingNeighbour(layout.Strands);
        Assert.All(
            layout.SurfaceUnits,
            group => Assert.True(group.IsBoundary));
    }

    [Fact]
    public void RepresentativeNumericConstructions_AllProduceCompleteLayouts()
    {
        var examples = new[]
        {
            "4/0.10 TCW",
            "7/0.20 TCW",
            "16/0.196 TCW",
            "19/0.10 TCW",
            "32/0.196 TCW",
            "7x19/0.32 H72 Strand",
        };

        foreach (var description in examples)
        {
            var construction = ConductorConstructionCalculator.TryCalculate(
                description,
                0m);
            Assert.NotNull(construction);

            var layout = ConductorPreviewLayoutBuilder.Create(
                construction,
                centerX: 0d,
                centerY: 0d,
                envelopeRadius: 50d);
            Assert.Equal(construction.TotalStrandCount, layout.Strands.Count);
            Assert.NotEmpty(layout.SurfaceUnits);
            AssertNoOverlaps(layout.Strands);
            if (layout.PackingKind ==
                ConductorPreviewPackingKind.FiveCoreElevenOuter)
            {
                AssertEveryCircleHasNearbyNeighbour(layout.Strands, 1.05d);
            }
            else if (layout.Strands.Count > 1)
            {
                AssertEveryCircleHasTouchingNeighbour(layout.Strands);
            }

            Assert.All(
                layout.Strands,
                strand => Assert.True(
                    Math.Sqrt(strand.X * strand.X + strand.Y * strand.Y) +
                    strand.Radius <=
                    50.000001d,
                    $"{description} exceeded its preview envelope."));
        }
    }

    private static void AssertEveryCircleHasTouchingNeighbour(
        IReadOnlyList<ConductorPreviewCircle> circles)
    {
        var radius = circles[0].Radius;
        var cellSize = Math.Max(0.0000001d, radius * 2.01d);
        var cells = circles
            .Select((circle, index) => new
            {
                Circle = circle,
                Index = index,
                CellX = (int)Math.Floor(circle.X / cellSize),
                CellY = (int)Math.Floor(circle.Y / cellSize),
            })
            .GroupBy(item => (item.CellX, item.CellY))
            .ToDictionary(group => group.Key, group => group.ToArray());

        for (var index = 0; index < circles.Count; index++)
        {
            var subject = circles[index];
            var cellX = (int)Math.Floor(subject.X / cellSize);
            var cellY = (int)Math.Floor(subject.Y / cellSize);
            var touching = false;
            for (var xOffset = -1; xOffset <= 1 && !touching; xOffset++)
            {
                for (var yOffset = -1; yOffset <= 1 && !touching; yOffset++)
                {
                    if (!cells.TryGetValue(
                            (cellX + xOffset, cellY + yOffset),
                            out var candidates))
                    {
                        continue;
                    }

                    touching = candidates.Any(candidate =>
                        candidate.Index != index &&
                        Math.Abs(
                            Distance(subject, candidate.Circle) -
                            (subject.Radius + candidate.Circle.Radius)) <
                        0.00001d);
                }
            }

            Assert.True(touching, $"Preview circle {index} is isolated.");
        }
    }

    private static bool HasTouchingNeighbour(
        ConductorPreviewCircle subject,
        IReadOnlyList<ConductorPreviewCircle> circles) =>
        circles.Any(candidate =>
            !ReferenceEquals(subject, candidate) &&
            Math.Abs(
                Distance(subject, candidate) -
            (subject.Radius + candidate.Radius)) <
            0.00001d);

    private static void AssertEveryCircleHasNearbyNeighbour(
        IReadOnlyList<ConductorPreviewCircle> circles,
        double maximumGapFactor)
    {
        Assert.DoesNotContain(
            circles,
            subject => !circles.Any(candidate =>
                !ReferenceEquals(subject, candidate) &&
                Distance(subject, candidate) <=
                (subject.Radius + candidate.Radius) * maximumGapFactor));
    }

    private static void AssertNoOverlaps(
        IReadOnlyList<ConductorPreviewCircle> circles)
    {
        var ordered = circles
            .OrderBy(circle => circle.X - circle.Radius)
            .ToArray();
        for (var left = 0; left < ordered.Length; left++)
        {
            var leftCircle = ordered[left];
            var leftEdge = leftCircle.X + leftCircle.Radius;
            for (var right = left + 1; right < ordered.Length; right++)
            {
                var rightCircle = ordered[right];
                if (rightCircle.X - rightCircle.Radius >
                    leftEdge + 0.00001d)
                {
                    break;
                }

                if (Math.Abs(leftCircle.Y - rightCircle.Y) + 0.00001d >=
                    leftCircle.Radius + rightCircle.Radius)
                {
                    continue;
                }

                Assert.True(
                    Distance(leftCircle, rightCircle) + 0.00001d >=
                    leftCircle.Radius + rightCircle.Radius,
                    $"Preview circles {left} and {right} overlap.");
            }
        }
    }

    private static double Distance(
        ConductorPreviewCircle left,
        ConductorPreviewCircle right) =>
        Math.Sqrt(
            Math.Pow(left.X - right.X, 2d) +
            Math.Pow(left.Y - right.Y, 2d));
}
