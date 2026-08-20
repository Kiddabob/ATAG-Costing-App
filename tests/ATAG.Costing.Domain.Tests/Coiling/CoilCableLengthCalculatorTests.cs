using ATAG.Costing.Domain.Coiling;
using Xunit;

namespace ATAG.Costing.Domain.Tests.Coiling;

public sealed class CoilCableLengthCalculatorTests
{
    [Fact]
    public void Calculate_FlatCableUsesHeightRadiallyAndWidthAsPitch()
    {
        var result = CoilCableLengthCalculator.Calculate(FlatInputs());

        Assert.Equal(2.5d, result.RadialCableThicknessMillimetres, 10);
        Assert.Equal(4.8d, result.AxialPitchMillimetres, 10);
        Assert.Equal(8d, result.RequiredBarDiameterMillimetres, 10);
        Assert.Equal(10.5d, result.MeanPathDiameterMillimetres, 10);
        Assert.Equal(19, result.CompleteTurns);
        Assert.Equal(91.2d, result.ActualWoundAxialLengthMillimetres, 10);
        Assert.Equal(1.2d, result.AxialOverrunMillimetres, 10);

        var expectedTurnLength = Math.Sqrt(
            Math.Pow(Math.PI * 10.5d, 2d) + Math.Pow(4.8d, 2d));
        Assert.Equal(expectedTurnLength, result.HelicalLengthPerTurnMillimetres, 10);
        Assert.Equal((19d * expectedTurnLength) + 100d, result.CableLengthPerCoilMillimetres, 10);
        Assert.Equal(result.CableLengthPerCoilMillimetres * 1.1d, result.TotalCableLengthMetres, 10);
        Assert.Equal(13, result.Steps.Count);
        Assert.Contains(result.Steps, step => step.Id == "actual-wound-width" && step.Warning is not null);
    }

    [Fact]
    public void Calculate_RoundCableUsesDiameterForRadialThicknessAndPitch()
    {
        var result = CoilCableLengthCalculator.Calculate(
            FlatInputs() with
            {
                Shape = CoilCableShape.Round,
                CableHeightMillimetres = 2d,
                CableWidthMillimetres = 99d,
                FinishedCoilOutsideDiameterMillimetres = 20d,
                RequiredAxialLengthMillimetres = 10d,
                TailOneMillimetres = 0d,
                TailTwoMillimetres = 0d,
                CoilQuantity = 1,
            });

        Assert.Equal(2d, result.RadialCableThicknessMillimetres, 10);
        Assert.Equal(2d, result.AxialPitchMillimetres, 10);
        Assert.Equal(16d, result.RequiredBarDiameterMillimetres, 10);
        Assert.Equal(5, result.CompleteTurns);
    }

    [Fact]
    public void Calculate_DShapeUsesTheApprovedFlatOrientation()
    {
        var result = CoilCableLengthCalculator.Calculate(
            FlatInputs() with { Shape = CoilCableShape.DShape });

        Assert.Equal(2.5d, result.RadialCableThicknessMillimetres, 10);
        Assert.Equal(4.8d, result.AxialPitchMillimetres, 10);
    }

    [Fact]
    public void Calculate_UserBarExampleRemovesOneCableHeightFromEachSide()
    {
        var result = CoilCableLengthCalculator.Calculate(
            FlatInputs() with
            {
                FinishedCoilOutsideDiameterMillimetres = 10d,
            });

        Assert.Equal(5d, result.RequiredBarDiameterMillimetres, 10);
    }

    [Fact]
    public void Calculate_AddsStripLengthsSeparatelyFromTails()
    {
        var withoutStrips = CoilCableLengthCalculator.Calculate(FlatInputs());
        var withStrips = CoilCableLengthCalculator.Calculate(
            FlatInputs() with
            {
                StripOneMillimetres = 7d,
                StripTwoMillimetres = 5d,
            });

        Assert.Equal(
            withoutStrips.CableLengthPerCoilMillimetres + 12d,
            withStrips.CableLengthPerCoilMillimetres,
            10);
    }

    [Fact]
    public void Calculate_RejectsAnImpossibleBarDiameter()
    {
        var inputs = FlatInputs() with
        {
            FinishedCoilOutsideDiameterMillimetres = 5d,
        };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CoilCableLengthCalculator.Calculate(inputs));

        Assert.Contains("greater than two cable heights", exception.Message);
    }

    [Fact]
    public void Calculate_RejectsNegativeTailOrStripLengths()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CoilCableLengthCalculator.Calculate(
                FlatInputs() with { TailOneMillimetres = -1d }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CoilCableLengthCalculator.Calculate(
                FlatInputs() with { StripTwoMillimetres = -1d }));
    }

    [Fact]
    public void Calculate_RejectsUnsupportedShapeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CoilCableLengthCalculator.Calculate(
                FlatInputs() with { Shape = (CoilCableShape)99 }));
    }

    private static CoilCableLengthInputs FlatInputs() =>
        new(
            Shape: CoilCableShape.Flat,
            CableHeightMillimetres: 2.5d,
            CableWidthMillimetres: 4.8d,
            FinishedCoilOutsideDiameterMillimetres: 13d,
            RequiredAxialLengthMillimetres: 90d,
            TailOneMillimetres: 50d,
            TailTwoMillimetres: 50d,
            StripOneMillimetres: 0d,
            StripTwoMillimetres: 0d,
            CoilQuantity: 1100);
}
