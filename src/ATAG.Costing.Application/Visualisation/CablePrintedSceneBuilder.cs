using System.Collections.Immutable;
using System.Numerics;

namespace ATAG.Costing.Application.Visualisation;

/// <summary>Fits two complete impressions onto an existing approved cylindrical layer.</summary>
public static class CablePrintedSceneBuilder
{
    public static CablePreviewScene Apply(CablePreviewScene scene, CablePrintSettings settings,
        string outerComponentId = "insulation", Vector4? colour = null)
    {
        CablePreviewGeometry.Validate(scene);
        var layer = scene.Components.SingleOrDefault(component => component.Id == outerComponentId)
            ?? throw new ArgumentException("The selected printable surface is unavailable.");
        if (layer.Section.Shape != CablePreviewShape.Round ||
            Math.Abs(layer.Section.WidthMm - layer.Section.HeightMm) > .00001 ||
            layer.PathMm.Length != 2 || layer.PathMm.Any(point => Math.Abs(point.X) > .00001 || Math.Abs(point.Y) > .00001))
            throw new ArgumentException("The current print preview requires a straight circular outer surface.");

        var diameter = layer.Section.WidthMm;
        var layout = CablePrintLayout.Build(settings, marginMm: Math.Max(1, diameter));
        var length = layout.RequiredCableLengthMm;
        var start = (float)(-length / 2);
        var end = (float)(length / 2);
        var oldMin = layer.PathMm.Min(point => point.Z);
        var oldMax = layer.PathMm.Max(point => point.Z);
        var oldLength = oldMax - oldMin;
        if (oldLength <= 0) throw new ArgumentException("The printable surface must advance along the cable axis.");

        // Preserve each supplied cross-section and a small axial cutaway. Only
        // representative sample length changes; saved quoted length is untouched.
        var components = scene.Components.Select(component =>
        {
            if (component.PathMm.Length != 2)
                throw new ArgumentException("A printed straight-cable sample requires straight layer paths.");
            var componentStart = component.PathMm[0];
            var componentEnd = component.PathMm[^1];
            var reveal = component.Id == outerComponentId ? 0 :
                Math.Max(0, (componentEnd.Z - oldMax) / oldLength) * diameter * 4;
            return component with
            {
                PathMm = [new(componentStart.X, componentStart.Y, start),
                    new(componentEnd.X, componentEnd.Y, end + (float)reveal)],
            };
        }).ToImmutableArray();
        return CablePreviewScene.Create(scene.Source, scene.Title,
            scene.Description + $" · printed sample {length:0.###} mm · two full impressions",
            components, scene.Notes.AddRange(layout.Notes),
            new CablePreviewPrint(layout, diameter, settings.DotDiameterMm,
                colour ?? Vector4.One, start));
    }
}
