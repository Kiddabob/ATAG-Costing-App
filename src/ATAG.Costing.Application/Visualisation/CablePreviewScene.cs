using System.Collections.Immutable;
using System.Numerics;

namespace ATAG.Costing.Application.Visualisation;

public enum CablePreviewShape { Round, Flat, DShape }
public enum CablePreviewComponentKind { Conductor, Core, Insulation, Tape, Foil, Braid, Drain, Coil, Tail, Bar, Other }
public enum CablePreviewProjection { CrossSection, Side }

/// <summary>Physical section dimensions in millimetres. Inner dimensions describe an annulus.</summary>
public sealed record CablePreviewSection(
    double WidthMm,
    double HeightMm,
    CablePreviewShape Shape = CablePreviewShape.Round,
    double InnerWidthMm = 0,
    double InnerHeightMm = 0);

/// <summary>
/// One material swept along a physical centreline. Z is the straight cable axis;
/// X/Y are the end view. Flat section width follows the transported X frame.
/// CrossSectionCentreMm identifies its representative end-view position.
/// DetailOnly and SimpleOnly permit an envelope and its strands without double drawing.
/// </summary>
public sealed record CablePreviewComponent(
    string Id,
    string Label,
    CablePreviewComponentKind Kind,
    CablePreviewSection Section,
    ImmutableArray<Vector3> PathMm,
    Vector4 Colour,
    bool DetailOnly = false,
    bool SimpleOnly = false,
    bool CrossSectionVisible = true,
    Vector2? CrossSectionCentreMm = null,
    bool RadialFrame = false,
    bool SideViewVisible = true,
    bool ThreeDimensionalVisible = true);

/// <summary>Presentation-only immutable snapshot. Geometry never supplies engineering results.</summary>
public sealed record CablePreviewScene(
    string Source,
    string Title,
    string Description,
    ImmutableArray<CablePreviewComponent> Components,
    ImmutableArray<string> Notes,
    CablePreviewPrint? Print = null)
{
    public static CablePreviewScene Create(string source, string title, string description,
        IEnumerable<CablePreviewComponent> components, IEnumerable<string>? notes = null,
        CablePreviewPrint? print = null)
    {
        var scene = new CablePreviewScene(source, title, description, components.ToImmutableArray(),
            notes?.ToImmutableArray() ?? [], print);
        CablePreviewGeometry.Validate(scene);
        return scene;
    }
}

public readonly record struct CablePreviewVertex(Vector3 Position, Vector3 Normal, Vector4 Colour);
public sealed record CablePreviewMesh(
    ImmutableArray<CablePreviewVertex> Vertices,
    ImmutableArray<uint> Indices,
    Vector3 MinimumMm,
    Vector3 MaximumMm,
    ImmutableArray<string> Notes);

/// <summary>Closed filled outline, optionally with an inner cutout. Coordinates remain millimetres.</summary>
public sealed record CablePreviewPath2D(
    string ComponentId,
    ImmutableArray<Vector2> Points,
    ImmutableArray<Vector2> InnerPoints,
    Vector4 Colour,
    bool IsDetail,
    float DepthMm = 0,
    bool IsSurfacePrint = false);
public sealed record CablePreviewDrawing(
    ImmutableArray<CablePreviewPath2D> Paths,
    Vector2 MinimumMm,
    Vector2 MaximumMm,
    ImmutableArray<string> Notes);

public sealed record CablePreviewGeometryBudget(
    int MaximumComponents = 1024,
    int MaximumPathPoints = 262144,
    int MaximumVertices = 240000,
    int MaximumIndices = 1200000);

public sealed class CablePreviewGeometryLimitException(string message) : ArgumentException(message);
