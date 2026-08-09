using System.Globalization;
using ATAG.Costing.Application.Visualisation;

namespace ATAG.Costing.Application.CentralData;

public sealed record CopperReferenceDerivationInputs(
    decimal? ManufacturingCostPerKilogram = null,
    decimal? CopperCostPerKilogram = null,
    decimal? CopperIncludingPremiumPerKilogram = null,
    decimal? NetWeightKilograms = null,
    decimal? LengthMetres = null,
    decimal? VolumeCubicMillimetresPerMetre = null);

/// <summary>
/// Completes only mathematically defensible gaps in the typed costing view.
/// The retained source row is never changed. Every calculated or estimated
/// value carries its formula and source values into the offline snapshot.
/// </summary>
public static class CopperReferenceDeriver
{
    public const string RuleVersion = "copper-reference-derivation/v1";

    private const decimal DecimalPi =
        3.1415926535897932384626433833m;

    public static CopperReference FillMissing(
        CopperReference source,
        CentralDataPreviewRow row,
        IReadOnlyDictionary<string, string> mappings) =>
        FillMissing(
            source,
            new CopperReferenceDerivationInputs(
                DecimalOrNull(row, mappings, "ManufacturingCostPerKilogram"),
                DecimalOrNull(row, mappings, "CopperCostPerKilogram"),
                DecimalOrNull(row, mappings, "CopperIncludingPremiumPerKilogram"),
                DecimalOrNull(row, mappings, "NetWeightKilograms"),
                DecimalOrNull(row, mappings, "LengthMetres"),
                DecimalOrNull(row, mappings, "VolumeCubicMillimetresPerMetre")));

    public static CopperReference FillMissing(
        CopperReference source,
        CopperReferenceDerivationInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(inputs);

        var price = source.PricePerKilogram;
        var yield = source.YieldMetresPerKilogram;
        var outsideDiameter = source.NominalOutsideDiameterMillimetres;
        var nominalArea = source.NominalAreaSquareMillimetres;
        var derivations = source.EffectiveDerivedValues.ToList();
        var changed = false;

        var copperComponent = EffectiveCopperComponent(inputs);
        if (price <= 0m &&
            inputs.ManufacturingCostPerKilogram is >= 0m and var manufacture &&
            copperComponent is >= 0m &&
            manufacture + copperComponent.Value > 0m)
        {
            price = manufacture + copperComponent.Value;
            derivations.Add(new CentralDataValueDerivation(
                "PricePerKilogram",
                "Price per kilogram",
                "manufacturing cost + copper cost",
                $"{manufacture:0.#####} + {copperComponent.Value:0.#####} = {price:0.#####} £/kg"));
            changed = true;
        }

        if (yield <= 0m &&
            inputs.LengthMetres is > 0m and var length &&
            inputs.NetWeightKilograms is > 0m and var netWeight)
        {
            yield = length / netWeight;
            derivations.Add(new CentralDataValueDerivation(
                "YieldMetresPerKilogram",
                "Yield",
                "reel length ÷ reel net weight",
                $"{length:0.######} m ÷ {netWeight:0.######} kg = {yield:0.######} m/kg"));
            changed = true;
        }

        var construction = source.Construction;
        if (nominalArea <= 0m && construction is not null)
        {
            nominalArea = construction.CalculatedMetalAreaSquareMillimetres;
            derivations.Add(new CentralDataValueDerivation(
                "NominalAreaSquareMillimetres",
                "Metal area",
                "strand count × π × strand diameter² ÷ 4",
                $"{construction.TotalStrandCount:N0} strands × {construction.StrandDiameterMillimetres:0.####} mm = {nominalArea:0.######} mm²"));
            changed = true;
        }

        if (outsideDiameter <= 0m &&
            inputs.VolumeCubicMillimetresPerMetre is > 0m and var volume)
        {
            var area = volume / 1000m;
            outsideDiameter = (decimal)Math.Sqrt(
                (double)(4m * area / DecimalPi));
            derivations.Add(new CentralDataValueDerivation(
                "NominalOutsideDiameterMillimetres",
                "Outside diameter",
                "√(4 × volume per metre ÷ (1,000 × π))",
                $"{volume:0.######} mm³/m = {outsideDiameter:0.######} mm"));
            changed = true;
        }

        if (outsideDiameter <= 0m)
        {
            var effectiveConstruction = source with
            {
                NominalAreaSquareMillimetres = nominalArea,
            };
            construction = effectiveConstruction.Construction;
            if (construction is not null)
            {
                outsideDiameter =
                    ConductorPreviewLayoutBuilder
                        .EstimatePackedEnvelopeDiameterMillimetres(construction);
                derivations.Add(new CentralDataValueDerivation(
                    "NominalOutsideDiameterMillimetres",
                    "Outside diameter",
                    "close-packed strand-envelope estimate",
                    $"{construction.NormalizedConstruction} using {ConductorPreviewLayoutBuilder.RuleVersion} = {outsideDiameter:0.######} mm",
                    IsEstimate: true));
                changed = true;
            }
        }

        if (!changed)
        {
            return source;
        }

        return source with
        {
            PricePerKilogram = price,
            YieldMetresPerKilogram = yield,
            NominalOutsideDiameterMillimetres = outsideDiameter,
            NominalAreaSquareMillimetres = nominalArea,
            DerivedValues = derivations,
        };
    }

    private static decimal? EffectiveCopperComponent(
        CopperReferenceDerivationInputs inputs) =>
        inputs.CopperCostPerKilogram ??
        inputs.CopperIncludingPremiumPerKilogram;

    private static decimal? DecimalOrNull(
        CentralDataPreviewRow row,
        IReadOnlyDictionary<string, string> mappings,
        string field)
    {
        if (!mappings.TryGetValue(field, out var column) ||
            string.IsNullOrWhiteSpace(column))
        {
            return null;
        }

        var cell = row.Cell(column);
        if (cell.HasError || string.IsNullOrWhiteSpace(cell.Value))
        {
            return null;
        }

        var cleaned = cell.Value
            .Replace("£", string.Empty, StringComparison.Ordinal)
            .Replace("€", string.Empty, StringComparison.Ordinal)
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace("/kg", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("m/kg", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mm³/m", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mm²", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mm2", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mm", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        if (!decimal.TryParse(
                cleaned,
                NumberStyles.Number | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture,
                out var parsed) &&
            !decimal.TryParse(
                cleaned,
                NumberStyles.Number | NumberStyles.AllowExponent,
                CultureInfo.CurrentCulture,
                out parsed))
        {
            return null;
        }

        return parsed >= 0m ? parsed : null;
    }
}
