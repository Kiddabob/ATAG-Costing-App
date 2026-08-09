using ATAG.Costing.Domain.Calculations;
using ATAG.Costing.Domain.Materials;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Materials;

public sealed class MasterbatchUsageCalculatorTests
{
    [Fact]
    public void Calculate_ConvertsQuoteUsageToPerMetreMassAndCost()
    {
        var result = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(10m),
                new UsageAllowanceRateFraction(0.03m),
                new AdditionRateFraction(0.025m),
                new LengthMetres(200m),
                new PricePerKilogram(8m)));

        Assert.Equal(0.2575m, result.MasterbatchMassForQuote.Value);
        Assert.Equal(0.0012875m, result.MasterbatchKilogramsPerMetre.Value);
        Assert.Equal(1.2875m, result.MasterbatchGramsPerMetre.Value);
        Assert.Equal(0.0103m, result.MasterbatchPricePerMetre.Value);
    }

    [Fact]
    public void Calculate_ReturnsAnOrderedAuditableTrace()
    {
        var result = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(10m),
                new UsageAllowanceRateFraction(0.03m),
                new AdditionRateFraction(0.025m),
                new LengthMetres(200m),
                new PricePerKilogram(8m)));

        Assert.Collection(
            result.Steps,
            step => Assert.Equal("base-compound-mass-before-allowance", step.Id),
            step => Assert.Equal("usage-allowance-rate", step.Id),
            step => Assert.Equal("usage-allowance-multiplier", step.Id),
            step => Assert.Equal("base-compound-mass-with-allowance", step.Id),
            step => Assert.Equal("masterbatch-addition-rate", step.Id),
            step => Assert.Equal("quote-length", step.Id),
            step => Assert.Equal("masterbatch-price-per-kilogram", step.Id),
            step => Assert.Equal("masterbatch-mass-for-quote", step.Id),
            step => Assert.Equal("masterbatch-kilograms-per-metre", step.Id),
            step => Assert.Equal("masterbatch-grams-per-metre", step.Id),
            step => Assert.Equal("masterbatch-price-per-metre", step.Id));

        var finalStep = result.Steps[^1];
        Assert.Equal(MasterbatchUsageCalculator.RuleVersion, finalStep.RuleVersion);
        Assert.Equal("0.01", finalStep.DisplayValue);
        Assert.Equal("£/m", finalStep.Unit);
        Assert.Contains("0.0012875 kg/m", finalStep.SubstitutedExpression);
        Assert.Contains("8 £/kg", finalStep.SubstitutedExpression);
        Assert.Equal(2, finalStep.InputSteps.Count);
        Assert.False(string.IsNullOrWhiteSpace(finalStep.BusinessMeaning));
        Assert.False(string.IsNullOrWhiteSpace(finalStep.RoundingRule));
    }

    [Fact]
    public void Calculate_DoesNotRoundIntermediateValues()
    {
        var result = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(1m),
                new UsageAllowanceRateFraction(0.03m),
                new AdditionRateFraction(0.3333333333333333333333333333m),
                new LengthMetres(7m),
                new PricePerKilogram(19.99m)));

        var expectedKilogramsPerMetre =
            (1m * 1.03m * 0.3333333333333333333333333333m) / 7m;
        var expectedPricePerMetre = expectedKilogramsPerMetre * 19.99m;

        Assert.Equal(expectedKilogramsPerMetre, result.MasterbatchKilogramsPerMetre.Value);
        Assert.Equal(expectedPricePerMetre, result.MasterbatchPricePerMetre.Value);
    }

    [Fact]
    public void Calculate_UsesTheExplicitMidpointDisplayRule()
    {
        var result = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(0.0000000005m),
                new UsageAllowanceRateFraction(0m),
                new AdditionRateFraction(1m),
                new LengthMetres(1m),
                new PricePerKilogram(1m)));

        var kilogramsPerMetreStep =
            Assert.Single(result.Steps, step => step.Id == "masterbatch-kilograms-per-metre");
        var gramsPerMetreStep =
            Assert.Single(result.Steps, step => step.Id == "masterbatch-grams-per-metre");

        Assert.Equal("0.000000001", kilogramsPerMetreStep.DisplayValue);
        Assert.Equal("0.000001", gramsPerMetreStep.DisplayValue);
    }

    [Fact]
    public void Calculate_AllowsAZeroUsageBoundary()
    {
        var result = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(10m),
                new UsageAllowanceRateFraction(0.03m),
                new AdditionRateFraction(0m),
                new LengthMetres(200m),
                new PricePerKilogram(8m)));

        Assert.Equal(0m, result.MasterbatchMassForQuote.Value);
        Assert.Equal(0m, result.MasterbatchKilogramsPerMetre.Value);
        Assert.Equal(0m, result.MasterbatchGramsPerMetre.Value);
        Assert.Equal(0m, result.MasterbatchPricePerMetre.Value);
    }

    [Fact]
    public void Calculate_WarnsWhenTheFractionExceedsOne()
    {
        var result = MasterbatchUsageCalculator.Calculate(
            new MasterbatchUsageInputs(
                new MassKilograms(10m),
                new UsageAllowanceRateFraction(0.03m),
                new AdditionRateFraction(1.01m),
                new LengthMetres(200m),
                new PricePerKilogram(8m)));

        var rateStep = Assert.Single(result.Steps, step => step.Id == "masterbatch-addition-rate");
        Assert.NotNull(rateStep.Warning);
    }

    [Fact]
    public void TypedInputs_RejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MassKilograms(-0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsageAllowanceRateFraction(-0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AdditionRateFraction(-0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LengthMetres(0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PricePerKilogram(-0.01m));
    }

    [Fact]
    public void UsageAllowance_IsASeparateNamedThreePercentBoost()
    {
        var result = UsageAllowanceCalculator.Apply(
            100m,
            new UsageAllowanceRateFraction(0.03m));

        Assert.Equal(1.03m, result.Multiplier);
        Assert.Equal(103m, result.AdjustedUsage);
    }
}
