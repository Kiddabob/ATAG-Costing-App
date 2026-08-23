using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Production host for the shared interactive 3D LIVE Preview. The host owns
/// at most one renderer and writes camera state back to the session before it
/// moves between the dock and detached window.
/// </summary>
internal sealed class LivePreview3DHost : UserControl, IDisposable
{
    private readonly LivePreviewSession _session;
    private readonly SwapChainPanel _surface = new();
    private readonly TextBlock _statusText = new();
    private readonly TextBlock _sceneText = new();
    private Preview3DRenderer? _renderer;
    private bool _isPointerActive;
    private uint _activePointerId;
    private Point _lastPointerPosition;
    private bool _isPanGesture;
    private bool _isDisposed;
    private bool _hasLoggedRendererReady;

    public LivePreview3DHost(LivePreviewSession session)
    {
        _session = session;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        MinHeight = 320;
        Content = BuildContent();
        _session.SceneChanged += Session_SceneChanged;
        _surface.Loaded += Surface_Loaded;
        _surface.Unloaded += Surface_Unloaded;
        _surface.SizeChanged += Surface_SizeChanged;
        _surface.PointerPressed += Surface_PointerPressed;
        _surface.PointerMoved += Surface_PointerMoved;
        _surface.PointerReleased += Surface_PointerReleased;
        _surface.PointerCanceled += Surface_PointerReleased;
        _surface.PointerWheelChanged += Surface_PointerWheelChanged;
    }

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            Background = ResourceBrush("SolidBackgroundFillColorBaseBrush"),
        };
        root.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star),
        });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _surface.HorizontalAlignment = HorizontalAlignment.Stretch;
        _surface.VerticalAlignment = VerticalAlignment.Stretch;
        root.Children.Add(_surface);

        var footer = new Grid
        {
            Padding = new Thickness(12, 10, 12, 12),
            ColumnSpacing = 10,
            Background = ResourceBrush("ControlFillColorDefaultBrush"),
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });

        var resetButton = new Button
        {
            Content = "Reset view",
            VerticalAlignment = VerticalAlignment.Center,
        };
        resetButton.Click += (_, _) => _renderer?.ResetCamera();
        footer.Children.Add(resetButton);

        var status = new StackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        _sceneText.Text = _session.Scene.Description;
        _sceneText.HorizontalAlignment = HorizontalAlignment.Right;
        _sceneText.TextAlignment = TextAlignment.Right;
        _sceneText.TextWrapping = TextWrapping.Wrap;
        _sceneText.Foreground = ResourceBrush("TextFillColorSecondaryBrush");
        _statusText.Text = "Preparing renderer";
        _statusText.HorizontalAlignment = HorizontalAlignment.Right;
        _statusText.TextAlignment = TextAlignment.Right;
        _statusText.FontSize = 11;
        _statusText.Foreground = ResourceBrush("TextFillColorTertiaryBrush");
        status.Children.Add(_sceneText);
        status.Children.Add(_statusText);
        Grid.SetColumn(status, 1);
        footer.Children.Add(status);
        Grid.SetRow(footer, 1);
        root.Children.Add(footer);
        return root;
    }

    private void Surface_Loaded(object sender, RoutedEventArgs e) =>
        RecreateRenderer();

    private void Surface_Unloaded(object sender, RoutedEventArgs e) =>
        ReleaseRenderer();

    private void Surface_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _surface.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height),
        };
        _renderer?.Resize(
            e.NewSize.Width,
            e.NewSize.Height,
            _surface.XamlRoot?.RasterizationScale ?? 1d);
    }

    private void Session_SceneChanged(object? sender, EventArgs e)
    {
        _sceneText.Text = _session.Scene.Description;
        try
        {
            _renderer?.UpdateScene(_session.Scene);
        }
        catch (Exception exception)
        {
            Program.Log($"LIVE Preview scene update failed: {exception}");
            _statusText.Text = $"Preview update failed: {exception.Message}";
        }
    }

    private void RecreateRenderer()
    {
        if (!_surface.IsLoaded || _isDisposed)
        {
            return;
        }

        try
        {
            ReleaseRenderer();
            _renderer = new Preview3DRenderer(
                _surface,
                UpdateStatistics,
                _session.Scene,
                _session.Camera);
            _renderer.Initialize(_session.ForceWarp);
        }
        catch (Exception exception)
        {
            Program.Log($"LIVE Preview 3D renderer failed: {exception}");
            ReleaseRenderer();
            _statusText.Text = $"3D unavailable: {exception.Message}";
        }
    }

    private void Surface_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_surface);
        if (!point.Properties.IsLeftButtonPressed &&
            !point.Properties.IsRightButtonPressed &&
            !point.Properties.IsMiddleButtonPressed)
        {
            return;
        }

        _activePointerId = e.Pointer.PointerId;
        _isPointerActive = _surface.CapturePointer(e.Pointer);
        _lastPointerPosition = point.Position;
        _isPanGesture = point.Properties.IsRightButtonPressed ||
            point.Properties.IsMiddleButtonPressed ||
            e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift);
        e.Handled = true;
    }

    private void Surface_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerActive || e.Pointer.PointerId != _activePointerId)
        {
            return;
        }

        var position = e.GetCurrentPoint(_surface).Position;
        var deltaX = (float)(position.X - _lastPointerPosition.X);
        var deltaY = (float)(position.Y - _lastPointerPosition.Y);
        _lastPointerPosition = position;
        if (_isPanGesture)
        {
            _renderer?.Pan(deltaX, deltaY);
        }
        else
        {
            _renderer?.Orbit(deltaX, deltaY);
        }

        e.Handled = true;
    }

    private void Surface_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerId != _activePointerId)
        {
            return;
        }

        _surface.ReleasePointerCapture(e.Pointer);
        _isPointerActive = false;
        e.Handled = true;
    }

    private void Surface_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        _renderer?.Zoom(e.GetCurrentPoint(_surface).Properties.MouseWheelDelta);
        e.Handled = true;
    }

    private void UpdateStatistics(Preview3DStatistics statistics)
    {
        _statusText.Text =
            $"{statistics.Driver} · {statistics.RenderMilliseconds:F2} ms · " +
            $"{statistics.PixelWidth}×{statistics.PixelHeight} px";
        if (!_hasLoggedRendererReady)
        {
            _hasLoggedRendererReady = true;
            Program.Log(
                $"LIVE Preview 3D renderer ready: {statistics.Driver}; " +
                $"{statistics.RenderMilliseconds:F2} ms; " +
                $"{statistics.PixelWidth}x{statistics.PixelHeight} px.");
        }
    }

    private void ReleaseRenderer()
    {
        if (_renderer is not null)
        {
            _session.Camera = _renderer.CameraState;
            _renderer.Dispose();
            _renderer = null;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        ReleaseRenderer();
        _session.SceneChanged -= Session_SceneChanged;
        _surface.Loaded -= Surface_Loaded;
        _surface.Unloaded -= Surface_Unloaded;
        _surface.SizeChanged -= Surface_SizeChanged;
        _surface.PointerPressed -= Surface_PointerPressed;
        _surface.PointerMoved -= Surface_PointerMoved;
        _surface.PointerReleased -= Surface_PointerReleased;
        _surface.PointerCanceled -= Surface_PointerReleased;
        _surface.PointerWheelChanged -= Surface_PointerWheelChanged;
    }

    private static Brush ResourceBrush(string key) =>
        (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];
}
