using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using ATAG.Costing.Domain.Braiding;
using ATAG.Costing.Domain.Coiling;
using ATAG.Costing.Domain.Conductors;

namespace ATAG.Costing.Application.Visualisation;

/// <summary>
/// Maps already-calculated module dimensions to the shared visual contract.
/// None of these presentation limits or illustrative dimensions feeds costing.
/// </summary>
public static class ModulePreviewSceneFactory
{
    public const int MaximumDetailedConductorStrands = 384;
    public const int MaximumVisibleCoilTurns = 32;
    private static readonly Vector4 Copper = new(0.78f, 0.47f, 0.18f, 1f);
    private static readonly Vector4 Insulation = new(0.22f, 0.62f, 0.77f, 1f);
    private static readonly Vector4 Sheath = new(0.14f, 0.34f, 0.50f, 1f);
    private static readonly Vector4[] CoreColours =
    [
        new(0.25f, 0.62f, 0.77f, 1), new(0.88f, 0.56f, 0.21f, 1),
        new(0.32f, 0.69f, 0.44f, 1), new(0.60f, 0.46f, 0.80f, 1),
        new(0.85f, 0.37f, 0.36f, 1), new(0.79f, 0.73f, 0.29f, 1),
    ];

    public static CablePreviewScene Unavailable(string source, string title, string reason) =>
        CablePreviewScene.Create(source, title, reason, []);

    public static CablePreviewScene CreateSingleCore(
        double conductorOd, double finishedOd, ConductorConstructionResult? construction,
        string? conductorColour = null, string? insulationColour = null)
    {
        if (!Positive(conductorOd) || !Positive(finishedOd) || finishedOd <= conductorOd)
            return Unavailable("cor", "Single core", "Enter a valid conductor OD and a larger finished core OD.");

        var components = new List<CablePreviewComponent>();
        var notes = new List<string>();
        var length = finishedOd * 8;
        AddConductor(components, notes, conductorOd, length, construction, Colour(conductorColour, Copper));
        components.Add(new("insulation", "Insulation", CablePreviewComponentKind.Insulation,
            new(finishedOd, finishedOd, InnerWidthMm: conductorOd, InnerHeightMm: conductorOd),
            Straight(length * 0.65), Colour(insulationColour, Insulation)));
        notes.Add("Side view uses a cutaway to reveal layers. Strand packing is schematic; no conductor lay pitch is supplied.");
        return CablePreviewScene.Create("cor", "Single core",
            $"Conductor {conductorOd:0.###} mm · finished OD {finishedOd:0.###} mm", components, notes);
    }

    public static CablePreviewScene CreateDual(
        double conductorOd, double firstOd, double finalOd, ConductorConstructionResult? construction,
        string? firstColour = null, string? secondColour = null, IEnumerable<string>? unspecifiedLayers = null)
    {
        if (!Positive(conductorOd) || !Positive(firstOd) || !Positive(finalOd) ||
            firstOd <= conductorOd || finalOd <= firstOd)
            return Unavailable("dual", "Dual insulation", "Enter increasing valid conductor, first insulation and final outside diameters.");

        var components = new List<CablePreviewComponent>();
        var notes = new List<string>();
        var length = finalOd * 8;
        AddConductor(components, notes, conductorOd, length, construction, Copper);
        components.Add(new("first-insulation", "First insulation", CablePreviewComponentKind.Insulation,
            new(firstOd, firstOd, InnerWidthMm: conductorOd, InnerHeightMm: conductorOd),
            Straight(length * 0.77), Colour(firstColour, Insulation)));
        components.Add(new("second-insulation", "Second insulation", CablePreviewComponentKind.Insulation,
            new(finalOd, finalOd, InnerWidthMm: firstOd, InnerHeightMm: firstOd),
            Straight(length * 0.54), Colour(secondColour, Sheath)));
        var missing = unspecifiedLayers?.ToArray() ?? [];
        if (missing.Length > 0)
            notes.Add($"Selected but not drawn: {string.Join(", ", missing)}. These module selections do not yet contain physical thickness, width, pitch or placement data.");
        notes.Add("Side view is a layer cutaway; conductor strand lay remains illustrative.");
        return CablePreviewScene.Create("dual", "Dual insulation",
            $"Conductor {conductorOd:0.###} mm · first OD {firstOd:0.###} mm · final OD {finalOd:0.###} mm", components, notes);
    }

    public static CablePreviewScene CreateBraid(BraidCoreLayout? layout, double coreOd,
        double meanOd, int endsPerCarrier, double wireOd, int carriers, double pitch,
        IEnumerable<string>? resultNotes = null)
    {
        if (layout is null || !Positive(coreOd) || !Positive(meanOd) || !Positive(wireOd) ||
            !Positive(pitch) || endsPerCarrier is < 1 or > 10 || carriers is not (16 or 24))
            return Unavailable("braid", "Braid coverage", "Choose a valid retained Copper wire, core group and calculated braid pitch.");

        var components = new List<CablePreviewComponent>();
        var notes = resultNotes?.ToList() ?? [];
        var length = Math.Min(pitch * 2, coreOd * 30);
        var cores = CoreCentres(layout, coreOd);
        if (cores is null)
            return Unavailable("braid", "Braid coverage", InvalidCoreGroupMessage(layout));
        var coreExtent = cores.Max(core => core.Length()) + (coreOd / 2);
        for (var index = 0; index < cores.Count; index++)
            components.Add(new($"core-{index}", $"Core {index + 1}", CablePreviewComponentKind.Core,
                new(coreOd, coreOd), Straight(length, cores[index]), CoreColours[index % CoreColours.Length]));

        var braidRadius = Math.Max(meanOd / 2, coreExtent) + (wireOd / 2);
        if (Math.Abs((coreExtent * 2) - meanOd) > coreOd * 0.01)
            notes.Add("Equal-core ring placement is illustrative. Its visible envelope differs from the workbook OD factor; the labelled mean OD and calculated pitch remain authoritative.");
        for (var carrier = 0; carrier < carriers; carrier++)
        {
            var rightHanded = carrier % 2 == 0;
            var group = carrier / 2;
            var phase = Math.Tau * group / (carriers / 2d);
            var colour = rightHanded ? Copper : new Vector4(0.87f, 0.74f, 0.51f, 1);
            components.Add(new($"carrier-{carrier}", $"Carrier {carrier + 1} bundle", CablePreviewComponentKind.Braid,
                new(wireOd * endsPerCarrier, wireOd, CablePreviewShape.Flat),
                BraidPath(braidRadius, wireOd, pitch, length, phase, rightHanded, carriers), colour,
                SimpleOnly: true, RadialFrame: true));
            for (var end = 0; end < endsPerCarrier; end++)
            {
                var strandPhase = phase + ((end - ((endsPerCarrier - 1) / 2d)) * wireOd / braidRadius);
                components.Add(new($"carrier-{carrier}-end-{end}", $"Carrier {carrier + 1} · end {end + 1}",
                    CablePreviewComponentKind.Braid, new(wireOd, wireOd),
                    BraidPath(braidRadius, wireOd, pitch, length, strandPhase, rightHanded, carriers), colour,
                    DetailOnly: true));
            }
        }
        notes.Add("The two carrier directions have alternating over-under clearance. Detailed mode separates every retained end; cross-section is a representative cut through the weave.");
        return CablePreviewScene.Create("braid", $"{carriers}-carrier braid",
            $"Pitch {pitch:0.###} mm · wire Ø {wireOd:0.###} mm · {endsPerCarrier} ends/carrier · {carriers * endsPerCarrier} strands · mean OD {meanOd:0.###} mm",
            components, notes);
    }

    public static CablePreviewScene CreateBuncher(BraidCoreLayout? layout, double layLength)
    {
        if (layout is null || !Positive(layLength))
            return Unavailable("buncher", "Core lay", "Choose a retained core group and target lay length.");
        const double illustrativeCoreOd = 2;
        var cores = CoreCentres(layout, illustrativeCoreOd);
        if (cores is null)
            return Unavailable("buncher", "Core lay", InvalidCoreGroupMessage(layout));
        var components = cores.Select((centre, index) => new CablePreviewComponent(
            $"core-{index}", $"Core {index + 1}", CablePreviewComponentKind.Core,
            new(illustrativeCoreOd, illustrativeCoreOd),
            Helix(centre.Length(), layLength, layLength * 2, Math.Atan2(centre.Y, centre.X)),
            CoreColours[index % CoreColours.Length], CrossSectionCentreMm: centre));
        return CablePreviewScene.Create("buncher", "Core lay",
            $"{layout.CoreCount} equal cable cores · group {(string.IsNullOrWhiteSpace(layout.Layup) ? layout.CoreCount.ToString(CultureInfo.InvariantCulture) : layout.Layup)} · lay {layLength:0.###} mm",
            components,
            ["Core OD is unspecified in this module: equal 2 mm cores are illustrative. Only the retained group count and selected lay length are supplied. Colours identify cores, not a specified colour sequence."]);
    }

    public static CablePreviewScene CreateCoil(CoilCableShape shape, CoilCableLengthResult? result,
        double tailOne, double tailTwo, double stripOne, double stripTwo)
    {
        if (result is null || !Positive(result.RadialCableThicknessMillimetres) ||
            !Positive(result.AxialPitchMillimetres) || !Positive(result.RequiredBarDiameterMillimetres) ||
            result.CompleteTurns <= 0 || new[] { tailOne, tailTwo, stripOne, stripTwo }.Any(value => !NonNegative(value)))
            return Unavailable("coil", "Coil planning", "Enter valid cable and finished-coil dimensions.");

        var notes = new List<string>();
        var turns = Math.Min(result.CompleteTurns, MaximumVisibleCoilTurns);
        if (turns < result.CompleteTurns)
            notes.Add($"Representative view: {turns} of {result.CompleteTurns:N0} complete turns drawn at the actual pitch. The displayed wound width is the full calculated result.");
        var sectionShape = shape switch
        {
            CoilCableShape.Flat => CablePreviewShape.Flat,
            CoilCableShape.DShape => CablePreviewShape.DShape,
            _ => CablePreviewShape.Round,
        };
        var section = new CablePreviewSection(result.AxialPitchMillimetres,
            result.RadialCableThicknessMillimetres, sectionShape);
        var visibleLength = turns * result.AxialPitchMillimetres;
        var radius = result.MeanPathDiameterMillimetres / 2;
        var path = Helix(radius, result.AxialPitchMillimetres, visibleLength);
        var components = new List<CablePreviewComponent>
        {
            new("coil", "Wound cable", CablePreviewComponentKind.Coil, section, path, Insulation,
                CrossSectionCentreMm: Vector2.Zero, RadialFrame: true),
            new("bar", "Winding bar", CablePreviewComponentKind.Bar,
                new(result.RequiredBarDiameterMillimetres, result.RequiredBarDiameterMillimetres),
                Straight(visibleLength + (2 * result.AxialPitchMillimetres)), new(0.48f, 0.53f, 0.58f, 1),
                CrossSectionVisible: false),
        };
        AddTail(components, "start", path[0], -Vector3.UnitY, section, tailOne, stripOne);
        AddTail(components, "end", path[^1], Vector3.UnitY, section, tailTwo, stripTwo);
        notes.Add("Cable cross-section is shown separately. Tails leave complete turns parallel; stripping allowances extend each tail. Strip colours identify allowances and do not imply an imported internal conductor construction.");
        return CablePreviewScene.Create("coil", $"{shape} cable coil",
            $"Bar Ø {result.RequiredBarDiameterMillimetres:0.###} mm · {result.CompleteTurns:N0} turns · wound width {result.ActualWoundAxialLengthMillimetres:0.###} mm · radial height {result.RadialCableThicknessMillimetres:0.###} mm · axial pitch {result.AxialPitchMillimetres:0.###} mm",
            components, notes);
    }

    private static void AddConductor(List<CablePreviewComponent> components, List<string> notes,
        double diameter, double length, ConductorConstructionResult? construction, Vector4 colour)
    {
        var detailed = construction is { TotalStrandCount: > 0 and <= MaximumDetailedConductorStrands };
        if (construction is { TotalStrandCount: > MaximumDetailedConductorStrands })
            notes.Add($"Conductor has {construction.TotalStrandCount:N0} strands: an envelope replaces strand geometry above the {MaximumDetailedConductorStrands}-strand interactive detail limit.");
        if (construction is null)
            notes.Add("No reliable numeric conductor stranding is available; the supplied conductor OD is shown as an envelope.");
        components.Add(new("conductor-envelope", "Conductor envelope", CablePreviewComponentKind.Conductor,
            new(diameter, diameter), Straight(length), colour, SimpleOnly: detailed));
        if (!detailed) return;
        var layout = ConductorPreviewLayoutBuilder.Create(construction!, 0, 0, diameter / 2);
        for (var index = 0; index < layout.Strands.Count; index++)
        {
            var strand = layout.Strands[index];
            components.Add(new($"strand-{index}", $"Conductor strand {index + 1}", CablePreviewComponentKind.Conductor,
                new(strand.Radius * 2, strand.Radius * 2),
                Straight(length, new((float)strand.X, (float)strand.Y)), colour, DetailOnly: true));
        }
    }

    private static IReadOnlyList<Vector2>? CoreCentres(BraidCoreLayout layout, double diameter)
    {
        var layers = string.IsNullOrWhiteSpace(layout.Layup) ? [layout.CoreCount] :
            layout.Layup.Split('-', StringSplitOptions.TrimEntries).Select(value =>
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : 0).ToArray();
        if (layers.Any(count => count <= 0) || layers.Sum() != layout.CoreCount || layout.CoreCount > 45)
            return null;
        var centres = new List<Vector2>();
        double previousRadius = 0;
        for (var layer = 0; layer < layers.Length; layer++)
        {
            var count = layers[layer];
            var radius = layer == 0 && count == 1 ? 0 :
                Math.Max(layer == 0 ? 0 : previousRadius + diameter,
                    count == 1 ? 0 : diameter / (2 * Math.Sin(Math.PI / count)));
            for (var index = 0; index < count; index++)
            {
                var phase = -Math.PI / 2 + (Math.Tau * index / count);
                centres.Add(new((float)(radius * Math.Cos(phase)), (float)(radius * Math.Sin(phase))));
            }
            previousRadius = radius;
        }
        return centres;
    }

    private static string InvalidCoreGroupMessage(BraidCoreLayout layout) =>
        $"The retained {layout.CoreCount}-core row has an inconsistent lay-up ({layout.Layup}). " +
        "The preview is unavailable until that reference row is corrected; no extra core is invented or removed.";

    private static void AddTail(List<CablePreviewComponent> components, string id, Vector3 start,
        Vector3 direction, CablePreviewSection section, double tail, double strip)
    {
        var end = start + (direction * (float)tail);
        if (tail > 0)
            components.Add(new($"tail-{id}", $"{id} tail", CablePreviewComponentKind.Tail,
                section, [start, end], Insulation, CrossSectionVisible: false, RadialFrame: true));
        if (strip > 0)
            components.Add(new($"strip-{id}", $"{id} stripping allowance", CablePreviewComponentKind.Tail,
                section, [end, end + (direction * (float)strip)], Copper, CrossSectionVisible: false, RadialFrame: true));
    }

    private static ImmutableArray<Vector3> Straight(double length, Vector2 centre = default) =>
        [new(centre.X, centre.Y, (float)(-length / 2)), new(centre.X, centre.Y, (float)(length / 2))];

    private static ImmutableArray<Vector3> Helix(double radius, double pitch, double length,
        double phase = 0, bool rightHanded = true)
    {
        if (radius < 0.000001) return Straight(length);
        var samples = Math.Clamp((int)Math.Ceiling(Math.Min(length / pitch, 32) * 48), 24, 1536);
        return Enumerable.Range(0, samples + 1).Select(index =>
        {
            var z = length * index / samples;
            var angle = phase + ((rightHanded ? 1 : -1) * Math.Tau * z / pitch);
            return new Vector3((float)(radius * Math.Cos(angle)), (float)(radius * Math.Sin(angle)), (float)(z - length / 2));
        }).ToImmutableArray();
    }

    private static ImmutableArray<Vector3> BraidPath(double radius, double wireDiameter, double pitch,
        double length, double phase, bool rightHanded, int carriers)
    {
        var source = Helix(radius, pitch, length, phase, rightHanded);
        return source.Select((point, index) =>
        {
            var z = length * index / (source.Length - 1d);
            // A carrier meets the opposite family every 1/carriers revolution.
            // Half that crossing frequency alternates over and under, while the
            // direction sign gives both intersecting wires the same phase.
            var weave = Math.Cos(((rightHanded ? 1 : -1) * Math.Tau * z / pitch * carriers / 2)
                + (phase * carriers / 2));
            var radial = radius + ((rightHanded ? 1 : -1) * wireDiameter * 0.6 * weave);
            return new Vector3(point.X * (float)(radial / radius), point.Y * (float)(radial / radius), point.Z);
        }).ToImmutableArray();
    }

    private static bool Positive(double value) => double.IsFinite(value) && value > 0 && value < 1e7;
    private static bool NonNegative(double value) => double.IsFinite(value) && value >= 0 && value < 1e7;
    private static Vector4 Colour(string? hex, Vector4 fallback)
    {
        var value = hex?.Trim().TrimStart('#');
        if (value?.Length != 6 || !uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)) return fallback;
        return new((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
