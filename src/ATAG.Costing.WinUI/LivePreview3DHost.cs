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
    private bool _softwareRecoveryAttempted;
    private XamlRoot? _observedXamlRoot;
    private double _rasterizationScale;
    private long _surfaceLifecycleRevision;

    public LivePreview3DHost(LivePreviewSession session)
    {
        _session = session;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        MinHeight = 320;
        Content = BuildContent();
        _session.SceneChanged += Session_SceneChanged;
        _session.DriverChanged += Session_DriverChanged;
        IsTabStop = true;
        KeyDown += Host_KeyDown;
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

        var cameraButtons = new StackPanel { Spacing = 6 };
        var resetButton = new Button
        {
            Content = "Fit cable",
            VerticalAlignment = VerticalAlignment.Center,
        };
        resetButton.Click += (_, _) => _renderer?.ResetCamera();
        cameraButtons.Children.Add(resetButton);
        var inspectButton = new Button { Content = "Inspect surface" };
        inspectButton.Click += (_, _) => _renderer?.InspectSurface();
        cameraButtons.Children.Add(inspectButton);
        footer.Children.Add(cameraButtons);

        var status = new StackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        _sceneText.Text = SceneDescription;
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

    private void Surface_Loaded(object sender, RoutedEventArgs e) => ScheduleSurfaceReconcile();

    private void Surface_Unloaded(object sender, RoutedEventArgs e) => ScheduleSurfaceReconcile();

    private void ScheduleSurfaceReconcile()
    {
        var revision = ++_surfaceLifecycleRevision;
        // Reparenting between XamlRoots can deliver the old Unloaded after the
        // new Loaded. Reconcile the settled tree once, rather than allowing an
        // obsolete event to dispose the newly attached renderer.
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isDisposed || revision != _surfaceLifecycleRevision) return;
            var root = _surface.XamlRoot;
            if (!_surface.IsLoaded || root is null)
            {
                ObserveXamlRoot(null);
                ReleaseRenderer();
                return;
            }
            var changedRoot = !ReferenceEquals(root, _observedXamlRoot);
            ObserveXamlRoot(root);
            if (_renderer is null || changedRoot) RecreateRenderer();
        });
    }

    private void ObserveXamlRoot(XamlRoot? root)
    {
        if (_observedXamlRoot is not null) _observedXamlRoot.Changed -= XamlRoot_Changed;
        _observedXamlRoot = root;
        _rasterizationScale = root?.RasterizationScale ?? 1;
        if (root is not null) root.Changed += XamlRoot_Changed;
    }

    private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        if (Math.Abs(sender.RasterizationScale - _rasterizationScale) < 0.001) return;
        _rasterizationScale = sender.RasterizationScale;
        try { _renderer?.Resize(_surface.ActualWidth, _surface.ActualHeight, _rasterizationScale); }
        catch (Exception exception) { HandleRenderFailure(exception.Message); }
    }

    private void Surface_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _surface.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height),
        };
        try
        {
            _renderer?.Resize(
                e.NewSize.Width,
                e.NewSize.Height,
                _surface.XamlRoot?.RasterizationScale ?? 1d);
        }
        catch (Exception exception)
        {
            HandleRenderFailure(exception.Message);
        }
    }

    private void Session_SceneChanged(object? sender, EventArgs e)
    {
        _sceneText.Text = SceneDescription;
        try
        {
            if (_session.Geometry is { } geometry)
                _renderer?.UpdateGeometry(geometry);
            else
                _renderer?.UpdateScene(_session.Scene);
        }
        catch (Exception exception)
        {
            Program.Log($"LIVE Preview scene update failed: {exception}");
            _statusText.Text = $"Preview update failed: {exception.Message}";
        }
    }

    private string SceneDescription => _session.Geometry is null
        ? _session.Scene.Description : _session.GeometryDescription;

    private void Session_DriverChanged(object? sender, EventArgs e)
    {
        _softwareRecoveryAttempted = false;
        RecreateRenderer();
    }

    private void Host_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var pan = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        float dx = 0, dy = 0;
        switch (e.Key)
        {
            case VirtualKey.Left: dx = -8; break;
            case VirtualKey.Right: dx = 8; break;
            case VirtualKey.Up: dy = -8; break;
            case VirtualKey.Down: dy = 8; break;
            case VirtualKey.Add:
            case VirtualKey.PageUp: _renderer?.Zoom(120); break;
            case VirtualKey.Subtract:
            case VirtualKey.PageDown: _renderer?.Zoom(-120); break;
            case VirtualKey.Home: _renderer?.ResetCamera(); break;
            default: return;
        }
        if (dx != 0 || dy != 0)
        {
            if (pan) _renderer?.Pan(dx, dy);
            else _renderer?.Orbit(dx, dy);
        }
        e.Handled = true;
    }

    private void RecreateRenderer(bool forceSoftware = false)
    {
        if (!_surface.IsLoaded || _isDisposed)
        {
            return;
        }

        try
        {
            ReleaseRenderer();
            _hasLoggedRendererReady = false;
            _renderer = new Preview3DRenderer(
                _surface,
                UpdateStatistics,
                _session.Scene,
                _session.Camera,
                _session.Geometry,
                HandleRenderFailure);
            _renderer.Initialize(_session.ForceWarp || forceSoftware);
        }
        catch (Exception exception)
        {
            Program.Log($"LIVE Preview 3D renderer failed: {exception}");
            ReleaseRenderer();
            HandleRenderFailure(exception.Message);
        }
    }

    private void HandleRenderFailure(string message)
    {
        if (!_isDisposed && !_session.ForceWarp && !_softwareRecoveryAttempted)
        {
            _softwareRecoveryAttempted = true;
            RecreateRenderer(forceSoftware: true);
            return;
        }
        ReleaseRenderer();
        _statusText.Text = $"3D unavailable: {message} Select Simple 2D to continue.";
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
        Focus(FocusState.Pointer);
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
        ObserveXamlRoot(null);
        ReleaseRenderer();
        _session.SceneChanged -= Session_SceneChanged;
        _session.DriverChanged -= Session_DriverChanged;
        KeyDown -= Host_KeyDown;
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
