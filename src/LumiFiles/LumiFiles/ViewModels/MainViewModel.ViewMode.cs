using Microsoft.Extensions.DependencyInjection;
using LumiFiles.Models;
using LumiFiles.Services;
using System;

namespace LumiFiles.ViewModels
{
    /// <summary>
    /// MainViewModel partial — 뷰 모드 전환 및 영속화.
    /// Miller Columns/Details/Icon/Home/Settings 모드 스위칭, 듀얼 패인 별 ViewMode 관리,
    /// 미리보기 패널 토글, Split View 상태 저장/복원.
    /// </summary>
    public partial class MainViewModel
    {
        #region View Mode Switching

        /// <summary>
        /// 뷰 모드 전환 — 활성 패널에 적용
        /// </summary>
        public void SwitchViewMode(ViewMode mode)
        {
            // Settings mode: 별도 탭으로 열기
            if (mode == ViewMode.Settings)
            {
                OpenOrSwitchToSettingsTab();
                return;
            }

            // RecycleBin mode: Home과 동일하게 현재 탭에서 ViewMode 전환
            if (mode == ViewMode.RecycleBin)
            {
                if (CurrentViewMode == ViewMode.RecycleBin) return;
                // RecycleBin 전환 전 현재 ViewMode 저장 (복귀용)
                if (CurrentViewMode != ViewMode.Settings && CurrentViewMode != ViewMode.ActionLog
                    && CurrentViewMode != ViewMode.Home && CurrentViewMode != ViewMode.RecycleBin)
                    _viewModeBeforeHome = CurrentViewMode;
                ActivePane = ActivePane.Left;
                CurrentViewMode = ViewMode.RecycleBin;
                LeftViewMode = ViewMode.RecycleBin;
                if (ActiveTab != null)
                {
                    ActiveTab.ViewMode = ViewMode.RecycleBin;
                }
                UpdateActiveTabHeader();
                UpdateStatusBar();
                _ = RefreshRecycleBinInfoAsync();
                return;
            }

            // Home mode — targets whichever pane is active
            if (mode == ViewMode.Home)
            {
                if (IsSplitViewEnabled && ActivePane == ActivePane.Right)
                {
                    // Right pane → Home
                    Helpers.DebugLogger.Log($"[SwitchViewMode→Home] RightViewMode={RightViewMode} → Home");
                    if (RightViewMode == ViewMode.Home) return;
                    if (_rightPreferredViewMode == null)
                        _rightPreferredViewMode = RightViewMode;
                    RightViewMode = ViewMode.Home;
                    Helpers.DebugLogger.Log($"[MainViewModel] ViewMode changed: Home (right pane)");
                    UpdateStatusBar();
                    return;
                }

                // Left pane → Home
                Helpers.DebugLogger.Log($"[SwitchViewMode→Home] CurrentViewMode={CurrentViewMode}, _viewModeBeforeHome={_viewModeBeforeHome}, _lastClosedViewMode={_lastClosedViewMode}");
                if (CurrentViewMode == ViewMode.Home) return;
                // Home 전환 전 현재 ViewMode 저장 — 드라이브/즐겨찾기 클릭 시 이전 뷰모드 복원에 사용.
                // Settings/ActionLog는 탐색기 뷰모드가 아니므로 저장하지 않음 (복원해도 의미 없음).
                // 저장된 값은 ResolveViewModeFromHome() 또는 CloseTab()에서 소비됨.
                if (CurrentViewMode != ViewMode.Settings && CurrentViewMode != ViewMode.ActionLog && CurrentViewMode != ViewMode.RecycleBin)
                    _viewModeBeforeHome = CurrentViewMode;
                Helpers.DebugLogger.Log($"[SwitchViewMode→Home] SAVED _viewModeBeforeHome={_viewModeBeforeHome}");
                ActivePane = ActivePane.Left;
                CurrentViewMode = ViewMode.Home;
                LeftViewMode = ViewMode.Home;
                SaveViewModePreference();
                Helpers.DebugLogger.Log($"[MainViewModel] ViewMode changed: Home (left pane)");
                UpdateStatusBar();
                return;
            }

            // Determine which pane's view mode to update
            if (IsSplitViewEnabled && ActivePane == ActivePane.Right)
            {
                if (RightViewMode == mode) return;

                if (Helpers.ViewModeExtensions.IsIconMode(mode))
                {
                    CurrentIconSize = mode;
                    RightViewMode = mode;
                }
                else
                {
                    RightViewMode = mode;
                }

                RightExplorer.EnableAutoNavigation = ShouldAutoNavigate(mode);
                Helpers.DebugLogger.Log($"[MainViewModel] Right pane AutoNav: {RightExplorer.EnableAutoNavigation} (mode: {mode})");
            }
            else
            {
                if (CurrentViewMode == mode) return;

                if (Helpers.ViewModeExtensions.IsIconMode(mode))
                {
                    CurrentIconSize = mode;
                    CurrentViewMode = mode;
                    LeftViewMode = mode;
                }
                else
                {
                    CurrentViewMode = mode;
                    LeftViewMode = mode;
                }

                LeftExplorer.EnableAutoNavigation = ShouldAutoNavigate(mode);
                Helpers.DebugLogger.Log($"[MainViewModel] Left pane AutoNav: {LeftExplorer.EnableAutoNavigation} (mode: {mode})");
            }

            // 활성 탭의 ViewMode를 먼저 동기화 (UpdateActiveTabHeader가 참조하므로)
            if (ActiveTab != null)
            {
                ActiveTab.ViewMode = CurrentViewMode;
                ActiveTab.IconSize = CurrentIconSize;
            }
            SaveViewModePreference();
            UpdateActiveTabHeader();
            Helpers.DebugLogger.Log($"[MainViewModel] ViewMode changed: {Helpers.ViewModeExtensions.GetDisplayName(mode)}");
            UpdateStatusBar();
        }

        /// <summary>
        /// Determines if auto-navigation should be enabled based on view mode and MillerClickBehavior setting.
        /// </summary>
        private bool ShouldAutoNavigate(ViewMode mode)
        {
            if (mode != ViewMode.MillerColumns) return false;
            try
            {
                var settings = App.Current.Services.GetRequiredService<Services.SettingsService>();
                return settings.MillerClickBehavior != "double";
            }
            catch { return true; }
        }

        #endregion

        #region View Mode Persistence

        /// <summary>
        /// ViewMode 설정 저장 (LocalSettings)
        /// </summary>
        private void SaveViewModePreference()
        {
            try
            {
                // Don't persist Home, Settings, ActionLog or RecycleBin as startup mode
                if (CurrentViewMode == ViewMode.Home || CurrentViewMode == ViewMode.Settings || CurrentViewMode == ViewMode.ActionLog || CurrentViewMode == ViewMode.RecycleBin) return;

                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                settings.Values["ViewMode"] = (int)CurrentViewMode;
                settings.Values["IconSize"] = (int)CurrentIconSize;
                settings.Values["LeftViewMode"] = (int)LeftViewMode;
                settings.Values["RightViewMode"] = (int)RightViewMode;
                Helpers.DebugLogger.Log($"[MainViewModel] ViewMode saved: L={LeftViewMode}, R={RightViewMode}, IconSize={CurrentIconSize}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveViewModePreference error: {ex.Message}");
            }
        }

        /// <summary>
        /// ViewMode 설정 로드 (앱 시작 시)
        /// </summary>
        public void LoadViewModePreference()
        {
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;

                if (settings.Values.TryGetValue("ViewMode", out var mode) && mode is int modeInt
                    && System.Enum.IsDefined(typeof(ViewMode), modeInt))
                {
                    CurrentViewMode = (ViewMode)modeInt;
                    LeftViewMode = CurrentViewMode;
                }

                if (settings.Values.TryGetValue("IconSize", out var size) && size is int sizeInt
                    && System.Enum.IsDefined(typeof(ViewMode), sizeInt))
                {
                    CurrentIconSize = (ViewMode)sizeInt;
                }

                if (settings.Values.TryGetValue("LeftViewMode", out var leftMode) && leftMode is int leftInt
                    && System.Enum.IsDefined(typeof(ViewMode), leftInt))
                {
                    LeftViewMode = (ViewMode)leftInt;
                    CurrentViewMode = LeftViewMode;
                }

                if (settings.Values.TryGetValue("RightViewMode", out var rightMode) && rightMode is int rightInt
                    && System.Enum.IsDefined(typeof(ViewMode), rightInt))
                {
                    RightViewMode = (ViewMode)rightInt;
                }

                // Split view: 항상 단일 패널로 시작 (분할 상태는 세션 복원하지 않음)
                IsSplitViewEnabled = false;

                // Preview: 설정에서 기본값 로드 (DefaultPreviewEnabled)
                var settingsSvc = App.Current.Services.GetRequiredService<SettingsService>();
                var previewDefault = settingsSvc.DefaultPreviewEnabled;
                IsLeftPreviewEnabled = previewDefault;
                IsRightPreviewEnabled = previewDefault;

                // Set auto-navigation based on loaded view mode
                LeftExplorer.EnableAutoNavigation = ShouldAutoNavigate(LeftViewMode);
                RightExplorer.EnableAutoNavigation = ShouldAutoNavigate(RightViewMode);
                Helpers.DebugLogger.Log($"[MainViewModel] AutoNav: L={LeftExplorer.EnableAutoNavigation}, R={RightExplorer.EnableAutoNavigation}");

                Helpers.DebugLogger.Log($"[MainViewModel] ViewMode loaded: L={Helpers.ViewModeExtensions.GetDisplayName(LeftViewMode)}, R={Helpers.ViewModeExtensions.GetDisplayName(RightViewMode)}, Split={IsSplitViewEnabled}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadViewModePreference error: {ex.Message}");
                CurrentViewMode = ViewMode.MillerColumns;
                LeftViewMode = ViewMode.MillerColumns;
                RightViewMode = ViewMode.MillerColumns;
                LeftExplorer.EnableAutoNavigation = ShouldAutoNavigate(ViewMode.MillerColumns);
                RightExplorer.EnableAutoNavigation = ShouldAutoNavigate(ViewMode.MillerColumns);
            }
        }

        #endregion

        #region Preview / Split View State

        /// <summary>
        /// Toggle preview panel for the active pane.
        /// </summary>
        public void TogglePreview()
        {
            if (ActivePane == ActivePane.Left)
                IsLeftPreviewEnabled = !IsLeftPreviewEnabled;
            else
                IsRightPreviewEnabled = !IsRightPreviewEnabled;

            SavePreviewState();
        }

        /// <summary>
        /// Save preview panel state to LocalSettings.
        /// </summary>
        public void SavePreviewState()
        {
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                settings.Values["IsLeftPreviewEnabled"] = IsLeftPreviewEnabled;
                settings.Values["IsRightPreviewEnabled"] = IsRightPreviewEnabled;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainViewModel] Error saving preview state: {ex.Message}");
            }
        }

        /// <summary>
        /// Save preview panel widths (called from MainWindow on close).
        /// </summary>
        public void SavePreviewWidths(double leftWidth, double rightWidth)
        {
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                settings.Values["LeftPreviewWidth"] = leftWidth;
                settings.Values["RightPreviewWidth"] = rightWidth;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainViewModel] Error saving preview widths: {ex.Message}");
            }
        }

        /// <summary>
        /// Save split view state to LocalSettings
        /// </summary>
        private void SaveSplitViewState()
        {
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                settings.Values["IsSplitViewEnabled"] = IsSplitViewEnabled;

                // Save right pane path for restore on next launch
                if (!string.IsNullOrEmpty(RightExplorer?.CurrentPath) && RightExplorer.CurrentPath != "PC")
                {
                    settings.Values["RightPanePath"] = RightExplorer.CurrentPath;
                }

                Helpers.DebugLogger.Log($"[MainViewModel] Split state saved: {IsSplitViewEnabled}");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainViewModel] Error saving split state: {ex.Message}");
            }
        }

        #endregion
    }
}
