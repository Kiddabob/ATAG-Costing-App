using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace ATAG.Costing.WinUI;

/// <summary>A presentation-only owned window; the live control is transferred, never cloned.</summary>
internal sealed class UnifiedPreviewWindow : Window
{
    private readonly Grid _host = new();
    private readonly AppWindow _retainedAppWindow;
    private readonly Action _returnToDock;
    private readonly Window? _owner;
    private bool _enforcingMinimum;
    private bool _closed;
    private static RectInt32? _lastPlacement;

    public UnifiedPreviewWindow(FrameworkElement presentation, ElementTheme theme,
        string title, Action returnToDock)
    {
        _returnToDock = returnToDock;
        _retainedAppWindow = AppWindow;
        _owner = App.Window;
        try
        {
            InitializePresentation(presentation, theme, title);
        }
        catch
        {
            ReleasePresentation();
            _retainedAppWindow.Changed -= Window_Changed;
            if (_owner is not null) _owner.Closed -= Owner_Closed;
            Closed -= Window_Closed;
            try { Close(); }
            catch (Exception cleanupError) { Program.Log($"Preview window cleanup failed: {cleanupError.Message}"); }
            throw;
        }
    }

    private void InitializePresentation(FrameworkElement presentation, ElementTheme theme, string title)
    {
        Title = $"{AppRuntimeMode.ProductName} — {title}";
        _retainedAppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory,
            AppRuntimeMode.AppIconRelativePath));
        if (_retainedAppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = false;
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
        }
        if (_owner is not null)
        {
            SetWindowLongPtr(WinRT.Interop.WindowNative.GetWindowHandle(this), -8, App.WindowHandle);
        }
        var root = new Grid
        {
            RequestedTheme = theme,
            Background = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["SolidBackgroundFillColorBaseBrush"],
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var bar = new TitleBar { Title = AppRuntimeMode.ProductName, Subtitle = "LIVE Preview" };
        root.Children.Add(bar);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(bar);
        _host.Padding = new Thickness(20);
        _host.Children.Add(presentation);
        var scroll = new ScrollViewer { Content = _host, HorizontalContentAlignment = HorizontalAlignment.Stretch };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);
        Content = root;
        RestoreVisiblePlacement();
        _retainedAppWindow.Changed += Window_Changed;
        Closed += Window_Closed;
        if (_owner is not null) _owner.Closed += Owner_Closed;
    }

    public void ReleasePresentation() => _host.Children.Clear();

    private void RestoreVisiblePlacement()
    {
        var display = DisplayArea.GetFromWindowId(_owner?.AppWindow.Id ?? AppWindow.Id, DisplayAreaFallback.Primary);
        var work = display.WorkArea;
        var bounds = _lastPlacement ?? new RectInt32(work.X + 60, work.Y + 60,
            Math.Min(1080, work.Width - 120), Math.Min(860, work.Height - 120));
        // Placement is validated against the current display; unplugged displays
        // cannot strand the preview outside the available work area.
        bounds.Width = Math.Min(Math.Max(600, bounds.Width), work.Width);
        bounds.Height = Math.Min(Math.Max(440, bounds.Height), work.Height);
        bounds.X = Math.Clamp(bounds.X, work.X, work.X + work.Width - bounds.Width);
        bounds.Y = Math.Clamp(bounds.Y, work.Y, work.Y + work.Height - bounds.Height);
        _retainedAppWindow.MoveAndResize(bounds);
    }

    private void Window_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange)
        {
            var position = sender.Position;
            var savedSize = sender.Size;
            if (sender.Presenter is not OverlappedPresenter { State: OverlappedPresenterState.Minimized })
                _lastPlacement = new RectInt32(position.X, position.Y, savedSize.Width, savedSize.Height);
        }
        if (!args.DidSizeChange || _enforcingMinimum) return;
        var size = sender.Size;
        if (size.Width >= 600 && size.Height >= 440) return;
        _enforcingMinimum = true;
        sender.Resize(new SizeInt32(Math.Max(600, size.Width), Math.Max(440, size.Height)));
        _enforcingMinimum = false;
    }

    private void Owner_Closed(object sender, WindowEventArgs args)
    {
        if (!_closed) Close();
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        if (_closed) return;
        _closed = true;
        _retainedAppWindow.Changed -= Window_Changed;
        if (_owner is not null) _owner.Closed -= Owner_Closed;
        Closed -= Window_Closed;
        ReleasePresentation();
        _returnToDock();
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);
}
