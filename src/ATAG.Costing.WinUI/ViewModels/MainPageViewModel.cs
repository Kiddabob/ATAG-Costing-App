using CommunityToolkit.Mvvm.ComponentModel;
using ATAG.Costing.Application.Preferences;

namespace ATAG.Costing.WinUI.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private static readonly string[] AccentNames =
    [
        "Coral",
        "Blue",
        "Cyan",
        "Green",
        "Purple",
        "Gold",
        "Custom",
    ];

    private static readonly string[] AccentHexValues =
    [
        "#F78370",
        "#1679B8",
        "#43B8D4",
        "#2E9D62",
        "#8B6FD6",
        "#C88A04",
    ];

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
    public partial int SelectedAccentIndex { get; set; }

    [ObservableProperty]
    public partial string CustomAccentHex { get; set; } = "#F78370";

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
        SelectedAccentIndex = Array.IndexOf(AccentNames, preferences.AccentColour) is var accentIndex && accentIndex >= 0
            ? accentIndex
            : 0;
        CustomAccentHex = NormalizeHex(preferences.CustomAccentHex) ?? "#F78370";
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

    partial void OnSelectedAccentIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ResolvedAccentHex));
        OnPropertyChanged(nameof(SelectedAccentName));
        if (!_isLoading)
        {
            SavePreferences();
        }
    }

    partial void OnCustomAccentHexChanged(string value)
    {
        OnPropertyChanged(nameof(ResolvedAccentHex));
        if (!_isLoading)
        {
            SavePreferences();
        }
    }

    public string ResolvedAccentHex =>
        SelectedAccentIndex >= 0 && SelectedAccentIndex < AccentHexValues.Length
            ? AccentHexValues[SelectedAccentIndex]
            : NormalizeHex(CustomAccentHex) ?? "#F78370";

    public string SelectedAccentName =>
        SelectedAccentIndex >= 0 && SelectedAccentIndex < AccentNames.Length
            ? AccentNames[SelectedAccentIndex]
            : "Coral";

    public bool TryUseCustomAccent(string value)
    {
        var normalized = NormalizeHex(value);
        if (normalized is null)
        {
            return false;
        }

        _isLoading = true;
        CustomAccentHex = normalized;
        SelectedAccentIndex = AccentNames.Length - 1;
        _isLoading = false;
        SavePreferences();
        OnPropertyChanged(nameof(ResolvedAccentHex));
        OnPropertyChanged(nameof(SelectedAccentName));
        return true;
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
                : "Stable",
            AccentColour: SelectedAccentName,
            CustomAccentHex: NormalizeHex(CustomAccentHex) ?? "#F78370"));
    }

    private static string? NormalizeHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('#'))
        {
            trimmed = $"#{trimmed}";
        }

        if (trimmed.Length != 7 ||
            !trimmed[1..].All(Uri.IsHexDigit))
        {
            return null;
        }

        return trimmed.ToUpperInvariant();
    }
}
