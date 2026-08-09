using ATAG.Costing.Application.CentralData;
using ATAG.Costing.Infrastructure.CentralData;
using Xunit;

namespace ATAG.Costing.Application.Tests.CentralData;

public sealed class CentralDataNavigationTests
{
    [Theory]
    [InlineData("#DIV/0!")]
    [InlineData(" #div/0! ")]
    [InlineData("DIVISION BY ZERO")]
    public void PreviewCell_TreatsDivisionByZeroAsANonBlockingBlank(string value)
    {
        var cell = CentralDataPreviewCell.FromValue(value);

        Assert.True(cell.HasError);
        Assert.Equal(CentralDataCellErrorKind.DivisionByZero, cell.ErrorKind);
        Assert.Null(cell.Value);
        Assert.Contains("Ignored", cell.DisplayValue, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QuerySteps_KeepAValidRowThatAlsoContainsAnIgnoredError()
    {
        var preview = Preview(
            ["Compound", "Cost (£/kg)"],
            [
                [CentralDataPreviewCell.FromValue("  Valid PVC  "), CentralDataPreviewCell.FromValue("#DIV/0!")],
                [CentralDataPreviewCell.FromValue("  "), CentralDataPreviewCell.FromValue(null)],
            ]);

        var transformed = preview.Apply(
        [
            new CentralDataQueryStep(CentralDataQueryStepKind.TrimText, "Trim", "", true),
            new CentralDataQueryStep(CentralDataQueryStepKind.RemoveBlankRows, "Blank", "", true),
        ]);

        var row = Assert.Single(transformed.Rows);
        Assert.Equal("Valid PVC", row.Cell("Compound").Value);
        Assert.Equal(CentralDataCellErrorKind.DivisionByZero, row.Cell("Cost (£/kg)").ErrorKind);
    }

    [Fact]
    public void ImportSchema_AutomaticallyMatchesTheAccessCompoundHeaders()
    {
        var matches = CentralDataImportSchema.Match(
            CentralDataArea.Compounds,
            ["ID", "Compound", "Company", "Cost (£/kg)", "Specific Gravity", "Type", "Material Description"]);

        Assert.True(CentralDataImportSchema.HasAllRequiredMatches(matches));
        Assert.Equal(
            "Cost (£/kg)",
            matches.Single(match => match.Field.Key == "PricePerKilogram").SourceColumn);
        Assert.All(
            matches.Where(match => match.Field.IsRequired),
            match => Assert.True(match.WasAutomaticallyMatched));
    }

    [Fact]
    public void ImportSchema_UsesAccessColumnDescriptionWhenThePhysicalNameIsNominal()
    {
        var columns = new[]
        {
            new CentralDataPreviewColumn("Description", "Text", 0, true),
            new CentralDataPreviewColumn("Company", "Text", 1, true),
            new CentralDataPreviewColumn("Total Cost", "Currency", 2, true),
            new CentralDataPreviewColumn("Yield", "Double", 3, true),
            new CentralDataPreviewColumn(
                "Nominal",
                "Double",
                4,
                true,
                SourceName: "Nominal",
                Description: "Nom OD (mm)"),
        };

        var matches = CentralDataImportSchema.Match(CentralDataArea.Copper, columns);

        Assert.True(CentralDataImportSchema.HasAllRequiredMatches(matches));
        Assert.Equal(
            "Nominal",
            matches.Single(match =>
                match.Field.Key == "NominalOutsideDiameterMillimetres").SourceColumn);
    }

    [Fact]
    public void QuerySteps_RenameAndRemoveColumnsWithoutDiscardingUnrelatedData()
    {
        var preview = Preview(
            ["Description", "Nominal", "Legacy value"],
            [[
                CentralDataPreviewCell.FromValue("7/0.20 TCW"),
                CentralDataPreviewCell.FromValue("0.58"),
                CentralDataPreviewCell.FromValue("keep me out of the projection"),
            ]]);

        var transformed = preview.Apply(
        [
            new CentralDataQueryStep(
                CentralDataQueryStepKind.RemoveColumn,
                "Remove Legacy value",
                "Remove the unused source column.",
                SourceColumn: "Legacy value"),
            new CentralDataQueryStep(
                CentralDataQueryStepKind.RenameColumn,
                "Rename Nominal",
                "Rename Nominal to Nom OD (mm).",
                SourceColumn: "Nominal",
                TargetColumn: "Nom OD (mm)"),
        ]);

        Assert.Equal(["Description", "Nom OD (mm)"], transformed.Columns.Select(column => column.Name));
        Assert.Equal("0.58", Assert.Single(transformed.Rows).Cell("Nom OD (mm)").Value);
        Assert.Equal("Nominal", transformed.Columns[1].EffectiveSourceName);
        Assert.DoesNotContain("Legacy value", transformed.Rows[0].Cells.Keys);
    }

    [Fact]
    public void QuerySteps_FilterRowsKeepsOnlyMatchingOfficeOperators()
    {
        var preview = Preview(
            ["First Name", "Office"],
            [
                [
                    CentralDataPreviewCell.FromValue("Laura"),
                    CentralDataPreviewCell.FromValue("True"),
                ],
                [
                    CentralDataPreviewCell.FromValue("Production only"),
                    CentralDataPreviewCell.FromValue("False"),
                ],
                [
                    CentralDataPreviewCell.FromValue("Emma"),
                    CentralDataPreviewCell.FromValue(" true "),
                ],
            ]);

        var transformed = preview.Apply(
        [
            new CentralDataQueryStep(
                CentralDataQueryStepKind.FilterRows,
                "Filter Office",
                "Keep Office records.",
                SourceColumn: "Office",
                FilterOperator: CentralDataFilterOperator.Equals,
                FilterValue: "true"),
        ]);

        Assert.Equal(2, transformed.Rows.Count);
        Assert.Equal(
            ["Laura", "Emma"],
            transformed.Rows.Select(row => row.Cell("First Name").Value));
    }

    [Fact]
    public void MasterbatchImport_ProjectsEachMaterialCompatibilityAndTemperatureColumn()
    {
        var seed = TestCentralDataSeed.Create().Snapshot;
        var preview = Preview(
            [
                "Colour",
                "Colour Supplier",
                "£/kg",
                "PVC Use",
                "PVC Max Temp",
                "PE/PP/PUR Use",
                "PE/PP/PUR Max Temp",
            ],
            [[
                CentralDataPreviewCell.FromValue("Test Blue"),
                CentralDataPreviewCell.FromValue("Test supplier"),
                CentralDataPreviewCell.FromValue("12.50"),
                CentralDataPreviewCell.FromValue("True"),
                CentralDataPreviewCell.FromValue("*200+ °C"),
                CentralDataPreviewCell.FromValue("False"),
                CentralDataPreviewCell.FromValue("280 °C"),
            ]]);
        var mappings = CentralDataImportSchema.Match(
                CentralDataArea.Masterbatch,
                preview.Columns)
            .Where(match => match.IsResolved)
            .ToDictionary(match => match.Field.Key, match => match.SourceColumn!);

        var result = CentralDataTableImporter.Import(
            seed,
            CentralDataArea.Masterbatch,
            preview,
            mappings,
            [],
            "Masterbatch · Access");

        Assert.True(result.Succeeded, result.Message);
        var masterbatch = Assert.Single(result.Snapshot!.Masterbatches);
        Assert.Equal(8, masterbatch.EffectiveMaterialLimits.Count);
        var pvc = Assert.Single(
            masterbatch.CompatibilityCells,
            cell => cell.MaterialFamily == "PVC");
        Assert.True(pvc.IsRecorded);
        Assert.True(pvc.IsCompatible);
        Assert.Equal("*200+ °C", pvc.TemperatureDisplay);
        var pe = Assert.Single(
            masterbatch.CompatibilityCells,
            cell => cell.MaterialFamily == "PE/PP/PUR");
        Assert.True(pe.IsRecorded);
        Assert.False(pe.IsCompatible);
        Assert.Empty(pe.TemperatureDisplay);
    }

    [Fact]
    public void ImportSchema_RespectsAnExplicitDoNotImportChoice()
    {
        var matches = CentralDataImportSchema.Match(
            CentralDataArea.Compounds,
            ["Compound", "Company", "Cost (£/kg)", "Specific Gravity", "Description"],
            new Dictionary<string, string>
            {
                ["Description"] = string.Empty,
            });

        Assert.False(matches.Single(match => match.Field.Key == "Description").IsResolved);
        Assert.True(CentralDataImportSchema.HasAllRequiredMatches(matches));
    }

    [Fact]
    public void CompoundImport_BlanksOnlyTheDivisionErrorAndKeepsBothRecords()
    {
        var seed = TestCentralDataSeed.Create().Snapshot;
        var preview = Preview(
            ["ID", "Compound", "Company", "Cost (£/kg)", "Specific Gravity"],
            [
                [
                    CentralDataPreviewCell.FromValue(1),
                    CentralDataPreviewCell.FromValue("Error price compound"),
                    CentralDataPreviewCell.FromValue("Supplier A"),
                    CentralDataPreviewCell.FromValue("#DIV/0!"),
                    CentralDataPreviewCell.FromValue("1.25"),
                ],
                [
                    CentralDataPreviewCell.FromValue(2),
                    CentralDataPreviewCell.FromValue("Valid compound"),
                    CentralDataPreviewCell.FromValue("Supplier B"),
                    CentralDataPreviewCell.FromValue("2.63"),
                    CentralDataPreviewCell.FromValue("1.40"),
                ],
            ]);
        var mappings = CentralDataImportSchema.Match(
                CentralDataArea.Compounds,
                preview.Columns.Select(column => column.Name))
            .Where(match => match.IsResolved)
            .ToDictionary(match => match.Field.Key, match => match.SourceColumn!);

        var result = CentralDataTableImporter.Import(
            seed,
            CentralDataArea.Compounds,
            preview,
            mappings,
            [new CentralDataQueryStep(
                CentralDataQueryStepKind.ReplaceDivisionByZeroWithNull,
                "Ignore errors",
                "")],
            "Test Access table");

        Assert.True(result.Succeeded, result.Message);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(2, result.ImportedRows);
        Assert.Equal(0m, result.Snapshot.Compounds[0].PricePerKilogram);
        Assert.Equal(2.63m, result.Snapshot.Compounds[1].PricePerKilogram);
        Assert.Contains(result.Warnings, warning => warning.Contains("blank", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CopperImport_ProjectsNominalOdFromMetadataAndRetainsUnmappedColumns()
    {
        var seed = TestCentralDataSeed.Create().Snapshot;
        var columns = new[]
        {
            new CentralDataPreviewColumn("Description", "Text", 0, true),
            new CentralDataPreviewColumn("Company", "Text", 1, true),
            new CentralDataPreviewColumn("Total Cost", "Currency", 2, true),
            new CentralDataPreviewColumn("Yield", "Double", 3, true),
            new CentralDataPreviewColumn(
                "Nominal",
                "Double",
                4,
                true,
                SourceName: "Nominal",
                Description: "Nom OD (mm)"),
            new CentralDataPreviewColumn("Drawing reference", "Text", 5, true),
        };
        var preview = new CentralDataTablePreview(
            new CentralDataSourceObject("Copper", null, CentralDataObjectKind.Table, "Copper"),
            columns,
            [new CentralDataPreviewRow(
                1,
                columns.ToDictionary(
                    column => column.Name,
                    column => column.Name switch
                    {
                        "Description" => CentralDataPreviewCell.FromValue("7/0.20 TCW"),
                        "Company" => CentralDataPreviewCell.FromValue("Copper supplier"),
                        "Total Cost" => CentralDataPreviewCell.FromValue("10.50"),
                        "Yield" => CentralDataPreviewCell.FromValue("531.24"),
                        "Nominal" => CentralDataPreviewCell.FromValue("0.58"),
                        _ => CentralDataPreviewCell.FromValue("DRG-17"),
                    },
                    StringComparer.OrdinalIgnoreCase))],
            [],
            200);

        var result = CentralDataTableImporter.Import(
            seed,
            CentralDataArea.Copper,
            preview,
            new Dictionary<string, string>(),
            [],
            "Copper · Access");

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(0.58m, Assert.Single(result.Snapshot!.Copper).NominalOutsideDiameterMillimetres);
        Assert.Equal(6, result.RetainedTable!.Columns.Count);
        Assert.Equal(
            "DRG-17",
            Assert.Single(result.RetainedTable.Rows).Cell("Drawing reference").Value);
    }

    [Fact]
    public void CopperImport_CalculatesMissingFieldsAndPreservesTheSourceCells()
    {
        var seed = TestCentralDataSeed.Create().Snapshot;
        var preview = Preview(
            [
                "Description",
                "Company",
                "Total Cost 2 (£/kg)",
                "Yield (m/kg) Manual",
                "Nom OD (mm)",
                "Manufature Cost",
                "Copper Cost (£/kg)",
                "Net Weight",
                "Length",
                "Volume (mm³/m)",
            ],
            [[
                CentralDataPreviewCell.FromValue("32/0.196 TCW (H)"),
                CentralDataPreviewCell.FromValue("Hayo Energi"),
                CentralDataPreviewCell.FromValue("0"),
                CentralDataPreviewCell.FromValue(null),
                CentralDataPreviewCell.FromValue(null),
                CentralDataPreviewCell.FromValue("1.25"),
                CentralDataPreviewCell.FromValue("7.00"),
                CentralDataPreviewCell.FromValue("344"),
                CentralDataPreviewCell.FromValue("30000"),
                CentralDataPreviewCell.FromValue("1130.973355"),
            ]]);

        var result = CentralDataTableImporter.Import(
            seed,
            CentralDataArea.Copper,
            preview,
            new Dictionary<string, string>(),
            [],
            "Copper · Access");

        Assert.True(result.Succeeded, result.Message);
        var copper = Assert.Single(result.Snapshot!.Copper);
        Assert.Equal(8.25m, copper.PricePerKilogram);
        Assert.Equal(30000m / 344m, copper.YieldMetresPerKilogram);
        Assert.InRange(
            copper.NominalOutsideDiameterMillimetres,
            1.199999m,
            1.200001m);
        Assert.True(copper.NominalAreaSquareMillimetres > 0m);
        Assert.Equal(4, copper.EffectiveDerivedValues.Count);
        Assert.False(copper.HasEstimatedValues);
        Assert.Contains(result.Warnings, warning =>
            warning.Contains("Calculated 4", StringComparison.OrdinalIgnoreCase));

        var retained = Assert.Single(result.RetainedTable!.Rows);
        Assert.Equal("0", retained.Cell("Total Cost 2 (£/kg)").Value);
        Assert.Null(retained.Cell("Yield (m/kg) Manual").Value);
        Assert.Null(retained.Cell("Nom OD (mm)").Value);
    }

    [Fact]
    public void CopperDerivation_EstimatesPackedOdOnlyWhenVolumeAndOdAreMissing()
    {
        var completed = CopperReferenceDeriver.FillMissing(
            new CopperReference(
                "863",
                "32/0.196 TCW (H)",
                "Hayo Energi",
                PricePerKilogram: 0m,
                YieldMetresPerKilogram: 87.209302m,
                NominalOutsideDiameterMillimetres: 0m),
            new CopperReferenceDerivationInputs());

        Assert.True(completed.NominalOutsideDiameterMillimetres > 0m);
        Assert.True(completed.HasEstimatedValues);
        Assert.True(completed.IsSelectableForCosting);
        var estimate = Assert.Single(
            completed.EffectiveDerivedValues,
            value => value.FieldKey ==
                     "NominalOutsideDiameterMillimetres");
        Assert.True(estimate.IsEstimate);
        Assert.Contains("close-packed", estimate.Formula);
    }

    [Fact]
    public void Import_DoesNotReplaceRetainedDataWhenTheProviderReturnedOnlyAPartialTable()
    {
        var seed = TestCentralDataSeed.Create().Snapshot;
        var partial = Preview(
            ["Compound", "Company", "Cost (£/kg)", "Specific Gravity"],
            [[
                CentralDataPreviewCell.FromValue("Partial row"),
                CentralDataPreviewCell.FromValue("Supplier"),
                CentralDataPreviewCell.FromValue("1.00"),
                CentralDataPreviewCell.FromValue("1.20"),
            ]]) with
        {
            Issues =
            [
                new CentralDataPreviewIssue(
                    null,
                    null,
                    "Provider aborted the query.",
                    IsBlocking: true),
            ],
        };

        var result = CentralDataTableImporter.Import(
            seed,
            CentralDataArea.Compounds,
            partial,
            new Dictionary<string, string>(),
            [],
            "Partial source");

        Assert.False(result.Succeeded);
        Assert.Null(result.Snapshot);
        Assert.Contains("complete table", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServiceImport_ReplacesOnlyTheSelectedRetainedTableAfterValidation()
    {
        var store = new TestStore(TestCentralDataSeed.Create());
        var service = new CentralDataService(store, []);
        var preview = Preview(
            ["First Name", "Last Name", "Initials", "Future payroll code"],
            [[
                CentralDataPreviewCell.FromValue("Alex"),
                CentralDataPreviewCell.FromValue("Tester"),
                CentralDataPreviewCell.FromValue("AT"),
                CentralDataPreviewCell.FromValue("P-001"),
            ]]);
        var mappings = CentralDataImportSchema.Match(
                CentralDataArea.Operators,
                preview.Columns.Select(column => column.Name))
            .Where(match => match.IsResolved)
            .ToDictionary(match => match.Field.Key, match => match.SourceColumn!);
        var originalCopper = store.Load().Snapshot.Copper;
        var link = new CentralDataTableLink(
            CentralDataArea.Operators,
            CentralDataSourceKind.AccessDatabase,
            "Operators · Access",
            "Operators",
            mappings,
            AccessDatabasePath: "central.accdb");

        var result = service.ImportTable(link, preview);

        Assert.True(result.Succeeded, result.Message);
        Assert.Single(store.Load().Snapshot.EffectiveOperators);
        Assert.Same(originalCopper, store.Load().Snapshot.Copper);
        Assert.Single(store.Load().EffectiveTableLinks);
        var retained = Assert.Single(store.Load().EffectiveRetainedTables);
        Assert.Equal(4, retained.Columns.Count);
        Assert.Equal(
            "P-001",
            Assert.Single(retained.Rows).Cell("Future payroll code").Value);
    }

    [Fact]
    public async Task Refresh_UsesTheSavedNavigatorQueryAndUpdatesTheLinkedArea()
    {
        var store = new TestStore(TestCentralDataSeed.Create());
        var preview = Preview(
            ["ID", "Compound", "Company", "Cost (£/kg)", "Specific Gravity"],
            [[
                CentralDataPreviewCell.FromValue(17),
                CentralDataPreviewCell.FromValue("Refreshed compound"),
                CentralDataPreviewCell.FromValue("Live supplier"),
                CentralDataPreviewCell.FromValue("4.25"),
                CentralDataPreviewCell.FromValue("1.31"),
            ]]);
        var mappings = CentralDataImportSchema.Match(
                CentralDataArea.Compounds,
                preview.Columns.Select(column => column.Name))
            .Where(match => match.IsResolved)
            .ToDictionary(match => match.Field.Key, match => match.SourceColumn!);
        store.SaveTableLink(new CentralDataTableLink(
            CentralDataArea.Compounds,
            CentralDataSourceKind.AccessDatabase,
            "Compounds · Access",
            "Compounds",
            mappings,
            AccessDatabasePath: "central.accdb"));
        var service = new CentralDataService(
            store,
            [],
            [new StubNavigator(preview)]);

        var result = await service.RefreshAsync();

        Assert.True(result.Updated, result.Message);
        Assert.False(result.UsedRetainedSnapshot);
        var areaResult = Assert.Single(result.EffectiveAreaResults);
        Assert.Equal(CentralDataArea.Compounds, areaResult.Area);
        Assert.True(areaResult.Updated);
        Assert.False(areaResult.UsedRetainedSnapshot);
        var compound = Assert.Single(result.State.Snapshot.Compounds);
        Assert.Equal("Refreshed compound", compound.CompoundName);
        Assert.Equal(4.25m, compound.PricePerKilogram);
    }

    [Fact]
    public void ServiceLoad_CompletesAnExistingOfflineRetainedCopperProjection()
    {
        var seed = TestCentralDataSeed.Create();
        var sourceCopper = new CopperReference(
            "863",
            "32/0.196 TCW (H)",
            "Hayo Energi",
            PricePerKilogram: 0m,
            YieldMetresPerKilogram: 0m,
            NominalOutsideDiameterMillimetres: 0m);
        var preview = Preview(
            [
                "Description",
                "Company",
                "Total Cost 2 (£/kg)",
                "Yield (m/kg) Manual",
                "Nom OD (mm)",
                "Net Weight",
                "Length",
                "Volume (mm³/m)",
            ],
            [[
                CentralDataPreviewCell.FromValue(sourceCopper.Description),
                CentralDataPreviewCell.FromValue(sourceCopper.Supplier),
                CentralDataPreviewCell.FromValue("0"),
                CentralDataPreviewCell.FromValue(null),
                CentralDataPreviewCell.FromValue(null),
                CentralDataPreviewCell.FromValue("344"),
                CentralDataPreviewCell.FromValue("30000"),
                CentralDataPreviewCell.FromValue(null),
            ]]);
        var link = new CentralDataTableLink(
            CentralDataArea.Copper,
            CentralDataSourceKind.AccessDatabase,
            "Copper · Access",
            "Copper",
            new Dictionary<string, string>
            {
                ["Description"] = "Description",
                ["Supplier"] = "Company",
                ["PricePerKilogram"] = "Total Cost 2 (£/kg)",
                ["YieldMetresPerKilogram"] = "Yield (m/kg) Manual",
                ["NominalOutsideDiameterMillimetres"] = "Nom OD (mm)",
            },
            AccessDatabasePath: "central.accdb");
        var retained = new CentralDataRetainedTable(
            CentralDataArea.Copper,
            link.DisplayName,
            link.TableName,
            null,
            CentralDataObjectKind.Table,
            DateTimeOffset.UtcNow,
            preview.Columns,
            preview.Rows);
        var state = seed with
        {
            Snapshot = seed.Snapshot with { Copper = [sourceCopper] },
            TableLinks = [link],
            RetainedTables = [retained],
        };
        var store = new TestStore(state);
        var service = new CentralDataService(store, []);

        var completed = Assert.Single(service.Load().Snapshot.Copper);

        Assert.Equal(30000m / 344m, completed.YieldMetresPerKilogram);
        Assert.True(completed.NominalOutsideDiameterMillimetres > 0m);
        Assert.True(completed.HasEstimatedValues);
        Assert.Equal(
            0m,
            Assert.Single(store.Load().Snapshot.Copper)
                .NominalOutsideDiameterMillimetres);
        Assert.Null(
            Assert.Single(store.Load().EffectiveRetainedTables)
                .Rows[0]
                .Cell("Nom OD (mm)")
                .Value);
    }

    private static CentralDataTablePreview Preview(
        IReadOnlyList<string> columnNames,
        IReadOnlyList<IReadOnlyList<CentralDataPreviewCell>> values)
    {
        var columns = columnNames
            .Select((name, index) => new CentralDataPreviewColumn(name, "Text", index, true))
            .ToArray();
        var rows = values
            .Select((row, rowIndex) => new CentralDataPreviewRow(
                rowIndex + 1,
                columns.ToDictionary(
                    column => column.Name,
                    column => row[column.Ordinal],
                    StringComparer.OrdinalIgnoreCase)))
            .ToArray();
        return new CentralDataTablePreview(
            new CentralDataSourceObject("Test", null, CentralDataObjectKind.Table, "Test"),
            columns,
            rows,
            [],
            200);
    }

    private sealed class TestStore(CentralDataState initial) : ICentralDataStore
    {
        private CentralDataState _state = initial;

        public CentralDataState Load() => _state;

        public void SaveConfiguration(CentralDataSourceConfiguration configuration) =>
            _state = _state with { Configuration = configuration };

        public void SaveTableLink(CentralDataTableLink link) =>
            _state = _state with
            {
                TableLinks = _state.EffectiveTableLinks
                    .Where(existing => existing.Area != link.Area)
                    .Append(link)
                    .ToArray(),
            };

        public void RemoveTableLink(CentralDataArea area) =>
            _state = _state with
            {
                TableLinks = _state.EffectiveTableLinks
                    .Where(existing => existing.Area != area)
                    .ToArray(),
            };

        public void SaveSnapshot(CentralDataSnapshot snapshot) =>
            _state = _state with { Snapshot = snapshot };

        public void SaveImportedTable(
            CentralDataTableLink link,
            CentralDataSnapshot snapshot,
            CentralDataRetainedTable retainedTable)
        {
            SaveTableLink(link);
            SaveSnapshot(snapshot);
            _state = _state with
            {
                RetainedTables = _state.EffectiveRetainedTables
                    .Where(existing => existing.Area != retainedTable.Area)
                    .Append(retainedTable)
                    .ToArray(),
            };
        }
    }

    private sealed class StubNavigator(
        CentralDataTablePreview preview) : ICentralDataDatabaseNavigator
    {
        public CentralDataSourceKind Kind => CentralDataSourceKind.AccessDatabase;

        public Task<IReadOnlyList<CentralDataSourceObject>> DiscoverAsync(
            CentralDataDatabaseConnection connection,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CentralDataSourceObject>>([preview.SourceObject]);

        public Task<CentralDataTablePreview> PreviewAsync(
            CentralDataDatabaseConnection connection,
            CentralDataSourceObject sourceObject,
            int rowLimit = 200,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(preview);
    }
}
