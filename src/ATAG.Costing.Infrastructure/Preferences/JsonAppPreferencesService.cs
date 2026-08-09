using System.Text.Json;
using ATAG.Costing.Application.Preferences;

namespace ATAG.Costing.Infrastructure.Preferences;

/// <summary>
/// Stores lightweight startup preferences independently from the user-selected
/// costing document location.
/// </summary>
public sealed class JsonAppPreferencesService : IAppPreferencesService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly Lock _syncRoot = new();
    private readonly string _settingsPath;

    public JsonAppPreferencesService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ATAG Design Ltd",
            "ATAG Costing",
            "settings.json");
    }

    public AppPreferences Load()
    {
        lock (_syncRoot)
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    return AppPreferences.Default;
                }

                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppPreferences>(json, SerializerOptions)
                    ?? AppPreferences.Default;
            }
            catch (IOException)
            {
                return AppPreferences.Default;
            }
            catch (UnauthorizedAccessException)
            {
                return AppPreferences.Default;
            }
            catch (JsonException)
            {
                return AppPreferences.Default;
            }
        }
    }

    public void Save(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        lock (_syncRoot)
        {
            var settingsDirectory = Path.GetDirectoryName(_settingsPath)
                ?? throw new InvalidOperationException("The settings path has no parent directory.");

            Directory.CreateDirectory(settingsDirectory);

            var temporaryPath = $"{_settingsPath}.tmp";
            var json = JsonSerializer.Serialize(preferences, SerializerOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
    }
}
