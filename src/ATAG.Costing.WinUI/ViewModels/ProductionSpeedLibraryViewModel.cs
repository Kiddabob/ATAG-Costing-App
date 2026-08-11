using System.Collections.ObjectModel;
using System.Globalization;
using ATAG.Costing.Application.Production;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATAG.Costing.WinUI.ViewModels;

public sealed record ProductionSpeedBandRow(
    ProductionSpeedBandDefinition Definition)
{
    public string MaximumOutsideDiameterDisplay =>
        $"≤ {Definition.MaximumFinishedOutsideDiameterMillimetres:0.###} mm";

    public string LineSpeedDisplay =>
        $"{Definition.LineSpeedMetresPerHour:N0} m/h";
}

public sealed record ProductionRunObservationRow(
    ProductionRunObservation Observation)
{
    public string CableDisplay => string.IsNullOrWhiteSpace(Observation.CableReference)
        ? "Unnamed cable"
        : Observation.CableReference;

    public string DimensionsDisplay =>
        $"Core {Observation.CoreOutsideDiameterMillimetres:0.###} ±" +
        $"{Observation.CoreOutsideDiameterToleranceMillimetres:0.###} mm · " +
        $"Finished {Observation.FinishedOutsideDiameterMillimetres:0.###} ±" +
        $"{Observation.FinishedOutsideDiameterToleranceMillimetres:0.###} mm";

    public string SettingsDisplay
    {
        get
        {
            var capstan = Observation.CapstanSetting is > 0m
                ? Observation.CapstanSetting.Value.ToString("0.###", CultureInfo.InvariantCulture)
                : "not recorded";
            var extruder = Observation.ExtruderSetting is > 0m
                ? Observation.ExtruderSetting.Value.ToString("0.###", CultureInfo.InvariantCulture)
                : "not recorded";
            return $"Capstan {capstan} · Extruder {extruder}";
        }
    }

    public string SpeedDisplay
    {
        get
        {
            var speed = ProductionSpeedEstimator.EffectiveObservationSpeed(Observation);
            if (speed is null)
            {
                return "Calibration only · add measured speed or length/time";
            }

            return Observation.MeasuredLineSpeedMetresPerHour is > 0m
                ? $"Measured {speed.Value:N0} m/h"
                : $"Derived {speed.Value:N0} m/h from length ÷ time";
        }
    }
}

public partial class ProductionSpeedLibraryViewModel : ObservableObject
{
    private readonly IProductionSpeedLibraryStore _store;

    public ObservableCollection<ProductionLineDefinition> Lines { get; } = [];

    public ObservableCollection<ProductionSpeedBandRow> SpeedBands { get; } = [];

    public ObservableCollection<ProductionRunObservationRow> Observations { get; } = [];

    public bool IsEditingEnabled { get; }

    public ProductionSpeedEstimate? LatestEstimate { get; private set; }

    [ObservableProperty]
    public partial ProductionLineDefinition? SelectedLine { get; set; }

    [ObservableProperty]
    public partial string LibraryStatus { get; set; } =
        "Choose a production line or add one to begin.";

    [ObservableProperty]
    public partial double EstimateCoreOutsideDiameterMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double EstimateFinishedOutsideDiameterMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double EstimateQuoteLengthMetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double EstimateCapstanSetting { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double EstimateExtruderSetting { get; set; } = double.NaN;

    [ObservableProperty]
    public partial string EstimateProcessName { get; set; } = "";

    [ObservableProperty]
    public partial string EstimateHeadline { get; set; } =
        "No speed estimate calculated";

    [ObservableProperty]
    public partial string EstimateDetail { get; set; } =
        "Enter the cable dimensions and calculate an estimate.";

    [ObservableProperty]
    public partial string EstimateEvidenceDisplay { get; set; } =
        "The OD speed table will be used until sufficiently similar measured runs exist.";

    [ObservableProperty]
    public partial bool HasEstimate { get; set; }

    public ProductionSpeedLibraryViewModel(
        IProductionSpeedLibraryStore store,
        bool isEditingEnabled = true)
    {
        _store = store;
        IsEditingEnabled = isEditingEnabled;

        var state = _store.Load();
        foreach (var line in state.Lines)
        {
            Lines.Add(line);
        }

        SelectedLine = Lines.FirstOrDefault();
        LibraryStatus = isEditingEnabled
            ? "Production lines are retained privately for this Windows user."
            : "Production data is disabled in the interface-only public review.";
    }

    public bool TryAddGeneralStarterProfile(out string message)
    {
        if (!CanEdit(out message))
        {
            return false;
        }

        if (Lines.Any(line => string.Equals(
                line.Name,
                "General insulation starter profile",
                StringComparison.OrdinalIgnoreCase)))
        {
            message = "The general insulation starter profile is already in this library.";
            return false;
        }

        var line = ProductionSpeedLibraryDefaults.CreateGeneralInsulationStarterLine();
        Lines.Add(line);
        SelectedLine = line;
        Persist();
        message =
            "Added the general insulation bands. Rename the line and adjust its values to match the actual production line.";
        LibraryStatus = message;
        return true;
    }

    public void SetEstimateInputs(
        string processName,
        double coreOutsideDiameterMillimetres,
        double finishedOutsideDiameterMillimetres,
        double quoteLengthMetres,
        string source)
    {
        EstimateProcessName = processName;
        EstimateCoreOutsideDiameterMillimetres = PositiveOrBlank(
            coreOutsideDiameterMillimetres);
        EstimateFinishedOutsideDiameterMillimetres = PositiveOrBlank(
            finishedOutsideDiameterMillimetres);
        EstimateQuoteLengthMetres = PositiveOrBlank(quoteLengthMetres);
        LatestEstimate = null;
        HasEstimate = false;
        EstimateHeadline = "No speed estimate calculated";
        EstimateDetail =
            $"Copied the available dimensions from {source}. Complete any blank fields, then calculate.";
        EstimateEvidenceDisplay =
            "The selected production line's measured runs and OD bands will be evaluated.";
    }

    public bool TryAddLine(
        string name,
        decimal aboveMaximumSpeed,
        out string message)
    {
        if (!CanEdit(out message) ||
            !ValidateLine(name, aboveMaximumSpeed, out message))
        {
            return false;
        }

        var line = new ProductionLineDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            AboveMaximumLineSpeedMetresPerHour = aboveMaximumSpeed,
        };
        Lines.Add(line);
        SelectedLine = line;
        Persist();
        message = $"Added {line.Name}. Add OD bands or known runs next.";
        LibraryStatus = message;
        return true;
    }

    public bool TryUpdateSelectedLine(
        string name,
        decimal aboveMaximumSpeed,
        out string message)
    {
        if (!CanEdit(out message) || SelectedLine is null)
        {
            message = SelectedLine is null
                ? "Choose a production line first."
                : message;
            return false;
        }

        if (!ValidateLine(name, aboveMaximumSpeed, out message))
        {
            return false;
        }

        var updated = SelectedLine with
        {
            Name = name.Trim(),
            AboveMaximumLineSpeedMetresPerHour = aboveMaximumSpeed,
        };
        ReplaceSelectedLine(updated);
        message = $"Updated {updated.Name}.";
        LibraryStatus = message;
        return true;
    }

    public bool TryDeleteSelectedLine(out string message)
    {
        if (!CanEdit(out message) || SelectedLine is null)
        {
            message = SelectedLine is null
                ? "Choose a production line first."
                : message;
            return false;
        }

        var name = SelectedLine.Name;
        var index = Lines.IndexOf(SelectedLine);
        Lines.Remove(SelectedLine);
        SelectedLine = Lines.Count == 0
            ? null
            : Lines[Math.Clamp(index, 0, Lines.Count - 1)];
        Persist();
        message = $"Deleted {name}.";
        LibraryStatus = message;
        return true;
    }

    public bool TryAddBand(
        decimal maximumOutsideDiameter,
        decimal lineSpeed,
        out string message) =>
        TryUpsertBand(
            existingId: null,
            maximumOutsideDiameter,
            lineSpeed,
            out message);

    public bool TryUpdateBand(
        string id,
        decimal maximumOutsideDiameter,
        decimal lineSpeed,
        out string message) =>
        TryUpsertBand(id, maximumOutsideDiameter, lineSpeed, out message);

    public bool TryDeleteBand(string id, out string message)
    {
        if (!CanEdit(out message) || SelectedLine is null)
        {
            message = SelectedLine is null
                ? "Choose a production line first."
                : message;
            return false;
        }

        var bands = SelectedLine.SpeedBands
            .Where(band => band.Id != id)
            .ToArray();
        if (bands.Length == SelectedLine.SpeedBands.Count)
        {
            message = "The selected OD band no longer exists.";
            return false;
        }

        ReplaceSelectedLine(SelectedLine with { SpeedBands = bands });
        message = "Deleted the OD speed band.";
        LibraryStatus = message;
        return true;
    }

    public bool TryAddObservation(
        ProductionRunObservation observation,
        out string message) =>
        TryUpsertObservation(observation with
        {
            Id = Guid.NewGuid().ToString("N"),
        }, existingId: null, out message);

    public bool TryUpdateObservation(
        ProductionRunObservation observation,
        out string message) =>
        TryUpsertObservation(observation, observation.Id, out message);

    public bool TryDeleteObservation(string id, out string message)
    {
        if (!CanEdit(out message) || SelectedLine is null)
        {
            message = SelectedLine is null
                ? "Choose a production line first."
                : message;
            return false;
        }

        var observations = SelectedLine.Observations
            .Where(observation => observation.Id != id)
            .ToArray();
        if (observations.Length == SelectedLine.Observations.Count)
        {
            message = "The selected known run no longer exists.";
            return false;
        }

        ReplaceSelectedLine(SelectedLine with { Observations = observations });
        message = "Deleted the known cable run.";
        LibraryStatus = message;
        return true;
    }

    public bool TryCalculateEstimate(out string message)
    {
        LatestEstimate = null;
        HasEstimate = false;
        if (SelectedLine is null)
        {
            message = "Choose or add a production line first.";
            SetEstimateFailure(message);
            return false;
        }

        if (string.IsNullOrWhiteSpace(EstimateProcessName) ||
            !Positive(EstimateCoreOutsideDiameterMillimetres) ||
            !Positive(EstimateFinishedOutsideDiameterMillimetres) ||
            !Positive(EstimateQuoteLengthMetres))
        {
            message = "Enter a process, core OD, finished OD, and quote length greater than zero.";
            SetEstimateFailure(message);
            return false;
        }

        try
        {
            LatestEstimate = ProductionSpeedEstimator.Estimate(
                SelectedLine,
                new ProductionSpeedEstimateRequest(
                    EstimateProcessName,
                    ToDecimal(EstimateCoreOutsideDiameterMillimetres),
                    ToDecimal(EstimateFinishedOutsideDiameterMillimetres),
                    ToDecimal(EstimateQuoteLengthMetres),
                    OptionalDecimal(EstimateCapstanSetting),
                    OptionalDecimal(EstimateExtruderSetting)));
            HasEstimate = true;
            EstimateHeadline =
                $"{LatestEstimate.RecommendedLineSpeedMetresPerHour:N0} m/h · " +
                $"{Duration(LatestEstimate.RunningTimeHours)} running time";
            EstimateDetail =
                $"{SelectedLine.Name} · {LatestEstimate.Source} · " +
                $"{LatestEstimate.Confidence} confidence. {LatestEstimate.Explanation}";
            EstimateEvidenceDisplay = LatestEstimate.Evidence.Count == 0
                ? LatestEstimate.Explanation
                : string.Join(
                    Environment.NewLine,
                    LatestEstimate.Evidence.Select(evidence =>
                        $"• {evidence.CableReference}: " +
                        $"{evidence.EffectiveLineSpeedMetresPerHour:N0} m/h · " +
                        evidence.Explanation));
            message = "The estimate is ready and can be applied to a costing.";
            LibraryStatus = message;
            return true;
        }
        catch (ArgumentException exception)
        {
            message = exception.Message;
            SetEstimateFailure(message);
            return false;
        }
        catch (InvalidOperationException exception)
        {
            message = exception.Message;
            SetEstimateFailure(message);
            return false;
        }
    }

    partial void OnSelectedLineChanged(ProductionLineDefinition? value)
    {
        RefreshSelectedLineRows();
        LatestEstimate = null;
        HasEstimate = false;
        EstimateHeadline = "No speed estimate calculated";
        EstimateDetail = value is null
            ? "Add a production line to begin."
            : $"Selected {value.Name}. Enter cable dimensions and calculate an estimate.";
    }

    private bool TryUpsertBand(
        string? existingId,
        decimal maximumOutsideDiameter,
        decimal lineSpeed,
        out string message)
    {
        if (!CanEdit(out message) || SelectedLine is null)
        {
            message = SelectedLine is null
                ? "Choose a production line first."
                : message;
            return false;
        }

        if (maximumOutsideDiameter <= 0m || lineSpeed <= 0m)
        {
            message = "Maximum finished OD and line speed must both be greater than zero.";
            return false;
        }

        if (SelectedLine.SpeedBands.Any(band =>
                band.Id != existingId &&
                band.MaximumFinishedOutsideDiameterMillimetres == maximumOutsideDiameter))
        {
            message = "That maximum finished OD already has a speed band on this line.";
            return false;
        }

        var updatedBand = new ProductionSpeedBandDefinition
        {
            Id = existingId ?? Guid.NewGuid().ToString("N"),
            MaximumFinishedOutsideDiameterMillimetres = maximumOutsideDiameter,
            LineSpeedMetresPerHour = lineSpeed,
        };
        var bands = SelectedLine.SpeedBands
            .Where(band => band.Id != existingId)
            .Append(updatedBand)
            .OrderBy(band => band.MaximumFinishedOutsideDiameterMillimetres)
            .ToArray();
        ReplaceSelectedLine(SelectedLine with { SpeedBands = bands });
        message = existingId is null
            ? "Added the OD speed band."
            : "Updated the OD speed band.";
        LibraryStatus = message;
        return true;
    }

    private bool TryUpsertObservation(
        ProductionRunObservation observation,
        string? existingId,
        out string message)
    {
        if (!CanEdit(out message) || SelectedLine is null)
        {
            message = SelectedLine is null
                ? "Choose a production line first."
                : message;
            return false;
        }

        if (!ValidateObservation(observation, out message))
        {
            return false;
        }

        var observations = SelectedLine.Observations
            .Where(current => current.Id != existingId)
            .Append(observation with
            {
                CableReference = observation.CableReference.Trim(),
                ProcessName = observation.ProcessName.Trim(),
                Notes = observation.Notes?.Trim() ?? "",
            })
            .OrderBy(current => current.CableReference, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        ReplaceSelectedLine(SelectedLine with { Observations = observations });
        var usable = ProductionSpeedEstimator.EffectiveObservationSpeed(observation) is not null;
        message = usable
            ? existingId is null
                ? "Added the measured cable run."
                : "Updated the measured cable run."
            : "Saved the machine settings. Add measured speed or length/time before this row can influence estimates.";
        LibraryStatus = message;
        return true;
    }

    private static bool ValidateLine(
        string name,
        decimal aboveMaximumSpeed,
        out string message)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            message = "Enter a production-line name.";
            return false;
        }

        if (aboveMaximumSpeed <= 0m)
        {
            message = "Above-maximum line speed must be greater than zero.";
            return false;
        }

        message = "";
        return true;
    }

    private static bool ValidateObservation(
        ProductionRunObservation observation,
        out string message)
    {
        if (string.IsNullOrWhiteSpace(observation.CableReference) ||
            string.IsNullOrWhiteSpace(observation.ProcessName))
        {
            message = "Enter a cable reference and process name.";
            return false;
        }

        if (observation.CoreOutsideDiameterMillimetres <= 0m ||
            observation.FinishedOutsideDiameterMillimetres <= 0m ||
            observation.CoreOutsideDiameterToleranceMillimetres < 0m ||
            observation.FinishedOutsideDiameterToleranceMillimetres < 0m)
        {
            message = "Nominal ODs must be greater than zero and tolerances cannot be negative.";
            return false;
        }

        if (observation.CapstanSetting is <= 0m ||
            observation.ExtruderSetting is <= 0m ||
            observation.MeasuredLineSpeedMetresPerHour is <= 0m ||
            observation.ProducedLengthMetres is <= 0m ||
            observation.RunningTimeMinutes is <= 0m)
        {
            message = "Optional settings, measured speed, length, and running time must be greater than zero when supplied.";
            return false;
        }

        var hasLength = observation.ProducedLengthMetres is not null;
        var hasTime = observation.RunningTimeMinutes is not null;
        if (hasLength != hasTime)
        {
            message = "Enter both produced length and running minutes, or leave both blank.";
            return false;
        }

        message = "";
        return true;
    }

    private void ReplaceSelectedLine(ProductionLineDefinition updated)
    {
        if (SelectedLine is null)
        {
            return;
        }

        var index = Lines.IndexOf(SelectedLine);
        Lines[index] = updated;
        SelectedLine = updated;
        Persist();
    }

    private void RefreshSelectedLineRows()
    {
        SpeedBands.Clear();
        Observations.Clear();
        if (SelectedLine is null)
        {
            return;
        }

        foreach (var band in SelectedLine.SpeedBands
                     .OrderBy(band => band.MaximumFinishedOutsideDiameterMillimetres))
        {
            SpeedBands.Add(new ProductionSpeedBandRow(band));
        }

        foreach (var observation in SelectedLine.Observations
                     .OrderBy(observation => observation.CableReference, StringComparer.OrdinalIgnoreCase))
        {
            Observations.Add(new ProductionRunObservationRow(observation));
        }
    }

    private void Persist() =>
        _store.Save(new ProductionSpeedLibraryState
        {
            Lines = Lines.ToArray(),
        });

    private bool CanEdit(out string message)
    {
        if (IsEditingEnabled)
        {
            message = "";
            return true;
        }

        message = "Production data is disabled in the interface-only public review.";
        return false;
    }

    private void SetEstimateFailure(string message)
    {
        EstimateHeadline = "Estimate unavailable";
        EstimateDetail = message;
        EstimateEvidenceDisplay =
            "No quote value was changed. Add a valid OD rule or measured cable run and try again.";
        LibraryStatus = message;
    }

    private static bool Positive(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d;

    private static double PositiveOrBlank(double value) =>
        Positive(value) ? value : double.NaN;

    private static decimal ToDecimal(double value) =>
        Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    private static decimal? OptionalDecimal(double value) =>
        Positive(value) ? ToDecimal(value) : null;

    private static string Duration(decimal hours)
    {
        var span = TimeSpan.FromHours((double)hours);
        return span.TotalHours >= 1d
            ? $"{(int)span.TotalHours}h {span.Minutes}m"
            : $"{Math.Max(1, (int)Math.Ceiling(span.TotalMinutes))}m";
    }
}

internal sealed class InMemoryProductionSpeedLibraryStore(
    ProductionSpeedLibraryState initialState) : IProductionSpeedLibraryStore
{
    private ProductionSpeedLibraryState _state = initialState;

    public ProductionSpeedLibraryState Load() => _state;

    public void Save(ProductionSpeedLibraryState state) => _state = state;
}
