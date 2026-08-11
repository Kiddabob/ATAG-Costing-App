using System.Text.Json;
using ATAG.Costing.Application.Production;

namespace ATAG.Costing.Infrastructure.Production;

/// <summary>
/// Retains user-defined production lines and measured cable runs outside the
/// replaceable application folder. No production rows are shipped in release
/// packages or committed to source control.
/// </summary>
public sealed class JsonProductionSpeedLibraryStore : IProductionSpeedLibraryStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly Lock _syncRoot = new();
    private readonly string _statePath;

    public JsonProductionSpeedLibraryStore(string? statePath = null)
    {
        _statePath = statePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ATAG Design Ltd",
            "ATAG Costing",
            "production-speed-library.json");
    }

    public ProductionSpeedLibraryState Load()
    {
        lock (_syncRoot)
        {
            try
            {
                if (!File.Exists(_statePath))
                {
                    return ProductionSpeedLibraryDefaults.Empty();
                }

                var json = File.ReadAllText(_statePath);
                var state = JsonSerializer.Deserialize<ProductionSpeedLibraryState>(
                    json,
                    SerializerOptions);
                return state is null
                    ? ProductionSpeedLibraryDefaults.Empty()
                    : Normalize(state);
            }
            catch (IOException)
            {
                return ProductionSpeedLibraryDefaults.Empty();
            }
            catch (UnauthorizedAccessException)
            {
                return ProductionSpeedLibraryDefaults.Empty();
            }
            catch (JsonException)
            {
                return ProductionSpeedLibraryDefaults.Empty();
            }
        }
    }

    public void Save(ProductionSpeedLibraryState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_syncRoot)
        {
            var normalized = Normalize(state);
            var directory = Path.GetDirectoryName(_statePath)
                ?? throw new InvalidOperationException(
                    "The production-speed library path has no parent directory.");
            Directory.CreateDirectory(directory);

            var temporaryPath = $"{_statePath}.tmp";
            var json = JsonSerializer.Serialize(normalized, SerializerOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _statePath, overwrite: true);
        }
    }

    private static ProductionSpeedLibraryState Normalize(
        ProductionSpeedLibraryState state)
    {
        var lines = (state.Lines ?? [])
            .Where(line => line is not null)
            .Select(NormalizeLine)
            .Where(line => !string.IsNullOrWhiteSpace(line.Id))
            .ToArray();
        return state with
        {
            SchemaVersion = ProductionSpeedLibraryState.CurrentSchemaVersion,
            Lines = lines,
        };
    }

    private static ProductionLineDefinition NormalizeLine(
        ProductionLineDefinition line) =>
        line with
        {
            Id = line.Id?.Trim() ?? "",
            Name = string.IsNullOrWhiteSpace(line.Name)
                ? "Unnamed production line"
                : line.Name.Trim(),
            SpeedBands = (line.SpeedBands ?? [])
                .Where(band => band is not null)
                .OrderBy(band => band.MaximumFinishedOutsideDiameterMillimetres)
                .ToArray(),
            Observations = (line.Observations ?? [])
                .Where(observation => observation is not null)
                .ToArray(),
        };
}
