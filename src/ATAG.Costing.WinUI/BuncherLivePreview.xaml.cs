using System.ComponentModel;
using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.Domain.Braiding;
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
/// Draws the retained bunch group as equal physical cores. There is no
/// invented central carrier or sheath: a centre core appears only when the
/// selected retained group explicitly contains one.
/// </summary>
public sealed partial class BuncherLivePreview : UserControl
{
    private readonly DispatcherTimer _renderTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(50d),
    };
    private INotifyPropertyChanged? _observedViewModel;

    public BuncherLivePreview()
    {
        InitializeComponent();
        _renderTimer.Tick += RenderTimer_Tick;
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private BuncherLayViewModel? ViewModel =>
        DataContext as BuncherLayViewModel;

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
        if (!IsLoaded || ViewModel?.SelectedCoreLayout is null)
        {
            return;
        }

        DrawCrossSection(ViewModel.SelectedCoreLayout);
        DrawSideProfile(ViewModel.SelectedCoreLayout);
    }

    private void DrawCrossSection(BraidCoreLayout definition)
    {
        var width = Math.Max(CrossSectionCanvas.ActualWidth, 300d);
        var height = Math.Max(CrossSectionCanvas.ActualHeight, 220d);
        CrossSectionCanvas.Children.Clear();
        var layout = CoreLayupPreviewLayoutBuilder.Create(
            definition,
            width,
            height - 24d);
        var outline = ResourceBrush("TextFillColorSecondaryBrush", Colors.LightGray);

        for (var index = 0; index < layout.Cores.Count; index++)
        {
            var core = layout.Cores[index];
            var circle = new Ellipse
            {
                Width = core.Radius * 2d,
                Height = core.Radius * 2d,
                Fill = CoreBrush(index, core.LayerIndex),
                Stroke = outline,
                StrokeThickness = 1.2d,
            };
            Canvas.SetLeft(circle, core.X - core.Radius);
            Canvas.SetTop(circle, core.Y - core.Radius);
            CrossSectionCanvas.Children.Add(circle);
        }

        var label = new TextBlock
        {
            FontSize = 11d,
            Foreground = outline,
            Text = $"{layout.CoreCount} equal cable cores · group {string.Join('-', layout.LayerCounts)}",
        };
        Canvas.SetLeft(label, 8d);
        Canvas.SetTop(label, height - 19d);
        CrossSectionCanvas.Children.Add(label);
    }

    private void DrawSideProfile(BraidCoreLayout definition)
    {
        var width = Math.Max(SideProfileCanvas.ActualWidth, 300d);
        var height = Math.Max(SideProfileCanvas.ActualHeight, 150d);
        SideProfileCanvas.Children.Clear();
        var reference = CoreLayupPreviewLayoutBuilder.Create(
            definition,
            240d,
            180d);
        var centreY = (height / 2d) - 8d;
        var pitch = Math.Clamp(
            (ViewModel?.SelectedLayLengthMillimetres ?? 20d) * 3.2d,
            48d,
            220d);
        var maximumAmplitude = Math.Max(12d, height * 0.34d);
        var outlineColour = Color.FromArgb(210, 63, 43, 28);

        for (var coreIndex = 0; coreIndex < reference.Cores.Count; coreIndex++)
        {
            var core = reference.Cores[coreIndex];
            var radialDistance = Math.Sqrt(
                Math.Pow(core.X - 120d, 2d) +
                Math.Pow(core.Y - 90d, 2d));
            var amplitude = reference.CompositeRadius <= 0d
                ? 0d
                : maximumAmplitude * radialDistance / reference.CompositeRadius;
            var points = new PointCollection();
            for (var pointIndex = 0; pointIndex <= 120; pointIndex++)
            {
                var x = 10d + ((width - 20d) * pointIndex / 120d);
                var angle = (Math.Tau * (x - 10d) / pitch) + core.PhaseRadians;
                var y = core.IsCentral
                    ? centreY
                    : centreY + (Math.Sin(angle) * amplitude);
                points.Add(new Point(x, y));
            }

            SideProfileCanvas.Children.Add(CreateCorePath(
                points,
                new SolidColorBrush(outlineColour),
                12d));
            SideProfileCanvas.Children.Add(CreateCorePath(
                points,
                CoreBrush(coreIndex, core.LayerIndex),
                9d));
        }

        var label = new TextBlock
        {
            FontSize = 11d,
            Foreground = ResourceBrush("TextFillColorSecondaryBrush", Colors.LightGray),
            Text = $"Equal-width cores · {ViewModel?.SelectedChoice?.LayDisplay ?? "no lay selected"} repeat",
        };
        Canvas.SetLeft(label, 8d);
        Canvas.SetTop(label, height - 19d);
        SideProfileCanvas.Children.Add(label);
    }

    private static Polyline CreateCorePath(
        PointCollection points,
        Brush stroke,
        double thickness)
    {
        var ownedPoints = new PointCollection();
        foreach (var point in points)
        {
            ownedPoints.Add(point);
        }

        return new Polyline
        {
            Points = ownedPoints,
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
    }

    private static Brush CoreBrush(int coreIndex, int layerIndex)
    {
        Color[] colours =
        [
            Color.FromArgb(255, 63, 157, 196),
            Color.FromArgb(255, 224, 143, 54),
            Color.FromArgb(255, 83, 177, 111),
            Color.FromArgb(255, 154, 118, 203),
            Color.FromArgb(255, 218, 94, 91),
            Color.FromArgb(255, 202, 187, 74),
        ];
        var colour = colours[(coreIndex + layerIndex) % colours.Length];
        return new LinearGradientBrush
        {
            StartPoint = new Point(0d, 0d),
            EndPoint = new Point(1d, 1d),
            GradientStops =
            {
                new GradientStop { Color = Lighten(colour, 0.22d), Offset = 0d },
                new GradientStop { Color = colour, Offset = 0.55d },
                new GradientStop { Color = Darken(colour, 0.24d), Offset = 1d },
            },
        };
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
