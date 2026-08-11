using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Owned, resizable reader for cumulative update notes. It remains above the
/// main app, not unrelated Windows applications.
/// </summary>
internal sealed class UpdateReleaseNotesWindow : Window
{
    private const int OwnerWindowIndex = -8;
    private readonly Button _fullScreenButton;
    private bool _isFullScreen;

    public UpdateReleaseNotesWindow(
        string version,
        string releaseNotes,
        ElementTheme requestedTheme)
    {
        Title = $"{AppRuntimeMode.ProductName} - Update {version}";
        AppWindow.SetIcon(Path.Combine(
            AppContext.BaseDirectory,
            AppRuntimeMode.AppIconRelativePath));
        ConfigureOverlappedPresenter();

        if (App.Window is not null)
        {
            SetWindowLongPtr(
                WinRT.Interop.WindowNative.GetWindowHandle(this),
                OwnerWindowIndex,
                App.WindowHandle);
        }

        ExtendsContentIntoTitleBar = true;
        var root = new Grid
        {
            RequestedTheme = requestedTheme,
            Background = ResourceBrush("SolidBackgroundFillColorBaseBrush"),
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star),
        });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleBar = new TitleBar
        {
            Title = AppRuntimeMode.ProductName,
            Subtitle = "Cumulative update changelog",
            IconSource = new ImageIconSource
            {
                ImageSource = new BitmapImage(
                    new Uri("ms-appx:///Assets/AppIcon.ico")),
            },
        };
        root.Children.Add(titleBar);
        SetTitleBar(titleBar);

        var heading = new StackPanel
        {
            Margin = new Thickness(28, 22, 28, 16),
            Spacing = 5,
        };
        heading.Children.Add(new TextBlock
        {
            Text = $"Changes available through version {version}",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Every applicable release since the installed version is shown below.",
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetRow(heading, 1);
        root.Children.Add(heading);

        var scrollViewer = new ScrollViewer
        {
            Padding = new Thickness(28, 0, 28, 22),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = UpdateReleaseNotesPresenter.Create(releaseNotes),
        };
        Grid.SetRow(scrollViewer, 2);
        root.Children.Add(scrollViewer);

        var footer = new Grid
        {
            Padding = new Thickness(28, 14, 28, 18),
            ColumnSpacing = 10,
            Background = ResourceBrush("ControlFillColorDefaultBrush"),
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _fullScreenButton = new Button
        {
            Content = "Full screen",
            MinWidth = 130,
        };
        _fullScreenButton.Click += (_, _) => ToggleFullScreen();
        Grid.SetColumn(_fullScreenButton, 1);
        footer.Children.Add(_fullScreenButton);
        var closeButton = new Button
        {
            Content = "Close",
            MinWidth = 110,
        };
        closeButton.Click += (_, _) => Close();
        Grid.SetColumn(closeButton, 2);
        footer.Children.Add(closeButton);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        Content = root;
        SizeAndCentre();
    }

    public void ShowFullScreen()
    {
        Activate();
        if (!_isFullScreen)
        {
            ToggleFullScreen();
        }
    }

    private void ToggleFullScreen()
    {
        if (_isFullScreen)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            ConfigureOverlappedPresenter();
            SizeAndCentre();
            _isFullScreen = false;
            _fullScreenButton.Content = "Full screen";
            return;
        }

        AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        _isFullScreen = true;
        _fullScreenButton.Content = "Exit full screen";
    }

    private void ConfigureOverlappedPresenter()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
        }
    }

    private void SizeAndCentre()
    {
        var anchorWindowId = App.Window is null
            ? AppWindow.Id
            : App.Window.AppWindow.Id;
        var displayArea = DisplayArea.GetFromWindowId(
            anchorWindowId,
            DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var width = Math.Min(1100, Math.Max(720, workArea.Width - 160));
        var height = Math.Min(820, Math.Max(520, workArea.Height - 120));
        width = Math.Min(width, workArea.Width);
        height = Math.Min(height, workArea.Height);
        AppWindow.MoveAndResize(new RectInt32(
            workArea.X + ((workArea.Width - width) / 2),
            workArea.Y + ((workArea.Height - height) / 2),
            width,
            height));
    }

    private static Brush ResourceBrush(string key) =>
        (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(
        nint windowHandle,
        int index,
        nint newValue);
}
