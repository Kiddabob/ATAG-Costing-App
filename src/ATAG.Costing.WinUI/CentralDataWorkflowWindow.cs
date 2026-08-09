using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Movable, resizable host for the central-data setup, Navigator, and transform
/// workflow. The window only supplies layout and confirmation; the existing
/// import services continue to own discovery, validation, and atomic saves.
/// </summary>
internal sealed class CentralDataWorkflowWindow : Window
{
    private const int OwnerWindowIndex = -8;
    private readonly TaskCompletionSource<ContentDialogResult> _completion = new();
    private readonly Button _primaryButton;
    private bool _completed;

    public CentralDataWorkflowWindow(
        string title,
        FrameworkElement content,
        string primaryButtonText,
        string closeButtonText,
        ElementTheme requestedTheme,
        CentralDataWorkflowWindowSize size = CentralDataWorkflowWindowSize.Workspace,
        bool showPrimaryButton = true)
    {
        Title = $"{AppRuntimeMode.ProductName} - {title}";
        AppWindow.SetIcon("Assets/AppIcon.ico");
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
        }

        // A native owner keeps the workflow above the ATAG main window without
        // making it topmost over unrelated applications.
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
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var appTitleBar = new TitleBar
        {
            Title = AppRuntimeMode.ProductName,
            Subtitle = title,
            IconSource = new ImageIconSource
            {
                ImageSource = new BitmapImage(
                    new Uri("ms-appx:///Assets/AppIcon.ico")),
            },
        };
        root.Children.Add(appTitleBar);
        SetTitleBar(appTitleBar);

        var heading = new TextBlock
        {
            Text = title,
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(24, 20, 24, 16),
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(heading, 1);
        root.Children.Add(heading);

        var hostedContent = size == CentralDataWorkflowWindowSize.Compact
            ? new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            }
            : content;
        var contentHost = new Border
        {
            Child = hostedContent,
            Padding = new Thickness(24, 0, 24, 20),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Grid.SetRow(contentHost, 2);
        root.Children.Add(contentHost);

        var footer = new Grid
        {
            Padding = new Thickness(24, 14, 24, 18),
            ColumnSpacing = 12,
            Background = ResourceBrush("ControlFillColorDefaultBrush"),
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var closeButton = new Button
        {
            Content = closeButtonText,
            MinWidth = 150,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        closeButton.Click += (_, _) => Complete(ContentDialogResult.None);
        Grid.SetColumn(closeButton, 1);
        footer.Children.Add(closeButton);

        _primaryButton = new Button
        {
            Content = primaryButtonText,
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Right,
            Visibility = showPrimaryButton ? Visibility.Visible : Visibility.Collapsed,
            Style = ResourceStyle("AccentButtonStyle"),
        };
        _primaryButton.Click += (_, _) => Complete(ContentDialogResult.Primary);
        Grid.SetColumn(_primaryButton, 2);
        footer.Children.Add(_primaryButton);

        Grid.SetRow(footer, 3);
        root.Children.Add(footer);
        Content = root;

        Closed += (_, _) =>
        {
            if (!_completed)
            {
                _completed = true;
                _completion.TrySetResult(ContentDialogResult.None);
            }
        };

        SizeAndCentre(size);
    }

    public bool IsPrimaryButtonEnabled
    {
        get => _primaryButton.IsEnabled;
        set => _primaryButton.IsEnabled = value;
    }

    public async Task<ContentDialogResult> ShowAsync()
    {
        Activate();
        return await _completion.Task;
    }

    private void Complete(ContentDialogResult result)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _completion.TrySetResult(result);
        Close();
    }

    private void SizeAndCentre(CentralDataWorkflowWindowSize size)
    {
        // Database workflow windows follow the ATAG app instead of defaulting
        // to the operating system's primary display.
        var anchorWindowId = App.Window is null
            ? AppWindow.Id
            : App.Window.AppWindow.Id;
        var displayArea = DisplayArea.GetFromWindowId(
            anchorWindowId,
            DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        var horizontalMargin = size switch
        {
            CentralDataWorkflowWindowSize.Workspace => 72,
            CentralDataWorkflowWindowSize.Compact => 180,
            _ => 260,
        };
        var verticalMargin = size switch
        {
            CentralDataWorkflowWindowSize.Workspace => 72,
            CentralDataWorkflowWindowSize.Compact => 150,
            _ => 260,
        };
        var preferredWidth = size switch
        {
            CentralDataWorkflowWindowSize.Workspace => 1600,
            CentralDataWorkflowWindowSize.Compact => 980,
            _ => 820,
        };
        var preferredHeight = size switch
        {
            CentralDataWorkflowWindowSize.Workspace => 900,
            CentralDataWorkflowWindowSize.Compact => 700,
            _ => 460,
        };
        var minimumWidth = size switch
        {
            CentralDataWorkflowWindowSize.Workspace => 980,
            CentralDataWorkflowWindowSize.Compact => 720,
            _ => 600,
        };
        var minimumHeight = size switch
        {
            CentralDataWorkflowWindowSize.Workspace => 620,
            CentralDataWorkflowWindowSize.Compact => 480,
            _ => 320,
        };
        var width = Math.Min(
            preferredWidth,
            Math.Max(minimumWidth, workArea.Width - horizontalMargin));
        var height = Math.Min(
            preferredHeight,
            Math.Max(minimumHeight, workArea.Height - verticalMargin));
        width = Math.Min(width, workArea.Width);
        height = Math.Min(height, workArea.Height);
        var x = workArea.X + ((workArea.Width - width) / 2);
        var y = workArea.Y + ((workArea.Height - height) / 2);

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private static Brush ResourceBrush(string key) =>
        (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];

    private static Style ResourceStyle(string key) =>
        (Style)Microsoft.UI.Xaml.Application.Current.Resources[key];

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(
        nint windowHandle,
        int index,
        nint newValue);
}

internal enum CentralDataWorkflowWindowSize
{
    Compact,
    Workspace,
    Message,
}
