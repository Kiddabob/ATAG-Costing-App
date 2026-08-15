using System.ComponentModel;
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
/// Visual-only braid preview. Calculation values continue to come
/// exclusively from <see cref="BraidCoverageViewModel"/>.
/// </summary>
public sealed partial class BraidLivePreview : UserControl
{
    private INotifyPropertyChanged? _observedViewModel;

    public BraidLivePreview()
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

    private BraidCoverageViewModel? ViewModel =>
        DataContext as BraidCoverageViewModel;

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

    private void DetailedPreviewToggle_Toggled(
        object sender,
        RoutedEventArgs e) => RenderAll();

    private void RenderAll()
    {
        if (!IsLoaded || ViewModel is null)
        {
            return;
        }

        DrawCoveragePreview(SixteenCarrierCanvas, carrierCount: 16);
        DrawCoveragePreview(TwentyFourCarrierCanvas, carrierCount: 24);
    }

    private void DrawCoveragePreview(Canvas canvas, int carrierCount)
    {
        var width = Math.Max(canvas.ActualWidth, 300d);
        var height = Math.Max(canvas.ActualHeight, 120d);
        canvas.Children.Clear();

        var bodyLeft = 12d;
        var bodyTop = 20d;
        var bodyWidth = width - 24d;
        var bodyHeight = height - 44d;
        var bodyBottom = bodyTop + bodyHeight;
        var detailed = DetailedPreviewToggle?.IsOn == true;
        var endCount = Math.Max(ViewModel!.SelectedEndsPerCarrier, 1);
        var strandThickness = Math.Clamp(
            (ViewModel.SelectedEffectiveWireDiameterMillimetres /
             Math.Max(ViewModel.CoreOutsideDiameterMillimetres, 0.001d)) * bodyHeight,
            detailed ? 0.75d : 1.6d,
            detailed ? 2.2d : 5.4d);
        var pitch = CarrierPitch(carrierCount);
        var visualPitch = Math.Clamp(
            pitch / Math.Max(ViewModel.CoreOutsideDiameterMillimetres, 0.001d) *
            bodyHeight * 0.72d,
            34d,
            170d);

        var cableBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0.5d, 0d),
            EndPoint = new Point(0.5d, 1d),
            GradientStops =
            {
                new GradientStop { Color = Color.FromArgb(255, 35, 48, 58), Offset = 0d },
                new GradientStop { Color = Color.FromArgb(255, 76, 91, 102), Offset = 0.45d },
                new GradientStop { Color = Color.FromArgb(255, 26, 36, 44), Offset = 1d },
            }
        };
        var outlineBrush = ResourceBrush(
            "TextFillColorSecondaryBrush",
            Colors.Gray);
        var (wireLight, wireMid, wireDark) = WirePalette();

        var cableBody = new Rectangle
        {
            Width = bodyWidth,
            Height = bodyHeight,
            RadiusX = bodyHeight / 2d,
            RadiusY = bodyHeight / 2d,
            Fill = cableBrush,
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

        DrawInterlacedWeave(
            weaveLayer,
            bodyWidth,
            bodyHeight,
            visualPitch,
            carrierCount,
            endCount,
            detailed,
            strandThickness,
            wireLight,
            wireMid,
            wireDark);

        AddEndFace(canvas, bodyLeft, bodyTop, bodyHeight, outlineBrush);
        AddEndFace(
            canvas,
            bodyLeft + bodyWidth - (bodyHeight * 0.18d),
            bodyTop,
            bodyHeight,
            outlineBrush);

        var caption = new TextBlock
        {
            FontSize = 11d,
            Foreground = outlineBrush,
            Text = $"{carrierCount} carriers · {ViewModel.MeanOutsideDiameterDisplay} mean OD"
        };
        Canvas.SetLeft(caption, bodyLeft + 8d);
        Canvas.SetTop(caption, bodyBottom + 7d);
        canvas.Children.Add(caption);
    }

    private void DrawInterlacedWeave(
        Canvas layer,
        double width,
        double height,
        double pitch,
        int carrierCount,
        int endsPerCarrier,
        bool detailed,
        double strandThickness,
        Color clockwiseColour,
        Color counterClockwiseColour,
        Color edgeColour)
    {
        // A braid alternates which carrier direction is uppermost. Rendering the
        // complete clockwise set and then the complete counter-clockwise set
        // makes one direction look pasted over the other. These clipped bands
        // alternate the drawing order so each family visibly passes over and
        // under the other while retaining the same pitch geometry.
        var bandWidth = Math.Clamp(pitch / 4d, 7d, 24d);
        var bandCount = Math.Max((int)Math.Ceiling(width / bandWidth), 1);
        for (var bandIndex = 0; bandIndex < bandCount; bandIndex++)
        {
            var bandLeft = bandIndex * bandWidth;
            var clippedWidth = Math.Min(bandWidth + 0.8d, width - bandLeft);
            var band = new Canvas
            {
                Width = width,
                Height = height,
                Clip = new RectangleGeometry
                {
                    Rect = new Rect(bandLeft, 0d, clippedWidth, height)
                }
            };
            layer.Children.Add(band);

            var clockwiseOnTop = bandIndex % 2 == 0;
            if (clockwiseOnTop)
            {
                DrawCarrierDirection(band, -1d, counterClockwiseColour);
                DrawCarrierDirection(band, 1d, clockwiseColour);
            }
            else
            {
                DrawCarrierDirection(band, 1d, clockwiseColour);
                DrawCarrierDirection(band, -1d, counterClockwiseColour);
            }
        }

        void DrawCarrierDirection(Canvas target, double direction, Color colour) =>
            DrawHelicalCarrierSet(
                target,
                width,
                height,
                pitch,
                carrierCount / 2,
                endsPerCarrier,
                direction,
                detailed,
                strandThickness,
                colour,
                edgeColour);
    }

    private void DrawHelicalCarrierSet(
        Canvas layer,
        double width,
        double height,
        double pitch,
        int carriersPerDirection,
        int endsPerCarrier,
        double direction,
        bool detailed,
        double strandThickness,
        Color faceColour,
        Color edgeColour)
    {
        var visibleCarriers = Math.Clamp(carriersPerDirection, 4, 12);
        var phaseStep = Math.PI * 2d / visibleCarriers;
        var renderEnds = detailed ? Math.Clamp(endsPerCarrier, 1, 10) : 1;
        var bundleWidth = detailed
            ? strandThickness * Math.Max(renderEnds - 1, 0) * 0.72d
            : Math.Clamp(strandThickness * Math.Sqrt(endsPerCarrier) * 1.65d, 2.4d, 9d);

        for (var carrier = 0; carrier < visibleCarriers; carrier++)
        {
            var phase = carrier * phaseStep;
            for (var end = 0; end < renderEnds; end++)
            {
                var endOffset = renderEnds == 1
                    ? 0d
                    : (end - ((renderEnds - 1d) / 2d)) * strandThickness * 0.72d;
                var shadow = BuildHelix(
                    width,
                    height,
                    pitch,
                    phase,
                    direction,
                    endOffset + 0.65d);
                shadow.Stroke = new SolidColorBrush(edgeColour);
                shadow.StrokeThickness = detailed
                    ? strandThickness + 0.65d
                    : bundleWidth + 1.1d;
                shadow.Opacity = 0.72d;
                layer.Children.Add(shadow);

                var strand = BuildHelix(
                    width,
                    height,
                    pitch,
                    phase,
                    direction,
                    endOffset);
                strand.Stroke = new SolidColorBrush(faceColour);
                strand.StrokeThickness = detailed
                    ? strandThickness
                    : bundleWidth;
                strand.Opacity = carrier % 2 == 0 ? 0.98d : 0.86d;
                layer.Children.Add(strand);
            }
        }
    }

    private static Polyline BuildHelix(
        double width,
        double height,
        double pitch,
        double phase,
        double direction,
        double verticalOffset)
    {
        var line = new Polyline
        {
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
        var centre = height / 2d;
        var amplitude = Math.Max((height / 2d) - 4d, 4d);
        var sampleCount = Math.Max((int)Math.Ceiling(width / 3d), 80);
        for (var index = 0; index <= sampleCount; index++)
        {
            var x = width * index / sampleCount;
            var angle = direction * ((Math.PI * 2d * x / pitch) + phase);
            var y = centre + (Math.Sin(angle) * amplitude) + verticalOffset;
            line.Points.Add(new Point(x, y));
        }

        return line;
    }

    private double CarrierPitch(int carrierCount)
    {
        var display = carrierCount == 16
            ? ViewModel?.SixteenCarrierPitchDisplay
            : ViewModel?.TwentyFourCarrierPitchDisplay;
        var token = display?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return double.TryParse(token, out var pitch) && pitch > 0d
            ? pitch
            : 55d;
    }

    private (Color Light, Color Mid, Color Dark) WirePalette()
    {
        var material = ViewModel?.SelectedBraidWire?.Copper.MaterialTypeCode;
        return string.Equals(material, "PCW", StringComparison.OrdinalIgnoreCase)
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
    }

    private static void AddEndFace(
        Canvas canvas,
        double left,
        double top,
        double bodyHeight,
        Brush outlineBrush)
    {
        var face = new Ellipse
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
