using System.Globalization;
using System.Text.Json;
using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Materials;
using Xunit;

namespace ATAG.Costing.WorkbookParity.Tests.Materials;

public sealed class MasterbatchUsageWorkbookParityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void PendingFixture_RecordsPortableWorkbookEvidenceWithoutClaimingApproval()
    {
        var fixture = LoadFixture();

        Assert.Equal("PendingBusinessApproval", fixture.ApprovalStatus);
        Assert.Equal("../(WIP Mitchell) Costing Sheet.xlsm", fixture.Workbook.RelativePath);
        Assert.Equal(
            "6A9DBE53DF2A403BDB92A23FDC2C4AD55702B6ADF089ED02FA327F3E504851D3",
            fixture.Workbook.Sha256);
        Assert.Equal(MasterbatchUsageCalculator.RuleVersion, fixture.CalculationRule);
        Assert.Contains(
            fixture.SourceCells,
            source => source.Sheet == "COR1MBPrice" && source.Cell == "V26");
        Assert.Contains(
            fixture.SourceCells,
            source => source.Sheet == "COR1MBPrice" && source.Cell == "B32");
    }

    [Fact(
        Skip =
            "The discovered COR1 workbook case is pending business approval. " +
            "See docs/OPEN-QUESTIONS.md before promoting it to a golden parity case.")]
    public void ApprovedFixture_MatchesThePureDomainCalculation()
    {
        var fixture = LoadFixture();
        Assert.Equal("Approved", fixture.ApprovalStatus);

        var result = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(
                    Parse(fixture.Inputs.BaseCompoundMassBeforeAllowanceForQuoteKilograms)),
                new UsageAllowanceRateFraction(Parse(fixture.Inputs.UsageAllowanceRateFraction)),
                new AdditionRateFraction(Parse(fixture.Inputs.AdditionRateFraction)),
                new LengthMetres(Parse(fixture.Inputs.QuoteLengthMetres)),
                new PricePerKilogram(Parse(fixture.Inputs.MasterbatchPricePerKilogram))));
        var tolerance = Parse(fixture.AllowedAbsoluteTolerance);

        AssertWithin(
            Parse(fixture.Expected.MasterbatchMassForQuoteKilograms),
            result.MasterbatchMassForQuote.Value,
            tolerance);
        AssertWithin(
            Parse(fixture.Expected.MasterbatchKilogramsPerMetre),
            result.MasterbatchKilogramsPerMetre.Value,
            tolerance);
        AssertWithin(
            Parse(fixture.Expected.MasterbatchGramsPerMetre),
            result.MasterbatchGramsPerMetre.Value,
            tolerance);
        AssertWithin(
            Parse(fixture.Expected.MasterbatchPricePerMetre),
            result.MasterbatchPricePerMetre.Value,
            tolerance);
    }

    private static WorkbookParityFixture LoadFixture()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "masterbatch-usage-cor1.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<WorkbookParityFixture>(json, JsonOptions)
            ?? throw new InvalidOperationException("The parity fixture could not be read.");
    }

    private static decimal Parse(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static void AssertWithin(decimal expected, decimal actual, decimal tolerance) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);

    private sealed record WorkbookParityFixture(
        string CaseId,
        string CalculationRule,
        string ApprovalStatus,
        WorkbookIdentity Workbook,
        FixtureInputs Inputs,
        FixtureExpected Expected,
        string AllowedAbsoluteTolerance,
        IReadOnlyList<SourceCell> SourceCells);

    private sealed record WorkbookIdentity(string RelativePath, string Sha256);

    private sealed record FixtureInputs(
        string BaseCompoundMassBeforeAllowanceForQuoteKilograms,
        string UsageAllowanceRateFraction,
        string AdditionRateFraction,
        string QuoteLengthMetres,
        string MasterbatchPricePerKilogram);

    private sealed record FixtureExpected(
        string MasterbatchMassForQuoteKilograms,
        string MasterbatchKilogramsPerMetre,
        string MasterbatchGramsPerMetre,
        string MasterbatchPricePerMetre);

    private sealed record SourceCell(string Sheet, string Cell);
}
