using System.Text.Json;
using ATAG.Costing.Application.Projects;
using ATAG.Costing.Infrastructure.Projects;
using Xunit;

namespace ATAG.Costing.Application.Tests.Projects;

public sealed class SingleCoreProjectDocumentTests
{
    [Fact]
    public void ProjectDocument_RoundTripsUserInputsAndReviewState()
    {
        var source = new SingleCoreProjectDocument
        {
            CopperId = "860",
            CompoundId = "63",
            MasterbatchCode = "CUS3872",
            QuoteLengthMetres = 5000,
            UsageAllowancePercent = 3,
            RiskPercent = 4,
            MarkupPercent = 45,
            CustomerName = "Example customer",
            QuoteReelCount = 8,
            QuoteMetresPerReel = 625,
            QuoteConductorDisplayModeIndex = 3,
            UseExactCustomerColourName = true,
            QuoteDescription = "Customer cable reference",
            QuotePackaging = "Returnable reels",
            QuoteEstimatedDelivery = "Six weeks",
            QuoteSpecialNotes = "Use the customer's exact colour wording.",
            QuoteTermsAndConditions = "Example quotation terms.",
            HasCorePrint = true,
            CorePrintText = "ATAG CORE 01",
            CorePrintColourHex = "#F4F4F4",
            CorePrintHeightMillimetres = 0.7,
            CorePrintRepeatDistanceMillimetres = 300,
            CorePrintDotPitchHorizontalMillimetres = 0.2,
            CorePrintDotPitchVerticalMillimetres = 0.3,
            ReviewNotes = "Review retained",
            AdditionalRisksAtAcceptance = true,
        };

        var json = JsonSerializer.Serialize(source);
        var restored =
            JsonSerializer.Deserialize<SingleCoreProjectDocument>(json);

        Assert.NotNull(restored);
        Assert.Equal("860", restored.CopperId);
        Assert.Equal(3, restored.UsageAllowancePercent);
        Assert.Equal("Example customer", restored.CustomerName);
        Assert.Equal(8, restored.QuoteReelCount);
        Assert.Equal(625, restored.QuoteMetresPerReel);
        Assert.Equal(3, restored.QuoteConductorDisplayModeIndex);
        Assert.True(restored.UseExactCustomerColourName);
        Assert.Equal("Customer cable reference", restored.QuoteDescription);
        Assert.Equal("Returnable reels", restored.QuotePackaging);
        Assert.Equal("Six weeks", restored.QuoteEstimatedDelivery);
        Assert.Equal(
            "Use the customer's exact colour wording.",
            restored.QuoteSpecialNotes);
        Assert.Equal("Example quotation terms.", restored.QuoteTermsAndConditions);
        Assert.True(restored.HasCorePrint);
        Assert.Equal("ATAG CORE 01", restored.CorePrintText);
        Assert.Equal("#F4F4F4", restored.CorePrintColourHex);
        Assert.Equal(0.7, restored.CorePrintHeightMillimetres);
        Assert.Equal(300, restored.CorePrintRepeatDistanceMillimetres);
        Assert.Equal(0.2, restored.CorePrintDotPitchHorizontalMillimetres);
        Assert.Equal(0.3, restored.CorePrintDotPitchVerticalMillimetres);
        Assert.Equal("Review retained", restored.ReviewNotes);
        Assert.True(restored.AdditionalRisksAtAcceptance);
    }

    [Fact]
    public async Task JsonStore_SavesAndReopensThePortableDocument()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"atag-project-document-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "core.atagcosting");
            var store = new JsonSingleCoreProjectDocumentStore();
            var source = new SingleCoreProjectDocument
            {
                CopperId = "860",
                CompoundId = "63",
                MasterbatchCode = "CUS3872",
                QuoteLengthMetres = 1250,
                CustomerName = "Saved customer",
            };

            await store.SaveAsync(path, source);
            var restored = await store.LoadAsync(path);

            Assert.Equal("860", restored.CopperId);
            Assert.Equal(1250, restored.QuoteLengthMetres);
            Assert.Equal("Saved customer", restored.CustomerName);
            Assert.Empty(
                Directory.GetFiles(directory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LegacySchema_UpgradesWithStableRevisionMetadataForTheNextSave()
    {
        var savedAt = new DateTimeOffset(
            2026,
            7,
            29,
            9,
            15,
            0,
            TimeSpan.Zero);
        var legacy = new SingleCoreProjectDocument
        {
            SchemaVersion = 1,
            SavedAt = savedAt,
            CopperId = "860",
        };

        var upgraded = legacy.Upgrade();

        Assert.Equal(
            SingleCoreProjectDocument.CurrentSchemaVersion,
            upgraded.SchemaVersion);
        Assert.NotEqual(Guid.Empty, upgraded.ProjectId);
        Assert.NotEqual(Guid.Empty, upgraded.RevisionId);
        Assert.Equal(1, upgraded.RevisionNumber);
        Assert.Equal(CostingRevisionState.WorkingCopy, upgraded.RevisionState);
        Assert.Equal(savedAt, upgraded.CreatedAtUtc);
        Assert.Equal(savedAt, upgraded.UpdatedAtUtc);
    }

    [Fact]
    public void RevisionService_ApprovesForksAndDuplicatesWithoutChangingSource()
    {
        var service = new SingleCoreProjectRevisionService();
        var created = new DateTimeOffset(
            2026,
            7,
            29,
            10,
            0,
            0,
            TimeSpan.Zero);
        var working = CreateRevision(created);
        var approved = service.Approve(
            working,
            created.AddMinutes(5));
        var next = service.CreateNextRevision(
            approved,
            created.AddMinutes(10));
        var duplicate = service.Duplicate(
            approved,
            created.AddMinutes(15));

        Assert.Equal(
            CostingRevisionState.ApprovedRevision,
            approved.RevisionState);
        Assert.Equal(created.AddMinutes(5), approved.ApprovedAtUtc);
        Assert.Equal(working.ProjectId, next.ProjectId);
        Assert.NotEqual(working.RevisionId, next.RevisionId);
        Assert.Equal(2, next.RevisionNumber);
        Assert.Equal(CostingRevisionState.WorkingCopy, next.RevisionState);
        Assert.Null(next.ApprovedAtUtc);
        Assert.NotEqual(working.ProjectId, duplicate.ProjectId);
        Assert.NotEqual(working.RevisionId, duplicate.RevisionId);
        Assert.Equal(1, duplicate.RevisionNumber);
        Assert.Equal(
            CostingRevisionState.WorkingCopy,
            duplicate.RevisionState);
        Assert.Equal(working.CopperId, duplicate.CopperId);
    }

    [Fact]
    public async Task JsonStore_ApprovedRevisionCannotBeOverwritten()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"atag-approved-revision-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "revision.atagcosting");
            var store = new JsonSingleCoreProjectDocumentStore();
            var service = new SingleCoreProjectRevisionService();
            var working = CreateRevision(DateTimeOffset.UtcNow);
            await store.SaveAsync(path, working);
            var approved = service.Approve(
                working,
                DateTimeOffset.UtcNow);
            await store.SaveAsync(path, approved);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => store.SaveAsync(
                    path,
                    service.CreateNextRevision(
                        approved,
                        DateTimeOffset.UtcNow)));

            Assert.Contains("immutable", exception.Message);
            var restored = await store.LoadAsync(path);
            Assert.Equal(
                CostingRevisionState.ApprovedRevision,
                restored.RevisionState);
            Assert.Equal(approved.RevisionId, restored.RevisionId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Repository_IndexesRelativePathsInsideSelectedFolder()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"atag-project-index-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var repository = new JsonSingleCoreProjectRepository();
            var source = CreateRevision(DateTimeOffset.UtcNow);

            var saved = await repository.SaveAsync(directory, source);
            var entries = await repository.ListAsync(directory);
            var restored = await repository.LoadAsync(
                directory,
                Assert.Single(entries));

            Assert.True(File.Exists(saved.FullPath));
            Assert.True(
                File.Exists(
                    Path.Combine(
                        directory,
                        JsonSingleCoreProjectRepository.IndexFileName)));
            Assert.False(Path.IsPathRooted(saved.IndexEntry.RelativePath));
            Assert.StartsWith(
                Path.GetFullPath(directory),
                Path.GetFullPath(saved.FullPath),
                StringComparison.OrdinalIgnoreCase);
            Assert.Equal(source.ProjectId, restored.ProjectId);
            Assert.Equal(source.RevisionId, restored.RevisionId);
            Assert.Equal(
                "£123.45",
                restored.CalculatedResult?.MarkedUpQuote);
            Assert.Equal(
                "quote",
                restored.CalculatedResult?.Trace[0].Steps[0].Id);
            Assert.Equal(
                "input",
                restored.CalculatedResult?.Trace[0].Steps[0].Inputs[0].Id);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Repository_RefusesUnavailableOrEscapingStoragePaths()
    {
        var unavailable = Path.Combine(
            Path.GetTempPath(),
            $"atag-missing-root-{Guid.NewGuid():N}");
        var repository = new JsonSingleCoreProjectRepository();

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => repository.ListAsync(unavailable));

        var directory = Path.Combine(
            Path.GetTempPath(),
            $"atag-contained-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var entry = new SingleCoreProjectIndexEntry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                CostingRevisionState.WorkingCopy,
                "Escape attempt",
                "",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                Path.Combine("..", "outside.atagcosting"));
            await Assert.ThrowsAsync<InvalidDataException>(
                () => repository.LoadAsync(directory, entry));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static SingleCoreProjectDocument CreateRevision(
        DateTimeOffset created)
    {
        var input = new SavedCalculationStep(
            "input",
            "Quote length",
            "Input",
            "5000 m",
            5000m,
            "5000",
            "m",
            [],
            null,
            "Length requested by the customer.",
            "No calculation rounding.",
            "single-core-material-costing/v1");
        var quote = new SavedCalculationStep(
            "quote",
            "Recommended quote",
            "Cost × (1 + markup)",
            "85.14 × 1.45 = 123.453",
            123.453m,
            "123.45",
            "£",
            [input],
            null,
            "Recommended selling price.",
            "Display rounded to 2 decimal places.",
            "commercial-pricing/v1");
        return new SingleCoreProjectDocument
        {
            ProjectId = Guid.NewGuid(),
            RevisionId = Guid.NewGuid(),
            RevisionNumber = 1,
            CreatedAtUtc = created,
            UpdatedAtUtc = created,
            SavedAt = created,
            CopperId = "860",
            CalculatedResult = new SingleCoreCalculatedResultSnapshot
            {
                RecommendedQuotePrice = 123.453m,
                EffectiveCoreName = "Example core",
                MarkedUpQuote = "£123.45",
                Trace =
                [
                    new SavedCalculationSection(
                        "commercial",
                        "Commercial pricing",
                        [quote]),
                ],
            },
            RuleVersions =
            [
                "single-core-material-costing/v1",
                "commercial-pricing/v1",
            ],
        };
    }
}
