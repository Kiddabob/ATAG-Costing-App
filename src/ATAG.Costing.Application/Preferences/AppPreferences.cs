namespace ATAG.Costing.Application.Preferences;

/// <summary>
/// User-level preferences that must be available before a quote database or
/// document folder has been opened.
/// </summary>
public sealed record AppPreferences(
    string? SaveFolderPath,
    bool ShowStorageSetupOnStartup,
    string ThemeMode = "System",
    string BackdropMode = "Mica",
    bool HasCompletedFirstRunSetup = false,
    bool AutomaticallyCheckForUpdates = true,
    string UpdateChannel = "Stable")
{
    public static AppPreferences Default { get; } = new(
        SaveFolderPath: null,
        ShowStorageSetupOnStartup: true,
        ThemeMode: "System",
        BackdropMode: "Mica",
        HasCompletedFirstRunSetup: false,
        AutomaticallyCheckForUpdates: true,
        UpdateChannel: "Stable");
}
