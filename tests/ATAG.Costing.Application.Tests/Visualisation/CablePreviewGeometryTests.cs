using System.Collections.Immutable;
using System.Numerics;
using ATAG.Costing.Application.Visualisation;
using Xunit;

namespace ATAG.Costing.Application.Tests.Visualisation;

public sealed class CablePreviewGeometryTests
{
    [Theory]
    [InlineData(CablePreviewShape.Round)]
    [InlineData(CablePreviewShape.Flat)]
    [InlineData(CablePreviewShape.DShape)]
    public void SolidAndAnnularSections_HaveFiniteNormalsAndConsistentTriangleWinding(CablePreviewShape shape)
    {
        foreach (var hollow in new[] { false, true })
        {
            var scene = Scene(Component("layer", new(4, 3, shape, hollow ? 2 : 0, hollow ? 1 : 0)));
            var mesh = CablePreviewGeometry.BuildMesh(scene);
            Assert.NotEmpty(mesh.Vertices);
            Assert.All(mesh.Vertices, vertex =>
            {
                Assert.InRange(vertex.Normal.Length(), .999f, 1.001f);
                Assert.InRange(vertex.Position.X, -3.001f, 3.001f);
                Assert.InRange(vertex.Position.Y, -3.001f, 3.001f);
                Assert.InRange(vertex.Position.Z, -3.001f, 3.001f);
            });
            for (var index = 0; index < mesh.Indices.Length; index += 3)
            {
                var a = mesh.Vertices[(int)mesh.Indices[index]];
                var b = mesh.Vertices[(int)mesh.Indices[index + 1]];
                var c = mesh.Vertices[(int)mesh.Indices[index + 2]];
                var winding = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
                Assert.True(winding.LengthSquared() > 1e-12f, $"Degenerate face {index / 3} for {shape}, hollow {hollow}.");
                Assert.True(Vector3.Dot(winding, a.Normal + b.Normal + c.Normal) > 0,
                    $"Reversed face {index / 3} for {shape}, hollow {hollow}.");
            }
        }
    }

    [Fact]
    public void Annulus_CrossSectionRetainsHoleAndCapsDoNotFillItsCentre()
    {
        var scene = Scene(Component("foil", new(4, 4, InnerWidthMm: 3, InnerHeightMm: 3)));
        var drawing = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.CrossSection);
        var path = Assert.Single(drawing.Paths);
        Assert.NotEmpty(path.InnerPoints);
        Assert.All(path.InnerPoints, point => Assert.InRange(point.Length(), 1.499f, 1.501f));
        var mesh = CablePreviewGeometry.BuildMesh(scene);
        Assert.All(mesh.Vertices, vertex =>
        {
            if (Math.Abs(vertex.Normal.Z) > .99f)
                Assert.True(new Vector2(vertex.Position.X, vertex.Position.Y).Length() >= .899f);
        });
    }

    [Fact]
    public void SideView_IncludesSectionWidthForTailWhoseAxisIsVerticalInScreen()
    {
        var tail = Component("tail", new(4, 2, CablePreviewShape.Flat)) with
        {
            PathMm = [new(0, 0, 0), new(0, 10, 0)],
        };
        var drawing = CablePreviewGeometry.BuildDrawing(Scene(tail), CablePreviewProjection.Side);
        Assert.NotEmpty(drawing.Paths);
        Assert.True(drawing.MaximumMm.X - drawing.MinimumMm.X >= 1.999f);
        Assert.True(drawing.MaximumMm.Y - drawing.MinimumMm.Y >= 9.999f);
        Assert.All(drawing.Paths, path => Assert.True(Area(path.Points) > 0));
    }

    [Fact]
    public void FlatCoil_UsesAxialWidthAndRadialHeightInBothProjections()
    {
        var coil = Component("coil", new(4.8, 2.5, CablePreviewShape.Flat)) with
        {
            Kind = CablePreviewComponentKind.Coil,
            PathMm = CablePreviewPaths.Helix(8, 4.8, 9.6),
            CrossSectionCentreMm = Vector2.Zero,
            RadialFrame = true,
        };
        var scene = Scene(coil);
        var side = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side);
        var mesh = CablePreviewGeometry.BuildMesh(scene);
        Assert.True(side.MaximumMm.X - side.MinimumMm.X > 14);
        Assert.InRange(mesh.MaximumMm.Z - mesh.MinimumMm.Z, 14f, 14.5f);
        Assert.InRange(side.MaximumMm.X - side.MinimumMm.X,
            mesh.MaximumMm.Z - mesh.MinimumMm.Z - .01f, mesh.MaximumMm.Z - mesh.MinimumMm.Z + .01f);
        var cross = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.CrossSection);
        Assert.InRange(cross.MaximumMm.X - cross.MinimumMm.X, 4.799f, 4.801f);
        Assert.InRange(cross.MaximumMm.Y - cross.MinimumMm.Y, 2.499f, 2.501f);
    }

    [Fact]
    public void SideView_PreservesDepthOrderAcrossCounterHelicalWires()
    {
        var clockwise = Component("clockwise", new(.2, .2)) with { PathMm = CablePreviewPaths.Helix(2, 8, 16) };
        var counter = Component("counter", new(.2, .2)) with { PathMm = CablePreviewPaths.Helix(2, 8, 16, rightHanded: false) };
        var drawing = CablePreviewGeometry.BuildDrawing(Scene(clockwise, counter), CablePreviewProjection.Side);
        Assert.Equal(96, drawing.Paths.Length);
        Assert.True(drawing.Paths.Select(path => path.DepthMm).SequenceEqual(drawing.Paths.Select(path => path.DepthMm).OrderDescending()));
        Assert.Contains(drawing.Paths, path => path.ComponentId == "clockwise" && path.DepthMm < 0);
        Assert.Contains(drawing.Paths, path => path.ComponentId == "counter" && path.DepthMm > 0);
    }

    [Fact]
    public void GeometryBudget_ReducesSurfaceDetailWithoutDroppingAnyComponents()
    {
        var components = Enumerable.Range(0, 24).Select(index => Component($"core-{index}", new(.2, .2)) with
        {
            PathMm = CablePreviewPaths.Helix(2, 10, 20, Math.Tau * index / 24),
        }).ToArray();
        var budget = new CablePreviewGeometryBudget(MaximumVertices: 12000, MaximumIndices: 70000);
        var mesh = CablePreviewGeometry.BuildMesh(Scene(components), budget: budget);
        Assert.InRange(mesh.Vertices.Length, 1, budget.MaximumVertices);
        Assert.InRange(mesh.Indices.Length, 1, budget.MaximumIndices);
        Assert.Contains(mesh.Notes, note => note.Contains("reduced"));
        // With 49 rings + both capped ends each six-sided strand has 308 vertices.
        Assert.True(mesh.Vertices.Length >= 24 * 308);
        Assert.Throws<CablePreviewGeometryLimitException>(() => CablePreviewGeometry.BuildMesh(Scene(components),
            budget: budget with { MaximumVertices = 100 }));
    }

    [Fact]
    public void SimpleAndDetailed_UseExplicitEnvelopesAndStrandsWithoutDuplication()
    {
        var scene = Scene(
            Component("envelope", new(3, 3)) with { SimpleOnly = true },
            Component("wire-1", new(1, 1)) with { DetailOnly = true },
            Component("wire-2", new(1, 1)) with { DetailOnly = true, CrossSectionCentreMm = new(1, 0) });
        Assert.Single(CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.CrossSection, detailed: false).Paths);
        Assert.Equal(2, CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.CrossSection).Paths.Length);
    }

    [Theory]
    [InlineData(CablePreviewComponentKind.Tape)]
    [InlineData(CablePreviewComponentKind.Foil)]
    public void HelicalMaterialWrap_UsesSuppliedPitchWidthAndRadialThickness(CablePreviewComponentKind kind)
    {
        var layers = CablePreviewLayers.Wrap("wrap", "Material wrap", kind, innerDiameterMm: 10,
            thicknessMm: .2, widthMm: 3, pitchMm: 5, lengthMm: 10, colour: new(.7f, .7f, .75f, 1));
        var winding = Assert.Single(layers, component => component.ThreeDimensionalVisible);
        Assert.Equal(3, winding.Section.WidthMm);
        Assert.Equal(.2, winding.Section.HeightMm);
        Assert.True(winding.RadialFrame);
        Assert.InRange(winding.PathMm[0].X, 5.099f, 5.101f);
        Assert.InRange(winding.PathMm[24].Z, 4.999f, 5.001f);
        var scene = Scene(layers.ToArray());
        var cross = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.CrossSection);
        var ring = Assert.Single(cross.Paths);
        Assert.NotEmpty(ring.InnerPoints);
        Assert.InRange(cross.MaximumMm.X - cross.MinimumMm.X, 10.399f, 10.401f);
        var side = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side);
        Assert.Equal(48, side.Paths.Length);
        Assert.DoesNotContain(side.Paths, path => path.ComponentId.EndsWith("-end-envelope"));
        var mesh = CablePreviewGeometry.BuildMesh(scene);
        Assert.NotEmpty(mesh.Vertices);
        Assert.True(mesh.MaximumMm.Z - mesh.MinimumMm.Z > 12);
        var reverse = CablePreviewLayers.Wrap("reverse", "Reverse", kind, 10, .2, 3, 5, 10,
            new(.7f, .7f, .75f, 1), rightHanded: false)[0];
        Assert.InRange(winding.PathMm[1].Y + reverse.PathMm[1].Y, -.0001f, .0001f);
    }

    [Fact]
    public void SideView_CoveringSleevePaintsOverFrontConductorInCoveredRegion()
    {
        var wire = Component("wire", new(.8, .8)) with { PathMm = CablePreviewPaths.Straight(10, new(-1, 0)) };
        var sleeve = Component("sleeve", new(4, 4, InnerWidthMm: 3, InnerHeightMm: 3));
        var side = CablePreviewGeometry.BuildDrawing(Scene(wire, sleeve), CablePreviewProjection.Side);
        Assert.Equal("sleeve", side.Paths[^1].ComponentId);
        Assert.True(side.Paths[^1].DepthMm < side.Paths[0].DepthMm);
    }

    [Fact]
    public void PrintDots_FollowCylinderAndPaintOverItsVisibleSurfaceWithOutwardTriangles()
    {
        var ink = new Vector4(.95f, .1f, .2f, 1);
        var print = new CablePreviewPrint(new([new(2, 0), new(5, .5f)], 3.2, 1.2, 10, []), 4, .2, ink);
        var scene = Scene(Component("jacket", new(4, 4))) with { Print = print };
        var mesh = CablePreviewGeometry.BuildMesh(scene);
        var dots = mesh.Vertices.Where(vertex => vertex.Colour == ink).ToArray();
        Assert.Equal(18, dots.Length);
        Assert.All(dots, vertex => Assert.True(vertex.Normal.X < -.9f));
        for (var index = mesh.Indices.Length - 48; index < mesh.Indices.Length; index += 3)
        {
            var a = mesh.Vertices[(int)mesh.Indices[index]];
            var b = mesh.Vertices[(int)mesh.Indices[index + 1]];
            var c = mesh.Vertices[(int)mesh.Indices[index + 2]];
            Assert.True(Vector3.Dot(Vector3.Cross(b.Position - a.Position, c.Position - a.Position), a.Normal) > 0);
        }
        var side = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side);
        Assert.Equal(2, side.Paths.Count(path => path.ComponentId == "surface-print"));
        Assert.All(side.Paths.TakeLast(2), path => Assert.Equal("surface-print", path.ComponentId));
        var cross = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.CrossSection);
        Assert.DoesNotContain(cross.Paths, path => path.ComponentId == "surface-print");
    }

    [Fact]
    public void PrintBudgetsAndCircumference_RejectWholeSceneInsteadOfTruncatingText()
    {
        var scene = Scene(Component("jacket", new(4, 4)));
        var print = new CablePreviewPrint(new(Enumerable.Repeat(new Vector2(1, 0), 20001).ToImmutableArray(), 3, 1, 10, []),
            4, .1, Vector4.One);
        Assert.Throws<CablePreviewGeometryLimitException>(() => CablePreviewGeometry.BuildMesh(scene with { Print = print }));
        var tooTall = print with { Layout = new([new(1, 0)], 3, 20, 10, []) };
        Assert.Throws<ArgumentException>(() => CablePreviewGeometry.BuildMesh(scene with { Print = tooTall }));
        var bounded = print with { Layout = new([new(1, 0), new(2, 0)], 3, 1, 10, []) };
        Assert.Throws<CablePreviewGeometryLimitException>(() => CablePreviewGeometry.BuildMesh(scene with { Print = bounded },
            budget: new(MaximumVertices: 20)));
    }

    [Fact]
    public void CenteredPrintedCable_AllowsNegativeStartAndKeepsBothImpressionsOnTheSurface()
    {
        var source = ModulePreviewSceneFactory.CreateSingleCore(2, 4, null);
        var scene = CablePrintedSceneBuilder.Apply(source, new("ATAG", .1, 7, .15, .15, 100));
        Assert.NotNull(scene.Print);
        Assert.True(scene.Print.StartZMm < 0);
        var surface = scene.Components.Single(component => component.Id == "insulation");
        Assert.All(scene.Print.Layout.Dots, dot => Assert.InRange(scene.Print.StartZMm + dot.X,
            surface.PathMm[0].Z, surface.PathMm[^1].Z));
        Assert.NotEmpty(CablePreviewGeometry.BuildMesh(scene).Vertices);
        Assert.Equal(scene.Print.Layout.Dots.Length,
            CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side).Paths.Count(path => path.IsSurfacePrint));
    }

    [Fact]
    public void FlatCoilTailsAndStrips_PreserveAxialWidthWithoutNinetyDegreeTwist()
    {
        var result = ATAG.Costing.Domain.Coiling.CoilCableLengthCalculator.Calculate(
            new(ATAG.Costing.Domain.Coiling.CoilCableShape.Flat, 2.5, 4.8, 10, 24, 50, 60, 4, 7, 1));
        var scene = ModulePreviewSceneFactory.CreateCoil(ATAG.Costing.Domain.Coiling.CoilCableShape.Flat,
            result, 50, 60, 4, 7);
        foreach (var tail in scene.Components.Where(component => component.Kind == CablePreviewComponentKind.Tail))
        {
            Assert.True(tail.RadialFrame);
            var mesh = CablePreviewGeometry.BuildMesh(Scene(tail));
            Assert.InRange(mesh.MaximumMm.Z - mesh.MinimumMm.Z, 4.799f, 4.801f);
            Assert.InRange(mesh.MaximumMm.X - mesh.MinimumMm.X, 2.499f, 2.501f);
        }
    }

    [Fact]
    public void BraidAlternatesActualOverUnderAtSuccessiveCarrierIntersections()
    {
        var scene = ModulePreviewSceneFactory.CreateBraid(
            ATAG.Costing.Domain.Braiding.BraidReferenceTables.CoreLayoutFor(1), 2, 2, 1, .15, 16, 20);
        var right = scene.Components.Single(component => component.Id == "carrier-0-end-0");
        for (var group = 0; group < 8; group++)
        {
            var left = scene.Components.Single(component => component.Id == $"carrier-{2 * group + 1}-end-0");
            var index = group * 3; // 48 samples/turn, crossing every 1/16 turn.
            var rightPoint = new Vector2(right.PathMm[index].X, right.PathMm[index].Y);
            var leftPoint = new Vector2(left.PathMm[index].X, left.PathMm[index].Y);
            Assert.InRange(Vector2.Distance(Vector2.Normalize(rightPoint), Vector2.Normalize(leftPoint)), 0, .0001f);
            var clearance = rightPoint.Length() - leftPoint.Length();
            Assert.True(group % 2 == 0 ? clearance > .17f : clearance < -.17f,
                $"Crossing {group} did not alternate over-under: radial separation {clearance}.");
        }
    }

    [Fact]
    public void InvalidInputAndCancellation_StopBeforeProducingPartialGeometry()
    {
        Assert.Throws<ArgumentException>(() => Scene(Component("invalid", new(double.NaN, 2))));
        Assert.Throws<ArgumentException>(() => Scene(Component("invalid", new(2, 2, InnerWidthMm: 1))));
        Assert.Throws<ArgumentException>(() => Scene(Component("invalid", new(2, 2)) with { PathMm = [Vector3.Zero, Vector3.Zero] }));
        Assert.Throws<ArgumentException>(() => Scene(Component("same", new(2, 2)), Component("same", new(2, 2))));
        Assert.Throws<CablePreviewGeometryLimitException>(() => CablePreviewPaths.Helix(2, 1, 100));
        var scene = Scene(Component("valid", new(2, 2)));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => CablePreviewGeometry.BuildMesh(scene, cancellationToken: cancellation.Token));
        Assert.Throws<OperationCanceledException>(() => CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side, cancellationToken: cancellation.Token));
    }

    private static CablePreviewComponent Component(string id, CablePreviewSection section) =>
        new(id, id, CablePreviewComponentKind.Core, section, CablePreviewPaths.Straight(10), new(.3f, .6f, .8f, 1));

    private static CablePreviewScene Scene(params CablePreviewComponent[] components) =>
        CablePreviewScene.Create("test", "Test", "Physical geometry", components);

    private static float Area(ImmutableArray<Vector2> points)
    {
        var twiceArea = 0f;
        for (var index = 0; index < points.Length; index++)
        {
            var next = points[(index + 1) % points.Length];
            twiceArea += points[index].X * next.Y - next.X * points[index].Y;
        }
        return Math.Abs(twiceArea) / 2;
    }
}
