using System.ComponentModel;
using ATAG.Costing.Domain.Coiling;
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
/// Bounded event-driven coil visualisation. The domain result remains the
/// single source for bar, turn and length geometry.
/// </summary>
public sealed partial class CoilLivePreview : UserControl
{
    private const int MaximumVisibleTurns = 24;
    private readonly DispatcherTimer _renderTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(50d),
    };
    private INotifyPropertyChanged? _observedViewModel;

    public CoilLivePreview()
    {
        InitializeComponent();
        _renderTimer.Tick += RenderTimer_Tick;
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private CoilCalculatorViewModel? ViewModel =>
        DataContext as CoilCalculatorViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Observe(DataContext as INotifyPropertyChanged);
        ScheduleRender();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _renderTimer.Stop();
        Observe(null);
    }

    private void OnDataContextChanged(
        FrameworkElement sender,
        DataContextChangedEventArgs args)
    {
        if (IsLoaded)
        {
            Observe(args.NewValue as INotifyPropertyChanged);
            ScheduleRender();
        }
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
        PropertyChangedEventArgs e) => ScheduleRender();

    private void PreviewCanvas_SizeChanged(
        object sender,
        SizeChangedEventArgs e) => ScheduleRender();

    private void ScheduleRender()
    {
        if (!IsLoaded)
        {
            return;
        }
        _renderTimer.Stop();
        _renderTimer.Start();
    }

    private void RenderTimer_Tick(object? sender, object e)
    {
        _renderTimer.Stop();
        Render();
    }

    private void Render()
    {
        var canvas = PreviewCanvas;
        canvas.Children.Clear();
        var width = Math.Max(canvas.ActualWidth, 320d);
        var height = Math.Max(canvas.ActualHeight, 320d);
        var result = ViewModel?.PreviewResult;
        if (result is null)
        {
            AddLabel(
                canvas,
                16d,
                22d,
                "Enter valid cable and finished-coil dimensions to draw the LIVE coil.",
                ResourceBrush("TextFillColorSecondaryBrush", Colors.Gray));
            return;
        }

        var outline = ResourceBrush("TextFillColorSecondaryBrush", Colors.LightGray);
        var cable = CableBrush();
        var coilTop = 34d;
        var coilHeight = Math.Min(210d, height * 0.57d);
        var centreY = coilTop + (coilHeight / 2d);
        var left = Math.Max(52d, width * 0.15d);
        var right = Math.Min(width - 52d, width * 0.85d);
        var span = Math.Max(120d, right - left);
        var outsideDiameter = Math.Max(
            ViewModel!.FinishedCoilOutsideDiameterMillimetres,
            result.RequiredBarDiameterMillimetres +
            (2d * result.RadialCableThicknessMillimetres));
        var barHeight = Math.Max(
            12d,
            coilHeight * result.RequiredBarDiameterMillimetres / outsideDiameter);

        var bar = new Rectangle
        {
            Width = span + 44d,
            Height = barHeight,
            RadiusX = barHeight / 2d,
            RadiusY = barHeight / 2d,
            Fill = BarBrush(),
            Stroke = outline,
            StrokeThickness = 1d,
        };
        Canvas.SetLeft(bar, left - 22d);
        Canvas.SetTop(bar, centreY - (barHeight / 2d));
        canvas.Children.Add(bar);

        var visibleTurns = Math.Clamp(result.CompleteTurns, 1, MaximumVisibleTurns);
        var cableThickness = Math.Clamp(
            span / Math.Max(visibleTurns * 1.45d, 12d),
            5d,
            12d);
        for (var turn = 0; turn < visibleTurns; turn++)
        {
            var x = visibleTurns == 1
                ? left + (span / 2d)
                : left + (span * turn / (visibleTurns - 1d));
            var loop = new Ellipse
            {
                Width = Math.Max(34d, coilHeight * 0.34d),
                Height = coilHeight,
                Fill = new SolidColorBrush(Color.FromArgb(18, 230, 145, 61)),
                Stroke = cable,
                StrokeThickness = cableThickness,
            };
            Canvas.SetLeft(loop, x - (loop.Width / 2d));
            Canvas.SetTop(loop, coilTop);
            canvas.Children.Add(loop);
        }

        var firstTail = new Line
        {
            X1 = left,
            Y1 = centreY - (coilHeight / 2d),
            X2 = 10d,
            Y2 = centreY - (coilHeight / 2d),
            Stroke = cable,
            StrokeThickness = cableThickness,
            StrokeStartLineCap = PenLineCap.Round,
        };
        var secondTail = new Line
        {
            X1 = right,
            Y1 = centreY - (coilHeight / 2d),
            X2 = width - 10d,
            Y2 = centreY - (coilHeight / 2d),
            Stroke = cable,
            StrokeThickness = cableThickness,
            StrokeEndLineCap = PenLineCap.Round,
        };
        canvas.Children.Add(firstTail);
        canvas.Children.Add(secondTail);

        AddCableSection(canvas, 18d, height - 92d, cable, outline);
        AddLabel(canvas, 76d, height - 86d, ViewModel.SelectedShape?.Name ?? "Cable", outline);
        AddLabel(canvas, 76d, height - 65d,
            $"Radial {result.RadialCableThicknessMillimetres:0.###} mm · pitch {result.AxialPitchMillimetres:0.###} mm",
            outline);
        AddLabel(canvas, 16d, height - 34d,
            $"Bar {result.RequiredBarDiameterMillimetres:0.###} mm · {result.CompleteTurns:N0} complete turns · wound width {result.ActualWoundAxialLengthMillimetres:0.###} mm",
            outline);
        if (result.CompleteTurns > MaximumVisibleTurns)
        {
            AddLabel(canvas, 16d, 8d,
                $"Representative view · {MaximumVisibleTurns} of {result.CompleteTurns:N0} turns drawn",
                outline);
        }
    }

    private void AddCableSection(
        Canvas canvas,
        double left,
        double top,
        Brush fill,
        Brush outline)
    {
        var shape = ViewModel?.SelectedShape?.Shape ?? CoilCableShape.Round;
        Shape section = shape == CoilCableShape.Round
            ? new Ellipse()
            : new Rectangle
            {
                RadiusX = shape == CoilCableShape.DShape ? 14d : 4d,
                RadiusY = shape == CoilCableShape.DShape ? 14d : 4d,
            };
        section.Width = shape == CoilCableShape.Round ? 42d : 52d;
        section.Height = 42d;
        section.Fill = fill;
        section.Stroke = outline;
        section.StrokeThickness = 1.2d;
        Canvas.SetLeft(section, left);
        Canvas.SetTop(section, top);
        canvas.Children.Add(section);
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
            Foreground = brush,
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = Math.Max(180d, canvas.ActualWidth - left - 12d),
        };
        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        canvas.Children.Add(label);
    }

    private static Brush CableBrush() => new LinearGradientBrush
    {
        StartPoint = new Point(0d, 0d),
        EndPoint = new Point(1d, 1d),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(255, 245, 176, 89), Offset = 0d },
            new GradientStop { Color = Color.FromArgb(255, 190, 94, 36), Offset = 0.58d },
            new GradientStop { Color = Color.FromArgb(255, 104, 49, 24), Offset = 1d },
        },
    };

    private static Brush BarBrush() => new LinearGradientBrush
    {
        StartPoint = new Point(0.5d, 0d),
        EndPoint = new Point(0.5d, 1d),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(255, 160, 173, 184), Offset = 0d },
            new GradientStop { Color = Color.FromArgb(255, 84, 96, 107), Offset = 0.5d },
            new GradientStop { Color = Color.FromArgb(255, 43, 51, 59), Offset = 1d },
        },
    };

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
