using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace ATAG.Costing.WinUI;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
#if ATAG_PUBLIC_REVIEW
    private static readonly string PlacementPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Costing App",
        "Public Review",
        "window-placement.json");
#else
    private static readonly string PlacementPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ATAG Design Ltd",
        "ATAG Costing",
        "window-placement.json");
#endif

    private RectInt32 _lastRestoredBounds;

    public MainWindow()
    {
        InitializeComponent();

        Title = AppRuntimeMode.ProductName;
        AppTitleBar.Title = AppRuntimeMode.ProductName;
        Program.Log($"Runtime product name: {AppRuntimeMode.ProductName}.");
        if (AppRuntimeMode.IsPublicReview)
        {
            AppTitleBar.Subtitle =
                "Interface-only edition · no private data or database links";
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon(Path.Combine(
            AppContext.BaseDirectory,
            AppRuntimeMode.AppIconRelativePath));

        RestoreOrSizeWindow();
        AppWindow.Changed += AppWindow_Changed;
        Closed += (_, _) => SaveWindowPlacement();
        RootFrame.Navigate(typeof(MainPage));
    }

    public void ApplyVisualStyle(
        ElementTheme requestedTheme,
        int backdropIndex)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = requestedTheme;
        }

        SystemBackdrop = backdropIndex == 1
            ? new DesktopAcrylicBackdrop()
            : new MicaBackdrop();
    }

    private void RestoreOrSizeWindow()
    {
        var saved = TryLoadWindowPlacement();
        // Some secondary displays and scaled work areas are shorter than the
        // normal desktop minimum. Accept any practical window saved by WinUI;
        // the work-area clamp below still protects against corrupt values.
        if (saved is not null && saved.Width >= 480 && saved.Height >= 320)
        {
            var centre = new PointInt32(
                saved.X + (saved.Width / 2),
                saved.Y + (saved.Height / 2));
            var displayArea = DisplayArea.GetFromPoint(
                centre,
                DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;
            var width = Math.Min(saved.Width, workArea.Width);
            var height = Math.Min(saved.Height, workArea.Height);
            var x = Math.Clamp(saved.X, workArea.X, workArea.X + workArea.Width - width);
            var y = Math.Clamp(saved.Y, workArea.Y, workArea.Y + workArea.Height - height);

            _lastRestoredBounds = new RectInt32(x, y, width, height);
            AppWindow.MoveAndResize(_lastRestoredBounds);
            if (saved.IsMaximized && AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            return;
        }

        SizeAndCentreWindow();
    }

    private void SizeAndCentreWindow()
    {
        var displayArea = DisplayArea.GetFromWindowId(
            AppWindow.Id,
            DisplayAreaFallback.Primary);

        var workArea = displayArea.WorkArea;
        var width = Math.Min(1320, Math.Max(980, workArea.Width - 120));
        var height = Math.Min(860, Math.Max(700, workArea.Height - 120));
        var x = workArea.X + ((workArea.Width - width) / 2);
        var y = workArea.Y + ((workArea.Height - height) / 2);

        _lastRestoredBounds = new RectInt32(x, y, width, height);
        AppWindow.MoveAndResize(_lastRestoredBounds);
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if ((!args.DidPositionChange && !args.DidSizeChange && !args.DidPresenterChange) ||
            sender.Presenter is not OverlappedPresenter presenter ||
            presenter.State != OverlappedPresenterState.Restored)
        {
            return;
        }

        _lastRestoredBounds = new RectInt32(
            sender.Position.X,
            sender.Position.Y,
            sender.Size.Width,
            sender.Size.Height);
    }

    private void SaveWindowPlacement()
    {
        try
        {
            if (_lastRestoredBounds.Width <= 0 || _lastRestoredBounds.Height <= 0)
            {
                return;
            }

            var directory = Path.GetDirectoryName(PlacementPath)!;
            Directory.CreateDirectory(directory);
            var isMaximized = AppWindow.Presenter is OverlappedPresenter presenter &&
                presenter.State == OverlappedPresenterState.Maximized;
            var savedBounds = _lastRestoredBounds;
            if (isMaximized)
            {
                // A maximised window can be moved to another monitor without
                // ever entering Restored state. Keep its restored size, but
                // relocate that rectangle onto the display it was closed on.
                var workArea = DisplayArea.GetFromWindowId(
                    AppWindow.Id,
                    DisplayAreaFallback.Primary).WorkArea;
                var centreX = savedBounds.X + (savedBounds.Width / 2);
                var centreY = savedBounds.Y + (savedBounds.Height / 2);
                var isOnCurrentDisplay =
                    centreX >= workArea.X &&
                    centreX < workArea.X + workArea.Width &&
                    centreY >= workArea.Y &&
                    centreY < workArea.Y + workArea.Height;
                if (!isOnCurrentDisplay)
                {
                    var width = Math.Min(savedBounds.Width, workArea.Width);
                    var height = Math.Min(savedBounds.Height, workArea.Height);
                    savedBounds = new RectInt32(
                        workArea.X + ((workArea.Width - width) / 2),
                        workArea.Y + ((workArea.Height - height) / 2),
                        width,
                        height);
                }
            }
            var placement = new WindowPlacement(
                savedBounds.X,
                savedBounds.Y,
                savedBounds.Width,
                savedBounds.Height,
                isMaximized);
            var temporaryPath = PlacementPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(placement));
            File.Move(temporaryPath, PlacementPath, overwrite: true);
        }
        catch (IOException)
        {
            // Window placement is a convenience; launch must never depend on it.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep the app usable if the local profile is temporarily read-only.
        }
    }

    private static WindowPlacement? TryLoadWindowPlacement()
    {
        try
        {
            return File.Exists(PlacementPath)
                ? JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(PlacementPath))
                : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record WindowPlacement(
        int X,
        int Y,
        int Width,
        int Height,
        bool IsMaximized);
}
