using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Application.Projects;
using ATAG.Costing.Application.Updates;
using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.Domain.Conductors;
using ATAG.Costing.Infrastructure.CentralData;
using ATAG.Costing.Infrastructure.Currency;
using ATAG.Costing.Infrastructure.Preferences;
using ATAG.Costing.Infrastructure.Projects;
using ATAG.Costing.Reporting.Quotations;
using ATAG.Costing.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;

namespace ATAG.Costing.WinUI;

/// <summary>
/// The main content page displayed inside the application window.
/// </summary>
public sealed partial class MainPage : Page
{
    private static readonly CentralDataArea[] RequiredCentralDataAreas =
    [
        CentralDataArea.Copper,
        CentralDataArea.Compounds,
        CentralDataArea.Masterbatch,
        CentralDataArea.Contacts,
        CentralDataArea.Operators,
    ];
    private const double PreviewRightDockThreshold = 1100d;
    private const double PreviewMinimumRightDockWidth = 380d;
    private const double PreviewMaximumRightDockWidth = 820d;
    private const double PreviewMinimumWorkspaceWidth = 520d;
    private static readonly double[] BundleTextureOffsets =
        [-0.24d, 0d, 0.24d];
    private readonly DispatcherTimer _centralDataRefreshTimer = new()
    {
        Interval = TimeSpan.FromSeconds(30),
    };
    private readonly ISingleCoreProjectRepository _projectRepository =
        new JsonSingleCoreProjectRepository();
    private readonly ISingleCoreProjectDocumentStore _portableDocumentStore =
        new JsonSingleCoreProjectDocumentStore();
    private readonly A4QuotationPdfGenerator _quotationPdfGenerator = new();
    private readonly IAppUpdateService _appUpdateService =
        new VelopackAppUpdateService();
    private readonly CentralDataService _centralDataService;
    private readonly IReadOnlyDictionary<CentralDataSourceKind, ICentralDataDatabaseNavigator> _databaseNavigators;
    private ResultWindow? _resultWindow;
    private bool _isPreviewDockedRight;
    private bool _isPreviewResizeActive;
    private uint _previewResizePointerId;
    private double _previewResizeStartX;
    private double _previewResizeStartWidth = 600d;
    private double _lastPreviewRightDockWidth = 600d;
    private AppUpdateRelease? _availableAppUpdate;
    private bool _isAppUpdateOperationActive;

    public MainPageViewModel ViewModel { get; }
    public SingleCoreCostingViewModel CostingViewModel { get; }

    public MainPage()
    {
        ICentralDataDatabaseNavigator[] databaseNavigators;
        if (AppRuntimeMode.IsPublicReview)
        {
            ViewModel = new MainPageViewModel(
                new PublicReviewAppPreferencesService());
            databaseNavigators = [];
            _centralDataService = new CentralDataService(
                new PublicReviewCentralDataStore(),
                Array.Empty<ICentralDataSourceReader>(),
                databaseNavigators);
            CostingViewModel = new SingleCoreCostingViewModel(
                _centralDataService);
        }
        else
        {
            ViewModel = new MainPageViewModel(
                new JsonAppPreferencesService());
            databaseNavigators =
            [
                new AccessCentralDataDatabaseNavigator(),
                new SqlServerCentralDataDatabaseNavigator(),
            ];
            _centralDataService = new CentralDataService(
                new JsonCentralDataStore(),
                Array.Empty<ICentralDataSourceReader>(),
                databaseNavigators);
            CostingViewModel = new SingleCoreCostingViewModel(
                _centralDataService,
                new EcbExchangeRateService());
        }

        _databaseNavigators = databaseNavigators.ToDictionary(
            navigator => navigator.Kind);
        InitializeComponent();
        InitializeAppUpdateDisplay();
        AppNavigation.PaneTitle = AppRuntimeMode.ProductName;
        ContractReviewPanel.Children.Remove(WorkingCentralDataTablesCard);
        var connectionOptionsIndex =
            LiveDataPanel.Children.IndexOf(CentralDataConnectionOptionsCard);
        LiveDataPanel.Children.Insert(
            connectionOptionsIndex + 1,
            WorkingCentralDataTablesCard);
        RefreshRetainedSourceTablesView();
        UpdateFirstRunSetupState();
        CostingWorkspaceView.DataContext = CostingViewModel;
        ContractReviewView.DataContext = CostingViewModel;
        MaterialDataView.DataContext = CostingViewModel;
        if (AppRuntimeMode.IsPublicReview)
        {
            ConfigurePublicReviewMode();
        }
        CostingViewModel.PropertyChanged +=
            CostingViewModel_PropertyChanged;
        _centralDataRefreshTimer.Tick += CentralDataRefreshTimer_Tick;
        Loaded += MainPage_Loaded;
        Unloaded += MainPage_Unloaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyVisualStyle();
        UpdateFirstRunSetupState();
        if (!AppRuntimeMode.IsPublicReview)
        {
            ViewModel.EvaluateStartupPrompt();
        }
        UpdateStorageSetupVisibility();
        UpdateCentralDataConnectionVisuals();
        if (AppRuntimeMode.IsPublicReview)
        {
            ConfigurePublicReviewMode();
        }
        if (!AppRuntimeMode.IsPublicReview)
        {
            _centralDataRefreshTimer.Start();
        }
        ShowSection("home");
        UpdatePreviewDockLayout(
            CostingEditorLayout.ActualWidth,
            CostingEditorLayout.ActualHeight);
        if (!AppRuntimeMode.IsPublicReview)
        {
            await CostingViewModel.RefreshExchangeRatesAsync();
        }
        if (!AppRuntimeMode.IsPublicReview &&
            ViewModel.AutomaticallyCheckForUpdates &&
            _appUpdateService.IsInstalled)
        {
            await CheckForAppUpdatesAsync(isAutomatic: true);
        }
    }

    private void InitializeAppUpdateDisplay()
    {
        UpdateCurrentVersionText.Text = _appUpdateService.IsInstalled
            ? $"Version {_appUpdateService.CurrentVersion} · installed"
            : $"Version {_appUpdateService.CurrentVersion} · development build";
        CheckForUpdatesButton.IsEnabled = _appUpdateService.IsInstalled;
        UpdateChannelComboBox.IsEnabled = _appUpdateService.IsInstalled;
        AppUpdateInfoBar.Title = _appUpdateService.IsInstalled
            ? "Ready to check"
            : "Installer-managed updates";
        AppUpdateInfoBar.Message = _appUpdateService.IsInstalled
            ? "Updates are downloaded anonymously from the public Costing App GitHub releases."
            : "Updates are available after this app has been installed with Costing App Setup.";
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e) =>
        await CheckForAppUpdatesAsync(isAutomatic: false);

    private async Task CheckForAppUpdatesAsync(bool isAutomatic)
    {
        if (_isAppUpdateOperationActive || !_appUpdateService.IsInstalled)
        {
            return;
        }

        _isAppUpdateOperationActive = true;
        CheckForUpdatesButton.IsEnabled = false;
        UpdateChannelComboBox.IsEnabled = false;
        AppUpdateInfoBar.IsOpen = true;
        AppUpdateInfoBar.Title = "Checking for updates";
        AppUpdateInfoBar.Message = "Contacting the public release feed...";
        AppUpdateInfoBar.Severity = InfoBarSeverity.Informational;
        AvailableUpdatePanel.Visibility = Visibility.Collapsed;
        AppUpdateProgressBar.IsIndeterminate = true;
        AppUpdateProgressBar.Visibility = Visibility.Visible;

        try
        {
            var channel = ViewModel.SelectedUpdateChannelIndex == 1
                ? AppUpdateChannel.Beta
                : AppUpdateChannel.Stable;
            _availableAppUpdate = await _appUpdateService
                .CheckForUpdatesAsync(channel);
            if (_availableAppUpdate is null)
            {
                AppUpdateInfoBar.Title = "Costing App is up to date";
                AppUpdateInfoBar.Message =
                    $"Version {_appUpdateService.CurrentVersion} is the newest {channel.ToString().ToLowerInvariant()} release.";
                AppUpdateInfoBar.Severity = InfoBarSeverity.Success;
                return;
            }

            var size = FormatFileSize(_availableAppUpdate.DownloadSizeBytes);
            AppUpdateInfoBar.Title =
                $"Version {_availableAppUpdate.Version} is available";
            AppUpdateInfoBar.Message =
                $"Download size: {size}. The package SHA-256 is checked before installation.";
            AppUpdateInfoBar.Severity = InfoBarSeverity.Success;
            AppUpdateReleaseNotesText.Text =
                string.IsNullOrWhiteSpace(_availableAppUpdate.ReleaseNotes)
                    ? "No release notes were supplied for this version."
                    : _availableAppUpdate.ReleaseNotes.Trim();
            AvailableUpdatePanel.Visibility = Visibility.Visible;
        }
        catch (Exception exception)
        {
            Program.Log($"Update check failed: {exception}");
            AppUpdateInfoBar.Title = isAutomatic
                ? "Automatic update check unavailable"
                : "Could not check for updates";
            AppUpdateInfoBar.Message =
                "The app remains available. Check the internet connection and try again later.";
            AppUpdateInfoBar.Severity = InfoBarSeverity.Warning;
        }
        finally
        {
            AppUpdateProgressBar.IsIndeterminate = false;
            AppUpdateProgressBar.Visibility = Visibility.Collapsed;
            CheckForUpdatesButton.IsEnabled = true;
            UpdateChannelComboBox.IsEnabled = true;
            _isAppUpdateOperationActive = false;
        }
    }

    private async void DownloadAndInstallUpdate_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isAppUpdateOperationActive || _availableAppUpdate is null)
        {
            return;
        }

        var confirmation = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Install version {_availableAppUpdate.Version}?",
            Content =
                "Save any working costing first. The update will be verified, installed, and Costing App will restart. Your LocalAppData settings, database links, cached tables, and costing files are outside the replaceable app folder.",
            PrimaryButtonText = "Download and restart",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        _isAppUpdateOperationActive = true;
        CheckForUpdatesButton.IsEnabled = false;
        DownloadAndInstallUpdateButton.IsEnabled = false;
        UpdateChannelComboBox.IsEnabled = false;
        AppUpdateProgressBar.IsIndeterminate = false;
        AppUpdateProgressBar.Value = 0;
        AppUpdateProgressBar.Visibility = Visibility.Visible;
        AppUpdateInfoBar.Title =
            $"Downloading version {_availableAppUpdate.Version}";
        AppUpdateInfoBar.Message = "0% downloaded";
        AppUpdateInfoBar.Severity = InfoBarSeverity.Informational;

        try
        {
            var progress = new Progress<int>(value =>
            {
                AppUpdateProgressBar.Value = value;
                AppUpdateInfoBar.Message = $"{value}% downloaded";
            });
            await _appUpdateService.DownloadUpdateAsync(progress);
            AppUpdateInfoBar.Title = "Update verified";
            AppUpdateInfoBar.Message = "Restarting Costing App to install it...";
            AppUpdateInfoBar.Severity = InfoBarSeverity.Success;
            _appUpdateService.ApplyUpdateAndRestart();
        }
        catch (Exception exception)
        {
            Program.Log($"Update download/apply failed: {exception}");
            AppUpdateInfoBar.Title = "Update was not installed";
            AppUpdateInfoBar.Message =
                "The current version is unchanged. Check the connection and try again.";
            AppUpdateInfoBar.Severity = InfoBarSeverity.Error;
            AppUpdateProgressBar.Visibility = Visibility.Collapsed;
            CheckForUpdatesButton.IsEnabled = true;
            DownloadAndInstallUpdateButton.IsEnabled = true;
            UpdateChannelComboBox.IsEnabled = true;
            _isAppUpdateOperationActive = false;
        }
    }

    private void DeferUpdate_Click(object sender, RoutedEventArgs e)
    {
        AvailableUpdatePanel.Visibility = Visibility.Collapsed;
        AppUpdateInfoBar.Title = "Update deferred";
        AppUpdateInfoBar.Message =
            "The current version will keep working. Check again when you are ready.";
        AppUpdateInfoBar.Severity = InfoBarSeverity.Informational;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0)
        {
            return "size not reported";
        }

        var megabytes = bytes / (1024d * 1024d);
        return $"{megabytes:0.0} MB";
    }

    private void ConfigurePublicReviewMode()
    {
        PublicReviewInfoBar.IsOpen = true;
        HomeStorageCard.Visibility = Visibility.Collapsed;
        OpenCostingButton.Visibility = Visibility.Collapsed;
        SaveCostingButton.Visibility = Visibility.Collapsed;
        RevisionActionsButton.Visibility = Visibility.Collapsed;
        CentralDataConnectionOptionsCard.Visibility = Visibility.Collapsed;
        CentralDataRefreshPanel.Visibility = Visibility.Collapsed;
        StorageAndFilesSettingsLabel.Visibility = Visibility.Collapsed;
        StorageAndFilesSettingsCard.Visibility = Visibility.Collapsed;
        StorageSafeFallbackInfoBar.Visibility = Visibility.Collapsed;

        CentralDataLinksButton.Flyout = null;
        CentralDataLinksButton.IsEnabled = false;
        CentralDataConnectionText.Text = "Public review · no data";
        CentralDataConnectionDot.Fill = ResourceBrush(
            "TextFillColorSecondaryBrush");

        CentralDataConnectionInfoBar.Title =
            "Public review · database links disabled";
        CentralDataConnectionInfoBar.Message =
            "This edition uses an empty, in-memory table set. It does not read retained organisation rows, saved database paths, or app preferences from this PC.";
        CentralDataConnectionInfoBar.Severity = InfoBarSeverity.Informational;
    }

    private static void ClearPublicReviewInputDefaults(
        DependencyObject element)
    {
        switch (element)
        {
            case NumberBox numberBox:
                numberBox.ClearValue(NumberBox.MinimumProperty);
                numberBox.ClearValue(NumberBox.MaximumProperty);
                numberBox.ClearValue(NumberBox.ValueProperty);
                numberBox.Value = double.NaN;
                numberBox.PlaceholderText = numberBox.IsEnabled
                    ? "Enter value"
                    : "No linked data";
                break;

            case TextBox textBox:
                textBox.ClearValue(TextBox.TextProperty);
                textBox.Text = string.Empty;
                break;

            case AutoSuggestBox autoSuggestBox:
                autoSuggestBox.ClearValue(AutoSuggestBox.TextProperty);
                autoSuggestBox.Text = string.Empty;
                break;

            case ComboBox comboBox:
                comboBox.ClearValue(Selector.SelectedItemProperty);
                comboBox.ClearValue(Selector.SelectedValueProperty);
                comboBox.ClearValue(Selector.SelectedIndexProperty);
                comboBox.SelectedItem = null;
                comboBox.SelectedIndex = -1;
                break;

            case DatePicker datePicker:
                datePicker.ClearValue(DatePicker.SelectedDateProperty);
                datePicker.SelectedDate = null;
                break;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (var index = 0; index < childCount; index++)
        {
            ClearPublicReviewInputDefaults(
                VisualTreeHelper.GetChild(element, index));
        }
    }

    private static (int Numbers, int Text, int Selections, int Dates)
        CountPublicReviewInputValues(DependencyObject element)
    {
        var numbers = element is NumberBox numberBox &&
                      double.IsFinite(numberBox.Value)
            ? 1
            : 0;
        var text = element switch
        {
            TextBox textBox when !string.IsNullOrWhiteSpace(textBox.Text) => 1,
            AutoSuggestBox autoSuggestBox
                when !string.IsNullOrWhiteSpace(autoSuggestBox.Text) => 1,
            _ => 0,
        };
        var selections = element is ComboBox comboBox &&
                         comboBox.SelectedIndex >= 0
            ? 1
            : 0;
        var dates = element is DatePicker datePicker &&
                    datePicker.SelectedDate.HasValue
            ? 1
            : 0;

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (var index = 0; index < childCount; index++)
        {
            var child = CountPublicReviewInputValues(
                VisualTreeHelper.GetChild(element, index));
            numbers += child.Numbers;
            text += child.Text;
            selections += child.Selections;
            dates += child.Dates;
        }

        return (numbers, text, selections, dates);
    }

    private void QueuePublicReviewInputSanitization()
    {
        if (!AppRuntimeMode.IsPublicReview)
        {
            return;
        }

        // A collapsed page can finish applying its XAML bindings only after it
        // becomes visible. Two low-priority passes clear those realized controls
        // without changing the normal app's working values or saved data.
        DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () =>
            {
                ClearPublicReviewInputDefaults(this);
                DispatcherQueue.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                    () =>
                    {
                        ClearPublicReviewInputDefaults(this);
                        var remaining = CountPublicReviewInputValues(this);
                        Program.Log(
                            "Public-review visible input values after render: " +
                            $"numbers={remaining.Numbers}, " +
                            $"text={remaining.Text}, " +
                            $"selections={remaining.Selections}, " +
                            $"dates={remaining.Dates}.");
                    });
            });
    }

    private void CostingEditorLayout_SizeChanged(
        object sender,
        SizeChangedEventArgs e) =>
        UpdatePreviewDockLayout(e.NewSize.Width, e.NewSize.Height);

    private void UpdatePreviewDockLayout(
        double availableWidth,
        double availableHeight)
    {
        if (CostingEditorLayout is null ||
            PreviewBottomDockRow is null ||
            PreviewSplitterColumn is null ||
            PreviewRailColumn is null ||
            PreviewResizeHandle is null ||
            SingleCorePreviewRail is null ||
            PreviewDockModeText is null ||
            availableWidth <= 0d)
        {
            return;
        }

        var dockRight = availableWidth >= PreviewRightDockThreshold;
        _isPreviewDockedRight = dockRight;
        if (dockRight)
        {
            PreviewBottomDockRow.Height = new GridLength(0d);
            PreviewSplitterColumn.Width = new GridLength(8d);
            var maximumWidth = Math.Max(
                PreviewMinimumRightDockWidth,
                Math.Min(
                    PreviewMaximumRightDockWidth,
                    availableWidth - PreviewMinimumWorkspaceWidth - 8d));
            _lastPreviewRightDockWidth = Math.Clamp(
                _lastPreviewRightDockWidth,
                PreviewMinimumRightDockWidth,
                maximumWidth);
            PreviewRailColumn.Width = new GridLength(
                _lastPreviewRightDockWidth);
            Grid.SetRow(SingleCorePreviewRail, 0);
            Grid.SetColumn(SingleCorePreviewRail, 2);
            Grid.SetColumnSpan(SingleCorePreviewRail, 1);
            SingleCorePreviewRail.Margin = new Thickness(0, 12, 24, 48);
            PreviewResizeHandle.Visibility = Visibility.Visible;
            PreviewDockModeText.Text =
                "Resizable right-hand dock · drag the divider";
            return;
        }

        if (_isPreviewResizeActive)
        {
            PreviewResizeHandle.ReleasePointerCaptures();
            _isPreviewResizeActive = false;
        }

        PreviewSplitterColumn.Width = new GridLength(0d);
        PreviewRailColumn.Width = new GridLength(0d);
        UpdateCompactPreviewHeight(availableHeight);
        Grid.SetRow(SingleCorePreviewRail, 1);
        Grid.SetColumn(SingleCorePreviewRail, 0);
        Grid.SetColumnSpan(SingleCorePreviewRail, 3);
        SingleCorePreviewRail.Margin = new Thickness(24, 8, 24, 24);
        PreviewResizeHandle.Visibility = Visibility.Collapsed;
        PreviewDockModeText.Text =
            "Bottom dock · compact window";
    }

    private void UpdateCompactPreviewHeight(double availableHeight)
    {
        if (_isPreviewDockedRight || PreviewBottomDockRow is null)
        {
            return;
        }

        var previewIsOn = SingleCorePreviewToggle?.IsOn == true;
        var height = previewIsOn
            ? Math.Clamp(availableHeight * 0.46d, 240d, 440d)
            : 118d;
        PreviewBottomDockRow.Height = new GridLength(height);
    }

    private void PreviewResizeHandle_PointerPressed(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isPreviewDockedRight ||
            sender is not UIElement resizeHandle)
        {
            return;
        }

        var point = e.GetCurrentPoint(CostingEditorLayout);
        _isPreviewResizeActive = true;
        _previewResizePointerId = e.Pointer.PointerId;
        _previewResizeStartX = point.Position.X;
        _previewResizeStartWidth = PreviewRailColumn.ActualWidth;
        SetPreviewResizeAffordance(isActive: true);
        resizeHandle.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void PreviewResizeHandle_PointerMoved(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isPreviewDockedRight ||
            !_isPreviewResizeActive ||
            e.Pointer.PointerId != _previewResizePointerId)
        {
            return;
        }

        var point = e.GetCurrentPoint(CostingEditorLayout);
        var maximumWidth = Math.Max(
            PreviewMinimumRightDockWidth,
            Math.Min(
                PreviewMaximumRightDockWidth,
                CostingEditorLayout.ActualWidth -
                PreviewMinimumWorkspaceWidth -
                8d));
        var resizedWidth = Math.Clamp(
            _previewResizeStartWidth -
            (point.Position.X - _previewResizeStartX),
            PreviewMinimumRightDockWidth,
            maximumWidth);
        _lastPreviewRightDockWidth = resizedWidth;
        PreviewRailColumn.Width = new GridLength(resizedWidth);
        e.Handled = true;
    }

    private void PreviewResizeHandle_PointerReleased(
        object sender,
        PointerRoutedEventArgs e) =>
        EndPreviewResize(sender, e);

    private void PreviewResizeHandle_PointerCanceled(
        object sender,
        PointerRoutedEventArgs e) =>
        EndPreviewResize(sender, e);

    private void EndPreviewResize(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isPreviewResizeActive ||
            e.Pointer.PointerId != _previewResizePointerId)
        {
            return;
        }

        if (sender is UIElement resizeHandle)
        {
            resizeHandle.ReleasePointerCapture(e.Pointer);
        }

        _isPreviewResizeActive = false;
        SetPreviewResizeAffordance(isActive: false);
        e.Handled = true;
    }

    private void PreviewResizeHandle_PointerEntered(
        object sender,
        PointerRoutedEventArgs e) =>
        SetPreviewResizeAffordance(isActive: true);

    private void PreviewResizeHandle_PointerExited(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isPreviewResizeActive)
        {
            SetPreviewResizeAffordance(isActive: false);
        }
    }

    private void SetPreviewResizeAffordance(bool isActive)
    {
        if (PreviewResizeIndicator is null)
        {
            return;
        }

        PreviewResizeIndicator.Width = isActive ? 6d : 4d;
        PreviewResizeIndicator.Opacity = isActive ? 1d : 0.72d;
        PreviewResizeIndicator.Background = ResourceBrush(
            isActive
                ? "AccentFillColorDefaultBrush"
                : "CardStrokeColorDefaultBrush");
    }

    private void MainPage_Unloaded(object sender, RoutedEventArgs e) =>
        _centralDataRefreshTimer.Stop();

    private async void CentralDataRefreshTimer_Tick(
        object? sender,
        object e)
    {
        if (!CostingViewModel.ShouldAttemptAutomaticRefresh)
        {
            return;
        }

        await CostingViewModel.RefreshCentralDataAsync(isAutomatic: true);
        UpdateCentralDataConnectionVisuals();
    }

    private async void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                CommitButtonText = "Use this folder"
            };

            picker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            // This is an unpackaged desktop app. It has normal filesystem access,
            // so persisting the selected path is sufficient. FutureAccessList is
            // a package-identity API and fails after a successful pick when the
            // app is run unpackaged from the USB/exFAT workspace.
            ViewModel.SetStorageFolder(folder.Path);
            UpdateStorageSetupVisibility();
        }
        catch (Exception exception)
        {
            Program.Log($"Folder selection failed: {exception}");
            ViewModel.SetStorageStatus(
                "The folder could not be saved. Please choose it again, or select another location.");
            UpdateStorageSetupVisibility();
        }
    }

    private void ContinueFromStorageSetup_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DismissStorageSetup();
        UpdateStorageSetupVisibility();
    }

    private void ShowStorageSetup_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowStorageSetup();
        UpdateStorageSetupVisibility();
    }

    private void RecalculateSingleCore_Click(object sender, RoutedEventArgs e)
    {
        CostingViewModel.Recalculate();
    }

    private async void SaveSingleCoreProject_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!TryGetSelectedStorageRoot(out var storageRoot))
        {
            return;
        }

        if (CostingViewModel.CurrentRevisionState ==
                CostingRevisionState.ApprovedRevision &&
            !CostingViewModel.HasUnsavedChanges)
        {
            CostingViewModel.CalculationStatus =
                $"Approved revision {CostingViewModel.CurrentRevisionNumber} is already saved and immutable. " +
                "Change an input to begin the next working revision, or duplicate it as a new project.";
            return;
        }

        try
        {
            var document = CostingViewModel.CreateProjectDocument();
            var saved = await _projectRepository.SaveAsync(
                storageRoot,
                document);
            CostingViewModel.MarkDocumentPersisted(
                saved.Document,
                saved.FullPath);
            CostingViewModel.CalculationStatus =
                $"Saved working revision {saved.Document.RevisionNumber} in the selected business-data folder. " +
                "The project index and local save remain independent of the central-data link.";
        }
        catch (Exception exception)
        {
            Program.Log($"Costing save failed: {exception}");
            CostingViewModel.CalculationStatus =
                "The costing could not be saved. No existing file was changed.";
        }
    }

    private async void OpenSingleCoreProject_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!TryGetSelectedStorageRoot(out var storageRoot))
        {
            return;
        }

        try
        {
            if (CostingViewModel.HasUnsavedChanges &&
                !await ConfirmDiscardUnsavedChangesAsync())
            {
                return;
            }

            var entries = await _projectRepository.ListAsync(storageRoot);
            var selection = entries.Count == 0
                ? await ChoosePortableProjectFileAsync()
                : await ChooseProjectRevisionAsync(entries);
            if (selection is null)
            {
                return;
            }

            var document = selection.IndexEntry is not null
                ? await _projectRepository.LoadAsync(
                    storageRoot,
                    selection.IndexEntry)
                : await _portableDocumentStore.LoadAsync(
                    selection.PortablePath!);

            if (!CostingViewModel.TryApplyProjectDocument(
                    document,
                    out var message))
            {
                CostingViewModel.CalculationStatus = message;
                return;
            }

            if (selection.IndexEntry is not null)
            {
                CostingViewModel.MarkDocumentPersisted(
                    document,
                    Path.Combine(
                        storageRoot,
                        selection.IndexEntry.RelativePath));
                CostingViewModel.CalculationStatus = message;
            }
            else
            {
                CostingViewModel.MarkDocumentNeedsIndexing(
                    selection.PortablePath!);
                CostingViewModel.CalculationStatus =
                    message +
                    " Use Save costing to add this portable file to the selected business-data folder index.";
            }
            ShowSection("costing");
        }
        catch (System.Text.Json.JsonException)
        {
            CostingViewModel.CalculationStatus =
                "The selected file is not a readable costing file.";
        }
        catch (Exception exception)
        {
            Program.Log($"Costing open failed: {exception}");
            CostingViewModel.CalculationStatus =
                "The costing could not be opened. Current values were retained.";
        }
    }

    private void DuplicateSingleCoreProject_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CostingViewModel.TryDuplicateCurrentProject(out var message))
        {
            CostingViewModel.CalculationStatus = message;
            ShowSection("costing");
        }
    }

    private async void ApproveSingleCoreRevision_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!TryGetSelectedStorageRoot(out var storageRoot))
        {
            return;
        }

        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title =
                    $"Approve revision {CostingViewModel.CurrentRevisionNumber}?",
                Content =
                    "Approval stores the current outputs and complete calculation trace. " +
                    "This saved revision becomes immutable; later edits automatically start the next working revision.",
                PrimaryButtonText = "Approve revision",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            var approved =
                CostingViewModel.CreateApprovedProjectDocument();
            var saved = await _projectRepository.SaveAsync(
                storageRoot,
                approved);
            CostingViewModel.MarkDocumentPersisted(
                saved.Document,
                saved.FullPath);
            CostingViewModel.CalculationStatus =
                $"Revision {saved.Document.RevisionNumber} is approved and immutable. " +
                "Its stored outputs and trace will reopen exactly even when central data is offline.";
        }
        catch (InvalidOperationException exception)
        {
            CostingViewModel.CalculationStatus = exception.Message;
        }
        catch (Exception exception)
        {
            Program.Log($"Costing approval failed: {exception}");
            CostingViewModel.CalculationStatus =
                "The revision could not be approved. The working copy was retained.";
        }
    }

    private bool TryGetSelectedStorageRoot(out string storageRoot)
    {
        storageRoot = ViewModel.StorageFolderPath;
        if (ViewModel.HasStorageFolder &&
            !string.IsNullOrWhiteSpace(storageRoot) &&
            Directory.Exists(storageRoot))
        {
            return true;
        }

        ViewModel.ShowStorageSetup();
        ViewModel.SetStorageStatus(
            "The selected business-data folder is unavailable. Choose an available folder before saving or opening costings.");
        UpdateStorageSetupVisibility();
        CostingViewModel.CalculationStatus =
            "No costing file was read or written because the selected business-data folder is unavailable.";
        storageRoot = string.Empty;
        return false;
    }

    private async Task<bool> ConfirmDiscardUnsavedChangesAsync()
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Open another costing?",
            Content =
                "The current working revision has unsaved changes. Opening another costing will replace those values.",
            PrimaryButtonText = "Open another",
            CloseButtonText = "Keep working",
            DefaultButton = ContentDialogButton.Close,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async Task<ProjectOpenSelection?> ChooseProjectRevisionAsync(
        IReadOnlyList<SingleCoreProjectIndexEntry> entries)
    {
        var selector = new ComboBox
        {
            Header = "Saved project and revision",
            ItemsSource = entries,
            DisplayMemberPath = nameof(SingleCoreProjectIndexEntry.DisplayName),
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 460,
        };
        var content = new StackPanel
        {
            Spacing = 12,
        };
        content.Children.Add(
            new TextBlock
            {
                Text =
                    "These revisions are indexed inside the selected business-data folder. Approved revisions reopen their stored outputs and trace.",
                TextWrapping = TextWrapping.Wrap,
            });
        content.Children.Add(selector);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Open costing",
            Content = content,
            PrimaryButtonText = "Open",
            SecondaryButtonText = "Browse portable file…",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            return selector.SelectedItem is SingleCoreProjectIndexEntry entry
                ? new ProjectOpenSelection(entry, null)
                : null;
        }

        return result == ContentDialogResult.Secondary
            ? await ChoosePortableProjectFileAsync()
            : null;
    }

    private async Task<ProjectOpenSelection?> ChoosePortableProjectFileAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            CommitButtonText = "Open portable costing",
        };
        picker.FileTypeFilter.Add(".atagcosting");
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            App.WindowHandle);

        var file = await picker.PickSingleFileAsync();
        return file is null
            ? null
            : new ProjectOpenSelection(null, file.Path);
    }

    private async void SetupCentralDataLink_Click(
        object sender,
        RoutedEventArgs e) =>
        await SetupCentralDataLinkAsync();

    private async void SetupFirstRunCentralData_Click(
        object sender,
        RoutedEventArgs e) =>
        await SetupCentralDataLinkAsync();

    private async Task SetupCentralDataLinkAsync()
    {
        var area = await ChooseCentralDataAreaAsync();
        if (area is null)
        {
            return;
        }

        var existingLink = CostingViewModel.GetDatabaseTableLink(area.Value);
        var source = await ChooseDatabaseSourceAsync(area.Value, existingLink);
        if (source is null)
        {
            return;
        }

        await NavigateAndTransformDatabaseTableAsync(source, existingLink);
        UpdateFirstRunSetupState();
        UpdateCentralDataConnectionVisuals();
    }

    private async void EditCentralDataLink_Click(
        object sender,
        RoutedEventArgs e)
    {
        var existingLink = await ChooseExistingDataLinkAsync();
        if (existingLink is null)
        {
            return;
        }

        var source = existingLink.SourceKind == CentralDataSourceKind.SqlDatabase &&
                     !existingLink.UseWindowsAuthentication
            ? await ChooseDatabaseSourceAsync(existingLink.Area, existingLink)
            : DatabaseLinkDraft.FromLink(existingLink);
        if (source is null)
        {
            return;
        }

        await EditExistingDatabaseTableAsync(source, existingLink);
        UpdateFirstRunSetupState();
        UpdateCentralDataConnectionVisuals();
    }

    private async void RemoveCentralDataLink_Click(
        object sender,
        RoutedEventArgs e)
    {
        var existingLink = await ChooseExistingDataLinkAsync(
            title: "Remove data link",
            primaryButtonText: "Continue",
            instruction: "Choose the refresh link to remove. Imported data is not removed.",
            alwaysShowPicker: true);
        if (existingLink is null)
        {
            return;
        }

        var content = CreateWizardContent(
            step: 3,
            $"Remove the {AreaName(existingLink.Area)} refresh link to {existingLink.TableName}? " +
            "The full transformed table, the validated costing view, and manual project save remain available offline.");
        var confirmation = CreateWizardWindow(
            $"Remove {AreaName(existingLink.Area)} link",
            content,
            primaryButtonText: "Remove link");
        if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        CostingViewModel.RemoveDatabaseTableLink(existingLink.Area);
        RefreshRetainedSourceTablesView();
        UpdateFirstRunSetupState();
        UpdateCentralDataConnectionVisuals();
    }

    private async void RefreshCentralData_Click(
        object sender,
        RoutedEventArgs e)
    {
        await CostingViewModel.RefreshCentralDataAsync();
        UpdateFirstRunSetupState();
        UpdateCentralDataConnectionVisuals();
    }

    private async void RefreshExchangeRates_Click(
        object sender,
        RoutedEventArgs e) =>
        await CostingViewModel.RefreshExchangeRatesAsync();

    private async void GenerateQuotationPdf_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName =
                    string.IsNullOrWhiteSpace(CostingViewModel.QuoteNumber)
                        ? AppRuntimeMode.IsOrganisationBranded
                            ? "ATAG quotation"
                            : "Quotation"
                        : CostingViewModel.QuoteNumber,
                CommitButtonText = "Generate A4 quotation",
            };
            picker.FileTypeChoices.Add(
                "A4 PDF quotation",
                new List<string> { ".pdf" });
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                App.WindowHandle);

            var file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                return;
            }

            var quotation = CostingViewModel.CreateA4QuotationDocument();
            await using var stream = File.Create(file.Path);
            _quotationPdfGenerator.Generate(stream, quotation);
            CostingViewModel.CalculationStatus =
                $"Generated A4 quotation {file.Name} in " +
                $"{quotation.CurrencyCode}.";
        }
        catch (Exception exception)
        {
            Program.Log($"Quotation PDF generation failed: {exception}");
            CostingViewModel.CalculationStatus =
                "The quotation PDF could not be generated. " +
                "Choose GBP or refresh the selected currency rate, then try again.";
        }
    }

    private void PinResults_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle)
        {
            PinnedResultBar.Visibility =
                toggle.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }

    private void UnpinResults_Click(object sender, RoutedEventArgs e)
    {
        PinnedResultBar.Visibility = Visibility.Collapsed;
        PinResultsButton.IsChecked = false;
    }

    private void SingleCorePreviewToggle_Toggled(
        object sender,
        RoutedEventArgs e)
    {
        if (SingleCorePreviewContent is null ||
            sender is not ToggleSwitch toggle)
        {
            return;
        }

        SingleCorePreviewContent.Visibility =
            toggle.IsOn
                ? Visibility.Visible
                : Visibility.Collapsed;
        SingleCorePreviewOffHint.Visibility =
            toggle.IsOn
                ? Visibility.Collapsed
                : Visibility.Visible;
        if (toggle.IsOn)
        {
            UpdateCorePrintPreviewVisibility();
            RenderSingleCorePreviewGeometry();
        }
        else
        {
            SingleCoreDetailedStrandPath.Data = null;
            SingleCoreRopeGroupOutlinePath.Data = null;
            SingleCoreSideStrandCanvas.Children.Clear();
        }

        UpdateCompactPreviewHeight(CostingEditorLayout.ActualHeight);
    }

    private void SingleCoreDetailedPreviewToggle_Toggled(
        object sender,
        RoutedEventArgs e) =>
        RenderSingleCorePreviewGeometry();

    private void CostingViewModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(SingleCoreCostingViewModel.RetainedSourceTables))
        {
            RefreshRetainedSourceTablesView();
            UpdateFirstRunSetupState();
        }

        if (e.PropertyName ==
            nameof(SingleCoreCostingViewModel.DatabaseTableLinks))
        {
            UpdateFirstRunSetupState();
        }

        if (e.PropertyName ==
            nameof(SingleCoreCostingViewModel.HasCorePrint))
        {
            UpdateCorePrintPreviewVisibility();
        }

        if (SingleCorePreviewToggle?.IsOn != true)
        {
            return;
        }

        if (e.PropertyName is
            nameof(SingleCoreCostingViewModel.SelectedCopper) or
            nameof(SingleCoreCostingViewModel.PreviewConductorDiameterPixels) or
            nameof(SingleCoreCostingViewModel.PreviewSideConductorHeightPixels) or
            nameof(SingleCoreCostingViewModel.PreviewConductorColourHex))
        {
            RenderSingleCorePreviewGeometry();
        }
    }

    private void UpdateFirstRunSetupState()
    {
        var completedAreas = RequiredCentralDataAreas
            .Where(area =>
                CostingViewModel.DatabaseTableLinks.Any(link =>
                    link.Area == area) &&
                CostingViewModel.RetainedSourceTables.Any(table =>
                    table.Area == area))
            .ToHashSet();
        var missingAreas = RequiredCentralDataAreas
            .Where(area => !completedAreas.Contains(area))
            .Select(AreaName)
            .ToArray();

        ViewModel.SetCentralDataSetupState(
            completedAreas.Count,
            missingAreas);
    }

    private void UpdateCorePrintPreviewVisibility()
    {
        if (SingleCorePrintPreviewBlock is null)
        {
            return;
        }

        SingleCorePrintPreviewBlock.Visibility =
            CostingViewModel.HasCorePrint
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void RenderSingleCorePreviewGeometry()
    {
        if (SingleCoreSimpleConductorCircle is null ||
            SingleCoreDetailedStrandPath is null ||
            SingleCoreRopeGroupOutlinePath is null ||
            SingleCoreWallDimensionLine is null ||
            SingleCoreSideStrandCanvas is null ||
            SingleCoreSideConductorBody is null ||
            SingleCoreSideConductorHighlight is null ||
            SingleCoreSideConductorEndFace is null ||
            SingleCoreSideInsulationRing is null ||
            SingleCoreSideInsulationRingHighlight is null ||
            SingleCoreSideInsulationCutout is null)
        {
            return;
        }

        var conductorDiameter =
            Math.Clamp(
                CostingViewModel.PreviewConductorDiameterPixels,
                3d,
                176d);
        SingleCoreWallDimensionLine.X1 =
            Math.Clamp(105d + conductorDiameter / 2d, 105d, 193d);
        SingleCoreWallDimensionLine.X2 = 193d;
        UpdateSingleCoreSideProfileGeometry();

        var construction = CostingViewModel.SelectedCopper?.Construction;
        var showDetailed =
            SingleCoreDetailedPreviewToggle?.IsOn == true &&
            construction is not null;
        SingleCoreSimpleConductorCircle.Visibility =
            showDetailed
                ? Visibility.Collapsed
                : Visibility.Visible;
        SingleCoreDetailedStrandPath.Visibility =
            showDetailed
                ? Visibility.Visible
                : Visibility.Collapsed;
        SingleCoreRopeGroupOutlinePath.Visibility =
            showDetailed && construction!.IsRopeLay
                ? Visibility.Visible
                : Visibility.Collapsed;
        SingleCoreSideConductorBody.Opacity = showDetailed ? 0.28d : 1d;
        SingleCoreSideConductorHighlight.Opacity = showDetailed ? 0.42d : 1d;
        SingleCoreSideConductorEndFace.Opacity = showDetailed ? 0.28d : 1d;
        SingleCoreSideConductorOpeningFace.Opacity = showDetailed ? 0.28d : 1d;

        if (!showDetailed)
        {
            SingleCoreDetailedStrandPath.Data = null;
            SingleCoreRopeGroupOutlinePath.Data = null;
            SingleCoreSideStrandCanvas.Children.Clear();
            return;
        }

        var layout = ConductorPreviewLayoutBuilder.Create(
            construction!,
            centerX: 105d,
            centerY: 105d,
            envelopeRadius: conductorDiameter / 2d - 1d);
        var smallestStrandRadius = layout.Strands
            .Select(strand => strand.Radius)
            .DefaultIfEmpty(1d)
            .Min();
        SingleCoreDetailedStrandPath.StrokeThickness = Math.Clamp(
            smallestStrandRadius * 0.18d,
            0.06d,
            0.45d);
        SingleCoreRopeGroupOutlinePath.StrokeThickness = Math.Clamp(
            layout.Groups
                .Where(group => group.Level == 0)
                .Select(group => group.Radius)
                .DefaultIfEmpty(1d)
                .Min() * 0.06d,
            0.18d,
            0.8d);
        var strandGeometry = new GeometryGroup();
        foreach (var strand in layout.Strands)
        {
            strandGeometry.Children.Add(
                new EllipseGeometry
                {
                    Center = new Windows.Foundation.Point(
                        strand.X,
                        strand.Y),
                    RadiusX = strand.Radius,
                    RadiusY = strand.Radius,
                });
        }

        var groupGeometry = new GeometryGroup();
        foreach (var group in layout.Groups)
        {
            groupGeometry.Children.Add(
                new EllipseGeometry
                {
                    Center = new Windows.Foundation.Point(
                        group.X,
                        group.Y),
                    RadiusX = group.Radius,
                    RadiusY = group.Radius,
                });
        }

        SingleCoreDetailedStrandPath.Data = strandGeometry;
        SingleCoreRopeGroupOutlinePath.Data = groupGeometry;
        RenderDetailedSideStrands(construction!, layout);
    }

    private void RenderDetailedSideStrands(
        ConductorConstructionResult construction,
        ConductorPreviewLayout layout)
    {
        SingleCoreSideStrandCanvas.Children.Clear();
        var sideHeight = Math.Max(
            8d,
            CostingViewModel.PreviewSideConductorHeightPixels);
        var crossDiameter = Math.Max(
            1d,
            CostingViewModel.PreviewConductorDiameterPixels);
        var yScale = sideHeight / crossDiameter;
        var colour = ParsePreviewColour(
            CostingViewModel.PreviewConductorColourHex);
        var sideUnits = layout.SurfaceUnits;
        if (sideUnits.Count == 0)
        {
            return;
        }

        var outline = new SolidColorBrush(
            Color.FromArgb(165, 65, 34, 15));
        var turns = construction.IsRopeLay ? 0.22d : 0.34d;
        AddDetailedOpeningFace(
            layout,
            yScale,
            colour,
            outline);
        var coreUnits = construction.IsRopeLay
            ? layout.Groups
                .Where(unit => unit.Level == 0 && !unit.IsBoundary)
                .ToArray()
            : layout.Strands
                .Where(unit => unit.Level == 0 && !unit.IsBoundary)
                .ToArray();
        foreach (var coreUnit in coreUnits.OrderBy(unit => unit.Y))
        {
            AddContinuousHelixSurface(
                new HelixSurface(coreUnit, -10),
                yScale,
                turns,
                colour,
                outline,
                construction.IsRopeLay);
        }

        var maximumOrbit = Math.Max(
            1d,
            sideUnits
                .Select(unit => UnitDistanceFromCenter(unit) * yScale)
                .DefaultIfEmpty(1d)
                .Max());
        var helicalSurfaces = sideUnits
            .Select(
                surface =>
                    new HelixSurface(
                        surface,
                        GetSurfaceColourAdjustment(
                            surface,
                            yScale,
                            turns,
                            maximumOrbit)))
            .ToArray();

        foreach (var surface in helicalSurfaces
                     .Where(surface =>
                         GetHelixDepthAt(
                             surface.Surface,
                             turns,
                             0.5d) < 0d)
                     .OrderBy(surface =>
                         GetHelixDepthAt(
                             surface.Surface,
                             turns,
                             0.5d)))
        {
            AddContinuousHelixSurface(
                surface,
                yScale,
                turns,
                colour,
                outline,
                construction.IsRopeLay);
        }

        foreach (var surface in helicalSurfaces
                     .Where(surface =>
                         GetHelixDepthAt(
                             surface.Surface,
                             turns,
                             0.5d) >= 0d)
                     .OrderBy(surface =>
                         GetHelixDepthAt(
                             surface.Surface,
                             turns,
                             0.5d)))
        {
            AddContinuousHelixSurface(
                surface,
                yScale,
                turns,
                colour,
                outline,
                construction.IsRopeLay);
        }

        AddDetailedEndFace(
            layout,
            yScale,
            turns,
            colour,
            outline);
    }

    private void AddDetailedEndFace(
        ConductorPreviewLayout layout,
        double yScale,
        double turns,
        Color colour,
        Brush outline)
    {
        var strandGeometry = CreateAngledEndFaceGeometry(
            layout.Strands,
            yScale,
            turns);
        var strandOutlineThickness = Math.Clamp(
            layout.Strands
                .Select(strand => strand.Radius * yScale)
                .DefaultIfEmpty(1d)
                .Min() * 0.22d,
            0.05d,
            0.55d);
        SingleCoreSideStrandCanvas.Children.Add(
            new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = strandGeometry,
                Fill = new SolidColorBrush(AdjustColour(colour, -2)),
                Stroke = outline,
                StrokeThickness = strandOutlineThickness,
                IsHitTestVisible = false,
            });

        if (layout.Groups.Count == 0)
        {
            return;
        }

        var groupGeometry = CreateAngledEndFaceGeometry(
            layout.Groups.Where(group => group.Level == 0),
            yScale,
            turns);
        SingleCoreSideStrandCanvas.Children.Add(
            new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = groupGeometry,
                Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Stroke = new SolidColorBrush(
                    Color.FromArgb(135, 86, 46, 21)),
                StrokeThickness = Math.Clamp(
                    layout.Groups
                        .Where(group => group.Level == 0)
                        .Select(group => group.Radius * yScale)
                        .DefaultIfEmpty(1d)
                        .Min() * 0.08d,
                    0.12d,
                    0.75d),
                IsHitTestVisible = false,
            });
    }

    private void AddDetailedOpeningFace(
        ConductorPreviewLayout layout,
        double yScale,
        Color colour,
        Brush outline)
    {
        var strandGeometry = CreateAngledFaceGeometry(
            layout.Strands,
            centerX: 348d,
            yScale,
            rotationTurns: 0d);
        SingleCoreSideStrandCanvas.Children.Add(
            new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = strandGeometry,
                Fill = new SolidColorBrush(AdjustColour(colour, -2)),
                Stroke = outline,
                StrokeThickness = Math.Clamp(
                    layout.Strands
                        .Select(strand => strand.Radius * yScale)
                        .DefaultIfEmpty(1d)
                        .Min() * 0.22d,
                    0.05d,
                    0.55d),
                IsHitTestVisible = false,
            });
    }

    private static GeometryGroup CreateAngledEndFaceGeometry(
        IEnumerable<ConductorPreviewCircle> circles,
        double yScale,
        double turns) =>
        CreateAngledFaceGeometry(
            circles,
            centerX: 482d,
            yScale,
            rotationTurns: turns);

    private static GeometryGroup CreateAngledFaceGeometry(
        IEnumerable<ConductorPreviewCircle> circles,
        double centerX,
        double yScale,
        double rotationTurns)
    {
        const double centerY = 75d;
        const double perspectiveScale =
            0.3420201433256687d;
        var rotation = rotationTurns * 2d * Math.PI;
        var cosine = Math.Cos(rotation);
        var sine = Math.Sin(rotation);
        var geometry = new GeometryGroup();
        foreach (var circle in circles)
        {
            var relativeX = circle.X - 105d;
            var relativeY = circle.Y - 105d;
            var rotatedX =
                relativeX * cosine - relativeY * sine;
            var rotatedY =
                relativeX * sine + relativeY * cosine;
            var radiusY = Math.Max(0.2d, circle.Radius * yScale);
            var radiusX = Math.Max(
                0.14d,
                radiusY * perspectiveScale);
            geometry.Children.Add(
                new EllipseGeometry
                {
                    Center = new Windows.Foundation.Point(
                        centerX +
                        rotatedX * yScale * perspectiveScale,
                        centerY + rotatedY * yScale),
                    RadiusX = radiusX,
                    RadiusY = radiusY,
                });
        }

        return geometry;
    }

    private void AddContinuousHelixSurface(
        HelixSurface surface,
        double yScale,
        double turns,
        Color colour,
        Brush outline,
        bool showBundleTexture)
    {
        var scaledDiameter = Math.Max(
            0.55d,
            surface.Surface.Radius * 2d * yScale);
        var outlineAddition = Math.Clamp(
            scaledDiameter * 0.16d,
            0.22d,
            1.3d);
        var outlineGeometry = CreateContinuousHelixGeometry(
            surface.Surface,
            yScale,
            turns);

        SingleCoreSideStrandCanvas.Children.Add(
            CreateSurfacePath(
                outlineGeometry,
                outline,
                scaledDiameter + outlineAddition,
                PenLineCap.Round));
        var fillGeometry = CreateContinuousHelixGeometry(
            surface.Surface,
            yScale,
            turns);
        SingleCoreSideStrandCanvas.Children.Add(
            CreateSurfacePath(
                fillGeometry,
                new SolidColorBrush(
                    AdjustColour(
                        colour,
                        surface.ColourAdjustment)),
                scaledDiameter,
                PenLineCap.Round));
        var highlightGeometry = CreateContinuousHelixGeometry(
            surface.Surface,
            yScale,
            turns,
            -scaledDiameter * 0.18d);
        var highlightColour = AdjustColour(
            colour,
            surface.ColourAdjustment + 42);
        SingleCoreSideStrandCanvas.Children.Add(
            CreateSurfacePath(
                highlightGeometry,
                new SolidColorBrush(Color.FromArgb(
                    105,
                    highlightColour.R,
                    highlightColour.G,
                    highlightColour.B)),
                Math.Clamp(scaledDiameter * 0.09d, 0.28d, 1.1d),
                PenLineCap.Round));

        if (showBundleTexture)
        {
            AddBundleTexture(
                surface.Surface,
                yScale,
                turns,
                scaledDiameter,
                colour,
                surface.ColourAdjustment);
        }
    }

    private static Microsoft.UI.Xaml.Shapes.Path CreateSurfacePath(
        Geometry geometry,
        Brush stroke,
        double strokeThickness,
        PenLineCap lineCap = PenLineCap.Round)
    {
        return new Microsoft.UI.Xaml.Shapes.Path
        {
            Data = geometry,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            StrokeStartLineCap = lineCap,
            StrokeEndLineCap = lineCap,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false,
        };
    }

    private void AddBundleTexture(
        ConductorPreviewCircle surface,
        double yScale,
        double turns,
        double scaledDiameter,
        Color colour,
        int colourAdjustment)
    {
        foreach (var offsetFactor in BundleTextureOffsets)
        {
            var textureGeometry = CreateContinuousHelixGeometry(
                surface,
                yScale,
                turns,
                scaledDiameter * offsetFactor);
            SingleCoreSideStrandCanvas.Children.Add(
                CreateSurfacePath(
                    textureGeometry,
                    CreateBundleTextureBrush(
                        colour,
                        colourAdjustment),
                    Math.Max(0.45d, scaledDiameter * 0.055d),
                    PenLineCap.Flat));
        }
    }

    private static PathGeometry CreateContinuousHelixGeometry(
        ConductorPreviewCircle surface,
        double yScale,
        double turns,
        double verticalOffset = 0d)
    {
        const int segmentCount = 72;
        const double startX = 348d;
        const double endX = 482d;
        const double centerY = 75d;
        var orbitRadius =
            UnitDistanceFromCenter(surface) * yScale;
        var phase =
            Math.Atan2(
                surface.Y - 105d,
                surface.X - 105d);
        var figure = new PathFigure
        {
            StartPoint = new Windows.Foundation.Point(
                startX,
                centerY +
                Math.Sin(phase) * orbitRadius +
                verticalOffset),
            IsClosed = false,
        };
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        for (var segment = 1; segment <= segmentCount; segment++)
        {
            var progress = segment / (double)segmentCount;
            var angle =
                phase +
                progress * turns * 2d * Math.PI;
            figure.Segments.Add(
                new LineSegment
                {
                    Point =
                new Windows.Foundation.Point(
                    startX + (endX - startX) * progress,
                    centerY +
                    Math.Sin(angle) * orbitRadius +
                    verticalOffset),
                });
        }

        return geometry;
    }

    private static double GetHelixDepthAt(
        ConductorPreviewCircle surface,
        double turns,
        double progress)
    {
        var phase =
            Math.Atan2(
                surface.Y - 105d,
                surface.X - 105d);
        return Math.Cos(
            phase +
            progress * turns * 2d * Math.PI);
    }

    private static int GetSurfaceColourAdjustment(
        ConductorPreviewCircle surface,
        double yScale,
        double turns,
        double maximumOrbit)
    {
        var depth =
            GetHelixDepthAt(surface, turns, 0.5d) *
            UnitDistanceFromCenter(surface) *
            yScale;
        return (int)Math.Round(
            Math.Clamp(
                depth / maximumOrbit * 18d,
                -18d,
                18d));
    }

    private void UpdateSingleCoreSideProfileGeometry()
    {
        var sideHeight = Math.Clamp(
            CostingViewModel.PreviewSideConductorHeightPixels,
            6d,
            90d);
        var top = 75d - sideHeight / 2d;
        var endFaceWidth = Math.Clamp(
            sideHeight * Math.Sin(20d * Math.PI / 180d),
            6d,
            28d);

        Canvas.SetTop(SingleCoreSideConductorBody, top);
        Canvas.SetTop(SingleCoreSideConductorHighlight, top);
        Canvas.SetTop(SingleCoreSideConductorEndFace, top);
        Canvas.SetTop(SingleCoreSideConductorOpeningFace, top);
        Canvas.SetTop(SingleCoreSideInsulationCutout, top);
        Canvas.SetLeft(SingleCoreSideConductorBody, 348d);
        Canvas.SetLeft(SingleCoreSideConductorHighlight, 348d);
        SingleCoreSideConductorBody.Width = 134d;
        SingleCoreSideConductorHighlight.Width = 134d;
        SingleCoreSideConductorEndFace.Width = endFaceWidth;
        Canvas.SetLeft(
            SingleCoreSideConductorEndFace,
            482d - endFaceWidth / 2d);
        SingleCoreSideConductorOpeningFace.Width = endFaceWidth;
        Canvas.SetLeft(
            SingleCoreSideConductorOpeningFace,
            348d - endFaceWidth / 2d);
        SingleCoreSideInsulationCutout.Width = endFaceWidth;
        Canvas.SetLeft(
            SingleCoreSideInsulationCutout,
            348d - endFaceWidth / 2d);
        var insulationRingGeometry = CreateInsulationRingGeometry(
            endFaceWidth,
            sideHeight);
        SingleCoreSideInsulationRing.Data = insulationRingGeometry;
        SingleCoreSideInsulationRingHighlight.Data =
            CreateInsulationRingGeometry(
                endFaceWidth,
                sideHeight);
    }

    private static GeometryGroup CreateInsulationRingGeometry(
        double innerWidth,
        double innerHeight)
    {
        var ring = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd,
        };
        ring.Children.Add(
            new EllipseGeometry
            {
                Center = new Windows.Foundation.Point(348d, 75d),
                RadiusX = 15d,
                RadiusY = 45d,
            });
        ring.Children.Add(
            new EllipseGeometry
            {
                Center = new Windows.Foundation.Point(348d, 75d),
                RadiusX = innerWidth / 2d,
                RadiusY = innerHeight / 2d,
            });
        return ring;
    }

    private static double UnitDistanceFromCenter(ConductorPreviewCircle unit) =>
        Math.Sqrt(
            Math.Pow(unit.X - 105d, 2d) +
            Math.Pow(unit.Y - 105d, 2d));

    private static Color AdjustColour(Color colour, int amount) =>
        Color.FromArgb(
            235,
            (byte)Math.Clamp(colour.R + amount, 0, 255),
            (byte)Math.Clamp(colour.G + amount, 0, 255),
            (byte)Math.Clamp(colour.B + amount, 0, 255));

    private static SolidColorBrush CreateBundleTextureBrush(
        Color colour,
        int amount)
    {
        var adjusted = AdjustColour(colour, amount + 38);
        return new SolidColorBrush(
            Color.FromArgb(
                120,
                adjusted.R,
                adjusted.G,
                adjusted.B));
    }

    private static Color ParsePreviewColour(string? value)
    {
        var hex = value?.Trim().TrimStart('#') ?? "";
        if (hex.Length != 6 ||
            !byte.TryParse(
                hex[..2],
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var red) ||
            !byte.TryParse(
                hex[2..4],
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var green) ||
            !byte.TryParse(
                hex[4..6],
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var blue))
        {
            return Color.FromArgb(255, 199, 120, 46);
        }

        return Color.FromArgb(255, red, green, blue);
    }

    private async void OpenWallReferenceSource_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Uri.TryCreate(
                CostingViewModel.PreviewWallSourceUrl,
                UriKind.Absolute,
                out var source))
        {
            await Launcher.LaunchUriAsync(source);
        }
    }

    private sealed record HelixSurface(
        ConductorPreviewCircle Surface,
        int ColourAdjustment);

    private void OpenResultWindow_Click(object sender, RoutedEventArgs e)
    {
        if (_resultWindow is null)
        {
            _resultWindow = new ResultWindow(CostingViewModel);
            _resultWindow.Closed += (_, _) => _resultWindow = null;
        }

        _resultWindow.Activate();
    }

    private void JumpToCostingSection_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string section })
        {
            return;
        }

        FrameworkElement? target = section switch
        {
            "basis" => CostingBasisSection,
            "conductor" => ConductorSection,
            "compound" => CompoundSection,
            "masterbatch" => MasterbatchSection,
            "print" => CorePrintSection,
            "labour" => LabourSection,
            "name" => CoreNameSection,
            "results" => ResultsSection,
            "quotation" => QuotationSection,
            "trace" => CalculationTraceSection,
            _ => null,
        };
        target?.StartBringIntoView();
    }

    private void MasterbatchSearch_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        UpdateMasterbatchSearchSuggestions();
    }

    private void MasterbatchColourFilter_Changed(
        object sender,
        SelectionChangedEventArgs e) =>
        UpdateMasterbatchSearchSuggestions();

    private void MasterbatchColourTypeFilter_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason ==
            AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text.Trim();
            sender.ItemsSource = string.IsNullOrEmpty(query)
                ? CostingViewModel.MasterbatchColourTypeOptions
                : CostingViewModel.MasterbatchColourTypeOptions
                    .Where(
                        item => item.Contains(
                            query,
                            StringComparison.CurrentCultureIgnoreCase))
                    .ToArray();
        }

        UpdateMasterbatchSearchSuggestions();
    }

    private void UpdateMasterbatchSearchSuggestions()
    {
        if (MasterbatchSearchBox is null)
        {
            return;
        }

        var group = MasterbatchGroupFilter?.SelectedItem as string;
        var type = MasterbatchTypeFilter?.Text;
        MasterbatchSearchBox.ItemsSource =
            CostingViewModel.MasterbatchMaterials
                .Where(
                    item => MasterbatchColourSearch.Matches(
                        item,
                        MasterbatchSearchBox.Text,
                        group,
                        type))
                .ToArray();
    }

    private void MasterbatchSearch_SuggestionChosen(
        AutoSuggestBox sender,
        AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is not MasterbatchReference selected)
        {
            return;
        }

        CostingViewModel.SelectedMasterbatch = selected;
        sender.Text = selected.ColourName;
    }

    private void MasterbatchSearch_QuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is MasterbatchReference selected)
        {
            CostingViewModel.SelectedMasterbatch = selected;
            sender.Text = selected.ColourName;
        }
    }

    private void CustomerSearch_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        var query = sender.Text.Trim();
        sender.ItemsSource = string.IsNullOrEmpty(query)
            ? CostingViewModel.ContactMaterials
            : CostingViewModel.ContactMaterials
                .Where(
                    item =>
                        item.AccountName.Contains(
                            query,
                            StringComparison.CurrentCultureIgnoreCase) ||
                        item.ShortName.Contains(
                            query,
                            StringComparison.CurrentCultureIgnoreCase) ||
                        item.PostCode.Contains(
                            query,
                            StringComparison.CurrentCultureIgnoreCase) ||
                        item.SalesEmail.Contains(
                            query,
                            StringComparison.CurrentCultureIgnoreCase))
                .ToArray();
    }

    private void CustomerSearch_SuggestionChosen(
        AutoSuggestBox sender,
        AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is not ContactReference selected)
        {
            return;
        }

        CostingViewModel.SelectedCustomerContact = selected;
        sender.Text = selected.AccountName;
    }

    private void CustomerSearch_QuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is ContactReference selected)
        {
            CostingViewModel.SelectedCustomerContact = selected;
            sender.Text = selected.AccountName;
        }
    }

    private void UpdateCentralDataConnectionVisuals()
    {
        var (colour, severity) = CostingViewModel.ConnectionState switch
        {
            CentralDataConnectionState.Online =>
                (Color.FromArgb(255, 40, 167, 69), InfoBarSeverity.Success),
            CentralDataConnectionState.Offline =>
                (Color.FromArgb(255, 196, 75, 44), InfoBarSeverity.Error),
            CentralDataConnectionState.Checking =>
                (Color.FromArgb(255, 0, 120, 212), InfoBarSeverity.Informational),
            _ =>
                (Color.FromArgb(255, 202, 138, 4), InfoBarSeverity.Warning),
        };

        CentralDataConnectionDot.Fill = new SolidColorBrush(colour);
        CentralDataConnectionText.Text =
            CostingViewModel.CentralDataLinkSummaryDisplay;
        CentralDataConnectionInfoBar.Severity = severity;
        if (EditCentralDataLinkButton is not null)
        {
            EditCentralDataLinkButton.IsEnabled =
                CostingViewModel.HasConfiguredLiveLink;
        }
        if (RemoveCentralDataLinkButton is not null)
        {
            RemoveCentralDataLinkButton.IsEnabled =
                CostingViewModel.HasConfiguredLiveLink;
        }
    }

    private void ThemeMode_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            ViewModel.SelectedThemeIndex = comboBox.SelectedIndex;
            ApplyVisualStyle();
        }
    }

    private void BackdropMode_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            ViewModel.SelectedBackdropIndex = comboBox.SelectedIndex;
            ApplyVisualStyle();
        }
    }

    private async void OpenWindowsSetting_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is Button { Tag: string settingsUri })
        {
            await Launcher.LaunchUriAsync(new Uri(settingsUri));
        }
    }

    private async void OpenStorageFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(
                ViewModel.StorageFolderPath);
            await Launcher.LaunchFolderAsync(folder);
        }
        catch (Exception)
        {
            ViewModel.SetStorageStatus(
                "The selected folder is not currently available. Choose it again or select another location.");
            ViewModel.ShowStorageSetup();
            UpdateStorageSetupVisibility();
        }
    }

    private void AppNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ShowSection("settings");
            return;
        }

        if (args.SelectedItemContainer?.Tag is string tag)
        {
            const string costingSectionPrefix = "costing-section:";
            if (tag.StartsWith(
                    costingSectionPrefix,
                    StringComparison.Ordinal))
            {
                ShowSection("costing", syncNavigation: false);
                var costingSection = tag[costingSectionPrefix.Length..];
                DispatcherQueue.TryEnqueue(
                    () => BringCostingSectionIntoView(costingSection));
                return;
            }

            ShowSection(tag);
        }
    }

    private void ModuleCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag })
        {
            ShowSection(tag);
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowSection("settings");
        AppNavigation.SelectedItem = null;
    }

    private void ShowSection(
        string section,
        bool syncNavigation = true)
    {
        HomeView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        ModulePlaceholderView.Visibility = Visibility.Collapsed;
        DualConstructionView.Visibility = Visibility.Collapsed;
        CostingWorkspaceView.Visibility = Visibility.Collapsed;
        ContractReviewView.Visibility = Visibility.Collapsed;
        MaterialDataView.Visibility = Visibility.Collapsed;

        switch (section)
        {
            case "home":
                ViewModel.SetSection(
                    "Home",
                    "Start a costing, review recent work, or manage the calculation data.");
                HomeView.Visibility = Visibility.Visible;
                break;

            case "settings":
                ViewModel.SetSection(
                    "Settings",
                    "Control visual style, file locations and how the application starts.");
                SettingsView.Visibility = Visibility.Visible;
                break;

            case "costing":
                ViewModel.SetSection(
                    "Costing workspace",
                    "Version 1 builds and prices one insulated conductor core with a complete calculation trace.");
                CostingWorkspaceView.Visibility = Visibility.Visible;
                break;

            case "contract-review":
                ViewModel.SetSection(
                    "Contract review",
                    "Review the current costing, compare selling-price methods, and record approval, order acceptance, and contract amendments.");
                ContractReviewView.Visibility = Visibility.Visible;
                break;

            case "costing-dual":
                ViewModel.SetSection(
                    "Dual insulated cable",
                    "Choose the optional layers and review the confirmed inside-to-outside dual-insulation build.");
                DualConstructionView.Visibility = Visibility.Visible;
                break;

            case "costing-flat":
                ShowModulePlaceholder(
                    "Flat cable",
                    "Planned construction for up to ten cores arranged in a line. Its geometry and costing editor will reuse the shared layer modules.");
                break;

            case "costing-dshape":
                ShowModulePlaceholder(
                    "D-shape cable",
                    "Planned in-line construction for up to ten cores with a D-shaped finish and the shared layer modules.");
                break;

            case "materials":
                ViewModel.SetSection(
                    "Live Data",
                    "Use retained last-known data and map each material table from Microsoft Access or SQL Server.");
                MaterialDataView.Visibility = Visibility.Visible;
                break;

            case "braid":
                ShowModulePlaceholder(
                    "Braid calculator",
                    "Compare carrier counts, pitch, coverage and buncher settings with a full calculation trace.");
                break;

            case "reports":
                ShowModulePlaceholder(
                    "Reports",
                    "Preview quotes, internal costing sheets, contract reviews and calculation appendices.");
                break;
        }

        if (syncNavigation)
        {
            SyncNavigationSelection(section);
        }

        QueuePublicReviewInputSanitization();
    }

    private void BringCostingSectionIntoView(string section)
    {
        FrameworkElement? target = section switch
        {
            "basis" => CostingBasisSection,
            "conductor" => ConductorSection,
            "compound" => CompoundSection,
            "masterbatch" => MasterbatchSection,
            "print" => CorePrintSection,
            "labour" => LabourSection,
            "name" => CoreNameSection,
            "results" => ResultsSection,
            "quotation" => QuotationSection,
            "trace" => CalculationTraceSection,
            _ => null,
        };
        target?.StartBringIntoView();
    }

    private void SyncNavigationSelection(string section)
    {
        if (section == "settings")
        {
            return;
        }

        var navigationTag = section.StartsWith(
            "costing",
            StringComparison.Ordinal)
            ? "costing"
            : section;
        var matchingItem = AppNavigation.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => string.Equals(
                item.Tag as string,
                navigationTag,
                StringComparison.Ordinal));
        if (matchingItem is not null &&
            !ReferenceEquals(AppNavigation.SelectedItem, matchingItem))
        {
            AppNavigation.SelectedItem = matchingItem;
        }
    }

    private void ShowModulePlaceholder(string title, string description)
    {
        ViewModel.SetSection(title, description);
        ModulePlaceholderView.Visibility = Visibility.Visible;
    }

    private void DualModuleSelection_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (DualTapeFlow is null)
        {
            return;
        }

        DualTapeFlow.Visibility = Selected(DualTapeSelection);
        DualChalkFlow.Visibility = Selected(DualChalkSelection);
        DualFoilFlow.Visibility = Selected(DualFoilSelection);
        DualBraidFlow.Visibility = Selected(DualBraidSelection);
        DualLapscreenFlow.Visibility = Selected(DualLapscreenSelection);
        DualDrainFlow.Visibility = Selected(DualDrainSelection);
    }

    private static Visibility Selected(CheckBox checkBox) =>
        checkBox.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;

    private void UpdateStorageSetupVisibility()
    {
        if (AppRuntimeMode.IsPublicReview)
        {
            StorageSetupOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        StorageSetupOverlay.Visibility = ViewModel.IsStorageSetupVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async Task<CentralDataArea?> ChooseCentralDataAreaAsync()
    {
        var areaPicker = new ListView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedIndex = 0,
            SelectionMode = ListViewSelectionMode.Single,
            MaxHeight = 470,
        };
        var choices = new[]
        {
            (CentralDataArea.Copper, "Conductor constructions, suppliers, dimensions, yield, and prices"),
            (CentralDataArea.Compounds, "Insulation materials, suppliers, physical properties, and prices"),
            (CentralDataArea.Masterbatch, "Colours, compatibility, temperature limits, suppliers, and prices"),
            (CentralDataArea.Contacts, "Customer and delivery details used by costing and quotation"),
            (CentralDataArea.Operators, "Office and production people used by quotation and review"),
        };
        foreach (var (area, description) in choices)
        {
            var existing = CostingViewModel.GetDatabaseTableLink(area);
            var details = new StackPanel { Spacing = 3 };
            details.Children.Add(new TextBlock
            {
                Text = AreaName(area),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            });
            details.Children.Add(new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            details.Children.Add(new TextBlock
            {
                Text = existing is null
                    ? "Not linked · retained workbook data is available"
                    : $"Linked · {existing.TableName} · {existing.DisplayName}",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            areaPicker.Items.Add(new ListViewItem
            {
                Content = details,
                Tag = area,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(12, 10, 12, 10),
            });
        }

        var content = CreateWizardContent(
            step: 1,
            "Choose one of the five independent central-data areas. Every area remains available here after another table has been imported.");
        content.Children.Add(new TextBlock
        {
            Text = "Data to link",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        content.Children.Add(areaPicker);

        var dialog = CreateWizardWindow(
            "Central data setup · Choose data",
            content,
            primaryButtonText: "Next");
        var result = await dialog.ShowAsync();

        return result == ContentDialogResult.Primary
            ? areaPicker.SelectedItem is ListViewItem { Tag: CentralDataArea selectedArea }
                ? selectedArea
                : null
            : null;
    }

    private async Task<DatabaseLinkDraft?> ChooseDatabaseSourceAsync(
        CentralDataArea area,
        CentralDataTableLink? existingLink)
    {
        var sourceType = new ComboBox
        {
            Header = "Database type",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedIndex = existingLink?.SourceKind ==
                CentralDataSourceKind.SqlDatabase
                ? 1
                : 0,
        };
        sourceType.Items.Add("Microsoft Access (.accdb or .mdb)");
        sourceType.Items.Add("SQL Server");

        var accessPath = new TextBox
        {
            Header = "Access database",
            IsReadOnly = true,
            PlaceholderText = "Choose an Access database file",
            Text = existingLink?.AccessDatabasePath ?? string.Empty,
            TextWrapping = TextWrapping.Wrap,
        };
        var browseButton = new Button
        {
            Content = "Browse…",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        var accessPanel = new StackPanel
        {
            Spacing = 10,
        };
        accessPanel.Children.Add(accessPath);
        accessPanel.Children.Add(browseButton);

        var sqlServer = new TextBox
        {
            Header = "Server or instance",
            PlaceholderText = @"Example: SERVER\INSTANCE",
            Text = existingLink?.SqlServer ?? string.Empty,
        };
        var sqlDatabase = new TextBox
        {
            Header = "Database",
            PlaceholderText = "Central database name",
            Text = existingLink?.SqlDatabase ?? string.Empty,
        };
        var windowsAuthentication = new ToggleSwitch
        {
            Header = "Authentication",
            IsOn = existingLink?.UseWindowsAuthentication ?? true,
            OnContent = "Use Windows sign-in",
            OffContent = "SQL sign-in at update time",
        };
        var sqlUserName = new TextBox
        {
            Header = "SQL user name",
            PlaceholderText = "User name",
        };
        var sqlPassword = new PasswordBox
        {
            Header = "SQL password",
            PlaceholderText = "Used for this import only",
        };
        var credentialsNote = new TextBlock
        {
            Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"],
            Text = "SQL passwords are held only for this connection attempt and are never saved.",
            TextWrapping = TextWrapping.Wrap,
        };
        var sqlPanel = new StackPanel
        {
            Spacing = 10,
        };
        sqlPanel.Children.Add(sqlServer);
        sqlPanel.Children.Add(sqlDatabase);
        sqlPanel.Children.Add(windowsAuthentication);
        sqlPanel.Children.Add(sqlUserName);
        sqlPanel.Children.Add(sqlPassword);
        sqlPanel.Children.Add(credentialsNote);

        var content = CreateWizardContent(
            step: 2,
            $"Choose the database containing the {AreaName(area)} table. " +
            "Saving this link does not remove the cached material records.");
        content.Children.Add(sourceType);
        content.Children.Add(accessPanel);
        content.Children.Add(sqlPanel);

        var dialog = CreateWizardWindow(
            $"Central data setup · {AreaName(area)} source",
            content,
            primaryButtonText: "Next");

        void UpdateState()
        {
            var sqlSelected = sourceType.SelectedIndex == 1;
            accessPanel.Visibility = sqlSelected
                ? Visibility.Collapsed
                : Visibility.Visible;
            sqlPanel.Visibility = sqlSelected
                ? Visibility.Visible
                : Visibility.Collapsed;
            sqlUserName.Visibility = windowsAuthentication.IsOn
                ? Visibility.Collapsed
                : Visibility.Visible;
            sqlPassword.Visibility = windowsAuthentication.IsOn
                ? Visibility.Collapsed
                : Visibility.Visible;
            credentialsNote.Visibility = windowsAuthentication.IsOn
                ? Visibility.Collapsed
                : Visibility.Visible;
            dialog.IsPrimaryButtonEnabled = sqlSelected
                ? !string.IsNullOrWhiteSpace(sqlServer.Text) &&
                  !string.IsNullOrWhiteSpace(sqlDatabase.Text) &&
                  (windowsAuthentication.IsOn ||
                   !string.IsNullOrWhiteSpace(sqlUserName.Text) &&
                   !string.IsNullOrWhiteSpace(sqlPassword.Password))
                : !string.IsNullOrWhiteSpace(accessPath.Text);
        }

        sourceType.SelectionChanged += (_, _) => UpdateState();
        accessPath.TextChanged += (_, _) => UpdateState();
        sqlServer.TextChanged += (_, _) => UpdateState();
        sqlDatabase.TextChanged += (_, _) => UpdateState();
        sqlUserName.TextChanged += (_, _) => UpdateState();
        sqlPassword.PasswordChanged += (_, _) => UpdateState();
        windowsAuthentication.Toggled += (_, _) => UpdateState();
        browseButton.Click += async (_, _) =>
        {
            var selectedPath = await PickAccessDatabaseAsync();
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                accessPath.Text = selectedPath;
            }
        };
        UpdateState();

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        return sourceType.SelectedIndex == 1
            ? new DatabaseLinkDraft(
                area,
                CentralDataSourceKind.SqlDatabase,
                AccessDatabasePath: null,
                SqlServer: sqlServer.Text.Trim(),
                SqlDatabase: sqlDatabase.Text.Trim(),
                UseWindowsAuthentication: windowsAuthentication.IsOn,
                SqlUserName: windowsAuthentication.IsOn ? null : sqlUserName.Text.Trim(),
                SqlPassword: windowsAuthentication.IsOn ? null : sqlPassword.Password)
            : new DatabaseLinkDraft(
                area,
                CentralDataSourceKind.AccessDatabase,
                AccessDatabasePath: accessPath.Text,
                SqlServer: null,
                SqlDatabase: null,
                UseWindowsAuthentication: true,
                SqlUserName: null,
                SqlPassword: null);
    }

    private async Task<string?> PickAccessDatabaseAsync()
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                CommitButtonText = "Use this database",
            };
            picker.FileTypeFilter.Add(".accdb");
            picker.FileTypeFilter.Add(".mdb");
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                App.WindowHandle);

            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }
        catch (Exception exception)
        {
            Program.Log($"Access database selection failed: {exception}");
            CostingViewModel.CentralDataStatus =
                "The Access database could not be selected. The retained central-data snapshot is unchanged.";
            return null;
        }
    }

    private CentralDataWorkflowWindow CreateWizardWindow(
        string title,
        FrameworkElement content,
        string primaryButtonText) =>
        new(
            title,
            content,
            primaryButtonText,
            closeButtonText: "Cancel",
            requestedTheme: ActualTheme,
            size: CentralDataWorkflowWindowSize.Compact);

    private static StackPanel CreateWizardContent(
        int step,
        string instruction)
    {
        var content = new StackPanel
        {
            MinWidth = 560,
            Spacing = 14,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        content.Children.Add(
            new ProgressBar
            {
                Minimum = 0,
                Maximum = 3,
                Value = step,
            });
        content.Children.Add(
            new TextBlock
            {
                Text = $"Step {step} of 3",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            });
        content.Children.Add(
            new TextBlock
            {
                Text = instruction,
                TextWrapping = TextWrapping.Wrap,
            });
        return content;
    }

    private async Task<CentralDataTableLink?> ChooseExistingDataLinkAsync(
        string title = "Edit existing data link",
        string primaryButtonText = "Edit transform",
        string instruction = "Choose the saved table whose column transforms and costing-field matches you want to review. The previous retained table is only replaced after a successful import.",
        bool alwaysShowPicker = false)
    {
        var links = CostingViewModel.DatabaseTableLinks
            .OrderBy(link => link.Area)
            .ToArray();
        if (links.Length == 0)
        {
            await ShowCentralDataMessageAsync(
                "No saved link to edit",
                "Set up a data link first. The retained workbook and last-successful data remain available.");
            return null;
        }

        if (links.Length == 1 && !alwaysShowPicker)
        {
            return links[0];
        }

        var picker = new ComboBox
        {
            Header = "Saved data link",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedIndex = 0,
        };
        foreach (var link in links)
        {
            picker.Items.Add(new ComboBoxItem
            {
                Content = $"{AreaName(link.Area)} · {link.TableName} · {link.DisplayName}",
                Tag = link,
            });
        }

        var content = CreateWizardContent(step: 3, instruction);
        content.Children.Add(picker);
        var window = CreateWizardWindow(
            title,
            content,
            primaryButtonText: primaryButtonText);
        var result = await window.ShowAsync();
        return result == ContentDialogResult.Primary &&
               picker.SelectedItem is ComboBoxItem { Tag: CentralDataTableLink selectedLink }
            ? selectedLink
            : null;
    }

    private static string AreaName(CentralDataArea area) =>
        area switch
        {
            CentralDataArea.Copper => "Copper",
            CentralDataArea.Compounds => "Compounds",
            CentralDataArea.Masterbatch => "Masterbatch",
            CentralDataArea.Contacts => "Contacts",
            CentralDataArea.Operators => "Operators",
            _ => area.ToString(),
        };

    private sealed record DatabaseLinkDraft(
        CentralDataArea Area,
        CentralDataSourceKind SourceKind,
        string? AccessDatabasePath,
        string? SqlServer,
        string? SqlDatabase,
        bool UseWindowsAuthentication,
        string? SqlUserName,
        string? SqlPassword)
    {
        public static DatabaseLinkDraft FromLink(CentralDataTableLink link) => new(
            link.Area,
            link.SourceKind,
            link.AccessDatabasePath,
            link.SqlServer,
            link.SqlDatabase,
            link.UseWindowsAuthentication,
            SqlUserName: null,
            SqlPassword: null);

        public CentralDataDatabaseConnection ToConnection() => new(
            SourceKind,
            SourceKind == CentralDataSourceKind.AccessDatabase
                ? Path.GetFileName(AccessDatabasePath) ?? "Access database"
                : $"{SqlServer} / {SqlDatabase}",
            AccessDatabasePath,
            SqlServer,
            SqlDatabase,
            UseWindowsAuthentication,
            SqlUserName,
            SqlPassword);
    }

    private sealed record ProjectOpenSelection(
        SingleCoreProjectIndexEntry? IndexEntry,
        string? PortablePath);

    private void ApplyVisualStyle()
    {
        var requestedTheme = ViewModel.SelectedThemeIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        RequestedTheme = requestedTheme;

        if (App.Window is MainWindow mainWindow)
        {
            mainWindow.ApplyVisualStyle(
                requestedTheme,
                ViewModel.SelectedBackdropIndex);
        }
    }
}
