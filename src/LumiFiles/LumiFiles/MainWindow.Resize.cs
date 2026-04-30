// =============================================================================
//  MainWindow.Resize.cs
//
//  Stage S-3.31 — Manual window edge resize, implemented purely in user code.
//
//  WHY THIS EXISTS
//  ----------------
//  Standard Win32 borderless edge-resize requires WS_THICKFRAME + a custom
//  WM_NCCALCSIZE / WM_NCHITTEST handler so the OS can hit-test resize zones
//  and route SC_SIZE on mouse-down. We tried that path repeatedly through
//  S-3.31 attempts; every variant that successfully turned on WS_THICKFRAME
//  also clobbered the rounded SetWindowRgn (Windows rebuilds the window
//  region on style change). Trade-off: edge-resize OR rounded corners,
//  pick one.
//
//  This file picks BOTH by sidestepping the OS resize path entirely.
//  Eight transparent Borders at the WallpaperRoot level (declared in
//  MainWindow.xaml) cover the four edges and four corners. We track mouse
//  drag manually and call AppWindow.MoveAndResize. The OS window keeps
//  WS_POPUP (no WS_THICKFRAME), so SetWindowRgn never gets clobbered and
//  the 18 px rounded corners stay intact.
//
//  HOW IT WORKS
//  -------------
//  - PointerEntered → set ProtectedCursor to the appropriate resize cursor
//    so the user gets visual feedback (≡, ⇕, ⇖, ⇗, etc.).
//  - PointerPressed → capture pointer, snapshot screen-cursor pos and the
//    current AppWindow rect (physical pixels).
//  - PointerMoved during capture → compute delta in physical pixels using
//    GetCursorPos (XAML PointerRoutedEventArgs gives DIP, AppWindow uses
//    physical px — using the Win32 cursor pos avoids the DPI mismatch),
//    call AppWindow.MoveAndResize with the new rect, clamped to the
//    minimum window size.
//  - PointerReleased / CaptureLost → release capture, end resize.
// =============================================================================

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;

namespace LumiFiles
{
    public sealed partial class MainWindow
    {
        // Drag state. _isResizing acts as the active flag; the snapshots are
        // taken on PointerPressed and remain valid for the duration of the
        // drag.
        private bool _resizeActive = false;
        private string _resizeDir = string.Empty;
        private int _resizeStartCursorX;     // screen, physical px
        private int _resizeStartCursorY;
        private int _resizeStartWinX;        // AppWindow.Position
        private int _resizeStartWinY;
        private int _resizeStartWinW;        // AppWindow.Size
        private int _resizeStartWinH;

        // Minimum size the window can be resized down to. Tuned to keep the
        // sidebar + at least one column visible; adjust if mockup demands.
        private const int ResizeMinWidth  = 600;
        private const int ResizeMinHeight = 400;

        // Edge-band width to ignore when corner zones overlap (12 px corner
        // borders take priority over 4 px edge borders by virtue of being
        // declared after them in XAML — later siblings render on top).

        private void OnResizePointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Controls.ResizeHandle rh) return;
            if (rh.Tag is not string dir) return;

            var shape = dir switch
            {
                "Top"         => InputSystemCursorShape.SizeNorthSouth,
                "Bottom"      => InputSystemCursorShape.SizeNorthSouth,
                "Left"        => InputSystemCursorShape.SizeWestEast,
                "Right"       => InputSystemCursorShape.SizeWestEast,
                "TopLeft"     => InputSystemCursorShape.SizeNorthwestSoutheast,
                "BottomRight" => InputSystemCursorShape.SizeNorthwestSoutheast,
                "TopRight"    => InputSystemCursorShape.SizeNortheastSouthwest,
                "BottomLeft"  => InputSystemCursorShape.SizeNortheastSouthwest,
                _             => InputSystemCursorShape.Arrow,
            };
            rh.SetResizeCursor(shape);
        }

        private void OnResizePointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            if (fe.Tag is not string dir) return;

            // Only left mouse button initiates resize.
            var props = e.GetCurrentPoint(fe).Properties;
            if (!props.IsLeftButtonPressed) return;

            // Snapshot the current screen cursor (physical px) and AppWindow
            // rect (physical px). Using Win32 GetCursorPos avoids the DIP →
            // physical-px conversion needed if we read from the
            // PointerRoutedEventArgs (which is in DIP).
            if (!Helpers.NativeMethods.GetCursorPos(out var pt))
                return;

            _resizeStartCursorX = pt.X;
            _resizeStartCursorY = pt.Y;
            _resizeStartWinX    = AppWindow.Position.X;
            _resizeStartWinY    = AppWindow.Position.Y;
            _resizeStartWinW    = AppWindow.Size.Width;
            _resizeStartWinH    = AppWindow.Size.Height;
            _resizeDir          = dir;
            _resizeActive       = fe.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void OnResizePointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_resizeActive) return;

            if (!Helpers.NativeMethods.GetCursorPos(out var pt))
                return;

            int dx = pt.X - _resizeStartCursorX;
            int dy = pt.Y - _resizeStartCursorY;

            int newX = _resizeStartWinX;
            int newY = _resizeStartWinY;
            int newW = _resizeStartWinW;
            int newH = _resizeStartWinH;

            // Each direction tweaks (newX, newY, newW, newH) so the OPPOSITE
            // edge stays anchored. For "Left", increasing dx (cursor moves
            // right) shrinks the window from the left, so we add dx to X
            // and subtract dx from W. For "Right", we just grow W.
            switch (_resizeDir)
            {
                case "Right":
                    newW = Math.Max(ResizeMinWidth, _resizeStartWinW + dx);
                    break;

                case "Bottom":
                    newH = Math.Max(ResizeMinHeight, _resizeStartWinH + dy);
                    break;

                case "Left":
                    {
                        int proposedW = _resizeStartWinW - dx;
                        if (proposedW < ResizeMinWidth)
                        {
                            // Clamp: don't let X cross the right edge.
                            int clampDx = _resizeStartWinW - ResizeMinWidth;
                            newX = _resizeStartWinX + clampDx;
                            newW = ResizeMinWidth;
                        }
                        else
                        {
                            newX = _resizeStartWinX + dx;
                            newW = proposedW;
                        }
                    }
                    break;

                case "Top":
                    {
                        int proposedH = _resizeStartWinH - dy;
                        if (proposedH < ResizeMinHeight)
                        {
                            int clampDy = _resizeStartWinH - ResizeMinHeight;
                            newY = _resizeStartWinY + clampDy;
                            newH = ResizeMinHeight;
                        }
                        else
                        {
                            newY = _resizeStartWinY + dy;
                            newH = proposedH;
                        }
                    }
                    break;

                case "TopLeft":
                    {
                        int proposedW = _resizeStartWinW - dx;
                        int proposedH = _resizeStartWinH - dy;
                        if (proposedW >= ResizeMinWidth)  { newX = _resizeStartWinX + dx; newW = proposedW; }
                        else                              { newX = _resizeStartWinX + (_resizeStartWinW - ResizeMinWidth); newW = ResizeMinWidth; }
                        if (proposedH >= ResizeMinHeight) { newY = _resizeStartWinY + dy; newH = proposedH; }
                        else                              { newY = _resizeStartWinY + (_resizeStartWinH - ResizeMinHeight); newH = ResizeMinHeight; }
                    }
                    break;

                case "TopRight":
                    {
                        int proposedH = _resizeStartWinH - dy;
                        newW = Math.Max(ResizeMinWidth, _resizeStartWinW + dx);
                        if (proposedH >= ResizeMinHeight) { newY = _resizeStartWinY + dy; newH = proposedH; }
                        else                              { newY = _resizeStartWinY + (_resizeStartWinH - ResizeMinHeight); newH = ResizeMinHeight; }
                    }
                    break;

                case "BottomLeft":
                    {
                        int proposedW = _resizeStartWinW - dx;
                        if (proposedW >= ResizeMinWidth) { newX = _resizeStartWinX + dx; newW = proposedW; }
                        else                             { newX = _resizeStartWinX + (_resizeStartWinW - ResizeMinWidth); newW = ResizeMinWidth; }
                        newH = Math.Max(ResizeMinHeight, _resizeStartWinH + dy);
                    }
                    break;

                case "BottomRight":
                    newW = Math.Max(ResizeMinWidth,  _resizeStartWinW + dx);
                    newH = Math.Max(ResizeMinHeight, _resizeStartWinH + dy);
                    break;

                default:
                    return;
            }

            try
            {
                AppWindow.MoveAndResize(new RectInt32(newX, newY, newW, newH));
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[Resize] MoveAndResize failed: {ex.Message}");
            }
            e.Handled = true;
        }

        private void OnResizePointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndResize(sender, e);
        }

        private void OnResizePointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            EndResize(sender, e);
        }

        private void EndResize(object sender, PointerRoutedEventArgs e)
        {
            if (!_resizeActive) return;
            _resizeActive = false;
            _resizeDir = string.Empty;
            if (sender is FrameworkElement fe)
            {
                try { fe.ReleasePointerCapture(e.Pointer); } catch { }
            }
            e.Handled = true;
        }
    }
}
