using System.Collections.Immutable;
using System.Numerics;

namespace ATAG.Costing.Application.Visualisation;

public sealed record CablePrintSettings(string Text, double DotDiameterMm, int DotsHigh,
    double HorizontalPitchMm, double VerticalPitchMm, double RepeatDistanceMm);

public sealed record CablePrintLayoutResult(ImmutableArray<Vector2> Dots,
    double PrintedWidthMm, double PrintedHeightMm, double RequiredCableLengthMm,
    ImmutableArray<string> Notes);

/// <summary>Ink on a supplied cylindrical surface; no material or costing rule.</summary>
public sealed record CablePreviewPrint(CablePrintLayoutResult Layout, double SurfaceDiameterMm,
    double DotDiameterMm, Vector4 Colour, double StartZMm = 0);

/// <summary>
/// Deterministic, bounded dot-matrix presentation. Pitch is centre-to-centre.
/// The two complete impressions retain the exact requested start-to-start repeat.
/// The built-in 5x7 reference alphabet is illustrative, not a printer firmware font.
/// </summary>
public static class CablePrintLayout
{
    public const int MaximumDots = 20000;

    public static CablePrintLayoutResult Build(CablePrintSettings settings, double marginMm = 2)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.Text) || settings.Text.Length > 160 ||
            settings.Text.Any(char.IsControl))
            throw new ArgumentException("Print text must contain 1 to 160 characters on one line.");
        if (settings.DotsHigh is < 5 or > 64)
            throw new ArgumentException("Choose a whole-number print height from 5 to 64 dots.");
        if (!Positive(settings.DotDiameterMm) || !Positive(settings.HorizontalPitchMm) ||
            !Positive(settings.VerticalPitchMm) || !Positive(settings.RepeatDistanceMm) ||
            !double.IsFinite(marginMm) || marginMm < 0 || marginMm > 1000)
            throw new ArgumentException("Dot diameter, dot pitches and repeat distance must be positive finite dimensions.");

        var columns = Math.Max(3, (int)Math.Round(settings.DotsHigh * 5d / 7));
        var characterAdvance = (columns + 1) * settings.HorizontalPitchMm;
        var printedWidth = (settings.Text.Length - 1) * characterAdvance +
            (columns - 1) * settings.HorizontalPitchMm + settings.DotDiameterMm;
        var printedHeight = (settings.DotsHigh - 1) * settings.VerticalPitchMm + settings.DotDiameterMm;
        var cableLength = settings.RepeatDistanceMm + printedWidth + 2 * marginMm;
        if (cableLength > 1000000 || printedHeight > 1000000)
            throw new ArgumentException("The requested print exceeds the supported preview dimensions.");

        var dots = ImmutableArray.CreateBuilder<Vector2>();
        var unsupported = new HashSet<char>();
        for (var repeat = 0; repeat < 2; repeat++)
        for (var character = 0; character < settings.Text.Length; character++)
        {
            var letter = settings.Text[character];
            if (!Glyphs.TryGetValue(letter, out var glyph))
            {
                glyph = Glyphs['?'];
                unsupported.Add(letter);
            }
            for (var row = 0; row < settings.DotsHigh; row++)
            for (var column = 0; column < columns; column++)
            {
                var sourceRow = Math.Min(6, row * 7 / settings.DotsHigh);
                var sourceColumn = Math.Min(4, column * 5 / columns);
                if (glyph[sourceRow * 5 + sourceColumn] != '1') continue;
                if (dots.Count == MaximumDots)
                    throw new CablePreviewGeometryLimitException("The two prints exceed the 20,000-dot preview limit. Reduce text length or dots high.");
                var axial = marginMm + settings.DotDiameterMm / 2 +
                    repeat * settings.RepeatDistanceMm + character * characterAdvance +
                    column * settings.HorizontalPitchMm;
                var arc = ((settings.DotsHigh - 1) / 2d - row) * settings.VerticalPitchMm;
                dots.Add(new((float)axial, (float)arc));
            }
        }

        var notes = ImmutableArray.CreateBuilder<string>();
        notes.Add($"Two full dot-matrix prints · {settings.DotsHigh} dots high · dot Ø {settings.DotDiameterMm:0.###} mm · horizontal/vertical pitch {settings.HorizontalPitchMm:0.###}/{settings.VerticalPitchMm:0.###} mm (centre-to-centre). Reference bitmap font; verify the final printer font during setup.");
        if (unsupported.Count > 0)
            notes.Add($"The reference font shows '?' for unsupported characters: {string.Join(", ", unsupported)}. Saved print text is unchanged.");
        if (settings.RepeatDistanceMm < printedWidth)
            notes.Add("Repeat distance is shorter than one complete print: the impressions overlap at the supplied spacing.");
        if (settings.DotDiameterMm > Math.Min(settings.HorizontalPitchMm, settings.VerticalPitchMm))
            notes.Add("Dot diameter exceeds a centre-to-centre pitch, so neighbouring ink dots overlap.");
        return new(dots.ToImmutable(), printedWidth, printedHeight, cableLength, notes.ToImmutable());
    }

    private static bool Positive(double value) => double.IsFinite(value) && value > 0 && value <= 10000;

    private static readonly IReadOnlyDictionary<char, string> Glyphs = MakeGlyphs();

    private static IReadOnlyDictionary<char, string> MakeGlyphs()
    {
        // Rows are authored here as a small technical reference alphabet.
        var rows = new Dictionary<char, string>
        {
            [' ']="00000/00000/00000/00000/00000/00000/00000",
            ['A']="01110/10001/10001/11111/10001/10001/10001",
            ['B']="11110/10001/10001/11110/10001/10001/11110",
            ['C']="01111/10000/10000/10000/10000/10000/01111",
            ['D']="11110/10001/10001/10001/10001/10001/11110",
            ['E']="11111/10000/10000/11110/10000/10000/11111",
            ['F']="11111/10000/10000/11110/10000/10000/10000",
            ['G']="01111/10000/10000/10111/10001/10001/01111",
            ['H']="10001/10001/10001/11111/10001/10001/10001",
            ['I']="11111/00100/00100/00100/00100/00100/11111",
            ['J']="00111/00010/00010/00010/10010/10010/01100",
            ['K']="10001/10010/10100/11000/10100/10010/10001",
            ['L']="10000/10000/10000/10000/10000/10000/11111",
            ['M']="10001/11011/10101/10101/10001/10001/10001",
            ['N']="10001/11001/10101/10011/10001/10001/10001",
            ['O']="01110/10001/10001/10001/10001/10001/01110",
            ['P']="11110/10001/10001/11110/10000/10000/10000",
            ['Q']="01110/10001/10001/10001/10101/10010/01101",
            ['R']="11110/10001/10001/11110/10100/10010/10001",
            ['S']="01111/10000/10000/01110/00001/00001/11110",
            ['T']="11111/00100/00100/00100/00100/00100/00100",
            ['U']="10001/10001/10001/10001/10001/10001/01110",
            ['V']="10001/10001/10001/10001/10001/01010/00100",
            ['W']="10001/10001/10001/10101/10101/11011/10001",
            ['X']="10001/10001/01010/00100/01010/10001/10001",
            ['Y']="10001/10001/01010/00100/00100/00100/00100",
            ['Z']="11111/00001/00010/00100/01000/10000/11111",
            ['a']="00000/00000/01110/00001/01111/10001/01111",
            ['b']="10000/10000/10110/11001/10001/10001/11110",
            ['c']="00000/00000/01111/10000/10000/10000/01111",
            ['d']="00001/00001/01101/10011/10001/10001/01111",
            ['e']="00000/00000/01110/10001/11111/10000/01111",
            ['f']="00110/01001/01000/11100/01000/01000/01000",
            ['g']="00000/01111/10001/10001/01111/00001/01110",
            ['h']="10000/10000/10110/11001/10001/10001/10001",
            ['i']="00100/00000/01100/00100/00100/00100/01110",
            ['j']="00010/00000/00110/00010/00010/10010/01100",
            ['k']="10000/10000/10010/10100/11000/10100/10010",
            ['l']="01100/00100/00100/00100/00100/00100/01110",
            ['m']="00000/00000/11010/10101/10101/10101/10101",
            ['n']="00000/00000/10110/11001/10001/10001/10001",
            ['o']="00000/00000/01110/10001/10001/10001/01110",
            ['p']="00000/00000/11110/10001/11110/10000/10000",
            ['q']="00000/00000/01111/10001/01111/00001/00001",
            ['r']="00000/00000/10111/11000/10000/10000/10000",
            ['s']="00000/00000/01111/10000/01110/00001/11110",
            ['t']="01000/01000/11100/01000/01000/01001/00110",
            ['u']="00000/00000/10001/10001/10001/10011/01101",
            ['v']="00000/00000/10001/10001/10001/01010/00100",
            ['w']="00000/00000/10001/10001/10101/10101/01010",
            ['x']="00000/00000/10001/01010/00100/01010/10001",
            ['y']="00000/00000/10001/10001/01111/00001/01110",
            ['z']="00000/00000/11111/00010/00100/01000/11111",
            ['0']="01110/10001/10011/10101/11001/10001/01110",
            ['1']="00100/01100/00100/00100/00100/00100/01110",
            ['2']="01110/10001/00001/00010/00100/01000/11111",
            ['3']="11110/00001/00001/01110/00001/00001/11110",
            ['4']="00010/00110/01010/10010/11111/00010/00010",
            ['5']="11111/10000/10000/11110/00001/00001/11110",
            ['6']="01110/10000/10000/11110/10001/10001/01110",
            ['7']="11111/00001/00010/00100/01000/01000/01000",
            ['8']="01110/10001/10001/01110/10001/10001/01110",
            ['9']="01110/10001/10001/01111/00001/00001/01110",
            ['.']="00000/00000/00000/00000/00000/00110/00110",
            [',']="00000/00000/00000/00000/00110/00100/01000",
            [':']="00000/00110/00110/00000/00110/00110/00000",
            [';']="00000/00110/00110/00000/00110/00100/01000",
            ['-']="00000/00000/00000/11111/00000/00000/00000",
            ['_']="00000/00000/00000/00000/00000/00000/11111",
            ['/']="00001/00010/00010/00100/01000/01000/10000",
            ['\\']="10000/01000/01000/00100/00010/00010/00001",
            ['(']="00010/00100/01000/01000/01000/00100/00010",
            [')']="01000/00100/00010/00010/00010/00100/01000",
            ['[']="01110/01000/01000/01000/01000/01000/01110",
            [']']="01110/00010/00010/00010/00010/00010/01110",
            ['+']="00000/00100/00100/11111/00100/00100/00000",
            ['=']="00000/00000/11111/00000/11111/00000/00000",
            ['%']="11001/11010/00100/00100/01000/10110/00110",
            ['?']="01110/10001/00001/00010/00100/00000/00100",
            ['!']="00100/00100/00100/00100/00100/00000/00100",
            ['#']="01010/11111/01010/01010/11111/01010/00000",
            ['*']="00000/10101/01110/11111/01110/10101/00000",
            ['°']="01100/10010/10010/01100/00000/00000/00000",
            ['²']="01110/00001/00110/01000/01111/00000/00000",
        };
        return rows.ToDictionary(pair => pair.Key, pair => pair.Value.Replace("/", "", StringComparison.Ordinal));
    }
}
