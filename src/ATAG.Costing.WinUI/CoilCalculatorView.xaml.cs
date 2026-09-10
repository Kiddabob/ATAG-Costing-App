using ATAG.Costing.WinUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ATAG.Costing.WinUI;

public sealed partial class CoilCalculatorView : UserControl
{
    public CoilCalculatorView()
    {
        InitializeComponent();
        DataContextChanged += (_, args) =>
        {
            if (args.NewValue is CoilCalculatorViewModel viewModel)
            {
                LivePreview.BindSource(viewModel, () => PreviewSceneAdapters.CreateCoil(viewModel));
            }
        };
    }

    public void SetPreviewActive(bool active) =>
        WorkspaceShell.SetWorkspaceActive(active);
}
