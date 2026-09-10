using System.Collections.Immutable;
using System.Numerics;

namespace ATAG.Costing.Application.Visualisation;

/// <summary>
/// One bounded presentation geometry boundary shared by vector views and the GPU.
/// A failed budget rejects the whole scene; primary cores or material layers are never dropped.
/// </summary>
public static class CablePreviewGeometry
{
    public static void Validate(CablePreviewScene scene, CablePreviewGeometryBudget? budget = null)
    {
        ArgumentNullException.ThrowIfNull(scene);
        budget ??= new();
        if (budget.MaximumComponents < 1 || budget.MaximumPathPoints < 2 || budget.MaximumVertices < 16 || budget.MaximumIndices < 24)
            throw new ArgumentOutOfRangeException(nameof(budget));
        if (scene.Components.IsDefault || scene.Components.Length > budget.MaximumComponents)
            throw new CablePreviewGeometryLimitException("The scene exceeds the LIVE Preview component budget.");
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        long totalPoints = 0;
        foreach (var component in scene.Components)
        {
            if (component is null || string.IsNullOrWhiteSpace(component.Id) || !identifiers.Add(component.Id))
                throw new ArgumentException("Every preview component must have a unique identifier.", nameof(scene));
            var section = component.Section;
            if (section is null || !Positive(section.WidthMm) || !Positive(section.HeightMm)
                || !Nonnegative(section.InnerWidthMm) || !Nonnegative(section.InnerHeightMm)
                || (section.InnerWidthMm == 0) != (section.InnerHeightMm == 0)
                || section.InnerWidthMm >= section.WidthMm || section.InnerHeightMm >= section.HeightMm
                || !Enum.IsDefined(section.Shape))
                throw new ArgumentException("Preview sections require finite positive dimensions and a smaller paired inner section.", nameof(scene));
            if (component.DetailOnly && component.SimpleOnly)
                throw new ArgumentException("A preview component cannot be both Simple-only and Detailed-only.", nameof(scene));
            if (!Finite(component.Colour) || component.Colour.X < 0 || component.Colour.X > 1
                || component.Colour.Y < 0 || component.Colour.Y > 1 || component.Colour.Z < 0 || component.Colour.Z > 1
                || component.Colour.W < 0 || component.Colour.W > 1)
                throw new ArgumentException("Preview colour channels must be finite values between zero and one.", nameof(scene));
            if (component.CrossSectionCentreMm is { } centre && (!float.IsFinite(centre.X) || !float.IsFinite(centre.Y)))
                throw new ArgumentException("Preview end-view centres must be finite.", nameof(scene));
            if (component.PathMm.IsDefault || component.PathMm.Length < 2)
                throw new ArgumentException("Preview paths require at least two distinct points.", nameof(scene));
            totalPoints += component.PathMm.Length;
            if (totalPoints > budget.MaximumPathPoints)
                throw new CablePreviewGeometryLimitException("The scene exceeds the LIVE Preview centreline budget.");
            for (var index = 0; index < component.PathMm.Length; index++)
            {
                var point = component.PathMm[index];
                if (!Finite(point) || Math.Abs(point.X) > 1000000 || Math.Abs(point.Y) > 1000000 || Math.Abs(point.Z) > 1000000)
                    throw new ArgumentException("Preview paths require finite, bounded coordinates.", nameof(scene));
                if (index > 0 && Vector3.DistanceSquared(point, component.PathMm[index - 1]) < 1e-12f)
                    throw new ArgumentException("Adjacent preview path points must be distinct.", nameof(scene));
            }
        }
        if (scene.Print is { } print)
        {
            if (!Positive(print.SurfaceDiameterMm) || !Positive(print.DotDiameterMm)
                || !double.IsFinite(print.StartZMm) || Math.Abs(print.StartZMm) > 1000000
                || print.Layout is null || print.Layout.Dots.IsDefault
                || !Finite(print.Colour) || print.Colour.X < 0 || print.Colour.X > 1
                || print.Colour.Y < 0 || print.Colour.Y > 1 || print.Colour.Z < 0 || print.Colour.Z > 1
                || print.Colour.W < 0 || print.Colour.W > 1)
                throw new ArgumentException("Print requires finite supplied diameter, dot size, position and colour.", nameof(scene));
            if (print.Layout.Dots.Length > 20000)
                throw new CablePreviewGeometryLimitException("A LIVE Preview supports at most 20,000 print dots.");
            if (!Nonnegative(print.Layout.PrintedHeightMm) || print.Layout.PrintedHeightMm > Math.PI * print.SurfaceDiameterMm)
                throw new ArgumentException("The print height must fit the supplied cable circumference.", nameof(scene));
            foreach (var dot in print.Layout.Dots)
                if (!float.IsFinite(dot.X) || !float.IsFinite(dot.Y) || dot.X < 0 || Math.Abs(dot.X + print.StartZMm) > 1000000
                    || Math.Abs(dot.Y) + print.DotDiameterMm / 2 > Math.PI * print.SurfaceDiameterMm / 2 + .001)
                    throw new ArgumentException("Print dot positions must be finite and fit the supplied cylindrical surface.", nameof(scene));
        }
    }

    public static CablePreviewMesh BuildMesh(CablePreviewScene scene, bool detailed = true,
        CancellationToken cancellationToken = default, CablePreviewGeometryBudget? budget = null)
    {
        budget ??= new();
        cancellationToken.ThrowIfCancellationRequested();
        Validate(scene, budget);
        var components = Visible(scene, detailed).Where(component => component.ThreeDimensionalVisible).ToArray();
        var radialSegments = detailed ? 16 : 8;
        var dotSegments = detailed ? 8 : 6;
        var notes = scene.Notes.IsDefault ? new List<string>() : scene.Notes.ToList();
        if (scene.Print is { } printed) notes.AddRange(printed.Layout.Notes);
        while (!Fits(components, radialSegments, budget, scene.Print, dotSegments) && radialSegments > 6) radialSegments -= 2;
        while (!Fits(components, radialSegments, budget, scene.Print, dotSegments) && dotSegments > 4) dotSegments -= 2;
        if (!Fits(components, radialSegments, budget, scene.Print, dotSegments))
            throw new CablePreviewGeometryLimitException("This scene exceeds the 3D geometry budget. Use Simple mode or a shorter representative length.");
        if (radialSegments < (detailed ? 16 : 8)) notes.Add("Circular surface detail was reduced to keep the complete scene within the rendering budget.");
        if (dotSegments < (detailed ? 8 : 6)) notes.Add("Print dots use fewer edge segments to preserve every dot within the rendering budget.");
        var vertices = new List<CablePreviewVertex>();
        var indices = new List<uint>();
        foreach (var component in components)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Sweep(component, radialSegments, vertices, indices, cancellationToken);
        }
        if (scene.Print is { } meshPrint) AddPrintMesh(meshPrint, dotSegments, vertices, indices, cancellationToken);
        if (vertices.Count == 0) return new([], [], Vector3.Zero, Vector3.Zero, notes.ToImmutableArray());
        var minimum = vertices[0].Position;
        var maximum = minimum;
        foreach (var vertex in vertices)
        {
            minimum = Vector3.Min(minimum, vertex.Position);
            maximum = Vector3.Max(maximum, vertex.Position);
        }
        var centre = (minimum + maximum) / 2;
        var size = maximum - minimum;
        var scale = 6f / Math.Max(size.X, Math.Max(size.Y, size.Z));
        for (var index = 0; index < vertices.Count; index++)
            vertices[index] = vertices[index] with { Position = (vertices[index].Position - centre) * scale };
        return new(vertices.ToImmutableArray(), indices.ToImmutableArray(), minimum, maximum, notes.ToImmutableArray());
    }

    public static CablePreviewDrawing BuildDrawing(CablePreviewScene scene, CablePreviewProjection projection,
        bool detailed = true, CancellationToken cancellationToken = default, CablePreviewGeometryBudget? budget = null)
    {
        budget ??= new();
        cancellationToken.ThrowIfCancellationRequested();
        Validate(scene, budget);
        if (!Enum.IsDefined(projection)) throw new ArgumentOutOfRangeException(nameof(projection));
        var drawingNotes = (scene.Notes.IsDefault ? [] : scene.Notes).AddRange(scene.Print?.Layout.Notes ?? []);
        var sideSegments = detailed ? 16 : 8;
        if (projection == CablePreviewProjection.Side)
        {
            var visible = Visible(scene, detailed).Where(component => component.SideViewVisible).ToArray();
            while (EstimateSidePoints(visible, sideSegments, scene.Print) > budget.MaximumVertices && sideSegments > 4)
                sideSegments -= 2;
            if (EstimateSidePoints(visible, sideSegments, scene.Print) > budget.MaximumVertices)
                throw new CablePreviewGeometryLimitException("The complete scene exceeds the 2D path budget. Use a shorter representative length.");
            if (sideSegments < (detailed ? 16 : 8))
                drawingNotes = drawingNotes.Add("Side-view wire edges use reduced surface detail to preserve every strand within the drawing budget.");
        }
        var paths = ImmutableArray.CreateBuilder<CablePreviewPath2D>();
        long pointCount = 0;
        foreach (var component in Visible(scene, detailed))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (projection == CablePreviewProjection.CrossSection)
            {
                if (!component.CrossSectionVisible) continue;
                var centre = component.CrossSectionCentreMm ?? new Vector2(component.PathMm[0].X, component.PathMm[0].Y);
                var outer = SectionPoints(component.Section, detailed ? 64 : 48, false).Select(point => point + centre).ToImmutableArray();
                var inner = HasHole(component.Section)
                    ? SectionPoints(component.Section, detailed ? 64 : 48, true).Select(point => point + centre).ToImmutableArray()
                    : [];
                pointCount += outer.Length + inner.Length;
                paths.Add(new(component.Id, outer, inner, component.Colour, component.DetailOnly));
            }
            else
            {
                if (!component.SideViewVisible) continue;
                // Project the same transported rings as 3D. Segment hulls preserve loops,
                // tails and ribbon widths even when their axis is not the cable axis.
                var frames = Frames(component);
                var profile = SectionPoints(component.Section, sideSegments, false);
                Vector2[]? previous = null;
                var previousDepth = 0f;
                for (var ring = 0; ring < frames.Length; ring++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var centre = component.PathMm[ring];
                    var worldPoints = profile.Select(point =>
                        centre + frames[ring].X * point.X + frames[ring].Y * point.Y).ToArray();
                    var projected = worldPoints.Select(point => new Vector2(point.Z, point.Y)).ToArray();
                    var frontDepth = worldPoints.Min(point => point.X);
                    if (previous is not null)
                    {
                        var outline = ConvexHull(previous.Concat(projected));
                        pointCount += outline.Length;
                        if (pointCount > budget.MaximumVertices)
                            throw new CablePreviewGeometryLimitException("The scene exceeds the 2D path budget. Use a shorter representative length.");
                        paths.Add(new(component.Id, outline, [], component.Colour, component.DetailOnly,
                            Math.Min(previousDepth, frontDepth)));
                    }
                    previous = projected;
                    previousDepth = frontDepth;
                }
            }
            if (pointCount > budget.MaximumVertices)
                throw new CablePreviewGeometryLimitException("The scene exceeds the 2D path budget. Use a shorter representative length.");
        }
        if (projection == CablePreviewProjection.Side && scene.Print is { } sidePrint)
        {
            foreach (var dot in sidePrint.Layout.Dots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var position = PrintPosition(sidePrint, dot.X, dot.Y);
                // The side view looks toward +X. Back-surface ink is physically occluded.
                if (position.X >= 0) continue;
                var points = Enumerable.Range(0, 8).Select(index =>
                {
                    var angle = Math.Tau * index / 8;
                    var point = PrintPosition(sidePrint, dot.X + sidePrint.DotDiameterMm / 2 * Math.Cos(angle),
                        dot.Y + sidePrint.DotDiameterMm / 2 * Math.Sin(angle));
                    return new Vector2(point.Z, point.Y);
                }).ToImmutableArray();
                pointCount += points.Length;
                if (pointCount > budget.MaximumVertices)
                    throw new CablePreviewGeometryLimitException("The complete printed scene exceeds the 2D path budget.");
                paths.Add(new("surface-print", points, [], sidePrint.Colour, false, position.X, IsSurfacePrint: true));
            }
        }
        var all = paths.SelectMany(path => path.Points).ToArray();
        var minimum2 = all.Length == 0 ? Vector2.Zero : all.Aggregate(Vector2.Min);
        var maximum2 = all.Length == 0 ? Vector2.Zero : all.Aggregate(Vector2.Max);
        var orderedPaths = projection == CablePreviewProjection.Side
            ? paths.OrderBy(path => path.IsSurfacePrint ? 1 : 0)
                .ThenByDescending(path => path.DepthMm).ToImmutableArray()
            : paths.ToImmutable();
        return new(orderedPaths, minimum2, maximum2, drawingNotes);
    }

    private static IEnumerable<CablePreviewComponent> Visible(CablePreviewScene scene, bool detailed) =>
        scene.Components.Where(component => detailed ? !component.SimpleOnly : !component.DetailOnly);

    private static bool Fits(CablePreviewComponent[] components, int segments, CablePreviewGeometryBudget budget,
        CablePreviewPrint? print, int dotSegments)
    {
        long vertices = 0, indices = 0;
        foreach (var component in components)
        {
            var count = SectionPoints(component.Section, segments, false).Length;
            var rings = component.PathMm.Length;
            var hole = HasHole(component.Section);
            vertices += (long)count * rings * (hole ? 2 : 1) + (hole ? count * 4 : (count + 1) * 2);
            indices += (long)count * (rings - 1) * 6 * (hole ? 2 : 1) + count * (hole ? 12 : 6);
        }
        if (print is not null)
        {
            vertices += (long)print.Layout.Dots.Length * (dotSegments + 1);
            indices += (long)print.Layout.Dots.Length * dotSegments * 3;
        }
        return vertices <= budget.MaximumVertices && indices <= budget.MaximumIndices;
    }

    private static long EstimateSidePoints(CablePreviewComponent[] components, int segments, CablePreviewPrint? print) =>
        components.Sum(component => (long)(component.PathMm.Length - 1) * SectionPoints(component.Section, segments, false).Length * 2)
        + (long)(print?.Layout.Dots.Length ?? 0) * 8;

    private static void AddPrintMesh(CablePreviewPrint print, int segments, List<CablePreviewVertex> vertices,
        List<uint> indices, CancellationToken cancellationToken)
    {
        foreach (var dot in print.Layout.Dots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var start = vertices.Count;
            var centre = PrintPosition(print, dot.X, dot.Y);
            var normal = Vector3.Normalize(new(centre.X, centre.Y, 0));
            vertices.Add(new(centre, normal, print.Colour));
            for (var index = 0; index < segments; index++)
            {
                var angle = Math.Tau * index / segments;
                var position = PrintPosition(print, dot.X + print.DotDiameterMm / 2 * Math.Cos(angle),
                    dot.Y + print.DotDiameterMm / 2 * Math.Sin(angle));
                vertices.Add(new(position, Vector3.Normalize(new(position.X, position.Y, 0)), print.Colour));
            }
            for (var index = 0; index < segments; index++)
            {
                indices.Add((uint)start);
                indices.Add((uint)(start + 1 + index));
                indices.Add((uint)(start + 1 + (index + 1) % segments));
            }
        }
    }

    private static Vector3 PrintPosition(CablePreviewPrint print, double axialMm, double arcMm)
    {
        var radius = print.SurfaceDiameterMm / 2;
        var angle = arcMm / radius;
        // A minute raised ink surface prevents depth-buffer fighting against the sheath.
        var visibleRadius = radius + Math.Max(.001, print.SurfaceDiameterMm * .0001);
        return new((float)(-visibleRadius * Math.Cos(angle)), (float)(visibleRadius * Math.Sin(angle)),
            (float)(print.StartZMm + axialMm));
    }

    private static void Sweep(CablePreviewComponent component, int segments, List<CablePreviewVertex> vertices,
        List<uint> indices, CancellationToken cancellationToken)
    {
        var frames = Frames(component);
        var outer = SectionPoints(component.Section, segments, false);
        var inner = HasHole(component.Section) ? SectionPoints(component.Section, segments, true) : [];
        var outerStart = vertices.Count;
        AddWalls(outer, false);
        var innerStart = vertices.Count;
        if (inner.Length > 0) AddWalls(inner, true);
        AddCap(0, false);
        AddCap(component.PathMm.Length - 1, true);

        void AddWalls(Vector2[] profile, bool inside)
        {
            var start = vertices.Count;
            for (var ring = 0; ring < frames.Length; ring++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var index = 0; index < profile.Length; index++)
                {
                    var previous = profile[(index + profile.Length - 1) % profile.Length];
                    var next = profile[(index + 1) % profile.Length];
                    var tangent = next - previous;
                    var normal2 = Vector2.Normalize(new(tangent.Y, -tangent.X)) * (inside ? -1 : 1);
                    var normal = frames[ring].X * normal2.X + frames[ring].Y * normal2.Y;
                    vertices.Add(new(component.PathMm[ring] + frames[ring].X * profile[index].X + frames[ring].Y * profile[index].Y,
                        Vector3.Normalize(normal), component.Colour));
                }
            }
            for (var ring = 0; ring < frames.Length - 1; ring++)
                for (var index = 0; index < profile.Length; index++)
                {
                    var next = (index + 1) % profile.Length;
                    var a = start + ring * profile.Length + index;
                    var b = start + ring * profile.Length + next;
                    var c = a + profile.Length;
                    var d = b + profile.Length;
                    Triangle(a, b, c, inside);
                    Triangle(b, d, c, inside);
                }
        }

        void AddCap(int ring, bool end)
        {
            var normal = frames[ring].Tangent * (end ? 1 : -1);
            var start = vertices.Count;
            for (var index = 0; index < outer.Length; index++)
                vertices.Add(vertices[outerStart + ring * outer.Length + index] with { Normal = normal });
            if (inner.Length == 0)
            {
                var centre = vertices.Count;
                vertices.Add(new(component.PathMm[ring], normal, component.Colour));
                for (var index = 0; index < outer.Length; index++)
                    Triangle(centre, start + index, start + (index + 1) % outer.Length, !end);
            }
            else
            {
                var innerCap = vertices.Count;
                for (var index = 0; index < inner.Length; index++)
                    vertices.Add(vertices[innerStart + ring * inner.Length + index] with { Normal = normal });
                for (var index = 0; index < outer.Length; index++)
                {
                    var next = (index + 1) % outer.Length;
                    Triangle(start + index, start + next, innerCap + index, !end);
                    Triangle(start + next, innerCap + next, innerCap + index, !end);
                }
            }
        }

        void Triangle(int a, int b, int c, bool reverse)
        {
            indices.Add((uint)a);
            indices.Add((uint)(reverse ? c : b));
            indices.Add((uint)(reverse ? b : c));
        }
    }

    private static Vector2[] SectionPoints(CablePreviewSection section, int segments, bool inner)
    {
        var halfWidth = (float)((inner ? section.InnerWidthMm : section.WidthMm) / 2);
        var halfHeight = (float)((inner ? section.InnerHeightMm : section.HeightMm) / 2);
        if (section.Shape == CablePreviewShape.Flat)
            return [new(halfWidth, -halfHeight), new(halfWidth, halfHeight), new(-halfWidth, halfHeight), new(-halfWidth, -halfHeight)];
        if (section.Shape == CablePreviewShape.DShape)
            return Enumerable.Range(0, segments + 1).Select(index =>
                new Vector2(halfWidth * MathF.Cos(MathF.PI * index / segments),
                    -halfHeight + 2 * halfHeight * MathF.Sin(MathF.PI * index / segments))).ToArray();
        return Enumerable.Range(0, segments).Select(index =>
            new Vector2(halfWidth * MathF.Cos(MathF.Tau * index / segments),
                halfHeight * MathF.Sin(MathF.Tau * index / segments))).ToArray();
    }

    private static Frame[] Frames(CablePreviewComponent component)
    {
        var path = component.PathMm;
        var frames = new Frame[path.Length];
        var previousX = Vector3.UnitX;
        for (var index = 0; index < path.Length; index++)
        {
            var direction = index == 0 ? path[1] - path[0]
                : index == path.Length - 1 ? path[^1] - path[^2]
                : path[index + 1] - path[index - 1];
            if (direction.LengthSquared() < 1e-12f) direction = path[index + 1] - path[index];
            var tangent = Vector3.Normalize(direction);
            if (component.RadialFrame)
            {
                var radial = new Vector3(path[index].X, path[index].Y, 0);
                radial -= Vector3.Dot(radial, tangent) * tangent;
                if (radial.LengthSquared() > 1e-6f)
                {
                    var radialY = Vector3.Normalize(radial);
                    var radialX = Vector3.Normalize(Vector3.Cross(radialY, tangent));
                    frames[index] = new(radialX, radialY, tangent);
                    previousX = radialX;
                    continue;
                }
            }
            var x = previousX - Vector3.Dot(previousX, tangent) * tangent;
            if (x.LengthSquared() < 1e-6f)
            {
                var reference = Math.Abs(tangent.Y) < .9f ? Vector3.UnitY : Vector3.UnitZ;
                x = reference - Vector3.Dot(reference, tangent) * tangent;
            }
            x = Vector3.Normalize(x);
            var y = Vector3.Normalize(Vector3.Cross(tangent, x));
            frames[index] = new(x, y, tangent);
            previousX = x;
        }
        return frames;
    }

    private static ImmutableArray<Vector2> ConvexHull(IEnumerable<Vector2> source)
    {
        var points = source.Distinct().OrderBy(point => point.X).ThenBy(point => point.Y).ToArray();
        if (points.Length < 3) return points.ToImmutableArray();
        var hull = new List<Vector2>();
        foreach (var point in points)
        {
            while (hull.Count > 1 && Cross(hull[^1] - hull[^2], point - hull[^1]) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        var lowerCount = hull.Count;
        for (var index = points.Length - 2; index >= 0; index--)
        {
            var point = points[index];
            while (hull.Count > lowerCount && Cross(hull[^1] - hull[^2], point - hull[^1]) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        hull.RemoveAt(hull.Count - 1);
        return hull.ToImmutableArray();
    }

    private static float Cross(Vector2 first, Vector2 second) => first.X * second.Y - first.Y * second.X;

    private static bool HasHole(CablePreviewSection section) => section.InnerWidthMm > 0;
    private static bool Positive(double value) => double.IsFinite(value) && value > 0 && value <= 1000000;
    private static bool Nonnegative(double value) => double.IsFinite(value) && value >= 0 && value <= 1000000;
    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    private static bool Finite(Vector4 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z) && float.IsFinite(value.W);
    private readonly record struct Frame(Vector3 X, Vector3 Y, Vector3 Tangent);
}
