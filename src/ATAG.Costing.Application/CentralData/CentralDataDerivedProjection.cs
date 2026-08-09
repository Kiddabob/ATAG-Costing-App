namespace ATAG.Costing.Application.CentralData;

/// <summary>
/// Applies new derivation rules to an already-retained full source table. This
/// lets cached/offline links benefit immediately without rewriting source cells
/// or requiring a live database refresh.
/// </summary>
public static class CentralDataDerivedProjection
{
    public static CentralDataState Complete(CentralDataState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var retained = state.EffectiveRetainedTables.FirstOrDefault(table =>
            table.Area == CentralDataArea.Copper);
        var link = state.EffectiveTableLinks.FirstOrDefault(tableLink =>
            tableLink.Area == CentralDataArea.Copper);
        if (retained is null || retained.Rows.Count == 0)
        {
            return state;
        }

        var mappings = CentralDataImportSchema
            .Match(
                CentralDataArea.Copper,
                retained.Columns,
                link?.ColumnMappings)
            .Where(match => match.IsResolved)
            .ToDictionary(
                match => match.Field.Key,
                match => match.SourceColumn!,
                StringComparer.OrdinalIgnoreCase);
        if (!mappings.TryGetValue("Description", out var descriptionColumn))
        {
            return state;
        }

        mappings.TryGetValue("Supplier", out var supplierColumn);
        var rowsByDescription = retained.Rows
            .Where(row => !string.IsNullOrWhiteSpace(
                row.Cell(descriptionColumn).Value))
            .GroupBy(
                row => row.Cell(descriptionColumn).Value!.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var completed = state.Snapshot.Copper
            .Select(copper =>
            {
                if (!rowsByDescription.TryGetValue(
                        copper.Description,
                        out var candidates))
                {
                    return CopperReferenceDeriver.FillMissing(
                        copper,
                        new CopperReferenceDerivationInputs());
                }

                var row = candidates.FirstOrDefault(candidate =>
                              string.IsNullOrWhiteSpace(supplierColumn) ||
                              string.Equals(
                                  candidate.Cell(supplierColumn).Value?.Trim(),
                                  copper.Supplier,
                                  StringComparison.OrdinalIgnoreCase)) ??
                          candidates[0];
                return CopperReferenceDeriver.FillMissing(
                    copper,
                    row,
                    mappings);
            })
            .ToArray();

        return state with
        {
            Snapshot = state.Snapshot with { Copper = completed },
        };
    }
}
