using System.Collections.ObjectModel;
using ATAG.Costing.Application.Braiding;
using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Domain.Braiding;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATAG.Costing.WinUI.ViewModels;

public partial class BraidCoverageViewModel : ObservableObject
{
    public IReadOnlyList<BraidCoreLayout> CoreLayouts =>
        BraidReferenceTables.CoreLayouts;

    public ObservableCollection<CalculationStepRow> CalculationSteps { get; } = [];

    private IReadOnlyList<BraidWireReference> _allBraidWires = [];
    private bool _isSynchronizingWireChoice;

    [ObservableProperty]
    public partial IReadOnlyList<int> AvailableEndCounts { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<BraidWireReference> BraidWireOptions { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedEndCount { get; set; }

    [ObservableProperty]
    public partial BraidWireReference? SelectedBraidWire { get; set; }

    [ObservableProperty]
    public partial string BraidWireSourceStatus { get; set; } =
        "No suitable braid-wire rows are available in the retained Copper table.";

    public int SelectedEndsPerCarrier => SelectedBraidWire?.EndsPerCarrier ?? 0;

    public double SelectedEffectiveWireDiameterMillimetres =>
        (double)(SelectedBraidWire?.StrandDiameterMillimetres ?? 0m);

    public string SelectedWireDetail =>
        SelectedBraidWire?.Detail ?? "Choose a retained Copper record.";

    [ObservableProperty]
    public partial double TargetCoveragePercent { get; set; } = 80d;

    [ObservableProperty]
    public partial double CoreOutsideDiameterMillimetres { get; set; } = 10d;

    [ObservableProperty]
    public partial BraidCoreLayout? SelectedCoreLayout { get; set; }

    [ObservableProperty]
    public partial double CableLengthMetres { get; set; } = 1d;

    [ObservableProperty]
    public partial string CalculationStatus { get; set; } = "Ready";

    [ObservableProperty]
    public partial string CoreLayoutDetail { get; set; } = "";

    [ObservableProperty]
    public partial string MeanOutsideDiameterDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TargetFillDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierPitchDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierAnglesDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierCoverageDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierLengthDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierStrandsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierLongitudinalAngleDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierPerpendicularAngleDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierReferenceCoverageDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierBaseFillDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string SixteenCarrierTotalStrandsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierPitchDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierAnglesDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierCoverageDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierLengthDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierStrandsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierLongitudinalAngleDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierPerpendicularAngleDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierReferenceCoverageDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierBaseFillDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TwentyFourCarrierTotalStrandsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial bool IsSixteenCarrierRecommended { get; set; }

    [ObservableProperty]
    public partial string RecommendationDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial long PreviewRevision { get; set; }

    public double MeanOutsideDiameterMillimetres { get; private set; }

    public double SixteenCarrierPitchMillimetres { get; private set; }

    public double TwentyFourCarrierPitchMillimetres { get; private set; }

    public BraidCoverageViewModel(CentralDataState centralDataState)
    {
        SelectedCoreLayout = CoreLayouts[0];
        RefreshCentralData(centralDataState);
    }

    public void RefreshCentralData(CentralDataState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var retainedId = SelectedBraidWire?.Id;
        _allBraidWires = BraidWireCatalogue.Create(state.Snapshot.Copper);
        AvailableEndCounts = _allBraidWires
            .Select(item => item.EndsPerCarrier)
            .Distinct()
            .Order()
            .ToArray();

        var retained = _allBraidWires.FirstOrDefault(item => item.Id == retainedId);
        SelectedEndCount = retained?.EndsPerCarrier ??
            AvailableEndCounts.FirstOrDefault();
        FilterBraidWireOptions(retainedId);

        BraidWireSourceStatus = _allBraidWires.Count == 0
            ? "No retained Copper rows match the Braid boundary: a simple 1–10 end construction with strand diameter no greater than 0.25 mm."
            : $"{_allBraidWires.Count:N0} retained Copper choice{(_allBraidWires.Count == 1 ? string.Empty : "s")} · " +
              "supplier, finish, exact strand count and diameter remain linked to the selected record.";
    }

    partial void OnTargetCoveragePercentChanged(double value) => Recalculate();

    partial void OnCoreOutsideDiameterMillimetresChanged(double value) => Recalculate();

    partial void OnSelectedCoreLayoutChanged(BraidCoreLayout? value) => Recalculate();

    partial void OnSelectedEndCountChanged(int value)
    {
        if (!_isSynchronizingWireChoice)
        {
            FilterBraidWireOptions();
        }
    }

    partial void OnSelectedBraidWireChanged(BraidWireReference? value)
    {
        OnPropertyChanged(nameof(SelectedEndsPerCarrier));
        OnPropertyChanged(nameof(SelectedEffectiveWireDiameterMillimetres));
        OnPropertyChanged(nameof(SelectedWireDetail));
        Recalculate();
    }

    partial void OnCableLengthMetresChanged(double value) => Recalculate();

    private void Recalculate()
    {
        if (SelectedCoreLayout is null)
        {
            SetUnavailable("Choose the number of cores and lay-up.");
            return;
        }

        if (SelectedBraidWire is null)
        {
            SetUnavailable("Choose a braid-wire construction from the retained Copper table.");
            return;
        }

        try
        {
            var result = BraidCoverageCalculator.Calculate(
                new BraidCoverageInputs(
                    TargetCoveragePercent / 100d,
                    CoreOutsideDiameterMillimetres,
                    SelectedCoreLayout.CoreCount,
                    SelectedEndsPerCarrier,
                    SelectedEffectiveWireDiameterMillimetres,
                    CableLengthMetres));
            CoreLayoutDetail = string.IsNullOrWhiteSpace(result.CoreLayout.Layup)
                ? $"{result.CoreLayout.CoreCount} core arrangement · OD factor ×{result.CoreLayout.OutsideDiameterMultiplier:0.###}"
                : $"{result.CoreLayout.Layup} cores per layer · OD factor ×{result.CoreLayout.OutsideDiameterMultiplier:0.###}";
            MeanOutsideDiameterDisplay = $"{result.MeanOutsideDiameterMillimetres:0.000} mm";
            MeanOutsideDiameterMillimetres =
                result.MeanOutsideDiameterMillimetres;
            TargetFillDisplay = $"{result.TargetFillFraction:P2}";
            SetCarrierDisplays(result.SixteenCarrier, isSixteenCarrier: true);
            SetCarrierDisplays(result.TwentyFourCarrier, isSixteenCarrier: false);
            SetRecommendation(result);

            CalculationSteps.Clear();
            foreach (var step in result.Steps)
            {
                CalculationSteps.Add(new(
                    step.Label,
                    step.BusinessMeaning ?? "",
                    step.Expression,
                    step.SubstitutedExpression,
                    $"{step.DisplayValue} {step.Unit}".Trim(),
                    step.RoundingRule ?? "",
                    step.RuleVersion ?? BraidCoverageCalculator.RuleVersion,
                    step.Warning));
            }

            CalculationStatus =
                "Live · every result below is recalculated from the visible inputs.";
            PreviewRevision++;
        }
        catch (ArgumentException exception)
        {
            SetUnavailable(exception.Message);
        }
    }

    private void SetCarrierDisplays(
        BraidCarrierResult carrier,
        bool isSixteenCarrier)
    {
        var pitch = $"{carrier.RecommendedPitchMillimetres:0.00} mm pitch";
        var angles =
            $"{carrier.LongitudinalAngleDegrees:0.00}° longitudinal · " +
            $"{carrier.PerpendicularAngleDegrees:0.00}° workbook perpendicular";
        var coverage =
            $"{carrier.CoverageAtReferencePitchFraction:P2} at 55 mm pitch";
        var length =
            $"{carrier.StrandLengthPerBobbinMetres:0.000} m per strand/bobbin";
        var strands =
            $"{carrier.TotalBraidStrands} total strands · {carrier.BaseFillFraction:P2} base fill";
        var longitudinalAngle = $"{carrier.LongitudinalAngleDegrees:0.00}°";
        var perpendicularAngle = $"{carrier.PerpendicularAngleDegrees:0.00}°";
        var referenceCoverage = $"{carrier.CoverageAtReferencePitchFraction:P2}";
        var baseFill = $"{carrier.BaseFillFraction:P2}";

        if (isSixteenCarrier)
        {
            SixteenCarrierPitchMillimetres =
                carrier.RecommendedPitchMillimetres;
            SixteenCarrierPitchDisplay = pitch;
            SixteenCarrierAnglesDisplay = angles;
            SixteenCarrierCoverageDisplay = coverage;
            SixteenCarrierLengthDisplay = length;
            SixteenCarrierStrandsDisplay = strands;
            SixteenCarrierLongitudinalAngleDisplay = longitudinalAngle;
            SixteenCarrierPerpendicularAngleDisplay = perpendicularAngle;
            SixteenCarrierReferenceCoverageDisplay = referenceCoverage;
            SixteenCarrierBaseFillDisplay = baseFill;
            SixteenCarrierTotalStrandsDisplay = $"{carrier.TotalBraidStrands:N0} total strands";
        }
        else
        {
            TwentyFourCarrierPitchMillimetres =
                carrier.RecommendedPitchMillimetres;
            TwentyFourCarrierPitchDisplay = pitch;
            TwentyFourCarrierAnglesDisplay = angles;
            TwentyFourCarrierCoverageDisplay = coverage;
            TwentyFourCarrierLengthDisplay = length;
            TwentyFourCarrierStrandsDisplay = strands;
            TwentyFourCarrierLongitudinalAngleDisplay = longitudinalAngle;
            TwentyFourCarrierPerpendicularAngleDisplay = perpendicularAngle;
            TwentyFourCarrierReferenceCoverageDisplay = referenceCoverage;
            TwentyFourCarrierBaseFillDisplay = baseFill;
            TwentyFourCarrierTotalStrandsDisplay = $"{carrier.TotalBraidStrands:N0} total strands";
        }
    }

    private void SetRecommendation(BraidCoverageResult result)
    {
        var recommendation = BraidCarrierRecommender.Select(
            result,
            (double)TargetCoveragePercent / 100d);
        IsSixteenCarrierRecommended = recommendation.CarrierCount == 16;
        RecommendationDisplay =
            $"{recommendation.CarrierCount}-carrier is recommended. " +
            recommendation.Reason;
    }

    private void FilterBraidWireOptions(string? retainedId = null)
    {
        _isSynchronizingWireChoice = true;
        try
        {
            BraidWireOptions = _allBraidWires
                .Where(item => item.EndsPerCarrier == SelectedEndCount)
                .ToArray();
            SelectedBraidWire = BraidWireOptions.FirstOrDefault(item =>
                    item.Id == retainedId) ??
                BraidWireOptions.FirstOrDefault(item =>
                    item.Id == SelectedBraidWire?.Id) ??
                BraidWireOptions.FirstOrDefault();
        }
        finally
        {
            _isSynchronizingWireChoice = false;
        }
    }

    private void SetUnavailable(string message)
    {
        CalculationStatus = message;
        CoreLayoutDetail = SelectedCoreLayout?.Display ?? "No core layout selected";
        MeanOutsideDiameterDisplay = "—";
        MeanOutsideDiameterMillimetres = 0d;
        TargetFillDisplay = "—";
        SixteenCarrierPitchDisplay = "—";
        SixteenCarrierAnglesDisplay = "—";
        SixteenCarrierCoverageDisplay = "—";
        SixteenCarrierLengthDisplay = "—";
        SixteenCarrierStrandsDisplay = "—";
        TwentyFourCarrierPitchDisplay = "—";
        TwentyFourCarrierAnglesDisplay = "—";
        TwentyFourCarrierCoverageDisplay = "—";
        TwentyFourCarrierLengthDisplay = "—";
        TwentyFourCarrierStrandsDisplay = "—";
        SixteenCarrierPitchMillimetres = 0d;
        TwentyFourCarrierPitchMillimetres = 0d;
        SixteenCarrierLongitudinalAngleDisplay = "—";
        SixteenCarrierPerpendicularAngleDisplay = "—";
        SixteenCarrierReferenceCoverageDisplay = "—";
        SixteenCarrierBaseFillDisplay = "—";
        SixteenCarrierTotalStrandsDisplay = "—";
        TwentyFourCarrierLongitudinalAngleDisplay = "—";
        TwentyFourCarrierPerpendicularAngleDisplay = "—";
        TwentyFourCarrierReferenceCoverageDisplay = "—";
        TwentyFourCarrierBaseFillDisplay = "—";
        TwentyFourCarrierTotalStrandsDisplay = "—";
        IsSixteenCarrierRecommended = false;
        RecommendationDisplay = "—";
        CalculationSteps.Clear();
        PreviewRevision++;
    }
}
