namespace ATAG.Costing.Application.Production;

public sealed record ProductionSpeedLibraryState
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public IReadOnlyList<ProductionLineDefinition> Lines { get; init; } = [];
}

public sealed record ProductionLineDefinition
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public bool IsActive { get; init; } = true;

    public decimal AboveMaximumLineSpeedMetresPerHour { get; init; }

    public IReadOnlyList<ProductionSpeedBandDefinition> SpeedBands { get; init; } = [];

    public IReadOnlyList<ProductionRunObservation> Observations { get; init; } = [];
}

public sealed record ProductionSpeedBandDefinition
{
    public string Id { get; init; } = "";

    public decimal MaximumFinishedOutsideDiameterMillimetres { get; init; }

    public decimal LineSpeedMetresPerHour { get; init; }
}

public sealed record ProductionRunObservation
{
    public string Id { get; init; } = "";

    public string CableReference { get; init; } = "";

    public string ProcessName { get; init; } = "";

    public decimal CoreOutsideDiameterMillimetres { get; init; }

    public decimal CoreOutsideDiameterToleranceMillimetres { get; init; }

    public decimal FinishedOutsideDiameterMillimetres { get; init; }

    public decimal FinishedOutsideDiameterToleranceMillimetres { get; init; }

    public decimal? CapstanSetting { get; init; }

    public decimal? ExtruderSetting { get; init; }

    public decimal? MeasuredLineSpeedMetresPerHour { get; init; }

    public decimal? ProducedLengthMetres { get; init; }

    public decimal? RunningTimeMinutes { get; init; }

    public string Notes { get; init; } = "";
}

public static class ProductionSpeedLibraryDefaults
{
    public static ProductionSpeedLibraryState Empty() => new();

    /// <summary>
    /// Creates the general insulation rule only after the user explicitly asks
    /// for it. A clean installation deliberately starts with no production
    /// rows or machine-specific values.
    /// </summary>
    public static ProductionLineDefinition CreateGeneralInsulationStarterLine(
        string? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Name = "General insulation starter profile",
            AboveMaximumLineSpeedMetresPerHour = 700m,
            SpeedBands =
            [
                Band("general-band-100", 1.00m, 15000m),
                Band("general-band-120", 1.20m, 13000m),
                Band("general-band-200", 2.00m, 8000m),
                Band("general-band-250", 2.50m, 6000m),
            ],
        };

    private static ProductionSpeedBandDefinition Band(
        string id,
        decimal maximumOutsideDiameter,
        decimal speed) =>
        new()
        {
            Id = id,
            MaximumFinishedOutsideDiameterMillimetres = maximumOutsideDiameter,
            LineSpeedMetresPerHour = speed,
        };
}
