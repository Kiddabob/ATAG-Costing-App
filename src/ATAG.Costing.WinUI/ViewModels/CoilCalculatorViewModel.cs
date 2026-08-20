using System.Collections.ObjectModel;
using ATAG.Costing.Domain.Coiling;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace ATAG.Costing.WinUI.ViewModels;

public sealed record CoilShapeChoice(
    CoilCableShape Shape,
    string Name,
    string Description);

public partial class CoilCalculatorViewModel : ObservableObject
{
    public IReadOnlyList<CoilShapeChoice> ShapeOptions { get; } =
    [
        new(
            CoilCableShape.Round,
            "Round cable",
            "One diameter is used both radially and as the no-gap pitch between turns."),
        new(
            CoilCableShape.Flat,
            "Flat cable",
            "Cable height sits radially around the bar; cable width advances along the coil with no gap."),
        new(
            CoilCableShape.DShape,
            "D-shape cable",
            "Cable height sits radially around the bar; cable width advances along the coil with no gap."),
    ];

    public ObservableCollection<CalculationStepRow> CalculationSteps { get; } = [];

    [ObservableProperty]
    public partial CoilShapeChoice? SelectedShape { get; set; }

    [ObservableProperty]
    public partial double CableHeightMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double CableWidthMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double FinishedCoilOutsideDiameterMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double RequiredAxialLengthMillimetres { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double TailOneMillimetres { get; set; }

    [ObservableProperty]
    public partial double TailTwoMillimetres { get; set; }

    [ObservableProperty]
    public partial double StripOneMillimetres { get; set; }

    [ObservableProperty]
    public partial double StripTwoMillimetres { get; set; }

    [ObservableProperty]
    public partial double CoilQuantity { get; set; } = 1d;

    [ObservableProperty]
    public partial string CalculationStatus { get; set; } =
        "Enter the cable and finished-coil dimensions to calculate.";

    [ObservableProperty]
    public partial string CalculationStatusTitle { get; set; } = "Waiting for dimensions";

    [ObservableProperty]
    public partial InfoBarSeverity CalculationStatusSeverity { get; set; } =
        InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial string RequiredBarDiameterDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CompleteTurnsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string CablePerCoilDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string TotalCableDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string ActualWoundWidthDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string MeanPathDiameterDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string HelicalTurnLengthDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string EndsAndStripsDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial string AxialFitDisplay { get; set; } = "—";

    [ObservableProperty]
    public partial InfoBarSeverity AxialFitSeverity { get; set; } =
        InfoBarSeverity.Informational;

    public string ShapeDescription =>
        SelectedShape?.Description ?? "Choose the cable cross-section.";

    public string CableHeightHeader => SelectedShape?.Shape == CoilCableShape.Round
        ? "Cable diameter (mm)"
        : "Cable height · radial (mm)";

    public string CableWidthHeader => SelectedShape?.Shape == CoilCableShape.Round
        ? "Axial pitch · same as diameter (mm)"
        : "Cable width · axial pitch (mm)";

    public bool IsCableWidthEnabled => SelectedShape?.Shape != CoilCableShape.Round;

    public CoilCalculatorViewModel()
    {
        SelectedShape = ShapeOptions.First(choice =>
            choice.Shape == CoilCableShape.Flat);
    }

    partial void OnSelectedShapeChanged(CoilShapeChoice? value)
    {
        OnPropertyChanged(nameof(ShapeDescription));
        OnPropertyChanged(nameof(CableHeightHeader));
        OnPropertyChanged(nameof(CableWidthHeader));
        OnPropertyChanged(nameof(IsCableWidthEnabled));
        if (value?.Shape == CoilCableShape.Round)
        {
            CableWidthMillimetres = CableHeightMillimetres;
        }

        Recalculate();
    }

    partial void OnCableHeightMillimetresChanged(double value)
    {
        if (SelectedShape?.Shape == CoilCableShape.Round)
        {
            CableWidthMillimetres = value;
        }

        Recalculate();
    }

    partial void OnCableWidthMillimetresChanged(double value) => Recalculate();

    partial void OnFinishedCoilOutsideDiameterMillimetresChanged(double value) =>
        Recalculate();

    partial void OnRequiredAxialLengthMillimetresChanged(double value) => Recalculate();

    partial void OnTailOneMillimetresChanged(double value) => Recalculate();

    partial void OnTailTwoMillimetresChanged(double value) => Recalculate();

    partial void OnStripOneMillimetresChanged(double value) => Recalculate();

    partial void OnStripTwoMillimetresChanged(double value) => Recalculate();

    partial void OnCoilQuantityChanged(double value) => Recalculate();

    private void Recalculate()
    {
        if (SelectedShape is null)
        {
            SetUnavailable("Choose a cable shape.");
            return;
        }

        if (!double.IsFinite(CoilQuantity) ||
            CoilQuantity <= 0d ||
            CoilQuantity > int.MaxValue ||
            Math.Abs(CoilQuantity - Math.Round(CoilQuantity)) > 0.000000001d)
        {
            SetUnavailable("Coil quantity must be a positive whole number.");
            return;
        }

        try
        {
            var result = CoilCableLengthCalculator.Calculate(
                new CoilCableLengthInputs(
                    SelectedShape.Shape,
                    CableHeightMillimetres,
                    CableWidthMillimetres,
                    FinishedCoilOutsideDiameterMillimetres,
                    RequiredAxialLengthMillimetres,
                    TailOneMillimetres,
                    TailTwoMillimetres,
                    StripOneMillimetres,
                    StripTwoMillimetres,
                    checked((int)Math.Round(CoilQuantity))));

            RequiredBarDiameterDisplay =
                $"{result.RequiredBarDiameterMillimetres:0.###} mm";
            CompleteTurnsDisplay =
                $"{result.CompleteTurns:N0} full turn{(result.CompleteTurns == 1 ? string.Empty : "s")}";
            CablePerCoilDisplay =
                $"{result.CableLengthPerCoilMillimetres / 1000d:0.000} m";
            TotalCableDisplay = $"{result.TotalCableLengthMetres:0.000} m";
            ActualWoundWidthDisplay =
                $"{result.ActualWoundAxialLengthMillimetres:0.###} mm";
            MeanPathDiameterDisplay =
                $"{result.MeanPathDiameterMillimetres:0.###} mm";
            HelicalTurnLengthDisplay =
                $"{result.HelicalLengthPerTurnMillimetres:0.###} mm";
            EndsAndStripsDisplay =
                $"{result.EndAndStripLengthMillimetres:0.###} mm per coil";
            AxialFitDisplay = result.AxialOverrunMillimetres <= 0.000001d
                ? "Exact axial fit · no turn-rounding overrun"
                : $"Complete turns add {result.AxialOverrunMillimetres:0.###} mm beyond the requested axial length.";
            AxialFitSeverity = result.AxialOverrunMillimetres <= 0.000001d
                ? InfoBarSeverity.Success
                : InfoBarSeverity.Warning;

            CalculationSteps.Clear();
            foreach (var step in result.Steps)
            {
                CalculationSteps.Add(new(
                    step.Label,
                    step.BusinessMeaning ?? string.Empty,
                    step.Expression,
                    step.SubstitutedExpression,
                    $"{step.DisplayValue} {step.Unit}".Trim(),
                    step.RoundingRule ?? string.Empty,
                    step.RuleVersion ?? CoilCableLengthCalculator.RuleVersion,
                    step.Warning));
            }

            CalculationStatusTitle = "Coil plan ready";
            CalculationStatus =
                "Live · geometry only. Pricing is intentionally excluded from this module.";
            CalculationStatusSeverity = InfoBarSeverity.Success;
        }
        catch (ArgumentException exception)
        {
            SetUnavailable(FriendlyMessage(exception));
        }
    }

    private void SetUnavailable(string message)
    {
        CalculationStatusTitle = "Check the inputs";
        CalculationStatus = message;
        CalculationStatusSeverity = InfoBarSeverity.Warning;
        RequiredBarDiameterDisplay = "—";
        CompleteTurnsDisplay = "—";
        CablePerCoilDisplay = "—";
        TotalCableDisplay = "—";
        ActualWoundWidthDisplay = "—";
        MeanPathDiameterDisplay = "—";
        HelicalTurnLengthDisplay = "—";
        EndsAndStripsDisplay = "—";
        AxialFitDisplay = "—";
        AxialFitSeverity = InfoBarSeverity.Informational;
        CalculationSteps.Clear();
    }

    private static string FriendlyMessage(ArgumentException exception)
    {
        const string parameterSuffix = " (Parameter";
        var suffixIndex = exception.Message.IndexOf(
            parameterSuffix,
            StringComparison.Ordinal);
        return suffixIndex < 0
            ? exception.Message
            : exception.Message[..suffixIndex];
    }
}
