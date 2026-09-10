#if DEBUG
namespace ATAG.Costing.WinUI;

public sealed partial class MainPage
{
    /// <summary>
    /// Opt-in, in-memory physical inputs for repeatable native preview acceptance.
    /// No project, material price, retained table or machine setting is saved.
    /// </summary>
    private void SeedUnifiedPreviewDiagnosticsIfRequested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(
                "ATAG_COSTING_PREVIEW_FIXTURES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        CostingViewModel.ConductorOutsideDiameterMillimetres = 2.2;
        CostingViewModel.NominalFinishedCoreOutsideDiameterMillimetres = 3.2;
        CostingViewModel.HasCorePrint = true;
        CostingViewModel.CorePrintText = "ATAG CORE 01";
        CostingViewModel.CorePrintDotDiameterMillimetres = 0.12;
        CostingViewModel.CorePrintDotsHigh = 7;
        CostingViewModel.CorePrintDotPitchHorizontalMillimetres = 0.2;
        CostingViewModel.CorePrintDotPitchVerticalMillimetres = 0.18;
        CostingViewModel.CorePrintRepeatDistanceMillimetres = 40;
        DualCostingViewModel.ConductorOutsideDiameterMillimetres = 2.2;
        DualCostingViewModel.FirstFinishedOutsideDiameterMillimetres = 3.2;
        DualCostingViewModel.SecondFinishedOutsideDiameterMillimetres = 4.8;
        CoilViewModel.CableHeightMillimetres = 2.5;
        CoilViewModel.CableWidthMillimetres = 4.8;
        CoilViewModel.FinishedCoilOutsideDiameterMillimetres = 10;
        CoilViewModel.RequiredAxialLengthMillimetres = 48;
        CoilViewModel.TailOneMillimetres = 15;
        CoilViewModel.TailTwoMillimetres = 15;
        CoilViewModel.StripOneMillimetres = 3;
        CoilViewModel.StripTwoMillimetres = 3;
        Program.Log("Unified LIVE Preview diagnostic physical inputs prepared in memory.");
    }
}
#endif
