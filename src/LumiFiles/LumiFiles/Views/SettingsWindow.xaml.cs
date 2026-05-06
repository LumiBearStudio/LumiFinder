// =============================================================================
//  SettingsWindow.xaml.cs — separate borderless rounded glass window hosting
//  the SettingsModeView UserControl.
//
//  Architecture:
//    - Mirrors MainWindow's borderless setup so visual language matches:
//      WS_POPUP (no chrome) + DwmExtendFrameIntoClientArea(-1) + DONOTROUND
//      + SetWindowRgn for custom 18 px rounded corners + DesktopAcrylic
//      backdrop. The same constants/helpers used by MainWindow are reused
//      via Helpers.NativeMethods.
//
//    - FIXED SIZE (1000 x 750). No WS_THICKFRAME, no resize handles. The
//      window is sized once on creation and centered over its owner; the
//      user cannot drag-resize.
//
//    - Caption: only a close button. No minimize / maximize because a
//      fixed-size settings dialog has no use for them.
//
//    - Drag: AppTitleBar is set as the OS drag region via SetTitleBar so
//      the user can move the window by the caption.
//
//    - SettingsModeView (existing UserControl, unchanged) is the entire
//      Grid.Row=1 content — all 10 sections, all logic, ported verbatim.
// =============================================================================

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using LumiFiles.Services;

namespace LumiFiles.Views
{
    public sealed partial class SettingsWindow : Window
    {
        // Fixed window size in DIPs (Device-Independent Pixels). Conversion
        // to physical pixels happens via DPI scale at activation time.
        public const int DesignWidth  = 1000;
        public const int DesignHeight = 750;

        // Win32 message subclass plumbing (mirrored from MainWindow).
        private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);
        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);
        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass);
        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        private const int WM_GETMINMAXINFO = 0x0024;

        // S-3.34 재시도: WM_DPICHANGED — 듀얼 모니터에서 다른 DPI 이동 시 발생.
        private const int WM_DPICHANGED = 0x02E0;

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public Helpers.NativeMethods.POINT ptReserved;
            public Helpers.NativeMethods.POINT ptMaxSize;
            public Helpers.NativeMethods.POINT ptMaxPosition;
            public Helpers.NativeMethods.POINT ptMinTrackSize;
            public Helpers.NativeMethods.POINT ptMaxTrackSize;
        }

        private IntPtr _hwnd;
        private SUBCLASSPROC? _subclassProc;
        private bool _isClosed = false;
        private SettingsService? _settingsService;
        private Action<string, object?>? _settingChangedHandler;

        public SettingsWindow()
        {
            this.InitializeComponent();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Title in the OS window manager / taskbar.
            this.Title = "Lumi Files Settings";

            // ── 1. Acrylic backdrop (DesktopAcrylic > Mica > none, mirrors MainWindow) ──
            try
            {
                if (DesktopAcrylicController.IsSupported())
                    SystemBackdrop = new DesktopAcrylicBackdrop();
                else if (MicaController.IsSupported())
                    SystemBackdrop = new MicaBackdrop();
                else
                    SystemBackdrop = null;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[SettingsWindow] SystemBackdrop init failed: {ex.Message}");
                SystemBackdrop = null;
            }

            // ── 2. Borderless setup (same pattern as MainWindow Stage S-3.21+) ──
            try
            {
                // Hide presenter chrome.
                if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.SetBorderAndTitleBar(false, false);
                    presenter.IsResizable   = false;  // FIXED size — no edge drag.
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = false;
                }

                // Sheet of glass.
                var margins = new Helpers.NativeMethods.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                Helpers.NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                // Strip WS_OVERLAPPEDWINDOW → WS_POPUP. NO WS_THICKFRAME (fixed
                // size; this also keeps SetWindowRgn round intact, since
                // WS_THICKFRAME is the bit that makes Windows clobber the
                // region on style change).
                uint style = unchecked((uint)Helpers.NativeMethods.GetWindowLong(
                    _hwnd, Helpers.NativeMethods.GWL_STYLE));
                style &= ~Helpers.NativeMethods.WS_OVERLAPPEDWINDOW;
                style |= Helpers.NativeMethods.WS_POPUP | Helpers.NativeMethods.WS_CLIPCHILDREN;
                Helpers.NativeMethods.SetWindowLong(_hwnd, Helpers.NativeMethods.GWL_STYLE, unchecked((int)style));

                // Suppress Win11's default ~8 px corner round so our 18 px
                // SetWindowRgn shape is the only visible curve.
                int pref = Helpers.NativeMethods.DWMWCP_DONOTROUND;
                Helpers.NativeMethods.DwmSetWindowAttribute(
                    _hwnd, Helpers.NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref pref, sizeof(int));

                // Drag region: the entire AppTitleBar grid acts as caption.
                if (ExtendsContentIntoTitleBar)
                {
                    SetTitleBar(AppTitleBar);
                }
                else
                {
                    ExtendsContentIntoTitleBar = true;
                    SetTitleBar(AppTitleBar);
                }

                // Win32 subclass for WM_GETMINMAXINFO (defensive — even
                // though the window can't be maximized, leftover OS routing
                // can still send this).
                _subclassProc = new SUBCLASSPROC(WndProc);
                SetWindowSubclass(_hwnd, _subclassProc, IntPtr.Zero, IntPtr.Zero);

                // Apply the rounded region. RootGrid.SizeChanged re-applies
                // it on any layout change (e.g. DPI shift).
                ApplyRoundedWindowRegion();
                if (RootGrid != null)
                {
                    RootGrid.SizeChanged += (_, __) => ApplyRoundedWindowRegion();
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[SettingsWindow] Borderless init failed: {ex.Message}");
            }

            // ── 3. Wire SettingsModeView's BackRequested → close window ──
            SettingsView.BackRequested += (_, __) => this.Close();

            // ── 4. Apply current theme to SettingsWindow's own content + subscribe
            //       so that subsequent theme/font/density changes (made from
            //       within this very window) reflect on this window too.
            //       (MainWindow has its own subscription that handles itself; we
            //       can't share that subscription because it applies to MainWindow's
            //       content tree, not ours.)
            try
            {
                _settingsService = App.Current.Services.GetRequiredService<SettingsService>();
                ApplyThemeToSelf(_settingsService.Theme);

                _settingChangedHandler = (key, value) =>
                {
                    if (_isClosed) return;
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_isClosed) return;
                        if (key == "Theme")
                            ApplyThemeToSelf(value as string ?? "system");
                    });
                };
                _settingsService.SettingChanged += _settingChangedHandler;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[SettingsWindow] Theme subscribe failed: {ex.Message}");
            }

            // ── 5. Lifecycle ──
            this.Closed += (_, __) =>
            {
                _isClosed = true;
                try
                {
                    if (_settingsService != null && _settingChangedHandler != null)
                        _settingsService.SettingChanged -= _settingChangedHandler;
                }
                catch { }
                try
                {
                    if (_subclassProc != null)
                        RemoveWindowSubclass(_hwnd, _subclassProc, IntPtr.Zero);
                }
                catch { }
            };
        }

        /// <summary>
        /// Apply Light/Dark/System theme to this window's own content tree.
        /// Mirrors the simplified ApplyTheme in MainWindow.SettingsHandler.cs
        /// (Stage S-3.32) — Light/Dark only, system default is the fallback.
        /// </summary>
        private void ApplyThemeToSelf(string theme)
        {
            try
            {
                if (this.Content is FrameworkElement root)
                {
                    var target = theme switch
                    {
                        "light" => ElementTheme.Light,
                        "dark"  => ElementTheme.Dark,
                        _       => ElementTheme.Default,
                    };
                    // Reverse → target toggle forces {ThemeResource} re-evaluation.
                    root.RequestedTheme = target == ElementTheme.Light
                        ? ElementTheme.Dark : ElementTheme.Light;
                    root.RequestedTheme = target;
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[SettingsWindow] ApplyThemeToSelf failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Resize the window to the design size (1000 x 750 DIP) and center
        /// over the given owner window. Called by SettingsWindowHost after
        /// Activate() so the OS has finalized DPI. The computed rect is
        /// clamped to the target monitor's work area so the title bar is
        /// always reachable (matches MainWindow.RestoreWindowPlacement).
        /// </summary>
        public void ResizeAndCenterOver(Window owner)
        {
            try
            {
                uint dpi = Helpers.NativeMethods.GetDpiForWindow(_hwnd);
                double scale = dpi > 0 ? dpi / 96.0 : 1.0;
                int w = (int)Math.Round(DesignWidth  * scale);
                int h = (int)Math.Round(DesignHeight * scale);

                int x, y;
                if (owner != null)
                {
                    var ownerHwnd = WinRT.Interop.WindowNative.GetWindowHandle(owner);
                    if (Helpers.NativeMethods.GetWindowRect(ownerHwnd, out var or))
                    {
                        int ownerCx = (or.Left + or.Right)  / 2;
                        int ownerCy = (or.Top  + or.Bottom) / 2;
                        x = ownerCx - w / 2;
                        y = ownerCy - h / 2;
                    }
                    else
                    {
                        x = 100; y = 100;
                    }
                }
                else
                {
                    x = 100; y = 100;
                }

                // ── Monitor work-area nudge ──
                // 정책: 설정창은 항상 owner(메인 창) 중앙에 떠야 한다. 단 그 결과가
                // 작업영역을 벗어나 타이틀바가 잡히지 않으면 owner-centered 좌표를
                // 작업영역 안으로 밀어 넣는다 (모니터 중앙으로 점프시키지 않음 —
                // 메인 창이 가장자리에 있을 때 설정창이 멀어지는 걸 방지).
                var targetRect = new Helpers.NativeMethods.RECT
                {
                    Left = x, Top = y, Right = x + w, Bottom = y + h
                };
                var hMonitor = Helpers.NativeMethods.MonitorFromRect(
                    ref targetRect, Helpers.NativeMethods.MONITOR_DEFAULTTONEAREST);
                if (hMonitor != IntPtr.Zero)
                {
                    var monInfo = new Helpers.NativeMethods.MONITORINFO();
                    monInfo.cbSize = Marshal.SizeOf<Helpers.NativeMethods.MONITORINFO>();
                    if (Helpers.NativeMethods.GetMonitorInfo(hMonitor, ref monInfo))
                    {
                        var work = monInfo.rcWork;
                        int workW = work.Right - work.Left;
                        int workH = work.Bottom - work.Top;
                        int origX = x, origY = y, origW = w, origH = h;

                        // 1) 작업영역보다 크면 축소
                        if (w > workW) w = workW;
                        if (h > workH) h = workH;

                        // 2) 위치 클램프: 창이 작업영역 안에 완전히 들어오도록 nudge
                        //    (size를 먼저 줄였으므로 항상 fit 가능)
                        if (x + w > work.Right)  x = work.Right  - w;
                        if (y + h > work.Bottom) y = work.Bottom - h;
                        if (x < work.Left)       x = work.Left;
                        if (y < work.Top)        y = work.Top;

                        if (origX != x || origY != y || origW != w || origH != h)
                        {
                            Helpers.DebugLogger.Log(
                                $"[SettingsWindow] nudged into work area " +
                                $"{work.Left},{work.Top} {workW}x{workH}: " +
                                $"({origX},{origY} {origW}x{origH}) → ({x},{y} {w}x{h})");
                        }
                    }
                }

                Helpers.NativeMethods.SetWindowPos(
                    _hwnd, IntPtr.Zero, x, y, w, h,
                    Helpers.NativeMethods.SWP_NOZORDER | Helpers.NativeMethods.SWP_NOACTIVATE);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[SettingsWindow] ResizeAndCenterOver failed: {ex.Message}");
            }
        }

        private void OnCaptionCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Hover state for the custom-shaped close button. Mirrors the
        // MainWindow pattern.
        private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush _captionCloseHoverBrush
            = new(Windows.UI.Color.FromArgb(0xFF, 0xE8, 0x11, 0x23));
        private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush _captionCloseHoverGlyphBrush
            = new(Microsoft.UI.Colors.White);

        private void OnCaptionClosePointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            try
            {
                if (CaptionCloseHoverBg != null) CaptionCloseHoverBg.Background = _captionCloseHoverBrush;
                if (CaptionCloseGlyph != null)  CaptionCloseGlyph.Foreground  = _captionCloseHoverGlyphBrush;
            }
            catch { }
        }

        private void OnCaptionClosePointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            try
            {
                if (CaptionCloseHoverBg != null)
                    CaptionCloseHoverBg.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                if (CaptionCloseGlyph != null)
                {
                    if (Application.Current.Resources.ThemeDictionaries.TryGetValue(
                            this.Content is FrameworkElement fe && fe.ActualTheme == ElementTheme.Light ? "Light" : "Dark",
                            out var dictObj)
                        && dictObj is ResourceDictionary dict
                        && dict.TryGetValue("LumiTextTertiaryBrush", out var brushObj)
                        && brushObj is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
                    {
                        CaptionCloseGlyph.Foreground = brush;
                    }
                }
            }
            catch { }
        }

        private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            if (uMsg == WM_DPICHANGED)
            {
                // S-3.34 재시도: DPI 변경 시 권장 RECT로 SetWindowPos 후 region 재계산.
                try
                {
                    if (lParam != IntPtr.Zero)
                    {
                        var suggested = Marshal.PtrToStructure<Helpers.NativeMethods.RECT>(lParam);
                        Helpers.NativeMethods.SetWindowPos(
                            hWnd, IntPtr.Zero,
                            suggested.Left, suggested.Top,
                            suggested.Right - suggested.Left,
                            suggested.Bottom - suggested.Top,
                            Helpers.NativeMethods.SWP_NOZORDER | Helpers.NativeMethods.SWP_NOACTIVATE);
                    }
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                        () => { if (!_isClosed) ApplyRoundedWindowRegion(); });
                    Helpers.DebugLogger.Log($"[SettingsWindow] WM_DPICHANGED: dpi={(int)wParam & 0xFFFF}");
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[SettingsWindow] WM_DPICHANGED error: {ex.Message}");
                }
                return IntPtr.Zero;
            }
            else if (uMsg == WM_GETMINMAXINFO)
            {
                // Defensive: even though IsMaximizable=false, lock the max
                // size to the work area to prevent any accidental coverage
                // of the taskbar.
                try
                {
                    IntPtr hMon = MonitorFromWindow(hWnd, Helpers.NativeMethods.MONITOR_DEFAULTTONEAREST);
                    if (hMon != IntPtr.Zero)
                    {
                        var mi = new Helpers.NativeMethods.MONITORINFO();
                        mi.cbSize = Marshal.SizeOf<Helpers.NativeMethods.MONITORINFO>();
                        if (Helpers.NativeMethods.GetMonitorInfo(hMon, ref mi))
                        {
                            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                            mmi.ptMaxPosition.X = mi.rcWork.Left - mi.rcMonitor.Left;
                            mmi.ptMaxPosition.Y = mi.rcWork.Top  - mi.rcMonitor.Top;
                            mmi.ptMaxSize.X     = mi.rcWork.Right  - mi.rcWork.Left;
                            mmi.ptMaxSize.Y     = mi.rcWork.Bottom - mi.rcWork.Top;
                            Marshal.StructureToPtr(mmi, lParam, false);
                            return IntPtr.Zero;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[SettingsWindow] WM_GETMINMAXINFO error: {ex.Message}");
                }
            }
            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        /// <summary>
        /// SetWindowRgn the OS region to a rounded rect at LumiWindowCornerRadius
        /// (18) + 6 px GDI-staircase headroom. Identical pattern to MainWindow.
        /// </summary>
        private void ApplyRoundedWindowRegion()
        {
            try
            {
                if (_hwnd == IntPtr.Zero || _isClosed) return;

                int widthPx  = AppWindow.Size.Width;
                int heightPx = AppWindow.Size.Height;
                if (widthPx <= 0 || heightPx <= 0) return;

                uint dpi = Helpers.NativeMethods.GetDpiForWindow(_hwnd);
                double scale = dpi > 0 ? dpi / 96.0 : 1.0;

                // Stage S-3.32: match SetWindowRgn radius to XAML CornerRadius
                // exactly (no +padding). See MainWindow.ApplyRoundedWindowRegion
                // for the rationale — this keeps the WindowFrame's 1 px
                // Border outline sitting at the actual visible window edge,
                // making the gradient hairline read as a sharp continuous
                // line at the corners. Pattern lifted from DragShelf.
                // S-3.34 (incremental fix #1): Math.Round → Math.Floor — fractional DPI 자글거림 제거.
                int radiusPx = (int)Math.Floor(18 * scale);
                if (radiusPx < 1) radiusPx = 1;

                IntPtr rgn = Helpers.NativeMethods.CreateRoundRectRgn(
                    0, 0, widthPx + 1, heightPx + 1,
                    radiusPx * 2, radiusPx * 2);
                if (rgn == IntPtr.Zero) return;

                Helpers.NativeMethods.SetWindowRgn(_hwnd, rgn, true);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[SettingsWindow] ApplyRoundedWindowRegion failed: {ex.Message}");
            }
        }
    }
}
