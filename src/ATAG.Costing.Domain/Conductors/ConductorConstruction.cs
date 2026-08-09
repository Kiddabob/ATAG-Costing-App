using System.Globalization;
using System.Text.RegularExpressions;

namespace ATAG.Costing.Domain.Conductors;

public enum ConductorClass
{
    Unclassified = 0,
    Class1Solid = 1,
    Class2Stranded = 2,
    Class5Flexible = 5,
    Class6ExtraFlexible = 6,
}

public sealed record ConductorConstructionResult(
    string RawConstruction,
    string NormalizedConstruction,
    string NormalizedDescription,
    bool IsRopeLay,
    bool WasRopeLayInferred,
    bool WasStrandDiameterInferred,
    IReadOnlyList<int> PackingLevels,
    int GroupCount,
    int StrandsPerGroup,
    int TotalStrandCount,
    decimal StrandDiameterMillimetres,
    decimal CalculatedMetalAreaSquareMillimetres,
    decimal NominalAreaSquareMillimetres,
    decimal AreaDifferencePercent,
    string NearestAwg,
    ConductorClass ConductorClass,
    string ConductorClassDisplay,
    string ClassReason,
    bool RequiresAreaReview,
    string AreaVerificationMessage);

/// <summary>
/// Parses conductor descriptions without replacing supplier data. The exact
/// strand diameter drives area and AWG calculations; a two-decimal construction
/// is presentation-only so 7/0.196 can be shown as 7/0.20 without losing
/// calculation precision.
/// </summary>
public static partial class ConductorConstructionCalculator
{
    public const string RuleVersion = "conductor-construction/v2";

    private static readonly IReadOnlyDictionary<decimal, FlexibleWireLimits>
        FlexibleWireDiameterLimits =
            new Dictionary<decimal, FlexibleWireLimits>
            {
                [0.50m] = new(0.21m, 0.16m),
                [0.75m] = new(0.21m, 0.16m),
                [1.00m] = new(0.21m, 0.16m),
                [1.50m] = new(0.26m, 0.16m),
                [2.50m] = new(0.26m, 0.16m),
                [4.00m] = new(0.31m, 0.16m),
                [6.00m] = new(0.31m, 0.21m),
                [10.00m] = new(0.41m, 0.21m),
                [16.00m] = new(0.41m, 0.21m),
                [25.00m] = new(0.41m, 0.21m),
                [35.00m] = new(0.41m, 0.21m),
                [50.00m] = new(0.41m, 0.31m),
                [70.00m] = new(0.51m, 0.31m),
                [95.00m] = new(0.51m, 0.31m),
                [120.00m] = new(0.51m, 0.31m),
                [150.00m] = new(0.51m, 0.31m),
                [185.00m] = new(0.51m, 0.41m),
                [240.00m] = new(0.51m, 0.41m),
                [300.00m] = new(0.51m, 0.41m),
                [400.00m] = new(0.51m, null),
                [500.00m] = new(0.61m, null),
                [630.00m] = new(0.61m, null),
            };

    public static ConductorConstructionResult? TryCalculate(
        string description,
        decimal nominalAreaSquareMillimetres)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var ropeMatch = RopeConstructionPattern().Match(description);
        var simpleMatch = SimpleConstructionPattern().Match(description);
        var countTimesDiameterMatch =
            CountTimesDiameterPattern().Match(description);
        var multiLevelWithDiameterMatch =
            MultiLevelWithDiameterPattern().Match(description);
        var multiLevelMatch = MultiLevelPattern().Match(description);

        Match match;
        IReadOnlyList<int> packingLevels;
        decimal strandDiameter;
        var wasRopeLayInferred = false;
        var wasStrandDiameterInferred = false;
        if (ropeMatch.Success)
        {
            match = ropeMatch;
            packingLevels =
            [
                ParseInteger(match.Groups["groups"].Value),
                ParseInteger(match.Groups["strands"].Value),
            ];
            strandDiameter = ParseDiameter(
                match.Groups["diameter"].Value);
        }
        else if (simpleMatch.Success)
        {
            match = simpleMatch;
            var parsedStrandCount =
                ParseInteger(match.Groups["strands"].Value);
            wasRopeLayInferred = parsedStrandCount == 133;
            packingLevels = wasRopeLayInferred
                ? [7, 19]
                : [parsedStrandCount];
            strandDiameter = ParseDiameter(
                match.Groups["diameter"].Value);
        }
        else if (countTimesDiameterMatch.Success)
        {
            match = countTimesDiameterMatch;
            packingLevels =
            [
                ParseInteger(match.Groups["strands"].Value),
            ];
            strandDiameter = ParseDiameter(
                match.Groups["diameter"].Value);
        }
        else if (multiLevelWithDiameterMatch.Success)
        {
            match = multiLevelWithDiameterMatch;
            packingLevels = ParsePackingLevels(
                match.Groups["factors"].Value);
            strandDiameter = ParseDiameter(
                match.Groups["diameter"].Value);
        }
        else if (multiLevelMatch.Success &&
                 nominalAreaSquareMillimetres > 0m)
        {
            match = multiLevelMatch;
            packingLevels = ParsePackingLevels(
                match.Groups["factors"].Value);
            var inferredStrandCount = MultiplyPackingLevels(packingLevels);
            strandDiameter = (decimal)Math.Sqrt(
                (double)(
                    4m * nominalAreaSquareMillimetres /
                    (DecimalPi * inferredStrandCount)));
            wasStrandDiameterInferred = true;
        }
        else
        {
            return null;
        }

        var isRopeLay = packingLevels.Count > 1;
        var groupCount = isRopeLay ? packingLevels[0] : 1;
        var strandsPerGroup = isRopeLay
            ? MultiplyPackingLevels(packingLevels.Skip(1))
            : packingLevels[0];
        var totalStrands = MultiplyPackingLevels(packingLevels);

        if (groupCount <= 0 || strandsPerGroup <= 0 || strandDiameter <= 0)
        {
            return null;
        }

        var rawConstruction = match.Value.Trim();
        var normalizedDiameter = decimal.Round(
            strandDiameter,
            2,
            MidpointRounding.AwayFromZero);
        var normalizedConstruction = isRopeLay
            ? $"{string.Join("x", packingLevels)}/{normalizedDiameter:0.00}" +
              (wasRopeLayInferred ? " (133 total)" : "") +
              (wasStrandDiameterInferred
                  ? " (strand diameter inferred)"
                  : "")
            : $"{strandsPerGroup}/{normalizedDiameter:0.00}";
        var normalizedDescription =
            normalizedConstruction + description[match.Length..];
        var calculatedArea =
            totalStrands *
            DecimalPi *
            strandDiameter *
            strandDiameter /
            4m;
        var effectiveNominalArea = nominalAreaSquareMillimetres > 0
            ? nominalAreaSquareMillimetres
            : calculatedArea;
        var areaDifferencePercent = nominalAreaSquareMillimetres > 0
            ? decimal.Abs(calculatedArea - nominalAreaSquareMillimetres) /
              nominalAreaSquareMillimetres *
              100m
            : 0m;
        var requiresReview =
            nominalAreaSquareMillimetres > 0 &&
            areaDifferencePercent > 10m;
        var (conductorClass, classDisplay, classReason) = InferClass(
            totalStrands,
            strandDiameter,
            effectiveNominalArea);
        var verificationMessage = nominalAreaSquareMillimetres <= 0
            ? "No nominal area is stored; the strand-calculated metal area is shown."
            : requiresReview
                ? $"Review required: nominal {nominalAreaSquareMillimetres:0.###} mm² " +
                  $"differs from calculated metal area {calculatedArea:0.###} mm² " +
                  $"by {areaDifferencePercent:0.0}%."
                : $"Nominal {nominalAreaSquareMillimetres:0.###} mm² and calculated " +
                  $"{calculatedArea:0.###} mm² are within {areaDifferencePercent:0.0}%.";
        if (wasRopeLayInferred)
        {
            verificationMessage =
                "The compact 133-strand notation is interpreted as a 7 × 19 rope lay-up. " +
                verificationMessage;
        }

        if (wasStrandDiameterInferred)
        {
            verificationMessage =
                $"The source lists the {string.Join(" x ", packingLevels)} packing hierarchy " +
                $"without a strand diameter; {strandDiameter:0.####} mm is inferred from " +
                $"the stored {nominalAreaSquareMillimetres:0.###} square millimetre nominal area " +
                "for preview and area checking. " +
                verificationMessage;
        }

        return new ConductorConstructionResult(
            rawConstruction,
            normalizedConstruction,
            normalizedDescription,
            isRopeLay,
            wasRopeLayInferred,
            wasStrandDiameterInferred,
            packingLevels,
            groupCount,
            strandsPerGroup,
            totalStrands,
            strandDiameter,
            calculatedArea,
            nominalAreaSquareMillimetres,
            areaDifferencePercent,
            FindNearestAwg(calculatedArea),
            conductorClass,
            classDisplay,
            classReason,
            requiresReview,
            verificationMessage);
    }

    private static (ConductorClass Class, string Display, string Reason) InferClass(
        int totalStrands,
        decimal strandDiameter,
        decimal nominalArea)
    {
        if (totalStrands == 1)
        {
            return (
                ConductorClass.Class1Solid,
                "Class 1 · solid",
                "A single-wire construction matches the Class 1 geometry.");
        }

        var standardArea = FlexibleWireDiameterLimits.Keys
            .Select(area => new
            {
                Area = area,
                Difference = decimal.Abs(area - nominalArea) / area,
            })
            .Where(candidate => candidate.Difference <= 0.15m)
            .OrderBy(candidate => candidate.Difference)
            .Select(candidate => (decimal?)candidate.Area)
            .FirstOrDefault();

        if (standardArea is null)
        {
            if (totalStrands <= 7)
            {
                return (
                    ConductorClass.Class2Stranded,
                    "Class 2 · stranded geometry",
                    "The construction uses seven or fewer strands, so it is grouped as non-flexible Class 2 geometry. Formal classification still requires supplier resistance evidence.");
            }

            return (
                ConductorClass.Unclassified,
                "Class not established",
                "The nominal size is outside the implemented IEC 60228 flexible-size table; resistance evidence is required.");
        }

        var limits = FlexibleWireDiameterLimits[standardArea.Value];
        if (limits.Class6MaximumDiameter is not null &&
            strandDiameter <= limits.Class6MaximumDiameter.Value)
        {
            return (
                ConductorClass.Class6ExtraFlexible,
                "Class 6 · extra flexible geometry",
                $"The {strandDiameter:0.###} mm strands are within the " +
                $"{limits.Class6MaximumDiameter.Value:0.###} mm Class 6 limit " +
                $"for nominal {standardArea.Value:0.###} mm². Resistance still requires supplier verification.");
        }

        if (strandDiameter <= limits.Class5MaximumDiameter)
        {
            return (
                ConductorClass.Class5Flexible,
                "Class 5 · flexible geometry",
                $"The {strandDiameter:0.###} mm strands are within the " +
                $"{limits.Class5MaximumDiameter:0.###} mm Class 5 limit " +
                $"for nominal {standardArea.Value:0.###} mm². Resistance still requires supplier verification.");
        }

        return (
            ConductorClass.Class2Stranded,
            "Class 2 / non-flexible geometry",
            $"The strand diameter exceeds the Class 5 flexible limit for nominal " +
            $"{standardArea.Value:0.###} mm². Supplier resistance data is required for formal classification.");
    }

    private static string FindNearestAwg(decimal areaSquareMillimetres)
    {
        var target = (double)areaSquareMillimetres;
        var nearest = Enumerable.Range(-3, 44)
            .Select(gauge => new
            {
                Gauge = gauge,
                Area = AwgAreaSquareMillimetres(gauge),
            })
            .OrderBy(candidate =>
                Math.Abs(Math.Log(target / candidate.Area)))
            .First()
            .Gauge;

        return nearest switch
        {
            0 => "1/0",
            -1 => "2/0",
            -2 => "3/0",
            -3 => "4/0",
            _ => nearest.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static double AwgAreaSquareMillimetres(int gauge)
    {
        var diameterMillimetres =
            0.127 * Math.Pow(92, (36d - gauge) / 39d);
        return Math.PI * diameterMillimetres * diameterMillimetres / 4d;
    }

    private static int ParseInteger(string value) =>
        int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);

    private static decimal ParseDiameter(string value) =>
        decimal.Parse(
            value,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture);

    private static IReadOnlyList<int> ParsePackingLevels(string value) =>
        PackingIntegerPattern()
            .Matches(value)
            .Select(match => ParseInteger(match.Value))
            .ToArray();

    private static int MultiplyPackingLevels(IEnumerable<int> levels)
    {
        var total = 1;
        foreach (var level in levels)
        {
            total = checked(total * level);
        }

        return total;
    }

    private const decimal DecimalPi = 3.1415926535897932384626433833m;

    private sealed record FlexibleWireLimits(
        decimal Class5MaximumDiameter,
        decimal? Class6MaximumDiameter);

    [GeneratedRegex(
        @"^\s*(?<groups>\d+)\s*[xX×]\s*(?<strands>\d+)\s*/\s*(?<diameter>\d+(?:\.\d+)?)",
        RegexOptions.CultureInvariant)]
    private static partial Regex RopeConstructionPattern();

    [GeneratedRegex(
        @"^\s*(?<strands>\d+)\s*/\s*(?<diameter>\d+(?:\.\d+)?)",
        RegexOptions.CultureInvariant)]
    private static partial Regex SimpleConstructionPattern();

    [GeneratedRegex(
        @"^\s*(?<strands>\d+)\s*[xX\u00D7]\s*(?<diameter>\d*\.\d+)\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex CountTimesDiameterPattern();

    [GeneratedRegex(
        @"^\s*(?<factors>\d+(?:\s*[xX\u00D7]\s*\d+)+)\s+(?<diameter>\d*\.\d+)\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex MultiLevelWithDiameterPattern();

    [GeneratedRegex(
        @"^\s*(?<factors>\d+(?:\s*[xX\u00D7]\s*\d+)+)\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex MultiLevelPattern();

    [GeneratedRegex(@"\d+", RegexOptions.CultureInvariant)]
    private static partial Regex PackingIntegerPattern();
}
