using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace ATAG.Costing.WinUI;

/// <summary>
/// WinUI resize surface with the correct horizontal resize pointer.
/// </summary>
public sealed class HorizontalResizeGrip : ContentControl
{
    public HorizontalResizeGrip()
    {
        ProtectedCursor = InputSystemCursor.Create(
            InputSystemCursorShape.SizeWestEast);
    }
}
