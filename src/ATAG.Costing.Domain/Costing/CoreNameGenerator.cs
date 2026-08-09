using System.Globalization;
using System.Text.RegularExpressions;

namespace ATAG.Costing.Domain.Costing;

public sealed record CoreNameInputs(
    string ConductorDescription,
    string CompoundMaterialType,
    bool IsCustomerSpecial,
    string? CustomerShortName);

public sealed record CoreNameResult(
    string GeneratedName,
    string StrandCountCode,
    string StrandDiameterCode,
    string WireCode,
    string MaterialTypeCode,
    string? CustomerSuffix);

public static partial class CoreNameGenerator
{
    public const string RuleVersion = "single-core-name/v1";

    private static readonly IReadOnlyDictionary<string, string> MaterialTypeCodes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PVC1"] = "T1",
            ["PVC2"] = "T2",
            ["PVC3"] = "T3",
            ["PE"] = "PE",
            ["PP"] = "PP",
            ["PS"] = "PS",
            ["TPE"] = "TPE",
            ["PU"] = "PU",
            ["PUR"] = "PU",
            ["PVDF"] = "PVDF",
            ["RUBBER"] = "RUBBER",
            ["CAT"] = "CAT",
            ["NEK606"] = "NEK606",
        };

    public static CoreNameResult Generate(CoreNameInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        if (string.IsNullOrWhiteSpace(inputs.ConductorDescription))
        {
            throw new ArgumentException(
                "A conductor description is required to generate the core name.",
                nameof(inputs));
        }

        if (string.IsNullOrWhiteSpace(inputs.CompoundMaterialType))
        {
            throw new ArgumentException(
                "A compound material type is required to generate the core name.",
                nameof(inputs));
        }

        var match = ConductorConstructionPattern().Match(
            inputs.ConductorDescription);
        if (!match.Success)
        {
            throw new ArgumentException(
                "The conductor description must start with a strand count and strand diameter, for example 7/0.196.",
                nameof(inputs));
        }

        var strandCount = int.Parse(
            match.Groups["count"].Value,
            CultureInfo.InvariantCulture);
        var strandDiameter = decimal.Parse(
            match.Groups["diameter"].Value,
            CultureInfo.InvariantCulture);
        var strandCountCode = strandCount.ToString("00", CultureInfo.InvariantCulture);
        var strandDiameterCode = decimal.Round(
                strandDiameter * 100m,
                0,
                MidpointRounding.AwayFromZero)
            .ToString("00", CultureInfo.InvariantCulture);
        var wireCode = WireCode(inputs.ConductorDescription);
        var materialTypeCode = MaterialTypeCodes.TryGetValue(
            inputs.CompoundMaterialType.Trim(),
            out var mapped)
            ? mapped
            : inputs.CompoundMaterialType.Trim().ToUpperInvariant();
        var customerSuffix =
            inputs.IsCustomerSpecial &&
            !string.IsNullOrWhiteSpace(inputs.CustomerShortName)
                ? $" ({inputs.CustomerShortName.Trim()})"
                : null;
        var generated =
            $"COR {strandCountCode}{strandDiameterCode} {wireCode} {materialTypeCode}" +
            customerSuffix;

        return new CoreNameResult(
            generated,
            strandCountCode,
            strandDiameterCode,
            wireCode,
            materialTypeCode,
            customerSuffix);
    }

    private static string WireCode(string conductorDescription)
    {
        var upper = conductorDescription.ToUpperInvariant();
        if (upper.Contains("TITANIUM", StringComparison.Ordinal) ||
            upper.Contains(" TI ", StringComparison.Ordinal))
        {
            return "Ti";
        }

        if (upper.Contains("TCW", StringComparison.Ordinal))
        {
            return "T";
        }

        if (upper.Contains("PCW", StringComparison.Ordinal))
        {
            return "P";
        }

        return "?";
    }

    [GeneratedRegex(
        @"^\s*(?<count>\d+)\s*/\s*(?<diameter>\d+(?:\.\d+)?)",
        RegexOptions.CultureInvariant)]
    private static partial Regex ConductorConstructionPattern();
}
