using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Owned, resizable surface for the app-wide LIVE Preview session. Moving the
/// preview here does not clone its scene, camera, or renderer.
/// </summary>
internal sealed class LivePreviewWindow : Window
{
    private const int OwnerWindowIndex = -8;
    private const int MinimumWindowWidth = 760;
    private const int MinimumWindowHeight = 520;
    private readonly LivePreviewSession _session;
    private readonly Grid _previewHost = new();
    private readonly Grid _twoDimensionalHost = new();
    private readonly ComboBox _modeComboBox = new();
    private readonly TextBlock _descriptionText = new();
    private readonly FrameworkElement _crossSectionCard;
    private readonly FrameworkElement _sideProfileCard;
    private LivePreview3DHost? _threeDHost;
    private bool _isSynchronizingMode;
    private bool _isEnforcingMinimumSize;

    public LivePreviewWindow(
        LivePreviewSession session,
        ElementTheme requestedTheme,
        FrameworkElement crossSectionCard,
        FrameworkElement sideProfileCard)
    {
        _session = session;
        _crossSectionCard = crossSectionCard;
        _sideProfileCard = sideProfileCard;
        Title = $"{AppRuntimeMode.ProductName} - LIVE Preview";
        AppWindow.SetIcon(System.IO.Path.Combine(
            AppContext.BaseDirectory,
            AppRuntimeMode.AppIconRelativePath));
        ConfigurePresenter();
        if (App.Window is not null)
        {
            SetWindowLongPtr(
                WinRT.Interop.WindowNative.GetWindowHandle(this),
                OwnerWindowIndex,
                App.WindowHandle);
        }

        ExtendsContentIntoTitleBar = true;
        Content = BuildContent(requestedTheme);
        _session.ModeChanged += Session_ModeChanged;
        _session.SceneChanged += Session_SceneChanged;
        AppWindow.Changed += AppWindow_Changed;
        Closed += LivePreviewWindow_Closed;
        SizeAndCentre();
        RenderCurrentMode();
    }

    public event EventHandler? RedockRequested;

    private FrameworkElement BuildContent(ElementTheme requestedTheme)
    {
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
            Subtitle = "Shared LIVE Preview",
            IconSource = new ImageIconSource
            {
                ImageSource = new BitmapImage(
                    new Uri("ms-appx:///Assets/AppIcon.ico")),
            },
        };
        root.Children.Add(titleBar);
        SetTitleBar(titleBar);

        var header = new Grid
        {
            Margin = new Thickness(24, 18, 24, 14),
            ColumnSpacing = 14,
        };
        header.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        header.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = GridLength.Auto,
        });
        var heading = new StackPanel { Spacing = 3 };
        heading.Children.Add(new TextBlock
        {
            Text = "LIVE cable preview",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        heading.Children.Add(new TextBlock
        {
            Text = "This window follows the active costing and can move to any display.",
            Foreground = ResourceBrush("TextFillColorSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
        });
        header.Children.Add(heading);
        _modeComboBox.MinWidth = 230;
        _modeComboBox.Header = "Preview type";
        _modeComboBox.Items.Add("Simple");
        _modeComboBox.Items.Add("Detailed strands");
        _modeComboBox.Items.Add("Interactive 3D");
        _modeComboBox.SelectedIndex = (int)_session.Mode;
        _modeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;
        Grid.SetColumn(_modeComboBox, 1);
        header.Children.Add(_modeComboBox);
        Grid.SetRow(header, 1);
        root.Children.Add(header);

        _previewHost.Margin = new Thickness(24, 0, 24, 16);
        Grid.SetRow(_previewHost, 2);
        root.Children.Add(_previewHost);

        var footer = new Grid
        {
            Padding = new Thickness(24, 12, 24, 16),
            ColumnSpacing = 12,
            Background = ResourceBrush("ControlFillColorDefaultBrush"),
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        footer.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = GridLength.Auto,
        });
        _descriptionText.Text = _session.Scene.Description;
        _descriptionText.VerticalAlignment = VerticalAlignment.Center;
        _descriptionText.Foreground = ResourceBrush(
            "TextFillColorSecondaryBrush");
        _descriptionText.TextWrapping = TextWrapping.Wrap;
        footer.Children.Add(_descriptionText);
        var redockButton = new Button
        {
            Content = "Return to dock",
            MinWidth = 140,
        };
        redockButton.Click += (_, _) =>
            RedockRequested?.Invoke(this, EventArgs.Empty);
        Grid.SetColumn(redockButton, 1);
        footer.Children.Add(redockButton);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        return root;
    }

    private void ModeComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isSynchronizingMode || _modeComboBox.SelectedIndex < 0)
        {
            return;
        }

        _session.Mode = (LivePreviewMode)_modeComboBox.SelectedIndex;
    }

    private void Session_ModeChanged(object? sender, EventArgs e)
    {
        _isSynchronizingMode = true;
        _modeComboBox.SelectedIndex = (int)_session.Mode;
        _isSynchronizingMode = false;
        RenderCurrentMode();
    }

    private void Session_SceneChanged(object? sender, EventArgs e)
    {
        _descriptionText.Text = _session.Scene.Description;
        if (_session.Mode != LivePreviewMode.Interactive3D)
        {
            RenderCurrentMode();
        }
    }

    private void RenderCurrentMode()
    {
        ReleaseThreeDHost();
        _previewHost.Children.Clear();
        if (_session.Mode == LivePreviewMode.Interactive3D)
        {
            _threeDHost = new LivePreview3DHost(_session);
            _previewHost.Children.Add(_threeDHost);
            return;
        }

        EnsureSharedTwoDimensionalCards();
        _previewHost.Children.Add(_twoDimensionalHost);
    }

    private void EnsureSharedTwoDimensionalCards()
    {
        if (_twoDimensionalHost.Children.Count > 0)
        {
            return;
        }

        _twoDimensionalHost.ColumnSpacing = 16;
        _twoDimensionalHost.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        _twoDimensionalHost.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        _twoDimensionalHost.Children.Add(_crossSectionCard);
        Grid.SetColumn(_sideProfileCard, 1);
        _twoDimensionalHost.Children.Add(_sideProfileCard);
    }

    internal void ReleaseSharedTwoDimensionalCards()
    {
        _twoDimensionalHost.Children.Remove(_crossSectionCard);
        _twoDimensionalHost.Children.Remove(_sideProfileCard);
        Grid.SetColumn(_sideProfileCard, 0);
    }

    private void ReleaseThreeDHost()
    {
        if (_threeDHost is null)
        {
            return;
        }

        _previewHost.Children.Remove(_threeDHost);
        _threeDHost.Dispose();
        _threeDHost = null;
    }

    private void LivePreviewWindow_Closed(object sender, WindowEventArgs args)
    {
        ReleaseThreeDHost();
        _session.ModeChanged -= Session_ModeChanged;
        _session.SceneChanged -= Session_SceneChanged;
        AppWindow.Changed -= AppWindow_Changed;
        _modeComboBox.SelectionChanged -= ModeComboBox_SelectionChanged;
        Closed -= LivePreviewWindow_Closed;
    }

    private void AppWindow_Changed(
        AppWindow sender,
        AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange || _isEnforcingMinimumSize)
        {
            return;
        }

        var current = sender.Size;
        var width = Math.Max(MinimumWindowWidth, current.Width);
        var height = Math.Max(MinimumWindowHeight, current.Height);
        if (width == current.Width && height == current.Height)
        {
            return;
        }

        _isEnforcingMinimumSize = true;
        sender.Resize(new SizeInt32(width, height));
        _isEnforcingMinimumSize = false;
    }

    private void ConfigurePresenter()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.IsAlwaysOnTop = false;
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
        var width = Math.Min(1320, Math.Max(820, workArea.Width - 180));
        var height = Math.Min(900, Math.Max(600, workArea.Height - 140));
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
