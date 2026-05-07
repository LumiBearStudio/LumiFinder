using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using LumiFiles.Models;
using LumiFiles.ViewModels;
using LumiFiles.Services;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;

namespace LumiFiles
{
    /// <summary>
    /// MainWindow의 분할 뷰 및 미리보기 패널 관리 부분 클래스.
    /// 좌/우 패널 활성 상태 관리, 분할 뷰 토글, 미리보기 패널 초기화·업데이트,
    /// 인라인 미리보기 컬럼(Miller Columns 모드), 선택 기반 미리보기 갱신,
    /// 활성 Explorer/ScrollViewer 접근자 등을 담당한다.
    /// </summary>
    public sealed partial class MainWindow
    {
        #region Active Pane Helpers

        /// <summary>
        /// 현재 활성 패널의 Miller Columns ItemsControl을 반환한다.
        /// 분할 뷰에서 우측 패널이 활성이면 Right 컨트롤, 아니면 활성 탭의 컨트롤을 반환한다.
        /// </summary>
        private ItemsControl GetActiveMillerColumnsControl()
        {
            if (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                return MillerColumnsControlRight;
            // 활성 탭의 Miller ItemsControl 반환
            if (_activeMillerTabId != null && _tabMillerPanels.TryGetValue(_activeMillerTabId, out var panel))
                return panel.items;
            return MillerColumnsControl;
        }

        /// <summary>
        /// Returns the ScrollViewer for the currently active pane.
        /// </summary>
        private ScrollViewer GetActiveMillerScrollViewer()
        {
            if (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                return MillerScrollViewerRight;
            // 활성 탭의 ScrollViewer 반환
            if (_activeMillerTabId != null && _tabMillerPanels.TryGetValue(_activeMillerTabId, out var panel))
                return panel.scroller;
            return MillerScrollViewer;
        }

        #endregion

        #region x:Bind Visibility / Brush Helpers

        // --- x:Bind visibility/brush helpers ---

        public Visibility IsSplitVisible(bool isSplitViewEnabled)
            => isSplitViewEnabled ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsNotSplitVisible(bool isSplitViewEnabled)
            => isSplitViewEnabled ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// Unified bar (address bar + toolbar): hidden only in Settings/ActionLog mode.
        /// RecycleBin은 주소바 영역을 유지한다 (Home과 동일).
        /// </summary>
        public Visibility IsNotSettingsMode(Models.ViewMode mode)
            => (mode != Models.ViewMode.Settings && mode != Models.ViewMode.ActionLog) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Split/Preview buttons: hidden in Settings/ActionLog/RecycleBin mode
        /// </summary>
        public Visibility IsNotSpecialMode(Models.ViewMode mode)
            => (mode != Models.ViewMode.Settings && mode != Models.ViewMode.ActionLog && mode != Models.ViewMode.RecycleBin) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Settings/ActionLog 모드일 때만 표시 (탭↔콘텐츠 연결 strip)
        /// </summary>
        public Visibility IsSettingsOrActionLogMode(Models.ViewMode mode)
            => (mode == Models.ViewMode.Settings || mode == Models.ViewMode.ActionLog) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Unified Bar 하단 border: 분할뷰에서는 LeftPathHeader border와 중복되므로 제거
        /// </summary>
        public Thickness UnifiedBarBorderThickness(bool isSplitViewEnabled)
            => isSplitViewEnabled ? new Thickness(0) : new Thickness(0, 0, 0, 1);

        /// <summary>
        /// Single mode toolbar/address bar: visible when NOT split AND NOT Home mode
        /// </summary>
        public Visibility IsSingleNonHomeVisible(bool isSplitViewEnabled, Models.ViewMode mode)
            => (!isSplitViewEnabled && mode != Models.ViewMode.Home) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Single mode nav/address bar: visible when NOT split AND NOT Settings/ActionLog mode (Home included)
        /// </summary>
        public Visibility IsSingleNonSettingsVisible(bool isSplitViewEnabled, Models.ViewMode mode)
            => (!isSplitViewEnabled && mode != Models.ViewMode.Settings && mode != Models.ViewMode.ActionLog) ? Visibility.Visible : Visibility.Collapsed;

        // Stage S-3: IsLeftPaneHeaderVisible / LeftPaneActiveLabelBrush /
        // RightPaneActiveLabelBrush were used by the 32px LeftPathHeader /
        // RightPathHeader Grids, which got removed once Stage S-2 stripped them
        // of all controls and the single LumiToolbar absorbed every action. The
        // header band itself was empty by then, so it's gone.

        public double LeftPaneAccentOpacity(ActivePane activePane)
            => activePane == ActivePane.Left ? 1.0 : 0.0;

        public double RightPaneAccentOpacity(ActivePane activePane)
            => activePane == ActivePane.Right ? 1.0 : 0.0;

        // ── Active-pane indicator brushes (Stage S-3.2) ─────────────────────
        // Replaces the boxed-in 1-2px amber outline (S-3 → S-3.1 dropped) with
        // element-level color cues. Each pane container holds its own scoped
        // SolidColorBrush instances under the keys LumiPaneColumnNameBrush and
        // ListViewItemBackgroundSelected*. We mutate Color in place on the
        // already-mounted brush instances so every consumer (Miller col-header
        // text, every ListView/GridView item selection) repaints automatically
        // — no Resources swap, no re-bind, no item rebuild.
        //
        // ACTIVE pane palette: amber (LumiAmberColor + LumiAmberDeepColor).
        // INACTIVE pane palette: warm gray + slightly translucent white.
        // S-3.40: amber 색은 더 이상 하드코드 상수가 아니라 현재 ThemeDictionary 의
        //         LumiAmberColor 를 읽는다 — 사용자가 커스텀 액센트로 색을 바꾸면
        //         RefreshActivePaneIndicators 호출 시 새 색이 반영됨.
        private static readonly Windows.UI.Color s_inactiveWhite = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Windows.UI.Color s_inactiveGray  = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x80, 0x80, 0x80);
        private static readonly Windows.UI.Color s_amberFallback = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xB8, 0x6B);

        private static Windows.UI.Color GetCurrentLumiAmberColor()
        {
            try
            {
                // 1. App-level ThemeDictionaries (ApplyAccentOverride 가 mirror 해 둠)
                var appResources = Application.Current.Resources;
                string themeKey = App.Current.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light ? "Light" : "Dark";
                if (appResources.ThemeDictionaries.TryGetValue(themeKey, out var dictObj)
                    && dictObj is ResourceDictionary dict
                    && dict.TryGetValue("LumiAmberColor", out var colorObj)
                    && colorObj is Windows.UI.Color color)
                {
                    return color;
                }
                // 2. Top-level (LumiTheme.xaml 의 정적 정의)
                if (appResources.TryGetValue("LumiAmberColor", out var topColorObj)
                    && topColorObj is Windows.UI.Color topColor)
                {
                    return topColor;
                }
            }
            catch { /* fallback */ }
            return s_amberFallback;
        }

        /// <summary>
        /// Push the current ActivePane state into per-pane scoped brushes.
        /// Cheap (4 brushes × 2 panes), safe to call any time.
        /// </summary>
        internal void RefreshActivePaneIndicators()
        {
            if (LeftPaneContainer == null || RightPaneContainer == null) return;

            bool leftActive = ViewModel.ActivePane == ActivePane.Left;
            bool rightActive = ViewModel.ActivePane == ActivePane.Right;

            // In single-pane mode the right pane is hidden anyway; leave the
            // left pane's amber tint intact so it matches single-pane styling.
            if (!ViewModel.IsSplitViewEnabled)
            {
                leftActive = true;
                rightActive = false;
            }

            ApplyPaneIndicatorBrushes(LeftPaneContainer, leftActive);
            ApplyPaneIndicatorBrushes(RightPaneContainer, rightActive);
        }

        private static void ApplyPaneIndicatorBrushes(FrameworkElement pane, bool isActive)
        {
            // S-3.40: 현재 LumiAmberColor 를 읽음 (커스텀 액센트 반영).
            var amber = GetCurrentLumiAmberColor();

            // Active pane uses bright amber (LumiAmberColor) for both the col-
            // header folder name and the ListView selection — amber-deep was
            // tested first but read too dark on dark glass. Inactive pane uses
            // warm gray. Alpha bumped above WinUI's default selection family
            // because amber needs more presence than the default neutral fill.
            var nameColor = isActive
                ? amber
                : Microsoft.UI.ColorHelper.FromArgb(0x80, s_inactiveWhite.R, s_inactiveWhite.G, s_inactiveWhite.B);

            // Bottom breadcrumb (LumiPanePathBar) folder icon color. Fully
            // opaque on both states; the difference is hue (amber vs gray)
            // rather than alpha, since this icon is small and subtle alpha
            // changes get lost.
            var pathIconColor = isActive ? amber : s_inactiveGray;

            // Selection background base — bright amber on the active pane so
            // the selected row clearly says "this is where focus lives now."
            var selBase = isActive ? amber : s_inactiveGray;

            SetBrush(pane, "LumiPaneColumnNameBrush", nameColor);
            SetBrush(pane, "LumiPanePathIconBrush", pathIconColor);
            SetBrush(pane, "ListViewItemBackgroundSelected",
                Microsoft.UI.ColorHelper.FromArgb(isActive ? (byte)0x80 : (byte)0x44, selBase.R, selBase.G, selBase.B));
            SetBrush(pane, "ListViewItemBackgroundSelectedPointerOver",
                Microsoft.UI.ColorHelper.FromArgb(isActive ? (byte)0xA0 : (byte)0x66, selBase.R, selBase.G, selBase.B));
            SetBrush(pane, "ListViewItemBackgroundSelectedPressed",
                Microsoft.UI.ColorHelper.FromArgb(isActive ? (byte)0xC0 : (byte)0x88, selBase.R, selBase.G, selBase.B));
            SetBrush(pane, "ListViewItemBackgroundSelectedDisabled",
                Microsoft.UI.ColorHelper.FromArgb(isActive ? (byte)0x44 : (byte)0x22, selBase.R, selBase.G, selBase.B));
        }

        private static void SetBrush(FrameworkElement pane, string key, Windows.UI.Color color)
        {
            if (pane.Resources.TryGetValue(key, out var v) && v is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
                brush.Color = color;
        }

        #endregion

        #region Focus Tracking

        // --- Focus tracking ---

        /// <summary>
        /// 좌측 패널 GotFocus 이벤트. ActivePane을 Left로 설정한다.
        /// </summary>
        private void OnLeftPaneGotFocus(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ActivePane != ActivePane.Left)
            {
                ViewModel.ActivePane = ActivePane.Left;
            }
        }

        /// <summary>
        /// 우측 패널 GotFocus 이벤트. ActivePane을 Right로 설정한다.
        /// </summary>
        private void OnRightPaneGotFocus(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ActivePane != ActivePane.Right)
            {
                ViewModel.ActivePane = ActivePane.Right;
            }
        }

        /// <summary>
        /// 빈 공간 클릭 시에도 ActivePane을 전환하고 포커스를 이동.
        /// GotFocus는 포커스 가능 요소가 hit될 때만 발생하므로, 빈 공간에서는
        /// PointerPressed로 보완해야 함.
        /// Stage S-3: LeftPathHeader / RightPathHeader Grids are gone, so the
        /// "skip FocusActivePane when click was inside the header buttons"
        /// branch is dead code and removed. Right-click still skips focus move
        /// since the ListView handles its own item selection.
        /// </summary>
        private void OnLeftPanePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (IsDragInProgress) return;
            if (ViewModel.ActivePane != ActivePane.Left)
            {
                ViewModel.ActivePane = ActivePane.Left;
                var props = e.GetCurrentPoint(sender as UIElement).Properties;
                if (!props.IsRightButtonPressed)
                    FocusActivePane();
            }
        }

        private void OnRightPanePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (IsDragInProgress) return;
            if (ViewModel.ActivePane != ActivePane.Right)
            {
                ViewModel.ActivePane = ActivePane.Right;
                var props = e.GetCurrentPoint(sender as UIElement).Properties;
                if (!props.IsRightButtonPressed)
                    FocusActivePane();
            }
        }

        // Stage S-3: OnLeftPaneHeaderTapped / OnRightPaneHeaderTapped removed
        // along with the LeftPathHeader / RightPathHeader Grids. The pane-level
        // PointerPressed handlers above already cover header-area clicks.

        #endregion

        #region Pane-Specific Flyout / View Mode Menus

        // --- Pane-specific flyout opening handlers (set ActivePane before menu item click) ---

        private void OnLeftPaneSortMenuOpening(object sender, object e)
        {
            ViewModel.ActivePane = ActivePane.Left;
        }

        private void OnRightPaneSortMenuOpening(object sender, object e)
        {
            ViewModel.ActivePane = ActivePane.Right;
        }

        private void OnMainViewModeMenuOpening(object sender, object e)
        {
            LocalizeViewMenuItems(MainVm_Miller, MainVm_Details, MainVm_Icons,
                MainVm_ExtraLarge, MainVm_Large, MainVm_Medium, MainVm_Small);
        }

        // OnLeftPaneViewModeMenuOpening / OnRightPaneViewModeMenuOpening removed — Stage S-2
        // dropped the per-pane mini toolbars (and their view-mode flyouts). Single LumiToolbar
        // ViewMode menu now handles all panes via ActiveExplorer binding.

        private void LocalizeViewMenuItems(
            MenuFlyoutItem miller, MenuFlyoutItem details, MenuFlyoutSubItem icons,
            MenuFlyoutItem extraLarge, MenuFlyoutItem large, MenuFlyoutItem medium, MenuFlyoutItem small)
        {
            miller.Text = _loc.Get("MillerColumns");
            details.Text = _loc.Get("Details");
            icons.Text = _loc.Get("Icons");
            extraLarge.Text = _loc.Get("ExtraLargeIcons");
            large.Text = _loc.Get("LargeIcons");
            medium.Text = _loc.Get("MediumIcons");
            small.Text = _loc.Get("SmallIcons");
        }

        /// <summary>
        /// Applies all localized strings to UI elements that have hardcoded XAML text.
        /// Called once at startup and again whenever <see cref="Services.LocalizationService.LanguageChanged"/> fires.
        /// </summary>
        private void LocalizeViewModeTooltips()
        {
            // --- Toolbar tooltips (single-pane mode) ---
            ToolTipService.SetToolTip(NewTabButton, _loc.Get("Tooltip_NewTab"));
            ToolTipService.SetToolTip(BackButton, _loc.Get("Tooltip_Back"));
            ToolTipService.SetToolTip(ForwardButton, _loc.Get("Tooltip_Forward"));
            ToolTipService.SetToolTip(UpButton, _loc.Get("Tooltip_Up"));
            ToolTipService.SetToolTip(CopyPathButton, _loc.Get("Tooltip_CopyPath"));
            ToolTipService.SetToolTip(NewFolderButton, _loc.Get("Tooltip_NewFolder"));
            ToolTipService.SetToolTip(NewItemDropdown, _loc.Get("Tooltip_NewFile"));
            ToolTipService.SetToolTip(ToolbarCutButton, _loc.Get("Tooltip_Cut"));
            ToolTipService.SetToolTip(ToolbarCopyButton, _loc.Get("Tooltip_Copy"));
            ToolTipService.SetToolTip(ToolbarPasteButton, _loc.Get("Tooltip_Paste"));
            ToolTipService.SetToolTip(ToolbarRenameButton, _loc.Get("Tooltip_Rename"));
            ToolTipService.SetToolTip(ToolbarDeleteButton, _loc.Get("Tooltip_Delete"));
            ToolTipService.SetToolTip(SortButton, _loc.Get("Tooltip_Sort"));
            ToolTipService.SetToolTip(SplitViewButton, _loc.Get("Tooltip_SplitView"));
            ToolTipService.SetToolTip(PreviewToggleButton, _loc.Get("Tooltip_Preview"));

            // View mode button tooltip (single toolbar — per-pane mini buttons removed in Stage S-2)
            ToolTipService.SetToolTip(ViewModeButton, _loc.Get("ViewModeSwitch"));

            // Sidebar bottom bar tooltips
            ToolTipService.SetToolTip(HelpButton, _loc.Get("Tooltip_Help"));
            ToolTipService.SetToolTip(LogButton, _loc.Get("Tooltip_Log"));
            ToolTipService.SetToolTip(SettingsButton, _loc.Get("Tooltip_Settings"));

            // --- Search placeholder ---
            SearchBox.PlaceholderText = _loc.Get("SearchPlaceholder");

            // --- Sidebar section labels ---
            SidebarHomeText.Text = _loc.Get("Home");
            SidebarFavoritesText.Text = _loc.Get("Favorites");
            SidebarLocalDrivesText.Text = _loc.Get("LocalDrives");
            SidebarCloudText.Text = _loc.Get("Cloud");
            SidebarNetworkText.Text = _loc.Get("Network");
            RecycleBinLabel.Text = _loc.Get("RecycleBin");

            // --- Main sort menu items ---
            SortByNameItem.Text = _loc.Get("Name");
            SortByDateItem.Text = _loc.Get("Date");
            SortBySizeItem.Text = _loc.Get("Size");
            SortByTypeItem.Text = _loc.Get("Type");
            SortAscendingItem.Text = _loc.Get("Ascending");
            SortDescendingItem.Text = _loc.Get("Descending");
            GroupBySubMenu.Text = _loc.Get("GroupBy");
            GroupByNoneItem.Text = _loc.Get("None");
            GroupByNameItem.Text = _loc.Get("Name");
            GroupByTypeItem.Text = _loc.Get("Type");
            GroupByDateItem.Text = _loc.Get("Date");
            GroupBySizeItem.Text = _loc.Get("Size");

            // --- Main view mode menu items ---
            MainVm_Miller.Text = _loc.Get("MillerColumns");
            MainVm_Details.Text = _loc.Get("Details");
            MainVm_List.Text = _loc.Get("ViewMode_List");
            MainVm_Icons.Text = _loc.Get("Icons");
            MainVm_ExtraLarge.Text = _loc.Get("ExtraLargeIcons");
            MainVm_Large.Text = _loc.Get("LargeIcons");
            MainVm_Medium.Text = _loc.Get("MediumIcons");
            MainVm_Small.Text = _loc.Get("SmallIcons");

            // Stage S-2: per-pane mini toolbar deleted, so all the per-pane tooltip /
            // sort-flyout / view-mode-flyout localization lines are gone. The single
            // LumiToolbar at the top covers every pane via ActiveExplorer binding.

            // --- Tab headers (Home / Settings / ActionLog) ---
            foreach (var tab in ViewModel.Tabs)
            {
                if (tab.ViewMode == Models.ViewMode.Home)
                    tab.Header = _loc.Get("Home");
                else if (tab.ViewMode == Models.ViewMode.Settings)
                    tab.Header = _loc.Get("Settings");
                else if (tab.ViewMode == Models.ViewMode.ActionLog)
                    tab.Header = _loc.Get("Log_Title");
            }

            // --- Sidebar favorites: localize known folder names ---
            ViewModel.LocalizeFavoriteNames();
        }

        #endregion

        #region Pane Preview Toggle

        private void OnPanePreviewToggle(object sender, RoutedEventArgs e)
        {
            // Tag에서 대상 패인을 결정 — ActivePane은 변경하지 않음 (포커스 사이드이펙트 방지)
            var targetPane = ActivePane.Left;
            if (sender is FrameworkElement fe && fe.Tag is string tag)
                targetPane = tag == "Right" ? ActivePane.Right : ActivePane.Left;

            TogglePreviewForPane(targetPane);
            UpdatePreviewButtonState();
        }

        #endregion

        // Breadcrumb scroll/overflow and breadcrumb click/chevron logic
        // are now handled internally by AddressBarControl.
        // Events are dispatched via OnAddressBarBreadcrumbClicked / OnAddressBarChevronClicked
        // in MainWindow.NavigationManager.cs.

        // ──── Legacy handlers removed ────
        // OnBreadcrumbScrollerSizeChanged, OnBreadcrumbContentSizeChanged,
        // OnBreadcrumbScrollerViewChanged, UpdateBreadcrumbOverflow,
        // OnPaneBreadcrumbClick, OnBreadcrumbChevronClick
        // are all now internal to AddressBarControl.

        #region Split View Toggle

        // --- Split View Toggle ---

        /// <summary>
        /// 분할 뷰 토글 버튼 클릭 이벤트.
        /// </summary>
        private void OnSplitViewToggleClick(object sender, RoutedEventArgs e)
        {
            ToggleSplitView();
            UpdateSplitViewButtonState();
        }

        /// <summary>
        /// RightExplorer PropertyChanged 구독 — RightAddressBar 동기화용
        /// </summary>
        private PropertyChangedEventHandler? _rightExplorerAddressBarHandler;

        private void ToggleSplitView()
        {
            if (ViewModel.IsRecycleBinTab) return;
            ViewModel.IsSplitViewEnabled = !ViewModel.IsSplitViewEnabled;

            if (ViewModel.IsSplitViewEnabled)
            {
                SplitterCol.Width = new GridLength(0);
                RightPaneCol.Width = new GridLength(1, GridUnitType.Star);

                // Stage S-2: per-pane LeftAddressBar removed; MainAddressBar (single toolbar)
                // already binds to ActiveExplorer so the breadcrumb stays in sync automatically.

                // Initialize right pane based on Tab2 startup settings.
                // behavior=0 default was Home; now Desktop (mirroring Tab1). All three
                // branches use NavigateTo(FolderItem) + EnableAutoNavigation suppressed
                // (sidebar-click pattern) so the right pane lands on the target folder
                // as COLUMN 1 instead of expanding the full ancestor chain.
                if (ViewModel.RightExplorer.Columns.Count == 0 ||
                    ViewModel.RightExplorer.CurrentPath == "PC")
                {
                    var tab2Behavior = _settings.Tab2StartupBehavior;
                    string? targetPath = null;

                    if (tab2Behavior == 2 && !string.IsNullOrEmpty(_settings.Tab2StartupPath)
                        && System.IO.Directory.Exists(_settings.Tab2StartupPath))
                    {
                        targetPath = _settings.Tab2StartupPath;
                        Helpers.DebugLogger.Log($"[ToggleSplitView] Right pane → custom path: {targetPath}");
                    }
                    else if (tab2Behavior == 1)
                    {
                        NavigateRightPaneToRealPath();
                        Helpers.DebugLogger.Log("[ToggleSplitView] Right pane → restore session");
                    }
                    else
                    {
                        // 0 (default) — Desktop.
                        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        if (!string.IsNullOrEmpty(desktop) && System.IO.Directory.Exists(desktop))
                        {
                            targetPath = desktop;
                            Helpers.DebugLogger.Log($"[ToggleSplitView] Right pane → Desktop: {targetPath}");
                        }
                        else
                        {
                            NavigateRightPaneToRealPath();
                            Helpers.DebugLogger.Log("[ToggleSplitView] Right pane → fallback (Desktop unavailable)");
                        }
                    }

                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        var leaf = System.IO.Path.GetFileName(targetPath);
                        if (string.IsNullOrEmpty(leaf)) leaf = targetPath;
                        var folder = new Models.FolderItem { Name = leaf, Path = targetPath };
                        bool prevAutoNav = ViewModel.RightExplorer.EnableAutoNavigation;
                        ViewModel.RightExplorer.EnableAutoNavigation = false;
                        _ = ViewModel.RightExplorer.NavigateTo(folder)
                            .ContinueWith(_ => ViewModel.RightExplorer.EnableAutoNavigation = prevAutoNav,
                                System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
                    }
                }

                // RightExplorer 네비게이션 시 RightAddressBar 자동 동기화
                SyncRightAddressBar();
                SubscribeRightExplorerForAddressBar();

                // Close ALL previews when entering split view (saves screen space)
                // 1) 사이드 패널 미리보기 비활성화
                ViewModel.IsLeftPreviewEnabled = false;
                LeftPreviewSplitterCol.Width = new GridLength(0);
                LeftPreviewCol.Width = new GridLength(0);
                LeftPreviewPanel.StopMedia();

                ViewModel.IsRightPreviewEnabled = false;
                RightPreviewSplitterCol.Width = new GridLength(0);
                RightPreviewCol.Width = new GridLength(0);
                RightPreviewPanel.StopMedia();

                // 2) 버튼 상태 동기화
                UpdatePreviewButtonState();

                // Set active pane to right and focus it after UI has updated
                ViewModel.ActivePane = ActivePane.Right;
                FocusActivePane();

                Helpers.DebugLogger.Log("[MainWindow] Split View enabled");
            }
            else
            {
                SplitterCol.Width = new GridLength(0);
                RightPaneCol.Width = new GridLength(0);

                // Right preview panel 정리 — 분할뷰 해제 시 우측 미리보기가 남는 버그 방지
                ViewModel.IsRightPreviewEnabled = false;
                RightPreviewSplitterCol.Width = new GridLength(0);
                RightPreviewCol.Width = new GridLength(0);
                RightPreviewPanel.StopMedia();

                // Sync main address bar — Split 모드에서 갱신 안 된 경우 보정
                if (ViewModel.Explorer?.PathSegments != null)
                {
                    MainAddressBar.PathSegments = ViewModel.Explorer.PathSegments;
                    MainAddressBar.CurrentPath = ViewModel.Explorer.CurrentPath;
                }

                // RightExplorer 구독 해제
                UnsubscribeRightExplorerForAddressBar();

                // 미리보기 상태 복원: 분할뷰 진입 시 비활성화했으므로 기본 설정값으로 복원
                try
                {
                    var settingsSvc = App.Current.Services.GetRequiredService<SettingsService>();
                    var previewDefault = settingsSvc.DefaultPreviewEnabled;

                    // 모든 뷰 모드 공통: 사이드 미리보기 패널 복원
                    ViewModel.IsLeftPreviewEnabled = previewDefault;
                    if (previewDefault)
                    {
                        LeftPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                        LeftPreviewCol.Width = new GridLength(GetSavedPreviewWidth("LeftPreviewWidth"), GridUnitType.Pixel);
                        SubscribePreviewToLastColumn(isLeft: true);
                    }
                    UpdatePreviewButtonState();
                    Helpers.DebugLogger.Log($"[MainWindow] Preview restored to default={previewDefault} after split view disabled");
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[MainWindow] Preview restore error: {ex.Message}");
                }

                // Reset active pane to left and focus it
                ViewModel.ActivePane = ActivePane.Left;
                FocusActivePane();

                Helpers.DebugLogger.Log("[MainWindow] Split View disabled");
            }
        }

        private void SyncRightAddressBar()
        {
            // Stage S-2: per-pane RightAddressBar removed. The single MainAddressBar
            // binds to ActiveExplorer; ActivePane=Right means RightExplorer's path
            // automatically appears there with no manual sync needed.
        }

        private void SubscribeRightExplorerForAddressBar()
        {
            UnsubscribeRightExplorerForAddressBar();
            if (ViewModel.RightExplorer == null) return;

            _rightExplorerAddressBarHandler = (s, e) =>
            {
                if (e.PropertyName == nameof(ExplorerViewModel.CurrentPath) ||
                    e.PropertyName == nameof(ExplorerViewModel.PathSegments))
                {
                    DispatcherQueue.TryEnqueue(() => SyncRightAddressBar());
                }
            };
            ViewModel.RightExplorer.PropertyChanged += _rightExplorerAddressBarHandler;
        }

        private void UnsubscribeRightExplorerForAddressBar()
        {
            if (_rightExplorerAddressBarHandler != null && ViewModel.RightExplorer != null)
            {
                ViewModel.RightExplorer.PropertyChanged -= _rightExplorerAddressBarHandler;
                _rightExplorerAddressBarHandler = null;
            }
        }

        /// <summary>
        /// Navigate the right pane to a real filesystem path (saved path, first drive, or user profile).
        /// </summary>
        private void NavigateRightPaneToRealPath()
        {
            var path = ViewModel.GetRightPaneInitialPath();
            var name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(name))
                name = path; // Drive root like "C:\"

            _ = ViewModel.RightExplorer.NavigateTo(new FolderItem { Name = name, Path = path });
            Helpers.DebugLogger.Log($"[MainWindow] Right pane navigated to: {path}");
        }

        #endregion

        #region Pane Navigation / Copy Path

        /// <summary>
        /// Per-pane navigate up button click.
        /// </summary>
        private void OnPaneNavigateUpClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var explorer = (btn.Tag as string) == "Right"
                    ? ViewModel.RightExplorer : ViewModel.LeftExplorer;
                explorer.NavigateUp();
            }
        }

        /// <summary>
        /// Per-pane copy path button click.
        /// </summary>
        private void OnPaneCopyPathClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var explorer = (btn.Tag as string) == "Right"
                    ? ViewModel.RightExplorer : ViewModel.LeftExplorer;
                var path = explorer.CurrentPath;
                if (!string.IsNullOrEmpty(path))
                {
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(path);
                    Clipboard.SetContent(dataPackage);
                    ViewModel.ShowToast(_loc.Get("Toast_PathCopied"), 2000);
                }
            }
        }

        #endregion

        #region Focus Active Pane

        /// <summary>
        /// Focus the active pane's content (used after pane switch or split toggle).
        /// Handles all view modes and retries if columns haven't loaded yet.
        /// </summary>
        private void FocusActivePane(int retryCount = 0)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (_isClosed || ViewModel == null) return;

                var viewMode = (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                    ? ViewModel.RightViewMode : ViewModel.CurrentViewMode;

                switch (viewMode)
                {
                    case Models.ViewMode.MillerColumns:
                        var columns = ViewModel.ActiveExplorer.Columns;
                        if (columns.Count > 0)
                        {
                            // autoSelect: false — 패인 전환 시 자동 선택 억제.
                            // 패인 클릭으로 포커스만 이동하고, 첫 항목을 자동 선택하지 않음.
                            // 이를 통해 좌/우클릭 번갈아 시 컬럼이 연쇄 생성되는 버그 방지.
                            FocusColumnAsync(columns.Count - 1, autoSelect: false);
                        }
                        else if (retryCount < 3)
                        {
                            // Columns may still be loading after NavigateRightPaneToRealPath
                            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                                () => FocusActivePane(retryCount + 1));
                        }
                        break;

                    case Models.ViewMode.Details:
                        GetActiveDetailsView()?.FocusListView();
                        break;

                    case Models.ViewMode.List:
                        GetActiveListView()?.FocusGridView();
                        break;

                    case Models.ViewMode.IconSmall:
                    case Models.ViewMode.IconMedium:
                    case Models.ViewMode.IconLarge:
                    case Models.ViewMode.IconExtraLarge:
                        GetActiveIconView()?.FocusGridView();
                        break;
                }
            });
        }

        #endregion

        // =================================================================
        //  Preview Panel
        // =================================================================

        #region Preview Panel

        /// <summary>
        /// x:Bind visibility helper for preview panel.
        /// </summary>
        public Visibility PreviewVisible(bool isPreviewEnabled)
            => isPreviewEnabled ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Initialize preview panels with ViewModels from DI.
        /// </summary>
        private void InitializePreviewPanels()
        {
            var previewService = App.Current.Services.GetRequiredService<PreviewService>();

            var leftVm = new PreviewPanelViewModel(previewService);
            LeftPreviewPanel.Initialize(leftVm);

            var rightVm = new PreviewPanelViewModel(previewService);
            RightPreviewPanel.Initialize(rightVm);

            // Defensive unsubscribe before subscribe to prevent handler accumulation
            ViewModel.LeftExplorer.Columns.CollectionChanged -= OnLeftColumnsChangedForPreview;
            ViewModel.RightExplorer.Columns.CollectionChanged -= OnRightColumnsChangedForPreview;
            ViewModel.PropertyChanged -= OnViewModelPropertyChangedForPreview;

            // Subscribe to LeftExplorer column changes for preview updates
            ViewModel.LeftExplorer.Columns.CollectionChanged += OnLeftColumnsChangedForPreview;
            ViewModel.RightExplorer.Columns.CollectionChanged += OnRightColumnsChangedForPreview;

            // Subscribe to ViewModel property changes for preview state
            ViewModel.PropertyChanged += OnViewModelPropertyChangedForPreview;

            // Initialize Git status bars
            InitializeGitStatusBars();

            // Stage S-3.2: seed pane indicator brushes for the initial state
            // (left active, right inactive). XAML literal colors already match
            // this case, but call explicitly so future XAML edits don't drift.
            RefreshActivePaneIndicators();
        }

        /// <summary>
        /// When columns change, subscribe to the last column's SelectedChild for preview.
        /// </summary>
        private void OnLeftColumnsChangedForPreview(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isClosed) return;
            if (!ViewModel.IsLeftPreviewEnabled) return;
            SubscribePreviewToLastColumn(isLeft: true);
        }

        private void OnRightColumnsChangedForPreview(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isClosed) return;
            bool needSubscribe = ViewModel.IsRightPreviewEnabled;
            if (!needSubscribe) return;
            SubscribePreviewToLastColumn(isLeft: false);
        }

        /// <summary>
        /// Subscribe to the last column's SelectedChild property changes to auto-update preview.
        /// </summary>
        private void SubscribePreviewToLastColumn(bool isLeft)
        {
            var explorer = isLeft ? ViewModel.LeftExplorer : ViewModel.RightExplorer;
            var columns = explorer.Columns;

            UnsubscribePreviewSelection(isLeft);

            if (columns.Count == 0) return;

            var lastColumn = columns[columns.Count - 1];
            lastColumn.PropertyChanged += isLeft ? OnLeftColumnSelectionForPreview : OnRightColumnSelectionForPreview;

            if (isLeft) _leftPreviewSubscribedColumn = lastColumn;
            else _rightPreviewSubscribedColumn = lastColumn;

            // Immediately update preview with current selection.
            var selectedChild = lastColumn.SelectedChild;
            var previewPanel = isLeft ? LeftPreviewPanel : RightPreviewPanel;
            var targetViewMode = isLeft ? ViewModel.CurrentViewMode : ViewModel.RightViewMode;

            if (targetViewMode == Models.ViewMode.MillerColumns)
            {
                // Miller: 파일 선택 시만 패널 표시, 그 외(폴더/미선택) → 패널 자체 숨김
                bool showPanel = selectedChild is FileViewModel;
                SetMillerPreviewPanelVisible(isLeft, showPanel);
                previewPanel.UpdatePreview(showPanel ? FilterPreviewItem(selectedChild) : null);
            }
            else
            {
                // Details/List/Icon: 기존 동작
                var previewItem = selectedChild != null
                    ? FilterPreviewItem(selectedChild)
                    : FilterPreviewItem(lastColumn);
                previewPanel.UpdatePreview(previewItem);
            }
        }

        /// <summary>
        /// 미리보기 대상 항목 필터: PreviewShowFolderInfo 설정에 따라 폴더 항목을 null로 변환.
        /// </summary>
        private FileSystemViewModel? FilterPreviewItem(FileSystemViewModel? item)
        {
            if (item is FolderViewModel)
            {
                try
                {
                    var settings = App.Current.Services.GetRequiredService<SettingsService>();
                    if (!settings.PreviewShowFolderInfo) return null;
                }
                catch { return null; }
            }
            return item;
        }

        /// <summary>
        /// Miller 모드 전용: 파일 선택 여부에 따라 사이드 미리보기 패널 Width를 표시/숨김.
        /// IsLeftPreviewEnabled 상태는 변경하지 않음 (토글 버튼 유지).
        /// </summary>
        private void SetMillerPreviewPanelVisible(bool isLeft, bool visible)
        {
            if (isLeft)
            {
                if (visible)
                {
                    if (LeftPreviewCol.Width.Value < 1)
                    {
                        LeftPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                        LeftPreviewCol.Width = new GridLength(GetSavedPreviewWidth("LeftPreviewWidth"), GridUnitType.Pixel);
                    }
                }
                else
                {
                    LeftPreviewSplitterCol.Width = new GridLength(0);
                    LeftPreviewCol.Width = new GridLength(0);
                    LeftPreviewPanel.StopMedia();
                }
            }
            else
            {
                if (visible)
                {
                    if (RightPreviewCol.Width.Value < 1)
                    {
                        RightPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                        RightPreviewCol.Width = new GridLength(GetSavedPreviewWidth("RightPreviewWidth"), GridUnitType.Pixel);
                    }
                }
                else
                {
                    RightPreviewSplitterCol.Width = new GridLength(0);
                    RightPreviewCol.Width = new GridLength(0);
                    RightPreviewPanel.StopMedia();
                }
            }
        }

        private void UnsubscribePreviewSelection(bool isLeft)
        {
            if (isLeft && _leftPreviewSubscribedColumn != null)
            {
                _leftPreviewSubscribedColumn.PropertyChanged -= OnLeftColumnSelectionForPreview;
                _leftPreviewSubscribedColumn = null;
            }
            else if (!isLeft && _rightPreviewSubscribedColumn != null)
            {
                _rightPreviewSubscribedColumn.PropertyChanged -= OnRightColumnSelectionForPreview;
                _rightPreviewSubscribedColumn = null;
            }
        }

        private void OnLeftColumnSelectionForPreview(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FolderViewModel.SelectedChild)) return;
            if (_isClosed) return;

            if (sender is FolderViewModel folder)
            {
                if (ViewModel.IsLeftPreviewEnabled)
                {
                    var child = folder.SelectedChild;
                    bool isMiller = ViewModel.CurrentViewMode == Models.ViewMode.MillerColumns;

                    if (isMiller)
                    {
                        // Miller: 파일 선택 시만 패널 표시, 폴더/미선택 → 패널 숨김
                        bool showPanel = child is FileViewModel;
                        SetMillerPreviewPanelVisible(isLeft: true, showPanel);
                        LeftPreviewPanel.UpdatePreview(showPanel ? FilterPreviewItem(child) : null);
                    }
                    else
                    {
                        var item = child != null ? FilterPreviewItem(child) : FilterPreviewItem(folder);
                        LeftPreviewPanel.UpdatePreview(item);
                    }
                }

                // Quick Look 윈도우가 열려 있으면 내용 업데이트
                if (ViewModel.ActivePane == ActivePane.Left)
                    UpdateQuickLookContent(folder.SelectedChild);
            }
        }

        private void OnRightColumnSelectionForPreview(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FolderViewModel.SelectedChild)) return;
            if (_isClosed) return;

            if (sender is FolderViewModel folder)
            {
                if (ViewModel.IsRightPreviewEnabled)
                {
                    var child = folder.SelectedChild;
                    bool isMiller = ViewModel.RightViewMode == Models.ViewMode.MillerColumns;

                    if (isMiller)
                    {
                        bool showPanel = child is FileViewModel;
                        SetMillerPreviewPanelVisible(isLeft: false, showPanel);
                        RightPreviewPanel.UpdatePreview(showPanel ? FilterPreviewItem(child) : null);
                    }
                    else
                    {
                        var item = child != null ? FilterPreviewItem(child) : FilterPreviewItem(folder);
                        RightPreviewPanel.UpdatePreview(item);
                    }
                }

                // Quick Look 윈도우가 열려 있으면 내용 업데이트
                if (ViewModel.ActivePane == ActivePane.Right)
                    UpdateQuickLookContent(folder.SelectedChild);
            }
        }

        /// <summary>
        /// Update preview when selection changes in Details/Icon mode (via Miller column selection handler).
        /// </summary>
        private void UpdatePreviewForSelection(FileSystemViewModel? selectedItem)
        {
            if (_isClosed) return;

            var filtered = FilterPreviewItem(selectedItem);
            if (ViewModel.ActivePane == ActivePane.Left && ViewModel.IsLeftPreviewEnabled)
                LeftPreviewPanel.UpdatePreview(filtered);
            else if (ViewModel.ActivePane == ActivePane.Right && ViewModel.IsRightPreviewEnabled)
                RightPreviewPanel.UpdatePreview(filtered);

            // Quick Look 윈도우가 열려 있으면 내용 업데이트
            UpdateQuickLookContent(selectedItem);
        }

        /// <summary>
        /// React to preview enable/disable changes to wire/unwire subscriptions.
        /// </summary>
        private void OnViewModelPropertyChangedForPreview(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsLeftPreviewEnabled))
            {
                if (ViewModel.IsLeftPreviewEnabled)
                    SubscribePreviewToLastColumn(isLeft: true);
                else
                {
                    UnsubscribePreviewSelection(isLeft: true);
                    LeftPreviewPanel.UpdatePreview(null);
                }
                // Single toolbar's preview-toggle highlight follows the active pane's
                // preview state — refresh when the active pane's flag flips.
                if (!ViewModel.IsSplitViewEnabled || ViewModel.ActivePane == ActivePane.Left)
                    UpdatePreviewButtonState();
            }
            else if (e.PropertyName == nameof(MainViewModel.IsRightPreviewEnabled))
            {
                if (ViewModel.IsRightPreviewEnabled)
                    SubscribePreviewToLastColumn(isLeft: false);
                else
                {
                    UnsubscribePreviewSelection(isLeft: false);
                    RightPreviewPanel.UpdatePreview(null);
                }
                if (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                    UpdatePreviewButtonState();
            }
            else if (e.PropertyName == nameof(MainViewModel.ActivePane))
            {
                // Stage S-2 sync — single LumiToolbar's view-mode segmented bar and
                // preview-toggle color must follow the focused pane. Without this the
                // toolbar stays frozen on whichever pane was active when the toolbar
                // was last refreshed (typically the left pane).
                UpdateViewModeIcon();
                UpdatePreviewButtonState();
                ViewModel.UpdateStatusBar();
                ViewModel.SyncNavigationHistoryState();
                // Stage S-3.2: pane-scoped indicator brushes (col-header name +
                // ListView selection) re-paint together so the active pane reads
                // amber, the inactive pane reads warm gray.
                RefreshActivePaneIndicators();
            }
            else if (e.PropertyName == nameof(MainViewModel.IsSplitViewEnabled))
            {
                // Single-pane mode forces the left pane's brushes back to amber
                // (no second pane to differentiate against) — and re-coloring on
                // entering split view is needed because RightPaneContainer's
                // brushes were initialized as inactive at XAML load time.
                RefreshActivePaneIndicators();
            }
            else if (e.PropertyName == nameof(MainViewModel.LeftViewMode)
                  || e.PropertyName == nameof(MainViewModel.RightViewMode))
            {
                // Pane-specific view mode changed (e.g. right pane switched from
                // Miller to Details). Toolbar segment must reflect new mode whenever
                // it belongs to the currently active pane.
                UpdateViewModeIcon();
                ViewModel.UpdateStatusBar();
            }
        }

        private void OnPreviewToggleClick(object sender, RoutedEventArgs e)
        {
            TogglePreviewPanel();
            UpdatePreviewButtonState();
        }

        private void TogglePreviewPanel()
        {
            TogglePreviewForPane(ViewModel.ActivePane);
        }

        /// <summary>
        /// 지정된 패인의 미리보기를 토글.
        /// ActivePane을 건드리지 않고 대상 프로퍼티를 직접 토글하여 경합 제거.
        /// </summary>
        private void TogglePreviewForPane(ActivePane targetPane)
        {
            if (ViewModel.IsRecycleBinTab) return;
            // 모든 뷰 모드 공통: 사이드 미리보기 패널 토글
            if (targetPane == ActivePane.Left)
            {
                ViewModel.IsLeftPreviewEnabled = !ViewModel.IsLeftPreviewEnabled;
                if (ViewModel.IsLeftPreviewEnabled)
                {
                    LeftPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                    LeftPreviewCol.Width = new GridLength(GetSavedPreviewWidth("LeftPreviewWidth"), GridUnitType.Pixel);
                }
                else
                {
                    LeftPreviewSplitterCol.Width = new GridLength(0);
                    LeftPreviewCol.Width = new GridLength(0);
                    LeftPreviewPanel.StopMedia();
                }
            }
            else
            {
                ViewModel.IsRightPreviewEnabled = !ViewModel.IsRightPreviewEnabled;
                if (ViewModel.IsRightPreviewEnabled)
                {
                    RightPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                    RightPreviewCol.Width = new GridLength(GetSavedPreviewWidth("RightPreviewWidth"), GridUnitType.Pixel);
                }
                else
                {
                    RightPreviewSplitterCol.Width = new GridLength(0);
                    RightPreviewCol.Width = new GridLength(0);
                    RightPreviewPanel.StopMedia();
                }
            }
            ViewModel.SavePreviewState();

            Helpers.DebugLogger.Log($"[MainWindow] Preview toggled (pane={targetPane}): Left={ViewModel.IsLeftPreviewEnabled}, Right={ViewModel.IsRightPreviewEnabled}");

            // After preview toggle, the Miller columns viewport width changes.
            // Scroll to keep the last column visible.
            var explorer = ViewModel.ActiveExplorer;
            if (explorer != null && explorer.Columns.Count > 0)
            {
                var scrollViewer = GetActiveMillerScrollViewer();
                ScrollToLastColumn(explorer, scrollViewer);
            }
        }

        /// <summary>
        /// 미리보기 토글 버튼의 활성 상태를 시각적으로 업데이트.
        /// Miller Columns 모드: 인라인 미리보기 설정 기반
        /// Details/List/Icon 모드: 사이드 패널 활성화 상태 기반
        /// </summary>
        internal void UpdatePreviewButtonState()
        {
            try
            {
                // Active uses LumiAmberBrush (theme-agnostic signature color)
                // instead of SpanAccentBrush — Span's accent is amber in Dark
                // but #0078D4 (Win11 blue) in Light, which contradicts the
                // LumiFiles identity. Amber stays consistent across themes.
                var accentBrush = GetThemeBrush("LumiAmberBrush");
                var defaultBrush = GetThemeBrush("SpanTextSecondaryBrush");
                var pillDefaultBrush = GetThemeBrush("LumiTextPrimaryBrush");

                // Stage S-2: single LumiToolbar's PreviewToggleIcon covers both modes.
                // In split view, follow the active pane's preview-enabled flag so the
                // toolbar's amber-state matches whichever pane currently has focus.
                bool isActive = (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                    ? ViewModel.IsRightPreviewEnabled
                    : ViewModel.IsLeftPreviewEnabled;

                // Address-bar toolbar icon (visible only in single-pane mode)
                if (PreviewToggleIcon != null)
                {
                    if (isActive) PreviewToggleIcon.Foreground = accentBrush;
                    else PreviewToggleIcon.ClearValue(Microsoft.UI.Xaml.Controls.Control.ForegroundProperty);
                }

                // Pill-bar 버튼 (LumiToolbar). S-3.39: view 모드 버튼과 동일 패턴 —
                // active 시 amber pill 배경, 아이콘은 항상 primary text 색.
                var pillActiveBrush = GetThemeBrush("LumiPillActiveBrush");
                var transparentBrush = (Microsoft.UI.Xaml.Media.Brush)
                    new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                if (LumiPreviewButton != null)
                {
                    LumiPreviewButton.Background = isActive ? pillActiveBrush : transparentBrush;
                }
                if (LumiPreviewIcon != null)
                {
                    LumiPreviewIcon.Foreground = pillDefaultBrush;
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] UpdatePreviewButtonState error: {ex.Message}");
            }
        }

        /// <summary>
        /// 뷰모드 버튼 아이콘을 현재 활성 뷰모드에 맞게 업데이트.
        /// </summary>
        internal void UpdateViewModeIcon()
        {
            try
            {
                // Stage S-2: single LumiToolbar shared by both panes. In split view
                // the toolbar must mirror the ACTIVE pane's view mode (RightViewMode
                // when ActivePane=Right), otherwise focusing the right pane leaves
                // the segmented bar stuck on the left pane's mode.
                var mode = (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                    ? ViewModel.RightViewMode
                    : ViewModel.CurrentViewMode;
                string glyph = GetViewModeGlyph(mode);

                ViewModeIcon.Glyph = glyph;

                // LumiToolbar View segmented bar — sync active highlight with current mode.
                UpdateLumiViewModeButtons(mode);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] UpdateViewModeIcon error: {ex.Message}");
            }
        }

        /// <summary>
        /// LumiToolbar의 View 그룹 (Miller / Details / Icons) 중 현재 모드에 해당하는 버튼만
        /// LumiPillActiveBrush + amber 텍스트로 highlight, 나머지는 inactive 톤으로 복원.
        /// </summary>
        private void UpdateLumiViewModeButtons(Models.ViewMode mode)
        {
            // GetThemeBrush (instance) honors window.ActualTheme so it returns
            // the correct Light/Dark variant when user-selected theme differs
            // from Application.RequestedTheme. The static ThemeBrush helper
            // below uses Application.Current.Resources which would return the
            // wrong-theme brush in that mismatch (light user pick over dark
            // system → buttons rendered with dark-theme primary text = white
            // on the light pill bar, invisible).
            var active = GetThemeBrush("LumiPillActiveBrush");
            // Inactive buttons are transparent so the outer container's PillBrush fill
            // shows through; only the active mode renders its own amber stadium pill.
            var inactive = (Microsoft.UI.Xaml.Media.Brush)new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            // S-3.39: active foreground도 LumiTextPrimaryBrush (라이트=거의검정, 다크=거의흰색)로
            // 유지. 이전엔 amber bg 위에 amber 아이콘(LumiAmberSoftBrush)이라 라이트 테마에서
            // 아이콘이 배경에 묻혀 안 보임. amber pill 배경만으로 active 상태 충분히 식별 가능.
            var amber = GetThemeBrush("LumiTextPrimaryBrush");
            var primary = GetThemeBrush("LumiTextPrimaryBrush");

            bool isMiller = mode == Models.ViewMode.MillerColumns;
            bool isDetails = mode == Models.ViewMode.Details;
            bool isIcons = mode == Models.ViewMode.IconSmall
                        || mode == Models.ViewMode.IconMedium
                        || mode == Models.ViewMode.IconLarge
                        || mode == Models.ViewMode.IconExtraLarge;

            ApplyViewModeButtonState(LumiViewMillerButton, isMiller, active, inactive, amber, primary);
            ApplyViewModeButtonState(LumiViewDetailsButton, isDetails, active, inactive, amber, primary);
            ApplyViewModeButtonState(LumiViewIconsButton, isIcons, active, inactive, amber, primary);
        }

        /// <summary>
        /// Apply active/inactive visuals to a view-mode toggle button. When active, also
        /// override PointerOver/Pressed theme brushes via Button.Resources so hover does
        /// NOT fall back to WinUI's default gray (the user-reported regression where the
        /// amber active state turned gray on mouse-over). When inactive, those overrides
        /// are removed so the button gets the standard subtle gray hover affordance.
        /// </summary>
        private static void ApplyViewModeButtonState(
            Microsoft.UI.Xaml.Controls.Button? btn, bool isActive,
            Microsoft.UI.Xaml.Media.Brush? activeBg, Microsoft.UI.Xaml.Media.Brush? inactiveBg,
            Microsoft.UI.Xaml.Media.Brush? amberFg, Microsoft.UI.Xaml.Media.Brush? primaryFg)
        {
            if (btn == null) return;
            // S-3.39 (revised): ClearValue를 쓰면 WinUI Button 기본 ButtonBackground
            // ThemeResource(라이트=옅은 회색 솔리드)로 폴백돼 view 버튼만 다른 회색 fill을
            // 가지게 됨. 따라서 inactive 시에도 명시적으로 inactiveBg(Transparent) +
            // primaryFg를 직접 할당해 다른 툴바 버튼들과 동일한 외관 유지.
            if (isActive)
            {
                btn.Background = activeBg;
                btn.Foreground = amberFg;
            }
            else
            {
                btn.Background = inactiveBg;
                btn.Foreground = primaryFg;
            }

            const string bgPointerOverKey = "ButtonBackgroundPointerOver";
            const string bgPressedKey     = "ButtonBackgroundPressed";
            const string fgPointerOverKey = "ButtonForegroundPointerOver";
            const string fgPressedKey     = "ButtonForegroundPressed";

            if (isActive && activeBg != null && amberFg != null)
            {
                btn.Resources[bgPointerOverKey] = activeBg;
                btn.Resources[bgPressedKey]     = activeBg;
                btn.Resources[fgPointerOverKey] = amberFg;
                btn.Resources[fgPressedKey]     = amberFg;
            }
            else
            {
                btn.Resources.Remove(bgPointerOverKey);
                btn.Resources.Remove(bgPressedKey);
                btn.Resources.Remove(fgPointerOverKey);
                btn.Resources.Remove(fgPressedKey);
            }
        }

        private static Microsoft.UI.Xaml.Media.Brush? ThemeBrush(string key)
        {
            if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(key, out var v) && v is Microsoft.UI.Xaml.Media.Brush b)
                return b;
            return null;
        }

        private static string GetViewModeGlyph(Models.ViewMode mode) => mode switch
        {
            Models.ViewMode.MillerColumns => "\uF0E2",
            Models.ViewMode.Details => "\uE8EF",
            Models.ViewMode.List => "\uE80A",
            _ when mode >= Models.ViewMode.IconSmall && mode <= Models.ViewMode.IconExtraLarge => "\uE91B",
            _ => "\uF0E2"
        };

        internal void UpdateSplitViewButtonState()
        {
            try
            {
                // LumiAmberBrush (theme-agnostic) — see UpdatePreviewButtonState comment.
                var accentBrush = GetThemeBrush("LumiAmberBrush");
                var defaultBrush = GetThemeBrush("SpanTextSecondaryBrush");
                var pillDefaultBrush = GetThemeBrush("LumiTextPrimaryBrush");
                bool isActive = ViewModel.IsSplitViewEnabled;

                // Address-bar toolbar icon (visible only when not in special modes)
                if (SplitViewIcon != null)
                {
                    if (isActive) SplitViewIcon.Foreground = accentBrush;
                    else SplitViewIcon.ClearValue(Microsoft.UI.Xaml.Controls.Control.ForegroundProperty);
                }

                // Pill-bar 버튼 (LumiToolbar). S-3.39: view 모드 버튼과 동일 패턴 —
                // active 시 amber pill 배경, 아이콘은 항상 primary text 색.
                var pillActiveBrush = GetThemeBrush("LumiPillActiveBrush");
                var transparentBrush = (Microsoft.UI.Xaml.Media.Brush)
                    new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                if (LumiSplitButton != null)
                {
                    LumiSplitButton.Background = isActive ? pillActiveBrush : transparentBrush;
                }
                if (LumiSplitIcon != null)
                {
                    LumiSplitIcon.Foreground = pillDefaultBrush;
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] UpdateSplitViewButtonState error: {ex.Message}");
            }
        }

        /// <summary>
        /// 우측 패인의 뷰모드 변경 시 프리뷰 패널 너비를 동기화.
        /// Miller → Details/List/Icon 전환 시 프리뷰 활성화 상태에 맞게 너비 조정.
        /// </summary>
        internal void SyncRightPreviewPanelWidth()
        {
            try
            {
                if (!ViewModel.IsSplitViewEnabled) return;

                if (ViewModel.IsRightPreviewEnabled)
                {
                    RightPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                    RightPreviewCol.Width = new GridLength(GetSavedPreviewWidth("RightPreviewWidth"), GridUnitType.Pixel);
                }
                else
                {
                    RightPreviewSplitterCol.Width = new GridLength(0);
                    RightPreviewCol.Width = new GridLength(0);
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] SyncRightPreviewPanelWidth error: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore preview panel widths from saved settings on Loaded.
        /// </summary>
        private void RestorePreviewState()
        {
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;

                if (ViewModel.IsLeftPreviewEnabled)
                {
                    LeftPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                    double leftW = 320;
                    if (settings.Values.TryGetValue("LeftPreviewWidth", out var lw))
                        leftW = Math.Max(320, (double)lw);
                    LeftPreviewCol.Width = new GridLength(leftW, GridUnitType.Pixel);
                    SubscribePreviewToLastColumn(isLeft: true);
                }

                if (ViewModel.IsRightPreviewEnabled)
                {
                    RightPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                    double rightW = 320;
                    if (settings.Values.TryGetValue("RightPreviewWidth", out var rw))
                        rightW = Math.Max(320, (double)rw);
                    RightPreviewCol.Width = new GridLength(rightW, GridUnitType.Pixel);
                    SubscribePreviewToLastColumn(isLeft: false);
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] RestorePreviewState error: {ex.Message}");
            }
        }

        /// <summary>
        /// LocalSettings에 저장된 미리보기 패널 너비를 읽는다. 미저장 시 기본 320px.
        /// </summary>
        private static double GetSavedPreviewWidth(string key)
        {
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (settings.Values.TryGetValue(key, out var val))
                    return Math.Max(320, (double)val);
            }
            catch { }
            return 320;
        }

        #endregion

        // =================================================================
        //  Git Status Bar (bottom of explorer)
        // =================================================================

        #region Git Status Bar

        private PropertyChangedEventHandler? _leftExplorerGitHandler;
        private PropertyChangedEventHandler? _rightExplorerGitHandler;

        /// <summary>
        /// Git 상태바 초기화: ViewModel 생성, 이벤트 구독, SizeChanged 연결.
        /// </summary>
        private void InitializeGitStatusBars()
        {
            _leftGitStatusBarVm = new GitStatusBarViewModel();
            _rightGitStatusBarVm = new GitStatusBarViewModel();

            // PropertyChanged로 UI 바인딩
            _leftGitStatusBarVm.PropertyChanged += OnLeftGitStatusBarChanged;
            _rightGitStatusBarVm.PropertyChanged += OnRightGitStatusBarChanged;

            // Explorer CurrentPath 변경 구독
            SubscribeGitStatusToExplorer(isLeft: true);
            SubscribeGitStatusToExplorer(isLeft: false);

            // SizeChanged로 반응형 텍스트 갱신
            LeftGitStatusBar.SizeChanged += (s, e) =>
                _leftGitStatusBarVm?.UpdateStatusText(e.NewSize.Width);
            RightGitStatusBar.SizeChanged += (s, e) =>
                _rightGitStatusBarVm?.UpdateStatusText(e.NewSize.Width);

            // 초기 경로로 갱신
            _ = _leftGitStatusBarVm.UpdateForPathAsync(ViewModel.LeftExplorer?.CurrentPath);
            _ = _rightGitStatusBarVm.UpdateForPathAsync(ViewModel.RightExplorer?.CurrentPath);
        }

        /// <summary>
        /// Explorer.CurrentPath 변경을 감시하여 Git 상태바 갱신.
        /// </summary>
        private void SubscribeGitStatusToExplorer(bool isLeft)
        {
            UnsubscribeGitStatusFromExplorer(isLeft);

            var explorer = isLeft ? ViewModel.LeftExplorer : ViewModel.RightExplorer;
            if (explorer == null) return;

            PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == nameof(ExplorerViewModel.CurrentPath))
                {
                    var vm = isLeft ? _leftGitStatusBarVm : _rightGitStatusBarVm;
                    var path = (s as ExplorerViewModel)?.CurrentPath;
                    if (vm != null)
                        _ = vm.UpdateForPathAsync(path);
                }
            };

            explorer.PropertyChanged += handler;
            if (isLeft) _leftExplorerGitHandler = handler;
            else _rightExplorerGitHandler = handler;
        }

        private void UnsubscribeGitStatusFromExplorer(bool isLeft)
        {
            var explorer = isLeft ? ViewModel.LeftExplorer : ViewModel.RightExplorer;
            if (isLeft && _leftExplorerGitHandler != null)
            {
                if (explorer != null) explorer.PropertyChanged -= _leftExplorerGitHandler;
                _leftExplorerGitHandler = null;
            }
            else if (!isLeft && _rightExplorerGitHandler != null)
            {
                if (explorer != null) explorer.PropertyChanged -= _rightExplorerGitHandler;
                _rightExplorerGitHandler = null;
            }
        }

        /// <summary>
        /// 탭 전환 시 Git 상태바 Explorer 구독을 재연결.
        /// </summary>
        internal void ResubscribeGitStatusBar(bool isLeft)
        {
            SubscribeGitStatusToExplorer(isLeft);
            var explorer = isLeft ? ViewModel.LeftExplorer : ViewModel.RightExplorer;
            var vm = isLeft ? _leftGitStatusBarVm : _rightGitStatusBarVm;
            if (vm == null) return;

            // 파일 탐색과 무관한 모드(Settings/ActionLog/Home)에서는 git 정보 조회 자체를 skip
            // (SetViewModeVisibility의 Clear()를 ResubscribeGitStatusBar가 덮어쓰는 레이스 방지)
            var mode = ViewModel.CurrentViewMode;
            if (mode == ViewMode.Settings || mode == ViewMode.ActionLog || mode == ViewMode.Home)
            {
                vm.Clear();
                return;
            }

            if (explorer != null)
                _ = vm.UpdateForPathAsync(explorer.CurrentPath);
        }

        /// <summary>
        /// Left Git 상태바 ViewModel → UI 동기화.
        /// </summary>
        private void OnLeftGitStatusBarChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isClosed) return;
            DispatcherQueue.TryEnqueue(() => SyncGitStatusBarUI(isLeft: true));
        }

        private void OnRightGitStatusBarChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isClosed) return;
            DispatcherQueue.TryEnqueue(() => SyncGitStatusBarUI(isLeft: false));
        }

        /// <summary>
        /// GitStatusBarViewModel 데이터를 XAML 요소에 반영.
        /// </summary>
        private void SyncGitStatusBarUI(bool isLeft)
        {
            var vm = isLeft ? _leftGitStatusBarVm : _rightGitStatusBarVm;
            if (vm == null) return;

            var bar = isLeft ? LeftGitStatusBar : RightGitStatusBar;
            var branchTb = isLeft ? LeftGitBranch : RightGitBranch;
            var statusTb = isLeft ? LeftGitStatus : RightGitStatus;
            var flyoutBranch = isLeft ? LeftFlyoutBranch : RightFlyoutBranch;
            var flyoutStatus = isLeft ? LeftFlyoutStatus : RightFlyoutStatus;
            var flyoutCommitsLabel = isLeft ? LeftFlyoutCommitsLabel : RightFlyoutCommitsLabel;
            var flyoutCommits = isLeft ? LeftFlyoutCommits : RightFlyoutCommits;
            var flyoutFilesLabel = isLeft ? LeftFlyoutFilesLabel : RightFlyoutFilesLabel;
            var flyoutFiles = isLeft ? LeftFlyoutFiles : RightFlyoutFiles;

            bar.Visibility = vm.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            branchTb.Text = vm.Branch;
            statusTb.Text = vm.StatusText;

            // Flyout content
            flyoutBranch.Text = vm.Branch;
            flyoutStatus.Text = vm.FullStatusText;
            flyoutCommitsLabel.Text = _loc?.Get("GitStatus_RecentCommits") ?? "Recent Commits";
            flyoutCommits.Text = vm.RecentCommits;
            flyoutFilesLabel.Text = _loc?.Get("GitStatus_ChangedFiles") ?? "Changed Files";
            flyoutFiles.Text = vm.ChangedFiles;
        }

        /// <summary>
        /// Git 상태바 리소스 해제.
        /// </summary>
        private void CleanupGitStatusBars()
        {
            UnsubscribeGitStatusFromExplorer(isLeft: true);
            UnsubscribeGitStatusFromExplorer(isLeft: false);

            if (_leftGitStatusBarVm != null)
            {
                _leftGitStatusBarVm.PropertyChanged -= OnLeftGitStatusBarChanged;
                _leftGitStatusBarVm.Dispose();
                _leftGitStatusBarVm = null;
            }
            if (_rightGitStatusBarVm != null)
            {
                _rightGitStatusBarVm.PropertyChanged -= OnRightGitStatusBarChanged;
                _rightGitStatusBarVm.Dispose();
                _rightGitStatusBarVm = null;
            }
        }

        #endregion
    }
}
