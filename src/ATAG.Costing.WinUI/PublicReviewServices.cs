using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Application.Preferences;
using ATAG.Costing.Infrastructure.CentralData;

namespace ATAG.Costing.WinUI;

/// <summary>
/// Public-review services deliberately have no filesystem or database backing.
/// This keeps the review executable independent from retained ATAG data on the
/// computer where it is opened.
/// </summary>
internal sealed class PublicReviewCentralDataStore : ICentralDataStore
{
    private const string ReadOnlyMessage =
        "Database links are disabled in the public review build.";

    public CentralDataState Load() => InitialCentralDataState.Create();

    public void SaveConfiguration(CentralDataSourceConfiguration configuration) =>
        throw new InvalidOperationException(ReadOnlyMessage);

    public void SaveTableLink(CentralDataTableLink link) =>
        throw new InvalidOperationException(ReadOnlyMessage);

    public void RemoveTableLink(CentralDataArea area) =>
        throw new InvalidOperationException(ReadOnlyMessage);

    public void SaveSnapshot(CentralDataSnapshot snapshot) =>
        throw new InvalidOperationException(ReadOnlyMessage);

    public void SaveImportedTable(
        CentralDataTableLink link,
        CentralDataSnapshot snapshot,
        CentralDataRetainedTable retainedTable) =>
        throw new InvalidOperationException(ReadOnlyMessage);
}

internal sealed class PublicReviewAppPreferencesService : IAppPreferencesService
{
    private static readonly AppPreferences Preferences = new(
        SaveFolderPath: null,
        ShowStorageSetupOnStartup: false,
        ThemeMode: "System",
        BackdropMode: "Mica",
        HasCompletedFirstRunSetup: true,
        AccentColour: "Coral",
        CustomAccentHex: "#F78370");

    public AppPreferences Load() => Preferences;

    public void Save(AppPreferences preferences)
    {
        // Public review never reads or writes the installed app's preferences.
    }
}
