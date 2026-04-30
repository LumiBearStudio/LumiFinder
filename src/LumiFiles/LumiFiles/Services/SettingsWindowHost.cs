// =============================================================================
//  SettingsWindowHost.cs — single-instance manager for the SettingsWindow.
//
//  Behavior:
//    - ShowOrFocus(owner) creates the window on first call. Subsequent calls
//      while it's open just bring it to front and activate it (single
//      instance — no duplicate settings windows).
//    - Centered over the owner (MainWindow) on first show.
//    - On Closed, the static reference clears so a future call recreates
//      a fresh instance.
//    - When the owner closes, CloseIfOpen() is called from MainWindow's
//      OnClosed so the settings window doesn't outlive its parent and
//      keep the process alive.
//
//  This intentionally does NOT register the window with App.RegisterWindow:
//    - App.UnregisterWindow force-kills the process when the last window
//      closes; if SettingsWindow were registered and the user closed
//      MainWindow first, the process would survive on the settings window
//      alone — a confusing UX.
//    - SettingsModeView's theme/setting broadcasts already use
//      ((App)App.Current).GetRegisteredWindows() to push changes to the
//      MainWindow; the settings window itself receives theme updates via
//      the SettingsService event-binding inside SettingsModeView, so
//      registration isn't required for visual consistency.
// =============================================================================

using System;
using Microsoft.UI.Xaml;
using LumiFiles.Helpers;
using LumiFiles.Views;

namespace LumiFiles.Services
{
    public static class SettingsWindowHost
    {
        private static readonly object _lock = new();
        private static SettingsWindow? _instance;

        /// <summary>
        /// Show the settings window. If one is already open, brings it to
        /// front and activates it. Otherwise creates a new one centered
        /// over <paramref name="owner"/>.
        /// </summary>
        public static void ShowOrFocus(Window? owner)
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    try
                    {
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_instance);
                        // Restore if minimized, then bring forward.
                        if (NativeMethods.IsIconic(hwnd))
                            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
                        NativeMethods.SetForegroundWindow(hwnd);
                        _instance.Activate();
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log($"[SettingsWindowHost] Activate existing failed: {ex.Message}");
                    }
                    return;
                }

                try
                {
                    var win = new SettingsWindow();
                    win.Closed += (_, __) =>
                    {
                        lock (_lock) { _instance = null; }
                    };
                    win.Activate();

                    // Sizing/centering happens AFTER Activate() so the
                    // OS has finalized DPI for the chosen monitor.
                    win.ResizeAndCenterOver(owner ?? win);

                    _instance = win;
                }
                catch (Exception ex)
                {
                    DebugLogger.Log($"[SettingsWindowHost] Show failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// If the settings window is currently open, close it. Called from
        /// MainWindow.OnClosed so the settings window doesn't outlive its
        /// parent.
        /// </summary>
        public static void CloseIfOpen()
        {
            SettingsWindow? toClose;
            lock (_lock)
            {
                toClose = _instance;
                _instance = null;
            }
            try { toClose?.Close(); } catch { }
        }

        /// <summary>
        /// True if a settings window is currently open. Used by callers
        /// that want to switch behavior based on settings visibility (rare).
        /// </summary>
        public static bool IsOpen
        {
            get { lock (_lock) { return _instance != null; } }
        }
    }
}
