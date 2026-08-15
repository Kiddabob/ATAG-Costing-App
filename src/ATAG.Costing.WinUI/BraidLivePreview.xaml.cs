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
/// Visual-only braid and buncher preview. Calculation values continue to come
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

    private void RenderAll()
    {
        if (!IsLoaded || ViewModel is null)
        {
            return;
        }

        DrawCoveragePreview(SixteenCarrierCanvas, carrierCount: 16);
        DrawCoveragePreview(TwentyFourCarrierCanvas, carrierCount: 24);
        DrawBuncherPreview(BuncherCanvas);
    }

    private void DrawCoveragePreview(Canvas canvas, int carrierCount)
    {
        var width = Math.Max(canvas.ActualWidth, 300d);
        var height = Math.Max(canvas.ActualHeight, 120d);
        canvas.Children.Clear();

        var bodyLeft = 12d;
        var bodyTop = 24d;
        var bodyWidth = width - 24d;
        var bodyHeight = height - 48d;
        var bodyBottom = bodyTop + bodyHeight;
        var strandThickness = Math.Clamp(
            1.25d +
            (ViewModel!.SelectedEffectiveWireDiameterMillimetres /
             Math.Max(ViewModel.CoreOutsideDiameterMillimetres, 0.001d)) * 45d,
            1.5d,
            4.5d);
        var coverage = Math.Clamp(
            ViewModel.TargetCoveragePercent / 100d,
            0.05d,
            0.99d);
        var densitySpacing = Math.Clamp(
            30d - (coverage * 20d) - (carrierCount == 24 ? 3d : 0d),
            7d,
            24d);

        var cableBrush = new SolidColorBrush(Color.FromArgb(42, 67, 184, 212));
        var outlineBrush = ResourceBrush(
            "TextFillColorSecondaryBrush",
            Colors.Gray);
        var forwardBrush = ResourceBrush(
            "AccentFillColorDefaultBrush",
            Colors.Coral);
        var reverseBrush = ResourceBrush(
            "AtagCyanBrush",
            Colors.DeepSkyBlue);

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

        var diagonalRun = Math.Max(bodyHeight * 1.15d, 54d);
        for (var x = -diagonalRun; x <= bodyWidth + diagonalRun; x += densitySpacing)
        {
            weaveLayer.Children.Add(new Line
            {
                X1 = x,
                Y1 = 0d,
                X2 = x + diagonalRun,
                Y2 = bodyHeight,
                Stroke = forwardBrush,
                StrokeThickness = strandThickness,
                Opacity = 0.82d
            });
            weaveLayer.Children.Add(new Line
            {
                X1 = x + diagonalRun,
                Y1 = 0d,
                X2 = x,
                Y2 = bodyHeight,
                Stroke = reverseBrush,
                StrokeThickness = strandThickness,
                Opacity = 0.72d
            });
        }

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

    private void DrawBuncherPreview(Canvas canvas)
    {
        var width = Math.Max(canvas.ActualWidth, 300d);
        var height = Math.Max(canvas.ActualHeight, 108d);
        canvas.Children.Clear();

        var bodyLeft = 12d;
        var bodyTop = 24d;
        var bodyWidth = width - 24d;
        var bodyHeight = height - 48d;
        var outlineBrush = ResourceBrush(
            "TextFillColorSecondaryBrush",
            Colors.Gray);
        var accentBrush = ResourceBrush(
            "AccentFillColorDefaultBrush",
            Colors.Coral);
        var mutedBrush = new SolidColorBrush(Color.FromArgb(34, 127, 127, 127));

        var body = new Rectangle
        {
            Width = bodyWidth,
            Height = bodyHeight,
            RadiusX = bodyHeight / 2d,
            RadiusY = bodyHeight / 2d,
            Fill = mutedBrush,
            Stroke = outlineBrush,
            StrokeThickness = 1.5d
        };
        Canvas.SetLeft(body, bodyLeft);
        Canvas.SetTop(body, bodyTop);
        canvas.Children.Add(body);

        var selectedLay = ViewModel?.SelectedBuncherLaySetting?.LayLengthMillimetres ?? 20d;
        var visualSpacing = Math.Clamp(selectedLay * 2.2d, 24d, 86d);
        var layLayer = new Canvas
        {
            Width = bodyWidth,
            Height = bodyHeight,
            Clip = new RectangleGeometry
            {
                Rect = new Rect(0d, 0d, bodyWidth, bodyHeight)
            }
        };
        Canvas.SetLeft(layLayer, bodyLeft);
        Canvas.SetTop(layLayer, bodyTop);
        canvas.Children.Add(layLayer);
        for (var x = -bodyHeight; x < bodyWidth; x += visualSpacing)
        {
            layLayer.Children.Add(new Line
            {
                X1 = x,
                Y1 = 0d,
                X2 = x + bodyHeight,
                Y2 = bodyHeight,
                Stroke = accentBrush,
                StrokeThickness = 2.5d,
                Opacity = 0.86d
            });
        }

        var caption = new TextBlock
        {
            FontSize = 11d,
            Foreground = outlineBrush,
            Text = $"{selectedLay:0.##} mm target lay · {ViewModel?.BuncherSizeDisplay}"
        };
        Canvas.SetLeft(caption, bodyLeft + 8d);
        Canvas.SetTop(caption, bodyTop + bodyHeight + 7d);
        canvas.Children.Add(caption);
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
