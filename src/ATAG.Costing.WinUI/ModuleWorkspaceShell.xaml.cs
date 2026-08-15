using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Shared feature-module shell. It keeps module actions/status above the
/// scrolling editor and provides the same optional, responsive LIVE Preview
/// slot for costing, production and engineering modules.
/// </summary>
public sealed partial class ModuleWorkspaceShell : UserControl
{
    private const double RightDockThreshold = 1120d;
    private const double MinimumWorkspaceWidth = 620d;
    private const double MinimumPreviewWidth = 360d;
    private const double MaximumPreviewWidth = 760d;

    private bool _isDockedRight = true;
    private bool _isResizeActive;
    private uint _resizePointerId;
    private double _resizeStartX;
    private double _resizeStartWidth;
    private double _lastRightDockWidth = 480d;
    private Brush? _idleResizeBrush;

    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(
            nameof(HeaderContent),
            typeof(UIElement),
            typeof(ModuleWorkspaceShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty WorkspaceContentProperty =
        DependencyProperty.Register(
            nameof(WorkspaceContent),
            typeof(UIElement),
            typeof(ModuleWorkspaceShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PreviewGuidanceContentProperty =
        DependencyProperty.Register(
            nameof(PreviewGuidanceContent),
            typeof(UIElement),
            typeof(ModuleWorkspaceShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PreviewContentProperty =
        DependencyProperty.Register(
            nameof(PreviewContent),
            typeof(UIElement),
            typeof(ModuleWorkspaceShell),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PreviewTitleProperty =
        DependencyProperty.Register(
            nameof(PreviewTitle),
            typeof(string),
            typeof(ModuleWorkspaceShell),
            new PropertyMetadata("LIVE Preview"));

    public static readonly DependencyProperty PreviewSubtitleProperty =
        DependencyProperty.Register(
            nameof(PreviewSubtitle),
            typeof(string),
            typeof(ModuleWorkspaceShell),
            new PropertyMetadata("Inspect the current module visually."));

    public static readonly DependencyProperty PreviewOffMessageProperty =
        DependencyProperty.Register(
            nameof(PreviewOffMessage),
            typeof(string),
            typeof(ModuleWorkspaceShell),
            new PropertyMetadata(
                "Turn on LIVE Preview when a visual check is useful. " +
                "The module calculations remain available while it is off."));

    public static readonly DependencyProperty IsPreviewEnabledProperty =
        DependencyProperty.Register(
            nameof(IsPreviewEnabled),
            typeof(bool),
            typeof(ModuleWorkspaceShell),
            new PropertyMetadata(false, OnIsPreviewEnabledChanged));

    public ModuleWorkspaceShell()
    {
        InitializeComponent();
        _idleResizeBrush = ResizeIndicator.Background;
        Loaded += (_, _) =>
        {
            PreviewToggle.IsOn = IsPreviewEnabled;
            UpdatePreviewVisibility();
            UpdateDockLayout(ActualWidth, ActualHeight);
        };
    }

    public UIElement? HeaderContent
    {
        get => (UIElement?)GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public UIElement? WorkspaceContent
    {
        get => (UIElement?)GetValue(WorkspaceContentProperty);
        set => SetValue(WorkspaceContentProperty, value);
    }

    public UIElement? PreviewGuidanceContent
    {
        get => (UIElement?)GetValue(PreviewGuidanceContentProperty);
        set => SetValue(PreviewGuidanceContentProperty, value);
    }

    public UIElement? PreviewContent
    {
        get => (UIElement?)GetValue(PreviewContentProperty);
        set => SetValue(PreviewContentProperty, value);
    }

    public string PreviewTitle
    {
        get => (string)GetValue(PreviewTitleProperty);
        set => SetValue(PreviewTitleProperty, value);
    }

    public string PreviewSubtitle
    {
        get => (string)GetValue(PreviewSubtitleProperty);
        set => SetValue(PreviewSubtitleProperty, value);
    }

    public string PreviewOffMessage
    {
        get => (string)GetValue(PreviewOffMessageProperty);
        set => SetValue(PreviewOffMessageProperty, value);
    }

    public bool IsPreviewEnabled
    {
        get => (bool)GetValue(IsPreviewEnabledProperty);
        set => SetValue(IsPreviewEnabledProperty, value);
    }

    private static void OnIsPreviewEnabledChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not ModuleWorkspaceShell shell ||
            shell.PreviewToggle is null)
        {
            return;
        }

        var enabled = eventArgs.NewValue is true;
        if (shell.PreviewToggle.IsOn != enabled)
        {
            shell.PreviewToggle.IsOn = enabled;
        }

        shell.UpdatePreviewVisibility();
        shell.UpdateDockLayout(shell.ActualWidth, shell.ActualHeight);
    }

    private void PreviewToggle_Toggled(object sender, RoutedEventArgs e)
    {
        IsPreviewEnabled = PreviewToggle.IsOn;
        UpdatePreviewVisibility();
        UpdateDockLayout(ActualWidth, ActualHeight);
    }

    private void UpdatePreviewVisibility()
    {
        if (PreviewContentPresenter is null || PreviewOffInfoBar is null)
        {
            return;
        }

        PreviewContentPresenter.Visibility = IsPreviewEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
        PreviewContentPresenter.Content = IsPreviewEnabled
            ? PreviewContent
            : null;
        PreviewOffInfoBar.Visibility = IsPreviewEnabled
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateDockLayout(e.NewSize.Width, e.NewSize.Height);

    private void UpdateDockLayout(double availableWidth, double availableHeight)
    {
        if (availableWidth <= 0d || AdaptiveWorkspace is null)
        {
            return;
        }

        _isDockedRight = availableWidth >= RightDockThreshold;
        if (_isDockedRight)
        {
            BottomPreviewRow.Height = new GridLength(0d);
            SplitterColumn.Width = new GridLength(10d);
            var maximumWidth = Math.Max(
                MinimumPreviewWidth,
                Math.Min(
                    MaximumPreviewWidth,
                    availableWidth - MinimumWorkspaceWidth - 10d));
            _lastRightDockWidth = Math.Clamp(
                _lastRightDockWidth,
                MinimumPreviewWidth,
                maximumWidth);
            PreviewColumn.Width = new GridLength(_lastRightDockWidth);
            Grid.SetRow(PreviewPanel, 0);
            Grid.SetColumn(PreviewPanel, 2);
            Grid.SetColumnSpan(PreviewPanel, 1);
            PreviewPanel.Margin = new Thickness(0, 12, 24, 48);
            ResizeHandle.Visibility = Visibility.Visible;
            DockModeText.Text = "Resizable right-hand dock · drag the divider";
            return;
        }

        EndResizeWithoutPointer();
        SplitterColumn.Width = new GridLength(0d);
        PreviewColumn.Width = new GridLength(0d);
        BottomPreviewRow.Height = new GridLength(
            IsPreviewEnabled
                ? Math.Clamp(availableHeight * 0.46d, 280d, 480d)
                : 176d);
        Grid.SetRow(PreviewPanel, 1);
        Grid.SetColumn(PreviewPanel, 0);
        Grid.SetColumnSpan(PreviewPanel, 3);
        PreviewPanel.Margin = new Thickness(24, 8, 24, 24);
        ResizeHandle.Visibility = Visibility.Collapsed;
        DockModeText.Text = "Bottom dock · compact window";
    }

    private void ResizeHandle_PointerPressed(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isDockedRight || sender is not UIElement resizeHandle)
        {
            return;
        }

        var point = e.GetCurrentPoint(AdaptiveWorkspace);
        _isResizeActive = true;
        _resizePointerId = e.Pointer.PointerId;
        _resizeStartX = point.Position.X;
        _resizeStartWidth = PreviewColumn.ActualWidth;
        SetResizeAffordance(active: true);
        resizeHandle.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void ResizeHandle_PointerMoved(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isDockedRight ||
            !_isResizeActive ||
            e.Pointer.PointerId != _resizePointerId)
        {
            return;
        }

        var point = e.GetCurrentPoint(AdaptiveWorkspace);
        var maximumWidth = Math.Max(
            MinimumPreviewWidth,
            Math.Min(
                MaximumPreviewWidth,
                AdaptiveWorkspace.ActualWidth - MinimumWorkspaceWidth - 10d));
        _lastRightDockWidth = Math.Clamp(
            _resizeStartWidth - (point.Position.X - _resizeStartX),
            MinimumPreviewWidth,
            maximumWidth);
        PreviewColumn.Width = new GridLength(_lastRightDockWidth);
        e.Handled = true;
    }

    private void ResizeHandle_PointerReleased(
        object sender,
        PointerRoutedEventArgs e) => EndResize(sender, e);

    private void ResizeHandle_PointerCanceled(
        object sender,
        PointerRoutedEventArgs e) => EndResize(sender, e);

    private void EndResize(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizeActive || e.Pointer.PointerId != _resizePointerId)
        {
            return;
        }

        if (sender is UIElement resizeHandle)
        {
            resizeHandle.ReleasePointerCapture(e.Pointer);
        }

        _isResizeActive = false;
        SetResizeAffordance(active: false);
        e.Handled = true;
    }

    private void EndResizeWithoutPointer()
    {
        if (!_isResizeActive)
        {
            return;
        }

        ResizeHandle.ReleasePointerCaptures();
        _isResizeActive = false;
        SetResizeAffordance(active: false);
    }

    private void ResizeHandle_PointerEntered(
        object sender,
        PointerRoutedEventArgs e) => SetResizeAffordance(active: true);

    private void ResizeHandle_PointerExited(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isResizeActive)
        {
            SetResizeAffordance(active: false);
        }
    }

    private void SetResizeAffordance(bool active)
    {
        if (ResizeIndicator is null)
        {
            return;
        }

        ResizeIndicator.Width = active ? 4d : 2d;
        ResizeIndicator.Background = active
            ? (Brush)Microsoft.UI.Xaml.Application.Current.Resources["AccentFillColorDefaultBrush"]
            : _idleResizeBrush;
    }
}
