using CommunityToolkit.Mvvm.ComponentModel;
using ATAG.Costing.Application.Preferences;

namespace ATAG.Costing.WinUI.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IAppPreferencesService _preferencesService;
    private bool _isLoading;

    [ObservableProperty]
    public partial string StorageFolderPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StorageFolderDisplay { get; set; } = "No folder selected";

    [ObservableProperty]
    public partial bool HasStorageFolder { get; set; }

    [ObservableProperty]
    public partial bool HasCompletedFirstRunSetup { get; set; }

    [ObservableProperty]
    public partial bool HasAllRequiredCentralData { get; set; }

    [ObservableProperty]
    public partial bool CanContinueSetup { get; set; }

    [ObservableProperty]
    public partial string CentralDataSetupStatus { get; set; } =
        "0 of 5 tables linked and imported";

    [ObservableProperty]
    public partial string CentralDataMissingAreasDisplay { get; set; } =
        "Still required: Copper, Compounds, Masterbatch, Contacts and Operators.";

    [ObservableProperty]
    public partial bool ShowStorageSetupOnStartup { get; set; } = true;

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedBackdropIndex { get; set; }

    [ObservableProperty]
    public partial bool AutomaticallyCheckForUpdates { get; set; } = true;

    [ObservableProperty]
    public partial int SelectedUpdateChannelIndex { get; set; }

    [ObservableProperty]
    public partial bool IsStorageSetupVisible { get; set; }

    [ObservableProperty]
    public partial string StorageStatusMessage { get; set; } =
        "Choose a folder to continue.";

    [ObservableProperty]
    public partial string PageTitle { get; set; } = "Home";

    [ObservableProperty]
    public partial string PageDescription { get; set; } =
        "Start a costing, review recent work, or manage the calculation data.";

    public MainPageViewModel(IAppPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;

        _isLoading = true;
        var preferences = _preferencesService.Load();
        StorageFolderPath = preferences.SaveFolderPath ?? string.Empty;
        ShowStorageSetupOnStartup = preferences.ShowStorageSetupOnStartup;
        HasCompletedFirstRunSetup = preferences.HasCompletedFirstRunSetup;
        SelectedThemeIndex = preferences.ThemeMode switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };
        SelectedBackdropIndex = preferences.BackdropMode == "Acrylic" ? 1 : 0;
        AutomaticallyCheckForUpdates = preferences.AutomaticallyCheckForUpdates;
        SelectedUpdateChannelIndex = preferences.UpdateChannel == "Beta" ? 1 : 0;
        RefreshStorageAvailability();
        _isLoading = false;
    }

    public void EvaluateStartupPrompt()
    {
        RefreshStorageAvailability();
        IsStorageSetupVisible =
            !HasCompletedFirstRunSetup ||
            ShowStorageSetupOnStartup ||
            !HasStorageFolder;
    }

    public void SetStorageFolder(string path)
    {
        StorageFolderPath = path;
        RefreshStorageAvailability();
        SavePreferences();
    }

    public void ShowStorageSetup()
    {
        RefreshStorageAvailability();
        IsStorageSetupVisible = true;
    }

    public void DismissStorageSetup()
    {
        if (!CanContinueSetup)
        {
            return;
        }

        if (!HasCompletedFirstRunSetup)
        {
            HasCompletedFirstRunSetup = true;
            SavePreferences();
        }

        IsStorageSetupVisible = false;
    }

    public void SetCentralDataSetupState(
        int completedAreaCount,
        IReadOnlyCollection<string> missingAreas)
    {
        HasAllRequiredCentralData = completedAreaCount >= 5;
        CentralDataSetupStatus =
            $"{Math.Clamp(completedAreaCount, 0, 5)} of 5 tables linked and imported";
        CentralDataMissingAreasDisplay = missingAreas.Count == 0
            ? "Copper, Compounds, Masterbatch, Contacts and Operators are ready."
            : $"Still required: {string.Join(", ", missingAreas)}.";
        RefreshSetupReadiness();
    }

    public void SetSection(string title, string description)
    {
        PageTitle = title;
        PageDescription = description;
    }

    public void SetStorageStatus(string message)
    {
        StorageStatusMessage = message;
    }

    partial void OnShowStorageSetupOnStartupChanged(bool value)
    {
        if (!_isLoading)
        {
            SavePreferences();
        }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (!_isLoading)
        {
            SavePreferences();
        }
    }

    partial void OnSelectedBackdropIndexChanged(int value)
    {
        if (!_isLoading)
        {
            SavePreferences();
        }
    }

    partial void OnAutomaticallyCheckForUpdatesChanged(bool value)
    {
        if (!_isLoading)
        {
            SavePreferences();
        }
    }

    partial void OnSelectedUpdateChannelIndexChanged(int value)
    {
        if (!_isLoading)
        {
            SavePreferences();
        }
    }

    private void RefreshStorageAvailability()
    {
        HasStorageFolder = StorageLocationPolicy.IsAvailable(StorageFolderPath);
        StorageFolderDisplay = HasStorageFolder
            ? StorageFolderPath
            : "No folder selected";

        StorageStatusMessage = HasStorageFolder
            ? "Folder ready. Costings, quotes, reports and backups will use this location."
            : "Choose an available folder to continue.";
        RefreshSetupReadiness();
    }

    private void RefreshSetupReadiness()
    {
        CanContinueSetup =
            HasStorageFolder &&
            (HasCompletedFirstRunSetup || HasAllRequiredCentralData);
    }

    private void SavePreferences()
    {
        _preferencesService.Save(new AppPreferences(
            SaveFolderPath: string.IsNullOrWhiteSpace(StorageFolderPath)
                ? null
                : StorageFolderPath,
            ShowStorageSetupOnStartup,
            ThemeMode: SelectedThemeIndex switch
            {
                1 => "Light",
                2 => "Dark",
                _ => "System"
            },
            BackdropMode: SelectedBackdropIndex == 1
                ? "Acrylic"
                : "Mica",
            HasCompletedFirstRunSetup,
            AutomaticallyCheckForUpdates,
            UpdateChannel: SelectedUpdateChannelIndex == 1
                ? "Beta"
                : "Stable"));
    }
}
