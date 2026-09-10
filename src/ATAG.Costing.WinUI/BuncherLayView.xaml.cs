using ATAG.Costing.WinUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ATAG.Costing.WinUI;

public sealed partial class BuncherLayView : UserControl
{
    public BuncherLayView()
    {
        InitializeComponent();
        DataContextChanged += (_, args) =>
        {
            if (args.NewValue is BuncherLayViewModel viewModel)
            {
                LivePreview.BindSource(viewModel, () => PreviewSceneAdapters.CreateBuncher(viewModel));
            }
        };
    }

    public void SetPreviewActive(bool active) =>
        WorkspaceShell.SetWorkspaceActive(active);
}
