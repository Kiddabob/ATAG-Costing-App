using System.ComponentModel;
using ATAG.Costing.Application.Visualisation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Shared presentation boundary for every cable module. A source adapter only
/// supplies an immutable physical scene; this control owns modes, cancellation,
/// vector batching, the single GPU surface, camera and detached presentation.
/// </summary>
public sealed class UnifiedLivePreview : UserControl, IDisposable
{
    public static readonly DependencyProperty SceneProperty = DependencyProperty.Register(
        nameof(Scene), typeof(CablePreviewScene), typeof(UnifiedLivePreview),
        new PropertyMetadata(null, OnSceneChanged));

    public static readonly DependencyProperty IsPreviewActiveProperty = DependencyProperty.Register(
        nameof(IsPreviewActive), typeof(bool), typeof(UnifiedLivePreview),
        new PropertyMetadata(true, OnActiveChanged));

    private readonly LivePreviewSession _session = new();
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(60) };
    private readonly StackPanel _presentation = new() { Spacing = 12 };
    private readonly Grid _body = new();
    private readonly TextBlock _heading = new() { FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _description = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly ComboBox _mode = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly CheckBox _software = new() { Content = "Use software rendering (WARP)", Visibility = Visibility.Collapsed };
    private readonly Button _popOut = new() { Content = "Pop out" };
    private readonly InfoBar _error = new() { IsClosable = false, Severity = InfoBarSeverity.Warning };
    private readonly Dictionary<int, PreparedPreview> _cache = new();
    private static UnifiedLivePreview? _detachedOwner;
    private INotifyPropertyChanged? _source;
    private Func<CablePreviewScene>? _sceneFactory;
    private CancellationTokenSource? _generationCancellation;
    private LivePreview3DHost? _threeD;
    private UnifiedPreviewWindow? _window;
    private bool _sourceObserved;
    private bool _factoryDirty;
    private bool _disposed;
    private bool _buildingScene;
    private long _revision;

    public UnifiedLivePreview()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        AutomationProperties.SetName(this, "Unified LIVE Preview");
        _mode.Items.Add("Simple 2D · cross-section and side");
        _mode.Items.Add("Detailed 2D · cross-section and side");
        _mode.Items.Add("Orbital 3D · orbit, pan and zoom");
        _mode.SelectedIndex = 0;
        AutomationProperties.SetName(_mode, "Preview type");
        _mode.SelectionChanged += (_, _) =>
        {
            _session.Mode = (LivePreviewMode)Math.Max(0, _mode.SelectedIndex);
            _software.Visibility = _mode.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            ReleaseSurface();
            Schedule();
        };
        _software.IsChecked = _session.ForceWarp;
        _software.Checked += (_, _) => _session.PreferSoftware = true;
        _software.Unchecked += (_, _) => _session.PreferSoftware = false;
        _popOut.Click += (_, _) => { if (_window is null) OpenWindow(); else ReturnToDock(); };
        var bar = new Grid { ColumnSpacing = 8 };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.Children.Add(_mode);
        Grid.SetColumn(_popOut, 1);
        bar.Children.Add(_popOut);
        _presentation.Children.Add(_heading);
        _presentation.Children.Add(_description);
        _presentation.Children.Add(bar);
        _presentation.Children.Add(_software);
        _presentation.Children.Add(_error);
        _presentation.Children.Add(_body);
        _presentation.Children.Add(_status);
        Content = _presentation;
        _debounce.Tick += Debounce_Tick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public CablePreviewScene? Scene
    {
        get => (CablePreviewScene?)GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public bool IsPreviewActive
    {
        get => (bool)GetValue(IsPreviewActiveProperty);
        set => SetValue(IsPreviewActiveProperty, value);
    }

    public void SetActive(bool active) => IsPreviewActive = active;
    public void UpdateScene(CablePreviewScene scene) => Scene = scene;
    public void ShowInteractive3D() => _mode.SelectedIndex = 2;
    public void Show3D() => ShowInteractive3D();

    public void BindSource(INotifyPropertyChanged source, Func<CablePreviewScene> factory)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(factory);
        Observe(false);
        _source = source;
        _sceneFactory = factory;
        _factoryDirty = true;
        Observe(CanRender);
        Schedule();
    }

    private bool CanRender => !_disposed && IsPreviewActive && (IsLoaded || _window is not null);

    private static void OnSceneChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var preview = (UnifiedLivePreview)sender;
        preview._cache.Clear();
        if (!preview._buildingScene)
        {
            preview._factoryDirty = false;
            preview.Schedule();
        }
    }

    private static void OnActiveChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var preview = (UnifiedLivePreview)sender;
        if (args.NewValue is true)
        {
            preview._factoryDirty = preview._sceneFactory is not null;
            preview.Observe(preview.CanRender);
            preview.Schedule();
        }
        else
        {
            preview.ReturnToDock();
            preview.Suspend();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        _factoryDirty = _sceneFactory is not null;
        Observe(CanRender);
        Schedule();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (_window is null) Suspend();
    }

    private void Observe(bool observe)
    {
        if (_source is null || _sourceObserved == observe) return;
        if (observe) _source.PropertyChanged += Source_Changed;
        else _source.PropertyChanged -= Source_Changed;
        _sourceObserved = observe;
    }

    private void Source_Changed(object? sender, PropertyChangedEventArgs args)
    {
        // Inputs and result notifications are captured as one coherent snapshot
        // after the VM's update burst; no geometry is constructed in a binding.
        if (!DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(() => Source_Changed(sender, args));
            return;
        }
        _factoryDirty = true;
        Schedule();
    }

    private void Schedule()
    {
        _generationCancellation?.Cancel();
        _revision++;
        _debounce.Stop();
        if (CanRender) _debounce.Start();
    }

    private async void Debounce_Tick(object? sender, object args)
    {
        _debounce.Stop();
        if (!CanRender) return;
        var revision = _revision;
        var mode = _mode.SelectedIndex;
        _generationCancellation?.Dispose();
        _generationCancellation = new CancellationTokenSource();
        var cancellation = _generationCancellation.Token;
        try
        {
            if (_factoryDirty && _sceneFactory is not null)
            {
                _buildingScene = true;
                try { Scene = _sceneFactory(); }
                finally { _buildingScene = false; }
                _factoryDirty = false;
            }
            var scene = Scene;
            _heading.Text = scene?.Title ?? "LIVE Preview";
            _description.Text = scene?.Description ?? "Enter physical cable dimensions to see the preview.";
            _error.IsOpen = false;
            if (scene is null || scene.Components.IsEmpty)
            {
                ReleaseSurface();
                _status.Text = "Preview awaiting valid physical inputs.";
                return;
            }

            if (!_cache.TryGetValue(mode, out var prepared))
            {
                _status.Text = "Preparing cable view…";
                prepared = await Task.Run(() => Prepare(scene, mode, cancellation), cancellation);
                if (cancellation.IsCancellationRequested || revision != _revision || !CanRender) return;
                _cache[mode] = prepared;
            }
            if (revision != _revision || !CanRender) return;
            await PresentAsync(prepared, scene, mode, cancellation, revision);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            if (revision != _revision || !CanRender) return;
            ReleaseSurface();
            _error.Title = "Preview unavailable";
            _error.Message = exception.Message + " Check the module inputs or select Simple 2D.";
            _error.IsOpen = true;
            _status.Text = "Calculation results are still available in the module.";
            Program.Log($"Unified LIVE Preview: {exception}");
        }
    }

    private static PreparedPreview Prepare(CablePreviewScene scene, int mode, CancellationToken token)
    {
        if (mode == 2)
        {
            var mesh = CablePreviewGeometry.BuildMesh(scene, detailed: true, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            if (mesh.Vertices.IsEmpty || mesh.Indices.IsEmpty)
                throw new InvalidOperationException("This scene has no 3D surface. Select a 2D view for its drawing.");
            var extent = mesh.MaximumMm - mesh.MinimumMm;
            var normalization = 6f / Math.Max(0.001f, Math.Max(extent.X, Math.Max(extent.Y, extent.Z)));
            var inspectionComponent = scene.Components.LastOrDefault(c => c.Kind == CablePreviewComponentKind.Insulation)
                ?? scene.Components.FirstOrDefault(c => c.Kind is CablePreviewComponentKind.Core or CablePreviewComponentKind.Coil)
                ?? scene.Components[0];
            var point = inspectionComponent.PathMm[inspectionComponent.PathMm.Length / 2];
            if (inspectionComponent.PathMm.Length == 2)
                point = (inspectionComponent.PathMm[0] + inspectionComponent.PathMm[1]) / 2;
            if (scene.Print is { } print && print.Layout.Dots.Length >= 2)
            {
                // The layout emits two equal complete impressions in order.
                // Focus actual ink in the first one, never empty cable halfway
                // between the two repeats (which can be hundreds of mm apart).
                var firstImpression = print.Layout.Dots.Take(print.Layout.Dots.Length / 2);
                var axialCentre = (firstImpression.Min(dot => dot.X) + firstImpression.Max(dot => dot.X)) / 2;
                point = new(-(float)print.SurfaceDiameterMm / 2, 0,
                    (float)print.StartZMm + axialCentre);
            }
            var target = (point - (mesh.MinimumMm + mesh.MaximumMm) / 2) * normalization;
            var inspectionDistance = Math.Clamp((float)Math.Max(inspectionComponent.Section.WidthMm,
                inspectionComponent.Section.HeightMm) * normalization * 3f, 0.02f, 10.5f);
            var geometry = new PreviewGeometry(mesh.Vertices.Select(v =>
                new PreviewVertex(new(v.Position.Z, v.Position.Y, -v.Position.X),
                    new(v.Normal.Z, v.Normal.Y, -v.Normal.X), v.Colour)).ToArray(), mesh.Indices.ToArray(),
                new(target.Z, target.Y, -target.X), inspectionDistance);
            return new PreparedPreview(null, null, geometry, mesh.Notes.ToArray());
        }
        var cross = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.CrossSection,
            detailed: mode == 1, cancellationToken: token);
        var side = CablePreviewGeometry.BuildDrawing(scene, CablePreviewProjection.Side,
            detailed: mode == 1, cancellationToken: token);
        return new PreparedPreview(UnifiedPreviewDrawing.Serialize(cross, true, token),
            UnifiedPreviewDrawing.Serialize(side, false, token), null,
            cross.Notes.Concat(side.Notes).Distinct().ToArray());
    }

    private async Task PresentAsync(PreparedPreview prepared, CablePreviewScene scene, int mode,
        CancellationToken cancellation, long revision)
    {
        if (prepared.Geometry is { } geometry)
        {
            _session.SetGeometry(geometry, scene.Title);
            if (_threeD is null)
            {
                _body.Children.Clear();
                _threeD = new LivePreview3DHost(_session) { Height = 380 };
                AutomationProperties.SetName(_threeD, "Orbital 3D cable view");
                _body.Children.Add(_threeD);
            }
            _status.Text = "Drag to orbit · Shift/right-drag to pan · wheel to zoom. " +
                "Keyboard: arrows orbit; Shift+arrows pan; Page Up/Down zoom; Home resets. " +
                string.Join(" ", scene.Notes.Concat(prepared.Notes).Distinct());
        }
        else
        {
            var prefix = mode == 1 ? "Detailed" : "Simple";
            var cross = await UnifiedPreviewDrawing.CreateAsync(prepared.CrossSection!, $"{prefix} cross-section", cancellation);
            var side = await UnifiedPreviewDrawing.CreateAsync(prepared.Side!, $"{prefix} side view", cancellation);
            if (revision != _revision || !CanRender) return;
            ReleaseSurface();
            var stack = new StackPanel { Spacing = 16 };
            stack.Children.Add(cross);
            stack.Children.Add(side);
            _body.Children.Add(stack);
            _status.Text = "Physical geometry · views fitted independently. " +
                string.Join(" ", scene.Notes.Concat(prepared.Notes).Distinct());
        }
    }

    public void OpenWindow()
    {
        if (!CanRender) return;
        try
        {
            if (_window is not null) { _window.Activate(); return; }
            _detachedOwner?.ReturnToDock();
            Content = new TextBlock { Text = "LIVE Preview is open in its own window.", TextWrapping = TextWrapping.Wrap };
            _popOut.Content = "Return to workspace";
            _window = new UnifiedPreviewWindow(_presentation, ActualTheme, Scene?.Title ?? "LIVE Preview", WindowClosed);
            _detachedOwner = this;
            _window.Activate();
        }
        catch (Exception exception)
        {
            // Roll back presentation ownership if window creation, moving to a
            // display or activation fails. The scene/camera stay in this session.
            var failedWindow = _window;
            _window = null;
            if (failedWindow is not null)
            {
                failedWindow.ReleasePresentation();
                try { failedWindow.Close(); }
                catch (Exception cleanupError) { Program.Log($"Preview window cleanup failed: {cleanupError.Message}"); }
            }
            WindowClosed();
            _error.Title = "Could not open the preview window";
            _error.Message = $"The preview has returned to the workspace. {exception.Message}";
            _error.IsOpen = true;
            Program.Log($"LIVE Preview pop-out failed: {exception}");
        }
    }

    public void ReturnToDock()
    {
        var window = _window;
        if (window is null) return;
        window.ReleasePresentation();
        window.Close();
        WindowClosed();
    }

    private void WindowClosed()
    {
        _window = null;
        if (ReferenceEquals(_detachedOwner, this)) _detachedOwner = null;
        _popOut.Content = "Pop out";
        if (!_disposed) Content = _presentation;
        if (CanRender) Schedule();
        else Suspend();
    }

    private void ReleaseSurface()
    {
        _threeD?.Dispose();
        _threeD = null;
        _body.Children.Clear();
    }

    private void Suspend()
    {
        _debounce.Stop();
        _generationCancellation?.Cancel();
        _revision++;
        Observe(false);
        ReleaseSurface();
        _cache.Clear();
        _session.ClearGeometry();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReturnToDock();
        Suspend();
        _generationCancellation?.Dispose();
        _cache.Clear();
        _source = null;
        _sceneFactory = null;
        _debounce.Tick -= Debounce_Tick;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
    }

    private sealed record PreparedPreview(byte[]? CrossSection,
        byte[]? Side, PreviewGeometry? Geometry, IReadOnlyList<string> Notes);
}
