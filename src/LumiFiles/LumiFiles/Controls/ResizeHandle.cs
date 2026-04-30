// =============================================================================
//  ResizeHandle.cs — Border subclass that exposes ProtectedCursor.
//
//  WinUI 3's UIElement.ProtectedCursor is protected, so external callers can't
//  set it. Stage S-3.31's manual edge-resize zones (declared in MainWindow.xaml
//  and handled in MainWindow.Resize.cs) need to swap the cursor on hover —
//  this thin subclass gives them a public API to do so.
//
//  Used with x:Name="..." Tag="Top|Bottom|Left|Right|TopLeft|..." in XAML.
// =============================================================================

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace LumiFiles.Controls
{
    // NOTE: Microsoft.UI.Xaml.Controls.Border is sealed in WinUI 3, so we
    // inherit from Grid instead. The hit-test surface works the same way
    // (Background="Transparent" makes it pointer-interactive).
    public class ResizeHandle : Grid
    {
        /// <summary>
        /// Set the hover cursor for this handle. Call from PointerEntered.
        /// Wraps ProtectedCursor (protected on the base UIElement) so the
        /// owner Window can set it without inheriting from UIElement itself.
        /// </summary>
        public void SetResizeCursor(InputSystemCursorShape shape)
        {
            try
            {
                this.ProtectedCursor = InputSystemCursor.Create(shape);
            }
            catch
            {
                // InputSystemCursor.Create can throw on unsupported shapes;
                // ignore — the resize still works without the cursor change.
            }
        }
    }
}
