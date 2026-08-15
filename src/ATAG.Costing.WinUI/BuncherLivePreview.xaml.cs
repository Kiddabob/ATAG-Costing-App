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

public sealed partial class BuncherLivePreview : UserControl
{
    private INotifyPropertyChanged? _observedViewModel;

    public BuncherLivePreview()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            Observe(DataContext as INotifyPropertyChanged);
            Render();
        };
        Unloaded += (_, _) => Observe(null);
    }

    private BuncherLayViewModel? ViewModel => DataContext as BuncherLayViewModel;

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        Observe(args.NewValue as INotifyPropertyChanged);
        Render();
    }

    private void Observe(INotifyPropertyChanged? viewModel)
    {
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

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        DispatcherQueue.TryEnqueue(Render);

    private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Render();

    private void Render()
    {
        if (!IsLoaded || ViewModel?.SelectedChoice is null)
        {
            return;
        }

        var width = Math.Max(PreviewCanvas.ActualWidth, 300d);
        var height = Math.Max(PreviewCanvas.ActualHeight, 160d);
        PreviewCanvas.Children.Clear();

        var bodyLeft = 12d;
        var bodyTop = 24d;
        var bodyWidth = width - 24d;
        var bodyHeight = height - 48d;
        var outline = new SolidColorBrush(Colors.LightGray);
        var body = new Rectangle
        {
            Width = bodyWidth,
            Height = bodyHeight,
            RadiusX = bodyHeight / 2d,
            RadiusY = bodyHeight / 2d,
            Fill = new LinearGradientBrush
            {
                StartPoint = new Point(0.5d, 0d),
                EndPoint = new Point(0.5d, 1d),
                GradientStops =
                {
                    new GradientStop { Color = Color.FromArgb(255, 235, 174, 99), Offset = 0d },
                    new GradientStop { Color = Color.FromArgb(255, 177, 94, 37), Offset = 0.52d },
                    new GradientStop { Color = Color.FromArgb(255, 103, 52, 25), Offset = 1d },
                }
            },
            Stroke = outline,
            StrokeThickness = 1.4d,
        };
        Canvas.SetLeft(body, bodyLeft);
        Canvas.SetTop(body, bodyTop);
        PreviewCanvas.Children.Add(body);

        var clipped = new Canvas
        {
            Width = bodyWidth,
            Height = bodyHeight,
            Clip = new RectangleGeometry { Rect = new Rect(0d, 0d, bodyWidth, bodyHeight) },
        };
        Canvas.SetLeft(clipped, bodyLeft);
        Canvas.SetTop(clipped, bodyTop);
        PreviewCanvas.Children.Add(clipped);

        var lay = ViewModel.SelectedLayLengthMillimetres;
        var pitch = Math.Clamp(lay * 2.2d, 24d, 180d);
        for (var phaseIndex = 0; phaseIndex < 7; phaseIndex++)
        {
            var line = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 217, 159)),
                StrokeThickness = 2.2d,
                StrokeLineJoin = PenLineJoin.Round,
            };
            for (var pointIndex = 0; pointIndex <= 120; pointIndex++)
            {
                var x = bodyWidth * pointIndex / 120d;
                var angle = (Math.PI * 2d * x / pitch) +
                    (phaseIndex * Math.PI * 2d / 7d);
                var y = (bodyHeight / 2d) +
                    (Math.Sin(angle) * ((bodyHeight / 2d) - 5d));
                line.Points.Add(new Point(x, y));
            }
            clipped.Children.Add(line);
        }
    }
}
