using System.ComponentModel;
using System.Globalization;
using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Visual-only dual-insulation renderer. It consumes the visible dimensions
/// and selected retained records; all costing remains in the Domain layer.
/// </summary>
public sealed partial class DualLivePreview : UserControl
{
    private INotifyPropertyChanged? _observedViewModel;

    public DualLivePreview()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            Observe(DataContext as INotifyPropertyChanged);
            RenderAll();
        };
        Unloaded += (_, _) => Observe(null);
    }

    private DualInsulationCostingViewModel? ViewModel =>
        DataContext as DualInsulationCostingViewModel;

    private void OnDataContextChanged(
        FrameworkElement sender,
        DataContextChangedEventArgs args)
    {
        Observe(args.NewValue as INotifyPropertyChanged);
        RenderAll();
    }

    private void Observe(INotifyPropertyChanged? viewModel)
    {
        if (ReferenceEquals(_observedViewModel, viewModel))
        {
            return;
        }

        if (_observedViewModel is not null)
        {
            _observedViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _observedViewModel = viewModel;
        if (_observedViewModel is not null)
        {
            _observedViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e) =>
        DispatcherQueue.TryEnqueue(RenderAll);

    private void PreviewCanvas_SizeChanged(
        object sender,
        SizeChangedEventArgs e) => RenderAll();

    private void DetailedToggle_Toggled(
        object sender,
        RoutedEventArgs e) => RenderAll();

    private void RenderAll()
    {
        if (!IsLoaded || ViewModel is null)
        {
            return;
        }

        DrawCrossSection();
        DrawSideProfile();
    }

    private void DrawCrossSection()
    {
        var canvas = CrossSectionCanvas;
        canvas.Children.Clear();
        var width = Math.Max(canvas.ActualWidth, 300d);
        var height = Math.Max(canvas.ActualHeight, 250d);
        var centerX = width / 2d;
        var centerY = (height / 2d) - 12d;
        var (conductorOd, firstOd, finalOd) = EffectiveDiameters();
        var radius = Math.Min(width * 0.32d, height * 0.39d);
        var firstRadius = radius * firstOd / finalOd;
        var conductorRadius = radius * conductorOd / finalOd;
        var outline = ResourceBrush("TextFillColorSecondaryBrush", Colors.White);
        var firstColour = LayerBrush(ViewModel!.SelectedFirstMasterbatch?.ColourHex, Color.FromArgb(255, 57, 150, 187));
        var secondColour = LayerBrush(ViewModel.SelectedSecondMasterbatch?.ColourHex, Color.FromArgb(255, 35, 86, 128));

        AddCircle(canvas, centerX, centerY, radius, secondColour, outline, 1.5d);
        AddCircle(canvas, centerX, centerY, firstRadius, firstColour, outline, 1.2d);
        DrawConductor(canvas, centerX, centerY, conductorRadius, outline);

        AddLabel(canvas, 12d, 7d, $"Final OD  {finalOd:0.###} mm", outline);
        AddLabel(canvas, 12d, 28d, $"Second wall  {(finalOd - firstOd) / 2d:0.###} mm", outline);
        AddLabel(canvas, 12d, height - 40d, $"First wall  {(firstOd - conductorOd) / 2d:0.###} mm", outline);
        AddLabel(canvas, 12d, height - 19d, $"Conductor OD  {conductorOd:0.###} mm", outline);
    }

    private void DrawSideProfile()
    {
        var canvas = SideProfileCanvas;
        canvas.Children.Clear();
        var width = Math.Max(canvas.ActualWidth, 300d);
        var height = Math.Max(canvas.ActualHeight, 180d);
        var (conductorOd, firstOd, finalOd) = EffectiveDiameters();
        var maximumHeight = height * 0.62d;
        var firstHeight = maximumHeight * firstOd / finalOd;
        var conductorHeight = maximumHeight * conductorOd / finalOd;
        var top = 16d;
        var outline = ResourceBrush("TextFillColorSecondaryBrush", Colors.White);
        var firstColour = LayerBrush(ViewModel!.SelectedFirstMasterbatch?.ColourHex, Color.FromArgb(255, 57, 150, 187));
        var secondColour = LayerBrush(ViewModel.SelectedSecondMasterbatch?.ColourHex, Color.FromArgb(255, 35, 86, 128));

        AddCylinder(canvas, 12d, top, width * 0.70d, maximumHeight, secondColour, outline);
        AddCylinder(canvas, width * 0.20d, top + ((maximumHeight - firstHeight) / 2d), width * 0.62d, firstHeight, firstColour, outline);
        AddCylinder(canvas, width * 0.43d, top + ((maximumHeight - conductorHeight) / 2d), width * 0.53d, conductorHeight, CopperBrush(), outline);

        AddLabel(canvas, 14d, top + maximumHeight + 14d, "SECOND INSULATION", outline);
        AddLabel(canvas, width * 0.32d, top + maximumHeight + 35d, "FIRST INSULATION", outline);
        AddLabel(canvas, width * 0.69d, top + maximumHeight + 56d, "COPPER", outline);
    }

    private void DrawConductor(
        Canvas canvas,
        double centerX,
        double centerY,
        double radius,
        Brush outline)
    {
        var construction = ViewModel?.SelectedCopper?.Construction;
        if (DetailedToggle?.IsOn != true || construction is null)
        {
            AddCircle(canvas, centerX, centerY, radius, CopperBrush(), outline, 1.1d);
            return;
        }

        var layout = ConductorPreviewLayoutBuilder.Create(
            construction,
            centerX,
            centerY,
            radius);
        foreach (var strand in layout.Strands)
        {
            AddCircle(
                canvas,
                strand.X,
                strand.Y,
                strand.Radius,
                CopperBrush(),
                outline,
                0.65d);
        }
    }

    private (double Conductor, double First, double Final) EffectiveDiameters()
    {
        var conductor = PositiveOr(ViewModel?.ConductorOutsideDiameterMillimetres, 0.5d);
        var first = Math.Max(PositiveOr(ViewModel?.FirstFinishedOutsideDiameterMillimetres, conductor * 1.5d), conductor);
        var final = Math.Max(PositiveOr(ViewModel?.SecondFinishedOutsideDiameterMillimetres, first * 1.35d), first);
        return (conductor, first, final);
    }

    private static double PositiveOr(double? value, double fallback) =>
        value is > 0d && double.IsFinite(value.Value) ? value.Value : fallback;

    private static void AddCircle(
        Canvas canvas,
        double centerX,
        double centerY,
        double radius,
        Brush fill,
        Brush stroke,
        double strokeThickness)
    {
        var circle = new Ellipse
        {
            Width = radius * 2d,
            Height = radius * 2d,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness
        };
        Canvas.SetLeft(circle, centerX - radius);
        Canvas.SetTop(circle, centerY - radius);
        canvas.Children.Add(circle);
    }

    private static void AddCylinder(
        Canvas canvas,
        double left,
        double top,
        double width,
        double height,
        Brush fill,
        Brush outline)
    {
        var body = new Rectangle
        {
            Width = width,
            Height = height,
            RadiusX = height / 2d,
            RadiusY = height / 2d,
            Fill = fill,
            Stroke = outline,
            StrokeThickness = 1.2d
        };
        Canvas.SetLeft(body, left);
        Canvas.SetTop(body, top);
        canvas.Children.Add(body);
    }

    private static void AddLabel(
        Canvas canvas,
        double left,
        double top,
        string text,
        Brush brush)
    {
        var label = new TextBlock
        {
            FontSize = 11d,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = brush,
            Text = text
        };
        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        canvas.Children.Add(label);
    }

    private static Brush CopperBrush() => new LinearGradientBrush
    {
        StartPoint = new Point(0d, 0d),
        EndPoint = new Point(1d, 1d),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(255, 255, 194, 100), Offset = 0d },
            new GradientStop { Color = Color.FromArgb(255, 194, 105, 42), Offset = 0.55d },
            new GradientStop { Color = Color.FromArgb(255, 116, 56, 20), Offset = 1d },
        }
    };

    private static Brush LayerBrush(string? hex, Color fallback)
    {
        var baseColour = TryParseHex(hex, out var colour) ? colour : fallback;
        return new LinearGradientBrush
        {
            StartPoint = new Point(0.5d, 0d),
            EndPoint = new Point(0.5d, 1d),
            GradientStops =
            {
                new GradientStop { Color = Lighten(baseColour, 0.24d), Offset = 0d },
                new GradientStop { Color = baseColour, Offset = 0.48d },
                new GradientStop { Color = Darken(baseColour, 0.26d), Offset = 1d },
            }
        };
    }

    private static bool TryParseHex(string? hex, out Color colour)
    {
        colour = default;
        var value = hex?.Trim().TrimStart('#');
        if (value?.Length != 6 ||
            !byte.TryParse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) ||
            !byte.TryParse(value[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) ||
            !byte.TryParse(value[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return false;
        }

        colour = Color.FromArgb(255, red, green, blue);
        return true;
    }

    private static Color Lighten(Color colour, double amount) => Color.FromArgb(
        colour.A,
        (byte)Math.Clamp(colour.R + ((255 - colour.R) * amount), 0d, 255d),
        (byte)Math.Clamp(colour.G + ((255 - colour.G) * amount), 0d, 255d),
        (byte)Math.Clamp(colour.B + ((255 - colour.B) * amount), 0d, 255d));

    private static Color Darken(Color colour, double amount) => Color.FromArgb(
        colour.A,
        (byte)Math.Clamp(colour.R * (1d - amount), 0d, 255d),
        (byte)Math.Clamp(colour.G * (1d - amount), 0d, 255d),
        (byte)Math.Clamp(colour.B * (1d - amount), 0d, 255d));

    private static Brush ResourceBrush(string key, Color fallback)
    {
        try
        {
            return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];
        }
        catch
        {
            return new SolidColorBrush(fallback);
        }
    }
}
