using System.Globalization;

namespace ATAG.Costing.Application.CentralData;

/// <summary>
/// Searches retained masterbatch colours by text and by human colour
/// descriptions. The HSL ranges deliberately preserve the useful workbook VBA
/// behaviour while allowing combined searches such as "dark blue" or
/// "warm pastel".
/// </summary>
public static class MasterbatchColourSearch
{
    public static IReadOnlyList<string> GroupOptions { get; } =
    [
        "All colours",
        "Light",
        "Dark",
        "Pastel",
        "Bright / vivid",
        "Muted / dull",
        "Neutral",
        "Warm",
        "Cool",
        "Black",
        "White",
        "Grey",
        "Red",
        "Orange",
        "Yellow",
        "Lime",
        "Green",
        "Cyan / turquoise / teal",
        "Blue",
        "Purple / violet",
        "Pink / magenta",
        "Brown",
        "Navy",
        "Maroon / burgundy",
        "Cream",
        "Beige / tan",
        "Olive",
        "Gold",
        "Silver",
        "Charcoal",
    ];

    public static bool Matches(
        MasterbatchReference colour,
        string? searchText,
        string? groupFilter = null,
        string? typeFilter = null)
    {
        ArgumentNullException.ThrowIfNull(colour);

        if (!string.IsNullOrWhiteSpace(typeFilter) &&
            !ContainsText(colour.ColourType, typeFilter))
        {
            return false;
        }

        var normalizedGroup = NormalizeGroup(groupFilter);
        if (!string.IsNullOrEmpty(normalizedGroup) &&
            !HexLooksLikeSearchColour(colour.ColourHex, normalizedGroup))
        {
            return false;
        }

        var tokens = Tokenize(searchText);
        return tokens.Count == 0 ||
               tokens.All(token =>
                   TextLooksLikeSearch(colour, token) ||
                   HexLooksLikeSearchColour(colour.ColourHex, token));
    }

    public static string Describe(MasterbatchReference colour)
    {
        ArgumentNullException.ThrowIfNull(colour);
        if (!TryParseHex(colour.ColourHex, out var red, out var green, out var blue))
        {
            return string.IsNullOrWhiteSpace(colour.ColourType)
                ? "No colour preview recorded"
                : colour.ColourType.Trim();
        }

        var (hue, saturation, lightness) = ToHsl(red, green, blue);
        var tags = new List<string>();

        if (lightness >= 0.68)
        {
            tags.Add("Light");
        }
        else if (lightness <= 0.32)
        {
            tags.Add("Dark");
        }

        if (lightness >= 0.70 && saturation is >= 0.15 and <= 0.55)
        {
            tags.Add("Pastel");
        }
        else if (saturation >= 0.55 && lightness is >= 0.35 and <= 0.75)
        {
            tags.Add("Vivid");
        }
        else if (saturation <= 0.30 && lightness is > 0.20 and < 0.85)
        {
            tags.Add("Muted");
        }

        if (saturation <= 0.18)
        {
            tags.Add("Neutral");
        }
        else
        {
            tags.Add(
                HueInRange(hue, 0, 70) || HueInRange(hue, 330, 360)
                    ? "Warm"
                    : "Cool");
        }

        var family = MainFamily(hue, saturation, lightness);
        if (!string.IsNullOrEmpty(family))
        {
            tags.Add(family);
        }

        if (!string.IsNullOrWhiteSpace(colour.ColourType))
        {
            tags.Add(colour.ColourType.Trim());
        }

        return string.Join(
            " · ",
            tags.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public static bool HexLooksLikeSearchColour(
        string? hexCode,
        string? searchText)
    {
        if (!TryParseHex(hexCode, out var red, out var green, out var blue))
        {
            return false;
        }

        var search = NormalizeToken(searchText);
        var (hue, saturation, lightness) = ToHsl(red, green, blue);

        return search switch
        {
            "light" => lightness >= 0.68,
            "dark" => lightness <= 0.32,
            "pastel" => lightness >= 0.70 &&
                        saturation is >= 0.15 and <= 0.55,
            "bright" or "vivid" => saturation >= 0.55 &&
                                   lightness is >= 0.35 and <= 0.75,
            "muted" or "dull" => saturation <= 0.30 &&
                                 lightness is > 0.20 and < 0.85,
            "neutral" => saturation <= 0.18,
            "warm" => saturation >= 0.08 &&
                      (HueInRange(hue, 0, 70) ||
                       HueInRange(hue, 330, 360)),
            "cool" => saturation >= 0.08 && HueInRange(hue, 80, 290),
            "black" => lightness <= 0.18,
            "white" => lightness >= 0.88 && saturation <= 0.22,
            "grey" or "gray" => saturation <= 0.12 &&
                                lightness is > 0.18 and < 0.88,
            "red" => HueNear(hue, 0, 22) || HueNear(hue, 360, 22),
            "orange" => HueInRange(hue, 20, 45) && saturation >= 0.25,
            "yellow" => HueInRange(hue, 45, 70) && saturation >= 0.20,
            "lime" => HueInRange(hue, 70, 100) && saturation >= 0.25,
            "green" => HueInRange(hue, 80, 165) && saturation >= 0.20,
            "cyan" or "turquoise" or "teal" or "aqua" =>
                HueInRange(hue, 165, 205) && saturation >= 0.20,
            "blue" => HueInRange(hue, 205, 260) && saturation >= 0.18,
            "purple" or "violet" =>
                HueInRange(hue, 260, 300) && saturation >= 0.20,
            "pink" or "magenta" =>
                HueInRange(hue, 300, 345) &&
                saturation >= 0.18 &&
                lightness >= 0.45,
            "brown" => HueInRange(hue, 15, 50) &&
                       saturation >= 0.20 &&
                       lightness <= 0.48,
            "navy" => HueInRange(hue, 205, 250) && lightness <= 0.32,
            "maroon" or "burgundy" =>
                (HueNear(hue, 0, 20) || HueNear(hue, 350, 20)) &&
                lightness <= 0.38,
            "cream" => HueInRange(hue, 35, 65) &&
                       lightness >= 0.78 &&
                       saturation <= 0.45,
            "beige" or "tan" => HueInRange(hue, 25, 60) &&
                                lightness >= 0.50 &&
                                saturation <= 0.45,
            "olive" => HueInRange(hue, 55, 90) &&
                       lightness <= 0.45 &&
                       saturation >= 0.18,
            "gold" => HueInRange(hue, 35, 55) &&
                      saturation >= 0.35 &&
                      lightness is >= 0.25 and <= 0.78,
            "silver" => saturation <= 0.14 &&
                        lightness is >= 0.55 and <= 0.85,
            "charcoal" => saturation <= 0.16 &&
                          lightness is >= 0.18 and <= 0.35,
            "lavender" => HueInRange(hue, 260, 310) &&
                          lightness >= 0.68 &&
                          saturation is >= 0.12 and <= 0.55,
            "peach" => HueInRange(hue, 15, 40) &&
                       lightness >= 0.65 &&
                       saturation >= 0.25,
            "earthy" => (HueInRange(hue, 15, 95) &&
                         lightness <= 0.58 &&
                         saturation is >= 0.12 and <= 0.58),
            _ => false,
        };
    }

    private static bool TextLooksLikeSearch(
        MasterbatchReference colour,
        string token)
    {
        var fields = new[]
        {
            colour.ColourName,
            colour.ColourCode,
            colour.Supplier,
            colour.ColourType,
            colour.RalEquivalent ?? string.Empty,
        };

        if (fields.Any(field => ContainsText(field, token)))
        {
            return true;
        }

        if (token.Length < 4)
        {
            return false;
        }

        return fields
            .SelectMany(
                field => field.Split(
                    [' ', '/', '-', '_', '(', ')'],
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries))
            .Any(word =>
                Math.Abs(word.Length - token.Length) <= 1 &&
                EditDistanceAtMostOne(
                    word.ToLowerInvariant(),
                    token.ToLowerInvariant()));
    }

    private static bool ContainsText(string value, string search) =>
        value.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> Tokenize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(
                    [' ', ',', ';', '+'],
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(NormalizeToken)
                .Where(token => token.Length > 0)
                .ToArray();

    private static string NormalizeGroup(string? value)
    {
        var normalized = NormalizeToken(value);
        if (normalized is "" or "all" or "all colours" or "all colors")
        {
            return string.Empty;
        }

        return normalized switch
        {
            "bright / vivid" => "vivid",
            "muted / dull" => "muted",
            "cyan / turquoise / teal" => "cyan",
            "purple / violet" => "purple",
            "pink / magenta" => "pink",
            "maroon / burgundy" => "maroon",
            "beige / tan" => "beige",
            _ => normalized,
        };
    }

    private static string NormalizeToken(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool TryParseHex(
        string? hexCode,
        out int red,
        out int green,
        out int blue)
    {
        red = 0;
        green = 0;
        blue = 0;
        var value = hexCode?.Trim().TrimStart('#');
        if (value?.Length != 6)
        {
            return false;
        }

        return int.TryParse(
                   value.AsSpan(0, 2),
                   NumberStyles.HexNumber,
                   CultureInfo.InvariantCulture,
                   out red) &&
               int.TryParse(
                   value.AsSpan(2, 2),
                   NumberStyles.HexNumber,
                   CultureInfo.InvariantCulture,
                   out green) &&
               int.TryParse(
                   value.AsSpan(4, 2),
                   NumberStyles.HexNumber,
                   CultureInfo.InvariantCulture,
                   out blue);
    }

    private static (double Hue, double Saturation, double Lightness) ToHsl(
        int red,
        int green,
        int blue)
    {
        var r = red / 255d;
        var g = green / 255d;
        var b = blue / 255d;
        var maximum = Math.Max(r, Math.Max(g, b));
        var minimum = Math.Min(r, Math.Min(g, b));
        var delta = maximum - minimum;
        var lightness = (maximum + minimum) / 2d;

        if (delta <= double.Epsilon)
        {
            return (0, 0, lightness);
        }

        var saturation = delta / (1d - Math.Abs((2d * lightness) - 1d));
        var hue = maximum == r
            ? 60d * (((g - b) / delta) % 6d)
            : maximum == g
                ? 60d * (((b - r) / delta) + 2d)
                : 60d * (((r - g) / delta) + 4d);
        if (hue < 0)
        {
            hue += 360d;
        }

        return (hue, saturation, lightness);
    }

    private static bool HueInRange(
        double hue,
        double minimum,
        double maximum) =>
        hue >= minimum && hue <= maximum;

    private static bool HueNear(
        double hue,
        double target,
        double tolerance)
    {
        var distance = Math.Abs(hue - target);
        return Math.Min(distance, 360d - distance) <= tolerance;
    }

    private static string MainFamily(
        double hue,
        double saturation,
        double lightness)
    {
        if (lightness <= 0.18)
        {
            return "Black";
        }

        if (lightness >= 0.88 && saturation <= 0.22)
        {
            return "White";
        }

        if (saturation <= 0.12)
        {
            return "Grey";
        }

        return hue switch
        {
            < 20 or >= 345 => "Red",
            < 45 => lightness <= 0.48 ? "Brown" : "Orange",
            < 70 => "Yellow",
            < 100 => "Lime",
            < 165 => "Green",
            < 205 => "Teal",
            < 260 => "Blue",
            < 300 => "Purple",
            _ => "Pink",
        };
    }

    private static bool EditDistanceAtMostOne(string left, string right)
    {
        if (left == right)
        {
            return true;
        }

        if (Math.Abs(left.Length - right.Length) > 1)
        {
            return false;
        }

        if (left.Length == right.Length)
        {
            var differences = 0;
            for (var index = 0; index < left.Length; index++)
            {
                if (left[index] == right[index])
                {
                    continue;
                }

                differences++;
                if (differences > 1)
                {
                    return false;
                }
            }

            return true;
        }

        var shorter = left.Length < right.Length ? left : right;
        var longer = left.Length < right.Length ? right : left;
        var shortIndex = 0;
        var longIndex = 0;
        var skipped = false;
        while (shortIndex < shorter.Length && longIndex < longer.Length)
        {
            if (shorter[shortIndex] == longer[longIndex])
            {
                shortIndex++;
                longIndex++;
                continue;
            }

            if (skipped)
            {
                return false;
            }

            skipped = true;
            longIndex++;
        }

        return true;
    }
}
