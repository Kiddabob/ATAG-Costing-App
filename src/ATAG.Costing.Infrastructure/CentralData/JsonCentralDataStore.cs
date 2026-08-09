using System.Text.Json;
using ATAG.Costing.Application.CentralData;

namespace ATAG.Costing.Infrastructure.CentralData;

/// <summary>
/// Persists successful central-data imports independently of the linked source.
/// A clean install starts empty; source-controlled binaries never contain
/// business rows. Each successfully imported area is retained in LocalAppData.
/// </summary>
public sealed class JsonCentralDataStore : ICentralDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly Lock _syncRoot = new();
    private readonly string _statePath;

    public JsonCentralDataStore(string? statePath = null)
    {
        _statePath = statePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ATAG Design Ltd",
            "ATAG Costing",
            "central-data-state.json");
    }

    public CentralDataState Load()
    {
        lock (_syncRoot)
        {
            return NormalizeOrSeed(TryLoad());
        }
    }

    public void SaveConfiguration(CentralDataSourceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        lock (_syncRoot)
        {
            var current = NormalizeOrSeed(TryLoad());
            Save(current with { Configuration = configuration });
        }
    }

    public void SaveTableLink(CentralDataTableLink link)
    {
        ArgumentNullException.ThrowIfNull(link);

        lock (_syncRoot)
        {
            var current = LoadCurrentOrSeed();
            var links = current.EffectiveTableLinks
                .Where(existing => existing.Area != link.Area)
                .Append(link)
                .OrderBy(existing => existing.Area)
                .ToArray();

            Save(current with { TableLinks = links });
        }
    }

    public void RemoveTableLink(CentralDataArea area)
    {
        lock (_syncRoot)
        {
            var current = LoadCurrentOrSeed();
            var links = current.EffectiveTableLinks
                .Where(existing => existing.Area != area)
                .ToArray();

            // Removing a refresh link must not remove either the transformed
            // source table or the last validated costing snapshot.
            Save(current with { TableLinks = links });
        }
    }

    public void SaveSnapshot(CentralDataSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!HasAnyData(snapshot))
        {
            throw new ArgumentException(
                "A central-data snapshot must contain at least one imported row.",
                nameof(snapshot));
        }

        lock (_syncRoot)
        {
            var current = LoadCurrentOrSeed();
            Save(current with { Snapshot = snapshot });
        }
    }

    public void SaveImportedTable(
        CentralDataTableLink link,
        CentralDataSnapshot snapshot,
        CentralDataRetainedTable retainedTable)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(retainedTable);

        if (retainedTable.Area != link.Area)
        {
            throw new ArgumentException(
                "The retained table and table link must describe the same central-data area.",
                nameof(retainedTable));
        }

        if (!HasAreaData(snapshot, link.Area))
        {
            throw new ArgumentException(
                $"The validated snapshot does not contain imported {link.Area} rows.",
                nameof(snapshot));
        }

        lock (_syncRoot)
        {
            var current = LoadCurrentOrSeed();
            var links = current.EffectiveTableLinks
                .Where(existing => existing.Area != link.Area)
                .Append(link)
                .OrderBy(existing => existing.Area)
                .ToArray();
            var retainedTables = current.EffectiveRetainedTables
                .Where(existing => existing.Area != retainedTable.Area)
                .Append(retainedTable)
                .OrderBy(existing => existing.Area)
                .ToArray();
            Save(current with
            {
                Snapshot = snapshot,
                TableLinks = links,
                RetainedTables = retainedTables,
            });
        }
    }

    private CentralDataState LoadCurrentOrSeed()
        => NormalizeOrSeed(TryLoad());

    private static CentralDataState NormalizeOrSeed(
        CentralDataState? state)
    {
        if (state is null)
        {
            return InitialCentralDataState.Create();
        }

        return NormalizeLegacyConfiguration(
            state with { Snapshot = NormalizeSnapshot(state.Snapshot) });
    }

    private static CentralDataSnapshot NormalizeSnapshot(
        CentralDataSnapshot snapshot) =>
        snapshot with
        {
            SchemaVersion = Math.Max(2, snapshot.SchemaVersion),
            Revision = string.IsNullOrWhiteSpace(snapshot.Revision)
                ? "retained-local-data"
                : snapshot.Revision,
            SourceLabel = string.IsNullOrWhiteSpace(snapshot.SourceLabel)
                ? "Retained local central data"
                : snapshot.SourceLabel,
            Copper = snapshot.Copper ?? [],
            Compounds = snapshot.Compounds ?? [],
            Masterbatches = snapshot.Masterbatches ?? [],
            Contacts = snapshot.Contacts ?? [],
            Operators = snapshot.Operators ?? [],
        };

    private static CentralDataState NormalizeLegacyConfiguration(
        CentralDataState state) =>
        state.Configuration.Kind == CentralDataSourceKind.LinkedWorkbook
            ? state with
            {
                Configuration = CentralDataSourceConfiguration.Unconfigured,
            }
            : state;

    private CentralDataState? TryLoad()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return null;
            }

            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<CentralDataState>(
                json,
                SerializerOptions);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void Save(CentralDataState state)
    {
        var directory = Path.GetDirectoryName(_statePath)
            ?? throw new InvalidOperationException(
                "The central-data cache path has no parent directory.");

        Directory.CreateDirectory(directory);

        var temporaryPath = $"{_statePath}.tmp";
        var json = JsonSerializer.Serialize(state, SerializerOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, _statePath, overwrite: true);
    }

    private static bool HasAnyData(CentralDataSnapshot snapshot) =>
        snapshot.Copper.Count > 0 ||
        snapshot.Compounds.Count > 0 ||
        snapshot.Masterbatches.Count > 0 ||
        snapshot.EffectiveContacts.Count > 0 ||
        snapshot.EffectiveOperators.Count > 0;

    private static bool HasAreaData(
        CentralDataSnapshot snapshot,
        CentralDataArea area) =>
        area switch
        {
            CentralDataArea.Copper => snapshot.Copper.Count > 0,
            CentralDataArea.Compounds => snapshot.Compounds.Count > 0,
            CentralDataArea.Masterbatch => snapshot.Masterbatches.Count > 0,
            CentralDataArea.Contacts => snapshot.EffectiveContacts.Count > 0,
            CentralDataArea.Operators => snapshot.EffectiveOperators.Count > 0,
            _ => false,
        };
}
