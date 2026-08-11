using ATAG.Costing.Application.Preferences;
using ATAG.Costing.Infrastructure.Preferences;
using Xunit;

namespace ATAG.Costing.Application.Tests.Preferences;

public sealed class JsonAppPreferencesServiceTests
{
    [Fact]
    public void Load_OlderSettingsWithoutUpdateFields_UsesSafeDefaults()
    {
        var path = CreateTemporarySettingsPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(
                path,
                """
                {
                  "SaveFolderPath": null,
                  "ShowStorageSetupOnStartup": true,
                  "ThemeMode": "Dark",
                  "BackdropMode": "Mica",
                  "HasCompletedFirstRunSetup": false
                }
                """);

            var loaded = new JsonAppPreferencesService(path).Load();

            Assert.True(loaded.AutomaticallyCheckForUpdates);
            Assert.Equal("Stable", loaded.UpdateChannel);
            Assert.Equal("Coral", loaded.AccentColour);
            Assert.Equal("#F78370", loaded.CustomAccentHex);
        }
        finally
        {
            DeleteTemporarySettingsDirectory(path);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsUpdatePreferences()
    {
        var path = CreateTemporarySettingsPath();
        try
        {
            var service = new JsonAppPreferencesService(path);
            service.Save(AppPreferences.Default with
            {
                AutomaticallyCheckForUpdates = false,
                UpdateChannel = "Beta",
                AccentColour = "Purple",
                CustomAccentHex = "#654321",
            });

            var loaded = service.Load();

            Assert.False(loaded.AutomaticallyCheckForUpdates);
            Assert.Equal("Beta", loaded.UpdateChannel);
            Assert.Equal("Purple", loaded.AccentColour);
            Assert.Equal("#654321", loaded.CustomAccentHex);
        }
        finally
        {
            DeleteTemporarySettingsDirectory(path);
        }
    }

    private static string CreateTemporarySettingsPath() =>
        Path.Combine(
            Path.GetTempPath(),
            "ATAG-Costing-Tests",
            Guid.NewGuid().ToString("N"),
            "settings.json");

    private static void DeleteTemporarySettingsDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
