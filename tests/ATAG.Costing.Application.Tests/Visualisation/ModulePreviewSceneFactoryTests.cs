using System.Numerics;
using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.Domain.Braiding;
using ATAG.Costing.Domain.Coiling;
using ATAG.Costing.Domain.Conductors;
using Xunit;

namespace ATAG.Costing.Application.Tests.Visualisation;

public class ModulePreviewSceneFactoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ModuleMatrixRendersCrossSectionSideAnd3DWithinSharedBudgets(bool detailed)
    {
        var coilResult = CoilCableLengthCalculator.Calculate(new(CoilCableShape.Flat, 2.5, 4.8, 10, 96, 50, 70, 5, 8, 1));
        var roundCoilResult = CoilCableLengthCalculator.Calculate(new(CoilCableShape.Round, 2.5, 2.5, 10, 96, 50, 70, 5, 8, 1));
        CablePreviewScene[] scenes =
        [
            ModulePreviewSceneFactory.CreateSingleCore(2.2, 3.2, ConductorConstructionCalculator.TryCalculate("7x19/0.15", 2.5m)),
            ModulePreviewSceneFactory.CreateDual(2.2, 3.2, 4.8, null),
            ModulePreviewSceneFactory.CreateBraid(BraidReferenceTables.CoreLayoutFor(6), 2, 6, 10, .25, 24, 46.6),
            ModulePreviewSceneFactory.CreateBuncher(BraidReferenceTables.CoreLayoutFor(1), 19.43),
            ModulePreviewSceneFactory.CreateBuncher(BraidReferenceTables.CoreLayoutFor(6), 19.43),
            ModulePreviewSceneFactory.CreateBuncher(BraidReferenceTables.CoreLayoutFor(45), 19.43),
            ModulePreviewSceneFactory.CreateCoil(CoilCableShape.Round, roundCoilResult, 50, 70, 5, 8),
            ModulePreviewSceneFactory.CreateCoil(CoilCableShape.Flat, coilResult, 50, 70, 5, 8),
            ModulePreviewSceneFactory.CreateCoil(CoilCableShape.DShape, coilResult, 50, 70, 5, 8),
        ];
        foreach (var scene in scenes)
        {
            var mesh = CablePreviewGeometry.BuildMesh(scene, detailed);
            Assert.NotEmpty(mesh.Vertices);
            Assert.True(mesh.Vertices.Length <= new CablePreviewGeometryBudget().MaximumVertices);
            foreach (var projection in Enum.GetValues<CablePreviewProjection>())
            {
                var drawing = CablePreviewGeometry.BuildDrawing(scene, projection, detailed);
                Assert.NotEmpty(drawing.Paths);
                Assert.True(float.IsFinite(drawing.MinimumMm.X) && float.IsFinite(drawing.MaximumMm.Y));
            }
        }
    }

    [Theory]
    [InlineData(double.NaN, 3)]
    [InlineData(0, 3)]
    [InlineData(3, 2)]
    [InlineData(2, 2)]
    public void SingleCore_InvalidGeometryShowsUnavailableInsteadOfInventedDimensions(double conductor, double finished)
    {
        var scene = ModulePreviewSceneFactory.CreateSingleCore(conductor, finished, null);
        Assert.Empty(scene.Components);
        Assert.Contains("valid", scene.Description);
    }

    [Fact]
    public void Dual_PreservesActualDiametersAndDoesNotInventSelectedLayerDimensions()
    {
        var scene = ModulePreviewSceneFactory.CreateDual(2.2, 3.2, 4.8, null,
            unspecifiedLayers: ["tape", "foil", "drain wire"]);
        var first = Assert.Single(scene.Components, item => item.Id == "first-insulation");
        var second = Assert.Single(scene.Components, item => item.Id == "second-insulation");
        Assert.Equal(3.2, first.Section.WidthMm);
        Assert.Equal(2.2, first.Section.InnerWidthMm);
        Assert.Equal(4.8, second.Section.WidthMm);
        Assert.Equal(3.2, second.Section.InnerWidthMm);
        Assert.DoesNotContain(scene.Components, item => item.Kind is CablePreviewComponentKind.Foil or CablePreviewComponentKind.Tape or CablePreviewComponentKind.Drain);
        Assert.Contains(scene.Notes, note => note.Contains("Selected but not drawn: tape, foil, drain wire"));
    }

    [Fact]
    public void SingleCore_Preserves133StrandsAndRecursivePackingFromRetainedConstruction()
    {
        var construction = ConductorConstructionCalculator.TryCalculate("7x19/0.15", 2.5m);
        Assert.NotNull(construction);
        var scene = ModulePreviewSceneFactory.CreateSingleCore(2.2, 3.2, construction);
        Assert.Equal(133, scene.Components.Count(item => item.DetailOnly));
        Assert.True(scene.Components.Single(item => item.Id == "conductor-envelope").SimpleOnly);
    }

    [Fact]
    public void LargeConductor_UsesExplicitEnvelopeBeforeAllocatingAllStrands()
    {
        var construction = ConductorConstructionCalculator.TryCalculate("500/0.15", 10m);
        Assert.NotNull(construction);
        var scene = ModulePreviewSceneFactory.CreateSingleCore(5, 7, construction);
        Assert.Equal(2, scene.Components.Length);
        Assert.False(scene.Components[0].SimpleOnly);
        Assert.Contains(scene.Notes, note => note.Contains("500 strands"));
    }

    [Theory]
    [InlineData(16, 1)]
    [InlineData(24, 10)]
    public void Braid_DrawsEveryRetainedEndAndSelectedCarrierPitch(int carriers, int ends)
    {
        const double pitch = 46.6;
        var scene = ModulePreviewSceneFactory.CreateBraid(BraidReferenceTables.CoreLayouts[0],
            10, 10, ends, 0.15, carriers, pitch);
        Assert.Equal(carriers * ends, scene.Components.Count(item => item.DetailOnly));
        Assert.Equal(carriers, scene.Components.Count(item => item.SimpleOnly));
        Assert.Contains("46.6 mm", scene.Description);
        Assert.Contains($"{carriers * ends} strands", scene.Description);
        Assert.All(scene.Components, component => Assert.All(component.PathMm,
            point => Assert.True(float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z))));
        Assert.NotEmpty(CablePreviewGeometry.BuildMesh(scene).Vertices);
        Assert.NotEmpty(CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side).Paths);
    }

    [Fact]
    public void BuncherSixCore_IsOneEqualCoreInsideFive_NoFormerOrThinWireSurrounds()
    {
        var layout = BraidReferenceTables.CoreLayouts.Single(item => item.CoreCount == 6);
        var scene = ModulePreviewSceneFactory.CreateBuncher(layout, 19.43);
        Assert.Equal(6, scene.Components.Length);
        Assert.All(scene.Components, component => Assert.Equal(CablePreviewComponentKind.Core, component.Kind));
        Assert.All(scene.Components, component => Assert.Equal(2, component.Section.WidthMm));
        Assert.Single(scene.Components, component => component.CrossSectionCentreMm!.Value.Length() < 0.001);
        Assert.Equal(5, scene.Components.Count(component => component.CrossSectionCentreMm!.Value.Length() > 0.001));
        Assert.Contains(scene.Notes, note => note.Contains("unspecified") && note.Contains("illustrative"));
    }

    [Fact]
    public void ValidRetainedCoreGroupsPreserveCountsWithoutOverlappingEqualSections()
    {
        foreach (var layout in BraidReferenceTables.CoreLayouts)
        {
            var scene = ModulePreviewSceneFactory.CreateBuncher(layout, 19.43);
            if (layout.CoreCount == 26) continue; // The source lists 3-9-15 = 27; verified separately.
            Assert.Equal(layout.CoreCount, scene.Components.Length);
            for (var i = 0; i < scene.Components.Length; i++)
                for (var j = i + 1; j < scene.Components.Length; j++)
                    Assert.True(Vector2.Distance(scene.Components[i].CrossSectionCentreMm!.Value,
                        scene.Components[j].CrossSectionCentreMm!.Value) >= 1.9999f,
                        $"Overlapping cores {i}/{j} in retained group {layout.Display}.");
        }
    }

    [Fact]
    public void InconsistentRetained26CoreRowShowsExplicitIssueWithoutDroppingAnActualCore()
    {
        var layout = BraidReferenceTables.CoreLayouts.Single(item => item.CoreCount == 26);
        var buncher = ModulePreviewSceneFactory.CreateBuncher(layout, 19.43);
        var braid = ModulePreviewSceneFactory.CreateBraid(layout, 2, 12.31, 6, 0.15, 16, 20);
        Assert.Empty(buncher.Components);
        Assert.Empty(braid.Components);
        Assert.Contains("inconsistent lay-up (3-9-15)", buncher.Description);
    }

    [Theory]
    [InlineData(CoilCableShape.Round, CablePreviewShape.Round)]
    [InlineData(CoilCableShape.Flat, CablePreviewShape.Flat)]
    [InlineData(CoilCableShape.DShape, CablePreviewShape.DShape)]
    public void Coil_PreservesPhysicalOrientationAndAddsStripsAfterTails(CoilCableShape shape, CablePreviewShape expected)
    {
        var result = CoilCableLengthCalculator.Calculate(new(shape, 2.5, 4.8, 10, 24, 50, 60, 4, 7, 1));
        var scene = ModulePreviewSceneFactory.CreateCoil(shape, result, 50, 60, 4, 7);
        var coil = scene.Components.Single(item => item.Id == "coil");
        Assert.Equal(expected, coil.Section.Shape);
        Assert.Equal(2.5, coil.Section.HeightMm);
        Assert.Equal(shape == CoilCableShape.Round ? 2.5 : 4.8, coil.Section.WidthMm);
        Assert.True(coil.RadialFrame);
        Assert.Equal(5, scene.Components.Single(item => item.Id == "bar").Section.WidthMm);
        Assert.Equal(50, PathLength(scene, "tail-start"), 4);
        Assert.Equal(60, PathLength(scene, "tail-end"), 4);
        Assert.Equal(4, PathLength(scene, "strip-start"), 4);
        Assert.Equal(7, PathLength(scene, "strip-end"), 4);
        var startTail = scene.Components.Single(item => item.Id == "tail-start");
        var endTail = scene.Components.Single(item => item.Id == "tail-end");
        Assert.True(Vector3.Cross(startTail.PathMm[1] - startTail.PathMm[0],
            endTail.PathMm[1] - endTail.PathMm[0]).Length() < 0.001);
        Assert.NotEmpty(CablePreviewGeometry.BuildMesh(scene).Vertices);
        Assert.NotEmpty(CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side).Paths);
    }

    [Fact]
    public void Coil_HugeTurnCountIsBoundedAndClearlyLabelled()
    {
        var result = CoilCableLengthCalculator.Calculate(new(CoilCableShape.Flat, 2.5, 4.8, 10, 48000, 0, 0, 0, 0, 1));
        var scene = ModulePreviewSceneFactory.CreateCoil(CoilCableShape.Flat, result, 0, 0, 0, 0);
        Assert.True(scene.Components.Single(item => item.Id == "coil").PathMm.Length <= 1537);
        Assert.Contains(scene.Notes, note => note.Contains("Representative view: 32 of 10,000"));
        Assert.Contains("48000 mm", scene.Description);
    }

    private static double PathLength(CablePreviewScene scene, string id)
    {
        var component = scene.Components.Single(item => item.Id == id);
        return Vector3.Distance(component.PathMm[0], component.PathMm[1]);
    }
}
