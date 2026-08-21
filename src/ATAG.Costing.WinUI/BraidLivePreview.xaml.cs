using System.ComponentModel;
using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Visual-only braid preview. Calculation values continue to come exclusively
/// from <see cref="BraidCoverageViewModel"/>. Rendering is coalesced and uses a
/// bounded vector scene so input changes cannot create thousands of XAML
/// elements or repeatedly rebuild the same geometry.
/// </summary>
public sealed partial class BraidLivePreview : UserControl
{
    private static readonly TimeSpan RenderDelay =
        TimeSpan.FromMilliseconds(50d);

    private readonly DispatcherTimer _renderTimer = new()
    {
        Interval = RenderDelay
    };
    private INotifyPropertyChanged? _observedViewModel;

    public BraidLivePreview()
    {
        InitializeComponent();
        _renderTimer.Tick += RenderTimer_Tick;
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private BraidCoverageViewModel? ViewModel =>
        DataContext as BraidCoverageViewModel;

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
        PropertyChangedEventArgs e)
    {
        // Recalculate updates many display properties. PreviewRevision is the
        // single render boundary raised after the complete result is coherent.
        if (e.PropertyName == nameof(BraidCoverageViewModel.PreviewRevision))
        {
            ScheduleRender();
        }
    }

    private void PreviewCanvas_SizeChanged(
        object sender,
        SizeChangedEventArgs e) => ScheduleRender();

    private void DetailedPreviewToggle_Toggled(
        object sender,
        RoutedEventArgs e) => ScheduleRender();

    private void ScheduleRender()
    {
        if (!IsLoaded)
        {
            return;
        }

        // Restarting the short trailing timer collapses a resize drag or an
        // input recalculation burst into one frame on the WinUI thread.
        _renderTimer.Stop();
        _renderTimer.Start();
    }

    private void RenderTimer_Tick(object? sender, object e)
    {
        _renderTimer.Stop();
        RenderAll();
    }

    private void RenderAll()
    {
        var viewModel = ViewModel;
        if (!IsLoaded || viewModel is null)
        {
            return;
        }

        var outlineBrush = ResourceBrush(
            "TextFillColorSecondaryBrush",
            Colors.Gray);
        var palette = WirePalette(viewModel.SelectedBraidWire?.Copper.MaterialTypeCode);
        DrawCoveragePreview(
            SixteenCarrierCanvas,
            carrierCount: 16,
            viewModel.SixteenCarrierPitchMillimetres,
            viewModel,
            outlineBrush,
            palette);
        DrawCoveragePreview(
            TwentyFourCarrierCanvas,
            carrierCount: 24,
            viewModel.TwentyFourCarrierPitchMillimetres,
            viewModel,
            outlineBrush,
            palette);
    }

    private void DrawCoveragePreview(
        Canvas canvas,
        int carrierCount,
        double physicalPitchMillimetres,
        BraidCoverageViewModel viewModel,
        Brush outlineBrush,
        (Color Clockwise, Color CounterClockwise, Color Edge) palette)
    {
        var width = Math.Max(canvas.ActualWidth, 300d);
        var height = Math.Max(canvas.ActualHeight, 120d);
        canvas.Children.Clear();

        if (physicalPitchMillimetres <= 0d ||
            viewModel.MeanOutsideDiameterMillimetres <= 0d ||
            viewModel.SelectedEndsPerCarrier <= 0 ||
            viewModel.SelectedEffectiveWireDiameterMillimetres <= 0d)
        {
            return;
        }

        const double bodyLeft = 12d;
        const double bodyTop = 20d;
        var bodyWidth = width - 24d;
        var bodyHeight = height - 44d;
        var bodyBottom = bodyTop + bodyHeight;
        var detailed = DetailedPreviewToggle?.IsOn == true;
        var layout = BraidPreviewLayoutBuilder.Create(
            bodyWidth,
            bodyHeight,
            physicalPitchMillimetres,
            viewModel.MeanOutsideDiameterMillimetres,
            carrierCount,
            viewModel.SelectedEndsPerCarrier,
            viewModel.SelectedEffectiveWireDiameterMillimetres,
            detailed);

        var cableBody = new Microsoft.UI.Xaml.Shapes.Rectangle
        {
            Width = bodyWidth,
            Height = bodyHeight,
            RadiusX = bodyHeight / 2d,
            RadiusY = bodyHeight / 2d,
            Fill = CableBrush(),
            Stroke = outlineBrush,
            StrokeThickness = 1.5d
        };
        Canvas.SetLeft(cableBody, bodyLeft);
        Canvas.SetTop(cableBody, bodyTop);
        canvas.Children.Add(cableBody);

        var weaveLayer = new Canvas
        {
            Width = bodyWidth,
            Height = bodyHeight,
            Clip = new RectangleGeometry
            {
                Rect = new Rect(0d, 0d, bodyWidth, bodyHeight)
            }
        };
        Canvas.SetLeft(weaveLayer, bodyLeft);
        Canvas.SetTop(weaveLayer, bodyTop);
        canvas.Children.Add(weaveLayer);

        // Counter-clockwise is the upper base family. The alternating
        // clockwise segments are then drawn once more to create overpasses.
        AddFamily(
            weaveLayer,
            layout.Clockwise.Curves,
            palette.Clockwise,
            palette.Edge,
            layout.FaceThickness,
            layout.ShadowThickness);
        AddFamily(
            weaveLayer,
            layout.CounterClockwise.Curves,
            palette.CounterClockwise,
            palette.Edge,
            layout.FaceThickness,
            layout.ShadowThickness);
        AddFamily(
            weaveLayer,
            layout.Clockwise.OverpassSegments,
            palette.Clockwise,
            palette.Edge,
            layout.FaceThickness,
            layout.ShadowThickness);

        AddEndFace(canvas, bodyLeft, bodyTop, bodyHeight, outlineBrush);
        AddEndFace(
            canvas,
            bodyLeft + bodyWidth - bodyHeight * 0.18d,
            bodyTop,
            bodyHeight,
            outlineBrush);

        var caption = new TextBlock
        {
            FontSize = 11d,
            Foreground = outlineBrush,
            Text = $"{carrierCount} carriers · {viewModel.MeanOutsideDiameterDisplay} mean OD"
        };
        Canvas.SetLeft(caption, bodyLeft + 8d);
        Canvas.SetTop(caption, bodyBottom + 7d);
        canvas.Children.Add(caption);
    }

    private static void AddFamily(
        Canvas target,
        IReadOnlyList<BraidPreviewPolyline> curves,
        Color faceColour,
        Color edgeColour,
        double faceThickness,
        double shadowThickness)
    {
        if (curves.Count == 0)
        {
            return;
        }

        target.Children.Add(CreatePath(
            CreateGeometry(curves),
            edgeColour,
            shadowThickness,
            opacity: 0.72d,
            verticalOffset: 0.65d));
        target.Children.Add(CreatePath(
            CreateGeometry(curves),
            faceColour,
            faceThickness,
            opacity: 0.94d,
            verticalOffset: 0d));
    }

    private static Microsoft.UI.Xaml.Shapes.Path CreatePath(
        Geometry geometry,
        Color colour,
        double thickness,
        double opacity,
        double verticalOffset) => new()
        {
            Data = geometry,
            Stroke = new SolidColorBrush(colour),
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Opacity = opacity,
            RenderTransform = verticalOffset == 0d
                ? null
                : new TranslateTransform { Y = verticalOffset }
        };

    private static PathGeometry CreateGeometry(
        IEnumerable<BraidPreviewPolyline> curves)
    {
        var geometry = new PathGeometry();
        foreach (var curve in curves)
        {
            if (curve.Points.Count < 2)
            {
                continue;
            }

            var figure = new PathFigure
            {
                StartPoint = ToPoint(curve.Points[0]),
                IsClosed = false,
                IsFilled = false
            };
            var segment = new PolyLineSegment();
            for (var index = 1; index < curve.Points.Count; index++)
            {
                segment.Points.Add(ToPoint(curve.Points[index]));
            }

            figure.Segments.Add(segment);
            geometry.Figures.Add(figure);
        }

        return geometry;
    }

    private static Point ToPoint(BraidPreviewPoint point) =>
        new(point.X, point.Y);

    private static LinearGradientBrush CableBrush() => new()
    {
        StartPoint = new Point(0.5d, 0d),
        EndPoint = new Point(0.5d, 1d),
        GradientStops =
        {
            new GradientStop
            {
                Color = Color.FromArgb(255, 35, 48, 58),
                Offset = 0d
            },
            new GradientStop
            {
                Color = Color.FromArgb(255, 76, 91, 102),
                Offset = 0.45d
            },
            new GradientStop
            {
                Color = Color.FromArgb(255, 26, 36, 44),
                Offset = 1d
            }
        }
    };

    private static (Color Clockwise, Color CounterClockwise, Color Edge)
        WirePalette(string? material) =>
        string.Equals(material, "PCW", StringComparison.OrdinalIgnoreCase)
            ? (Color.FromArgb(255, 238, 154, 78),
               Color.FromArgb(255, 196, 103, 40),
               Color.FromArgb(255, 99, 50, 23))
            : string.Equals(material, "TI", StringComparison.OrdinalIgnoreCase)
                ? (Color.FromArgb(255, 205, 220, 230),
                   Color.FromArgb(255, 130, 151, 166),
                   Color.FromArgb(255, 66, 82, 94))
                : (Color.FromArgb(255, 239, 243, 245),
                   Color.FromArgb(255, 184, 198, 207),
                   Color.FromArgb(255, 91, 106, 116));

    private static void AddEndFace(
        Canvas canvas,
        double left,
        double top,
        double bodyHeight,
        Brush outlineBrush)
    {
        var face = new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Width = bodyHeight * 0.36d,
            Height = bodyHeight,
            Fill = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
            Stroke = outlineBrush,
            StrokeThickness = 1.2d
        };
        Canvas.SetLeft(face, left);
        Canvas.SetTop(face, top);
        canvas.Children.Add(face);
    }

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
