using System.Collections.Immutable;
using System.Numerics;

namespace ATAG.Costing.Application.Visualisation;

/// <summary>Reusable optional material-layer adapters. Dimensions and pitch must be supplied by the caller.</summary>
public static class CablePreviewLayers
{
    /// <summary>
    /// Creates a tape or foil ribbon at the supplied pitch, with radial thickness and
    /// width measured perpendicular to its path. The separate end-view ring is a
    /// representative material envelope, not a calculated overlap or coverage result.
    /// </summary>
    public static ImmutableArray<CablePreviewComponent> Wrap(string id, string label,
        CablePreviewComponentKind kind, double innerDiameterMm, double thicknessMm,
        double widthMm, double pitchMm, double lengthMm, Vector4 colour, bool rightHanded = true)
    {
        if (kind is not (CablePreviewComponentKind.Tape or CablePreviewComponentKind.Foil))
            throw new ArgumentOutOfRangeException(nameof(kind), "A helical wrap must identify tape or foil.");
        if (!double.IsFinite(innerDiameterMm) || innerDiameterMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(innerDiameterMm));
        if (!double.IsFinite(thicknessMm) || thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm));
        var outerDiameter = innerDiameterMm + 2 * thicknessMm;
        var result = ImmutableArray.Create(
            new CablePreviewComponent(id, label, kind, new(widthMm, thicknessMm, CablePreviewShape.Flat),
                CablePreviewPaths.Helix((innerDiameterMm + thicknessMm) / 2, pitchMm, lengthMm, rightHanded: rightHanded),
                colour, CrossSectionVisible: false, RadialFrame: true),
            new CablePreviewComponent($"{id}-end-envelope", $"{label} · representative end envelope", kind,
                new(outerDiameter, outerDiameter, InnerWidthMm: innerDiameterMm, InnerHeightMm: innerDiameterMm),
                CablePreviewPaths.Straight(lengthMm), colour,
                SideViewVisible: false, ThreeDimensionalVisible: false));
        CablePreviewGeometry.Validate(new("layer", label, "Representative wrap geometry", result, []));
        return result;
    }
}
