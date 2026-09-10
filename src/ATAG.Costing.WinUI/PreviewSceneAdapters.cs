using ATAG.Costing.Application.Visualisation;
using ATAG.Costing.WinUI.ViewModels;

namespace ATAG.Costing.WinUI;

/// <summary>Thin UI mappings; scene construction is independently testable in Application.</summary>
public static class PreviewSceneAdapters
{
    public static CablePreviewScene CreateSingleCore(SingleCoreCostingViewModel model)
    {
        var scene = ModulePreviewSceneFactory.CreateSingleCore(model.ConductorOutsideDiameterMillimetres,
            model.NominalFinishedCoreOutsideDiameterMillimetres, model.SelectedCopper?.Construction,
            model.PreviewConductorColourHex, model.PreviewInsulationColourHex);
        if (!model.HasCorePrint || scene.Components.IsEmpty) return scene;
        try
        {
            if (!double.IsFinite(model.CorePrintDotsHigh) ||
                model.CorePrintDotsHigh != Math.Truncate(model.CorePrintDotsHigh) ||
                model.CorePrintDotsHigh is < 5 or > 64)
                throw new ArgumentException("Dots high must be a whole number from 5 to 64.");
            return CablePrintedSceneBuilder.Apply(scene,
                new CablePrintSettings(model.CorePrintText, model.CorePrintDotDiameterMillimetres,
                    (int)model.CorePrintDotsHigh, model.CorePrintDotPitchHorizontalMillimetres,
                    model.CorePrintDotPitchVerticalMillimetres, model.CorePrintRepeatDistanceMillimetres),
                colour: LivePreview3DScene.FromHex(model.CorePrintColourHex));
        }
        catch (ArgumentException exception)
        {
            return scene with { Notes = scene.Notes.Add("Print not drawn: " + exception.Message) };
        }
    }

    public static CablePreviewScene CreateDual(DualInsulationCostingViewModel model)
    {
        var unspecified = new List<string>();
        if (model.IncludeTape) unspecified.Add("tape");
        if (model.IncludeChalk) unspecified.Add("chalk");
        if (model.IncludeFoil) unspecified.Add("foil");
        if (model.IncludeBraid) unspecified.Add("braid");
        if (model.IncludeLapscreen) unspecified.Add("lapscreen");
        if (model.IncludeDrainWire) unspecified.Add("drain wire");
        return ModulePreviewSceneFactory.CreateDual(model.ConductorOutsideDiameterMillimetres,
            model.FirstFinishedOutsideDiameterMillimetres, model.SecondFinishedOutsideDiameterMillimetres,
            model.SelectedCopper?.Construction, model.SelectedFirstMasterbatch?.ColourHex,
            model.SelectedSecondMasterbatch?.ColourHex, unspecified);
    }

    public static CablePreviewScene CreateBraid(BraidCoverageViewModel model)
    {
        var sixteen = model.SelectedPreviewCarrierCount == 16;
        return ModulePreviewSceneFactory.CreateBraid(model.SelectedCoreLayout,
            model.CoreOutsideDiameterMillimetres, model.MeanOutsideDiameterMillimetres,
            model.SelectedEndsPerCarrier, model.SelectedEffectiveWireDiameterMillimetres,
            model.SelectedPreviewCarrierCount,
            sixteen ? model.SixteenCarrierPitchMillimetres : model.TwentyFourCarrierPitchMillimetres,
            [sixteen ? model.SixteenCarrierCoverageDisplay : model.TwentyFourCarrierCoverageDisplay,
             sixteen ? model.SixteenCarrierStrandsDisplay : model.TwentyFourCarrierStrandsDisplay]);
    }

    public static CablePreviewScene CreateBuncher(BuncherLayViewModel model) =>
        ModulePreviewSceneFactory.CreateBuncher(model.SelectedCoreLayout, model.SelectedLayLengthMillimetres);

    public static CablePreviewScene CreateCoil(CoilCalculatorViewModel model) =>
        ModulePreviewSceneFactory.CreateCoil(model.SelectedShape?.Shape ?? Domain.Coiling.CoilCableShape.Round,
            model.PreviewResult, model.TailOneMillimetres, model.TailTwoMillimetres,
            model.StripOneMillimetres, model.StripTwoMillimetres);
}
