using System.Globalization;

namespace ATAG.Costing.Application.Production;

public sealed record ProductionSpeedEstimateRequest(
    string ProcessName,
    decimal CoreOutsideDiameterMillimetres,
    decimal FinishedOutsideDiameterMillimetres,
    decimal QuoteLengthMetres,
    decimal? CapstanSetting = null,
    decimal? ExtruderSetting = null);

public sealed record ProductionSpeedEstimateEvidence(
    string CableReference,
    decimal EffectiveLineSpeedMetresPerHour,
    decimal SimilarityScore,
    string Explanation);

public sealed record ProductionSpeedEstimate(
    decimal RecommendedLineSpeedMetresPerHour,
    decimal RunningTimeHours,
    string Source,
    string Confidence,
    string Explanation,
    IReadOnlyList<ProductionSpeedEstimateEvidence> Evidence);

public static class ProductionSpeedEstimator
{
    private const decimal MaximumObservationScore = 12m;

    public static ProductionSpeedEstimate Estimate(
        ProductionLineDefinition line,
        ProductionSpeedEstimateRequest request)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(request);

        if (request.CoreOutsideDiameterMillimetres <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Core outside diameter must be greater than zero.");
        }

        if (request.FinishedOutsideDiameterMillimetres <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Finished outside diameter must be greater than zero.");
        }

        if (request.QuoteLengthMetres <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Quote length must be greater than zero.");
        }

        var candidates = line.Observations
            .Where(observation =>
                string.Equals(
                    observation.ProcessName.Trim(),
                    request.ProcessName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            .Select(observation => CreateCandidate(observation, request))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .Where(candidate => candidate.Score <= MaximumObservationScore)
            .OrderBy(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Observation.CableReference, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        if (candidates.Length > 0)
        {
            var totalWeight = candidates.Sum(candidate => candidate.Weight);
            var speed = candidates.Sum(
                candidate => candidate.Speed * candidate.Weight) / totalWeight;
            var confidence = candidates.Length >= 2 && candidates.All(candidate => candidate.Score <= 2m)
                ? "High"
                : candidates[0].Score <= 4m
                    ? "Medium"
                    : "Low";
            var evidence = candidates
                .Select(candidate => new ProductionSpeedEstimateEvidence(
                    candidate.Observation.CableReference,
                    candidate.Speed,
                    candidate.Score,
                    CandidateExplanation(candidate)))
                .ToArray();

            return new ProductionSpeedEstimate(
                speed,
                request.QuoteLengthMetres / speed,
                "Known cable runs",
                confidence,
                $"Weighted estimate from {candidates.Length} measured run" +
                (candidates.Length == 1 ? "." : "s."),
                evidence);
        }

        var bands = line.SpeedBands
            .Where(IsUsableBand)
            .OrderBy(band => band.MaximumFinishedOutsideDiameterMillimetres)
            .ToArray();
        if (bands.Length == 0)
        {
            throw new InvalidOperationException(
                "The selected line has no close measured run or usable OD speed bands. Add an OD band or a measured cable run first.");
        }

        var selectedBand = bands.FirstOrDefault(
            band => request.FinishedOutsideDiameterMillimetres <=
                    band.MaximumFinishedOutsideDiameterMillimetres);
        var selectedSpeed = selectedBand?.LineSpeedMetresPerHour ??
            line.AboveMaximumLineSpeedMetresPerHour;
        if (selectedSpeed <= 0m)
        {
            throw new InvalidOperationException(
                "The selected line has no measured run or usable OD speed rule for this cable.");
        }

        var bandExplanation = selectedBand is null
            ? $"Finished OD {Number(request.FinishedOutsideDiameterMillimetres)} mm is above " +
              $"the largest configured band; use {Number(selectedSpeed)} m/h."
            : $"Finished OD {Number(request.FinishedOutsideDiameterMillimetres)} mm is within " +
              $"the ≤ {Number(selectedBand.MaximumFinishedOutsideDiameterMillimetres)} mm band; " +
              $"use {Number(selectedSpeed)} m/h.";

        return new ProductionSpeedEstimate(
            selectedSpeed,
            request.QuoteLengthMetres / selectedSpeed,
            "OD speed table",
            "Rule based",
            bandExplanation,
            []);
    }

    public static decimal? EffectiveObservationSpeed(
        ProductionRunObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (observation.MeasuredLineSpeedMetresPerHour is > 0m)
        {
            return observation.MeasuredLineSpeedMetresPerHour.Value;
        }

        if (observation.ProducedLengthMetres is > 0m &&
            observation.RunningTimeMinutes is > 0m)
        {
            return observation.ProducedLengthMetres.Value /
                   (observation.RunningTimeMinutes.Value / 60m);
        }

        return null;
    }

    private static ObservationCandidate? CreateCandidate(
        ProductionRunObservation observation,
        ProductionSpeedEstimateRequest request)
    {
        var speed = EffectiveObservationSpeed(observation);
        if (speed is null ||
            observation.CoreOutsideDiameterMillimetres <= 0m ||
            observation.FinishedOutsideDiameterMillimetres <= 0m)
        {
            return null;
        }

        var finishedScale = Math.Max(
            observation.FinishedOutsideDiameterToleranceMillimetres,
            0.10m);
        var coreScale = Math.Max(
            observation.CoreOutsideDiameterToleranceMillimetres,
            0.05m);
        var finishedScore = Math.Abs(
            request.FinishedOutsideDiameterMillimetres -
            observation.FinishedOutsideDiameterMillimetres) / finishedScale * 4m;
        var coreScore = Math.Abs(
            request.CoreOutsideDiameterMillimetres -
            observation.CoreOutsideDiameterMillimetres) / coreScale * 2m;
        var capstanScore = SettingScore(
            request.CapstanSetting,
            observation.CapstanSetting);
        var extruderScore = SettingScore(
            request.ExtruderSetting,
            observation.ExtruderSetting);
        var score = finishedScore + coreScore + capstanScore + extruderScore;
        return new ObservationCandidate(
            observation,
            speed.Value,
            score,
            1m / (1m + score));
    }

    private static decimal SettingScore(decimal? requested, decimal? observed)
    {
        if (requested is not > 0m || observed is not > 0m)
        {
            return 0m;
        }

        return Math.Abs(requested.Value - observed.Value) /
               Math.Max(Math.Abs(observed.Value) * 0.10m, 0.10m);
    }

    private static bool IsUsableBand(ProductionSpeedBandDefinition band) =>
        band.MaximumFinishedOutsideDiameterMillimetres > 0m &&
        band.LineSpeedMetresPerHour > 0m;

    private static string CandidateExplanation(ObservationCandidate candidate)
    {
        var speedSource = candidate.Observation.MeasuredLineSpeedMetresPerHour is > 0m
            ? "measured speed"
            : "speed derived from produced length and running time";
        return $"{speedSource}; similarity score {candidate.Score:F2}.";
    }

    private static string Number(decimal value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed record ObservationCandidate(
        ProductionRunObservation Observation,
        decimal Speed,
        decimal Score,
        decimal Weight);
}
