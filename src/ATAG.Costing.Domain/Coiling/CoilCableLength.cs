using System.Globalization;
using ATAG.Costing.Domain.Calculations;

namespace ATAG.Costing.Domain.Coiling;

public enum CoilCableShape
{
    Round,
    Flat,
    DShape,
}

public sealed record CoilCableLengthInputs(
    CoilCableShape Shape,
    double CableHeightMillimetres,
    double CableWidthMillimetres,
    double FinishedCoilOutsideDiameterMillimetres,
    double RequiredAxialLengthMillimetres,
    double TailOneMillimetres,
    double TailTwoMillimetres,
    double StripOneMillimetres,
    double StripTwoMillimetres,
    int CoilQuantity);

public sealed record CoilCableLengthResult(
    double RadialCableThicknessMillimetres,
    double AxialPitchMillimetres,
    double RequiredBarDiameterMillimetres,
    double MeanPathDiameterMillimetres,
    int CompleteTurns,
    double ActualWoundAxialLengthMillimetres,
    double AxialOverrunMillimetres,
    double HelicalLengthPerTurnMillimetres,
    double WoundCableLengthMillimetres,
    double EndAndStripLengthMillimetres,
    double CableLengthPerCoilMillimetres,
    double TotalCableLengthMetres,
    IReadOnlyList<CalculationStep> Steps);

/// <summary>
/// Plans a one-layer production coil. Every turn completes 360 degrees so the
/// two tails leave the coil parallel. Round cable uses its diameter as both the
/// radial thickness and axial pitch. Flat and D-shaped cable use height
/// radially and width as the no-gap axial pitch.
/// </summary>
public static class CoilCableLengthCalculator
{
    public const string RuleVersion = "coil-cable-length/v1";

    public static CoilCableLengthResult Calculate(CoilCableLengthInputs inputs)
    {
        Validate(inputs);

        var radialThickness = inputs.CableHeightMillimetres;
        var axialPitch = inputs.Shape == CoilCableShape.Round
            ? inputs.CableHeightMillimetres
            : inputs.CableWidthMillimetres;
        var barDiameter =
            inputs.FinishedCoilOutsideDiameterMillimetres -
            (2d * radialThickness);
        if (barDiameter <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Finished coil outside diameter must be greater than two cable heights.");
        }

        var meanPathDiameter = barDiameter + radialThickness;
        var rawTurns = inputs.RequiredAxialLengthMillimetres / axialPitch;
        if (!double.IsFinite(rawTurns) || rawTurns > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Required axial length and cable pitch produce too many turns to calculate safely.");
        }

        var completeTurns = checked((int)Math.Ceiling(rawTurns));
        var actualWoundLength = completeTurns * axialPitch;
        var axialOverrun = actualWoundLength - inputs.RequiredAxialLengthMillimetres;
        var circumference = Math.PI * meanPathDiameter;
        var helicalLengthPerTurn = Math.Sqrt(
            Math.Pow(circumference, 2d) + Math.Pow(axialPitch, 2d));
        var woundCableLength = completeTurns * helicalLengthPerTurn;
        var endAndStripLength =
            inputs.TailOneMillimetres +
            inputs.TailTwoMillimetres +
            inputs.StripOneMillimetres +
            inputs.StripTwoMillimetres;
        var cableLengthPerCoil = woundCableLength + endAndStripLength;
        var totalCableLengthMetres =
            cableLengthPerCoil * inputs.CoilQuantity / 1000d;
        ValidateCalculatedValues(
            barDiameter,
            meanPathDiameter,
            actualWoundLength,
            axialOverrun,
            circumference,
            helicalLengthPerTurn,
            woundCableLength,
            endAndStripLength,
            cableLengthPerCoil,
            totalCableLengthMetres);

        var steps = BuildSteps(
            inputs,
            radialThickness,
            axialPitch,
            barDiameter,
            meanPathDiameter,
            rawTurns,
            completeTurns,
            actualWoundLength,
            axialOverrun,
            circumference,
            helicalLengthPerTurn,
            woundCableLength,
            endAndStripLength,
            cableLengthPerCoil,
            totalCableLengthMetres);

        return new(
            radialThickness,
            axialPitch,
            barDiameter,
            meanPathDiameter,
            completeTurns,
            actualWoundLength,
            axialOverrun,
            helicalLengthPerTurn,
            woundCableLength,
            endAndStripLength,
            cableLengthPerCoil,
            totalCableLengthMetres,
            steps);
    }

    private static IReadOnlyList<CalculationStep> BuildSteps(
        CoilCableLengthInputs inputs,
        double radialThickness,
        double axialPitch,
        double barDiameter,
        double meanPathDiameter,
        double rawTurns,
        int completeTurns,
        double actualWoundLength,
        double axialOverrun,
        double circumference,
        double helicalLengthPerTurn,
        double woundCableLength,
        double endAndStripLength,
        double cableLengthPerCoil,
        double totalCableLengthMetres)
    {
        var shapeMeaning = inputs.Shape == CoilCableShape.Round
            ? "Round cable diameter is both the radial thickness and no-gap axial pitch."
            : $"{ShapeDisplay(inputs.Shape)} cable height is radial; cable width is the no-gap axial pitch.";

        return
        [
            Step(
                "radial-thickness",
                "Radial cable thickness",
                shapeMeaning,
                inputs.Shape == CoilCableShape.Round ? "Cable diameter" : "Cable height",
                Raw(radialThickness),
                radialThickness,
                "mm"),
            Step(
                "axial-pitch",
                "Axial pitch per turn",
                "Adjacent windings touch, so one turn advances by one cable width with no gap.",
                inputs.Shape == CoilCableShape.Round ? "Cable diameter" : "Cable width",
                Raw(axialPitch),
                axialPitch,
                "mm"),
            Step(
                "bar-diameter",
                "Required bar diameter",
                "The cable occupies one radial thickness on each side of the finished coil.",
                "Finished outside diameter − (2 × radial thickness)",
                $"{Raw(inputs.FinishedCoilOutsideDiameterMillimetres)} − (2 × {Raw(radialThickness)})",
                barDiameter,
                "mm"),
            Step(
                "mean-path-diameter",
                "Cable centreline diameter",
                "Cable length follows its centreline, halfway through the radial cable thickness.",
                "Bar diameter + radial thickness",
                $"{Raw(barDiameter)} + {Raw(radialThickness)}",
                meanPathDiameter,
                "mm"),
            Step(
                "raw-turns",
                "Unrounded turns required",
                "Required axial length divided by the no-gap pitch.",
                "Required axial length ÷ axial pitch",
                $"{Raw(inputs.RequiredAxialLengthMillimetres)} ÷ {Raw(axialPitch)}",
                rawTurns,
                "turns",
                "Shown to 4 decimal places before the production rounding step."),
            Step(
                "complete-turns",
                "Complete turns",
                "The winding rounds up so the final turn completes 360° and both tails remain parallel.",
                "Ceiling(unrounded turns)",
                $"Ceiling({Raw(rawTurns)})",
                completeTurns,
                "turns",
                "Whole turns; always rounded up."),
            Step(
                "actual-wound-width",
                "Actual wound axial length",
                "Complete turns can make the finished winding slightly longer than the requested axial length.",
                "Complete turns × axial pitch",
                $"{completeTurns} × {Raw(axialPitch)}",
                actualWoundLength,
                "mm",
                warning: axialOverrun > 0.000001d
                    ? $"The complete-turn rule adds {axialOverrun:0.###} mm beyond the requested axial length."
                    : null),
            Step(
                "centreline-circumference",
                "Centreline circumference",
                "Circumference around the mean cable path before allowing for axial travel.",
                "π × cable centreline diameter",
                $"π × {Raw(meanPathDiameter)}",
                circumference,
                "mm"),
            Step(
                "helical-turn-length",
                "Cable length per complete turn",
                "One turn follows a helix: circumference around the bar plus one axial pitch.",
                "√(circumference² + axial pitch²)",
                $"√({Raw(circumference)}² + {Raw(axialPitch)}²)",
                helicalLengthPerTurn,
                "mm"),
            Step(
                "wound-cable-length",
                "Cable in the winding",
                "Complete turns multiplied by the helical cable length per turn.",
                "Complete turns × helical length per turn",
                $"{completeTurns} × {Raw(helicalLengthPerTurn)}",
                woundCableLength,
                "mm"),
            Step(
                "ends-and-strips",
                "Tails and strip additions",
                "Both tails and any entered strip lengths are separate additions outside the winding.",
                "Tail 1 + Tail 2 + Strip 1 + Strip 2",
                $"{Raw(inputs.TailOneMillimetres)} + {Raw(inputs.TailTwoMillimetres)} + " +
                $"{Raw(inputs.StripOneMillimetres)} + {Raw(inputs.StripTwoMillimetres)}",
                endAndStripLength,
                "mm"),
            Step(
                "per-coil-length",
                "Cable required per coil",
                "Wound cable plus both tails and optional strip additions.",
                "Wound cable + tails and strips",
                $"{Raw(woundCableLength)} + {Raw(endAndStripLength)}",
                cableLengthPerCoil,
                "mm"),
            Step(
                "total-cable-length",
                "Total cable required",
                "Per-coil cable multiplied by the required quantity and converted to metres.",
                "Cable per coil × quantity ÷ 1,000",
                $"{Raw(cableLengthPerCoil)} × {inputs.CoilQuantity} ÷ 1,000",
                totalCableLengthMetres,
                "m",
                "Shown to 3 decimal places; the underlying value is retained."),
        ];
    }

    private static CalculationStep Step(
        string id,
        string label,
        string meaning,
        string expression,
        string substituted,
        double value,
        string unit,
        string rounding = "Shown to 3 decimal places; the underlying value is retained.",
        string? warning = null)
    {
        var display = unit == "turns" && Math.Abs(value - Math.Round(value)) < 0.000000001d
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.###", CultureInfo.InvariantCulture);
        return new(
            id,
            label,
            expression,
            substituted,
            Convert.ToDecimal(value),
            display,
            unit,
            Warning: warning,
            BusinessMeaning: meaning,
            RoundingRule: rounding,
            RuleVersion: RuleVersion);
    }

    private static void Validate(CoilCableLengthInputs inputs)
    {
        if (!Enum.IsDefined(inputs.Shape))
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Choose Round, Flat, or D-shape cable.");
        }

        ValidatePositive(inputs.CableHeightMillimetres, "Cable height or diameter");
        if (inputs.Shape != CoilCableShape.Round)
        {
            ValidatePositive(inputs.CableWidthMillimetres, "Cable width");
        }

        ValidatePositive(
            inputs.FinishedCoilOutsideDiameterMillimetres,
            "Finished coil outside diameter");
        ValidatePositive(inputs.RequiredAxialLengthMillimetres, "Required axial length");
        ValidateNonNegative(inputs.TailOneMillimetres, "Tail 1");
        ValidateNonNegative(inputs.TailTwoMillimetres, "Tail 2");
        ValidateNonNegative(inputs.StripOneMillimetres, "Strip length 1");
        ValidateNonNegative(inputs.StripTwoMillimetres, "Strip length 2");
        if (inputs.CoilQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "Coil quantity must be a positive whole number.");
        }
    }

    private static void ValidatePositive(double value, string label)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"{label} must be greater than zero.");
        }
    }

    private static void ValidateNonNegative(double value, string label)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"{label} cannot be negative.");
        }
    }

    private static void ValidateCalculatedValues(params double[] values)
    {
        if (values.Any(value =>
                !double.IsFinite(value) ||
                Math.Abs(value) > (double)decimal.MaxValue))
        {
            throw new ArgumentOutOfRangeException(
                nameof(values),
                "The entered dimensions or quantity are too large to calculate safely.");
        }
    }

    private static string ShapeDisplay(CoilCableShape shape) => shape switch
    {
        CoilCableShape.Flat => "Flat",
        CoilCableShape.DShape => "D-shaped",
        _ => "Round",
    };

    private static string Raw(double value) =>
        value.ToString("0.############", CultureInfo.InvariantCulture);
}
