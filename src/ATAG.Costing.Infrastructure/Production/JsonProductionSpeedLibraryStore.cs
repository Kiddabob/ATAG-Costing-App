using System.Text.Json;
using ATAG.Costing.Application.Production;
using ATAG.Costing.Infrastructure.Storage;

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
    private ProductionSpeedLibraryState? _baseline;

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
            using var gate = SharedFileGate.Acquire(_statePath);
            var state = LoadFromFile();
            _baseline = state;
            return state;
        }
    }

    private ProductionSpeedLibraryState LoadFromFile()
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

    public void Save(ProductionSpeedLibraryState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_syncRoot)
        {
            using var gate = SharedFileGate.Acquire(_statePath);
            var requested = Normalize(state);
            var current = LoadFromFile();
            var normalized = Merge(
                _baseline ?? current,
                current,
                requested);
            var directory = Path.GetDirectoryName(_statePath)
                ?? throw new InvalidOperationException(
                    "The production-speed library path has no parent directory.");
            Directory.CreateDirectory(directory);

            var temporaryPath = $"{_statePath}.{Guid.NewGuid():N}.tmp";
            var json = JsonSerializer.Serialize(normalized, SerializerOptions);
            try
            {
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, _statePath, overwrite: true);
                _baseline = normalized;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }

    private static ProductionSpeedLibraryState Merge(
        ProductionSpeedLibraryState baseline,
        ProductionSpeedLibraryState current,
        ProductionSpeedLibraryState requested)
    {
        var baselineById = baseline.Lines.ToDictionary(
            line => line.Id,
            StringComparer.OrdinalIgnoreCase);
        var requestedById = requested.Lines.ToDictionary(
            line => line.Id,
            StringComparer.OrdinalIgnoreCase);
        var result = current.Lines.ToDictionary(
            line => line.Id,
            StringComparer.OrdinalIgnoreCase);

        foreach (var baselineId in baselineById.Keys)
        {
            if (!requestedById.ContainsKey(baselineId))
            {
                result.Remove(baselineId);
            }
        }

        foreach (var requestedLine in requested.Lines)
        {
            if (!baselineById.TryGetValue(requestedLine.Id, out var baselineLine) ||
                !Equivalent(baselineLine, requestedLine))
            {
                result[requestedLine.Id] = requestedLine;
            }
        }

        return requested with
        {
            Lines = result.Values.ToArray(),
        };
    }

    private static bool Equivalent(
        ProductionLineDefinition left,
        ProductionLineDefinition right) =>
        string.Equals(
            JsonSerializer.Serialize(left, SerializerOptions),
            JsonSerializer.Serialize(right, SerializerOptions),
            StringComparison.Ordinal);

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
