using System.Collections.ObjectModel;
using ATAG.Costing.Domain.Braiding;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATAG.Costing.WinUI.ViewModels;

public partial class BraidCoverageViewModel : ObservableObject
{
    public IReadOnlyList<BraidCoreLayout> CoreLayouts =>
        BraidReferenceTables.CoreLayouts;

    public IReadOnlyList<int> EndsPerCarrierOptions =>
        BraidReferenceTables.EndsPerCarrierOptions;

    public IReadOnlyList<double> EffectiveWireDiameterOptions =>
        BraidReferenceTables.EffectiveWireDiameterOptionsMillimetres;

    public IReadOnlyList<BuncherLaySetting> BuncherLaySettings =>
        BraidReferenceTables.BuncherLaySettings;

    public ObservableCollection<CalculationStepRow> CalculationSteps { get; } = [];

    [ObservableProperty]
    public partial double TargetCoveragePercent { get; set; } = 80d;

    [ObservableProperty]
    public partial double CoreOutsideDiameterMillimetres { get; set; } = 10d;

    [ObservableProperty]
    public partial BraidCoreLayout? SelectedCoreLayout { get; set; }

    [ObservableProperty]
    public partial int SelectedEndsPerCarrier { get; set; } = 6;

    [ObservableProperty]
    public partial double SelectedEffectiveWireDiameterMillimetres { get; set; } = 0.2d;

    [ObservableProperty]
    public partial double CableLengthMetres { get; set; } = 1d;

    [ObservableProperty]
    public partial BuncherLaySetting? SelectedBuncherLaySetting { get; set; }

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
    public partial string BuncherSizeDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string BuncherGearsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string BuncherTraceDisplay { get; set; } =
        "Choose a target lay length to reveal the matching machine and gears.";

    public BraidCoverageViewModel()
    {
        SelectedCoreLayout = CoreLayouts[0];
        SelectedBuncherLaySetting = BuncherLaySettings.First(
            setting => Math.Abs(setting.LayLengthMillimetres - 19.43d) < 0.001d);
        Recalculate();
        RefreshBuncherResult();
    }

    partial void OnTargetCoveragePercentChanged(double value) => Recalculate();

    partial void OnCoreOutsideDiameterMillimetresChanged(double value) => Recalculate();

    partial void OnSelectedCoreLayoutChanged(BraidCoreLayout? value) => Recalculate();

    partial void OnSelectedEndsPerCarrierChanged(int value) => Recalculate();

    partial void OnSelectedEffectiveWireDiameterMillimetresChanged(double value) => Recalculate();

    partial void OnCableLengthMetresChanged(double value) => Recalculate();

    partial void OnSelectedBuncherLaySettingChanged(BuncherLaySetting? value) =>
        RefreshBuncherResult();

    private void Recalculate()
    {
        if (SelectedCoreLayout is null)
        {
            SetUnavailable("Choose the number of cores and lay-up.");
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
            TargetFillDisplay = $"{result.TargetFillFraction:P2}";
            SetCarrierDisplays(result.SixteenCarrier, isSixteenCarrier: true);
            SetCarrierDisplays(result.TwentyFourCarrier, isSixteenCarrier: false);

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

        if (isSixteenCarrier)
        {
            SixteenCarrierPitchDisplay = pitch;
            SixteenCarrierAnglesDisplay = angles;
            SixteenCarrierCoverageDisplay = coverage;
            SixteenCarrierLengthDisplay = length;
            SixteenCarrierStrandsDisplay = strands;
        }
        else
        {
            TwentyFourCarrierPitchDisplay = pitch;
            TwentyFourCarrierAnglesDisplay = angles;
            TwentyFourCarrierCoverageDisplay = coverage;
            TwentyFourCarrierLengthDisplay = length;
            TwentyFourCarrierStrandsDisplay = strands;
        }
    }

    private void RefreshBuncherResult()
    {
        var setting = SelectedBuncherLaySetting;
        if (setting is null)
        {
            BuncherSizeDisplay = "—";
            BuncherGearsDisplay = "—";
            BuncherTraceDisplay =
                "Choose a target lay length to reveal the matching machine and gears.";
            return;
        }

        BuncherSizeDisplay = $"{setting.BuncherSize} buncher";
        BuncherGearsDisplay = $"Gear A {setting.GearA} · Gear B {setting.GearB}";
        BuncherTraceDisplay =
            $"Exact table match: {setting.LayLengthMillimetres:0.##} mm → " +
            $"{setting.BuncherSize} buncher → gears {setting.GearA} & {setting.GearB}.";
    }

    private void SetUnavailable(string message)
    {
        CalculationStatus = message;
        CoreLayoutDetail = SelectedCoreLayout?.Display ?? "No core layout selected";
        MeanOutsideDiameterDisplay = "—";
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
        CalculationSteps.Clear();
    }
}
