using System.Collections.Immutable;
using System.Numerics;

namespace ATAG.Costing.Application.Visualisation;

/// <summary>Bounded presentation centrelines in millimetres; these are not costing calculations.</summary>
public static class CablePreviewPaths
{
    public static ImmutableArray<Vector3> Straight(double lengthMm, Vector2 centre = default)
    {
        Positive(lengthMm, nameof(lengthMm));
        return [new(centre, 0), new(centre, (float)lengthMm)];
    }

    public static ImmutableArray<Vector3> Helix(double radiusMm, double pitchMm, double lengthMm,
        double phaseRadians = 0, bool rightHanded = true, Vector2 centre = default)
    {
        Nonnegative(radiusMm, nameof(radiusMm));
        Positive(pitchMm, nameof(pitchMm));
        Positive(lengthMm, nameof(lengthMm));
        if (!double.IsFinite(phaseRadians)) throw new ArgumentOutOfRangeException(nameof(phaseRadians));
        if (radiusMm == 0) return Straight(lengthMm, centre);
        var turns = lengthMm / pitchMm;
        // Never silently alias a long/short-pitch helix to a different apparent lay.
        if (turns > 32) throw new CablePreviewGeometryLimitException("A LIVE Preview displays at most 32 helical turns. Choose a representative length.");
        var segments = Math.Max(8, (int)Math.Ceiling(turns * 24));
        var result = ImmutableArray.CreateBuilder<Vector3>(segments + 1);
        for (var index = 0; index <= segments; index++)
        {
            var fraction = (double)index / segments;
            var angle = phaseRadians + (rightHanded ? 1 : -1) * Math.Tau * turns * fraction;
            result.Add(new((float)(centre.X + radiusMm * Math.Cos(angle)),
                (float)(centre.Y + radiusMm * Math.Sin(angle)), (float)(lengthMm * fraction)));
        }
        return result.MoveToImmutable();
    }

    private static void Positive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0 || value > 1000000) throw new ArgumentOutOfRangeException(name);
    }

    private static void Nonnegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0 || value > 1000000) throw new ArgumentOutOfRangeException(name);
    }
}
