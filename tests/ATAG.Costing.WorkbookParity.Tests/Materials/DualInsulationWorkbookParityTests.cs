using System.Globalization;
using System.Text.Json;
using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Materials;
using Xunit;

namespace ATAG.Costing.WorkbookParity.Tests.Materials;

public sealed class DualInsulationWorkbookParityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void PendingFixture_RecordsConfirmedRulesAndWorkbookDefects()
    {
        var fixture = LoadFixture();

        Assert.Equal("PendingBusinessApproval", fixture.ApprovalStatus);
        Assert.Equal(
            "../(WIP Mitchell) Costing Sheet.xlsm",
            fixture.Workbook.RelativePath);
        Assert.Equal(
            "823FCE28815A9420E87A9FA119790243C8A4E9961B26B976A26EBE79BE9FA0ED",
            fixture.Workbook.Sha256);
        Assert.Equal(
            DualInsulationCostingCalculator.RuleVersion,
            fixture.CalculationRule);
        Assert.Contains(
            "first insulation layer only",
            fixture.ConfirmedRules.CoreStartupScope);
        Assert.Contains(
            "exactly once",
            fixture.ConfirmedRules.UsageAllowance);
        Assert.Contains(
            "exactly once",
            fixture.ConfirmedRules.SubtotalInclusion);
        Assert.Contains(
            fixture.KnownWorkbookDefects,
            defect => defect.Id == "first-masterbatch-second-allowance");
        Assert.Contains(
            fixture.KnownWorkbookDefects,
            defect => defect.Id == "second-masterbatch-second-allowance");
        Assert.Contains(
            fixture.KnownWorkbookDefects,
            defect => defect.Id == "summary-masterbatch-added-twice");
        Assert.Contains(
            fixture.SourceCells,
            source =>
                source.Sheet == "SBS1DualInsCompPrice2" &&
                source.Cell == "B32");
    }

    [Fact(
        Skip =
            "The mapped dual-insulation workbook case is pending business " +
            "approval of its complete workbook identity, source map and " +
            "corrected expected outputs. The formula corrections are confirmed.")]
    public void ApprovedFixture_MatchesTheCorrectedPureDomainCalculation()
    {
        var fixture = LoadFixture();
        Assert.Equal("Approved", fixture.ApprovalStatus);

        var inputs = fixture.Inputs;
        var result = DualInsulationCostingCalculator.Calculate(
            new DualInsulationCostingInputs(
                inputs.ConductorReference,
                Quote(
                    inputs.ConductorQuoteTotal,
                    inputs.ConductorQuotedMassKilograms),
                new YieldMetresPerKilogram(
                    Parse(inputs.ConductorYieldMetresPerKilogram)),
                new Millimetres(
                    Parse(inputs.ConductorOutsideDiameterMillimetres)),
                new DualInsulationLayerInputs(
                    inputs.FirstCompoundReference,
                    Quote(
                        inputs.FirstCompoundQuoteTotal,
                        inputs.FirstCompoundQuotedMassKilograms),
                    new SpecificGravity(
                        Parse(inputs.FirstCompoundSpecificGravity)),
                    new Millimetres(
                        Parse(inputs.FirstNominalOutsideDiameterMillimetres)),
                    new Millimetres(
                        Parse(inputs.FirstPositiveToleranceMillimetres)),
                    inputs.FirstMasterbatchReference,
                    Quote(
                        inputs.FirstMasterbatchQuoteTotal,
                        inputs.FirstMasterbatchQuotedMassKilograms),
                    new AdditionRateFraction(
                        Parse(inputs.FirstMasterbatchAdditionRateFraction))),
                new DualInsulationLayerInputs(
                    inputs.SecondCompoundReference,
                    Quote(
                        inputs.SecondCompoundQuoteTotal,
                        inputs.SecondCompoundQuotedMassKilograms),
                    new SpecificGravity(
                        Parse(inputs.SecondCompoundSpecificGravity)),
                    new Millimetres(
                        Parse(inputs.SecondNominalOutsideDiameterMillimetres)),
                    new Millimetres(
                        Parse(inputs.SecondPositiveToleranceMillimetres)),
                    inputs.SecondMasterbatchReference,
                    Quote(
                        inputs.SecondMasterbatchQuoteTotal,
                        inputs.SecondMasterbatchQuotedMassKilograms),
                    new AdditionRateFraction(
                        Parse(inputs.SecondMasterbatchAdditionRateFraction))),
                new LengthMetres(Parse(inputs.FinishedQuoteLengthMetres)),
                new AdditionalProductionLengthMetres(
                    Parse(inputs.CoreStartupLengthMetres)),
                new UsageAllowanceRateFraction(
                    Parse(inputs.UsageAllowanceRateFraction))));
        var expected = fixture.CorrectedDomainExpected;
        var tolerance = Parse(fixture.AllowedAbsoluteTolerance);

        AssertWithin(
            Parse(expected.CoreProductionLengthMetres),
            result.CoreProductionLength.Value,
            tolerance);
        AssertWithin(
            Parse(expected.SecondLayerProductionLengthMetres),
            result.SecondLayerProductionLength.Value,
            tolerance);
        AssertWithin(
            Parse(expected.ConductorPriceForRun),
            result.Conductor.QuotePrice,
            tolerance);
        AssertWithin(
            Parse(expected.FirstCompoundPriceForRun),
            result.FirstLayerCompound.Material.QuotePrice,
            tolerance);
        AssertWithin(
            Parse(expected.SecondCompoundPriceForRun),
            result.SecondLayerCompound.Material.QuotePrice,
            tolerance);
        AssertWithin(
            Parse(expected.SecondMasterbatchMassForRunKilograms),
            result.SecondLayerMasterbatch.MasterbatchMassForQuote.Value,
            tolerance);
        AssertWithin(
            Parse(expected.SecondMasterbatchPriceForRun),
            result.SecondLayerMasterbatch.MasterbatchPricePerMetre.Value *
            result.SecondLayerProductionLength.Value,
            tolerance);
        AssertWithin(
            Parse(expected.MaterialPriceForProductionRun),
            result.MaterialPriceForProductionRun,
            tolerance);
        AssertWithin(
            Parse(expected.MaterialPricePerFinishedMetre),
            result.MaterialPricePerFinishedMetre.Value,
            tolerance);
    }

    private static MaterialSupplierQuote Quote(string total, string mass) =>
        new(
            new SupplierQuoteTotal(Parse(total)),
            new MassKilograms(Parse(mass)));

    private static DualInsulationParityFixture LoadFixture()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "dual-insulation-sbs1.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<DualInsulationParityFixture>(
                   json,
                   JsonOptions) ??
            throw new InvalidOperationException(
                "The dual-insulation parity fixture could not be read.");
    }

    private static decimal Parse(string value) =>
        decimal.Parse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture);

    private static void AssertWithin(
        decimal expected,
        decimal actual,
        decimal tolerance) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);

    private sealed record DualInsulationParityFixture(
        string CaseId,
        string CalculationRule,
        string ApprovalStatus,
        WorkbookIdentity Workbook,
        ConfirmedRules ConfirmedRules,
        FixtureInputs Inputs,
        FixtureExpected CorrectedDomainExpected,
        string AllowedAbsoluteTolerance,
        IReadOnlyList<KnownWorkbookDefect> KnownWorkbookDefects,
        IReadOnlyList<SourceCell> SourceCells);

    private sealed record WorkbookIdentity(string RelativePath, string Sha256);

    private sealed record ConfirmedRules(
        string CoreStartupScope,
        string SecondLayerScope,
        string UsageAllowance,
        string SubtotalInclusion);

    private sealed record FixtureInputs(
        string ConductorReference,
        string ConductorQuoteTotal,
        string ConductorQuotedMassKilograms,
        string ConductorYieldMetresPerKilogram,
        string ConductorOutsideDiameterMillimetres,
        string FirstCompoundReference,
        string FirstCompoundQuoteTotal,
        string FirstCompoundQuotedMassKilograms,
        string FirstCompoundSpecificGravity,
        string FirstNominalOutsideDiameterMillimetres,
        string FirstPositiveToleranceMillimetres,
        string FirstMasterbatchReference,
        string FirstMasterbatchQuoteTotal,
        string FirstMasterbatchQuotedMassKilograms,
        string FirstMasterbatchAdditionRateFraction,
        string SecondCompoundReference,
        string SecondCompoundQuoteTotal,
        string SecondCompoundQuotedMassKilograms,
        string SecondCompoundSpecificGravity,
        string SecondNominalOutsideDiameterMillimetres,
        string SecondPositiveToleranceMillimetres,
        string SecondMasterbatchReference,
        string SecondMasterbatchQuoteTotal,
        string SecondMasterbatchQuotedMassKilograms,
        string SecondMasterbatchAdditionRateFraction,
        string FinishedQuoteLengthMetres,
        string CoreStartupLengthMetres,
        string UsageAllowanceRateFraction);

    private sealed record FixtureExpected(
        string CoreProductionLengthMetres,
        string SecondLayerProductionLengthMetres,
        string ConductorPriceForRun,
        string FirstCompoundPriceForRun,
        string SecondCompoundPriceForRun,
        string SecondMasterbatchMassForRunKilograms,
        string SecondMasterbatchPriceForRun,
        string MaterialPriceForProductionRun,
        string MaterialPricePerFinishedMetre);

    private sealed record KnownWorkbookDefect(
        string Id,
        string Sheet,
        string Cell);

    private sealed record SourceCell(string Sheet, string Cell);
}
