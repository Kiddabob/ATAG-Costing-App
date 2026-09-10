using System.Collections.Immutable;
using ATAG.Costing.Application.Visualisation;
using Xunit;

namespace ATAG.Costing.Application.Tests.Visualisation;

public sealed class CablePrintLayoutTests
{
    [Fact]
    public void TwoImpressionsKeepIdenticalDotsAndExactStartToStartRepeat()
    {
        var settings = new CablePrintSettings("Atag 12", 0.1, 7, 0.25, 0.2, 250);
        var result = CablePrintLayout.Build(settings);
        Assert.True(result.Dots.Length > 0 && result.Dots.Length % 2 == 0);
        var half = result.Dots.Length / 2;
        for (var index = 0; index < half; index++)
        {
            Assert.Equal(250, result.Dots[index + half].X - result.Dots[index].X, 4);
            Assert.Equal(result.Dots[index].Y, result.Dots[index + half].Y);
        }
    }

    [Fact]
    public void DotDimensionsAndPitchesDefinePhysicalRasterBounds()
    {
        var settings = new CablePrintSettings("H", 0.1, 7, 0.25, 0.2, 50);
        var result = CablePrintLayout.Build(settings, marginMm: 2);
        Assert.Equal(1.1, result.PrintedWidthMm, 8);
        Assert.Equal(1.3, result.PrintedHeightMm, 8);
        Assert.Equal(55.1, result.RequiredCableLengthMm, 8);
        var first = result.Dots.Take(result.Dots.Length / 2).ToArray();
        Assert.Equal(1.1, first.Max(dot => dot.X) - first.Min(dot => dot.X) + 0.1, 5);
        Assert.Equal(1.3, first.Max(dot => dot.Y) - first.Min(dot => dot.Y) + 0.1, 5);
        Assert.Equal(7, first.Select(dot => Math.Round(dot.Y, 5)).Distinct().Count());
        Assert.Equal(2.05, first.Min(dot => dot.X), 5);
        Assert.All(first, dot =>
        {
            Assert.Equal(Math.Round((dot.X - 2.05) / 0.25), (dot.X - 2.05) / 0.25, 4);
            Assert.Equal(Math.Round((dot.Y + 0.6) / 0.2), (dot.Y + 0.6) / 0.2, 4);
        });
    }

    [Fact]
    public void DotBudgetAcceptsCompletePrintsBelowCapAndRejectsWholeOversizeRequest()
    {
        // H is 17 lit pixels; double resolution makes 68 dots per glyph.
        var accepted = CablePrintLayout.Build(new(new string('H', 147), 0.1, 14, 0.25, 0.2, 500));
        Assert.Equal(19992, accepted.Dots.Length);
        var scene = CablePrintedSceneBuilder.Apply(ModulePreviewSceneFactory.CreateSingleCore(2.2, 3.2, null),
            new(new string('H', 147), 0.1, 14, 0.25, 0.2, 500));
        Assert.NotEmpty(CablePreviewGeometry.BuildMesh(scene).Vertices);
        Assert.Equal(19992, CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side).Paths.Count(path => path.IsSurfacePrint));
        Assert.Throws<CablePreviewGeometryLimitException>(() =>
            CablePrintLayout.Build(new(new string('H', 148), 0.1, 14, 0.25, 0.2, 500)));
    }

    [Fact]
    public void LowerCaseKeepsItsOwnRasterAndUnknownCharacterUsesExplicitFallback()
    {
        var upper = CablePrintLayout.Build(new("A", 0.1, 7, 0.25, 0.2, 50));
        var lower = CablePrintLayout.Build(new("a", 0.1, 7, 0.25, 0.2, 50));
        Assert.False(upper.Dots.SequenceEqual(lower.Dots));
        var settings = new CablePrintSettings("Ω", 0.1, 7, 0.25, 0.2, 50);
        var unknown = CablePrintLayout.Build(settings);
        var question = CablePrintLayout.Build(settings with { Text = "?" });
        Assert.True(question.Dots.SequenceEqual(unknown.Dots));
        Assert.Equal("Ω", settings.Text);
        Assert.Contains(unknown.Notes, note => note.Contains("unsupported characters: Ω"));
    }

    [Fact]
    public void SuppliedOverlapsAreLabelledAndSpacingIsNeverSilentlyAdjusted()
    {
        var result = CablePrintLayout.Build(new("HELLO", 0.3, 7, 0.2, 0.15, 1));
        Assert.Contains(result.Notes, note => note.Contains("impressions overlap"));
        Assert.Contains(result.Notes, note => note.Contains("ink dots overlap"));
        Assert.Equal(1, result.Dots[result.Dots.Length / 2].X - result.Dots[0].X, 5);
    }

    public static IEnumerable<object[]> InvalidSettings()
    {
        var valid = new CablePrintSettings("ATAG", 0.1, 7, 0.25, 0.2, 50);
        yield return [valid with { Text = "" }];
        yield return [valid with { Text = " A\nB" }];
        yield return [valid with { Text = new string('A', 161) }];
        yield return [valid with { DotDiameterMm = 0 }];
        yield return [valid with { DotDiameterMm = double.NaN }];
        yield return [valid with { DotsHigh = 4 }];
        yield return [valid with { DotsHigh = 65 }];
        yield return [valid with { HorizontalPitchMm = -1 }];
        yield return [valid with { VerticalPitchMm = double.PositiveInfinity }];
        yield return [valid with { RepeatDistanceMm = 0 }];
    }

    [Theory]
    [MemberData(nameof(InvalidSettings))]
    public void InvalidPrintSettingsAreRejected(CablePrintSettings settings) =>
        Assert.Throws<ArgumentException>(() => CablePrintLayout.Build(settings));

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void InvalidMarginsAreRejected(double margin) =>
        Assert.Throws<ArgumentException>(() => CablePrintLayout.Build(new("A", 0.1, 7, 0.25, 0.2, 50), margin));

    [Fact]
    public void PrintedSceneContainsBothFullImpressionsAndPreservesPhysicalSections()
    {
        var original = ModulePreviewSceneFactory.CreateSingleCore(2.2, 3.2, null);
        Assert.Null(original.Print);
        var scene = CablePrintedSceneBuilder.Apply(original, new("ATAG 2x1.5", 0.1, 7, 0.25, 0.2, 250));
        var print = Assert.IsType<CablePreviewPrint>(scene.Print);
        var surface = scene.Components.Single(item => item.Id == "insulation");
        Assert.Equal(3.2, print.SurfaceDiameterMm);
        Assert.Equal(0.1, print.DotDiameterMm);
        Assert.Equal(original.Components.Select(item => item.Section), scene.Components.Select(item => item.Section));
        Assert.Equal(print.Layout.RequiredCableLengthMm, surface.PathMm[^1].Z - surface.PathMm[0].Z, 4);
        Assert.Equal(surface.PathMm[0].Z, print.StartZMm);
        Assert.All(print.Layout.Dots, dot =>
        {
            var centreZ = print.StartZMm + dot.X;
            Assert.True(centreZ - print.DotDiameterMm / 2 >= surface.PathMm[0].Z - .0001);
            Assert.True(centreZ + print.DotDiameterMm / 2 <= surface.PathMm[^1].Z + .0001);
        });
        var mesh = CablePreviewGeometry.BuildMesh(scene);
        Assert.NotEmpty(mesh.Vertices);
        var side = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side);
        Assert.Equal(print.Layout.Dots.Length, side.Paths.Count(path => path.IsSurfacePrint));
        var cross = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.CrossSection);
        Assert.DoesNotContain(cross.Paths, path => path.IsSurfacePrint);
    }

    [Fact]
    public void PrintHigherThanCableCircumferenceIsRejected()
    {
        var scene = ModulePreviewSceneFactory.CreateSingleCore(0.4, 0.5, null);
        Assert.Throws<ArgumentException>(() =>
            CablePrintedSceneBuilder.Apply(scene, new("ATAG", 0.1, 7, 0.25, 0.5, 50)));
    }

    [Fact]
    public void UnknownOrNonCircularPrintableSurfaceIsRejected()
    {
        var scene = ModulePreviewSceneFactory.CreateSingleCore(2.2, 3.2, null);
        var settings = new CablePrintSettings("ATAG", 0.1, 7, 0.25, 0.2, 50);
        Assert.Throws<ArgumentException>(() => CablePrintedSceneBuilder.Apply(scene, settings, "missing-layer"));
        var changed = scene with
        {
            Components = scene.Components.Select(item => item.Id == "insulation"
                ? item with { Section = item.Section with { Shape = CablePreviewShape.Flat } }
                : item).ToImmutableArray(),
        };
        Assert.Throws<ArgumentException>(() => CablePrintedSceneBuilder.Apply(changed, settings));
    }
}
