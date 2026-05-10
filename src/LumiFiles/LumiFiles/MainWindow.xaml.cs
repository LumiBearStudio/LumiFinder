using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using LumiFiles.Helpers;
using LumiFiles.Models;
using LumiFiles.ViewModels;
using LumiFiles.Services;
using LumiFiles.Services.FileOperations;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition.SystemBackdrops;
using Windows.ApplicationModel.DataTransfer;
using Windows.Services.Store;

namespace LumiFiles
{
    /// <summary>
    /// 애플리케이션의 기본 메인 윈도우.
    /// Miller Columns, Details, List, Icon 등 다양한 뷰 모드를 호스팅하며,
    /// 사이드바 탐색, 탭 관리, 분할 뷰, 미리보기 패널, 드래그 앤 드롭,
    /// 키보드 단축키, 파일 작업, 설정 적용 등 전체 UI 로직을 관리한다.
    /// partial class로 분할되어 각 기능 영역별 핸들러 파일에서 확장된다.
    /// </summary>
    /// <remarks>
    /// <para>P/Invoke를 통해 WM_DEVICECHANGE(USB 핫플러그) 감지, 윈도우 서브클래싱,
    /// DPI 인식 윈도우 배치 복원 등 Win32 네이티브 기능을 활용한다.</para>
    /// <para>탭별 독립 뷰 패널(Show/Hide 패턴)을 유지하여 즉시 탭 전환을 구현하며,
    /// 탭 떼어내기(tear-off)를 통한 멀티 윈도우를 지원한다.</para>
    /// <para><see cref="Services.IContextMenuHost"/>를 구현하여
    /// 컨텍스트 메뉴 서비스에서 파일 작업 명령을 실행할 수 있는 호스트 역할을 한다.</para>
    /// </remarks>
    public sealed partial class MainWindow : Window, Services.IContextMenuHost
    {
        // --- WM_DEVICECHANGE P/Invoke for USB hotplug detection ---
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVNODES_CHANGED = 0x0007;

        // --- WM_GETMINMAXINFO: borderless 윈도우 최대화 시 작업표시줄 영역 침범 방지 ---
        private const int WM_GETMINMAXINFO = 0x0024;

        // --- WM_DPICHANGED: 듀얼 모니터에서 다른 DPI 모니터로 이동 시 발생 (S-3.34 재시도) ---
        // wParam LOWORD = 새 X축 DPI, lParam = RECT* (Windows 권장 위치/크기).
        // 안 처리하면: 새 DPI scale로 region이 재계산되지 않아 stale 상태가 됨 → 자글거림 심해짐.
        private const int WM_DPICHANGED = 0x02E0;

        // --- WM_NCCALCSIZE: WS_THICKFRAME 의 시각적 비클라이언트 보더 제거 (S-3.40)
        // wParam=TRUE 시 lParam = NCCALCSIZE_PARAMS*. rgrc[0] 를 그대로 두면 client area =
        // window area → NC area 가 0 이라 보더 안 그려짐. WS_THICKFRAME 으로 Snap Layouts
        // 활성화하면서 시각적 보더는 안 보이게 하는 표준 패턴.
        private const int WM_NCCALCSIZE = 0x0083;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public Helpers.NativeMethods.POINT ptReserved;
            public Helpers.NativeMethods.POINT ptMaxSize;
            public Helpers.NativeMethods.POINT ptMaxPosition;
            public Helpers.NativeMethods.POINT ptMinTrackSize;
            public Helpers.NativeMethods.POINT ptMaxTrackSize;
        }

        private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);


        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private IntPtr _hwnd;
        private SUBCLASSPROC? _subclassProc; // prevent GC collection
        private DispatcherTimer? _deviceChangeDebounceTimer;
        private DispatcherTimer? _drivePollingTimer;
        private HashSet<char> _lastKnownDriveLetters = new();

        private readonly Services.ContextMenuService _contextMenuService;
        private readonly Services.LocalizationService _loc;
        private readonly Services.SettingsService _settings;
        public MainViewModel ViewModel { get; }

        // Type-ahead search
        private string _typeAheadBuffer = string.Empty;
        private DispatcherTimer? _typeAheadTimer;

        // Filter bar debounce (300ms) — prevents 14K filter per keystroke
        private DispatcherTimer? _filterDebounceTimer;

        // Prevents DispatcherQueue callbacks and async methods from accessing
        // disposed UI after OnClosed has started teardown
        private bool _isClosed = false;
        internal bool IsClosed => _isClosed;

        /// <summary>
        /// Single Instance: 리다이렉트된 폴더를 새 탭으로 엽니다.
        /// AddNewTab + CreateMillerPanel + SwitchViewMode + NavigateTo를 통합 처리.
        /// </summary>
        internal void HandleRedirectedFolder(string folderPath)
        {
            if (_isClosed || ViewModel == null) return;
            try
            {
                // 새 탭 추가 + Miller 패널 생성
                ViewModel.AddNewTab();
                if (ViewModel.ActiveTab != null)
                {
                    CreateMillerPanelForTab(ViewModel.ActiveTab);
                    SwitchMillerPanel(ViewModel.ActiveTab.Id);
                }

                // Home → 탐색 뷰로 전환
                if (ViewModel.CurrentViewMode == ViewMode.Home)
                {
                    ViewModel.SwitchViewMode(ViewModel.ResolveViewModeFromHome());
                }
                UpdateViewModeVisibility();
                ResubscribeLeftExplorer();

                // 폴더로 이동 (즐겨찾기 패턴 — 해당 폴더가 루트)
                var folder = new Models.FolderItem
                {
                    Name = System.IO.Path.GetFileName(folderPath) ?? folderPath,
                    Path = folderPath
                };
                _ = ViewModel.ActiveExplorer?.NavigateTo(folder);
                FocusActiveView();
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] HandleRedirectedFolder error: {ex.Message}");
            }
        }

        /// <summary>파일 경로가 넘어온 경우: 부모 폴더를 열고 해당 파일을 선택.</summary>
        internal void HandleRedirectedFile(string filePath)
        {
            if (_isClosed || ViewModel == null) return;
            var parentDir = System.IO.Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(parentDir) || !System.IO.Directory.Exists(parentDir)) return;

            try
            {
                var fileName = System.IO.Path.GetFileName(filePath);

                // 새 탭 추가 + Miller 패널 생성
                ViewModel.AddNewTab();
                if (ViewModel.ActiveTab != null)
                {
                    CreateMillerPanelForTab(ViewModel.ActiveTab);
                    SwitchMillerPanel(ViewModel.ActiveTab.Id);
                }

                if (ViewModel.CurrentViewMode == ViewMode.Home
                    || ViewModel.CurrentViewMode == ViewMode.RecycleBin)
                {
                    ViewModel.SwitchViewMode(ViewModel.ResolveViewModeFromHome());
                }
                UpdateViewModeVisibility();
                ResubscribeLeftExplorer();

                // 부모 폴더로 이동 후 파일 선택
                var folder = new Models.FolderItem
                {
                    Name = System.IO.Path.GetFileName(parentDir) ?? parentDir,
                    Path = parentDir
                };
                _ = NavigateAndSelectFileAsync(folder, fileName);
                FocusActiveView();
                Helpers.DebugLogger.Log($"[MainWindow] HandleRedirectedFile: {parentDir} → select {fileName}");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] HandleRedirectedFile error: {ex.Message}");
            }
        }

        private async Task NavigateAndSelectFileAsync(Models.FolderItem folder, string fileName)
        {
            var explorer = ViewModel?.ActiveExplorer;
            if (explorer == null) return;
            await explorer.NavigateTo(folder);

            // 로딩 완료 후 파일 선택 시도
            await Task.Delay(300); // 폴더 로드 대기
            var lastCol = explorer.Columns.LastOrDefault();
            if (lastCol == null) return;

            var target = lastCol.Children.FirstOrDefault(
                i => string.Equals(i.Name, fileName, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                target.IsSelected = true;
            }
        }

        /// <summary>앱 활성화 시 새 탭 + 휴지통 뷰로 전환.</summary>
        internal void HandleRecycleBinActivation()
        {
            if (_isClosed || ViewModel == null) return;
            try
            {
                // 새 탭 추가 + 휴지통 뷰 전환
                ViewModel.AddNewTab();
                if (ViewModel.ActiveTab != null)
                {
                    CreateMillerPanelForTab(ViewModel.ActiveTab);
                    SwitchMillerPanel(ViewModel.ActiveTab.Id);
                }
                ViewModel.SwitchViewMode(ViewMode.RecycleBin);
                UpdateViewModeVisibility();
                ResubscribeLeftExplorer();
                Helpers.DebugLogger.Log("[MainWindow] Opened RecycleBin in new tab via activation");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] HandleRecycleBinActivation error: {ex.Message}");
            }
        }

        private bool _forceClose = false;

        /// <summary>
        /// Marks this window for a genuine close (bypassing Close-to-Tray hide behavior).
        /// Called by TrayIconService's "Exit LumiFiles" menu.
        /// </summary>
        internal void SetForceClose() => _forceClose = true;

        // Miller Columns checkbox mode tracking
        private ListViewSelectionMode _millerSelectionMode = ListViewSelectionMode.Extended;
        private Thickness _densityPadding = new(12, 2, 12, 2); // comfortable default
        private double _densityMinHeight = 24.0; // comfortable default — synced with Details/List views
        private static readonly Thickness _zeroPadding = new(0);

        // FileSystemWatcher 서비스 참조
        private FileSystemWatcherService? _watcherService;
        private System.IO.FileSystemWatcher? _networkShortcutsWatcher;

        /// <summary>
        /// 현재 테마에 맞는 브러시를 조회한다 (Brush 베이스 — Solid/Linear/Radial 모두 지원).
        /// 윈도우 ThemeDictionaries → 앱 ThemeDictionaries → 앱 MergedDictionaries[*].ThemeDictionaries
        /// → 앱 Resources(merged 포함) 순으로 fallback. XAML {ThemeResource}와 동일한 리소스 해석
        /// 순서를 코드-비하인드에서도 보장한다.
        /// S-3.39: 반환 타입을 SolidColorBrush → Brush로 확장. LumiTheme.xaml의 nested
        /// ThemeDictionaries에 정의된 LinearGradientBrush(LumiPillActiveBrush 등)도
        /// MergedDictionaries 재귀 walk를 통해 찾아낸다.
        /// </summary>
        internal Microsoft.UI.Xaml.Media.Brush GetThemeBrush(string key)
        {
            try
            {
                if (Content is FrameworkElement root)
                {
                    var currentThemeKey = root.ActualTheme == ElementTheme.Light ? "Light" : "Dark";

                    // 1. 윈도우 레벨 ThemeDictionaries (커스텀 테마 오버라이드 우선)
                    if (TryFindInThemeDictionaries(root.Resources, currentThemeKey, key, out var brush))
                        return brush;

                    // 2. 앱 레벨 ThemeDictionaries
                    if (TryFindInThemeDictionaries(Application.Current.Resources, currentThemeKey, key, out var appBrush))
                        return appBrush;

                    // 3. 앱 MergedDictionaries 안의 nested ThemeDictionaries (LumiTheme.xaml 등)
                    foreach (var merged in Application.Current.Resources.MergedDictionaries)
                    {
                        if (TryFindInThemeDictionaries(merged, currentThemeKey, key, out var mergedBrush))
                            return mergedBrush;
                    }
                }
            }
            catch { /* fallback to app level */ }

            return (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[key];
        }

        /// <summary>
        /// ResourceDictionary의 ThemeDictionaries[themeKey]에서 key를 Brush로 찾는다.
        /// SolidColorBrush, LinearGradientBrush, RadialGradientBrush 모두 매칭.
        /// </summary>
        private static bool TryFindInThemeDictionaries(
            ResourceDictionary container, string themeKey, string key, out Microsoft.UI.Xaml.Media.Brush result)
        {
            result = null!;
            if (container.ThemeDictionaries.TryGetValue(themeKey, out var dict)
                && dict is ResourceDictionary rd
                && rd.TryGetValue(key, out var val)
                && val is Microsoft.UI.Xaml.Media.Brush brush)
            {
                result = brush;
                return true;
            }
            return false;
        }

        // H1: FocusActiveView 중복 호출 제거 — UpdateViewModeVisibility 내에서 true로 설정
        private bool _suppressFocusOnViewModeChange = false;

        // H2: 동일 ViewMode 탭 전환 시 NotifyViewModeChanged 스킵
        private ViewMode _previousViewMode = ViewMode.MillerColumns;

        // ── Per-Tab Miller Panels (Show/Hide pattern for instant tab switching) ──
        // 각 탭마다 별도 ScrollViewer+ItemsControl 쌍 유지 — Visibility 토글로 즉시 전환
        private readonly Dictionary<string, (ScrollViewer scroller, ItemsControl items)> _tabMillerPanels = new();
        private string? _activeMillerTabId;

        // PathIndicator 중복 호출 차단용 캐시 (pane별 last applied).
        // 동일 highlight map 연속 호출을 스킵하여 native visual tree 접근 surface 축소.
        private readonly Dictionary<string, string> _lastPathIndicatorSignature = new();

        // ── Per-Tab Details/Icon/List Panels (Show/Hide pattern — Miller와 동일 패턴) ──
        private readonly Dictionary<string, Views.DetailsModeView> _tabDetailsPanels = new();
        private readonly Dictionary<string, Views.IconModeView> _tabIconPanels = new();
        private readonly Dictionary<string, Views.ListModeView> _tabListPanels = new();
        private string? _activeDetailsTabId;
        private string? _activeIconTabId;
        private string? _activeListTabId;

        // Clipboard
        private readonly List<string> _clipboardPaths = new();
        private bool _isCutOperation = false;
        private readonly List<ViewModels.FileSystemViewModel> _cutItems = new();

        // Rename 완료 직후 Enter가 파일 실행으로 이어지는 것을 방지
        private bool _justFinishedRename = false;

        // Selection synchronization guard (Phase 1)
        private bool _isSyncingSelection = false;

        // Rubber-band (marquee) selection helpers per column Grid
        private readonly Dictionary<Grid, Helpers.RubberBandSelectionHelper> _rubberBandHelpers = new();

        // Preview panel selection subscriptions
        private FolderViewModel? _leftPreviewSubscribedColumn;
        private FolderViewModel? _rightPreviewSubscribedColumn;

        // Git status bar ViewModels (left/right panes)
        private GitStatusBarViewModel? _leftGitStatusBarVm;
        private GitStatusBarViewModel? _rightGitStatusBarVm;

        // Sort state
        private string _currentSortField = "Name"; // Name, Date, Size, Type
        private bool _currentSortAscending = true;

        // Tab tear-off drag state
        private bool _isTabDragging;
        private Windows.Foundation.Point _tabDragStartPoint;
        private Models.TabItem? _draggingTab;
        private const double TAB_DRAG_THRESHOLD = 8;

        // Single-tab window drag state (탭 1개일 때 탭 드래그 → 윈도우 이동)
        private bool _isWindowDragging;
        private Helpers.NativeMethods.POINT _windowDragStartCursor;
        private Helpers.NativeMethods.RECT _windowDragStartRect;
        private MainWindow? _windowDragGhostTarget;
        private int _windowDragFrameCount;

        // Tear-off 드래그 타이머 (OnClosed에서 중지용)
        private DispatcherTimer? _tearOffDragTimer;

        // Dynamic tab width (Chrome-style)
        private const double MIN_TAB_WIDTH = 60;
        private const double MAX_TAB_WIDTH = 200;
        private double _calculatedTabWidth = MAX_TAB_WIDTH;

        // Pending tear-off tab state (set before Activate, consumed in Loaded)
        private Models.TabStateDto? _pendingTearOff;
        // True if this window was created from a tear-off (skip session save on close)
        private bool _isTearOffWindow;

        private const double ColumnWidth = 220;

        // Column resize state
        private bool _isResizingColumn = false;
        private Grid? _resizingColumnGrid = null;

        // ContentDialog 중복 열기 방지 가드
        private bool _isContentDialogOpen = false;

        // F2 rename selection cycling: 0=name only, 1=all, 2=extension only
        private int _renameSelectionCycle = 0;
        private string? _renameTargetPath = null;
        private bool _renamePendingFocus = false; // PerformRename → FocusRenameTextBox 사이 LostFocus 무시용
        private double _resizeStartX;
        private double _resizeStartWidth;

        // Spring-loaded folders: auto-open folder after drag hover delay
        private DispatcherTimer? _springLoadTimer;
        private FolderViewModel? _springLoadTarget;
        private Grid? _springLoadGrid;
        private const int SPRING_LOAD_DELAY_MS = 700;

        // Quick Look floating window
        private Views.QuickLookWindow? _quickLookWindow;

        /// <summary>
        /// MainWindow의 기본 생성자.
        /// XAML 컴포넌트 초기화, 서비스 주입, 이벤트 구독, P/Invoke 서브클래싱,
        /// 윈도우 배치 복원, 탭·뷰 패널 초기화, 설정 적용 등 전체 시작 로직을 수행한다.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // FontScaleService 싱글톤은 App.xaml 에 <helpers:FontScaleService x:Key="FontScale"/>
            // 로 선언되어 XAML 파서가 Application.Resources 에 등록함. XAML {StaticResource FontScale}
            // 와 C# FontScaleService.Instance 는 동일한 객체 하나만 존재.

            // 전역 FocusVisual 스타일: WinUI 3의 FocusVisualPrimaryBrush 기본값이 하드코딩(White)이라
            // ThemeResource 오버라이드 불가.
            // GettingFocus(포커스 설정 전)에서 브러시 교체 → 첫 포커스부터 올바른 스타일 적용
            this.Content.AddHandler(UIElement.GettingFocusEvent,
                new Windows.Foundation.TypedEventHandler<UIElement, GettingFocusEventArgs>(OnGlobalGettingFocus), true);

            // 좌/우 탐색기 패널 포커스: handledEventsToo=true로 등록해야
            // ListView/ScrollViewer가 이벤트를 처리한 후에도 Pane 포커스 전환 가능
            LeftPaneContainer.AddHandler(UIElement.PointerPressedEvent,
                new Microsoft.UI.Xaml.Input.PointerEventHandler(OnLeftPanePointerPressed), true);
            RightPaneContainer.AddHandler(UIElement.PointerPressedEvent,
                new Microsoft.UI.Xaml.Input.PointerEventHandler(OnRightPanePointerPressed), true);

            ViewModel = App.Current.Services.GetRequiredService<MainViewModel>();
            _contextMenuService = App.Current.Services.GetRequiredService<Services.ContextMenuService>();
            _loc = App.Current.Services.GetRequiredService<Services.LocalizationService>();
            _settings = App.Current.Services.GetRequiredService<Services.SettingsService>();

            // Folder custom icon service: UI dispatcher 주입 (설정 OFF이면 호출 없으니 무시됨)
            try
            {
                var folderIconSvc = App.Current.Services.GetService(typeof(Services.FolderIconService)) as Services.FolderIconService;
                folderIconSvc?.Initialize(this.DispatcherQueue);
            }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[MainWindow] FolderIconService init failed: {ex.Message}"); }

            // Workspace button
            WorkspaceButton.Click += async (s, e) => await ShowWorkspacePaletteAsync();

            // Subscribe to file open events for toast feedback
            var shellService = App.Current.Services.GetRequiredService<ShellService>();
            shellService.FileOpening += OnShellFileOpening;

            // Wire up file operation progress panel
            var fileOpManager = App.Current.Services.GetRequiredService<Services.FileOperationManager>();
            FileOpProgressControl.SetOperationManager(fileOpManager);

            // File Shelf initialization
            InitializeShelf();

            // Tahoe Liquid Glass + Win11 vibrancy hybrid (Stage S-3.8, Plan C).
            // Earlier this was forced to null so the custom 5-radial wallpaper
            // could be the only backdrop, but with the radials disabled the
            // window read as flat dark — no glass at all. Re-enable the system
            // backdrop so the desktop bleeds through the translucent
            // WindowFrame / sidebar / path-bar layers (the macOS-Finder
            // vibrancy effect the user is going for). DesktopAcrylic gives the
            // strongest "see the desktop" feel; if the host doesn't support it
            // (older Win10) fall back to Mica, then to no backdrop.
            try
            {
                if (DesktopAcrylicController.IsSupported())
                    SystemBackdrop = new DesktopAcrylicBackdrop();
                else if (MicaController.IsSupported())
                    SystemBackdrop = new MicaBackdrop();
                else
                    SystemBackdrop = null;
            }
            catch (System.Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] SystemBackdrop init failed: {ex.Message}");
                SystemBackdrop = null;
            }

            // Stage S-3.21 (Plan A — DragShelf borderless port):
            // Strip the system chrome entirely so our WindowFrame Border is
            // the only thing that draws the window outline. Without this,
            // Win11 forces a small ~8px chrome corner that fights any
            // larger LumiWindowCornerRadius we set; with this, the window's
            // visible shape is 100% controlled by our XAML.
            //
            // Pattern (matches D:\11.AI\DragShelf\source EphemeralShelfWindow):
            //   1. SetBorderAndTitleBar(false, false)   — hide system caption
            //   2. DwmExtendFrameIntoClientArea(-1)     — collapse system frame
            //   3. SetWindowLong: strip WS_OVERLAPPEDWINDOW, add WS_POPUP
            //   4. DwmSetWindowAttribute(ROUND)         — request rounded shape
            //
            // Caption buttons (min / max / close) are now drawn by us in
            // AppTitleBar.CaptionButtonsHost (XAML), wired to the handlers
            // below.
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

                // 1. Hide system caption buttons + border (presenter-level)
                if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.SetBorderAndTitleBar(false, false);
                }

                // 2. Collapse the system frame margin into the client area
                var margins = new Helpers.NativeMethods.MARGINS
                {
                    Left = -1, Right = -1, Top = -1, Bottom = -1
                };
                Helpers.NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);

                // 3. Strip WS_OVERLAPPEDWINDOW bits + add WS_POPUP only.
                //    Stage S-3.25: dropped WS_THICKFRAME. Earlier we kept it
                //    so the OS would still hit-test edge-resize, but THICKFRAME
                //    also makes the OS reserve a 1px non-client border on
                //    every side and paint it during composition. That painted
                //    1px is visible as the top "검은 라인" + the small square
                //    leftover patches at the four rounded corners — exactly
                //    what DragShelf's ShelfWindow avoids by NOT including
                //    THICKFRAME (the dock shelf is fixed-size).
                //    Trade-off: edge-drag-to-resize is lost. Maximize / restore
                //    via the caption buttons still works through
                //    OverlappedPresenter. If the user wants edge-drag back,
                //    Option B (WM_NCCALCSIZE subclass) is the next step.
                //    NativeMethods.GetWindowLong / SetWindowLong return int
                //    (legacy signatures); unchecked casts bridge to the uint
                //    style flags.
                uint style = unchecked((uint)Helpers.NativeMethods.GetWindowLong(
                    hwnd, Helpers.NativeMethods.GWL_STYLE));
                style &= ~Helpers.NativeMethods.WS_OVERLAPPEDWINDOW;
                // S-3.40: Snap Layouts (Win11 최대화 hover 팝업) 가 동작하려면 윈도우에
                // WS_MAXIMIZEBOX (+ 짝으로 WS_MINIMIZEBOX, WS_SYSMENU) 스타일이 살아있어야 함.
                // WS_OVERLAPPEDWINDOW 마스크에 포함된 비트들이라 strip 후 다시 OR 로 부활시킴.
                // InputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Maximize, ...)
                // 만으론 부족 — 윈도우 자체가 maximizable 임을 OS 가 알아야 hover 팝업이 뜸.
                style |= Helpers.NativeMethods.WS_POPUP
                       | Helpers.NativeMethods.WS_CLIPCHILDREN;
                // S-3.40 ROLLBACK: WS_MAXIMIZEBOX/MINIMIZEBOX/SYSMENU/THICKFRAME 추가
                // 시도 → Snap Layouts hover 팝업은 살짝 동작했으나 WS_THICKFRAME 이
                // SetWindowRgn / DWM 보더 / acrylic 잔여 픽셀과 충돌해 우/하단 아티팩트
                // 남는 문제 해결 안 됨. WM_NCCALCSIZE 차단, DWMWA_BORDER_COLOR=NONE,
                // GetWindowRect 기반 region 모두 시도했지만 cleanup 불완전.
                // 사용자 결정으로 Snap Layouts hover 기능 포기, 원래 borderless 안정 상태
                // (WS_POPUP + WS_CLIPCHILDREN 만) 로 복원.
                Helpers.NativeMethods.SetWindowLong(
                    hwnd,
                    Helpers.NativeMethods.GWL_STYLE,
                    unchecked((int)style));

                // Stage S-3.23: explicitly DWMWCP_DONOTROUND. Earlier S-3.22
                // dropped the corner-preference call entirely, mirroring
                // DragShelf's ShelfWindow — but that only worked for
                // ShelfWindow because IT also sets WS_EX_TOOLWINDOW, and
                // TOOLWINDOW windows are exempt from Win11's default
                // automatic ~8px corner clip. LumiFiles is a main window,
                // not a tool window (it must show in taskbar / alt-tab),
                // so just omitting the call leaves Win11's default ROUND
                // behaviour active and the 18px WindowFrame still gets
                // trimmed to ~8px. We have to ask DWM for DONOTROUND
                // explicitly to suppress the default mask.
                int pref = Helpers.NativeMethods.DWMWCP_DONOTROUND;
                Helpers.NativeMethods.DwmSetWindowAttribute(
                    hwnd,
                    Helpers.NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref pref,
                    sizeof(int));

                // Show the self-drawn caption buttons now that system chrome
                // is gone (otherwise the user would have no way to close).
                CaptionButtonsHost.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

                // Stage S-3.24: clip the OS window hit-area itself to a
                // rounded rect via SetWindowRgn. Without this, DONOTROUND
                // leaves the hit-area square, and the 4 outer-corner
                // triangles between our 18px round Border and the square
                // hit-area get painted with DesktopAcrylic — the user sees
                // them as dark leftover patches in the corners. Pattern
                // ported from DragShelf ShelfWindow.UpdateXamlClip.
                ApplyRoundedWindowRegion();
                if (RootGrid != null)
                {
                    RootGrid.SizeChanged += (_, __) => ApplyRoundedWindowRegion();
                }
            }
            catch (System.Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] Borderless init failed: {ex.Message}");
            }

            // ====================================================================
            // Stage 4 — LumiSidebar navigation dispatch (placeholder bindings).
            // Resolves path from the item's TextBlock label and navigates the
            // active explorer. Recent/Settings/Tags currently noop.
            // ====================================================================
            // (handler defined below as a method on this partial class)

            // Close-to-Tray policy:
            //   - Setting OFF  → always real close (existing behavior)
            //   - Setting ON + multiple windows open → real close this window only
            //     (other windows keep the app alive; X is treated as window cleanup)
            //   - Setting ON + this is the LAST window → hide to tray
            //     (preserves the user's "keep app running" intent)
            //   - _forceClose bypass: TrayIconService's "Exit LumiFiles" menu sets this flag.
            //
            // Rationale: avoids the trap where one window's X forces every window into
            // the tray. X keeps its intuitive "close this window" meaning unless the
            // window is the last thing keeping the app visible.
            this.AppWindow.Closing += (s, e) =>
            {
                if (!_settings.MinimizeToTray || _forceClose) return;

                // If other windows remain, let this one close normally.
                var windowCount = App.Current.GetRegisteredWindows().Count;
                if (windowCount > 1)
                {
                    Helpers.DebugLogger.Log($"[MainWindow] Close-to-Tray: {windowCount} windows open, closing this one normally");
                    return;
                }

                // Last window → hide to tray.
                try
                {
                    e.Cancel = true;
                    // AppWindow.Hide() removes from taskbar AND Alt+Tab, unlike SW_MINIMIZE.
                    this.AppWindow.Hide();
                    // Ensure tray icon is alive (user may have toggled mid-session).
                    (App.Current.Services.GetService(typeof(Services.TrayIconService)) as Services.TrayIconService)
                        ?.SyncWithSetting();
                    Helpers.DebugLogger.Log("[MainWindow] Close-to-Tray: last window hidden to tray");
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[MainWindow] Hide-to-tray failed: {ex.Message}");
                    e.Cancel = false; // fall back to real close rather than leave user stuck
                }
            };

            // TitleBar
            ExtendsContentIntoTitleBar = true;
            // SetTitleBar → 전체 타이틀바를 드래그 영역 + 캡션 버튼 자동 관리
            // Passthrough 영역은 Loaded 후 SetRegionRects로 별도 설정 (탭 영역만)
            SetTitleBar(AppTitleBar);

            // Auto-scroll on column change (both panes)
            _subscribedLeftExplorer = ViewModel.Explorer;
            ViewModel.Explorer.Columns.CollectionChanged += OnColumnsChanged;
            ViewModel.Explorer.NavigationError += OnNavigationError;
            ViewModel.Explorer.PathHighlightsUpdated += OnPathHighlightsUpdated;
            // v1.4.19: spacer 펼치기/접기로 ExtentWidth 박동 차단
            // 인스턴스 단위 구독은 backing field 직접 할당 케이스에서 새 인스턴스로 안 따라감 →
            // 정적 이벤트로 forward 받아 sender 비교로 라우팅 (인스턴스 무관 보장).
            ViewModels.ExplorerViewModel.AnyBeforeReplaceLastColumn += OnAnyBeforeReplaceLastColumn;
            ViewModels.ExplorerViewModel.AnyAfterReplaceLastColumn += OnAnyAfterReplaceLastColumn;
            Helpers.DebugLogger.Log($"[Diag-Miller] L:Subscribed.init.static (instance-agnostic forward)");
            ViewModel.RightExplorer.Columns.CollectionChanged += OnRightColumnsChanged;
            ViewModel.RightExplorer.NavigationError += OnNavigationError;
            ViewModel.RightExplorer.PathHighlightsUpdated += OnPathHighlightsUpdated;
            // v1.4.19: 좌/우 모두 정적 forward 이벤트로 통합 → 인스턴스 단위 구독 불필요

            // v1.4.19: 자식 컨트롤(ListView 등)의 자동 BringIntoView 요청을 부모 ScrollViewer가
            // 가로 스크롤로 처리하지 않도록 차단. 가로 스크롤은 ScrollToLastColumn / ChangeView
            // 명시 호출로만 제어 → 형제 폴더 토글 시 위치 점프·어중간 정렬 등 자동 동작 원천 차단.
            MillerScrollViewer.BringIntoViewRequested += OnMillerBringIntoViewRequested;
            MillerScrollViewerRight.BringIntoViewRequested += OnMillerBringIntoViewRequested;

            // ── Per-Tab Miller Panel 초기화 ──
            // XAML에서 ItemsSource가 제거되었으므로 코드에서 설정
            MillerColumnsControl.ItemsSource = ViewModel.Explorer.Columns;
            var firstTabId = ViewModel.Tabs.Count > 0 ? ViewModel.Tabs[0].Id : "_default";
            _tabMillerPanels[firstTabId] = (MillerScrollViewer, MillerColumnsControl);
            _activeMillerTabId = firstTabId;

            // ── Per-Tab Details/Icon/List Panel 초기화 ──
            _tabDetailsPanels[firstTabId] = DetailsView;
            _tabIconPanels[firstTabId] = IconView;
            _tabListPanels[firstTabId] = ListView;
            _activeDetailsTabId = firstTabId;
            _activeIconTabId = firstTabId;
            _activeListTabId = firstTabId;

            // Focus management on ViewMode change
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            ViewModel.LastTabClosed += (_, __) => this.Close();
            ViewModel.NetworkShortcutFtpRequested += OnNetworkShortcutFtpRequested;

            // Set ViewModel for Details, List and Icon views (left pane)
            DetailsView.ViewModel = ViewModel.Explorer;
            ListView.ViewModel = ViewModel.Explorer;
            IconView.ViewModel = ViewModel.Explorer;
            HomeView.MainViewModel = ViewModel;
            // Stage S-3.32: SettingsView is gone (moved to SettingsWindow).
            // The SettingsWindow itself wires its own BackRequested → Close().
            LogView.BackRequested += (s, e) => CloseCurrentActionLogTab();

            // AddressBarControl에 PathSegments/CurrentPath 바인딩
            SyncAddressBarControls(ViewModel.Explorer);

            // Set ViewModel for Details and Icon views (right pane)
            HomeViewRight.MainViewModel = ViewModel;
            DetailsViewRight.IsRightPane = true;
            DetailsViewRight.ViewModel = ViewModel.RightExplorer;
            ListViewRight.IsRightPane = true;
            ListViewRight.ViewModel = ViewModel.RightExplorer;
            IconViewRight.IsRightPane = true;
            IconViewRight.ViewModel = ViewModel.RightExplorer;

            // Get HWND early (needed by child views and context menu service)
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Increment app launch count for Store rating prompt
            _settings.AppLaunchCount++;

            // Window title (shown in taskbar thumbnail & Alt+Tab)
            this.Title = "LumiFinder";

            // Window icon (shown in taskbar & title bar)
            try
            {
#pragma warning disable CA1416 // Platform compatibility (guarded by try-catch)
                var iconPath = System.IO.Path.Combine(
                    Windows.ApplicationModel.Package.Current.InstalledPath,
                    "Assets", "app.ico");
#pragma warning restore CA1416
                if (System.IO.File.Exists(iconPath))
                    this.AppWindow.SetIcon(iconPath);
            }
            catch { /* unpackaged mode — icon set by manifest */ }

            // Pass context menu service and HWND to child views
            _contextMenuService.OwnerHwnd = _hwnd;
            _contextMenuService.XamlRootProvider = () => Content.XamlRoot;
            _contextMenuService.InvokeFailedCallback = (itemName) =>
            {
                Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () =>
                {
                    ViewModel.ShowToast(string.Format(_loc.Get("Toast_ShellCommandFailed"), itemName), 3000, isError: true);
                });
            };
            _contextMenuService.ShellCommandExecutedCallback = () =>
            {
                var currentPath = ViewModel?.ActiveExplorer?.CurrentPath;
                DispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await Task.Delay(1000);
                        // Only refresh if still on the same folder
                        if (currentPath != null && ViewModel?.ActiveExplorer?.CurrentPath == currentPath)
                            await ViewModel.RefreshCurrentFolderAsync();
                    }
                    catch (Exception ex)
                    {
                        Helpers.DebugLogger.Log($"[MainWindow] Post-shell refresh error: {ex.Message}");
                    }
                });
            };
            DetailsView.ContextMenuService = _contextMenuService;
            DetailsView.ContextMenuHost = this;
            DetailsView.OwnerHwnd = _hwnd;
            ListView.ContextMenuService = _contextMenuService;
            ListView.ContextMenuHost = this;
            ListView.OwnerHwnd = _hwnd;
            IconView.ContextMenuService = _contextMenuService;
            IconView.ContextMenuHost = this;
            IconView.OwnerHwnd = _hwnd;
            HomeView.ContextMenuService = _contextMenuService;
            HomeView.ContextMenuHost = this;
            HomeViewRight.ContextMenuService = _contextMenuService;
            HomeViewRight.ContextMenuHost = this;
            DetailsViewRight.ContextMenuService = _contextMenuService;
            DetailsViewRight.ContextMenuHost = this;
            DetailsViewRight.OwnerHwnd = _hwnd;
            IconViewRight.ContextMenuService = _contextMenuService;
            IconViewRight.ContextMenuHost = this;
            IconViewRight.OwnerHwnd = _hwnd;

            // ★ ItemsControl에서 키보드 이벤트 가로채기 (both panes)
            MillerColumnsControl.AddHandler(
                UIElement.KeyDownEvent,
                new KeyEventHandler(OnMillerKeyDown),
                true
            );
            MillerColumnsControlRight.AddHandler(
                UIElement.KeyDownEvent,
                new KeyEventHandler(OnMillerKeyDown),
                true
            );

            // ★ CharacterReceived: 비라틴 문자(한글/일본어/중국어) 타입 어헤드 지원
            MillerColumnsControl.AddHandler(
                UIElement.CharacterReceivedEvent,
                new Windows.Foundation.TypedEventHandler<UIElement, Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs>(OnMillerCharacterReceived),
                true
            );
            MillerColumnsControlRight.AddHandler(
                UIElement.CharacterReceivedEvent,
                new Windows.Foundation.TypedEventHandler<UIElement, Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs>(OnMillerCharacterReceived),
                true
            );

            // ★ Window-level 단축키 (Ctrl 조합)
            this.Content.AddHandler(
                UIElement.KeyDownEvent,
                new KeyEventHandler(OnGlobalKeyDown),
                true  // Handled 된 이벤트도 받음
            );

            // ★ Mouse Back/Forward buttons (XButton1=Back, XButton2=Forward)
            this.Content.AddHandler(
                UIElement.PointerPressedEvent,
                new PointerEventHandler(OnGlobalPointerPressed),
                true
            );

            // ★ Ctrl+Mouse Wheel view mode cycling (global — works in ALL views)
            this.Content.AddHandler(
                UIElement.PointerWheelChangedEvent,
                new PointerEventHandler(OnGlobalPointerWheelChanged),
                true  // handledEventsToo: catches events even after ScrollViewer/ListView consume them
            );

            // Type-ahead timer
            _typeAheadTimer = new DispatcherTimer();
            _typeAheadTimer.Interval = TimeSpan.FromMilliseconds(800);
            _typeAheadTimer.Tick += (s, e) =>
            {
                _typeAheadBuffer = string.Empty;
                _typeAheadTimer.Stop();
            };

            this.Closed += OnClosed;

            // WM_DEVICECHANGE: detect USB drive plug/unplug
            _subclassProc = new SUBCLASSPROC(WndProc);
            SetWindowSubclass(_hwnd, _subclassProc, IntPtr.Zero, IntPtr.Zero);


            _deviceChangeDebounceTimer = new DispatcherTimer();
            _deviceChangeDebounceTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _deviceChangeDebounceTimer.Tick += (s, e) =>
            {
                _deviceChangeDebounceTimer.Stop();
                if (!_isClosed)
                {
                    ViewModel.RefreshDrives();
                }
            };

            // Periodic drive polling: detect virtual drive mount/unmount
            // (Google Drive, OneDrive, etc. don't fire WM_DEVICECHANGE)
            _lastKnownDriveLetters = new HashSet<char>(
                System.IO.DriveInfo.GetDrives().Select(d => d.Name[0]));
            _drivePollingTimer = new DispatcherTimer();
            _drivePollingTimer.Interval = TimeSpan.FromSeconds(5);
            _drivePollingTimer.Tick += OnDrivePollingTick;
            _drivePollingTimer.Start();

            // ── Restore window position ──
            // Cloak the window so the user never sees the WinUI default size.
            // Activate() resets the size, but the Loaded handler re-applies
            // the saved placement and then uncloaks.
            // Skip for tear-off windows — TearOffTab manages cloak/position via drag timer.
            if (_settings.RememberWindowPosition && _pendingTearOff == null)
            {
                int cloakOn = 1;
                Helpers.NativeMethods.DwmSetWindowAttribute(
                    _hwnd, Helpers.NativeMethods.DWMWA_CLOAK, ref cloakOn, sizeof(int));
                RestoreWindowPlacement();
            }

            // Initialize preview panels
            InitializePreviewPanels();

            // Apply saved settings
            ApplyTheme(_settings.Theme);
            ApplyFontFamily(_settings.FontFamily);
            ApplyDensity(_settings.Density);
            ApplyIconFontScale(_settings.IconFontScale);
            _settings.SettingChanged += OnSettingChanged;

            // Connect Language setting to LocalizationService
            // "system" resolves to OS locale via ResolveSystemLanguage()
            _loc.Language = _settings.Language;
            LocalizeViewModeTooltips();
            _loc.LanguageChanged += LocalizeViewModeTooltips;
            // S-3.40: MainWindow 툴바/타이틀바 등 hard-coded 문자열 일괄 i18n
            LoadMainWindowLocalization();
            _loc.LanguageChanged += LoadMainWindowLocalization;

            // Restore split view state and preview state from persisted settings
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.Loaded += (s, e) =>
                {
                    if (_pendingTearOff != null)
                    {
                        // ── Tear-off mode: load single tab from DTO, skip session restore ──
                        _isTearOffWindow = true;
                        var dto = _pendingTearOff;
                        _pendingTearOff = null;

                        try
                        {
                            _ = ViewModel.LoadSingleTabFromDtoAsync(dto);
                        }
                        catch (Exception ex)
                        {
                            Helpers.DebugLogger.Log($"[TearOff] LoadSingleTabFromDtoAsync failed: {ex.Message}");
                        }

                        // Re-bind MillerColumnsControl to the new explorer
                        MillerColumnsControl.ItemsSource = ViewModel.Explorer.Columns;
                        var tabId = ViewModel.ActiveTab?.Id ?? "_default";
                        _tabMillerPanels.Clear();
                        _tabMillerPanels[tabId] = (MillerScrollViewer, MillerColumnsControl);
                        _activeMillerTabId = tabId;

                        // Re-bind Details/Icon panels
                        _tabDetailsPanels.Clear();
                        _tabIconPanels.Clear();
                        _tabDetailsPanels[tabId] = DetailsView;
                        _tabIconPanels[tabId] = IconView;
                        _activeDetailsTabId = tabId;
                        _activeIconTabId = tabId;

                        DetailsView.ViewModel = ViewModel.Explorer;
                        IconView.ViewModel = ViewModel.Explorer;
                        SyncAddressBarControls(ViewModel.Explorer);

                        // Resubscribe column changes
                        if (_subscribedLeftExplorer != null)
                        {
                            _subscribedLeftExplorer.Columns.CollectionChanged -= OnColumnsChanged;
                            _subscribedLeftExplorer.PathHighlightsUpdated -= OnPathHighlightsUpdated;
                        }
                        _subscribedLeftExplorer = ViewModel.Explorer;
                        ViewModel.Explorer.Columns.CollectionChanged += OnColumnsChanged;
                        ViewModel.Explorer.PathHighlightsUpdated += OnPathHighlightsUpdated;

                        _previousViewMode = ViewModel.CurrentViewMode;
                        SetViewModeVisibility(ViewModel.CurrentViewMode);

                        // ── 밀러컬럼 뷰포트 리사이즈 시 마지막 컬럼으로 자동 스크롤 ──
                        MillerScrollViewer.SizeChanged += OnMillerScrollViewerSizeChanged;

                        // Set tab bar as passthrough so pointer events work for tear-off
                        UpdateTitleBarRegions();
                        TabScrollViewer.SizeChanged += (_, __) => { UpdateTitleBarRegions(); DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, RecalculateTabWidths); };
                        TabBarContent.SizeChanged += (_, __) => UpdateTitleBarRegions();
                        this.SizeChanged += (_, __) => UpdateTitleBarRegions();

                        // Chrome-style dynamic tab width: recalculate on tab add/remove
                        ViewModel.Tabs.CollectionChanged += (_, __) =>
                            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, RecalculateTabWidths);
                        // Loaded 시점에는 레이아웃 미완료 → 지연 호출로 정확한 ActualWidth 사용
                        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, RecalculateTabWidths);

                        // Populate favorites tree for tear-off window
                        ApplyFavoritesTreeMode(_settings.ShowFavoritesTree);
                        PopulateFavoritesTree();
                        ViewModel.Favorites.CollectionChanged += OnFavoritesCollectionChanged;
                        ApplySidebarSectionVisibility();

                        // Uncloak if cloaked during constructor (RememberWindowPosition)
                        // For tear-off windows, uncloak is managed by StartManualWindowDrag timer
                        if (_settings.RememberWindowPosition && !_isTearOffWindow)
                        {
                            int cloakOff = 0;
                            Helpers.NativeMethods.DwmSetWindowAttribute(
                                _hwnd, Helpers.NativeMethods.DWMWA_CLOAK, ref cloakOff, sizeof(int));
                        }

                        // Re-apply icon/font scale after visual tree is fully ready
                        // level 0에서도 baseline 저장을 위해 반드시 실행 (idempotent)
                        Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue,
                            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                            () => ApplyIconFontScale(_settings.IconFontScale));

                        Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue,
                            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                            () => FocusActiveView());
                        return;
                    }

                    // ── Re-apply window placement after Activate + layout, then uncloak ──
                    if (!_isTearOffWindow && _settings.RememberWindowPosition)
                    {
                        RestoreWindowPlacement();
                        DispatcherQueue.TryEnqueue(
                            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                            () =>
                            {
                                if (!_isClosed && _settings.RememberWindowPosition)
                                    RestoreWindowPlacement();

                                // Uncloak — window is now at the correct size
                                int cloakOff = 0;
                                Helpers.NativeMethods.DwmSetWindowAttribute(
                                    _hwnd, Helpers.NativeMethods.DWMWA_CLOAK, ref cloakOff, sizeof(int));
                            });
                    }

                    // ── Normal startup: restore session tabs ──
                    RestorePreviewState();
                    ViewModel.LoadTabsFromSettings();

                    if (ViewModel.IsSplitViewEnabled)
                    {
                        SplitterCol.Width = new GridLength(0);
                        RightPaneCol.Width = new GridLength(1, GridUnitType.Star);

                        // Tab 2 startup. behavior=0 (default — was Home, now Desktop matching Tab1),
                        // 1=RestoreSession, 2=CustomPath. Uses the sidebar-click pattern
                        // (NavigateTo(FolderItem) + EnableAutoNavigation suppressed) so the right
                        // pane lands on the target folder as COLUMN 1 instead of expanding the
                        // full ancestor chain via NavigateToPath.
                        var tab2Behavior = _settings.Tab2StartupBehavior;
                        if (ViewModel.RightExplorer.Columns.Count == 0 ||
                            ViewModel.RightExplorer.CurrentPath == "PC")
                        {
                            string? targetPath = null;
                            if (tab2Behavior == 2 && !string.IsNullOrEmpty(_settings.Tab2StartupPath)
                                && System.IO.Directory.Exists(_settings.Tab2StartupPath))
                            {
                                targetPath = _settings.Tab2StartupPath;
                            }
                            else if (tab2Behavior == 1)
                            {
                                // Restore last session: keep legacy helper which uses prior path.
                                NavigateRightPaneToRealPath();
                                targetPath = null; // already handled
                            }
                            else
                            {
                                // 0 (default) — Desktop.
                                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                if (!string.IsNullOrEmpty(desktop) && System.IO.Directory.Exists(desktop))
                                    targetPath = desktop;
                                else
                                    NavigateRightPaneToRealPath(); // extreme fallback
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
                    }

                    // ── Per-Tab Miller Panels: 세션 복원 후 모든 탭에 대해 패널 생성 ──
                    InitializeTabMillerPanels();

                    // ── 세션 복원 후 Explorer가 교체될 수 있으므로 전체 동기화 ──
                    SyncAddressBarControls(ViewModel.Explorer);
                    DetailsView.ViewModel = ViewModel.Explorer;
                    ListView.ViewModel = ViewModel.Explorer;
                    IconView.ViewModel = ViewModel.Explorer;
                    ResubscribeLeftExplorer();

                    // ── Jump List activation: navigate to the specified folder ──
                    if (!string.IsNullOrEmpty(App.StartupArguments))
                    {
                        var jumpArg = App.StartupArguments;
                        App.StartupArguments = null; // Consume to prevent re-navigation
                        jumpArg = jumpArg?.Trim().Trim('"');

                        if (jumpArg != "--new-window")
                        {
                            // 가상 폴더 처리 (휴지통, 내 PC 등)
                            if (IsRecycleBinArgument(jumpArg))
                            {
                                Helpers.DebugLogger.Log($"[Startup] RecycleBin argument: {jumpArg}");
                                ViewModel.SwitchViewMode(ViewMode.RecycleBin);
                                UpdateViewModeVisibility();
                            }
                            else if (IsThisPCArgument(jumpArg))
                            {
                                // This PC → LumiFiles 홈 화면 (이미 기본값이므로 별도 처리 불필요)
                                Helpers.DebugLogger.Log($"[Startup] This PC argument → Home: {jumpArg}");
                            }
                            else if (TryDelegateVirtualFolder(jumpArg))
                            {
                                // shell:/CLSID 가상 폴더 → explorer.exe 위임 후 이 창 닫기
                            }
                            else if (System.IO.Directory.Exists(jumpArg))
                            {
                                Helpers.DebugLogger.Log($"[JumpList] Navigating to: {jumpArg}");
                                // Home/RecycleBin 모드면 탐색 뷰로 전환 후 네비게이션
                                if (ViewModel.CurrentViewMode == ViewMode.Home
                                    || ViewModel.CurrentViewMode == ViewMode.RecycleBin)
                                {
                                    ViewModel.SwitchViewMode(ViewModel.ResolveViewModeFromHome());
                                    UpdateViewModeVisibility();
                                }
                                _ = ViewModel.ActiveExplorer?.NavigateToPath(jumpArg);
                            }
                            else if (System.IO.File.Exists(jumpArg))
                            {
                                // 파일 경로 → 부모 폴더 열고 파일 선택
                                Helpers.DebugLogger.Log($"[Startup] File argument: {jumpArg}");
                                var parentDir = System.IO.Path.GetDirectoryName(jumpArg);
                                var fileName = System.IO.Path.GetFileName(jumpArg);
                                if (!string.IsNullOrEmpty(parentDir) && System.IO.Directory.Exists(parentDir))
                                {
                                    if (ViewModel.CurrentViewMode == ViewMode.Home
                                        || ViewModel.CurrentViewMode == ViewMode.RecycleBin)
                                    {
                                        ViewModel.SwitchViewMode(ViewModel.ResolveViewModeFromHome());
                                        UpdateViewModeVisibility();
                                    }
                                    var folder = new Models.FolderItem
                                    {
                                        Name = System.IO.Path.GetFileName(parentDir) ?? parentDir,
                                        Path = parentDir
                                    };
                                    _ = NavigateAndSelectFileAsync(folder, fileName);
                                }
                            }
                        }
                    }

                    // ── Populate Favorites Tree and observe changes ──
                    ApplyFavoritesTreeMode(_settings.ShowFavoritesTree);
                    PopulateFavoritesTree();
                    ViewModel.Favorites.CollectionChanged += OnFavoritesCollectionChanged;
                    ApplySidebarSectionVisibility();

                    // ── 밀러컬럼 뷰포트 리사이즈 시 마지막 컬럼으로 자동 스크롤 ──
                    MillerScrollViewer.SizeChanged += OnMillerScrollViewerSizeChanged;
                    MillerScrollViewerRight.SizeChanged += OnMillerScrollViewerRightSizeChanged;

                    // Set tab bar as passthrough so pointer events work for tab tear-off
                    UpdateTitleBarRegions();
                    TabScrollViewer.SizeChanged += (_, __) => { UpdateTitleBarRegions(); DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, RecalculateTabWidths); };
                    TabBarContent.SizeChanged += (_, __) => UpdateTitleBarRegions();
                    this.SizeChanged += (_, __) => UpdateTitleBarRegions();

                    // Chrome-style dynamic tab width: recalculate on tab add/remove
                    ViewModel.Tabs.CollectionChanged += (_, __) =>
                        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, RecalculateTabWidths);
                    RecalculateTabWidths();

                    // ViewMode Visibility 초기화 (x:Bind 제거 후 코드비하인드에서 관리)
                    _previousViewMode = ViewModel.CurrentViewMode;
                    SetViewModeVisibility(ViewModel.CurrentViewMode);

                    // Focus the active view after session restore
                    // NavigateTo is async, so delay to ensure items are loaded
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                        () => FocusActiveView());

                    // Apply ShowCheckboxes to Miller Columns after initial render
                    if (_settings.ShowCheckboxes)
                    {
                        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                            () => ApplyMillerCheckboxMode(true));
                    }

                    // Re-apply icon/font scale after visual tree is fully ready
                    // level 0에서도 실행: baseline 저장을 위해 반드시 필요 (idempotent)
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                        () => ApplyIconFontScale(_settings.IconFontScale));

                    // Apply MillerClickBehavior on startup
                    if (_settings.MillerClickBehavior == "double")
                    {
                        ViewModel.Explorer.EnableAutoNavigation = false;
                        ViewModel.RightExplorer.EnableAutoNavigation = false;
                    }

                    // Restore saved sort/group settings
                    try
                    {
                        var appSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                        if (appSettings.Values.TryGetValue("MillerSortBy", out var sby) && sby is string sortField)
                        {
                            _currentSortField = sortField switch { "DateModified" => "Date", _ => sortField };
                        }
                        if (appSettings.Values.TryGetValue("MillerSortAsc", out var sasc) && sasc is bool sortAsc)
                            _currentSortAscending = sortAsc;
                        if (appSettings.Values.TryGetValue("ViewGroupBy", out var vgb) && vgb is string grp)
                            _currentGroupBy = grp;
                        UpdateSortButtonIcons();
                    }
                    catch { }

                    // Restore saved sidebar width
                    RestoreSidebarWidth();

                    // Tab ElementPrepared: apply scale to newly created tabs
                    TabRepeater.ElementPrepared += OnTabElementPrepared;

                    // FileSystemWatcher 초기화
                    InitializeFileSystemWatcher();

                    // ── 첫 실행 시 온보딩 창 표시 ──
                    // Tear-off 윈도우는 대상 아님 (위 _pendingTearOff 분기에서 return됨)
                    // v1.0.17 (Span Discussion #30 port): OnboardingDisabled 옵션 켜져 있으면 첫 실행에도 차단.
                    // 가드 결과를 로그에 남겨 사용자가 "토글 켰는데도 떴다" 보고 시 어떤 플래그
                    // 상태였는지 즉시 파악 가능하게 함.
                    bool _obCompleted = _settings.OnboardingCompleted;
                    bool _obDisabled = _settings.OnboardingDisabled;
                    Helpers.DebugLogger.Log($"[Onboarding] gate check: completed={_obCompleted}, disabled={_obDisabled} → show={!_obCompleted && !_obDisabled}");
                    if (!_obCompleted && !_obDisabled)
                    {
                        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                        {
                            try
                            {
                                var onboarding = new Views.OnboardingWindow(_settings, _loc);
                                onboarding.Activate();
                            }
                            catch (Exception ex)
                            {
                                Helpers.DebugLogger.Log($"[Onboarding] Failed to show: {ex.Message}");
                            }
                        });
                    }

                    // Store 별점 요청 (5회 이상 실행 후 1회만)
                    TryRequestStoreRating();
                };
            }
        }

        #region Sidebar Resize

        private double _sidebarSplitterStartWidth;

        private void RestoreSidebarWidth()
        {
            try
            {
                var appSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (appSettings.Values.TryGetValue("CustomSidebarWidth", out var saved) && saved is double w)
                {
                    // Legacy SidebarBorder is collapsed; keep column at 0 regardless of saved width.
                    SidebarCol.Width = new GridLength(0);
                    _savedSidebarWidth = 0;
                }
            }
            catch { }
        }

        private void SaveSidebarWidth(double width)
        {
            try
            {
                var appSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                appSettings.Values["CustomSidebarWidth"] = width;
            }
            catch { }
        }

        private void OnSidebarSplitterPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement el)
                Helpers.CursorHelper.SetCursor(el, InputSystemCursorShape.SizeWestEast);
        }

        private void OnSidebarSplitterPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement el)
                Helpers.CursorHelper.SetCursor(el, InputSystemCursorShape.Arrow);
        }

        private void OnSidebarSplitterManipulationStarted(object sender, Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
        {
            _sidebarSplitterStartWidth = SidebarCol.Width.Value;
        }

        private void OnSidebarSplitterManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            // Legacy splitter no-op: LumiSidebar has fixed width and the legacy column is
            // suppressed (SidebarBorder.Visibility=Collapsed, SidebarCol.Width=0).
            double newWidth = 0;
            SidebarCol.Width = new GridLength(0);
            _savedSidebarWidth = 0;
        }

        private void OnTabElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            // 리사이클/신규 탭 요소: baseline 고정값 기반 직접 스케일 적용
            // ConditionalWeakTable 의존 제거 — DataTemplate 재활용 시 baseline 오염 방지
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                ApplyScaleToTabElement(args.Element, Helpers.FontScaleService.Instance.Level);

                // 재활용/신규 탭에 Chrome-style 고정 너비 적용 (auto-size 방지)
                if (_calculatedTabWidth > 0 && args.Element is FrameworkElement elem)
                {
                    if (elem is Grid grid)
                        grid.Width = _calculatedTabWidth;
                    else
                        elem.Width = _calculatedTabWidth;
                }
            });
        }

        /// <summary>
        /// 탭 요소에 baseline 고정값 기반 스케일 적용.
        /// DataTemplate: 탭 아이콘 FontIcon=14, 탭 이름 TextBlock=12, 닫기 버튼 FontIcon=9.
        /// ConditionalWeakTable 미사용 → 리사이클 시에도 항상 정확.
        /// </summary>
        private static void ApplyScaleToTabElement(UIElement element, int level)
        {
            // DataTemplate 구조:
            // Grid[Col0: Grid[StackPanel(FontIcon×4, baseline=14), TextBlock(baseline=12)],
            //      Col1: Button > FontIcon(baseline=9)]
            void Traverse(DependencyObject parent, bool insideButton)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is FontIcon fi)
                    {
                        double baseline = insideButton ? 9.0 : 14.0;
                        fi.FontSize = baseline + level;
                    }
                    else if (child is TextBlock tb)
                        tb.FontSize = 12.0 + level;
                    else
                        Traverse(child, insideButton || child is Button);
                }
            }
            Traverse(element, false);
        }

        #endregion Sidebar Resize

        #region Window Placement Persistence

        /// <summary>
        /// 현재 윈도우 위치와 크기를 <see cref="Windows.Storage.ApplicationData.Current.LocalSettings"/>에 저장한다.
        /// 최소화/최대화 상태에서는 저장하지 않는다.
        /// </summary>
        private void SaveWindowPlacement()
        {
            try
            {
                if (IsIconic(_hwnd) || IsZoomed(_hwnd)) return; // 최소화/최대화 상태는 저장 안 함
                if (!GetWindowRect(_hwnd, out var rect)) return;

                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                var composite = new Windows.Storage.ApplicationDataCompositeValue
                {
                    ["X"] = rect.Left,
                    ["Y"] = rect.Top,
                    ["Width"] = rect.Right - rect.Left,
                    ["Height"] = rect.Bottom - rect.Top
                };
                settings.Values["WindowPlacement"] = composite;
                var dpi = Helpers.NativeMethods.GetDpiForWindow(_hwnd);
                Helpers.DebugLogger.Log($"[Window] Saved placement: {rect.Left},{rect.Top} {rect.Right - rect.Left}x{rect.Bottom - rect.Top} (DPI={dpi})");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[Window] SavePlacement error: {ex.Message}");
            }
        }

        /// <summary>
        /// 저장된 윈도우 배치 정보를 복원한다.
        /// 모니터 영역 검증을 통해 창이 화면 밖에 위치하지 않도록 보정하며,
        /// 최소 크기(400×300)를 보장한다.
        /// </summary>
        private void RestoreWindowPlacement()
        {
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (settings.Values["WindowPlacement"] is not Windows.Storage.ApplicationDataCompositeValue composite)
                    return;

                if (composite.TryGetValue("X", out var xObj) && xObj is int x &&
                    composite.TryGetValue("Y", out var yObj) && yObj is int y &&
                    composite.TryGetValue("Width", out var wObj) && wObj is int w &&
                    composite.TryGetValue("Height", out var hObj) && hObj is int h)
                {
                    // 최소 크기 보장
                    if (w < 400) w = 400;
                    if (h < 300) h = 300;

                    // ── 모니터 영역 검증: 저장된 위치가 화면 밖이면 보정 ──
                    // 해상도/모니터 구성이 변경되면 저장된 좌표가 현재 작업영역을 벗어나
                    // 타이틀바가 위쪽으로 잘리거나 모니터 경계에 걸쳐 드래그가 불가능해짐.
                    // → 창 전체가 작업영역 안에 들어오지 않으면 모니터 중앙으로 재배치.
                    var savedRect = new Helpers.NativeMethods.RECT
                    {
                        Left = x,
                        Top = y,
                        Right = x + w,
                        Bottom = y + h
                    };
                    var hMonitor = Helpers.NativeMethods.MonitorFromRect(
                        ref savedRect, Helpers.NativeMethods.MONITOR_DEFAULTTONEAREST);
                    bool centered = false;
                    if (hMonitor != IntPtr.Zero)
                    {
                        var monInfo = new Helpers.NativeMethods.MONITORINFO();
                        monInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Helpers.NativeMethods.MONITORINFO>();
                        if (Helpers.NativeMethods.GetMonitorInfo(hMonitor, ref monInfo))
                        {
                            var work = monInfo.rcWork;
                            int workW = work.Right - work.Left;
                            int workH = work.Bottom - work.Top;

                            // 창 크기가 모니터 작업영역보다 크면 축소
                            bool sizeClamped = false;
                            if (w > workW) { w = workW; sizeClamped = true; }
                            if (h > workH) { h = workH; sizeClamped = true; }

                            // 타이틀바(상단 ~40 DIP)가 작업영역 안에 완전히 들어와야 드래그 가능.
                            // 아래 조건 중 하나라도 어긋나면 사용자가 창을 옮길 수 없으므로 재배치.
                            uint winDpi = Helpers.NativeMethods.GetDpiForWindow(_hwnd);
                            if (winDpi == 0) winDpi = 96;
                            int titleBarPx = (int)Math.Ceiling(40.0 * winDpi / 96.0);

                            bool offScreen =
                                x < work.Left ||                 // 왼쪽 가장자리가 작업영역 밖
                                y < work.Top ||                  // 타이틀바가 위쪽으로 잘림
                                x + w > work.Right ||            // 오른쪽 가장자리가 작업영역 밖
                                y + titleBarPx > work.Bottom;    // 타이틀바가 아래쪽으로 잘림

                            if (offScreen || sizeClamped)
                            {
                                x = work.Left + (workW - w) / 2;
                                y = work.Top + (workH - h) / 2;
                                centered = true;
                                Helpers.DebugLogger.Log(
                                    $"[Window] Saved placement out of bounds (offScreen={offScreen}, sizeClamped={sizeClamped}); " +
                                    $"centering on monitor work area {work.Left},{work.Top} {workW}x{workH}");
                            }
                        }
                    }
                    else
                    {
                        // 작업영역을 찾지 못함 (저장된 좌표가 어떤 모니터에도 속하지 않음)
                        // → primary monitor로 폴백 후 중앙 배치.
                        var primaryRect = new Helpers.NativeMethods.RECT
                        {
                            Left = 0, Top = 0, Right = 1, Bottom = 1
                        };
                        var hPrimary = Helpers.NativeMethods.MonitorFromRect(
                            ref primaryRect, Helpers.NativeMethods.MONITOR_DEFAULTTONEAREST);
                        if (hPrimary != IntPtr.Zero)
                        {
                            var monInfo = new Helpers.NativeMethods.MONITORINFO();
                            monInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Helpers.NativeMethods.MONITORINFO>();
                            if (Helpers.NativeMethods.GetMonitorInfo(hPrimary, ref monInfo))
                            {
                                var work = monInfo.rcWork;
                                int workW = work.Right - work.Left;
                                int workH = work.Bottom - work.Top;
                                if (w > workW) w = workW;
                                if (h > workH) h = workH;
                                x = work.Left + (workW - w) / 2;
                                y = work.Top + (workH - h) / 2;
                                centered = true;
                                Helpers.DebugLogger.Log(
                                    $"[Window] No monitor matched saved rect; centering on primary work area {work.Left},{work.Top} {workW}x{workH}");
                            }
                        }
                    }
                    _ = centered; // (디버깅 시 추적용)

                    // Win32 SetWindowPos 사용 (물리 픽셀 직접 지정)
                    // AppWindow.MoveAndResize는 DPI 이중적용 버그 있음
                    Helpers.NativeMethods.SetWindowPos(
                        _hwnd, Helpers.NativeMethods.HWND_TOP,
                        x, y, w, h,
                        Helpers.NativeMethods.SWP_NOZORDER | Helpers.NativeMethods.SWP_NOACTIVATE);

                    // 복원 후 실제 크기 확인
                    GetWindowRect(_hwnd, out var verifyRect);
                    var dpi = Helpers.NativeMethods.GetDpiForWindow(_hwnd);
                    Helpers.DebugLogger.Log($"[Window] Restored target: {x},{y} {w}x{h} | actual: {verifyRect.Left},{verifyRect.Top} {verifyRect.Right - verifyRect.Left}x{verifyRect.Bottom - verifyRect.Top} (DPI={dpi})");
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[Window] RestorePlacement error: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 윈도우 닫힘 이벤트 핸들러.
        /// 윈도우 배치 저장, 세션 탭 저장, 이벤트 구독 해제,
        /// FileSystemWatcher 정리, Win32 서브클래스 제거, 미리보기 서비스 정리 등
        /// 모든 리소스 해제 및 종료 작업을 수행한다.
        /// </summary>
        private void OnClosed(object sender, WindowEventArgs args)
        {
            try
            {
                Helpers.DebugLogger.Log("[MainWindow.OnClosed] Starting cleanup...");
                try { Sentry.SentrySdk.AddBreadcrumb($"Window closing: tearOff={_isTearOffWindow}, tabs={ViewModel?.Tabs?.Count ?? 0}, forceClose={_forceClose}", "window.close"); } catch { }

                // Stage S-3.32: close any open SettingsWindow before tearing
                // down. Without this the SettingsWindow can outlive its parent,
                // leaving a stranded settings dialog with no main window
                // behind it.
                try { Services.SettingsWindowHost.CloseIfOpen(); } catch { }

                // STEP 0: Block all queued DispatcherQueue callbacks and async continuations
                _isClosed = true;

                // STEP 0.1: 드래그 타이머 즉시 중지 (타이머 콜백이 teardown 중 UI 접근 방지)
                try { _tearOffDragTimer?.Stop(); _tearOffDragTimer = null; } catch { }

                // STEP 0.2: NonClientInputSource 영역 즉시 초기화 (WinUI teardown 중 stowed exception 방지)
                try
                {
                    if (ExtendsContentIntoTitleBar)
                    {
                        var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(this.AppWindow.Id);
                        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, Array.Empty<Windows.Graphics.RectInt32>());
                    }
                }
                catch { }

                // STEP 0.3: WS_EX_LAYERED 즉시 해제 (DirectComposition 충돌 방지)
                try { SetWindowOpacity(_hwnd, 255); } catch { }

                // Quick Look 윈도우 닫기
                CloseQuickLookWindow();

                // Save window position/size (skip for tear-off windows)
                if (!_isTearOffWindow && _settings.RememberWindowPosition)
                    SaveWindowPlacement();

                // Save tab state for session restore (skip for tear-off windows)
                if (!_isTearOffWindow)
                {
                    ViewModel.SaveActiveTabState();
                    ViewModel.SaveTabsToSettings();
                    SaveShelfToSettings();
                }

                // FileSystemWatcher 정리
                _watcherService?.StopAll();
                _networkShortcutsWatcher?.Dispose();
                _networkShortcutsWatcher = null;

                // Unsubscribe settings
                _settings.SettingChanged -= OnSettingChanged;

                // Unsubscribe file open toast
                try
                {
                    var shellService = App.Current.Services.GetRequiredService<ShellService>();
                    shellService.FileOpening -= OnShellFileOpening;
                }
                catch { }

                // STEP 1: Suppress ViewModel notifications FIRST (prevents PropertyChanged
                // from reaching UI during teardown — the primary crash cause).
                ViewModel?.Explorer?.Cleanup();       // Left pane
                ViewModel?.RightExplorer?.Cleanup();   // Right pane

                // STEP 2: Unsubscribe MainWindow event handlers BEFORE ViewModel.Cleanup()
                // so collection Clear() notifications don't reach MainWindow handlers.
                if (_subscribedLeftExplorer != null)
                {
                    _subscribedLeftExplorer.Columns.CollectionChanged -= OnColumnsChanged;
                    _subscribedLeftExplorer.Columns.CollectionChanged -= OnLeftColumnsChangedForPreview;
                    _subscribedLeftExplorer.NavigationError -= OnNavigationError;
                    _subscribedLeftExplorer.PathHighlightsUpdated -= OnPathHighlightsUpdated;
                    _subscribedLeftExplorer = null;
                }
                // v1.4.19: BringIntoView 핸들러 해제
                try { MillerScrollViewer.BringIntoViewRequested -= OnMillerBringIntoViewRequested; } catch { }
                try { MillerScrollViewerRight.BringIntoViewRequested -= OnMillerBringIntoViewRequested; } catch { }
                // v1.4.19: 정적 forward 이벤트 해제 (메모리 누수 방지)
                try { ViewModels.ExplorerViewModel.AnyBeforeReplaceLastColumn -= OnAnyBeforeReplaceLastColumn; } catch { }
                try { ViewModels.ExplorerViewModel.AnyAfterReplaceLastColumn -= OnAnyAfterReplaceLastColumn; } catch { }
                if (ViewModel?.RightExplorer != null)
                {
                    ViewModel.RightExplorer.Columns.CollectionChanged -= OnRightColumnsChanged;
                    ViewModel.RightExplorer.NavigationError -= OnNavigationError;
                }
                if (ViewModel != null)
                {
                    ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                    ViewModel.PropertyChanged -= OnViewModelPropertyChangedForPreview;
                }

                // Per-Tab Miller Panels 정리
                foreach (var kvp in _tabMillerPanels)
                {
                    kvp.Value.items.ItemsSource = null;
                }
                _tabMillerPanels.Clear();

                // Rubber-band selection helpers 정리
                foreach (var kvp in _rubberBandHelpers)
                    try { kvp.Value.Detach(); } catch (Exception ex) { Helpers.DebugLogger.LogCrash("OnClosed.RubberBand.Detach", ex); }
                _rubberBandHelpers.Clear();

                // Unsubscribe preview column change handlers
                // LeftExplorer preview는 _subscribedLeftExplorer에서 이미 해제됨
                if (ViewModel?.RightExplorer != null)
                    ViewModel.RightExplorer.Columns.CollectionChanged -= OnRightColumnsChangedForPreview;

                // STEP 2.5: Cleanup preview panels (stop media, dispose ViewModels)
                try { LeftPreviewPanel?.Cleanup(); } catch { }
                try { RightPreviewPanel?.Cleanup(); } catch { }
                UnsubscribePreviewSelection(isLeft: true);
                UnsubscribePreviewSelection(isLeft: false);

                // Cleanup Git status bars
                try { CleanupGitStatusBars(); } catch { }

                // Save preview panel widths
                try
                {
                    double leftW = LeftPreviewCol.Width.Value;
                    double rightW = RightPreviewCol.Width.Value;
                    ViewModel?.SavePreviewWidths(leftW, rightW);
                }
                catch { }

                // STEP 3: Per-tab Details/List/Icon 인스턴스 전체 정리
                foreach (var kvp in _tabDetailsPanels)
                    try { kvp.Value?.Cleanup(); } catch { }
                _tabDetailsPanels.Clear();

                foreach (var kvp in _tabListPanels)
                    try { kvp.Value?.Cleanup(); } catch { }
                _tabListPanels.Clear();

                foreach (var kvp in _tabIconPanels)
                    try { kvp.Value?.Cleanup(); } catch { }
                _tabIconPanels.Clear();

                try { HomeView?.Cleanup(); } catch { }
                try { DetailsViewRight?.Cleanup(); } catch { }
                try { IconViewRight?.Cleanup(); } catch { }

                // Disconnect sidebar bindings
                try
                {
                    FavoritesTreeView.RootNodes.Clear();
                    ViewModel.Favorites.CollectionChanged -= OnFavoritesCollectionChanged;
                }
                catch { /* ignore */ }

                // STEP 4: NOW safe to clear collections — UI bindings disconnected
                ViewModel?.Cleanup();            // Save state, cancel ops, clear collections

                // STEP 5: Stop timer and remove keyboard handlers
                try
                {
                    if (_typeAheadTimer != null)
                    {
                        _typeAheadTimer.Stop();
                        _typeAheadTimer = null;
                    }
                    if (this.Content != null)
                    {
                        this.Content.RemoveHandler(UIElement.KeyDownEvent, (KeyEventHandler)OnGlobalKeyDown);
                        this.Content.RemoveHandler(UIElement.PointerPressedEvent, (PointerEventHandler)OnGlobalPointerPressed);
                        this.Content.RemoveHandler(UIElement.PointerWheelChangedEvent, (PointerEventHandler)OnGlobalPointerWheelChanged);
                    }
                    if (MillerColumnsControl != null)
                    {
                        MillerColumnsControl.RemoveHandler(UIElement.KeyDownEvent, (KeyEventHandler)OnMillerKeyDown);
                        MillerColumnsControl.RemoveHandler(UIElement.CharacterReceivedEvent,
                            (Windows.Foundation.TypedEventHandler<UIElement, Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs>)OnMillerCharacterReceived);
                    }
                    if (MillerColumnsControlRight != null)
                    {
                        MillerColumnsControlRight.RemoveHandler(UIElement.KeyDownEvent, (KeyEventHandler)OnMillerKeyDown);
                        MillerColumnsControlRight.RemoveHandler(UIElement.CharacterReceivedEvent,
                            (Windows.Foundation.TypedEventHandler<UIElement, Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs>)OnMillerCharacterReceived);
                    }
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[MainWindow.OnClosed] STEP 5 error: {ex.Message}");
                }

                // STEP 6: Remove window subclass for device change
                try
                {
                    if (_subclassProc != null)
                    {
                        RemoveWindowSubclass(_hwnd, _subclassProc, IntPtr.Zero);
                    }
                    if (_deviceChangeDebounceTimer != null)
                    {
                        _deviceChangeDebounceTimer.Stop();
                        _deviceChangeDebounceTimer = null;
                    }
                    if (_drivePollingTimer != null)
                    {
                        _drivePollingTimer.Stop();
                        _drivePollingTimer.Tick -= OnDrivePollingTick;
                        _drivePollingTimer = null;
                    }
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[MainWindow.OnClosed] STEP 6 error: {ex.Message}");
                }

                // (NonClientInputSource, WS_EX_LAYERED은 STEP 0.2/0.3에서 이미 정리됨)

                Helpers.DebugLogger.Log("[MainWindow.OnClosed] Cleanup complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow.OnClosed] Error during close: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainWindow.OnClosed] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // CRITICAL: Always unregister window to ensure app exit.
                // Previously inside try block — if any cleanup step threw,
                // UnregisterWindow was skipped → Environment.Exit never called → process hung.
                try { App.Current.UnregisterWindow(this); } catch { }
            }
        }

        /// <summary>
        /// Win32 subclass procedure to intercept WM_DEVICECHANGE for USB hotplug detection.
        /// </summary>
        private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            if (uMsg == WM_DEVICECHANGE && wParam == (IntPtr)DBT_DEVNODES_CHANGED)
            {
                // Debounce: multiple WM_DEVICECHANGE messages fire in quick succession
                _deviceChangeDebounceTimer?.Stop();
                _deviceChangeDebounceTimer?.Start();
                Helpers.DebugLogger.Log("[MainWindow] WM_DEVICECHANGE: Device change detected");
            }
            else if (uMsg == WM_DPICHANGED)
            {
                // S-3.34 재시도: 듀얼 모니터에서 DPI 다른 모니터로 옮겼을 때 발생.
                // 1) lParam의 권장 RECT로 SetWindowPos — Windows 가이드 준수
                // 2) DispatcherQueue로 ApplyRoundedWindowRegion 재호출 → 새 DPI scale로 radius 재계산
                try
                {
                    if (lParam != IntPtr.Zero)
                    {
                        var suggested = System.Runtime.InteropServices.Marshal.PtrToStructure<Helpers.NativeMethods.RECT>(lParam);
                        Helpers.NativeMethods.SetWindowPos(
                            hWnd, IntPtr.Zero,
                            suggested.Left, suggested.Top,
                            suggested.Right - suggested.Left,
                            suggested.Bottom - suggested.Top,
                            Helpers.NativeMethods.SWP_NOZORDER | Helpers.NativeMethods.SWP_NOACTIVATE);
                    }
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                        () => { if (!_isClosed) ApplyRoundedWindowRegion(); });
                    Helpers.DebugLogger.Log($"[MainWindow] WM_DPICHANGED: dpi={(int)wParam & 0xFFFF}");
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[MainWindow] WM_DPICHANGED error: {ex.Message}");
                }
                return IntPtr.Zero;
            }
            else if (uMsg == WM_GETMINMAXINFO)
            {
                // Borderless 윈도우(SetBorderAndTitleBar(false,false))는 OS가 caption/border를
                // 갖고 있다고 판단해서, 최대화 시 ptMaxPosition을 음수로 보정해 화면 밖으로
                // 밀어버린다. 그 결과 작업표시줄 영역까지 덮어써 가려지는 문제가 발생한다.
                // → MONITORINFO.rcWork(작업영역)에 맞춰 직접 ptMaxPosition / ptMaxSize를 잡아준다.
                try
                {
                    IntPtr hMonitor = MonitorFromWindow(hWnd, Helpers.NativeMethods.MONITOR_DEFAULTTONEAREST);
                    if (hMonitor != IntPtr.Zero)
                    {
                        var monInfo = new Helpers.NativeMethods.MONITORINFO();
                        monInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Helpers.NativeMethods.MONITORINFO>();
                        if (Helpers.NativeMethods.GetMonitorInfo(hMonitor, ref monInfo))
                        {
                            var mmi = System.Runtime.InteropServices.Marshal.PtrToStructure<MINMAXINFO>(lParam);
                            // rcWork: 작업표시줄을 제외한 영역 (DPI 고려된 물리 픽셀)
                            mmi.ptMaxPosition.X = monInfo.rcWork.Left - monInfo.rcMonitor.Left;
                            mmi.ptMaxPosition.Y = monInfo.rcWork.Top  - monInfo.rcMonitor.Top;
                            mmi.ptMaxSize.X     = monInfo.rcWork.Right  - monInfo.rcWork.Left;
                            mmi.ptMaxSize.Y     = monInfo.rcWork.Bottom - monInfo.rcWork.Top;
                            mmi.ptMaxTrackSize.X = mmi.ptMaxSize.X;
                            mmi.ptMaxTrackSize.Y = mmi.ptMaxSize.Y;
                            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, false);
                            return IntPtr.Zero; // 처리 완료
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[MainWindow] WM_GETMINMAXINFO error: {ex.Message}");
                }
            }
            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        /// <summary>
        /// Lightweight poll: compare drive letters to detect virtual drive mount/unmount.
        /// </summary>
        private void OnDrivePollingTick(object? sender, object e)
        {
            if (_isClosed) return;
            try
            {
                var current = new HashSet<char>(
                    System.IO.DriveInfo.GetDrives().Select(d => d.Name[0]));
                if (!current.SetEquals(_lastKnownDriveLetters))
                {
                    Helpers.DebugLogger.Log($"[MainWindow] Drive poll: letters changed ({string.Join(",", _lastKnownDriveLetters)} → {string.Join(",", current)})");
                    _lastKnownDriveLetters = current;
                    ViewModel.RefreshDrives();
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] Drive poll error: {ex.Message}");
            }
        }

        // =================================================================
        //  Settings
        // =================================================================

        // 커스텀 테마 목록 — Stage S-3.32: 비활성화 (Light/Dark만 지원).
        // HashSet은 호환성을 위해 유지하되 빈 상태. 레거시 settings.json에서
        // "dracula" 등이 와도 _customThemes.Contains() == false 가 되어
        // ApplyCustomThemeOverrides가 dict 제거 분기를 타고 기본 테마로 fallback.
        internal static readonly HashSet<string> _customThemes = new();















        // =================================================================
        //  Auto Scroll
        // =================================================================

        /// <summary>
        /// 좌측 탐색기의 Miller Column 컬렉션 변경 시 호출.
        /// 새 컬럼 추가/교체 시 마지막 컬럼으로 자동 스크롤하고,
        /// 체크박스 모드와 밀도 설정을 새 컬럼에 적용한다.
        /// 탭 전환 중에는 성능 최적화를 위해 스킵한다.
        /// </summary>
        private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Helpers.DebugLogger.Log($"[OnColumnsChanged] Action={e.Action}, ViewMode={ViewModel?.CurrentViewMode}, IsSwitchingTab={ViewModel?.IsSwitchingTab}, LeftColumns={ViewModel?.LeftExplorer?.Columns?.Count}, MillerItemsSource={MillerColumnsControl.ItemsSource != null}");

            // 탭 전환 중에는 ScrollToLastColumn + UpdateLayout 비용 회피
            if (ViewModel?.IsSwitchingTab == true) return;

            // FileWatcher는 모든 뷰 모드에서 필요
            UpdateFileSystemWatcherPaths();

            // Miller Columns가 아닌 뷰 모드에서는 ItemsControl이 unloaded 상태이므로
            // ContainerFromIndex/ScrollToLastColumn이 AccessViolationException을 일으킬 수 있음
            if (ViewModel == null || ViewModel.CurrentViewMode != ViewMode.MillerColumns) return;

            // v1.4.19: 마지막 컬럼 RemoveAt+Delay+Insert 사이클 동안에는 ScrollToLastColumn /
            // 슬라이드-인 둘 다 skip. spacer Border가 ExtentWidth를 보존하므로 자동 좌측 클램프
            // 위험이 없어 ScrollTo를 호출하지 않아도 위치가 유지됨.
            // → ↑/↓ 토글마다 ChangeView 애니메이션이 좌→우로 휙 이동하는 현상 차단.
            // 깊이 진입(Replace 아님) 시는 정상 ScrollTo + 슬라이드-인.
            bool isReplacingLeft = ViewModel.LeftExplorer?.IsReplacingLastColumn == true;
            Helpers.DebugLogger.Log($"[Diag-Miller] L:ColumnsChanged action={e.Action} isReplacing={isReplacingLeft} {DiagSv(GetActiveMillerScrollViewer())}");

            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (!isReplacingLeft)
                {
                    Helpers.DebugLogger.Log($"[OnColumnsChanged] ScrollToLastColumn for left explorer");
                    ScrollToLastColumn(ViewModel.LeftExplorer, GetActiveMillerScrollViewer());
                }
                if (_millerSelectionMode != ListViewSelectionMode.Extended)
                {
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                        () => ApplyCheckboxToItemsControl(GetActiveMillerColumnsControl(), _millerSelectionMode));
                }
            }

            // Column slide-in animation: only for Add when not the root column,
            // and only when this is a genuine depth change (not a Replace cycle).
            if (e.Action == NotifyCollectionChangedAction.Add &&
                ViewModel.LeftExplorer.Columns.Count > 1 &&
                !isReplacingLeft)
            {
                Helpers.DebugLogger.Log($"[OnColumnsChanged] PrepareAndAnimateNewColumn for left");
                PrepareAndAnimateNewColumn(GetActiveMillerColumnsControl());
            }
        }

        /// <summary>
        /// 우측 탐색기의 Miller Column 컬렉션 변경 시 호출.
        /// 새 컬럼 추가/교체 시 마지막 컬럼으로 자동 스크롤하고 슬라이드 애니메이션을 적용한다.
        /// </summary>
        private void OnRightColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Helpers.DebugLogger.Log($"[OnRightColumnsChanged] Action={e.Action}, IsSplit={ViewModel.IsSplitViewEnabled}, RightViewMode={ViewModel.RightViewMode}, RightColumns={ViewModel.RightExplorer?.Columns?.Count}");

            if (!ViewModel.IsSplitViewEnabled) return;

            // 우측이 Miller 모드가 아니면 ItemsControl이 unloaded 상태이므로
            // ContainerFromIndex/ScrollToLastColumn이 AccessViolation을 일으킬 수 있음
            if (ViewModel.RightViewMode != ViewMode.MillerColumns) return;

            // v1.4.19: 좌측과 동일 — Replace 사이클 동안 ScrollTo / 슬라이드-인 둘 다 skip.
            // spacer Border가 ExtentWidth를 보존하므로 좌측 클램프 위험 없음.
            bool isReplacingRight = ViewModel.RightExplorer?.IsReplacingLastColumn == true;

            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (!isReplacingRight)
                {
                    Helpers.DebugLogger.Log($"[OnRightColumnsChanged] ScrollToLastColumn for right explorer");
                    ScrollToLastColumn(ViewModel.RightExplorer, MillerScrollViewerRight);
                }
                if (_millerSelectionMode != ListViewSelectionMode.Extended)
                {
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                        () => ApplyCheckboxToItemsControl(MillerColumnsControlRight, _millerSelectionMode));
                }
            }

            // Column slide-in animation for right pane (skip during Replace cycle)
            if (e.Action == NotifyCollectionChangedAction.Add &&
                ViewModel.RightExplorer.Columns.Count > 1 &&
                !isReplacingRight)
            {
                Helpers.DebugLogger.Log($"[OnRightColumnsChanged] PrepareAndAnimateNewColumn for right");
                PrepareAndAnimateNewColumn(MillerColumnsControlRight);
            }
        }

        // =================================================================
        //  v1.4.19: ExtentWidth 박동 + 자동 BringIntoView 차단을 위한 spacer + 우측 끝 즉시 정렬
        // =================================================================
        // 1) RemoveAt 직전에 spacer를 마지막 컬럼 폭만큼 펼치고, Insert 직후 0으로 접는다.
        //    → ItemsControl 폭 변동을 spacer가 흡수하여 ScrollViewer ExtentWidth 일정 유지
        //    → thumb 박동·자동 클램프 차단.
        // 2) AfterReplace 단계에서 ExtentWidth - ViewportWidth로 즉시 정렬(disableAnimation=true).
        //    형제 폴더 토글은 본질적으로 마지막 컬럼이 보이는 상태에서 일어나므로 우측 끝
        //    정렬이 자연스럽다. ChangeView를 즉시 + Low 큐 두 번 호출하여 WinUI 자동
        //    BringIntoView가 layout pass 이후에 끼어드는 케이스도 무력화.

        private void OnLeftBeforeReplaceLastColumn()
        {
            var sv = GetActiveMillerScrollViewer();
            Helpers.DebugLogger.Log($"[Diag-Miller] L:BeforeReplace.entry {DiagSv(sv)} spacer={MillerColumnSpacerLeft?.Width:F1}");
            SetMillerSpacerWidth(MillerColumnSpacerLeft, GetActiveMillerColumnsControl());
            Helpers.DebugLogger.Log($"[Diag-Miller] L:BeforeReplace.exit  {DiagSv(sv)} spacer={MillerColumnSpacerLeft?.Width:F1}");
        }
        private void OnLeftAfterReplaceLastColumn(bool insertedOk)
        {
            var sv = GetActiveMillerScrollViewer();
            Helpers.DebugLogger.Log($"[Diag-Miller] L:AfterReplace.entry insertedOk={insertedOk} {DiagSv(sv)} spacer={MillerColumnSpacerLeft?.Width:F1}");

            // v1.4.19: insertedOk=false (RemoveAt 후 빠른 cancel로 Insert 미실행) 시 spacer를
            // 그대로 유지하여 ItemsControl 폭 손실을 보상 → ExtentWidth 박동 + HO 자동 클램프
            // 좌측 점프 차단. 다음 BeforeReplace 또는 새 ScrollToLastColumn 호출 시 자연 정리.
            if (!insertedOk)
            {
                Helpers.DebugLogger.Log($"[Diag-Miller] L:AfterReplace.skip-spacer-reset (Insert canceled, spacer 유지)");
                return;
            }

            try { MillerColumnSpacerLeft.Width = 0; } catch { }
            Helpers.DebugLogger.Log($"[Diag-Miller] L:AfterReplace.spacer0 {DiagSv(sv)} spacer={MillerColumnSpacerLeft?.Width:F1}");
            if (_isClosed || ViewModel?.LeftExplorer == null) return;
            if (sv == null) return;
            // Insert 직후 새 컨테이너가 measure 전이라도 GetTotalColumnsActualWidth가 ColumnWidth
            // 폴백으로 정확한 totalWidth 계산. ScrollToLastColumn은 자체 Low queue 큐잉.
            // 두 번 호출하여 다음 layout pass 후에도 위치가 흔들리지 않도록 보강.
            ScrollToLastColumn(ViewModel.LeftExplorer, sv, disableAnimation: true);
            sv.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                () => {
                    if (_isClosed || ViewModel?.LeftExplorer == null) return;
                    Helpers.DebugLogger.Log($"[Diag-Miller] L:AfterReplace.lowQueueSync.before {DiagSv(sv)} spacer={MillerColumnSpacerLeft?.Width:F1}");
                    ScrollToLastColumnSync(ViewModel.LeftExplorer, sv, disableAnimation: true);
                    Helpers.DebugLogger.Log($"[Diag-Miller] L:AfterReplace.lowQueueSync.after  {DiagSv(sv)} spacer={MillerColumnSpacerLeft?.Width:F1}");
                });
        }

        private void OnRightBeforeReplaceLastColumn() => SetMillerSpacerWidth(MillerColumnSpacerRight, MillerColumnsControlRight);
        private void OnRightAfterReplaceLastColumn(bool insertedOk)
        {
            // v1.4.19: 좌측과 동일 - Insert 미실행 시 spacer 유지
            if (!insertedOk) return;
            try { MillerColumnSpacerRight.Width = 0; } catch { }
            if (_isClosed || ViewModel?.RightExplorer == null) return;
            ScrollToLastColumn(ViewModel.RightExplorer, MillerScrollViewerRight, disableAnimation: true);
            MillerScrollViewerRight.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                () => { if (!_isClosed && ViewModel?.RightExplorer != null) ScrollToLastColumnSync(ViewModel.RightExplorer, MillerScrollViewerRight, disableAnimation: true); });
        }

        /// <summary>v1.4.19 진단 로그용: ScrollViewer 상태 한 줄 포맷.</summary>
        private static string DiagSv(ScrollViewer? sv)
        {
            if (sv == null) return "sv=null";
            try { return $"HO={sv.HorizontalOffset:F1} Ext={sv.ExtentWidth:F1} VP={sv.ViewportWidth:F1} ScrW={sv.ScrollableWidth:F1}"; }
            catch { return "sv=err"; }
        }

        /// <summary>
        /// v1.4.19: 정적 forward 이벤트로부터 들어온 sender를 ViewModel.Left/RightExplorer 와
        /// 비교해 좌/우 spacer 핸들러에 라우팅. 인스턴스 단위 구독이 _leftExplorer 직접 할당으로
        /// 끊어지는 케이스를 모두 cover.
        /// </summary>
        private void OnAnyBeforeReplaceLastColumn(ViewModels.ExplorerViewModel sender)
        {
            if (_isClosed || ViewModel == null) return;
            Helpers.DebugLogger.Log($"[Diag-Miller] L:AnyBefore sender={sender.GetHashCode():X} Left={ViewModel.LeftExplorer?.GetHashCode():X} Right={ViewModel.RightExplorer?.GetHashCode():X}");
            if (ReferenceEquals(sender, ViewModel.LeftExplorer)) OnLeftBeforeReplaceLastColumn();
            else if (ReferenceEquals(sender, ViewModel.RightExplorer)) OnRightBeforeReplaceLastColumn();
        }

        private void OnAnyAfterReplaceLastColumn(ViewModels.ExplorerViewModel sender, bool insertedOk)
        {
            if (_isClosed || ViewModel == null) return;
            if (ReferenceEquals(sender, ViewModel.LeftExplorer)) OnLeftAfterReplaceLastColumn(insertedOk);
            else if (ReferenceEquals(sender, ViewModel.RightExplorer)) OnRightAfterReplaceLastColumn(insertedOk);
        }

        /// <summary>
        /// spacer Border의 폭을 ItemsControl의 마지막 컬럼 컨테이너 ActualWidth로 설정.
        /// 컨테이너가 아직 measure 전이거나 가져올 수 없으면 ColumnWidth(220) 폴백.
        /// </summary>
        private void SetMillerSpacerWidth(Border spacer, ItemsControl? control)
        {
            if (spacer == null) return;
            try
            {
                double w = ColumnWidth;
                if (control != null && control.Items != null && control.Items.Count > 0)
                {
                    int lastIdx = control.Items.Count - 1;
                    if (control.ContainerFromIndex(lastIdx) is FrameworkElement last && last.ActualWidth > 0)
                    {
                        w = last.ActualWidth;
                    }
                }
                spacer.Width = w;
            }
            catch { /* defensive: spacer 조정 실패는 무시 (회귀 위험 0 우선) */ }
        }

        /// <summary>
        /// v1.4.19: Miller ScrollViewer의 자식 컨트롤(ListView/SelectedItem/포커스 등)이 발생시키는
        /// 자동 BringIntoView 요청을 차단한다. 가로 스크롤은 ScrollToLastColumn 명시 호출로만 제어.
        /// </summary>
        private void OnMillerBringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
        {
            // BringIntoView가 ScrollViewer 자체를 가로로 흔드는 경우만 차단 (Vertical은 Disabled라 무영향).
            args.Handled = true;
        }

        // =================================================================
        //  밀러컬럼 뷰포트 리사이즈 → 마지막 컬럼 자동 스크롤
        // =================================================================

        /// <summary>
        /// 좌측 Miller 컬럼 ScrollViewer의 뷰포트 크기 변경 시 마지막 컬럼으로 자동 스크롤.
        /// 너비 변경만 처리하고 높이 변경은 무시한다.
        /// </summary>
        private void OnMillerScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isClosed || ViewModel?.LeftExplorer == null) return;
            // 뷰포트 너비가 변경되었을 때만 (높이 변경은 무시)
            if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) < 1) return;
            // 좌측 패인 전용 핸들러: 활성 탭의 좌측 ScrollViewer와 sender를 비교.
            // GetActiveMillerScrollViewer()는 Split View에서 우측 패인을 반환할 수 있으므로 사용 불가.
            ScrollViewer leftScrollViewer;
            if (_activeMillerTabId != null && _tabMillerPanels.TryGetValue(_activeMillerTabId, out var panel))
                leftScrollViewer = panel.scroller;
            else
                leftScrollViewer = MillerScrollViewer;
            if (sender == leftScrollViewer)
                ScrollToLastColumn(ViewModel.LeftExplorer, leftScrollViewer);
        }

        /// <summary>
        /// 우측 Miller 컬럼 ScrollViewer의 뷰포트 크기 변경 시 마지막 컬럼으로 자동 스크롤.
        /// 너비 변경만 처리하고 높이 변경은 무시한다.
        /// </summary>
        private void OnMillerScrollViewerRightSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isClosed || ViewModel?.RightExplorer == null) return;
            if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) < 1) return;
            ScrollToLastColumn(ViewModel.RightExplorer, MillerScrollViewerRight);
        }

        /// <summary>
        /// Force layout so the new column container exists, hide it immediately
        /// (preventing the 1-frame flash), then start animation on next frame.
        /// </summary>
        private void PrepareAndAnimateNewColumn(ItemsControl control)
        {
            if (control == null) { Helpers.DebugLogger.Log("[PrepareAndAnimate] control is null"); return; }
            var lastIndex = control.Items.Count - 1;
            if (lastIndex < 0) { Helpers.DebugLogger.Log("[PrepareAndAnimate] no items"); return; }

            Helpers.DebugLogger.Log($"[PrepareAndAnimate] lastIndex={lastIndex}, control={control.Name}, IsLoaded={control.IsLoaded}");

            try
            {
                var container = control.ContainerFromIndex(lastIndex);
                Helpers.DebugLogger.Log($"[PrepareAndAnimate] ContainerFromIndex({lastIndex})={container?.GetType().Name ?? "null"}");
                if (container is UIElement element)
                {
                    HideAndAnimateColumn(element);
                    return;
                }
            }
            catch (System.Runtime.InteropServices.COMException ex) { Helpers.DebugLogger.Log($"[PrepareAndAnimate] COMException: {ex.Message}"); return; }
            catch (AccessViolationException ex) { Helpers.DebugLogger.Log($"[PrepareAndAnimate] AccessViolation: {ex.Message}"); return; }

            // 컨테이너 미생성 시 → 다음 프레임에서 재시도
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (_isClosed) return;
                try
                {
                    var retryContainer = control.ContainerFromIndex(lastIndex);
                    if (retryContainer is UIElement retryElement)
                    {
                        HideAndAnimateColumn(retryElement);
                    }
                }
                catch (System.Runtime.InteropServices.COMException) { }
                catch (AccessViolationException) { }
            });
        }

        /// <summary>
        /// 새 컬럼 요소를 즉시 숨긴 뒤 다음 프레임에서 슬라이드-인 애니메이션을 시작한다.
        /// AnimationsEnabled=OFF 시 Opacity=0 설정 자체를 스킵하여 컬럼이 기본 상태로 즉시 표시되도록 한다.
        /// </summary>
        private void HideAndAnimateColumn(UIElement element)
        {
            // 애니메이션 OFF: 슬라이드/페이드 전 과정 스킵 — 컬럼은 기본 상태(Opacity=1)로 즉시 노출
            if (!_settings.AnimationsEnabled) return;

            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Opacity = 0f;

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (_isClosed) return;
                AnimateColumnEntrance(element);
            });
        }

        /// <summary>
        /// Smooth slide-in animation for new Miller columns.
        /// Spring-based Translation + Opacity (Apple Finder style).
        /// </summary>
        private static void AnimateColumnEntrance(UIElement element)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            // Clear any leftover clip from previous animation style
            visual.Clip = null;

            // Enable Translation property (layout-independent visual offset)
            ElementCompositionPreview.SetIsTranslationEnabled(element, true);
            visual.Properties.InsertVector3("Translation", new Vector3(30f, 0f, 0f));
            visual.Opacity = 0f;

            // Spring slide: 30px from right → final position (Apple-style natural motion)
            var slide = compositor.CreateSpringVector3Animation();
            slide.FinalValue = Vector3.Zero;
            slide.InitialValue = new Vector3(30f, 0f, 0f);
            slide.DampingRatio = 0.82f;
            slide.Period = TimeSpan.FromMilliseconds(50);

            // Fade: fast resolve at ~40% so content is readable quickly
            var easing = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.0f, 0.0f), new Vector2(0.2f, 1.0f));
            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0.4f, 1f, easing);
            fade.Duration = TimeSpan.FromMilliseconds(200);

            // Scoped batch to ensure clean final state
            var batch = compositor.CreateScopedBatch(
                Microsoft.UI.Composition.CompositionBatchTypes.Animation);

            visual.StartAnimation("Translation", slide);
            visual.StartAnimation("Opacity", fade);

            batch.End();
            batch.Completed += (_, _) =>
            {
                visual.Properties.InsertVector3("Translation", Vector3.Zero);
                visual.Opacity = 1f;
            };
        }

        // =================================================================
        //  FileSystemWatcher: 자동 새로고침
        // =================================================================

        /// <summary>
        /// 앱 실행 횟수가 기준 이상이면 Store 별점 요청 다이얼로그를 1회 표시한다.
        /// 실패해도 앱 동작에 영향 없음 (전체 try-catch 방어).
        /// </summary>
        private void TryRequestStoreRating()
        {
            if (_settings.RatingCompleted || _settings.AppLaunchCount < 10)
                return;

            // 최초 실행 날짜 기록 (이 기능이 추가된 버전부터 카운트)
            var firstLaunch = _settings.Get("FirstLaunchDate", "");
            if (string.IsNullOrEmpty(firstLaunch))
            {
                _settings.Set("FirstLaunchDate", DateTime.UtcNow.ToString("o"));
                return;
            }

            // 설치 후 7일 미경과 시 skip
            if (DateTime.TryParse(firstLaunch, null, System.Globalization.DateTimeStyles.RoundtripKind, out var firstDate)
                && (DateTime.UtcNow - firstDate).TotalDays < 7)
                return;

            Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                _ = RequestStoreRatingAsync();
            });
        }

        private async Task RequestStoreRatingAsync()
        {
            try
            {
                // Store 서명 체크 — 비-Store 환경에서 StoreContext API 호출 시
                // Access Violation(0xC0000005) 네이티브 크래시 발생하며 try-catch로 잡을 수 없음
                if (!IsStoreInstalled())
                {
                    DebugLogger.Log($"[Rating] Not Store-installed, skipping (LaunchCount={_settings.AppLaunchCount})");
                    return;
                }

                var storeContext = StoreContext.GetDefault();
                WinRT.Interop.InitializeWithWindow.Initialize(storeContext, _hwnd);

                var result = await storeContext.RequestRateAndReviewAppAsync();
                DebugLogger.Log($"[Rating] Result: {result.Status}");
                if (result.Status == StoreRateAndReviewStatus.Succeeded
                    || result.Status == StoreRateAndReviewStatus.CanceledByUser)
                {
                    _settings.RatingCompleted = true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[Rating] Store rating request failed: {ex.Message}");
                _settings.RatingCompleted = true;
            }
        }

        /// <summary>휴지통 관련 shell 인자인지 판별.</summary>
        private static bool IsRecycleBinArgument(string? arg)
        {
            if (string.IsNullOrEmpty(arg)) return false;
            return arg.Equals("shell:RecycleBinFolder", StringComparison.OrdinalIgnoreCase)
                || arg.Contains("{645FF040-5081-101B-9F08-00AA002F954E}", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>This PC (내 PC) CLSID인지 판별. LumiFiles 홈 화면으로 매핑.</summary>
        private static bool IsThisPCArgument(string? arg)
        {
            if (string.IsNullOrEmpty(arg)) return false;
            return arg.Contains("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// shell: 가상 폴더 또는 CLSID 경로를 감지하여 explorer.exe에 위임.
        /// 제어판, 네트워크, 프린터 등 Span이 탐색할 수 없는 가상 폴더 처리.
        /// </summary>
        /// <returns>위임 성공 시 true (호출측에서 창 닫기 처리 필요)</returns>
        private bool TryDelegateVirtualFolder(string? arg)
        {
            if (string.IsNullOrEmpty(arg)) return false;

            bool shouldDelegate = false;
            string delegatePath = arg;

            // 1. shell: 프로토콜 처리
            if (arg.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
            {
                // 실제 파일 시스템 경로로 변환 가능하면 Span이 직접 처리 (위임 안 함)
                var resolved = ResolveShellPath(arg);
                if (resolved != null && System.IO.Directory.Exists(resolved))
                    return false;

                // 가상 폴더 → explorer.exe 위임
                shouldDelegate = true;
            }
            // 2. CLSID 경로 (::{ 또는 ::{GUID}) → explorer.exe 위임
            else if (arg.StartsWith("::{", StringComparison.OrdinalIgnoreCase))
            {
                shouldDelegate = true;
            }

            if (!shouldDelegate) return false;

            try
            {
                Helpers.DebugLogger.Log($"[Startup] Virtual folder → explorer.exe: {arg}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = delegatePath,
                    UseShellExecute = true
                });

                // 이 창이 가상 폴더 전용으로 열렸으므로 닫기
                // 다른 창이 있으면 그 창만 닫히고, 마지막 창이면 앱 종료 (의도된 동작)
                DispatcherQueue.TryEnqueue(() =>
                {
                    try { Close(); } catch { }
                });
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[Startup] Virtual folder delegation failed: {ex.Message}");
            }

            return true;
        }

        private static bool IsStoreInstalled()
        {
            try
            {
                return Windows.ApplicationModel.Package.Current.SignatureKind
                    == Windows.ApplicationModel.PackageSignatureKind.Store;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// <see cref="FileSystemWatcherService"/>를 초기화하고 경로 변경 이벤트를 구독한다.
        /// 파일 시스템의 변경 사항을 감지하여 자동 새로고침을 수행한다.
        /// </summary>
        private void InitializeFileSystemWatcher()
        {
            try
            {
                _watcherService = App.Current.Services.GetRequiredService<FileSystemWatcherService>();
                _watcherService.PathChanged += OnWatcherPathChanged;
                UpdateFileSystemWatcherPaths();
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[FileSystemWatcher] 초기화 실패: {ex.Message}");
            }

            // Network Shortcuts 폴더 감시 — 네트워크 위치 추가/삭제 시 자동 동기화
            try
            {
                var shortcutsDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Network Shortcuts");
                if (System.IO.Directory.Exists(shortcutsDir))
                {
                    _networkShortcutsWatcher = new System.IO.FileSystemWatcher(shortcutsDir)
                    {
                        NotifyFilter = System.IO.NotifyFilters.DirectoryName,
                        IncludeSubdirectories = false,
                        EnableRaisingEvents = true
                    };
                    _networkShortcutsWatcher.Created += (s, e) => Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () => ViewModel?.RefreshDrives());
                    _networkShortcutsWatcher.Deleted += (s, e) => Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () => ViewModel?.RefreshDrives());
                    _networkShortcutsWatcher.Renamed += (s, e) => Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () => ViewModel?.RefreshDrives());
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[NetworkShortcutsWatcher] 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// FileSystemWatcher가 감시할 경로 목록을 갱신한다.
        /// 활성 탭의 좌/우 탐색기 컬럼 경로를 수집하여 감시 대상으로 등록한다.
        /// </summary>
        private void UpdateFileSystemWatcherPaths()
        {
            if (_watcherService == null || _isClosed) return;

            var paths = new List<string>();

            // 활성 탭의 Left explorer 컬럼 경로들
            var leftExplorer = ViewModel?.Explorer;
            if (leftExplorer != null)
            {
                foreach (var col in leftExplorer.Columns)
                {
                    if (!string.IsNullOrEmpty(col.Path))
                        paths.Add(col.Path);
                }
            }

            // Right explorer 컬럼 경로들 (Split View 시)
            if (ViewModel?.IsSplitViewEnabled == true)
            {
                var rightExplorer = ViewModel.RightExplorer;
                if (rightExplorer != null)
                {
                    foreach (var col in rightExplorer.Columns)
                    {
                        if (!string.IsNullOrEmpty(col.Path))
                            paths.Add(col.Path);
                    }
                }
            }

            _watcherService.SetWatchedPaths(paths);
        }

        /// <summary>
        /// FileSystemWatcher에서 경로 변경이 감지됐을 때 호출되는 콜백.
        /// 변경된 경로에 해당하는 좌/우 탐색기 컬럼을 찾아 비동기로 리로드한다.
        /// </summary>
        private async void OnWatcherPathChanged(string changedPath)
        {
            if (_isClosed) return;

            DispatcherQueue.TryEnqueue(async () =>
            {
                if (_isClosed) return;

                // Bug 4: 명시적 RefreshCurrentFolderAsync 직후엔 Watcher 리로드 스킵 (더블 리프레시 방지)
                if (ViewModel != null && (DateTime.UtcNow - ViewModel.LastExplicitRefreshTime).TotalMilliseconds < 500)
                    return;

                // 캐시 무효화
                try
                {
                    var cache = App.Current.Services.GetService(typeof(FolderContentCache)) as FolderContentCache;
                    cache?.Invalidate(changedPath);

                    // 폴더 크기 캐시도 무효화
                    var sizeSvc = App.Current.Services.GetService(typeof(FolderSizeService)) as FolderSizeService;
                    sizeSvc?.Invalidate(changedPath);
                }
                catch { }

                // 변경된 경로의 컬럼 리로드 — try-catch로 async void 람다 예외 방어
                // (네트워크 드라이브 해제 등 엣지 케이스에서 ReloadAsync 실패 시 앱 크래시 방지)
                try
                {
                    var leftExplorer = ViewModel?.Explorer;
                    if (leftExplorer != null)
                        await ReloadAndCleanupColumn(leftExplorer, changedPath);

                    if (ViewModel?.IsSplitViewEnabled == true)
                    {
                        var rightExplorer = ViewModel.RightExplorer;
                        if (rightExplorer != null)
                            await ReloadAndCleanupColumn(rightExplorer, changedPath);
                    }
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[FileWatcher] ReloadAsync failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Watcher 리로드 후 빈 컬럼 정리.
        /// 리로드된 컬럼이 비어 있으면 자식 컬럼 제거 + 부모 컬럼으로 Active 이동.
        /// </summary>
        private async Task ReloadAndCleanupColumn(ExplorerViewModel explorer, string changedPath)
        {
            for (int i = 0; i < explorer.Columns.Count; i++)
            {
                var col = explorer.Columns[i];
                if (!col.Path.Equals(changedPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                await col.ReloadAsync();
                explorer.NotifyCurrentItemsChanged();

                // 리로드 후 빈 컬럼 → 자식 컬럼 정리 + Active를 부모로 이동
                if (col.Children.Count == 0 && i + 1 < explorer.Columns.Count)
                {
                    explorer.CleanupColumnsFrom(i + 1);
                }
                // 빈 컬럼 자체가 Active이면 부모로 Active 이동
                if (col.Children.Count == 0 && col.IsActive && i > 0)
                {
                    explorer.SetActiveColumn(explorer.Columns[i - 1]);
                }
                break;
            }
        }

        /// <summary>
        /// 이전 LeftExplorer 참조 — 탭 전환 시 구독 해제용
        /// </summary>
        private ExplorerViewModel? _subscribedLeftExplorer;

        /// <summary>
        /// ViewModel의 프로퍼티 변경 이벤트 핸들러.
        /// CurrentViewMode/RightViewMode 변경 시 뷰 가시성을 전환하고,
        /// ActiveTab/Explorer 변경 시 현재 탐색기 구독을 재연결한다.
        /// 탭 전환 중에는 성능 최적화를 위해 뷰 포커스 전환을 스킵한다.
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isClosed) return;
            if (e.PropertyName == nameof(MainViewModel.CurrentViewMode))
            {
                // 탭 전환 중이거나 UpdateViewModeVisibility 내부에서는 FocusActiveView 억제
                if (!ViewModel.IsSwitchingTab && !_suppressFocusOnViewModeChange)
                {
                    // 좌측(CurrentViewMode) 변경 시 패널 Visibility 업데이트
                    var newMode = ViewModel.CurrentViewMode;
                    if (_previousViewMode != newMode)
                    {
                        _previousViewMode = newMode;
                        SetViewModeVisibility(newMode);
                    }
                    FocusActiveView();
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.RightViewMode))
            {
                // 우측 패인 뷰모드 변경 — 우측은 x:Bind로 Visibility 관리되므로
                // 프리뷰 패널 너비와 버튼 상태만 동기화
                // ※ FocusActiveView() 호출 금지: GotFocus 핸들러가 ActivePane을 Left로 뒤집음
                if (!ViewModel.IsSwitchingTab && !_suppressFocusOnViewModeChange)
                {
                    SyncRightPreviewPanelWidth();
                    UpdatePreviewButtonState();
                    UpdateViewModeIcon();
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.Explorer))
            {
                // LeftExplorer가 교체됨 — Columns 구독 재연결 및 View 업데이트
                ResubscribeLeftExplorer();
            }
            else if (e.PropertyName == nameof(MainViewModel.IsToastVisible))
            {
                Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () => AnimateToast(ViewModel.IsToastVisible));
            }
            else if (e.PropertyName == nameof(MainViewModel.ToastMessage))
            {
                Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () =>
                {
                    if (!string.IsNullOrEmpty(ViewModel.ToastMessage))
                        ToastText.Text = ViewModel.ToastMessage;
                });
            }
            else if (e.PropertyName == nameof(MainViewModel.HasCloudDrives) ||
                     e.PropertyName == nameof(MainViewModel.HasNetworkDrives))
            {
                // Sidebar 스케일은 이제 FontScaleService + XAML {Binding} 으로 자동 반영됨.
                // (기존 _iconFontScaleLevel 기반 fan-out 재적용 불필요 — Phase B/C 제거)
            }
            else if (e.PropertyName == nameof(MainViewModel.IsToastError))
            {
                Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () =>
                {
                    if (ViewModel.IsToastError)
                    {
                        ToastIcon.Glyph = "\uE783"; // ErrorBadge
                        ToastIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(255, 235, 87, 87));
                    }
                    else
                    {
                        ToastIcon.Glyph = "\uE73E"; // Checkmark
                        ToastIcon.Foreground = GetThemeBrush("SpanAccentBrush");
                    }
                });
            }
        }

        /// <summary>
        /// LeftExplorer 교체 시 Columns.CollectionChanged 구독 재연결 + View ViewModel 갱신
        /// </summary>
        private void ResubscribeLeftExplorer()
        {
            if (_isClosed) return;

            // 이전 Explorer 구독 해제
            if (_subscribedLeftExplorer != null)
            {
                _subscribedLeftExplorer.Columns.CollectionChanged -= OnColumnsChanged;
                _subscribedLeftExplorer.Columns.CollectionChanged -= OnLeftColumnsChangedForPreview;
                _subscribedLeftExplorer.PropertyChanged -= OnLeftExplorerCurrentPathChanged;
                _subscribedLeftExplorer.NavigationError -= OnNavigationError;
                _subscribedLeftExplorer.PathHighlightsUpdated -= OnPathHighlightsUpdated;
            }

            // 새 Explorer 구독
            var newExplorer = ViewModel.Explorer;
            if (newExplorer != null)
            {
                newExplorer.Columns.CollectionChanged += OnColumnsChanged;
                newExplorer.Columns.CollectionChanged += OnLeftColumnsChangedForPreview;
                newExplorer.PropertyChanged += OnLeftExplorerCurrentPathChanged;
                newExplorer.NavigationError += OnNavigationError;
                newExplorer.PathHighlightsUpdated += OnPathHighlightsUpdated;

                // AddressBarControl 동기화
                SyncAddressBarControls(newExplorer);

                // Per-tab 인스턴스가 자체 ViewModel을 보유하므로 DetailsView/IconView 교체 불필요
                // Miller Columns는 Per-Tab Panel이, Home은 MainViewModel 바인딩이 처리
            }

            _subscribedLeftExplorer = newExplorer;

            // M3: Preview 구독 갱신 — 크리티컬 패스에서 분리
            Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () =>
            {
                UnsubscribePreviewSelection(isLeft: true);
                if (ViewModel.IsLeftPreviewEnabled)
                    SubscribePreviewToLastColumn(isLeft: true);
            });

            // Git 상태바: 새 Explorer 구독
            ResubscribeGitStatusBar(isLeft: true);

            // FileSystemWatcher 감시 경로 갱신
            UpdateFileSystemWatcherPaths();
        }

        /// <summary>
        /// 모든 AddressBar의 편집 모드를 해제한다.
        /// 밀러 컬럼·사이드바 등 콘텐츠 영역 클릭 시 호출하여
        /// 빈 공간 클릭에서도 주소창 편집이 취소되도록 한다.
        /// </summary>
        private void DismissAddressBarEditMode()
        {
            // Stage S-2: only MainAddressBar remains; per-pane bars removed.
            MainAddressBar.ExitEditMode();
        }

        /// <summary>
        /// AddressBarControl들에 PathSegments/CurrentPath를 동기화한다.
        /// Left Explorer 교체, 탭 전환, 세션 복원 시 호출.
        /// </summary>
        private void SyncAddressBarControls(ExplorerViewModel? explorer)
        {
            if (explorer == null) return;

            // RecycleBin/Home 모드: Explorer 경로 대신 전용 브레드크럼 설정
            if (ViewModel.CurrentViewMode == ViewMode.RecycleBin)
            {
                SetSpecialModeAddressBar(ViewMode.RecycleBin);
                return;
            }
            if (ViewModel.CurrentViewMode == ViewMode.Home)
            {
                SetSpecialModeAddressBar(ViewMode.Home);
                return;
            }

            // Stage S-2: only MainAddressBar — it follows ActiveExplorer automatically.
            MainAddressBar.PathSegments = explorer.PathSegments;
            MainAddressBar.CurrentPath = explorer.CurrentPath ?? string.Empty;
        }

        /// <summary>
        /// Home/RecycleBin 등 특수 뷰모드에서 주소바에 아이콘 + 라벨 브레드크럼 설정.
        /// </summary>
        /// <summary>
        /// Home/RecycleBin 모드에서 주소바를 전용 브레드크럼으로 설정.
        /// XAML 아이콘(HomeAddressIcon/RecycleBinAddressIcon)은 호출자가 관리.
        /// </summary>
        private void SetSpecialModeAddressBar(ViewMode mode)
        {
            var loc = App.Current.Services.GetService<Services.LocalizationService>();
            var (label, path) = mode switch
            {
                ViewMode.RecycleBin => (loc?.Get("RecycleBin") ?? "Recycle Bin", "shell:RecycleBinFolder"),
                ViewMode.Home => (loc?.Get("Home") ?? "Home", "::home::"),
                _ => ("", "")
            };
            // isLast: false → chevron(>) 표시 (홈 패턴과 동일)
            var segments = new System.Collections.ObjectModel.ObservableCollection<Models.PathSegment>
            {
                new Models.PathSegment(label, path, isLast: false)
            };
            MainAddressBar.PathSegments = segments;
            MainAddressBar.CurrentPath = path;

            // XAML 아이콘 가시성
            HomeAddressIcon.Visibility = mode == ViewMode.Home ? Visibility.Visible : Visibility.Collapsed;
            RecycleBinAddressIcon.Visibility = mode == ViewMode.RecycleBin ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// LeftExplorer의 CurrentPath 변경 시 MainAddressBar/LeftAddressBar 동기화.
        /// </summary>
        private void OnLeftExplorerCurrentPathChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ExplorerViewModel explorer) return;

            if (e.PropertyName == nameof(ExplorerViewModel.CurrentPath))
            {
                Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () =>
                {
                    // RecycleBin/Home 모드: 전용 브레드크럼으로 강제 재설정
                    if (ViewModel.CurrentViewMode == ViewMode.RecycleBin
                        || ViewModel.CurrentViewMode == ViewMode.Home)
                    {
                        SetSpecialModeAddressBar(ViewModel.CurrentViewMode);
                        return;
                    }
                    MainAddressBar.PathSegments = explorer.PathSegments;
                    MainAddressBar.CurrentPath = explorer.CurrentPath ?? string.Empty;

                    // Downloads folder: deferred auto-grouping after children load
                    ScheduleDownloadsGroupingIfNeeded(explorer);
                });
            }
            else if (e.PropertyName == nameof(ExplorerViewModel.HasActiveSearchResults) ||
                     e.PropertyName == nameof(ExplorerViewModel.IsRecursiveSearching))
            {
                Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () =>
                {
                    bool showLoc = explorer.HasActiveSearchResults;
                    GetActiveDetailsView()?.ShowLocationColumn(showLoc);
                });
            }
        }

        /// <summary>
        /// SwitchToTab이 PropertyChanged를 우회했으므로,
        /// XAML x:Bind가 관찰하는 ViewMode 관련 프로퍼티의 변경을 일괄 통지한다.
        /// IsSwitchingTab=false 이후에 호출되므로 OnViewModelPropertyChanged의 FocusActiveView가 정상 동작.
        /// </summary>
        private void UpdateViewModeVisibility()
        {
            _suppressFocusOnViewModeChange = true;
            try
            {
                var newMode = ViewModel.CurrentViewMode;
                if (_previousViewMode != newMode)
                {
                    _previousViewMode = newMode;
                    // x:Bind 파이프라인 우회: 직접 Visibility 할당 (PropertyChanged → x:Bind 재평가 제거)
                    SetViewModeVisibility(newMode);
                    // IsSingleNonHomeVisible 등 남은 바인딩용 (경량)
                    ViewModel.NotifyViewModeChanged();

                    // Miller 뷰로 전환 시 열려있던 필터 바 자동 닫기 — Miller에서는 필터 미지원.
                    if (newMode == Models.ViewMode.MillerColumns
                        && LeftFilterBar != null
                        && LeftFilterBar.Visibility == Visibility.Visible)
                    {
                        CloseFilterBar();
                    }
                }
            }
            finally
            {
                _suppressFocusOnViewModeChange = false;
            }
        }

        /// <summary>
        /// x:Bind 바인딩 대신 코드비하인드에서 직접 4개 뷰의 Visibility를 설정.
        /// PropertyChanged 파이프라인을 거치지 않으므로 레이아웃 재계산 최소화.
        /// 또한 뷰 모드 전환 시 해당 뷰의 ViewModel을 lazy 갱신.
        /// </summary>
        private double _savedSidebarWidth = 200;
        private bool _sidebarHiddenForSpecialMode;

        /// <summary>
        /// 지정된 <see cref="ViewMode"/>에 따라 각 뷰 호스트(Miller, Details, List, Icon, Home, Settings)의
        /// Visibility를 전환하고, 특수 모드(Settings)에서는 툴바/사이드바를 숨기며,
        /// 일반 모드로 복귀 시 복원한다.
        /// </summary>
        /// <param name="mode">적용할 뷰 모드.</param>
        private void SetViewModeVisibility(ViewMode mode)
        {
            bool isSpecialMode = mode == ViewMode.Settings || mode == ViewMode.ActionLog;
            bool isRecycleBin = mode == ViewMode.RecycleBin;

            // ★ Host Visible 전에 per-tab 패널 정리 (이전 탭 잔상 방지)
            var tabId = ViewModel.ActiveTab?.Id;
            if (tabId != null && mode == ViewMode.MillerColumns)
            {
                // SwitchMillerPanel은 _activeMillerTabId == tabId일 때 early return하므로
                // 특수 탭(RecycleBin 등)에서 복귀 시 강제 리셋 후 호출
                if (_activeMillerTabId != tabId)
                    SwitchMillerPanel(tabId);
                else
                {
                    // 같은 탭이지만 Host가 Collapsed→Visible로 변경되는 경우 (RecycleBin 복귀)
                    if (_tabMillerPanels.TryGetValue(tabId, out var mp))
                        mp.scroller.Visibility = Visibility.Visible;
                }
            }
            if (tabId != null && mode == ViewMode.Details)
            {
                foreach (var kvp in _tabDetailsPanels)
                    kvp.Value.Visibility = kvp.Key == tabId ? Visibility.Visible : Visibility.Collapsed;
                if (!_tabDetailsPanels.ContainsKey(tabId))
                    CreateDetailsPanelForTab(ViewModel.ActiveTab!);
                if (_tabDetailsPanels.TryGetValue(tabId, out var dp))
                    dp.Visibility = Visibility.Visible;
                _activeDetailsTabId = tabId;
            }
            if (tabId != null && mode == ViewMode.List)
            {
                foreach (var kvp in _tabListPanels)
                    kvp.Value.Visibility = kvp.Key == tabId ? Visibility.Visible : Visibility.Collapsed;
                if (!_tabListPanels.ContainsKey(tabId))
                    CreateListPanelForTab(ViewModel.ActiveTab!);
                if (_tabListPanels.TryGetValue(tabId, out var mp))
                    mp.Visibility = Visibility.Visible;
                _activeListTabId = tabId;
            }
            if (tabId != null && Helpers.ViewModeExtensions.IsIconMode(mode))
            {
                foreach (var kvp in _tabIconPanels)
                    kvp.Value.Visibility = kvp.Key == tabId ? Visibility.Visible : Visibility.Collapsed;
                if (!_tabIconPanels.ContainsKey(tabId))
                    CreateIconPanelForTab(ViewModel.ActiveTab!);
                if (_tabIconPanels.TryGetValue(tabId, out var ip))
                    ip.Visibility = Visibility.Visible;
                _activeIconTabId = tabId;
            }

            // HOST 단위 Visibility (per-tab 패널이 정리된 후 설정)
            MillerTabsHost.Visibility = mode == ViewMode.MillerColumns ? Visibility.Visible : Visibility.Collapsed;
            DetailsTabsHost.Visibility = mode == ViewMode.Details ? Visibility.Visible : Visibility.Collapsed;
            ListTabsHost.Visibility = mode == ViewMode.List ? Visibility.Visible : Visibility.Collapsed;
            IconTabsHost.Visibility = Helpers.ViewModeExtensions.IsIconMode(mode) ? Visibility.Visible : Visibility.Collapsed;
            HomeView.Visibility = mode == ViewMode.Home ? Visibility.Visible : Visibility.Collapsed;
            // Stage S-3.32: SettingsView removed — Settings is now a separate window.
            LogView.Visibility = mode == ViewMode.ActionLog ? Visibility.Visible : Visibility.Collapsed;
            RecycleBinView.Visibility = mode == ViewMode.RecycleBin ? Visibility.Visible : Visibility.Collapsed;
            if (mode == ViewMode.RecycleBin)
            {
                SetSpecialModeAddressBar(ViewMode.RecycleBin);
                _ = LoadRecycleBinViewAsync();
            }
            if (mode == ViewMode.ActionLog)
            {
                LogView.Refresh();
            }
            else if (mode == ViewMode.Home)
            {
                SetSpecialModeAddressBar(ViewMode.Home);
                HomeView.ApplyIconFontScale(Helpers.FontScaleService.Instance.Level);
                // Home 탭도 특정 경로가 없으므로 git 상태바 숨김 (사이드바 복원은 아래 else 블록에서 정상 처리)
                _leftGitStatusBarVm?.Clear();
            }

            // 분할뷰 UI 동기화 — 탭별 분할 상태에 따라 우측 패인 표시/숨김
            if (ViewModel.IsSplitViewEnabled && !isSpecialMode && !isRecycleBin)
            {
                SplitterCol.Width = new GridLength(0);
                RightPaneCol.Width = new GridLength(1, GridUnitType.Star);
                SyncRightAddressBar();
                SubscribeRightExplorerForAddressBar();
            }
            else
            {
                SplitterCol.Width = new GridLength(0);
                RightPaneCol.Width = new GridLength(0);
                UnsubscribeRightExplorerForAddressBar();
                if (ViewModel.ActivePane == ActivePane.Right)
                    ViewModel.ActivePane = ActivePane.Left;
            }

            // Settings/ActionLog 모드: 사이드바 + 프리뷰 패널 완전 숨김
            if (isSpecialMode)
            {
                if (!_sidebarHiddenForSpecialMode)
                {
                    _savedSidebarWidth = SidebarCol.Width.Value;
                    _sidebarHiddenForSpecialMode = true;
                }
                SidebarBorder.Visibility = Visibility.Collapsed;
                SidebarSplitter.Visibility = Visibility.Collapsed;
                SidebarCol.MinWidth = 0;
                SidebarCol.Width = new GridLength(0);
                LeftPreviewSplitterCol.Width = new GridLength(0);
                LeftPreviewCol.Width = new GridLength(0);

                // Settings/ActionLog 탭은 파일 시스템 경로와 무관 → git 상태바 숨김
                _leftGitStatusBarVm?.Clear();
                _rightGitStatusBarVm?.Clear();
            }
            else
            {
                if (_sidebarHiddenForSpecialMode)
                {
                    // Legacy SidebarBorder remains collapsed under the LumiSidebar redesign.
                    SidebarBorder.Visibility = Visibility.Collapsed;
                    SidebarSplitter.Visibility = Visibility.Collapsed;
                    SidebarCol.Width = new GridLength(0); // legacy column always 0 under LumiSidebar
                    SidebarCol.MinWidth = 150;
                    _sidebarHiddenForSpecialMode = false;

                    // Sidebar 스케일은 FontScaleService + XAML {Binding} 으로 자동 반영 — 재적용 불필요.
                }
                // 프리뷰 패널 복원 (활성화 상태에 따라, Home에서는 숨김)
                bool hidePreview = mode == ViewMode.Home || isRecycleBin;
                bool isMillerMode = mode == ViewMode.MillerColumns;

                if (!hidePreview && ViewModel.IsLeftPreviewEnabled)
                {
                    // 모든 뷰 모드 공통: 사이드 미리보기 패널 표시
                    LeftPreviewSplitterCol.Width = new GridLength(2, GridUnitType.Pixel);
                    if (LeftPreviewCol.Width.Value < 1)
                    {
                        double savedWidth = 320;
                        try
                        {
                            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                            if (settings.Values.TryGetValue("LeftPreviewWidth", out var lw))
                                savedWidth = Math.Max(320, (double)lw);
                        }
                        catch { }
                        LeftPreviewCol.Width = new GridLength(savedWidth, GridUnitType.Pixel);
                    }
                }
                else
                {
                    // Home 모드 또는 미리보기 비활성: 사이드 패널 숨김
                    LeftPreviewSplitterCol.Width = new GridLength(0);
                    LeftPreviewCol.Width = new GridLength(0);
                }
            }

            // Home/ActionLog 모드: 툴바 버튼 비활성화 (탐색기 컨텍스트 없음)
            bool isNonExplorerMode = mode == ViewMode.Home || mode == ViewMode.ActionLog;
            BackButton.IsEnabled = !isNonExplorerMode && ViewModel.CanGoBack;
            ForwardButton.IsEnabled = !isNonExplorerMode && ViewModel.CanGoForward;
            UpButton.IsEnabled = !isNonExplorerMode;
            NewFolderButton.IsEnabled = !isNonExplorerMode;
            NewItemDropdown.IsEnabled = !isNonExplorerMode;
            SortButton.IsEnabled = !isNonExplorerMode;
            ViewModeButton.IsEnabled = !isNonExplorerMode;
            PreviewToggleButton.IsEnabled = !isNonExplorerMode;
            UpdatePreviewButtonState();
            UpdateSplitViewButtonState();
            UpdateViewModeIcon();
            SplitViewButton.IsEnabled = true; // 홈에서도 분할뷰 토글 가능
            CopyPathButton.IsEnabled = !isNonExplorerMode;
            SearchBox.IsEnabled = !isNonExplorerMode;
            ToolbarCutButton.IsEnabled = false;
            ToolbarCopyButton.IsEnabled = false;
            ToolbarPasteButton.IsEnabled = false;
            ToolbarRenameButton.IsEnabled = false;
            ToolbarDeleteButton.IsEnabled = false;

            // (per-tab 패널 생성/정리는 Host Visibility 설정 전에 처리됨 — 상단 참조)

            // Breadcrumb lazy 갱신 (ResubscribeLeftExplorer에서 skip된 경우 보정)
            var explorer = ViewModel.Explorer;
            if (!ViewModel.IsSplitViewEnabled && mode != ViewMode.Settings)
            {
                if (mode == ViewMode.Home)
                {
                    // 홈 모드: 🏠 > 홈 breadcrumb 표시
                    HomeAddressIcon.Visibility = Visibility.Visible;
                    RecycleBinAddressIcon.Visibility = Visibility.Collapsed;
                    var homeSegments = new[]
                    {
                        new Models.PathSegment(_loc.Get("Home"), "::home::", isLast: false)
                    };
                    MainAddressBar.PathSegments = homeSegments;
                    SearchBox.PlaceholderText = _loc.Get("HomeSearch");
                }
                else if (mode == ViewMode.RecycleBin)
                {
                    // 휴지통 모드: 🗑 > 휴지통 breadcrumb 표시 (홈과 동일 패턴)
                    HomeAddressIcon.Visibility = Visibility.Collapsed;
                    RecycleBinAddressIcon.Visibility = Visibility.Visible;
                    var rbSegments = new[]
                    {
                        new Models.PathSegment(_loc.Get("RecycleBin") ?? "Recycle Bin", "shell:RecycleBinFolder", isLast: false)
                    };
                    MainAddressBar.PathSegments = rbSegments;
                    MainAddressBar.CurrentPath = "shell:RecycleBinFolder";
                }
                else
                {
                    HomeAddressIcon.Visibility = Visibility.Collapsed;
                    RecycleBinAddressIcon.Visibility = Visibility.Collapsed;
                    MainAddressBar.PathSegments = explorer?.PathSegments;
                    MainAddressBar.CurrentPath = explorer?.CurrentPath ?? string.Empty;
                    SearchBox.PlaceholderText = _loc.Get("SearchPlaceholderWithHint");
                }
            }
        }

        private void OnNavigationError(string message)
        {
            Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () => ViewModel.ShowError(message));
        }

        /// <summary>
        /// 토스트 알림 UI의 나타남/사라짐 애니메이션을 실행한다.
        /// 불투명도와 Y축 이동 애니메이션을 조합하여 실행한다.
        /// </summary>
        /// <param name="show">true면 나타남, false면 사라짐.</param>
        private void AnimateToast(bool show)
        {
            if (_isClosed) return;

            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();

            var opacityAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                To = show ? 1.0 : 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(show ? 200 : 300)),
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase
                {
                    EasingMode = show
                        ? Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut
                        : Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseIn
                }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(opacityAnim, ToastOverlay);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(opacityAnim, "Opacity");

            var translateAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                To = show ? 0 : 20,
                Duration = new Duration(TimeSpan.FromMilliseconds(show ? 200 : 300)),
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase
                {
                    EasingMode = show
                        ? Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut
                        : Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseIn
                }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(translateAnim, ToastTranslate);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(translateAnim, "Y");

            storyboard.Children.Add(opacityAnim);
            storyboard.Children.Add(translateAnim);
            storyboard.Begin();
        }

        /// <summary>
        /// 현재 활성 뷰 모드에 따라 적절한 UI 요소에 포커스를 설정한다.
        /// Miller Columns 모드에서는 마지막 컬럼의 ListView에,
        /// Details/List/Icon 모드에서는 해당 뷰에 포커스를 설정한다.
        /// </summary>
        private void FocusActiveView()
        {
            // Use DispatcherQueue for proper timing (after visibility changes take effect)
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (_isClosed || ViewModel == null) return;

                // Determine which pane's view mode to use
                var viewMode = (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                    ? ViewModel.RightViewMode : ViewModel.CurrentViewMode;

                switch (viewMode)
                {
                    case Models.ViewMode.MillerColumns:
                        var columns = ViewModel.ActiveExplorer?.Columns;
                        if (columns != null && columns.Count > 0)
                        {
                            // H3: 동기 스크롤 (이미 Low priority 내부이므로 추가 디스패치 불필요)
                            ScrollToLastColumnSync(ViewModel.LeftExplorer, GetActiveMillerScrollViewer());
                            // 마지막 컬럼으로 포커스 (GetActiveColumnIndex 비주얼트리 순회 생략)
                            FocusColumnAsync(columns.Count - 1);
                        }
                        Helpers.DebugLogger.Log("[MainWindow] Focus: MillerColumns");
                        break;

                    case Models.ViewMode.Details:
                        GetActiveDetailsView()?.FocusListView();
                        Helpers.DebugLogger.Log("[MainWindow] Focus: Details");
                        break;

                    case Models.ViewMode.List:
                        GetActiveListView()?.FocusGridView();
                        Helpers.DebugLogger.Log("[MainWindow] Focus: List");
                        break;

                    case Models.ViewMode.IconSmall:
                    case Models.ViewMode.IconMedium:
                    case Models.ViewMode.IconLarge:
                    case Models.ViewMode.IconExtraLarge:
                        GetActiveIconView()?.FocusGridView();
                        Helpers.DebugLogger.Log($"[MainWindow] Focus: Icon ({viewMode})");
                        break;

                    case Models.ViewMode.Home:
                        Helpers.DebugLogger.Log("[MainWindow] Focus: Home");
                        break;
                }
            });
        }

        // ScrollToLastColumn, ScrollToLastColumnSync, GetTotalColumnsActualWidth → MainWindow.NavigationManager.cs


        // =================================================================
        //  Drive click
        // =================================================================

        /// <summary>
        /// 사이드바 드라이브 항목 클릭 이벤트 핸들러.
        /// 선택된 드라이브 경로로 탐색을 시작한다.
        /// OpenDrive 이후 현재 뷰 모드를 보존하며,
        /// MillerColumns이면 첫 컬럼에, 그 외 모드면 해당 뷰에 포커스를 이동한다.
        /// </summary>
        private void OnDriveItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is DriveItem drive)
            {
                Helpers.DebugLogger.Log($"[OnDriveItemClick] BEFORE: CurrentViewMode={ViewModel.CurrentViewMode}");
                ViewModel.OpenDrive(drive);
                Helpers.DebugLogger.Log($"[OnDriveItemClick] AFTER OpenDrive: CurrentViewMode={ViewModel.CurrentViewMode}");
                UpdateViewModeVisibility();
                Helpers.DebugLogger.Log($"[OnDriveItemClick] AFTER UpdateViewModeVisibility: CurrentViewMode={ViewModel.CurrentViewMode}");
                if (ViewModel.CurrentViewMode == ViewMode.MillerColumns)
                    FocusColumnAsync(0);
                else
                    FocusActiveView();
            }
        }

        /// <summary>
        /// 사이드바 섹션 헤더 접기/펴기 토글
        /// </summary>
        private void OnSidebarSectionHeaderTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.Tag is string tag)
            {
                switch (tag)
                {
                    case "Local": ViewModel.IsLocalDrivesExpanded = !ViewModel.IsLocalDrivesExpanded; break;
                    case "Cloud": ViewModel.IsCloudDrivesExpanded = !ViewModel.IsCloudDrivesExpanded; break;
                    case "Network": ViewModel.IsNetworkDrivesExpanded = !ViewModel.IsNetworkDrivesExpanded; break;
                }
            }
        }

        /// <summary>
        /// 하이브리드 사이드바 드라이브 항목 탭 이벤트.
        /// 원격 연결(FTP/SFTP)인 경우 비밀번호 확인 후 연결하고,
        /// 로컬 드라이브인 경우 OnDriveItemClick과 동일하게 뷰 모드를 보존하면서 탐색한다.
        /// </summary>
        private async void OnDriveItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.DataContext is DriveItem drive)
                {
                    if (drive.IsRemoteConnection && drive.ConnectionId != null)
                    {
                        // 원격 연결: 비밀번호 확인 → 연결
                        await HandleRemoteConnectionTapped(drive.ConnectionId);
                    }
                    else
                    {
                        Helpers.DebugLogger.Log($"[OnDriveItemTapped] BEFORE: CurrentViewMode={ViewModel.CurrentViewMode}");
                        ViewModel.OpenDrive(drive);
                        Helpers.DebugLogger.Log($"[OnDriveItemTapped] AFTER OpenDrive: CurrentViewMode={ViewModel.CurrentViewMode}");
                        UpdateViewModeVisibility();
                        Helpers.DebugLogger.Log($"[OnDriveItemTapped] AFTER UpdateViewModeVisibility: CurrentViewMode={ViewModel.CurrentViewMode}");
                        if (ViewModel.CurrentViewMode == ViewMode.MillerColumns)
                            FocusColumnAsync(0);
                        else
                            FocusActiveView();
                    }
                    Helpers.DebugLogger.Log($"[Sidebar] Drive tapped: {drive.Name}");
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[Sidebar] OnDriveItemTapped error: {ex.Message}");
            }
        }

        /// <summary>
        /// 네트워크 찾아보기 버튼 탭 이벤트.
        /// UNC 경로 입력 대화상자를 표시하며, SMB 네트워크 공유 폴더 검색과 연결을 처리한다.
        /// </summary>
        private async void OnBrowseNetworkTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
            var networkService = App.Current.Services.GetRequiredService<NetworkBrowserService>();
            var connService = App.Current.Services.GetRequiredService<ConnectionManagerService>();

            // Create dialog content
            var dialogPanel = new StackPanel { Spacing = 12, MinWidth = 360 };

            // UNC path input section
            var pathInput = new TextBox
            {
                PlaceholderText = @"\\server\share",
                Header = _loc.Get("UncPathInput"),
                MinWidth = 340
            };
            dialogPanel.Children.Add(pathInput);

            // Separator
            dialogPanel.Children.Add(new TextBlock
            {
                Text = _loc.Get("SearchNetwork"),
                Foreground = GetThemeBrush("SpanTextSecondaryBrush"),
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            });

            // Network list
            var networkList = new ListView
            {
                Height = 250,
                SelectionMode = ListViewSelectionMode.Single
            };
            var iconFontPath = Services.IconService.Current?.FontFamilyPath ?? "/Assets/Fonts/remixicon.ttf#remixicon";
            networkList.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                $@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                               xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <StackPanel Orientation='Horizontal' Spacing='8' Padding='4,2'>
                        <TextBlock Text='{{Binding IconGlyph}}'
                                   FontFamily='{iconFontPath}'
                                   FontSize='16' VerticalAlignment='Center'/>
                        <TextBlock Text='{{Binding Name}}' FontSize='13' VerticalAlignment='Center'/>
                    </StackPanel>
                  </DataTemplate>");

            dialogPanel.Children.Add(networkList);

            // Status text
            var statusText = new TextBlock
            {
                Text = _loc.Get("SearchingComputers"),
                FontSize = 12,
                Foreground = GetThemeBrush("SpanTextTertiaryBrush")
            };
            dialogPanel.Children.Add(statusText);

            // State tracking
            string? selectedPath = null;

            // Load computers asynchronously
            _ = LoadNetworkComputersAsync();

            async Task LoadNetworkComputersAsync()
            {
                var computers = await networkService.GetNetworkComputersAsync();
                if (computers.Count > 0)
                {
                    networkList.ItemsSource = computers;
                    statusText.Text = string.Format(_loc.Get("ComputersFound"), computers.Count);
                }
                else
                {
                    statusText.Text = _loc.Get("NoComputersFound");
                }
            }

            networkList.DoubleTapped += async (s, args) =>
            {
                if (networkList.SelectedItem is NetworkItem item)
                {
                    if (item.Type == NetworkItemType.Server)
                    {
                        // Load shares for this server
                        statusText.Text = string.Format(_loc.Get("SearchingShares"), item.Name);
                        networkList.ItemsSource = null;

                        var shares = await networkService.GetServerSharesAsync(item.Name);
                        if (shares.Count > 0)
                        {
                            networkList.ItemsSource = shares;
                            statusText.Text = string.Format(_loc.Get("SharesFound"), shares.Count);
                        }
                        else
                        {
                            statusText.Text = _loc.Get("NoSharesFound");
                        }
                    }
                }
            };

            networkList.SelectionChanged += (s, args) =>
            {
                if (networkList.SelectedItem is NetworkItem item)
                {
                    selectedPath = item.Path;
                    pathInput.Text = item.Path;
                }
            };

            var dialog = new ContentDialog
            {
                Title = _loc.Get("NetworkBrowse"),
                Content = dialogPanel,
                PrimaryButtonText = _loc.Get("Register"),
                CloseButtonText = _loc.Get("Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await ShowContentDialogSafeAsync(dialog);

            if (result == ContentDialogResult.Primary)
            {
                var targetPath = !string.IsNullOrWhiteSpace(pathInput.Text)
                    ? pathInput.Text.Trim()
                    : selectedPath;

                if (!string.IsNullOrEmpty(targetPath))
                {
                    // 중복 등록 방지: 같은 UNC 경로가 이미 등록되어 있는지 확인
                    var existing = connService.SavedConnections.FirstOrDefault(
                        c => c.Protocol == Models.RemoteProtocol.SMB
                             && string.Equals(c.UncPath, targetPath, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        // DisplayName: \\server\share → server\share
                        var displayName = targetPath.TrimStart('\\');

                        var newConn = new Models.ConnectionInfo
                        {
                            Protocol = Models.RemoteProtocol.SMB,
                            UncPath = targetPath,
                            DisplayName = displayName,
                            Port = Models.ConnectionInfo.GetDefaultPort(Models.RemoteProtocol.SMB),
                            LastConnected = DateTime.Now
                        };

                        connService.AddConnection(newConn);
                        Helpers.DebugLogger.Log($"[Network] SMB 연결 등록: {targetPath}");
                    }
                    else
                    {
                        Helpers.DebugLogger.Log($"[Network] SMB 연결 이미 등록됨: {targetPath}");
                    }

                    // 등록 후 해당 경로로 탐색
                    if (ViewModel.CurrentViewMode == ViewMode.Home)
                    {
                        ViewModel.SwitchViewMode(ViewMode.MillerColumns);
                    }

                    if (ViewModel.ActiveExplorer != null) await ViewModel.ActiveExplorer.NavigateToPath(targetPath);
                    FocusColumnAsync(0);
                }
            }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[Network] OnBrowseNetworkTapped error: {ex.Message}");
            }
        }

        /// <summary>
        /// 연결 다이얼로그 표시. existing이 null이면 새 연결, non-null이면 편집 모드.
        /// 반환: (result, connInfo, password, saveChecked)
        /// </summary>
        private async Task<(ContentDialogResult result, Models.ConnectionInfo? connInfo, string? password, bool saveChecked, IFileSystemProvider? provider)>
            ShowConnectionDialog(Models.ConnectionInfo? existing)
        {
            var isEdit = existing != null;
            var isSmbEdit = isEdit && existing!.Protocol == Models.RemoteProtocol.SMB;

            var dialogPanel = new StackPanel { Spacing = 8 };
            const double labelW = 140;

            // 인라인 라벨 행 헬퍼
            Grid MakeRow(string labelKey, FrameworkElement input)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelW) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var label = new TextBlock
                {
                    Text = _loc.Get(labelKey),
                    VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                    FontSize = 14
                };
                Grid.SetColumn(label, 0);
                Grid.SetColumn(input, 1);
                row.Children.Add(label);
                row.Children.Add(input);
                return row;
            }

            // SMB 편집: 표시 이름 + UNC 경로만
            TextBox? smbDisplayNameInput = null;
            TextBox? smbUncPathInput = null;
            ComboBox? protocolCombo = null;
            TextBox? hostInput = null;
            NumberBox? portInput = null;
            TextBox? usernameInput = null;
            PasswordBox? passwordInput = null;
            TextBox? pathInput = null;
            TextBox? displayNameInput = null;
            CheckBox? saveCheckBox = null;
            RadioButton? authPasswordRadio = null;
            RadioButton? authSshKeyRadio = null;
            StackPanel? sshKeyPanel = null;
            TextBox? sshKeyPathInput = null;
            PasswordBox? passphraseInput = null;

            if (isSmbEdit)
            {
                smbDisplayNameInput = new TextBox
                {
                    Text = existing!.DisplayName,
                    PlaceholderText = existing.UncPath ?? ""
                };
                dialogPanel.Children.Add(MakeRow("DisplayNameOptional", smbDisplayNameInput));

                smbUncPathInput = new TextBox
                {
                    Text = existing.UncPath ?? "",
                    PlaceholderText = @"\\server\share"
                };
                dialogPanel.Children.Add(MakeRow("Host", smbUncPathInput));
            }
            else
            {
                // 1행: 프로토콜 + 호스트 + 포트
                var firstRow = new Grid();
                firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelW) });
                firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // 프로토콜
                firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 호스트
                firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) }); // 포트

                var protocolLabel = new TextBlock
                {
                    Text = _loc.Get("Protocol"),
                    VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                    FontSize = 14
                };
                Grid.SetColumn(protocolLabel, 0);
                firstRow.Children.Add(protocolLabel);

                protocolCombo = new ComboBox
                {
                    ItemsSource = new[] { "SFTP", "FTP", "FTPS" },
                    SelectedIndex = isEdit ? (int)existing!.Protocol : 0,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
                };
                Grid.SetColumn(protocolCombo, 1);
                firstRow.Children.Add(protocolCombo);

                hostInput = new TextBox
                {
                    PlaceholderText = "example.com",
                    Text = isEdit ? existing!.Host : "",
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(hostInput, 2);
                firstRow.Children.Add(hostInput);

                portInput = new NumberBox
                {
                    Value = isEdit ? existing!.Port : 22,
                    Minimum = 1,
                    Maximum = 65535,
                    SpinButtonPlacementMode = Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(portInput, 3);
                firstRow.Children.Add(portInput);

                dialogPanel.Children.Add(firstRow);

                // 포트 자동 변경 (새 연결 모드에서만)
                if (!isEdit)
                {
                    protocolCombo.SelectionChanged += (s, args) =>
                    {
                        portInput.Value = protocolCombo.SelectedIndex switch
                        {
                            0 => 22,   // SFTP
                            1 => 21,   // FTP
                            2 => 990,  // FTPS
                            _ => 22
                        };
                    };
                }

                // 사용자명
                usernameInput = new TextBox
                {
                    PlaceholderText = "user",
                    Text = isEdit ? existing!.Username : ""
                };
                dialogPanel.Children.Add(MakeRow("Username", usernameInput));

                // 인증 방식 (SFTP만 SSH 키 지원)
                var isSftp = isEdit ? existing!.Protocol == Models.RemoteProtocol.SFTP : true;
                var useSshKey = isEdit && existing!.AuthMethod == Models.AuthMethod.SshKey;

                // 인증 방식 라디오 (인라인)
                var authInline = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                authPasswordRadio = new RadioButton { Content = _loc.Get("AuthPassword"), IsChecked = !useSshKey, GroupName = "AuthMethod", MinWidth = 0, Padding = new Thickness(4, 0, 0, 0) };
                authSshKeyRadio = new RadioButton { Content = _loc.Get("AuthSshKey"), IsChecked = useSshKey, GroupName = "AuthMethod", MinWidth = 0, Padding = new Thickness(4, 0, 0, 0) };
                authInline.Children.Add(authPasswordRadio);
                authInline.Children.Add(authSshKeyRadio);

                var authMethodRow = MakeRow("AuthMethodLabel", authInline);
                authMethodRow.Visibility = isSftp ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
                dialogPanel.Children.Add(authMethodRow);

                // 비밀번호 행
                passwordInput = new PasswordBox
                {
                    PlaceholderText = _loc.Get("Password")
                };
                if (isEdit && !useSshKey)
                {
                    var connService = App.Current.Services.GetRequiredService<ConnectionManagerService>();
                    var savedPw = connService.LoadCredential(existing!.Id);
                    if (!string.IsNullOrEmpty(savedPw))
                        passwordInput.Password = savedPw;
                }
                var passwordRow = MakeRow("Password", passwordInput);
                passwordRow.Visibility = useSshKey ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
                dialogPanel.Children.Add(passwordRow);

                // SSH 키 패널
                sshKeyPanel = new StackPanel { Spacing = 8, Visibility = useSshKey ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed };

                // SSH 키 파일 행
                var keyInputRow = new Grid();
                keyInputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                keyInputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                sshKeyPathInput = new TextBox
                {
                    PlaceholderText = @"C:\Users\...\.ssh\id_rsa",
                    Text = isEdit && !string.IsNullOrEmpty(existing!.SshKeyPath) ? existing.SshKeyPath : ""
                };
                Grid.SetColumn(sshKeyPathInput, 0);
                keyInputRow.Children.Add(sshKeyPathInput);

                var browseBtn = new Button
                {
                    Content = _loc.Get("SshKeyBrowse"),
                    VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                browseBtn.Click += async (s, args) =>
                {
                    var picker = new Windows.Storage.Pickers.FileOpenPicker();
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
                    picker.FileTypeFilter.Add("*");
                    picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.HomeGroup;
                    var file = await picker.PickSingleFileAsync();
                    if (file != null) sshKeyPathInput.Text = file.Path;
                };
                Grid.SetColumn(browseBtn, 1);
                keyInputRow.Children.Add(browseBtn);

                sshKeyPanel.Children.Add(MakeRow("SshKeyPath", keyInputRow));

                // 패스프레이즈 행
                passphraseInput = new PasswordBox
                {
                    PlaceholderText = _loc.Get("Optional")
                };
                if (isEdit && useSshKey)
                {
                    var connService = App.Current.Services.GetRequiredService<ConnectionManagerService>();
                    var savedPw = connService.LoadCredential(existing!.Id);
                    if (!string.IsNullOrEmpty(savedPw))
                        passphraseInput.Password = savedPw;
                }
                sshKeyPanel.Children.Add(MakeRow("Passphrase", passphraseInput));
                dialogPanel.Children.Add(sshKeyPanel);

                // 인증 방식 전환 이벤트
                authPasswordRadio.Checked += (s, args) =>
                {
                    passwordRow.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    sshKeyPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                };
                authSshKeyRadio.Checked += (s, args) =>
                {
                    passwordRow.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    sshKeyPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                };

                // 프로토콜 변경 시 인증 방식 표시/숨김
                if (!isEdit)
                {
                    protocolCombo.SelectionChanged += (s, args) =>
                    {
                        var isSftpNow = protocolCombo.SelectedIndex == 0;
                        authMethodRow.Visibility = isSftpNow ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
                        if (!isSftpNow)
                        {
                            authPasswordRadio.IsChecked = true;
                        }
                    };
                }

                // 원격 경로
                pathInput = new TextBox
                {
                    PlaceholderText = "/",
                    Text = isEdit ? existing!.RemotePath : "/"
                };
                dialogPanel.Children.Add(MakeRow("RemotePath", pathInput));

                // 표시 이름
                displayNameInput = new TextBox
                {
                    PlaceholderText = _loc.Get("Optional"),
                    Text = isEdit ? existing!.DisplayName : ""
                };
                dialogPanel.Children.Add(MakeRow("DisplayNameOptional", displayNameInput));

                // 연결 저장 체크박스 (새 연결 모드에서만)
                if (!isEdit)
                {
                    saveCheckBox = new CheckBox { Content = _loc.Get("SaveConnection"), IsChecked = true };
                    dialogPanel.Children.Add(saveCheckBox);
                }
            }

            // 에러 메시지 + ProgressRing
            var errorText = new TextBlock
            {
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
                Margin = new Thickness(0, 8, 0, 0),
                FontSize = 13
            };

            var connectingRing = new ProgressRing
            {
                IsActive = false,
                Width = 20,
                Height = 20,
                Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var outerPanel = new StackPanel { Spacing = 0 };
            outerPanel.Children.Add(dialogPanel);
            outerPanel.Children.Add(errorText);
            outerPanel.Children.Add(connectingRing);

            var dialog = new ContentDialog
            {
                Title = isEdit ? _loc.Get("EditConnection").TrimEnd('.') : _loc.Get("ConnectToServer"),
                Content = outerPanel,
                PrimaryButtonText = isEdit ? _loc.Get("Save") : _loc.Get("Connect"),
                CloseButtonText = _loc.Get("Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };
            dialog.Resources["ContentDialogMinWidth"] = 600.0;
            dialog.Resources["ContentDialogMaxWidth"] = 600.0;

            // 연결 결과를 저장할 변수 (Deferral 콜백에서 설정)
            IFileSystemProvider? connectedProvider = null;
            Models.ConnectionInfo? resultConnInfo = null;
            string? resultPassword = null;
            bool resultSaveChecked = false;

            // 편집 모드가 아닐 때: "연결" 클릭 시 다이얼로그 안에서 연결 시도
            if (!isEdit && !isSmbEdit)
            {
                dialog.PrimaryButtonClick += async (s, args) =>
                {
                    var deferral = args.GetDeferral();
                    try
                    {
                        errorText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

                        if (string.IsNullOrWhiteSpace(hostInput!.Text))
                        {
                            errorText.Text = _loc.Get("Host");
                            errorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                            args.Cancel = true;
                            return;
                        }

                        var protocol = (Models.RemoteProtocol)protocolCombo!.SelectedIndex;
                        var isSshKeyAuth = authSshKeyRadio?.IsChecked == true && protocol == Models.RemoteProtocol.SFTP;
                        var connInfo = new Models.ConnectionInfo
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            DisplayName = !string.IsNullOrWhiteSpace(displayNameInput!.Text)
                                ? displayNameInput.Text.Trim()
                                : $"{hostInput.Text.Trim()}:{(int)portInput!.Value}",
                            Protocol = protocol,
                            Host = hostInput.Text.Trim(),
                            Port = (int)portInput!.Value,
                            Username = usernameInput!.Text.Trim(),
                            AuthMethod = isSshKeyAuth ? Models.AuthMethod.SshKey : Models.AuthMethod.Password,
                            SshKeyPath = isSshKeyAuth ? sshKeyPathInput?.Text.Trim() : null,
                            RemotePath = string.IsNullOrWhiteSpace(pathInput!.Text) ? "/" : pathInput.Text.Trim(),
                            LastConnected = DateTime.Now
                        };
                        var credential = isSshKeyAuth ? passphraseInput?.Password : passwordInput!.Password;

                        // UI 상태: 연결 중
                        connectingRing.IsActive = true;
                        connectingRing.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                        dialog.IsPrimaryButtonEnabled = false;

                        var (provider, error) = await TryConnectAsync(connInfo, credential);

                        connectingRing.IsActive = false;
                        connectingRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                        dialog.IsPrimaryButtonEnabled = true;

                        if (error != null)
                        {
                            // 실패: 에러 표시 + 다이얼로그 유지
                            errorText.Text = error;
                            errorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                            args.Cancel = true;
                            return;
                        }

                        // 성공: 결과 저장 → 다이얼로그 닫힘 허용
                        connectedProvider = provider;
                        resultConnInfo = connInfo;
                        resultPassword = credential;
                        resultSaveChecked = saveCheckBox?.IsChecked == true;
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                };
            }

            var result = await ShowContentDialogSafeAsync(dialog);

            if (result != ContentDialogResult.Primary)
                return (result, null, null, false, null);

            // 편집 모드가 아닐 때: Deferral에서 이미 연결 완료됨
            if (!isEdit && !isSmbEdit)
                return (result, resultConnInfo, resultPassword, resultSaveChecked, connectedProvider);

            if (isSmbEdit)
            {
                var updated = new Models.ConnectionInfo
                {
                    Id = existing!.Id,
                    Protocol = Models.RemoteProtocol.SMB,
                    DisplayName = !string.IsNullOrWhiteSpace(smbDisplayNameInput!.Text)
                        ? smbDisplayNameInput.Text.Trim()
                        : (smbUncPathInput!.Text.Trim()),
                    UncPath = smbUncPathInput!.Text.Trim(),
                    Host = existing.Host,
                    Port = existing.Port,
                    Username = existing.Username,
                    RemotePath = existing.RemotePath,
                    LastConnected = existing.LastConnected
                };
                return (result, updated, null, false, null);
            }

            // 편집 모드: 연결 시도 없이 정보만 반환
            if (string.IsNullOrWhiteSpace(hostInput!.Text))
                return (ContentDialogResult.None, null, null, false, null);

            var editProtocol = (Models.RemoteProtocol)protocolCombo!.SelectedIndex;
            var editIsSshKey = authSshKeyRadio?.IsChecked == true && editProtocol == Models.RemoteProtocol.SFTP;
            var connInfoResult = new Models.ConnectionInfo
            {
                Id = existing!.Id,
                DisplayName = !string.IsNullOrWhiteSpace(displayNameInput!.Text)
                    ? displayNameInput.Text.Trim()
                    : $"{hostInput.Text.Trim()}:{(int)portInput!.Value}",
                Protocol = editProtocol,
                Host = hostInput.Text.Trim(),
                Port = (int)portInput!.Value,
                Username = usernameInput!.Text.Trim(),
                AuthMethod = editIsSshKey ? Models.AuthMethod.SshKey : Models.AuthMethod.Password,
                SshKeyPath = editIsSshKey ? sshKeyPathInput?.Text.Trim() : null,
                RemotePath = string.IsNullOrWhiteSpace(pathInput!.Text) ? "/" : pathInput.Text.Trim(),
                LastConnected = existing.LastConnected
            };

            var editCredential = editIsSshKey ? passphraseInput?.Password : passwordInput!.Password;
            return (result, connInfoResult, editCredential, false, null);
        }

        /// <summary>
        /// 네트워크 바로가기의 FTP URL 클릭 시: URL 파싱 → 기존 연결 검색 → 없으면 등록 다이얼로그 표시.
        /// </summary>
        private async void OnNetworkShortcutFtpRequested(object? sender, string ftpUrl)
        {
            try
            {
                var uri = new Uri(ftpUrl);
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 21;
                var username = string.IsNullOrEmpty(uri.UserInfo) ? "" : Uri.UnescapeDataString(uri.UserInfo);
                var remotePath = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
                var isFtps = ftpUrl.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase);

                // 기존 SavedConnections에서 같은 호스트+포트 연결 검색
                var existing = ViewModel.SavedConnections.FirstOrDefault(c =>
                    c.Host.Equals(host, StringComparison.OrdinalIgnoreCase) &&
                    c.Port == port &&
                    (c.Protocol == Models.RemoteProtocol.FTP || c.Protocol == Models.RemoteProtocol.FTPS));

                if (existing != null)
                {
                    // 이미 저장된 연결 → 기존 흐름으로 연결
                    await HandleRemoteConnectionTapped(existing.Id);
                    return;
                }

                // 새 연결: URL 정보를 미리 채운 등록 다이얼로그 표시
                var prefilled = new Models.ConnectionInfo
                {
                    DisplayName = host,
                    Protocol = isFtps ? Models.RemoteProtocol.FTPS : Models.RemoteProtocol.FTP,
                    Host = host,
                    Port = port,
                    Username = username,
                    RemotePath = remotePath
                };

                var (result, connInfo, password, _, provider) = await ShowConnectionDialog(prefilled);
                if (result != ContentDialogResult.Primary || connInfo == null || provider == null) return;

                // 네트워크 바로가기에서 온 연결은 항상 저장
                await OnConnectionSuccess(connInfo, password, true, provider);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[NetworkShortcutFtp] Error: {ex.Message}");
                ViewModel.ShowToast(string.Format(_loc?.Get("Toast_FtpParseFailed") ?? "FTP URL parse failed: {0}", ex.Message));
            }
        }

        /// <summary>
        /// 서버 연결 버튼 탭 이벤트.
        /// 연결 대화상자를 표시하고, 사용자가 입력한 연결 정보로
        /// 원격 서버(SFTP/FTP/SMB) 연결을 시도하고, 성공 시 저장한다.
        /// </summary>
        private async void OnConnectToServerTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var (result, connInfo, password, saveChecked, provider) = await ShowConnectionDialog(null);
            if (result != ContentDialogResult.Primary || connInfo == null || provider == null) return;
            await OnConnectionSuccess(connInfo, password, saveChecked, provider);
        }

        /// <summary>
        /// 원격 연결 시도. 성공 시 (provider, null) 반환, 실패 시 (null, 에러메시지) 반환.
        /// </summary>
        private async Task<(IFileSystemProvider? provider, string? error)> TryConnectAsync(Models.ConnectionInfo connInfo, string? password)
        {
            // SSH 키 파일 사전 검증
            if (connInfo.AuthMethod == Models.AuthMethod.SshKey)
            {
                if (string.IsNullOrWhiteSpace(connInfo.SshKeyPath))
                    return (null, _loc.Get("Error_SshKeyNotSpecified"));
                if (!System.IO.File.Exists(connInfo.SshKeyPath))
                    return (null, string.Format(_loc.Get("Error_SshKeyNotFound"), connInfo.SshKeyPath));
            }

            Helpers.DebugLogger.Log($"[Network] 서버 연결 시도: {connInfo.ToUri()}");
            try
            {
                if (connInfo.Protocol == Models.RemoteProtocol.SFTP)
                {
                    var sftp = new SftpProvider();
                    try
                    {
                        await sftp.ConnectAsync(connInfo, password ?? "");
                        if (!sftp.IsConnected) throw new Exception(_loc.Get("Error_ConnectionFailed"));
                    }
                    catch
                    {
                        try { sftp.Dispose(); } catch { }
                        throw;
                    }
                    return (sftp, null);
                }
                else
                {
                    var ftp = new FtpProvider();
                    try
                    {
                        await ftp.ConnectAsync(connInfo, password ?? "");
                        if (!ftp.IsConnected) throw new Exception(_loc.Get("Error_ConnectionFailed"));
                    }
                    catch
                    {
                        try { ftp.Dispose(); } catch { }
                        throw;
                    }
                    return (ftp, null);
                }
            }
            catch (Renci.SshNet.Common.SshPassPhraseNullOrEmptyException)
            {
                return (null, _loc.Get("Error_SshPassphraseRequired"));
            }
            catch (InvalidDataException)
            {
                return (null, _loc.Get("Error_SshKeyInvalid"));
            }
            catch (Renci.SshNet.Common.SshAuthenticationException ex)
            {
                return (null, string.Format(_loc.Get("Toast_AuthFailed"), ex.Message));
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                return (null, string.Format(_loc.Get("Toast_SocketError"), connInfo.Host, connInfo.Port, ex.Message));
            }
            catch (TimeoutException ex)
            {
                return (null, string.Format(_loc.Get("Toast_TimeoutError"), ex.Message));
            }
            catch (Exception ex)
            {
                return (null, string.Format(_loc.Get("Toast_ConnectionError"), ex.Message));
            }
        }

        /// <summary>
        /// 연결 성공 후 저장 + Router 등록 + 탐색.
        /// </summary>
        private async Task OnConnectionSuccess(Models.ConnectionInfo connInfo, string? password, bool saveChecked, IFileSystemProvider provider)
        {
            var connService = App.Current.Services.GetRequiredService<ConnectionManagerService>();
            var router = App.Current.Services.GetRequiredService<FileSystemRouter>();
            var uriPrefix = FileSystemRouter.GetUriPrefix(connInfo.ToUri());

            if (saveChecked)
            {
                connService.AddConnection(connInfo);
                if (!string.IsNullOrEmpty(password))
                    connService.SaveCredential(connInfo.Id, password);
            }

            router.RegisterConnection(uriPrefix, provider);
            connInfo.LastConnected = DateTime.Now;
            if (saveChecked)
                _ = connService.SaveConnectionsAsync();

            ViewModel.ShowToast(string.Format(_loc.Get("Toast_Connected"), connInfo.DisplayName));

            // 사이드바 갱신 (잠금 뱃지 제거 + 중복 제거)
            ViewModel.RefreshDrives();

            if (ViewModel.CurrentViewMode == ViewMode.Home)
                ViewModel.SwitchViewMode(ViewMode.MillerColumns);

            if (ViewModel.ActiveExplorer != null) await ViewModel.ActiveExplorer.NavigateToPath(connInfo.ToUri());
            FocusColumnAsync(0);
        }

        /// <summary>
        /// 저장된 원격 연결 항목 탭 이벤트.
        /// 선택된 연결 정보로 원격 서버에 재연결한다.
        /// </summary>
        private async void OnSavedConnectionTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.DataContext is Models.ConnectionInfo connInfo)
                {
                    Helpers.DebugLogger.Log($"[Sidebar] 저장된 연결 탭: {connInfo.DisplayName}");
                    await HandleRemoteConnectionTapped(connInfo.Id);
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[Sidebar] OnSavedConnectionTapped error: {ex.Message}");
            }
        }

        /// <summary>
        /// 사이드바 빈 공간 우클릭 → 네트워크/서버 연결 컨텍스트 메뉴
        /// </summary>
        private void OnSidebarEmptyRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            // 드라이브 아이템 위에서 우클릭한 경우는 스킵 (OnSidebarDriveRightTapped이 처리)
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is DriveItem)
                return;

            var flyout = new MenuFlyout();
            ApplyLumiFlyoutStyle(flyout);

            var currentFontFamily = new Microsoft.UI.Xaml.Media.FontFamily(
                Services.IconService.Current?.FontFamilyPath ?? "/Assets/Fonts/remixicon.ttf#remixicon");
            var browseNetwork = new MenuFlyoutItem
            {
                Text = _loc.Get("NetworkBrowse") + "...",
                Icon = new FontIcon
                {
                    Glyph = Services.IconService.Current?.NetworkGlyph ?? "\uEDD4",
                    FontFamily = currentFontFamily,
                    FontSize = 16
                }
            };
            browseNetwork.Click += (s, args) => OnBrowseNetworkTapped(s, null!);
            flyout.Items.Add(browseNetwork);

            var connectServer = new MenuFlyoutItem
            {
                Text = _loc.Get("ConnectToServer") + "...",
                Icon = new FontIcon
                {
                    Glyph = Services.IconService.Current?.ServerGlyph ?? "\uEE71",
                    FontFamily = currentFontFamily,
                    FontSize = 16
                }
            };
            connectServer.Click += (s, args) => OnConnectToServerTapped(s, null!);
            flyout.Items.Add(connectServer);

            flyout.ShowAt(sender as FrameworkElement, e.GetPosition(sender as UIElement));
        }

        /// <summary>
        /// 원격 연결 드라이브 클릭 처리 (ConnectionId로 저장된 연결 정보 조회 → 비밀번호 확인 → 연결)
        /// </summary>
        private async Task HandleRemoteConnectionTapped(string connectionId)
        {
            var connService = App.Current.Services.GetRequiredService<ConnectionManagerService>();
            var connInfo = ViewModel.SavedConnections.FirstOrDefault(c => c.Id == connectionId);
            if (connInfo == null)
            {
                Helpers.DebugLogger.Log($"[Sidebar] 연결 정보를 찾을 수 없음: {connectionId}");
                ViewModel.ShowToast(_loc.Get("Toast_ConnectionNotFound"));
                return;
            }

            // SMB 연결: 비밀번호/프로세스 없이 UNC 경로로 직접 탐색
            if (connInfo.Protocol == Models.RemoteProtocol.SMB && !string.IsNullOrEmpty(connInfo.UncPath))
            {
                Helpers.DebugLogger.Log($"[Sidebar] SMB 직접 탐색: {connInfo.UncPath}");
                connInfo.LastConnected = DateTime.Now;
                _ = connService.SaveConnectionsAsync();

                if (ViewModel.CurrentViewMode == ViewMode.Home)
                    ViewModel.SwitchViewMode(ViewMode.MillerColumns);

                if (ViewModel.ActiveExplorer != null) await ViewModel.ActiveExplorer.NavigateToPath(connInfo.UncPath);
                FocusColumnAsync(0);
                return;
            }

            var router = App.Current.Services.GetRequiredService<FileSystemRouter>();
            var uriPrefix = FileSystemRouter.GetUriPrefix(connInfo.ToUri());

            // 이미 연결된 경우: 바로 네비게이션
            if (router.GetConnectionForPath(uriPrefix + "/") != null)
            {
                Helpers.DebugLogger.Log($"[Sidebar] 기존 연결 재사용: {connInfo.DisplayName}");

                if (ViewModel.CurrentViewMode == ViewMode.Home)
                    ViewModel.SwitchViewMode(ViewMode.MillerColumns);

                if (ViewModel.ActiveExplorer != null) await ViewModel.ActiveExplorer.NavigateToPath(connInfo.ToUri());
                FocusColumnAsync(0);
                return;
            }

            var savedPassword = connService.LoadCredential(connInfo.Id);
            IFileSystemProvider? provider = null;

            if (string.IsNullOrEmpty(savedPassword))
            {
                // 비밀번호 입력 대화상자 (Deferral 패턴 — 실패 시 창 유지)
                var dialogPanel = new StackPanel { Spacing = 8, MinWidth = 320 };
                var passwordInput = new PasswordBox { PlaceholderText = _loc.Get("Password") };
                dialogPanel.Children.Add(passwordInput);

                var errorText = new TextBlock
                {
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                    Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
                    FontSize = 13
                };
                dialogPanel.Children.Add(errorText);

                var connectingRing = new ProgressRing
                {
                    IsActive = false, Width = 20, Height = 20,
                    Visibility = Microsoft.UI.Xaml.Visibility.Collapsed
                };
                dialogPanel.Children.Add(connectingRing);

                var dialog = new ContentDialog
                {
                    Title = string.Format(_loc.Get("ConnectionTitle"), connInfo.DisplayName),
                    Content = dialogPanel,
                    PrimaryButtonText = _loc.Get("Connect"),
                    CloseButtonText = _loc.Get("Cancel"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };

                dialog.PrimaryButtonClick += async (s, args) =>
                {
                    var deferral = args.GetDeferral();
                    try
                    {
                        errorText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                        connectingRing.IsActive = true;
                        connectingRing.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                        dialog.IsPrimaryButtonEnabled = false;

                        var (p, error) = await TryConnectAsync(connInfo, passwordInput.Password);

                        connectingRing.IsActive = false;
                        connectingRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                        dialog.IsPrimaryButtonEnabled = true;

                        if (error != null)
                        {
                            errorText.Text = error;
                            errorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                            args.Cancel = true;
                            return;
                        }

                        provider = p;
                        savedPassword = passwordInput.Password;
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                };

                var result = await ShowContentDialogSafeAsync(dialog);
                if (result != ContentDialogResult.Primary || provider == null) return;
            }
            else
            {
                // 저장된 비밀번호로 자동 연결 시도
                Helpers.DebugLogger.Log($"[Sidebar] 원격 연결 시도: {connInfo.DisplayName}");
                var (p, error) = await TryConnectAsync(connInfo, savedPassword);
                if (error != null)
                {
                    await ShowRemoteConnectionError(connInfo, error);
                    return;
                }
                provider = p;
            }

            // 연결 성공 → Router에 등록 + 네비게이션
            router.RegisterConnection(uriPrefix, provider!);
            connInfo.LastConnected = DateTime.Now;
            _ = connService.SaveConnectionsAsync();

            ViewModel.ShowToast(string.Format(_loc.Get("Toast_Connected"), connInfo.DisplayName));

            // Home 모드면 Miller로 전환 후 네비게이션
            if (ViewModel.CurrentViewMode == ViewMode.Home)
                ViewModel.SwitchViewMode(ViewMode.MillerColumns);

            if (ViewModel.ActiveExplorer != null) await ViewModel.ActiveExplorer.NavigateToPath(connInfo.ToUri());
            FocusColumnAsync(0);
        }

        /// <summary>
        /// 반환된 원격 연결 오류를 사용자에게 토스트 메시지로 표시한다.
        /// </summary>
        /// <param name="connInfo">연결 정보 객체.</param>
        /// <param name="detail">표시할 오류 상세 메시지.</param>
        private async Task ShowRemoteConnectionError(Models.ConnectionInfo connInfo, string detail)
        {
            Helpers.DebugLogger.Log($"[Network] 연결 실패: {connInfo.DisplayName} - {detail}");
            var errorDialog = new ContentDialog
            {
                Title = _loc.Get("ConnectionFailed"),
                Content = detail,
                CloseButtonText = _loc.Get("OK"),
                XamlRoot = this.Content.XamlRoot
            };
            await ShowContentDialogSafeAsync(errorDialog);
        }

        /// <summary>
        /// ContentDialog를 안전하게 표시한다.
        /// 이미 다른 ContentDialog가 열려 있으면 COMException을 방지한다.
        /// </summary>
        private async Task<ContentDialogResult> ShowContentDialogSafeAsync(ContentDialog dialog)
        {
            if (_isContentDialogOpen)
            {
                Helpers.DebugLogger.Log("[Dialog] ContentDialog 중복 열기 방지 — 이미 열려 있음");
                return ContentDialogResult.None;
            }

            // S-3.36 (옵션 A): ContentDialog의 비-액센트 버튼에 LumiSecondaryButtonStyle을 명시 적용.
            // 이 헬퍼를 거치는 모든 dialog가 자동으로 통일된 Lumi 스타일을 가짐.
            ApplyLumiDialogStyle(dialog);

            // WinUI 3 XYFocusNavigation 버그 방지: 화살표 키로 다이얼로그 밖으로 포커스 탈출 차단
            dialog.KeyDown += Dialog_SuppressArrowKeys;

            _isContentDialogOpen = true;
            try
            {
                return await dialog.ShowAsync();
            }
            finally
            {
                _isContentDialogOpen = false;
                dialog.KeyDown -= Dialog_SuppressArrowKeys;
            }
        }

        private static void Dialog_SuppressArrowKeys(object sender, KeyRoutedEventArgs e)
        {
            // TextBox/PasswordBox 내 화살표 키는 커서 이동에 필요 → 허용
            if (e.OriginalSource is TextBox or PasswordBox) return;

            if (e.Key is Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Right
                     or Windows.System.VirtualKey.Up or Windows.System.VirtualKey.Down)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 홈 항목 탭 이벤트. Home 뷰 모드로 전환한다.
        /// </summary>
        private void OnHomeItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ViewModel.SwitchViewMode(ViewMode.Home);
            Helpers.DebugLogger.Log("[Sidebar] Home tapped");
        }

        // =================================================================
        //  Sidebar Favorites Tree (TreeView with lazy-loaded subfolders)
        // =================================================================

        /// <summary>
        /// 즐겨찾기 사이드바의 표시 모드(Tree/Flat)를 설정에 따라 적용한다.
        /// </summary>
        /// <param name="showTree">true면 트리 모드, false면 플랫 리스트 모드를 표시한다.</param>
        internal void ApplySidebarSectionVisibility()
        {
            var v = Microsoft.UI.Xaml.Visibility.Visible;
            var c = Microsoft.UI.Xaml.Visibility.Collapsed;
            SidebarHomeSection.Visibility = _settings.SidebarShowHome ? v : c;
            SidebarFavoritesSection.Visibility = _settings.SidebarShowFavorites ? v : c;
            SidebarLocalDrivesSection.Visibility = _settings.SidebarShowLocalDrives ? v : c;
            SidebarCloudSection.Visibility = _settings.SidebarShowCloud ? v : c;
            SidebarNetworkSection.Visibility = _settings.SidebarShowNetwork ? v : c;
            SidebarRecycleBinSection.Visibility = _settings.SidebarShowRecycleBin ? v : c;
        }

        private void ApplyFavoritesTreeMode(bool showTree)
        {
            FavoritesTreeView.Visibility = showTree
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
            FavoritesFlatList.Visibility = showTree
                ? Microsoft.UI.Xaml.Visibility.Collapsed
                : Microsoft.UI.Xaml.Visibility.Visible;
        }

        /// <summary>
        /// 즐겨찾기 Flat 목록의 항목 탭 이벤트.
        /// 해당 즐겨찾기 경로로 탐색한다.
        /// </summary>
        private void OnFavoritesFlatItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is FavoriteItem fav)
                NavigateToFavorite(fav);
        }

        /// <summary>
        /// 즐겨찾기 Flat 목록의 항목 클릭 이벤트.
        /// ItemClick 이벤트를 통해 해당 경로로 탐색한다.
        /// </summary>
        private void OnFavoritesFlatItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FavoriteItem fav)
                NavigateToFavorite(fav);
        }

        /// <summary>
        /// 즐겨찾기 경로로 탐색을 실행한다.
        /// Home/ActionLog 모드인 경우 ResolveViewModeFromHome()으로 이전 뷰 모드를 복원한 후 탐색하므로,
        /// 사용자가 Details/List/Icon 모드를 사용 중이었다면 해당 모드가 유지된다.
        /// MillerColumns 모드이면 탐색 후 첫 컬럼에 포커스를 이동한다.
        /// </summary>
        /// <param name="fav">탐색할 즐겨찾기 항목.</param>
        private async void NavigateToFavorite(FavoriteItem fav)
        {
            try
            {
                if (!string.IsNullOrEmpty(fav.Path) && System.IO.Directory.Exists(fav.Path))
                {
                    var activeViewMode = (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                        ? ViewModel.RightViewMode : ViewModel.CurrentViewMode;
                    if (activeViewMode == ViewMode.Home || activeViewMode == ViewMode.RecycleBin)
                    {
                        ViewModel.SwitchViewMode(ViewModel.ResolveViewModeFromHome());
                    }

                    var folder = new FolderItem
                    {
                        Name = System.IO.Path.GetFileName(fav.Path) ?? fav.Path,
                        Path = fav.Path
                    };
                    _ = ViewModel.ActiveExplorer?.NavigateTo(folder);
                    if (ViewModel.CurrentViewMode == ViewMode.MillerColumns)
                        FocusColumnAsync(0);
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[Navigation] NavigateToFavorite error: {ex.Message}");
            }
        }

        /// <summary>
        /// 즐겨찾기 Flat 목록 항목 우클릭 이벤트.
        /// 즐겨찾기 컨텍스트 메뉴를 표시한다.
        /// </summary>
        private void OnFavoritesFlatItemRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is FavoriteItem fav)
            {
                var flyout = _contextMenuService.BuildFavoriteMenu(fav, this);
                ApplyLumiFlyoutStyle(flyout);
                flyout.ShowAt(fe, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                {
                    Position = e.GetPosition(fe)
                });
                e.Handled = true;
            }
        }

        /// <summary>
        /// 즐겨찾기 Flat 목록 빈 영역 우클릭 이벤트.
        /// 폴더 추가 컨텍스트 메뉴를 표시한다.
        /// </summary>
        private void OnFavoritesFlatListRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            // ListView의 우클릭 → 클릭된 아이템에서 컨텍스트 메뉴 표시
            if (e.OriginalSource is FrameworkElement fe)
            {
                var fav = FindParentDataContext<FavoriteItem>(fe);
                if (fav != null)
                {
                    var flyout = _contextMenuService.BuildFavoriteMenu(fav, this);
                    flyout.ShowAt(fe, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                    {
                        Position = e.GetPosition(fe)
                    });
                    e.Handled = true;
                }
            }
        }

        private static T? FindParentDataContext<T>(FrameworkElement fe) where T : class
        {
            var current = fe;
            while (current != null)
            {
                if (current.DataContext is T item) return item;
                current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current) as FrameworkElement;
            }
            return null;
        }

        private void OnFavoritesDragCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            // 드래그 리오더 완료 후 즐겨찾기 저장
            var favService = App.Current.Services.GetService(typeof(Services.IFavoritesService)) as Services.IFavoritesService;
            favService?.SaveFavorites(ViewModel.Favorites.ToList());
            Helpers.DebugLogger.Log($"[Favorites] Reordered and saved ({ViewModel.Favorites.Count} items)");
        }

        /// <summary>
        /// Populate the favorites TreeView from ViewModel.Favorites.
        /// Each root node is a FavoriteItem; child nodes (subfolders) are lazily loaded on expand.
        /// </summary>
        private void PopulateFavoritesTree()
        {
            FavoritesTreeView.RootNodes.Clear();
            foreach (var fav in ViewModel.Favorites)
            {
                var node = new TreeViewNode
                {
                    Content = fav,
                    HasUnrealizedChildren = HasSubfolders(fav.Path)
                };
                FavoritesTreeView.RootNodes.Add(node);
            }
        }

        /// <summary>
        /// Repopulate the tree when the Favorites collection changes (add/remove).
        /// </summary>
        private void OnFavoritesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isClosed) return;
            PopulateFavoritesTree();
        }

        /// <summary>
        /// Check if a directory path has any visible subfolders (for expand chevron).
        /// </summary>
        private static bool HasSubfolders(string path)
        {
            try
            {
                if (!System.IO.Directory.Exists(path)) return false;
                foreach (var dir in System.IO.Directory.EnumerateDirectories(path))
                {
                    try
                    {
                        var info = new System.IO.DirectoryInfo(dir);
                        if ((info.Attributes & System.IO.FileAttributes.Hidden) != 0) continue;
                        if ((info.Attributes & System.IO.FileAttributes.System) != 0) continue;
                        return true; // Found at least one visible subfolder
                    }
                    catch { continue; }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Lazy-load child subfolders when a tree node is expanded.
        /// </summary>
        private void OnFavoritesTreeExpanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (!args.Node.HasUnrealizedChildren) return;
            args.Node.HasUnrealizedChildren = false;

            var path = GetPathFromNode(args.Node);
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var dirs = System.IO.Directory.GetDirectories(path);
                Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
                foreach (var dir in dirs)
                {
                    try
                    {
                        var info = new System.IO.DirectoryInfo(dir);
                        if ((info.Attributes & System.IO.FileAttributes.Hidden) != 0) continue;
                        if ((info.Attributes & System.IO.FileAttributes.System) != 0) continue;

                        var childNode = new TreeViewNode
                        {
                            Content = new SidebarFolderNode
                            {
                                Name = info.Name,
                                Path = dir,
                                IconGlyph = Services.IconService.Current?.FolderGlyph ?? "\uED53"
                            },
                            HasUnrealizedChildren = true // Assume subfolders may exist; checked lazily on next expand
                        };
                        args.Node.Children.Add(childNode);
                    }
                    catch { /* Skip inaccessible directories */ }
                }
            }
            catch { }
        }

        /// <summary>
        /// Navigate to the folder when a tree item is invoked (clicked).
        /// </summary>
        private void OnFavoritesTreeItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var path = "";
            // InvokedItem may be the TreeViewNode (manual RootNodes mode) or the Content directly
            if (args.InvokedItem is TreeViewNode node)
            {
                path = GetPathFromNode(node);
            }
            else if (args.InvokedItem is FavoriteItem fav)
            {
                path = fav.Path;
            }
            else if (args.InvokedItem is SidebarFolderNode sfn)
            {
                path = sfn.Path;
            }

            if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
            {
                // Switch away from Home mode if needed (ActionLog has its own sidebar, no navigation)
                var activeViewMode = (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                    ? ViewModel.RightViewMode : ViewModel.CurrentViewMode;
                if (activeViewMode == ViewMode.Home)
                {
                    ViewModel.SwitchViewMode(ViewMode.MillerColumns);
                }

                var folder = new FolderItem
                {
                    Name = System.IO.Path.GetFileName(path) ?? path,
                    Path = path
                };
                _ = ViewModel.ActiveExplorer?.NavigateTo(folder);
                FocusColumnAsync(0);
                Helpers.DebugLogger.Log($"[Sidebar] Favorites tree item invoked: {path}");
            }
        }

        /// <summary>
        /// Extract the file system path from a TreeViewNode's content.
        /// </summary>
        private static string GetPathFromNode(TreeViewNode node)
        {
            if (node.Content is FavoriteItem fav)
                return fav.Path;
            if (node.Content is SidebarFolderNode sfn)
                return sfn.Path;
            return string.Empty;
        }

        /// <summary>
        /// Right-click context menu for favorites tree items.
        /// Root items (FavoriteItem) show the favorite context menu.
        /// Child items (SidebarFolderNode) navigate to the folder and offer basic folder actions.
        /// </summary>
        private void OnFavoritesTreeRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            // Find the TreeViewItem that was right-clicked
            var element = e.OriginalSource as DependencyObject;
            TreeViewItem? treeViewItem = null;
            while (element != null)
            {
                if (element is TreeViewItem tvi)
                {
                    treeViewItem = tvi;
                    break;
                }
                element = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
            }

            if (treeViewItem == null) return;

            // TreeViewItem.DataContext is TreeViewNode; the actual model is in TreeViewNode.Content
            var dataContext = treeViewItem.DataContext;
            object? content = dataContext is Microsoft.UI.Xaml.Controls.TreeViewNode tvNode ? tvNode.Content : dataContext;

            if (content is FavoriteItem favorite)
            {
                var flyout = _contextMenuService.BuildFavoriteMenu(favorite, this);
                flyout.ShowAt(treeViewItem, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                {
                    Position = e.GetPosition(treeViewItem)
                });
                e.Handled = true;
            }
            else if (content is SidebarFolderNode folderNode)
            {
                // Build a simple context menu for subfolder nodes
                var menu = new MenuFlyout();

                var openItem = new MenuFlyoutItem
                {
                    Text = _loc.Get("Open"),
                    Icon = new FontIcon { Glyph = "\uE8E5" }
                };
                openItem.Click += (s, a) =>
                {
                    if (System.IO.Directory.Exists(folderNode.Path))
                    {
                        var folder = new FolderItem
                        {
                            Name = folderNode.Name,
                            Path = folderNode.Path
                        };
                        _ = ViewModel.ActiveExplorer?.NavigateTo(folder);
                        FocusColumnAsync(0);
                    }
                };
                menu.Items.Add(openItem);
                menu.Items.Add(new MenuFlyoutSeparator());

                var addFavItem = new MenuFlyoutItem
                {
                    Text = _loc.Get("AddToFavorites"),
                    Icon = new FontIcon { Glyph = "\uE734" }
                };
                addFavItem.Click += (s, a) => ViewModel.AddToFavorites(folderNode.Path);
                menu.Items.Add(addFavItem);
                menu.Items.Add(new MenuFlyoutSeparator());

                var copyPathItem = new MenuFlyoutItem
                {
                    Text = _loc.Get("CopyPath"),
                    Icon = new FontIcon { Glyph = "\uE8C8" }
                };
                copyPathItem.Click += (s, a) =>
                {
                    var shellService = App.Current.Services.GetRequiredService<ShellService>();
                    shellService.CopyPathToClipboard(folderNode.Path);
                };
                menu.Items.Add(copyPathItem);

                var openExplorerItem = new MenuFlyoutItem
                {
                    Text = _loc.Get("OpenInExplorer"),
                    Icon = new FontIcon { Glyph = "\uED25" }
                };
                openExplorerItem.Click += (s, a) =>
                {
                    var shellService = App.Current.Services.GetRequiredService<ShellService>();
                    shellService.OpenInExplorer(folderNode.Path);
                };
                menu.Items.Add(openExplorerItem);

                menu.ShowAt(treeViewItem, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                {
                    Position = e.GetPosition(treeViewItem)
                });
                e.Handled = true;
            }
        }

        /// <summary>
        /// Miller Column ListView 빈 공간 우클릭 → 빈 영역 컨텍스트 메뉴.
        /// 아이템 위에서의 우클릭은 OnFolderRightTapped/OnFileRightTapped에서 e.Handled=true 처리됨.
        /// </summary>
        private async void OnMillerColumnEmptyAreaRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (e.Handled) return; // 아이템 핸들러가 이미 처리함
            if (!_settings.ShowContextMenu) return;

            if (sender is ListView listView && listView.DataContext is FolderViewModel folderVm)
            {
                bool shiftHeld = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                    Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

                _contextMenuService.SetLastEmptyAreaContext(folderVm.Path, this, listView, e.GetPosition(listView));
                var flyout = await _contextMenuService.BuildEmptyAreaMenuAsync(folderVm.Path, this, forceShellExtensions: shiftHeld);
                flyout.ShowAt(listView, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                {
                    Position = e.GetPosition(listView)
                });
                e.Handled = true;
            }
        }

        // ── Rubber-band selection: attach/detach helpers per column ──

        /// <summary>
        /// 사이드바 ListView(즐겨찾기) 컨테이너 생성 콜백.
        /// 폰트 스케일은 이제 FontScaleService + XAML {Binding} 으로 자동 반영됨 (Phase B/C).
        /// </summary>
        private void OnSidebarContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            // No-op: 스케일은 XAML 바인딩이 처리.
        }

        /// <summary>
        /// Miller Column 콘텐츠 Grid Loaded 이벤트.
        /// 러버밴드(marquee) 선택 헬퍼를 연결하고, 어두운 테마 등의 렌더링 설정을 적용한다.
        /// </summary>
        private void OnMillerColumnContentGridLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Grid grid) return;

            // 밀러 컬럼 폭은 XAML {Binding MillerColumnWidth, Source={StaticResource FontScale}} 으로 자동 반영됨 (Phase B-5).

            if (_rubberBandHelpers.ContainsKey(grid)) return;

            var listView = VisualTreeHelpers.FindChild<ListView>(grid);
            if (listView == null) return;

            var helper = new Helpers.RubberBandSelectionHelper(
                grid,
                listView,
                () => _isSyncingSelection,
                val => _isSyncingSelection = val,
                afterSyncCallback: () => ViewModel.UpdateStatusBar());

            _rubberBandHelpers[grid] = helper;

            // 컬럼 Grid Loaded 시점에 path highlight 리프레시
            // PathHighlightsUpdated 이벤트가 Loaded 전에 발생한 경우를 보완
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    var explorer = ViewModel?.Explorer;
                    explorer?.RefreshPathHighlights();
                    if (ViewModel?.IsSplitViewEnabled == true)
                        ViewModel.RightExplorer?.RefreshPathHighlights();
                }
                catch { /* ignore */ }
            });
        }

        /// <summary>
        /// Miller Column의 각 아이템이 렌더링될 때 호출되는 콜백.
        /// 대량 목록에서 성능 최적화를 위해 Preparing/Idle 페이즈를 처리하고,
        /// 체크박스 모드, 밀도 설정, 썸네일 로딩, 클라우드/Git 상태 주입 등을 수행한다.
        /// </summary>
        private void OnMillerContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            // 긴급 임시 가드: STATUS_STOWED_EXCEPTION 차단
            // 컨테이너 unload race / unloaded folder VM 접근 / stale dict race 등
            // ContainerContentChanging 처리 도중 발생 가능한 모든 throw를 흡수.
            // 근본 원인은 별도 작업으로 추적 (spawn_task: Git.Warm + ContainerContentChanging race).
            try
            {
                // 재활용 큐: 화면 밖 아이템의 썸네일 해제 (메모리 절약)
                if (args.InRecycleQueue)
                {
                    if (args.Item is ViewModels.FileViewModel recycledFile)
                    {
                        try { recycledFile.UnloadThumbnail(); } catch (Exception ex)
                        {
                            Helpers.DebugLogger.Log($"[OnMillerCCC] UnloadThumbnail failed: {ex.Message}");
                        }
                    }
                    return;
                }

                if (args.ItemContainer is ListViewItem item)
                {
                    // Reset any stale padding on the template root Grid (ContentBorder)
                    var rootGrid = VisualTreeHelpers.FindChild<Grid>(item);
                    if (rootGrid != null && rootGrid.Padding != _zeroPadding)
                        rootGrid.Padding = _zeroPadding;

                    // Apply density padding + min height to the DATA TEMPLATE Grid (inside ContentPresenter),
                    // NOT the template root Grid (ContentBorder).
                    // 값이 이미 동일하면 건너뛰어 불필요한 레이아웃 무효화 방지.
                    var cp = VisualTreeHelpers.FindChild<ContentPresenter>(item);
                    if (cp != null)
                    {
                        var grid = VisualTreeHelpers.FindChild<Grid>(cp);
                        if (grid != null)
                        {
                            if (grid.Padding != _densityPadding)
                                grid.Padding = _densityPadding;
                            if (grid.MinHeight != _densityMinHeight)
                                grid.MinHeight = _densityMinHeight;

                            // 폰트/아이콘 스케일은 FontScaleService + XAML {Binding} 으로 자동 반영 (Phase B-5).
                        }
                    }
                }

                // On-demand 썸네일 로딩: 보이는 아이템만 로드
                if (args.Item is ViewModels.FileViewModel fileVm && fileVm.IsThumbnailSupported && !fileVm.HasThumbnail)
                {
                    _ = fileVm.LoadThumbnailAsync();
                }

                // On-demand 클라우드 + Git 상태 주입: 보이는 아이템만
                if (args.Item is ViewModels.FileSystemViewModel fsVm
                    && sender.DataContext is ViewModels.FolderViewModel folderVm)
                {
                    try { folderVm.InjectCloudStateIfNeeded(fsVm); }
                    catch (Exception ex) { Helpers.DebugLogger.Log($"[OnMillerCCC] InjectCloud failed: {ex.Message}"); }
                    try { folderVm.InjectGitStateIfNeeded(fsVm); }
                    catch (Exception ex) { Helpers.DebugLogger.Log($"[OnMillerCCC] InjectGit failed: {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                // STATUS_STOWED_EXCEPTION 차단 — 마지막 안전망
                Helpers.DebugLogger.Log($"[OnMillerCCC] Outer guard caught: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Miller Column 콘텐츠 Grid Unloaded 이벤트.
        /// 러버밴드 선택 헬퍼를 분리하고 리소스를 정리한다.
        /// </summary>
        private void OnMillerColumnContentGridUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Grid grid) return;

            // PathIndicator 정리
            if (_pathIndicators.TryGetValue(grid, out var indicator))
            {
                grid.Children.Remove(indicator);
                _pathIndicators.Remove(grid);
            }
            _prevIndicatorY.Remove(grid.GetHashCode());

            if (_rubberBandHelpers.TryGetValue(grid, out var helper))
            {
                helper.Detach();
                _rubberBandHelpers.Remove(grid);
            }
        }

        /// <summary>
        /// Miller Column에서 폴더 아이템 우클릭 이벤트.
        /// 설정에서 ShowContextMenu가 활성화된 경우 폴더 컨텍스트 메뉴를 표시한다.
        /// </summary>
        private async void OnFolderRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            try
            {
                if (!_settings.ShowContextMenu) return;
                if (sender is Grid grid && grid.DataContext is FolderViewModel folder)
                {
                    e.Handled = true; // Prevent bubbling to empty area handler during await
                    bool shiftHeld = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                        Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

                    _contextMenuService.SetLastMenuContext(folder, this, grid, e.GetPosition(grid));
                    var flyout = await _contextMenuService.BuildFolderMenuAsync(folder, this, forceShellExtensions: shiftHeld);
                    flyout.ShowAt(grid, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                    {
                        Position = e.GetPosition(grid)
                    });
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ContextMenu] OnFolderRightTapped error: {ex.Message}");
            }
        }

        /// <summary>
        /// Miller Column에서 파일 아이템 우클릭 이벤트.
        /// 설정에서 ShowContextMenu가 활성화된 경우 파일 컨텍스트 메뉴를 표시한다.
        /// </summary>
        private async void OnFileRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            try
            {
                if (!_settings.ShowContextMenu) return;
                if (sender is Grid grid && grid.DataContext is FileViewModel file)
                {
                    e.Handled = true; // Prevent bubbling to empty area handler during await
                    bool shiftHeld = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                        Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

                    _contextMenuService.SetLastMenuContext(file, this, grid, e.GetPosition(grid));
                    Helpers.DebugLogger.Log($"[ContextMenu] OnFileRightTapped START: {file.Name} hasThumbnail={file.HasThumbnail}");
                    var flyout = await _contextMenuService.BuildFileMenuAsync(file, this, forceShellExtensions: shiftHeld);
                    Helpers.DebugLogger.Log($"[ContextMenu] OnFileRightTapped BUILT: {file.Name} items={flyout.Items.Count}");
                    flyout.ShowAt(grid, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                    {
                        Position = e.GetPosition(grid)
                    });
                    Helpers.DebugLogger.Log($"[ContextMenu] OnFileRightTapped SHOWN: {file.Name}");
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ContextMenu] OnFileRightTapped error: {ex.Message}");
            }
        }

        /// <summary>
        /// 사이드바 드라이브 항목 우클릭 이벤트.
        /// 드라이브 컨텍스트 메뉴(열기, 꾸내기, 미리보기 등)를 표시한다.
        /// </summary>
        private void OnSidebarDriveRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is DriveItem drive)
            {
                var flyout = _contextMenuService.BuildDriveMenu(drive, this);
                ApplyLumiFlyoutStyle(flyout);
                flyout.ShowAt(grid, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                {
                    Position = e.GetPosition(grid)
                });
                e.Handled = true;
            }
        }

        /// <summary>
        /// S-3.36: MenuFlyout에 Lumi 글래스 스타일 (LumiMenuFlyoutPresenterStyle)을 명시 적용.
        /// WinUI 3 popup이 Application.Resources의 implicit Style을 일부 케이스에서
        /// 무시할 수 있어, 사이드바 컨텍스트 메뉴에서는 명시 할당으로 보장한다.
        /// </summary>
        private void ApplyLumiFlyoutStyle(MenuFlyout flyout)
        {
            try
            {
                if (App.Current.Resources.TryGetValue("LumiMenuFlyoutPresenterStyle", out var styleObj)
                    && styleObj is Microsoft.UI.Xaml.Style style)
                {
                    flyout.MenuFlyoutPresenterStyle = style;
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ApplyLumiFlyoutStyle] failed: {ex.Message}");
            }
        }

        /// <summary>
        /// S-3.36: ContentDialog의 비-액센트 버튼에 LumiSecondaryButtonStyle 적용.
        /// 실제 구현은 Helpers.DialogStyleHelper.ApplyLumiStyle(dlg) 정적 헬퍼.
        /// MainWindow 외부 (ContextMenuService, SettingsModeView 등) 에서도 동일 헬퍼 사용.
        /// </summary>
        private void ApplyLumiDialogStyle(ContentDialog dlg)
            => Helpers.DialogStyleHelper.ApplyLumiStyle(dlg);

        // =================================================================
        //  S-3.35: LumiSidebar Recycle Bin 우클릭 — 열기 + 비우기 컨텍스트 메뉴
        // =================================================================
        private void OnLumiRecycleBinRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;

            var flyout = new MenuFlyout();
            ApplyLumiFlyoutStyle(flyout);
            var iconFontFamily = new Microsoft.UI.Xaml.Media.FontFamily(
                Services.IconService.Current?.FontFamilyPath ?? "/Assets/Fonts/remixicon.ttf#remixicon");

            // 열기 — 기존 OnRecycleBinTapped 재사용 (휴지통 모드 진입)
            var openItem = new MenuFlyoutItem
            {
                Text = _loc.Get("Open"),
                Icon = new FontIcon
                {
                    Glyph = "",
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                    FontSize = 16
                }
            };
            openItem.Click += (s, args) => OnRecycleBinTapped(s, null!);
            flyout.Items.Add(openItem);

            flyout.Items.Add(new MenuFlyoutSeparator());

            // 휴지통 비우기 — 기존 OnRecycleBinEmptyRequested 재사용 (확인 다이얼로그 + 비우기)
            // 비어있는 상태(RecycleBinIsEmpty)면 비활성.
            var emptyItem = new MenuFlyoutItem
            {
                Text = _loc.Get("RecycleBin_Empty"),
                Icon = new FontIcon
                {
                    Glyph = "",
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                    FontSize = 16
                },
                IsEnabled = !ViewModel.RecycleBinIsEmpty
            };
            emptyItem.Click += (s, args) => OnRecycleBinEmptyRequested(s, EventArgs.Empty);
            flyout.Items.Add(emptyItem);

            flyout.ShowAt(fe, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
            {
                Position = e.GetPosition(fe)
            });
            e.Handled = true;
        }

        // =================================================================
        //  Keyboard Handlers -> MainWindow.KeyboardHandler.cs
        //  (OnGlobalKeyDown, OnGlobalPointerPressed, OnMillerKeyDown,
        //   HandleRightArrow, HandleLeftArrow, HandleEnter, HandleTypeAhead,
        //   HandleQuickLook, KeyToChar)
        // =================================================================

        // =================================================================
        //  P1: Clipboard (Ctrl+C/X/V)
        // =================================================================

        // =================================================================
        //  Select All (Ctrl+A)
        // =================================================================


        // =================================================================
        //  Select None (Ctrl+Shift+A)
        // =================================================================


        // =================================================================
        //  Invert Selection (Ctrl+I)
        // =================================================================


        // =================================================================
        //  Helper: Get current selected items (multi or single)
        // =================================================================






        // =================================================================
        //  P1: New Folder (Ctrl+Shift+N)
        // =================================================================


        // =================================================================
        //  P1: Refresh (F5)
        // =================================================================


        // =================================================================
        //  P2: Rename (F2) — 인라인 이름 변경
        // =================================================================









        // =================================================================
        //  P2: Delete (Delete key)
        // =================================================================





        // =================================================================
        //  Search Box
        // =================================================================


        // ── Search Filter State ──



        // =================================================================
        //  P1: Focus Tracking (Active Column)
        // =================================================================

        /// <summary>
        /// Miller Column ListView의 GotFocus 이벤트.
        /// 포커스를 얻은 컬럼의 FolderViewModel을 찾아
        /// Left/Right Pane 활성 상태를 구분하여 ActivePane와 ActiveColumn을 설정한다.
        /// </summary>
        private void OnMillerColumnGotFocus(object sender, RoutedEventArgs e)
        {
            // 리네임 TextBox로 포커스가 간 경우는 제외 (GotFocus 버블링)
            if (e.OriginalSource is not TextBox)
                CancelAnyActiveRename();

            // Clear any active search filter when user focuses a different column
            if (_isSearchFiltered)
            {
                RestoreSearchFilter();
            }

            try
            {
                if (sender is FrameworkElement fe && fe.DataContext is FolderViewModel folderVm)
                {
                    // Detect which pane and set ActivePane + SetActiveColumn
                    if (ViewModel.IsSplitViewEnabled && IsDescendant(RightPaneContainer, fe))
                    {
                        ViewModel.ActivePane = ActivePane.Right;
                        ViewModel.RightExplorer.SetActiveColumn(folderVm);
                    }
                    else
                    {
                        ViewModel.ActivePane = ActivePane.Left;
                        ViewModel.LeftExplorer.SetActiveColumn(folderVm);
                    }

                    // 포커스된 컬럼 기준으로 상태바 갱신
                    ViewModel.UpdateStatusBar();
                }
            }
            catch (System.Runtime.InteropServices.COMException) { }
        }

        /// <summary>
        /// Miller Column Grid의 PointerPressed 이벤트.
        /// 클릭된 컬럼의 FolderViewModel을 찾아 ActivePane와 ActiveColumn을 설정한다.
        /// 빈 공간(ListViewItem 외) 클릭 시 해당 컬럼의 ListView에 키보드 포커스를 이동하여,
        /// 시각적 선택 표시(파란 테두리)와 실제 키보드 포커스를 동기화한다.
        /// </summary>
        private void OnMillerColumnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Grid grid) return;
            try
            {
                var props = e.GetCurrentPoint(grid).Properties;
                if (props.IsMiddleButtonPressed) return;

                // 주소창 편집 모드 해제 — 빈 공간 클릭 시에도 포커스가 이동하지 않으므로 명시적 해제
                DismissAddressBarEditMode();

                // Walk up to find the FolderViewModel DataContext (on the ItemTemplate root Grid)
                var parent = grid;
                while (parent != null && parent.DataContext is not FolderViewModel)
                    parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent) as Grid;
                if (parent?.DataContext is FolderViewModel folderVm)
                {
                    if (ViewModel.IsSplitViewEnabled && IsDescendant(RightPaneContainer, grid))
                    {
                        ViewModel.ActivePane = ActivePane.Right;
                        ViewModel.RightExplorer.SetActiveColumn(folderVm);
                    }
                    else
                    {
                        ViewModel.ActivePane = ActivePane.Left;
                        ViewModel.LeftExplorer.SetActiveColumn(folderVm);
                    }

                    // ★ 빈 공간 클릭 시 ListView에 키보드 포커스 이동
                    // ListViewItem이 아닌 Grid 여백 영역을 클릭한 경우,
                    // ListView 자체에 Programmatic 포커스를 부여하여
                    // 이후 화살표 키 등 키보드 탐색이 즉시 동작하도록 한다.
                    bool clickedOnItem = false;
                    var src = e.OriginalSource as DependencyObject;
                    while (src != null && src != grid)
                    {
                        if (src is ListViewItem) { clickedOnItem = true; break; }
                        src = VisualTreeHelper.GetParent(src);
                    }
                    if (!clickedOnItem)
                    {
                        var listView = VisualTreeHelpers.FindChild<ListView>(parent ?? grid);
                        listView?.Focus(FocusState.Programmatic);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException) { }
        }

        /// <summary>
        /// ListView 선택 변경 시 ViewModel과 명시적으로 동기화.
        /// x:Bind Mode=TwoWay가 복잡한 객체에서 제대로 동작하지 않을 수 있으므로.
        /// </summary>
        private void OnMillerColumnSelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return; // Prevent circular updates

            // 탭 닫기/정리 중에는 이미 visual tree에서 제거된 ListView 접근 금지
            if (_isClosed) return;

            // DataContext 접근이 COMException 발생 가능 (visual tree에서 제거된 ListView)
            ListView? listView;
            FolderViewModel? folderVm;
            try
            {
                listView = sender as ListView;
                folderVm = listView?.DataContext as FolderViewModel;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return; // 이미 visual tree에서 제거됨
            }

            if (listView == null || folderVm == null) return;

            // 다른 항목 선택 시 진행 중인 리네임 취소
            CancelAnyActiveRename();

            if (folderVm != null)
            {
                // Suppress selection sync during bulk Children updates (reload/refresh).
                // SyncChildren may replace the collection, causing ListView to lose selection
                // temporarily. Without this guard, SelectedChild would be nulled and child columns removed.
                if (folderVm.IsBulkUpdating) return;

                _isSyncingSelection = true;
                try
                {
                    // Multi-selection support: sync all selected items
                    if (listView.SelectedItems.Count > 1)
                    {
                        // Multi-selection: use SyncSelectedItems (suppresses navigation)
                        folderVm.SyncSelectedItems(listView.SelectedItems);
                    }
                    else
                    {
                        // Single selection: sync SelectedChild directly for navigation
                        var newSelection = listView.SelectedItem as FileSystemViewModel;
                        if (!ReferenceEquals(folderVm.SelectedChild, newSelection))
                        {
                            folderVm.SelectedChild = newSelection;
                        }
                        else if (newSelection is ViewModels.FolderViewModel clickedFolder)
                        {
                            // Already selected folder clicked again — force navigation
                            // Always re-trigger even if child column exists (e.g. arrow-key pre-selected)
                            folderVm.SelectedChild = null;
                            folderVm.SelectedChild = clickedFolder;
                        }
                        // Keep SelectedItems in sync for single selection too
                        folderVm.SyncSelectedItems(listView.SelectedItems);
                    }

                    // Update preview for the active pane
                    var previewItem = listView.SelectedItems.Count == 1
                        ? listView.SelectedItem as FileSystemViewModel
                        : null;
                    UpdatePreviewForSelection(previewItem);

                    // Update status bar selection count
                    ViewModel.UpdateStatusBar();

                    // Update toolbar button enabled states
                    UpdateToolbarButtonStates();
                }
                finally
                {
                    _isSyncingSelection = false;
                }
            }
        }

        #region Floating Path Indicator Animation

        /// <summary>
        /// 각 컬럼 콘텐츠 Grid → 플로팅 PathIndicator Border 매핑.
        /// OnMillerColumnContentGridLoaded에서 생성, Unloaded에서 제거.
        /// </summary>
        private readonly Dictionary<Grid, Border> _pathIndicators = new();

        /// <summary>
        /// 각 컬럼의 플로팅 인디케이터의 이전 Y 위치를 추적하여 슬라이드 방향 결정에 사용.
        /// Key = content Grid hashcode, Value = previous Y offset.
        /// </summary>
        private readonly Dictionary<int, double> _prevIndicatorY = new();

        /// <summary>
        /// ExplorerViewModel.PathHighlightsUpdated 이벤트 핸들러.
        /// 각 컬럼의 플로팅 인디케이터를 on-path 아이템 위치로 슬라이드 애니메이션.
        /// NavigationView의 SelectionIndicator 이동 효과를 Composition API로 재현.
        /// </summary>
        private void OnPathHighlightsUpdated(ViewModels.ExplorerViewModel sender, Dictionary<int, ViewModels.FileSystemViewModel?> highlightMap)
        {
            // Dispatch to Low priority so it runs after Loaded and layout pass
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    ApplyPathIndicators(sender, highlightMap);
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[PathIndicator] Animation error: {ex.Message}");
                }
            });
        }

        private void ApplyPathIndicators(ViewModels.ExplorerViewModel sender, Dictionary<int, ViewModels.FileSystemViewModel?> highlightMap)
        {
            // 긴급 가드: STATUS_STOWED_EXCEPTION 차단 (별도 이슈 추적 중)
            // ContainerFromIndex / FindChild / TransformToVisual 등이 unloaded visual tree에서
            // native exception throw 시 흡수
            try
            {
                ApplyPathIndicatorsImpl(sender, highlightMap);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogCrash($"[PathIndicator] Outer guard caught: {ex.GetType().Name}: {ex.Message}", ex);
            }
        }

        private void ApplyPathIndicatorsImpl(ViewModels.ExplorerViewModel sender, Dictionary<int, ViewModels.FileSystemViewModel?> highlightMap)
        {
            // Determine which ItemsControl based on sender (left vs right pane)
            ItemsControl control;
            string paneLabel;
            if (sender == ViewModel.RightExplorer)
            {
                control = MillerColumnsControlRight;
                paneLabel = "Right";
            }
            else if (_activeMillerTabId != null && _tabMillerPanels.TryGetValue(_activeMillerTabId, out var panel))
            {
                control = panel.items;
                paneLabel = "Left(tab)";
            }
            else
            {
                control = MillerColumnsControl;
                paneLabel = "Left(fallback)";
            }
            // v1.4.3: dedup — 동일 highlight state면 visual tree 재접근 스킵.
            // native 접근 race surface 축소 (기능 개선, 진단 아님).
            // Signature: "col1=vmHash|col2=vmHash|..."
            var signature = string.Join("|",
                highlightMap.OrderBy(kv => kv.Key)
                            .Select(kv => $"{kv.Key}={(kv.Value == null ? "null" : kv.Value.GetHashCode().ToString())}"));
            if (_lastPathIndicatorSignature.TryGetValue(paneLabel, out var prevSig) && prevSig == signature)
            {
                // 중복 호출 — 스킵
                return;
            }
            _lastPathIndicatorSignature[paneLabel] = signature;

            Helpers.DebugLogger.Log($"[PathIndicator] ApplyPathIndicators pane={paneLabel}, controlNull={control == null}, highlightCount={highlightMap.Count}, controlName={control?.Name}");
            if (control == null) return;

            foreach (var (colIndex, onPathItem) in highlightMap)
            {
                var colContainer = control.ContainerFromIndex(colIndex);
                if (colContainer == null)
                {
                    Helpers.DebugLogger.Log($"[PathIndicator] col={colIndex}: ContainerFromIndex returned NULL");
                    continue;
                }

                // Find ListView inside this column, then get its parent Grid (content grid)
                var listView = VisualTreeHelpers.FindChild<ListView>(colContainer);
                if (listView == null)
                {
                    Helpers.DebugLogger.Log($"[PathIndicator] col={colIndex}: ListView not found in container");
                    continue;
                }
                var contentGrid = listView.Parent as Grid;
                if (contentGrid == null)
                {
                    Helpers.DebugLogger.Log($"[PathIndicator] col={colIndex}: contentGrid is null (parent type={listView.Parent?.GetType().Name})");
                    continue;
                }

                // Get or create indicator for this content grid
                var indicator = GetOrCreateIndicator(contentGrid);

                bool animationsEnabled = _settings.AnimationsEnabled;

                if (onPathItem == null)
                {
                    if (animationsEnabled) AnimateIndicator(indicator, 0, null, null);
                    else SetIndicatorImmediate(indicator, 0, null);
                    continue;
                }

                // Find the ListViewItem container for the on-path item
                var itemContainer = listView.ContainerFromItem(onPathItem) as ListViewItem;
                if (itemContainer == null)
                {
                    Helpers.DebugLogger.Log($"[PathIndicator] col={colIndex}: ContainerFromItem returned NULL for '{onPathItem.Name}', listView.Items.Count={listView.Items.Count}");
                    if (animationsEnabled) AnimateIndicator(indicator, 0, null, null);
                    else SetIndicatorImmediate(indicator, 0, null);
                    continue;
                }
                Helpers.DebugLogger.Log($"[PathIndicator] col={colIndex}: indicator SHOWN for '{onPathItem.Name}' at pane={paneLabel}");

                // Get Y offset of the item relative to the contentGrid (indicator's parent)
                double targetY;
                try
                {
                    var transform = itemContainer.TransformToVisual(contentGrid);
                    var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                    targetY = point.Y + (itemContainer.ActualHeight / 2) - (indicator.Height / 2);
                }
                catch { continue; }

                // Determine animation direction from previous position
                int key = contentGrid.GetHashCode();
                double? fromY = _prevIndicatorY.TryGetValue(key, out var prev) ? prev : null;
                _prevIndicatorY[key] = targetY;

                if (animationsEnabled) AnimateIndicator(indicator, 1, targetY, fromY);
                else SetIndicatorImmediate(indicator, 1, targetY);
            }
        }

        /// <summary>
        /// content Grid에 대한 PathIndicator Border를 가져오거나, 없으면 새로 생성.
        /// Canvas.ZIndex를 높게 설정하여 ListView 위에 렌더링되도록 보장.
        /// </summary>
        private Border GetOrCreateIndicator(Grid contentGrid)
        {
            if (_pathIndicators.TryGetValue(contentGrid, out var existing))
                return existing;

            var indicator = new Border
            {
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top,
                Width = 3,
                Height = 16,
                CornerRadius = new CornerRadius(1.5),
                Margin = new Thickness(3, 0, 0, 0),
                Background = GetThemeBrush("SpanAccentBrush"),
                Opacity = 0,
                IsHitTestVisible = false,
            };
            // Z-index 최상위 — ListView 및 다른 요소 위에 렌더링
            Microsoft.UI.Xaml.Controls.Canvas.SetZIndex(indicator, 100);
            contentGrid.Children.Add(indicator);
            _pathIndicators[contentGrid] = indicator;
            return indicator;
        }

        /// <summary>
        /// Composition API를 사용하여 플로팅 인디케이터를 애니메이션.
        /// opacity=1이면 targetY 위치로 슬라이드, opacity=0이면 페이드아웃.
        /// fromY가 있으면 이전 위치에서 현재 위치로 슬라이드 + 스케일 효과.
        /// </summary>
        private static void AnimateIndicator(Border indicator, double opacity, double? targetY, double? fromY)
        {
            try
            {
                var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(indicator);
                var compositor = visual.Compositor;

                if (opacity <= 0)
                {
                    // Fade out
                    var fadeOut = compositor.CreateScalarKeyFrameAnimation();
                    fadeOut.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(
                        new System.Numerics.Vector2(0.1f, 0.9f), new System.Numerics.Vector2(0.2f, 1f)));
                    fadeOut.Duration = TimeSpan.FromMilliseconds(150);
                    visual.StartAnimation("Opacity", fadeOut);
                    return;
                }

                if (targetY == null) return;

                // Set Translation.Y (the indicator uses VerticalAlignment=Top, so Translation.Y positions it)
                var targetOffset = new System.Numerics.Vector3(3, (float)targetY.Value, 0);

                if (fromY != null && Math.Abs(fromY.Value - targetY.Value) > 2)
                {
                    // Slide animation: move from old position to new position
                    float startY = (float)fromY.Value;
                    float endY = (float)targetY.Value;

                    // Offset animation (slide)
                    var slideAnim = compositor.CreateVector3KeyFrameAnimation();
                    slideAnim.InsertKeyFrame(0f, new System.Numerics.Vector3(3, startY, 0));
                    slideAnim.InsertKeyFrame(1f, new System.Numerics.Vector3(3, endY, 0),
                        compositor.CreateCubicBezierEasingFunction(
                            new System.Numerics.Vector2(0.1f, 0.9f), new System.Numerics.Vector2(0.2f, 1f)));
                    slideAnim.Duration = TimeSpan.FromMilliseconds(250);
                    visual.StartAnimation("Offset", slideAnim);

                    // Fade in (in case it was hidden)
                    var fadeIn = compositor.CreateScalarKeyFrameAnimation();
                    fadeIn.InsertKeyFrame(0f, visual.Opacity);
                    fadeIn.InsertKeyFrame(1f, 1f);
                    fadeIn.Duration = TimeSpan.FromMilliseconds(150);
                    visual.StartAnimation("Opacity", fadeIn);
                }
                else
                {
                    // First appearance or same position: just set offset and fade in
                    visual.Offset = targetOffset;

                    var fadeIn = compositor.CreateScalarKeyFrameAnimation();
                    fadeIn.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(
                        new System.Numerics.Vector2(0.1f, 0.9f), new System.Numerics.Vector2(0.2f, 1f)));
                    fadeIn.Duration = TimeSpan.FromMilliseconds(200);
                    visual.StartAnimation("Opacity", fadeIn);
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[PathIndicator] AnimateIndicator error: {ex.Message}");
            }
        }

        /// <summary>
        /// AnimationsEnabled=OFF 경로. 애니메이션 없이 인디케이터의 최종 상태를 즉시 적용.
        /// 진행 중인 애니메이션을 명시적으로 중단(StopAnimation)하여 후속 대입이 덮이지 않도록 보장.
        /// </summary>
        private static void SetIndicatorImmediate(Border indicator, double opacity, double? targetY)
        {
            try
            {
                var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(indicator);

                // 진행 중 애니메이션이 남아있으면 직접 대입 값을 덮을 수 있음 → 중단 선행
                visual.StopAnimation("Offset");
                visual.StopAnimation("Opacity");

                if (opacity <= 0)
                {
                    visual.Opacity = 0f;
                    return;
                }

                if (targetY == null) return;

                visual.Offset = new System.Numerics.Vector3(3, (float)targetY.Value, 0);
                visual.Opacity = 1f;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[PathIndicator] SetIndicatorImmediate error: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Miller Column 더블 탭 이벤트.
        /// 파일 아이템을 더블 클릭하면 열기 동작을 실행하고,
        /// MillerClickBehavior 설정에 따라 폴더 더블 클릭 시 자동 탐색을 수행한다.
        /// </summary>
        private void OnMillerColumnDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (sender is ListView listView && listView.DataContext is FolderViewModel folderVm)
            {
                var selected = folderVm.SelectedChild;
                if (selected is FileViewModel file)
                {
                    if (Helpers.ArchivePathHelper.IsArchiveFile(file.Path))
                    {
                        // Archive already navigated on selection; double-click is no-op
                        Helpers.DebugLogger.Log($"[MainWindow] Miller Column DoubleClick: Archive {file.Name} (already navigated)");
                    }
                    else if (Helpers.ArchivePathHelper.IsArchivePath(file.Path))
                    {
                        // File inside archive: extract to temp and open
                        _ = OpenArchiveEntryAsync(file.Path);
                        Helpers.DebugLogger.Log($"[MainWindow] Miller Column DoubleClick: Extracting archive entry {file.Name}");
                    }
                    else if (file.Path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    {
                        // .lnk 바로가기: 대상이 폴더면 네비게이션, 파일이면 ShellExecute
                        var target = FileSystemService.ResolveShellLink(file.Path);
                        if (!string.IsNullOrEmpty(target) && System.IO.Directory.Exists(target))
                        {
                            _ = ViewModel.ActiveExplorer.NavigateToPath(target);
                            Helpers.DebugLogger.Log($"[MainWindow] Miller Column DoubleClick: .lnk → navigate to folder {target}");
                        }
                        else
                        {
                            var shellService = App.Current.Services.GetRequiredService<ShellService>();
                            shellService.OpenFile(file.Path);
                            Helpers.DebugLogger.Log($"[MainWindow] Miller Column DoubleClick: .lnk → opening {file.Name}");
                        }
                    }
                    else
                    {
                        // Open file with default application via ShellExecute (faster than WinRT Launcher)
                        var shellService = App.Current.Services.GetRequiredService<ShellService>();
                        shellService.OpenFile(file.Path);
                        Helpers.DebugLogger.Log($"[MainWindow] Miller Column DoubleClick: Opening file {file.Name}");
                    }
                }
                else if (selected is FolderViewModel folder && _settings.MillerClickBehavior == "double")
                {
                    // In double-click mode, navigate into folder as next column (preserve existing columns)
                    var explorer = ViewModel.ActiveExplorer;
                    explorer.NavigateIntoFolder(folder, folderVm);
                    Helpers.DebugLogger.Log($"[MainWindow] Miller Column DoubleClick: Navigating to folder {folder.Name}");
                }
            }
        }

        /// <summary>
        /// 폴더 로드 실패 시 재시도 버튼 클릭 핸들러.
        /// 해당 FolderViewModel의 로드를 다시 시도한다.
        /// </summary>
        private async void OnRetryFolderLoad(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                if (sender is Microsoft.UI.Xaml.Controls.HyperlinkButton btn && btn.Tag is FolderViewModel folder)
                {
                    folder.ResetLoadState();
                    await folder.EnsureChildrenLoadedAsync();
                }
            }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[MainWindow] OnRetryFolderLoad failed: {ex.Message}"); }
        }

        /// <summary>
        /// 현재 활성 뷰에서 선택된 항목들을 반환한다.
        /// Miller Columns 모드에서는 활성 컬럼의 선택 항목을 반환한다.
        /// </summary>
        private FileSystemViewModel? GetCurrentSelected()
        {
            var viewMode = (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                ? ViewModel.RightViewMode : ViewModel.CurrentViewMode;

            if (viewMode != ViewMode.MillerColumns)
            {
                // Details/List/Icon: CurrentFolder에서 선택된 항목을 가져옴
                return ViewModel.ActiveExplorer?.CurrentFolder?.SelectedChild;
            }

            // Miller Columns
            var columns = ViewModel.ActiveExplorer?.Columns;
            if (columns == null) return null;
            int activeIndex = GetActiveColumnIndex();
            if (activeIndex < 0) activeIndex = columns.Count - 1;
            if (activeIndex < 0 || activeIndex >= columns.Count) return null;
            return columns[activeIndex].SelectedChild;
        }





        /// <summary>
        /// 지정된 FolderViewModel에 바인딩된 ListView를 찾아 반환한다.
        /// Miller Column의 컬럼 번호 기반으로 탐색한다.
        /// </summary>
        private ListView? GetListViewForColumn(int columnIndex)
        {
            var control = GetActiveMillerColumnsControl();
            if (control == null) return null;
            var container = control.ContainerFromIndex(columnIndex) as ContentPresenter;
            if (container == null) return null;
            return VisualTreeHelpers.FindChild<ListView>(container);
        }

        /// <summary>
        /// 지정된 UI 요소가 부모 요소의 하위에 있는지 확인한다.
        /// Left/Right Pane 구분에 사용된다.
        /// </summary>
        private static bool IsDescendant(DependencyObject parent, DependencyObject child)
        {
            var current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }



        // ============================================================
        //  Breadcrumb Address Bar 핸들러
        // ============================================================








        // =================================================================
        //  Back/Forward History Dropdown (right-click on nav buttons)
        // =================================================================















        // =================================================================
        // UNIFIED BAR BUTTON HANDLERS
        // =================================================================

        /// <summary>
        /// Update toolbar button enabled/disabled states based on current selection and clipboard.
        /// </summary>
        private void UpdateToolbarButtonStates()
        {
            bool hasSelection = HasAnySelection();
            bool hasClipboard = _clipboardPaths.Count > 0;

            ToolbarCutButton.IsEnabled = hasSelection;
            ToolbarCopyButton.IsEnabled = hasSelection;
            ToolbarPasteButton.IsEnabled = hasClipboard;
            ToolbarRenameButton.IsEnabled = hasSelection;
            ToolbarDeleteButton.IsEnabled = hasSelection;
        }

        /// <summary>
        /// Check if any file/folder is currently selected in the active view.
        /// </summary>
        private bool HasAnySelection()
        {
            var explorer = ViewModel.ActiveExplorer;
            if (explorer == null) return false;

            // Check all columns for any selected item
            foreach (var col in explorer.Columns)
            {
                if (col.SelectedChild != null)
                    return true;
                if (col.SelectedItems != null && col.SelectedItems.Count > 0)
                    return true;
            }
            return false;
        }









        // Sort handlers










        // View mode handlers
        private void OnViewModeMillerColumns(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewMode(Models.ViewMode.MillerColumns);
            UpdateViewModeVisibility();
            UpdateViewModeIcon();
            UpdatePreviewButtonState();
        }

        private void OnViewModeDetails(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewMode(Models.ViewMode.Details);
            UpdateViewModeVisibility();
            UpdateViewModeIcon();
            UpdatePreviewButtonState();
        }

        private void OnViewModeList(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewMode(Models.ViewMode.List);
            UpdateViewModeVisibility();
            UpdateViewModeIcon();
            UpdatePreviewButtonState();
        }

        private void OnViewModeIconExtraLarge(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewMode(Models.ViewMode.IconExtraLarge);
            GetActiveIconView()?.UpdateIconSize(Models.ViewMode.IconExtraLarge);
            UpdateViewModeVisibility();
            UpdateViewModeIcon();
            UpdatePreviewButtonState();
        }

        private void OnViewModeIconLarge(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewMode(Models.ViewMode.IconLarge);
            GetActiveIconView()?.UpdateIconSize(Models.ViewMode.IconLarge);
            UpdateViewModeVisibility();
            UpdateViewModeIcon();
            UpdatePreviewButtonState();
        }

        private void OnViewModeIconMedium(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewMode(Models.ViewMode.IconMedium);
            GetActiveIconView()?.UpdateIconSize(Models.ViewMode.IconMedium);
            UpdateViewModeVisibility();
            UpdateViewModeIcon();
            UpdatePreviewButtonState();
        }

        private void OnViewModeIconSmall(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewMode(Models.ViewMode.IconSmall);
            GetActiveIconView()?.UpdateIconSize(Models.ViewMode.IconSmall);
            UpdateViewModeVisibility();
            UpdateViewModeIcon();
            UpdatePreviewButtonState();
        }

        // =================================================================
        //  Ctrl+Mouse Wheel — Cycle through ALL view modes (global window-level handler)
        //  Sequence: Miller → Details → IconSmall → IconMedium → IconLarge → IconExtraLarge
        //  Registered on this.Content with handledEventsToo=true so it works
        //  even when ScrollViewer/ListView consume the wheel event internally.
        // =================================================================

        private static readonly Models.ViewMode[] _allViewModes = new[]
        {
            Models.ViewMode.MillerColumns,
            Models.ViewMode.Details,
            Models.ViewMode.List,
            Models.ViewMode.IconSmall,
            Models.ViewMode.IconMedium,
            Models.ViewMode.IconLarge,
            Models.ViewMode.IconExtraLarge
        };

        /// <summary>
        /// 전역 GotFocus 버블링 핸들러: 포커스를 받은 요소의 FocusVisual을 테마 액센트로 교체.
        /// WinUI 3의 기본 FocusVisualPrimaryBrush(White)를 1px 액센트 톤으로 변경.
        /// </summary>
        /// <summary>
        /// GettingFocus 핸들러: 포커스 설정 전에 FocusVisual 브러시를 테마 액센트로 교체.
        /// GotFocus(설정 후)와 달리 첫 포커스부터 올바른 스타일로 그려짐.
        /// </summary>
        private void OnGlobalGettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            if (args.NewFocusedElement is FrameworkElement fe)
                ApplyFocusVisualToElement(fe);
        }

        /// <summary>
        /// 단일 FrameworkElement에 테마 FocusVisual 적용.
        /// TextBox 등 자체 포커스 인디케이터가 있는 컨트롤은 FocusVisual 제거.
        /// </summary>
        private void ApplyFocusVisualToElement(FrameworkElement fe)
        {
            // 이미 커스텀 설정된 요소는 스킵 (Transparent = 의도적 제거)
            if (fe.FocusVisualPrimaryBrush is SolidColorBrush existing && existing.Color.A == 0)
                return;

            // TextBox, PasswordBox, RichEditBox, AutoSuggestBox 내부 TextBox는
            // 자체 포커스 하단 라인이 있으므로 시스템 FocusVisual 제거
            if (fe is TextBox || fe is PasswordBox || fe is RichEditBox)
            {
                fe.UseSystemFocusVisuals = false;
                return;
            }

            // 기본 White/Black이면 테마 액센트로 교체
            if (fe.FocusVisualPrimaryBrush is SolidColorBrush scb
                && (scb.Color == Microsoft.UI.Colors.White || scb.Color == Microsoft.UI.Colors.Black))
            {
                var accentDimBrush = GetThemeBrush("SpanAccentDimBrush");
                fe.FocusVisualPrimaryBrush = accentDimBrush;
                fe.FocusVisualSecondaryBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                fe.FocusVisualPrimaryThickness = new Thickness(1);
                fe.FocusVisualSecondaryThickness = new Thickness(0);
            }
        }

        private void OnGlobalPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                       .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            if (!ctrl) return;

            var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
            if (delta == 0) return;

            // Dynamically find current position in the mode sequence
            var currentMode = ViewModel.CurrentViewMode;
            int currentIndex = Array.IndexOf(_allViewModes, currentMode);
            if (currentIndex < 0) currentIndex = 0; // fallback to Miller

            int newIndex = delta > 0
                ? Math.Min(currentIndex + 1, _allViewModes.Length - 1)  // scroll up = more visual
                : Math.Max(currentIndex - 1, 0);                         // scroll down = less visual

            if (newIndex == currentIndex) { e.Handled = true; return; }

            var newMode = _allViewModes[newIndex];
            ViewModel.SwitchViewMode(newMode);

            // If switching to icon mode, update icon size
            if (Helpers.ViewModeExtensions.IsIconMode(newMode))
            {
                GetActiveIconView()?.UpdateIconSize(newMode);
            }

            UpdateViewModeVisibility();
            UpdateViewModeIcon();
            UpdatePreviewButtonState();
            e.Handled = true;
        }

        private Views.IconModeView? GetActiveIconView()
        {
            if (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                return IconViewRight;
            if (_activeIconTabId != null && _tabIconPanels.TryGetValue(_activeIconTabId, out var view))
                return view;
            return null;
        }

        private Views.DetailsModeView? GetActiveDetailsView()
        {
            if (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == ActivePane.Right)
                return DetailsViewRight;
            if (_activeDetailsTabId != null && _tabDetailsPanels.TryGetValue(_activeDetailsTabId, out var view))
                return view;
            return null;
        }

        private Views.ListModeView? GetActiveListView()
        {
            // List has no right pane variant yet — left pane only
            if (_activeListTabId != null && _tabListPanels.TryGetValue(_activeListTabId, out var view))
                return view;
            return null;
        }

        // Visibility helper functions for x:Bind
        public Visibility IsMillerColumnsMode(Models.ViewMode mode)
            => mode == Models.ViewMode.MillerColumns ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsDetailsMode(Models.ViewMode mode)
            => mode == Models.ViewMode.Details ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsListMode(Models.ViewMode mode)
            => mode == Models.ViewMode.List ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsIconMode(Models.ViewMode mode)
            => Helpers.ViewModeExtensions.IsIconMode(mode) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsHomeMode(Models.ViewMode mode)
            => mode == Models.ViewMode.Home ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsNotHomeMode(Models.ViewMode mode)
            => mode != Models.ViewMode.Home ? Visibility.Visible : Visibility.Collapsed;

        // Tab management methods moved to MainWindow.TabManager.cs

        // =================================================================
        //  Per-Tab Miller Panel Management (Show/Hide pattern)
        // =================================================================





        // =================================================================
        //  Per-Tab Details Panel Management (Show/Hide pattern)
        // =================================================================




        // =================================================================
        //  Per-Tab List Panel Management (Show/Hide pattern)
        // =================================================================




        // =================================================================
        //  Per-Tab Icon Panel Management (Show/Hide pattern)
        // =================================================================




        // =================================================================
        //  Tab Event Handlers
        // =================================================================













        // =================================================================
        //  Tab Context Menu (Right-click on tab)
        // =================================================================



        // Sort menu opening - update checkmarks and icons
        private void OnSortMenuOpening(object sender, object e)
        {
            // Clear all checkmarks
            SortByNameItem.KeyboardAcceleratorTextOverride = string.Empty;
            SortByDateItem.KeyboardAcceleratorTextOverride = string.Empty;
            SortBySizeItem.KeyboardAcceleratorTextOverride = string.Empty;
            SortByTypeItem.KeyboardAcceleratorTextOverride = string.Empty;
            SortAscendingItem.KeyboardAcceleratorTextOverride = string.Empty;
            SortDescendingItem.KeyboardAcceleratorTextOverride = string.Empty;

            // Set checkmark on active sort field
            switch (_currentSortField)
            {
                case "Name":
                    SortByNameItem.KeyboardAcceleratorTextOverride = "✓";
                    break;
                case "Date":
                    SortByDateItem.KeyboardAcceleratorTextOverride = "✓";
                    break;
                case "Size":
                    SortBySizeItem.KeyboardAcceleratorTextOverride = "✓";
                    break;
                case "Type":
                    SortByTypeItem.KeyboardAcceleratorTextOverride = "✓";
                    break;
            }

            // Set checkmark on active sort direction
            if (_currentSortAscending)
                SortAscendingItem.KeyboardAcceleratorTextOverride = "✓";
            else
                SortDescendingItem.KeyboardAcceleratorTextOverride = "✓";

            // Group By checkmarks
            GroupByNoneItem.KeyboardAcceleratorTextOverride = _currentGroupBy == "None" ? "✓" : string.Empty;
            GroupByNameItem.KeyboardAcceleratorTextOverride = _currentGroupBy == "Name" ? "✓" : string.Empty;
            GroupByTypeItem.KeyboardAcceleratorTextOverride = _currentGroupBy == "Type" ? "✓" : string.Empty;
            GroupByDateItem.KeyboardAcceleratorTextOverride = _currentGroupBy == "DateModified" ? "✓" : string.Empty;
            GroupBySizeItem.KeyboardAcceleratorTextOverride = _currentGroupBy == "Size" ? "✓" : string.Empty;

            // Update button icons
            UpdateSortButtonIcons();
        }

        private void UpdateSortButtonIcons()
        {
            // Update sort field icon
            SortIcon.Glyph = _currentSortField switch
            {
                "Name" => "\uE8C1", // Name icon
                "Date" => "\uE787", // Calendar icon
                "Size" => "\uE7C6", // Size/ruler icon
                "Type" => "\uE7C3", // Tag/category icon
                _ => "\uE8CB" // Default sort icon
            };

            // Update sort direction icon
            SortDirectionIcon.Glyph = _currentSortAscending ? "\uE74A" : "\uE74B"; // Up/Down arrow
        }

        // =================================================================
        //  Split View — Pane Helpers & Handlers
        // =================================================================



        // --- x:Bind visibility/brush helpers ---








        // --- Focus tracking ---







        // --- Pane-specific flyout opening handlers (set ActivePane before menu item click) ---















        // --- Split View Toggle ---







        // =================================================================
        //  Preview Panel
        // =================================================================














        // =================================================================
        //  Inline Preview Column (inside Miller Columns)
        // =================================================================






        // =================================================================
        //  IContextMenuHost Implementation
        // =================================================================

        bool Services.IContextMenuHost.HasClipboardContent => _clipboardPaths.Count > 0;

        void Services.IContextMenuHost.PerformCut(string path)
        {
            if (Helpers.ArchivePathHelper.IsArchivePath(path)) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }

            // Multi-selection support: path 기반으로 올바른 컬럼의 선택 항목을 가져옴
            var paths = GetSelectedPathsForContextMenu(path);
            if (paths.Any(p => Helpers.ArchivePathHelper.IsArchivePath(p))) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }

            // 잘라내기 반투명 효과 적용
            var viewModels = GetViewModelsForPaths(paths);
            ApplyCutState(viewModels);

            _clipboardPaths.Clear();
            foreach (var p in paths)
                _clipboardPaths.Add(p);
            _isCutOperation = true;

            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Move;
            dataPackage.SetText(string.Join("\n", _clipboardPaths));

            // Provide StorageItems for Windows Explorer compatibility
            var capturedPaths = new List<string>(_clipboardPaths);
            dataPackage.SetDataProvider(StandardDataFormats.StorageItems, request =>
            {
                var deferral = request.GetDeferral();
                _ = Helpers.ViewDragDropHelper.ProvideStorageItemsAsync(request, capturedPaths, deferral);
            });

            Clipboard.SetContent(dataPackage);
            Helpers.DebugLogger.Log($"[ContextMenu] Cut: {_clipboardPaths.Count} item(s)");
            UpdateToolbarButtonStates();
        }

        void Services.IContextMenuHost.PerformCopy(string path)
        {
            // 이전 잘라내기 항목의 반투명 효과 해제
            ClearCutState();

            // Multi-selection support: path 기반으로 올바른 컬럼의 선택 항목을 가져옴
            var paths = GetSelectedPathsForContextMenu(path);

            _clipboardPaths.Clear();
            foreach (var p in paths)
                _clipboardPaths.Add(p);
            _isCutOperation = false;

            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(string.Join("\n", _clipboardPaths));

            // Provide StorageItems for Windows Explorer compatibility
            var capturedPaths = new List<string>(_clipboardPaths);
            dataPackage.SetDataProvider(StandardDataFormats.StorageItems, request =>
            {
                var deferral = request.GetDeferral();
                _ = Helpers.ViewDragDropHelper.ProvideStorageItemsAsync(request, capturedPaths, deferral);
            });

            Clipboard.SetContent(dataPackage);
            Helpers.DebugLogger.Log($"[ContextMenu] Copy: {_clipboardPaths.Count} item(s)");
            UpdateToolbarButtonStates();
        }

        async void Services.IContextMenuHost.PerformPaste(string targetFolderPath)
        {
            if (Helpers.ArchivePathHelper.IsArchivePath(targetFolderPath)) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }
            try
            {
            List<string> sourcePaths;
            bool isCut;

            if (_clipboardPaths.Count > 0)
            {
                // Internal clipboard (LumiFiles → LumiFiles)
                sourcePaths = new List<string>(_clipboardPaths);
                isCut = _isCutOperation;
            }
            else
            {
                // External clipboard (Windows Explorer → LumiFiles)
                try
                {
                    var content = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
                    if (!content.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems)) return;

                    var items = await content.GetStorageItemsAsync();
                    sourcePaths = items
                        .Select(i => i.Path)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                    if (sourcePaths.Count == 0) return;

                    isCut = content.RequestedOperation.HasFlag(
                        Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move);
                }
                catch { return; }
            }

            // Find target column index for targeted refresh
            int? targetColumnIndex = null;
            var columns = ViewModel.ActiveExplorer?.Columns;
            if (columns == null) return;
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Path.Equals(targetFolderPath, StringComparison.OrdinalIgnoreCase))
                {
                    targetColumnIndex = i;
                    break;
                }
            }

            var router = App.Current.Services.GetRequiredService<FileSystemRouter>();
            LumiFiles.Services.FileOperations.IFileOperation op = isCut
                ? new LumiFiles.Services.FileOperations.MoveFileOperation(sourcePaths, targetFolderPath, router)
                : new LumiFiles.Services.FileOperations.CopyFileOperation(sourcePaths, targetFolderPath, router);

            await ViewModel.ExecuteFileOperationAsync(op, targetColumnIndex);

            if (isCut && _clipboardPaths.Count > 0) _clipboardPaths.Clear();
            UpdateToolbarButtonStates();
            }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[ContextMenu] PerformPaste failed: {ex.Message}"); }
        }

        async void Services.IContextMenuHost.PerformDelete(string path, string itemName)
        {
            if (Helpers.ArchivePathHelper.IsArchivePath(path)) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }
            try
            {
            // Multi-selection support: path 기반으로 올바른 컬럼의 선택 항목을 가져옴
            // (Flyout 열린 상태에서 포커스 기반 검색은 잘못된 컬럼을 찾을 수 있음)
            var paths = GetSelectedPathsForContextMenu(path);
            string displayName = paths.Count > 1 ? string.Format(_loc.Get("StatusBar_Items"), paths.Count) : itemName;

            var dialog = new ContentDialog
            {
                Title = _loc.Get("DeleteConfirmTitle"),
                Content = string.Format(_loc.Get("DeleteConfirmContent"), displayName),
                PrimaryButtonText = _loc.Get("Delete"),
                CloseButtonText = _loc.Get("Cancel"),
                XamlRoot = this.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await ShowContentDialogSafeAsync(dialog);
            if (result != ContentDialogResult.Primary) return;

            var router = App.Current.Services.GetRequiredService<Services.FileSystemRouter>();
            var operation = new Services.FileOperations.DeleteFileOperation(
                paths, permanent: false, router: router);

            int activeIndex = GetColumnIndexForPath(path);
            if (activeIndex >= 0)
            {
                await ViewModel.ExecuteFileOperationAsync(operation, activeIndex);
                ViewModel.ActiveExplorer?.CleanupColumnsFrom(activeIndex + 1);
                FocusColumnAsync(activeIndex);
            }
            }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[ContextMenu] PerformDelete failed: {ex.Message}"); }
        }

        void Services.IContextMenuHost.PerformRename(FileSystemViewModel item)
        {
            if (Helpers.ArchivePathHelper.IsArchivePath(item.Path)) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }
            try
            {
            Helpers.DebugLogger.Log($"[Rename] PerformRename START: '{item.Name}'");

            var columns = ViewModel.ActiveExplorer?.Columns;
            if (columns == null) return;
            int targetIndex = -1;
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Children.Contains(item))
                {
                    targetIndex = i;
                    columns[i].SelectedChild = item;
                    break;
                }
            }

            Helpers.DebugLogger.Log($"[Rename] PerformRename targetIndex={targetIndex}");

            // MenuFlyout 닫힘 → LostFocus → CommitRename 방지
            _renamePendingFocus = true;
            item.BeginRename();

            if (targetIndex < 0)
                targetIndex = GetCurrentColumnIndex();
            if (targetIndex < 0) { _renamePendingFocus = false; return; }

            int colIdx = targetIndex;
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (_isClosed) return;
                Helpers.DebugLogger.Log($"[Rename] PerformRename Low dispatch: clearing pendingFocus, calling FocusRenameTextBox({colIdx})");
                _renamePendingFocus = false;
                FocusRenameTextBox(colIdx);
            });
            }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[ContextMenu] PerformRename failed: {ex.Message}"); }
        }

        void Services.IContextMenuHost.PerformOpen(FileSystemViewModel item)
        {
            if (item is FolderViewModel folder)
            {
                // 부모 컬럼을 명시 전달: fromColumn 누락 시 CurrentFolder(=마지막 컬럼)로 폴백되어
                // 엉뚱한 위치에 컬럼이 삽입되고 이후 컬럼이 stale 상태로 남는 버그 수정 (Discussion #20)
                var explorer = ViewModel.ActiveExplorer;
                var parentCol = explorer?.Columns.FirstOrDefault(c => c.Children.Contains(folder));
                explorer?.NavigateIntoFolder(folder, fromColumn: parentCol);
            }
            else if (item is FileViewModel file)
            {
                if (Helpers.ArchivePathHelper.IsArchiveFile(file.Path))
                {
                    // Archive: navigate into it instead of opening externally
                    var explorer = ViewModel.ActiveExplorer;
                    if (explorer != null)
                    {
                        // Selecting the archive triggers HandleFileSelection → NavigateIntoArchiveAsync
                        // For PerformOpen from context menu, we need to find the parent column
                        foreach (var col in explorer.Columns)
                        {
                            if (col.SelectedChild == file || col.Children.Contains(file))
                            {
                                col.SelectedChild = file;
                                break;
                            }
                        }
                    }
                }
                else if (Helpers.ArchivePathHelper.IsArchivePath(file.Path))
                {
                    // File inside archive: extract to temp and open
                    _ = OpenArchiveEntryAsync(file.Path);
                }
                else
                {
                    var shellService = App.Current.Services.GetRequiredService<ShellService>();
                    shellService.OpenFile(file.Path);
                }
            }
        }

        /// <summary>
        /// Extract a file from inside an archive to temp and open it with the default app.
        /// </summary>
        private async Task OpenArchiveEntryAsync(string archivePath)
        {
            await OpenArchiveEntryStaticAsync(archivePath);
        }

        /// <summary>
        /// Extract a file from inside an archive to temp and open it with the default app.
        /// Callable from any view (ListModeView, ViewItemHelper, etc.).
        /// </summary>
        internal static async Task OpenArchiveEntryStaticAsync(string archivePath)
        {
            try
            {
                var (archiveFilePath, internalPath) = Helpers.ArchivePathHelper.Parse(archivePath);
                if (string.IsNullOrEmpty(internalPath)) return;

                var reader = App.Current.Services.GetRequiredService<Services.ArchiveReaderService>();
                using var stream = await reader.OpenEntryAsync(archiveFilePath, internalPath);

                var fileName = System.IO.Path.GetFileName(internalPath);
                var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Span_Archive");
                System.IO.Directory.CreateDirectory(tempDir);
                var tempFile = System.IO.Path.Combine(tempDir, fileName);

                using (var fs = new System.IO.FileStream(tempFile, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    await stream.CopyToAsync(fs);
                }

                var shellService = App.Current.Services.GetRequiredService<Services.ShellService>();
                shellService.OpenFile(tempFile);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[OpenArchiveEntry] Error: {ex.Message}");
            }
        }

        private void OnShellFileOpening(string fileName)
        {
            Helpers.DispatcherHelper.SafeEnqueue(DispatcherQueue, () =>
            {
                if (_isClosed) return;
                ViewModel?.ShowToast($"\"{fileName}\" {_loc.Get("Opening")}...", 2000);
            });
        }

        void Services.IContextMenuHost.PerformOpenDrive(DriveItem drive)
        {
            ViewModel.OpenDrive(drive);
            UpdateViewModeVisibility();
            if (ViewModel.CurrentViewMode == ViewMode.MillerColumns)
                FocusColumnAsync(0);
            else
                FocusActiveView();
        }

        void Services.IContextMenuHost.PerformEjectDrive(DriveItem drive)
        {
            var shellService = App.Current.Services.GetRequiredService<ShellService>();
            shellService.EjectDrive(drive.Path);
            // WM_DEVICECHANGE 이벤트가 자동으로 드라이브 목록 갱신
        }

        void Services.IContextMenuHost.PerformDisconnectDrive(DriveItem drive)
        {
            // 1) 네트워크 바로가기: NetworkShortcutPath로 직접 삭제
            if (drive.IsNetworkShortcut)
            {
                try
                {
                    if (System.IO.Directory.Exists(drive.NetworkShortcutPath))
                    {
                        DeleteNetworkShortcutFolder(drive.NetworkShortcutPath!);
                        ViewModel.RefreshDrives();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[MainWindow] Delete network shortcut error: {ex.Message}");
                }
            }

            // 2) UNC 경로로 Network Shortcuts 폴더에서 일치하는 바로가기 검색 후 삭제
            //    (캐시에서 로드된 DriveItem은 NetworkShortcutPath가 없을 수 있음)
            if (drive.Path.StartsWith(@"\\"))
            {
                try
                {
                    var shortcutsDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Microsoft", "Windows", "Network Shortcuts");
                    if (System.IO.Directory.Exists(shortcutsDir))
                    {
                        foreach (var dir in System.IO.Directory.GetDirectories(shortcutsDir))
                        {
                            var target = FileSystemService.ResolveNetworkShortcutTarget(dir);
                            if (string.Equals(target, drive.Path, StringComparison.OrdinalIgnoreCase))
                            {
                                DeleteNetworkShortcutFolder(dir);
                                ViewModel.RefreshDrives();
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helpers.DebugLogger.Log($"[MainWindow] Search+delete network shortcut error: {ex.Message}");
                }
            }

            // 3) 매핑된 네트워크 드라이브: WNetCancelConnection2
            var shellService = App.Current.Services.GetRequiredService<ShellService>();
            if (shellService.DisconnectNetworkDrive(drive.Path))
                ViewModel.RefreshDrives();
        }

        /// <summary>
        /// 네트워크 바로가기 폴더 삭제. 읽기전용/시스템 속성을 해제 후 삭제.
        /// </summary>
        private static void DeleteNetworkShortcutFolder(string path)
        {
            // 폴더 및 내부 파일의 읽기전용/시스템 속성 제거
            var dirInfo = new System.IO.DirectoryInfo(path);
            dirInfo.Attributes = System.IO.FileAttributes.Normal;
            foreach (var file in dirInfo.GetFiles("*", System.IO.SearchOption.AllDirectories))
                file.Attributes = System.IO.FileAttributes.Normal;
            dirInfo.Delete(true);
        }

        void Services.IContextMenuHost.PerformOpenFavorite(FavoriteItem fav)
        {
            ViewModel.NavigateToFavorite(fav);
            FocusColumnAsync(0);
        }

        async void Services.IContextMenuHost.PerformNewFolder(string parentFolderPath)
        {
            if (Helpers.ArchivePathHelper.IsArchivePath(parentFolderPath)) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }
            string baseName = _loc.Get("NewFolderBaseName");
            string newPath = System.IO.Path.Combine(parentFolderPath, baseName);

            int count = 1;
            while (System.IO.Directory.Exists(newPath))
            {
                newPath = System.IO.Path.Combine(parentFolderPath, $"{baseName} ({count})");
                count++;
            }

            try
            {
                System.IO.Directory.CreateDirectory(newPath);

                // Find and refresh the column for this parent
                var columns = ViewModel.ActiveExplorer?.Columns; if (columns == null) return;
                var parentColumn = columns.FirstOrDefault(c =>
                    c.Path.Equals(parentFolderPath, StringComparison.OrdinalIgnoreCase));
                if (parentColumn != null)
                {
                    await parentColumn.ReloadAsync();
                    var newFolder = parentColumn.Children.FirstOrDefault(c =>
                        c.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase));
                    if (newFolder != null)
                    {
                        parentColumn.SelectedChild = newFolder;
                        newFolder.BeginRename();
                        await System.Threading.Tasks.Task.Delay(100);
                        int colIndex = columns.IndexOf(parentColumn);
                        if (colIndex >= 0)
                            FocusRenameTextBox(colIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ContextMenu] NewFolder error: {ex.Message}");
            }
        }

        async void Services.IContextMenuHost.PerformNewFile(string parentFolderPath, string fileName)
        {
            if (Helpers.ArchivePathHelper.IsArchivePath(parentFolderPath)) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }
            string baseName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string ext = System.IO.Path.GetExtension(fileName);
            string newPath = System.IO.Path.Combine(parentFolderPath, fileName);

            int count = 1;
            while (System.IO.File.Exists(newPath))
            {
                newPath = System.IO.Path.Combine(parentFolderPath, $"{baseName} ({count}){ext}");
                count++;
            }

            try
            {
                var op = new LumiFiles.Services.FileOperations.NewFileOperation(newPath);
                var result = await op.ExecuteAsync();
                if (!result.Success) return;

                // Refresh column and start rename
                var columns = ViewModel.ActiveExplorer?.Columns; if (columns == null) return;
                var parentColumn = columns.FirstOrDefault(c =>
                    c.Path.Equals(parentFolderPath, StringComparison.OrdinalIgnoreCase));
                if (parentColumn != null)
                {
                    await parentColumn.ReloadAsync();
                    var newFile = parentColumn.Children.FirstOrDefault(c =>
                        c.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase));
                    if (newFile != null)
                    {
                        parentColumn.SelectedChild = newFile;
                        newFile.BeginRename();
                        await System.Threading.Tasks.Task.Delay(100);
                        int colIndex = columns.IndexOf(parentColumn);
                        if (colIndex >= 0)
                            FocusRenameTextBox(colIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ContextMenu] NewFile error: {ex.Message}");
            }
        }

        async void Services.IContextMenuHost.PerformNewFileFromShellNew(string parentFolderPath, Services.ShellNewItem shellNewItem)
        {
            if (Helpers.ArchivePathHelper.IsArchivePath(parentFolderPath)) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }

            try
            {
                var shellNewService = App.Current.Services.GetRequiredService<Services.ShellNewService>();
                var newPath = await shellNewService.CreateNewFileAsync(shellNewItem, parentFolderPath);

                if (newPath == null) return; // Command 타입 — 외부 프로세스가 처리

                // Refresh column and start rename
                var columns = ViewModel.ActiveExplorer?.Columns; if (columns == null) return;
                var parentColumn = columns.FirstOrDefault(c =>
                    c.Path.Equals(parentFolderPath, StringComparison.OrdinalIgnoreCase));
                if (parentColumn != null)
                {
                    await parentColumn.ReloadAsync();
                    var newFile = parentColumn.Children.FirstOrDefault(c =>
                        c.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase));
                    if (newFile != null)
                    {
                        parentColumn.SelectedChild = newFile;
                        newFile.BeginRename();
                        await System.Threading.Tasks.Task.Delay(100);
                        int colIndex = columns.IndexOf(parentColumn);
                        if (colIndex >= 0)
                            FocusRenameTextBox(colIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ContextMenu] NewFileFromShellNew error: {ex.Message}");
            }
        }

        async void Services.IContextMenuHost.PerformCompress(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            if (paths.Any(p => Helpers.ArchivePathHelper.IsArchivePath(p))) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }

            try
            {
                // Multi-selection support: path 기반으로 올바른 컬럼의 선택 항목을 가져옴
                var allPaths = GetSelectedPathsForContextMenu(paths[0]);
                if (allPaths.Any(p => Helpers.ArchivePathHelper.IsArchivePath(p))) { ViewModel.ShowToast(_loc.Get("Toast_ArchiveReadOnly")); return; }

                // ZIP name: first item name + .zip
                string firstPath = allPaths[0];
                string parentDir = System.IO.Path.GetDirectoryName(firstPath)!;
                string zipName = System.IO.Path.GetFileNameWithoutExtension(firstPath) + ".zip";
                string zipPath = System.IO.Path.Combine(parentDir, zipName);

                int count = 1;
                while (System.IO.File.Exists(zipPath))
                {
                    zipPath = System.IO.Path.Combine(parentDir,
                        System.IO.Path.GetFileNameWithoutExtension(firstPath) + $" ({count}).zip");
                    count++;
                }

                var op = new LumiFiles.Services.FileOperations.CompressOperation(allPaths.ToArray(), zipPath);
                var activeIndex = GetColumnIndexForPath(paths[0]);
                await ViewModel.ExecuteFileOperationAsync(op, activeIndex >= 0 ? activeIndex : null);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ContextMenu] Compress error: {ex.Message}");
            }
        }

        async void Services.IContextMenuHost.PerformExtractHere(string zipPath)
        {
            if (string.IsNullOrEmpty(zipPath)) return;

            try
            {
                string parentDir = System.IO.Path.GetDirectoryName(zipPath)!;
                string folderName = System.IO.Path.GetFileNameWithoutExtension(zipPath);
                string destPath = System.IO.Path.Combine(parentDir, folderName);

                int count = 1;
                while (System.IO.Directory.Exists(destPath))
                {
                    destPath = System.IO.Path.Combine(parentDir, $"{folderName} ({count})");
                    count++;
                }

                var op = new LumiFiles.Services.FileOperations.ExtractOperation(zipPath, destPath);
                var activeIndex = GetActiveColumnIndex();
                await ViewModel.ExecuteFileOperationAsync(op, activeIndex >= 0 ? activeIndex : null);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ContextMenu] ExtractHere error: {ex.Message}");
            }
        }

        async void Services.IContextMenuHost.PerformExtractTo(string zipPath)
        {
            if (string.IsNullOrEmpty(zipPath)) return;

            try
            {
                // Use FolderPicker
                var picker = new Windows.Storage.Pickers.FolderPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                picker.FileTypeFilter.Add("*");

                // Initialize with window handle
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null) return;

                string folderName = System.IO.Path.GetFileNameWithoutExtension(zipPath);
                string destPath = System.IO.Path.Combine(folder.Path, folderName);

                int count = 1;
                while (System.IO.Directory.Exists(destPath))
                {
                    destPath = System.IO.Path.Combine(folder.Path, $"{folderName} ({count})");
                    count++;
                }

                var op = new LumiFiles.Services.FileOperations.ExtractOperation(zipPath, destPath);
                var activeIndex = GetActiveColumnIndex();
                await ViewModel.ExecuteFileOperationAsync(op, activeIndex >= 0 ? activeIndex : null);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[ContextMenu] ExtractTo error: {ex.Message}");
            }
        }

        void Services.IContextMenuHost.AddToFavorites(string path)
        {
            ViewModel.AddToFavorites(path);
        }

        void Services.IContextMenuHost.RemoveFromFavorites(string path)
        {
            ViewModel.RemoveFromFavorites(path);
        }

        async void Services.IContextMenuHost.RemoveRemoteConnection(string connectionId)
        {
            try
            {
            var connService = App.Current.Services.GetRequiredService<ConnectionManagerService>();
            var connInfo = ViewModel.SavedConnections.FirstOrDefault(c => c.Id == connectionId);
            string displayName = connInfo?.DisplayName ?? connectionId;

            var dialog = new ContentDialog
            {
                Title = _loc.Get("RemoveConnectionTitle"),
                Content = string.Format(_loc.Get("RemoveConnectionConfirm"), displayName),
                PrimaryButtonText = _loc.Get("Delete"),
                CloseButtonText = _loc.Get("Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await ShowContentDialogSafeAsync(dialog);
            if (result == ContentDialogResult.Primary)
            {
                // 활성 연결 해제
                if (connInfo != null)
                {
                    var router = App.Current.Services.GetRequiredService<FileSystemRouter>();
                    var uriPrefix = FileSystemRouter.GetUriPrefix(connInfo.ToUri());
                    router.UnregisterConnection(uriPrefix);
                }

                connService.RemoveConnection(connectionId);
                Helpers.DebugLogger.Log($"[Sidebar] 원격 연결 제거: {displayName}");
                ViewModel.ShowToast(string.Format(_loc.Get("ConnectionRemoved"), displayName));
            }
            }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[ContextMenu] RemoveRemoteConnection failed: {ex.Message}"); }
        }

        async void Services.IContextMenuHost.EditRemoteConnection(string connectionId)
        {
            try
            {
            var connService = App.Current.Services.GetRequiredService<ConnectionManagerService>();
            var existing = ViewModel.SavedConnections.FirstOrDefault(c => c.Id == connectionId);
            if (existing == null) return;

            var (result, updated, password, _, _provider) = await ShowConnectionDialog(existing);
            if (result != ContentDialogResult.Primary || updated == null) return;

            // SMB: 표시 이름 + UNC 경로만 업데이트
            if (updated.Protocol == Models.RemoteProtocol.SMB)
            {
                connService.UpdateConnection(updated);
                Helpers.DebugLogger.Log($"[Sidebar] SMB 연결 편집 완료: {updated.DisplayName}");
                return;
            }

            // SFTP/FTP: 속성 업데이트 + 비밀번호 저장
            connService.UpdateConnection(updated);
            if (!string.IsNullOrEmpty(password))
                connService.SaveCredential(updated.Id, password);

            Helpers.DebugLogger.Log($"[Sidebar] 원격 연결 편집 완료: {updated.DisplayName}");
            }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[ContextMenu] EditRemoteConnection failed: {ex.Message}"); }
        }

        bool Services.IContextMenuHost.IsFavorite(string path)
        {
            return ViewModel.IsFavorite(path);
        }

        void Services.IContextMenuHost.SwitchViewMode(ViewMode mode)
        {
            ViewModel.SwitchViewMode(mode);
            if (Helpers.ViewModeExtensions.IsIconMode(mode))
                GetActiveIconView()?.UpdateIconSize(mode);
            UpdateViewModeIcon();
        }

        void Services.IContextMenuHost.ApplySort(string field)
        {
            _currentSortField = field;
            SortCurrentColumn(_currentSortField, _currentSortAscending);
        }

        void Services.IContextMenuHost.ApplySortDirection(bool ascending)
        {
            _currentSortAscending = ascending;
            SortCurrentColumn(_currentSortField, _currentSortAscending);
        }

        // Group By state
        private string _currentGroupBy = "None";

        string Services.IContextMenuHost.CurrentGroupBy => _currentGroupBy;

        void Services.IContextMenuHost.ApplyGroupBy(string groupBy)
        {
            _currentGroupBy = groupBy;

            // Details 뷰 — 자체 GroupBy 시스템 사용
            var detailsView = GetActiveDetailsView();
            if (detailsView != null && ViewModel.CurrentViewMode == Models.ViewMode.Details)
            {
                detailsView.SetGroupByPublic(groupBy);
                return;
            }

            // Icon/List 뷰 — FolderViewModel의 Children 기반 그룹핑
            GetActiveIconView()?.ApplyGroupBy(groupBy);
            GetActiveListView()?.ApplyGroupBy(groupBy);

            // 설정 저장
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                settings.Values["ViewGroupBy"] = groupBy;
            }
            catch { }

            Helpers.DebugLogger.Log($"[GroupBy] Applied: {groupBy}");
        }

        /// <summary>
        /// 다운로드 폴더 진입 시 children 로드 완료 후 DateModified 그룹핑 자동 적용.
        /// 다운로드 폴더를 벗어나면 그룹핑 해제.
        /// Miller Columns 모드에서는 그룹핑 미적용 (정렬만).
        /// </summary>
        private bool _isDownloadsAutoGrouped;
        private System.ComponentModel.PropertyChangedEventHandler? _downloadsLoadHandler;
        private FolderViewModel? _watchedDownloadsFolder;

        private void ScheduleDownloadsGroupingIfNeeded(ExplorerViewModel explorer)
        {
            // 이전 감시 정리
            CleanupDownloadsLoadWatch();

            if (ViewModel.CurrentViewMode == Models.ViewMode.MillerColumns) return;

            var path = explorer.CurrentPath;
            bool isDownloads = !string.IsNullOrEmpty(path) && Helpers.KnownFolderHelper.IsDownloadsFolder(path);

            if (!isDownloads)
            {
                if (_isDownloadsAutoGrouped)
                {
                    _isDownloadsAutoGrouped = false;
                    ((Services.IContextMenuHost)this).ApplyGroupBy("None");
                }
                return;
            }

            // 다운로드 폴더 — children 로드 완료 후 그룹핑 적용
            var folder = explorer.CurrentFolder;
            if (folder == null) return;

            if (folder.Children.Count > 0)
            {
                // 이미 로드됨 (캐시) — 즉시 적용
                _isDownloadsAutoGrouped = true;
                ((Services.IContextMenuHost)this).ApplyGroupBy("DateModified");
                return;
            }

            // Children 아직 비어있음 — IsLoading 변화 감시
            // (CurrentPath 설정 시점에는 EnsureChildrenLoadedAsync 호출 전이라
            //  IsLoading=false, Children.Count=0 상태일 수 있음)
            _watchedDownloadsFolder = folder;
            _downloadsLoadHandler = (s, e) =>
            {
                if (e.PropertyName == nameof(FolderViewModel.IsLoading) && s is FolderViewModel f && !f.IsLoading)
                {
                    CleanupDownloadsLoadWatch();
                    DispatcherQueue?.TryEnqueue(() =>
                    {
                        // 아직 다운로드 폴더에 있는지 확인
                        if (ViewModel?.ActiveExplorer != null &&
                            Helpers.KnownFolderHelper.IsDownloadsFolder(ViewModel.ActiveExplorer.CurrentPath))
                        {
                            _isDownloadsAutoGrouped = true;
                            ((Services.IContextMenuHost)this).ApplyGroupBy("DateModified");
                        }
                    });
                }
            };
            folder.PropertyChanged += _downloadsLoadHandler;
        }

        private void CleanupDownloadsLoadWatch()
        {
            if (_downloadsLoadHandler != null && _watchedDownloadsFolder != null)
            {
                _watchedDownloadsFolder.PropertyChanged -= _downloadsLoadHandler;
            }
            _downloadsLoadHandler = null;
            _watchedDownloadsFolder = null;
        }

        void Services.IContextMenuHost.PerformSelectAll()
        {
            HandleSelectAll();
        }

        void Services.IContextMenuHost.PerformSelectNone()
        {
            HandleSelectNone();
        }

        void Services.IContextMenuHost.PerformInvertSelection()
        {
            HandleInvertSelection();
        }

        void Services.IContextMenuHost.PerformOpenInNewTab(string folderPath)
        {
            var root = new Models.FolderItem { Name = "PC", Path = "PC" };
            var explorer = new ViewModels.ExplorerViewModel(root, App.Current.Services.GetRequiredService<Services.FileSystemService>());
            var viewMode = ViewModel.CurrentViewMode;
            explorer.EnableAutoNavigation = viewMode == Models.ViewMode.MillerColumns;

            // 드라이브 루트 대응: GetFileName("C:\")은 빈 문자열 반환
            var header = System.IO.Path.GetFileName(folderPath.TrimEnd('\\', '/'));
            if (string.IsNullOrEmpty(header)) header = folderPath;

            var tab = new Models.TabItem
            {
                Header = header,
                Path = folderPath,
                ViewMode = viewMode,
                IconSize = Models.ViewMode.IconMedium,
                Explorer = explorer
            };
            ViewModel.Tabs.Add(tab);

            // View 레벨 패널 생성 및 전환
            CreateMillerPanelForTab(tab);
            if (tab.Explorer is ViewModels.ExplorerViewModel newExpl)
                newExpl.TabSwitchSuppressionTicks = Environment.TickCount64 + 500;
            SwitchMillerPanel(tab.Id);
            SwitchDetailsPanel(tab.Id, tab.ViewMode == Models.ViewMode.Details);
            SwitchListPanel(tab.Id, tab.ViewMode == Models.ViewMode.List);
            SwitchIconPanel(tab.Id, Helpers.ViewModeExtensions.IsIconMode(tab.ViewMode));

            ViewModel.SwitchToTab(ViewModel.Tabs.Count - 1);

            // 분할뷰 → 단일뷰 전환: SwitchToTab이 backing field로 IsSplitViewEnabled=false 설정하지만
            // Grid Column 너비와 x:Bind는 자동 갱신되지 않으므로 직접 처리
            if (!ViewModel.IsSplitViewEnabled)
            {
                SplitterCol.Width = new GridLength(0);
                RightPaneCol.Width = new GridLength(0);
                UnsubscribeRightExplorerForAddressBar();
            }
            ViewModel.NotifySplitViewChanged();

            ResubscribeLeftExplorer();
            UpdateViewModeVisibility();
            UpdateToolbarButtonStates();
            SyncAddressBarControls(ViewModel.Explorer);
            FocusActiveView();
            CloseQuickLookWindow();

            _ = explorer.NavigateToPath(folderPath);
        }

        void Services.IContextMenuHost.PerformOpenTerminal(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !System.IO.Directory.Exists(folderPath)) return;
            var shellService = App.Current.Services.GetRequiredService<Services.ShellService>();
            var settings = App.Current.Services.GetRequiredService<Services.SettingsService>();
            shellService.OpenTerminal(folderPath, settings.DefaultTerminal);
        }

        void Services.IContextMenuHost.PerformRefresh()
        {
            HandleRefresh();
        }

        void Services.IContextMenuHost.PerformUndo()
        {
            _ = ViewModel.UndoCommand.ExecuteAsync(null);
        }

        void Services.IContextMenuHost.PerformShowProperties(string path)
        {
            var shellService = App.Current.Services.GetRequiredService<Services.ShellService>();
            shellService.ShowProperties(path);
        }

        // =================================================================
        //  Help / Settings / Log
        // =================================================================








        // =================================================================
        //  P1 #12: Tab Re-docking — Merge torn-off tab back into window
        // =================================================================


        // =================================================================
        //  P1 #15: Ctrl+D — Duplicate selected file/folder
        // =================================================================



        // =================================================================
        //  P1 #18: Alt+Enter — Show Windows Properties dialog
        // =================================================================


        // =================================================================
        //  Filter Bar (Ctrl+Shift+F)
        // =================================================================

        private void ToggleFilterBar()
        {
            if (_isClosed) return;
            var explorer = ViewModel.ActiveExplorer;
            if (explorer == null) return;

            if (LeftFilterBar.Visibility == Visibility.Visible)
            {
                CloseFilterBar();
                return;
            }

            // Miller Column 뷰에서는 필터가 의미 없음 — 각 컬럼의 Children을 숨기면
            // 경로 하이라이트/SelectedChild 상태가 깨져 빈 컬럼 유령 UI 발생.
            // Details/List/Icon 같은 평면 목록 뷰에서만 필터 허용.
            var activeMode = (ViewModel.IsSplitViewEnabled && ViewModel.ActivePane == Models.ActivePane.Right)
                ? ViewModel.RightViewMode : ViewModel.LeftViewMode;
            if (activeMode == Models.ViewMode.MillerColumns)
            {
                ViewModel.ShowToast(_loc.Get("Filter_NotAvailableInMiller"), 2500, isError: false);
                return;
            }

            LeftFilterBar.Visibility = Visibility.Visible;
            LeftFilterTextBox.Focus(FocusState.Keyboard);
            UpdateFilterCount();
        }

        private void CloseFilterBar()
        {
            if (_isClosed) return;
            _filterDebounceTimer?.Stop();
            _filterDebounceTimer = null;
            LeftFilterBar.Visibility = Visibility.Collapsed;
            LeftFilterTextBox.Text = string.Empty;
            LeftFilterCountText.Text = string.Empty;

            var explorer = ViewModel.ActiveExplorer;
            if (explorer != null)
                explorer.FilterText = string.Empty;
        }

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isClosed) return;
            var explorer = ViewModel.ActiveExplorer;
            if (explorer == null) return;

            // Debounce: 14K+ 파일 폴더에서 키스트로크마다 전체 필터링 방지
            _filterDebounceTimer?.Stop();
            _filterDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _filterDebounceTimer.Tick += (_, _) =>
            {
                _filterDebounceTimer.Stop();
                if (_isClosed) return;
                var exp = ViewModel.ActiveExplorer;
                if (exp == null) return;
                exp.FilterText = LeftFilterTextBox.Text;
                UpdateFilterCount();
            };
            _filterDebounceTimer.Start();
        }

        private void OnFilterBarClose(object sender, RoutedEventArgs e)
        {
            CloseFilterBar();
        }

        private void OnFilterTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                CloseFilterBar();
                e.Handled = true;
            }
        }

        private void UpdateFilterCount()
        {
            var explorer = ViewModel.ActiveExplorer;
            if (explorer == null || !explorer.IsFilterActive)
            {
                LeftFilterCountText.Text = string.Empty;
                return;
            }

            // 모든 컬럼의 필터 카운트 합산 (Miller Columns에서 여러 컬럼에 필터 적용됨)
            int filteredTotal = 0;
            int allTotal = 0;
            foreach (var col in explorer.Columns)
            {
                if (!string.IsNullOrEmpty(col.CurrentFilterText))
                {
                    filteredTotal += col.Children.Count;
                    allTotal += col.TotalChildCount;
                }
            }

            if (allTotal > 0)
            {
                LeftFilterCountText.Text = $"{filteredTotal}/{allTotal}";
            }
            else
            {
                LeftFilterCountText.Text = string.Empty;
            }
        }

        // ============================================================
        // Stage 4 — LumiSidebar navigation dispatch
        // Each Lumi sidebar item is a Grid with a single TextBlock label;
        // we resolve the path by label text and call NavigateToPath on the
        // active explorer. Items without a path mapping are no-op for now.
        // ============================================================
        // ============================================================
        // LumiToolbar — inline search: 32px button ↔ 250px TextBox toggle
        // ============================================================
        private void OnLumiSearchClick(object sender, RoutedEventArgs e)
        {
            // Toggle the entire search button frame (the 3px group wrapper) — not just
            // the button — so the frame doesn't linger behind the rectangular search box.
            LumiSearchButtonFrame.Visibility = Visibility.Collapsed;
            LumiSearchExpanded.Visibility = Visibility.Visible;
            LumiSearchInput.Focus(FocusState.Programmatic);
        }

        private void OnLumiSearchCloseClick(object sender, RoutedEventArgs e)
        {
            CollapseLumiSearch();
        }

        private void OnLumiSearchInputKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                CollapseLumiSearch();
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // Hand off to the existing command palette / search infrastructure.
                try { OpenCommandPalette(); } catch { }
                e.Handled = true;
            }
        }

        private void OnLumiSearchInputLostFocus(object sender, RoutedEventArgs e)
        {
            // Auto-collapse when empty and focus leaves; keep expanded if user typed something.
            if (LumiSearchInput.Text.Length == 0)
                CollapseLumiSearch();
        }

        private void CollapseLumiSearch()
        {
            LumiSearchExpanded.Visibility = Visibility.Collapsed;
            LumiSearchButtonFrame.Visibility = Visibility.Visible;
            LumiSearchInput.Text = string.Empty;
        }

        // ============================================================
        // LumiSidebar — dynamic favorite / drive tap (Tag carries path)
        // ============================================================
        /// <summary>
        /// LumiSidebar section header toggle (Favorites / Local Drives / Cloud / Network).
        /// Mirrors the Span-original SidebarSectionToggle pattern: Tag identifies which
        /// section's IsXxxExpanded ObservableProperty to flip; the chevron rotates and
        /// the ItemsControl visibility-binds collapse/expand automatically.
        /// </summary>
        private void OnSidebarSectionToggle(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string section) return;
            switch (section)
            {
                case "Favorites": ViewModel.IsFavoritesExpanded = !ViewModel.IsFavoritesExpanded; break;
                case "Local":     ViewModel.IsLocalDrivesExpanded = !ViewModel.IsLocalDrivesExpanded; break;
                case "Cloud":     ViewModel.IsCloudDrivesExpanded = !ViewModel.IsCloudDrivesExpanded; break;
                case "Network":   ViewModel.IsNetworkDrivesExpanded = !ViewModel.IsNetworkDrivesExpanded; break;
            }
            e.Handled = true;
        }

        private async void OnLumiFavoriteItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
            => await NavigateLumiSidebarTagAsync(sender);

        private async void OnLumiDriveItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
            => await NavigateLumiSidebarTagAsync(sender);

        private async System.Threading.Tasks.Task NavigateLumiSidebarTagAsync(object sender)
        {
            if (sender is not FrameworkElement fe) return;
            if (fe.Tag is not string path || string.IsNullOrEmpty(path)) return;
            if (!System.IO.Directory.Exists(path)) { Helpers.DebugLogger.Log($"[LumiSidebar] path missing: {path}"); return; }
            var explorer = ViewModel?.ActiveExplorer;
            if (explorer == null) return;
            try
            {
                if (ViewModel.CurrentViewMode != ViewMode.MillerColumns &&
                    ViewModel.CurrentViewMode != ViewMode.Details &&
                    ViewModel.CurrentViewMode != ViewMode.IconSmall &&
                    ViewModel.CurrentViewMode != ViewMode.IconMedium &&
                    ViewModel.CurrentViewMode != ViewMode.IconLarge)
                {
                    ViewModel.CurrentViewMode = ViewMode.MillerColumns;
                }
                // Span-original favorite click pattern: build a FolderItem and call
                // NavigateTo(FolderItem) so the sidebar entry becomes COLUMN 1 itself
                // (no parent drive column above it). NavigateToPath would prepend the
                // full ancestor chain (e.g. D:\ before 2.Model_Data) which is wrong here.
                var leafName = System.IO.Path.GetFileName(path);
                if (string.IsNullOrEmpty(leafName)) leafName = path; // drives, UNC roots, etc.
                var folder = new Models.FolderItem { Name = leafName, Path = path };

                // Suppress NavigateTo's post-load auto-expand (ExplorerViewModel.cs:612-617):
                // when EnableAutoNavigation=false, the "if SelectedChild is folder, open column 2"
                // branch is skipped, so the user lands on column 1 with no second column eagerly
                // popped open. We restore EnableAutoNavigation right after NavigateTo so that
                // subsequent arrow-key navigation still auto-expands as before.
                bool prevAutoNav = explorer.EnableAutoNavigation;
                explorer.EnableAutoNavigation = false;
                try
                {
                    await explorer.NavigateTo(folder);
                }
                finally
                {
                    explorer.EnableAutoNavigation = (ViewModel.CurrentViewMode == ViewMode.MillerColumns)
                        ? true
                        : prevAutoNav;
                }

                UpdateViewModeVisibility();
                if (ViewModel.CurrentViewMode == ViewMode.MillerColumns) FocusColumnAsync(0);
                else FocusActiveView();
            }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[LumiSidebar] navigate '{path}' failed: {ex.Message}"); }
        }

        // ============================================================
        // Stage 8 — LumiPathBar segment click navigation
        // ============================================================
        private async void OnLumiPathSegmentClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not string fullPath || string.IsNullOrEmpty(fullPath)) return;
            var explorer = ViewModel?.ActiveExplorer;
            if (explorer == null) return;
            try { await explorer.NavigateToPath(fullPath); }
            catch (Exception ex) { Helpers.DebugLogger.Log($"[LumiPathBar] segment click '{fullPath}' failed: {ex.Message}"); }
        }

        // OnLumiPathSegmentLeftClick / OnLumiPathSegmentRightClick moved into the
        // LumiPanePathBar UserControl (Stage A of the toolbar refactor).

        private async void OnLumiSidebarItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is not Grid grid) return;
            // The sidebar item layout is: [icon at col 0] + [TextBlock label at col 1].
            string? label = null;
            foreach (var child in grid.Children)
            {
                if (child is TextBlock tb) { label = tb.Text; break; }
            }
            Helpers.DebugLogger.Log($"[LumiSidebar] tapped: '{label ?? "(null)"}'");
            if (string.IsNullOrEmpty(label)) return;

            string? path = label switch
            {
                "Desktop"         => Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Documents"       => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Downloads"       => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                "Pictures"        => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "Music"           => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                "Projects"        => @"D:\11.AI",
                "Local Disk (C:)" => @"C:\",
                "Data (D:)"       => @"D:\",
                "OneDrive"        => Environment.GetEnvironmentVariable("OneDrive"),
                _ => null
            };

            if (string.IsNullOrEmpty(path)) { Helpers.DebugLogger.Log($"[LumiSidebar] no path mapping for '{label}'"); return; }
            if (!System.IO.Directory.Exists(path)) { Helpers.DebugLogger.Log($"[LumiSidebar] path does not exist: {path}"); return; }

            var explorer = ViewModel?.ActiveExplorer;
            if (explorer == null) { Helpers.DebugLogger.Log("[LumiSidebar] ActiveExplorer null"); return; }
            try
            {
                // Switch out of Home/Settings/ActionLog into MillerColumns so navigation is visible.
                if (ViewModel.CurrentViewMode != ViewMode.MillerColumns &&
                    ViewModel.CurrentViewMode != ViewMode.Details &&
                    ViewModel.CurrentViewMode != ViewMode.IconSmall &&
                    ViewModel.CurrentViewMode != ViewMode.IconMedium &&
                    ViewModel.CurrentViewMode != ViewMode.IconLarge)
                {
                    ViewModel.CurrentViewMode = ViewMode.MillerColumns;
                }
                // Re-enable single-tap auto-navigation in MillerColumns (mockup behavior).
                // ShouldAutoNavigate is private on MainViewModel; setting true directly here
                // matches the default for MillerColumns (unless user has set MillerClickBehavior="double").
                if (ViewModel.CurrentViewMode == ViewMode.MillerColumns)
                {
                    explorer.EnableAutoNavigation = true;
                }
                await explorer.NavigateToPath(path);
                UpdateViewModeVisibility();
                if (ViewModel.CurrentViewMode == ViewMode.MillerColumns) FocusColumnAsync(0);
                else FocusActiveView();
                Helpers.DebugLogger.Log($"[LumiSidebar] navigated to '{path}', mode={ViewModel.CurrentViewMode}");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.Log($"[LumiSidebar] navigate failed for '{label}' -> '{path}': {ex.Message}");
            }
        }

        // ── Custom Caption Buttons (Stage S-3.21, borderless mode) ─────────
        // System min/max/close are suppressed by SetBorderAndTitleBar(false,
        // false); these handlers replace them. AppWindow.Presenter exposes
        // Minimize/Maximize/Restore so we just forward to it.

        private void OnCaptionMinimizeClick(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                p.Minimize();
        }

        private void OnCaptionMaximizeClick(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
            {
                if (p.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
                {
                    p.Restore();
                    if (CaptionMaximizeIcon != null) CaptionMaximizeIcon.Glyph = ""; // maximize
                }
                else
                {
                    p.Maximize();
                    if (CaptionMaximizeIcon != null) CaptionMaximizeIcon.Glyph = ""; // restore
                }
            }
        }

        private void OnCaptionCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Hover state for the custom-shaped close button. The Border child
        // (CaptionCloseHoverBg) carries CornerRadius="0,16,0,0" so the red
        // fill follows the WindowFrame's inner rounded corner instead of
        // painting over the hairline.
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
                    CaptionCloseGlyph.Foreground = GetThemeBrush("LumiTextSecondaryBrush");
            }
            catch { }
        }

        /// <summary>
        /// Stage S-3.24: clip the OS window hit-area to a rounded rect so the
        /// 4 outer corners are actually transparent (not painted with backdrop).
        /// Pattern ported from DragShelf ShelfWindow.UpdateXamlClip.
        ///
        /// Steps:
        ///   - Compute current pixel size from RootGrid (DIP) × DPI scale.
        ///   - Build a HRGN with CreateRoundRectRgn at radius matching
        ///     LumiWindowCornerRadius (currently 18) scaled by DPI.
        ///   - Pass it to SetWindowRgn(_hwnd, rgn, redraw=true). The OS now
        ///     owns the region; we don't DeleteObject it.
        /// </summary>
        private void ApplyRoundedWindowRegion()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (hwnd == IntPtr.Zero) return;

                // Pull the actual pixel size from the AppWindow (already in
                // physical pixels; RootGrid.ActualSize is DIP and we'd have
                // to multiply by scale anyway).
                int widthPx  = AppWindow.Size.Width;
                int heightPx = AppWindow.Size.Height;
                if (widthPx <= 0 || heightPx <= 0) return;

                uint dpi = Helpers.NativeMethods.GetDpiForWindow(hwnd);
                double scale = dpi > 0 ? dpi / 96.0 : 1.0;

                // Stage S-3.27 → S-3.32: OS region radius now MATCHES the XAML
                // LumiWindowCornerRadius (18) exactly — pattern lifted from
                // DragShelf ShelfWindow.UpdateXamlClip ("match XAML
                // CornerRadius=12 so border covers aliased edge").
                //
                // Why we used to add +6 px and why we removed it:
                //   With +6, the OS clip sat 6 px OUTSIDE the XAML curve.
                //   Inside the XAML curve: WindowFrame body (acrylic + tint).
                //   Between curve and OS clip: 6 px ring of acrylic ONLY
                //   (no XAML fill). That ring made the actual visible window
                //   edge sit 6 px OUTSIDE where the XAML 1 px BorderThickness
                //   line was being drawn. Result: a hairline floating inside
                //   the perceived window edge — looked weak / detached at
                //   the rounded corners.
                //
                //   With +0 (current), the GDI clip is right where XAML's
                //   1 px Border outline lives. The Direct2D anti-aliased
                //   stroke is drawn on top of the GDI clip boundary. Since
                //   XAML AA pixels at the curve are anywhere from 0%-100%
                //   alpha and the GDI clip only either preserves or kills
                //   each pixel, the visible curve = AA pixels that survive
                //   the binary OS clip. The bright top of our gradient
                //   BorderBrush (~22% white) makes that surviving edge
                //   read as a clean, continuous hairline at the actual
                //   window boundary — the corner finally looks "edged"
                //   instead of "fuzzy".
                // S-3.34 (incremental fix #1, single-step retry): Round → Floor.
                //   125%(18*1.25=22.5)/175%(18*1.75=31.5) 같은 fractional DPI에서
                //   Round는 GDI radius를 XAML curve보다 0.5px 크게 만들어 acrylic 링이
                //   노출되며 그 가장자리에 binary stair-step이 보임. Floor는 GDI radius
                //   ≤ XAML radius를 보장 → 보이는 곡선 = AA'd XAML border (계단은 stroke
                //   내부에 가려져 시각적으로 안 보임).
                int radiusPx = (int)System.Math.Floor(18 * scale);
                if (radiusPx < 1) radiusPx = 1;

                // CreateRoundRectRgn coords are inclusive on top/left and
                // exclusive on bottom/right; +1 prevents a 1px clip on the
                // far edge.
                IntPtr rgn = Helpers.NativeMethods.CreateRoundRectRgn(
                    0, 0, widthPx + 1, heightPx + 1,
                    radiusPx * 2, radiusPx * 2);
                if (rgn == IntPtr.Zero) return;

                // SetWindowRgn takes ownership; do NOT DeleteObject.
                Helpers.NativeMethods.SetWindowRgn(hwnd, rgn, true);
            }
            catch (System.Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow] ApplyRoundedWindowRegion failed: {ex.Message}");
            }
        }

    }
}
