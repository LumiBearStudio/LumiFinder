using LumiFiles.Services;
using System.Collections.Generic;
using System.Linq;

namespace LumiFiles.Models
{
    /// <summary>
    /// 커스텀 키보드 단축키 리매핑을 위한 Command ID 상수 클래스.
    /// 각 상수는 "lumifiles.카테고리.액션" 형식의 문자열.
    /// </summary>
    public static class ShortcutCommands
    {
        // ── Navigation ──────────────────────────────────────────
        public const string NavigateBack = "lumifiles.navigate.back";
        public const string NavigateForward = "lumifiles.navigate.forward";
        public const string NavigateUp = "lumifiles.navigate.up";
        public const string AddressBarFocus = "lumifiles.navigate.addressBar";
        public const string Search = "lumifiles.navigate.search";
        public const string FilterBar = "lumifiles.navigate.filterBar";

        // ── Edit ────────────────────────────────────────────────
        public const string Copy = "lumifiles.edit.copy";
        public const string Cut = "lumifiles.edit.cut";
        public const string Paste = "lumifiles.edit.paste";
        public const string PasteAsShortcut = "lumifiles.edit.pasteAsShortcut";
        public const string Delete = "lumifiles.edit.delete";
        public const string PermanentDelete = "lumifiles.edit.permanentDelete";
        public const string Rename = "lumifiles.edit.rename";
        public const string Duplicate = "lumifiles.edit.duplicate";
        public const string NewFolder = "lumifiles.edit.newFolder";
        public const string Undo = "lumifiles.edit.undo";
        public const string Redo = "lumifiles.edit.redo";

        // ── Selection ───────────────────────────────────────────
        public const string SelectAll = "lumifiles.selection.selectAll";
        public const string SelectNone = "lumifiles.selection.selectNone";
        public const string InvertSelection = "lumifiles.selection.invert";

        // ── View ────────────────────────────────────────────────
        public const string ViewMiller = "lumifiles.view.miller";
        public const string ViewDetails = "lumifiles.view.details";
        public const string ViewList = "lumifiles.view.list";
        public const string ViewIcon = "lumifiles.view.icon";
        public const string ToggleSplitView = "lumifiles.view.splitView";
        public const string TogglePreview = "lumifiles.view.preview";
        public const string EqualizeColumns = "lumifiles.view.equalizeColumns";
        public const string AutoFitColumns = "lumifiles.view.autoFitColumns";
        public const string Refresh = "lumifiles.view.refresh";
        public const string ToggleHidden = "lumifiles.view.toggleHidden";
        public const string ToggleExtensions = "lumifiles.view.toggleExtensions";
        public const string Fullscreen = "lumifiles.view.fullscreen";

        // ── Tab ─────────────────────────────────────────────────
        public const string NewTab = "lumifiles.tab.new";
        public const string CloseTab = "lumifiles.tab.close";
        public const string NextTab = "lumifiles.tab.next";
        public const string PrevTab = "lumifiles.tab.prev";
        public const string OpenInNewTab = "lumifiles.tab.openSelectedInNew";
        public const string SwitchPane = "lumifiles.view.switchPane";

        // ── Window ──────────────────────────────────────────────
        public const string NewWindow = "lumifiles.window.new";
        public const string OpenTerminal = "lumifiles.window.terminal";
        public const string OpenSettings = "lumifiles.window.settings";
        public const string ShowProperties = "lumifiles.window.properties";
        public const string ShowHelp = "lumifiles.window.help";
        public const string OpenHelp = "lumifiles.help.open";
        public const string QuickLook = "lumifiles.quickLook.toggle";

        // ── Workspace ──────────────────────────────────────────
        public const string SaveWorkspace = "lumifiles.workspace.save";
        public const string OpenWorkspacePalette = "lumifiles.workspace.open";

        // ── Shelf ───────────────────────────────────────────────
        public const string ShelfAdd = "lumifiles.shelf.add";
        public const string ShelfToggle = "lumifiles.shelf.toggle";
        public const string ShelfMoveHere = "lumifiles.shelf.moveHere";
        public const string ShelfCopyHere = "lumifiles.shelf.copyHere";
        public const string ShelfClear = "lumifiles.shelf.clear";

        // ── Command Palette (HIDDEN) ────────────────────────────
        // 2026-04-10: 파일 탐색기 워크플로우와 부합하지 않아 숨김 처리됨.
        // 상수와 카테고리 등록은 모두 유지 (사용자가 키 재할당 시 즉시 동작).
        // 자세한 사유는 MainWindow.CommandPaletteHandler.cs 상단 주석 참조.
        public const string OpenCommandPalette = "lumifiles.commandPalette.open";

        // ── Settings: Toggle (즉시 적용 boolean) ────────────────
        public const string SettingsToggleHidden = "lumifiles.settings.toggle.hidden";
        public const string SettingsToggleExtensions = "lumifiles.settings.toggle.extensions";
        public const string SettingsToggleCheckboxes = "lumifiles.settings.toggle.checkboxes";
        public const string SettingsToggleThumbnails = "lumifiles.settings.toggle.thumbnails";
        public const string SettingsToggleQuickLook = "lumifiles.settings.toggle.quickLook";
        public const string SettingsToggleWasd = "lumifiles.settings.toggle.wasd";
        public const string SettingsToggleConfirmDelete = "lumifiles.settings.toggle.confirmDelete";
        public const string SettingsTogglePreviewFolderInfo = "lumifiles.settings.toggle.previewFolderInfo";
        public const string SettingsToggleDefaultPreview = "lumifiles.settings.toggle.defaultPreview";
        public const string SettingsToggleFavoritesTree = "lumifiles.settings.toggle.favoritesTree";
        public const string SettingsToggleShelf = "lumifiles.settings.toggle.shelf";
        public const string SettingsToggleShelfSave = "lumifiles.settings.toggle.shelfSave";
        public const string SettingsToggleContextMenu = "lumifiles.settings.toggle.contextMenu";
        public const string SettingsToggleTray = "lumifiles.settings.toggle.tray";
        public const string SettingsToggleWindowPosition = "lumifiles.settings.toggle.windowPosition";
        public const string SettingsToggleGitIntegration = "lumifiles.settings.toggle.git";
        public const string SettingsToggleHexPreview = "lumifiles.settings.toggle.hexPreview";
        public const string SettingsToggleFileHash = "lumifiles.settings.toggle.fileHash";
        public const string SettingsToggleShellExtensions = "lumifiles.settings.toggle.shellExt";
        public const string SettingsToggleWindowsShellExtras = "lumifiles.settings.toggle.shellExtras";
        public const string SettingsToggleCopilotMenu = "lumifiles.settings.toggle.copilot";

        // Sidebar sections
        public const string SettingsSidebarHome = "lumifiles.settings.sidebar.home";
        public const string SettingsSidebarFavorites = "lumifiles.settings.sidebar.favorites";
        public const string SettingsSidebarDrives = "lumifiles.settings.sidebar.drives";
        public const string SettingsSidebarCloud = "lumifiles.settings.sidebar.cloud";
        public const string SettingsSidebarNetwork = "lumifiles.settings.sidebar.network";
        public const string SettingsSidebarRecycleBin = "lumifiles.settings.sidebar.recycleBin";

        // ── Settings: Select (선택형) ───────────────────────────
        public const string SettingsThemeSystem = "lumifiles.settings.theme.system";
        public const string SettingsThemeLight = "lumifiles.settings.theme.light";
        public const string SettingsThemeDark = "lumifiles.settings.theme.dark";

        public const string SettingsDensityCompact = "lumifiles.settings.density.compact";
        public const string SettingsDensityComfortable = "lumifiles.settings.density.comfortable";
        public const string SettingsDensitySpacious = "lumifiles.settings.density.spacious";

        public const string SettingsLanguageSystem = "lumifiles.settings.language.system";
        public const string SettingsLanguageEn = "lumifiles.settings.language.en";
        public const string SettingsLanguageKo = "lumifiles.settings.language.ko";
        public const string SettingsLanguageJa = "lumifiles.settings.language.ja";
        public const string SettingsLanguageZhHans = "lumifiles.settings.language.zh-Hans";
        public const string SettingsLanguageZhHant = "lumifiles.settings.language.zh-Hant";
        public const string SettingsLanguageDe = "lumifiles.settings.language.de";
        public const string SettingsLanguageEs = "lumifiles.settings.language.es";
        public const string SettingsLanguageFr = "lumifiles.settings.language.fr";
        public const string SettingsLanguagePtBr = "lumifiles.settings.language.pt-BR";

        public const string SettingsIconPackRemix = "lumifiles.settings.iconPack.remix";
        public const string SettingsIconPackPhosphor = "lumifiles.settings.iconPack.phosphor";
        public const string SettingsIconPackTabler = "lumifiles.settings.iconPack.tabler";

        // ── Settings: Open Section (점프) ───────────────────────
        public const string SettingsOpenGeneral = "lumifiles.settings.open.general";
        public const string SettingsOpenAppearance = "lumifiles.settings.open.appearance";
        public const string SettingsOpenBrowsing = "lumifiles.settings.open.browsing";
        public const string SettingsOpenSidebar = "lumifiles.settings.open.sidebar";
        public const string SettingsOpenTools = "lumifiles.settings.open.tools";
        public const string SettingsOpenShortcuts = "lumifiles.settings.open.shortcuts";
        public const string SettingsOpenAdvanced = "lumifiles.settings.open.advanced";

        // ── 내부 레지스트리 ─────────────────────────────────────

        private static readonly Dictionary<string, string> _categories = new()
        {
            // Navigation
            { NavigateBack, "Navigation" },
            { NavigateForward, "Navigation" },
            { NavigateUp, "Navigation" },
            { AddressBarFocus, "Navigation" },
            { Search, "Navigation" },
            { FilterBar, "Navigation" },
            // Edit
            { Copy, "Edit" },
            { Cut, "Edit" },
            { Paste, "Edit" },
            { PasteAsShortcut, "Edit" },
            { Delete, "Edit" },
            { PermanentDelete, "Edit" },
            { Rename, "Edit" },
            { Duplicate, "Edit" },
            { NewFolder, "Edit" },
            { Undo, "Edit" },
            { Redo, "Edit" },
            // Selection
            { SelectAll, "Selection" },
            { SelectNone, "Selection" },
            { InvertSelection, "Selection" },
            // View
            { ViewMiller, "View" },
            { ViewDetails, "View" },
            { ViewList, "View" },
            { ViewIcon, "View" },
            { ToggleSplitView, "View" },
            { TogglePreview, "View" },
            { EqualizeColumns, "View" },
            { AutoFitColumns, "View" },
            { Refresh, "View" },
            { ToggleHidden, "View" },
            { ToggleExtensions, "View" },
            { Fullscreen, "View" },
            // Tab
            { NewTab, "Tab" },
            { CloseTab, "Tab" },
            { NextTab, "Tab" },
            { PrevTab, "Tab" },
            { OpenInNewTab, "Tab" },
            { SwitchPane, "View" },
            // Window
            { NewWindow, "Window" },
            { OpenTerminal, "Window" },
            { OpenSettings, "Window" },
            { ShowProperties, "Window" },
            { ShowHelp, "Window" },
            { OpenHelp, "Help" },
            // Quick Look
            { QuickLook, "QuickLook" },
            // Workspace
            { SaveWorkspace, "Workspace" },
            { OpenWorkspacePalette, "Workspace" },
            // Shelf
            { ShelfAdd, "Shelf" },
            { ShelfToggle, "Shelf" },
            { ShelfMoveHere, "Shelf" },
            { ShelfCopyHere, "Shelf" },
            { ShelfClear, "Shelf" },
            // Command Palette
            { OpenCommandPalette, "CommandPalette" },
            // Settings: Toggle
            { SettingsToggleHidden, "Settings" },
            { SettingsToggleExtensions, "Settings" },
            { SettingsToggleCheckboxes, "Settings" },
            { SettingsToggleThumbnails, "Settings" },
            { SettingsToggleQuickLook, "Settings" },
            { SettingsToggleWasd, "Settings" },
            { SettingsToggleConfirmDelete, "Settings" },
            { SettingsTogglePreviewFolderInfo, "Settings" },
            { SettingsToggleDefaultPreview, "Settings" },
            { SettingsToggleFavoritesTree, "Settings" },
            { SettingsToggleShelf, "Settings" },
            { SettingsToggleShelfSave, "Settings" },
            { SettingsToggleContextMenu, "Settings" },
            { SettingsToggleTray, "Settings" },
            { SettingsToggleWindowPosition, "Settings" },
            { SettingsToggleGitIntegration, "Settings" },
            { SettingsToggleHexPreview, "Settings" },
            { SettingsToggleFileHash, "Settings" },
            { SettingsToggleShellExtensions, "Settings" },
            { SettingsToggleWindowsShellExtras, "Settings" },
            { SettingsToggleCopilotMenu, "Settings" },
            // Sidebar
            { SettingsSidebarHome, "Sidebar" },
            { SettingsSidebarFavorites, "Sidebar" },
            { SettingsSidebarDrives, "Sidebar" },
            { SettingsSidebarCloud, "Sidebar" },
            { SettingsSidebarNetwork, "Sidebar" },
            { SettingsSidebarRecycleBin, "Sidebar" },
            // Theme
            { SettingsThemeSystem, "Theme" },
            { SettingsThemeLight, "Theme" },
            { SettingsThemeDark, "Theme" },
            // Density
            { SettingsDensityCompact, "Density" },
            { SettingsDensityComfortable, "Density" },
            { SettingsDensitySpacious, "Density" },
            // Language
            { SettingsLanguageSystem, "Language" },
            { SettingsLanguageEn, "Language" },
            { SettingsLanguageKo, "Language" },
            { SettingsLanguageJa, "Language" },
            { SettingsLanguageZhHans, "Language" },
            { SettingsLanguageZhHant, "Language" },
            { SettingsLanguageDe, "Language" },
            { SettingsLanguageEs, "Language" },
            { SettingsLanguageFr, "Language" },
            { SettingsLanguagePtBr, "Language" },
            // Icon Pack
            { SettingsIconPackRemix, "IconPack" },
            { SettingsIconPackPhosphor, "IconPack" },
            { SettingsIconPackTabler, "IconPack" },
            // Settings open sections
            { SettingsOpenGeneral, "SettingsSection" },
            { SettingsOpenAppearance, "SettingsSection" },
            { SettingsOpenBrowsing, "SettingsSection" },
            { SettingsOpenSidebar, "SettingsSection" },
            { SettingsOpenTools, "SettingsSection" },
            { SettingsOpenShortcuts, "SettingsSection" },
            { SettingsOpenAdvanced, "SettingsSection" },
        };

        /// <summary>
        /// 로컬라이즈된 표시 이름을 가져옵니다. LocalizationService에서 키를 찾지 못하면 commandId를 그대로 반환합니다.
        /// 로컬라이즈 키 형식: "Shortcut_span_category_action" (점을 밑줄로 치환)
        /// </summary>
        private static readonly Dictionary<string, string> _displayNameKeys = new()
        {
            // Navigation
            { NavigateBack, "Shortcut_NavigateBack" },
            { NavigateForward, "Shortcut_NavigateForward" },
            { NavigateUp, "Shortcut_NavigateUp" },
            { AddressBarFocus, "Shortcut_AddressBarFocus" },
            { Search, "Shortcut_Search" },
            { FilterBar, "Shortcut_FilterBar" },
            // Edit
            { Copy, "Shortcut_Copy" },
            { Cut, "Shortcut_Cut" },
            { Paste, "Shortcut_Paste" },
            { PasteAsShortcut, "Shortcut_PasteAsShortcut" },
            { Delete, "Shortcut_Delete" },
            { PermanentDelete, "Shortcut_PermanentDelete" },
            { Rename, "Shortcut_Rename" },
            { Duplicate, "Shortcut_Duplicate" },
            { NewFolder, "Shortcut_NewFolder" },
            { Undo, "Shortcut_Undo" },
            { Redo, "Shortcut_Redo" },
            // Selection
            { SelectAll, "Shortcut_SelectAll" },
            { SelectNone, "Shortcut_SelectNone" },
            { InvertSelection, "Shortcut_InvertSelection" },
            // View
            { ViewMiller, "Shortcut_ViewMiller" },
            { ViewDetails, "Shortcut_ViewDetails" },
            { ViewList, "Shortcut_ViewList" },
            { ViewIcon, "Shortcut_ViewIcon" },
            { ToggleSplitView, "Shortcut_ToggleSplitView" },
            { TogglePreview, "Shortcut_TogglePreview" },
            { EqualizeColumns, "Shortcut_EqualizeColumns" },
            { AutoFitColumns, "Shortcut_AutoFitColumns" },
            { Refresh, "Shortcut_Refresh" },
            { ToggleHidden, "Shortcut_ToggleHidden" },
            { ToggleExtensions, "Shortcut_ToggleExtensions" },
            { Fullscreen, "Shortcut_Fullscreen" },
            // Window
            { NewTab, "Shortcut_NewTab" },
            { CloseTab, "Shortcut_CloseTab" },
            { NextTab, "Shortcut_NextTab" },
            { PrevTab, "Shortcut_PrevTab" },
            { SwitchPane, "Shortcut_SwitchPane" },
            { NewWindow, "Shortcut_NewWindow" },
            { OpenTerminal, "Shortcut_OpenTerminal" },
            { OpenSettings, "Shortcut_OpenSettings" },
            { ShowProperties, "Shortcut_ShowProperties" },
            { ShowHelp, "Shortcut_ShowHelp" },
            { OpenHelp, "Shortcut_OpenHelp" },
            { OpenInNewTab, "Shortcut_OpenInNewTab" },
            { QuickLook, "Shortcut_QuickLook" },
            // Workspace
            { SaveWorkspace, "Shortcut_SaveWorkspace" },
            { OpenWorkspacePalette, "Shortcut_OpenWorkspacePalette" },
            // Shelf
            { ShelfAdd, "Shortcut_ShelfAdd" },
            { ShelfToggle, "Shortcut_ShelfToggle" },
            { ShelfMoveHere, "Shortcut_ShelfMoveHere" },
            { ShelfCopyHere, "Shortcut_ShelfCopyHere" },
            { ShelfClear, "Shortcut_ShelfClear" },
            // Command Palette
            { OpenCommandPalette, "Shortcut_OpenCommandPalette" },
        };

        /// <summary>
        /// 리매핑 불가능한 시스템 커맨드 목록.
        /// 이 커맨드들은 OS 또는 WinUI 프레임워크 수준에서 처리되므로 사용자가 변경할 수 없습니다.
        /// </summary>
        private static readonly HashSet<string> _nonRemappable = new()
        {
            Fullscreen,  // F11 — OS 수준
        };

        /// <summary>
        /// 사용자에게 보여줄 로컬라이즈된 커맨드 표시 이름을 반환합니다.
        /// </summary>
        public static string GetDisplayName(string commandId)
        {
            if (_displayNameKeys.TryGetValue(commandId, out var key))
            {
                var localized = LocalizationService.L(key);
                // LocalizationService가 키를 찾지 못하면 키 자체를 반환하므로, 그 경우 fallback 사용
                if (!string.IsNullOrEmpty(localized) && localized != key)
                    return localized;
            }

            // Fallback: commandId에서 마지막 세그먼트를 PascalCase로 변환
            var lastDot = commandId.LastIndexOf('.');
            return lastDot >= 0 ? commandId.Substring(lastDot + 1) : commandId;
        }

        /// <summary>
        /// 커맨드가 속한 카테고리를 반환합니다.
        /// </summary>
        public static string GetCategory(string commandId)
        {
            return _categories.TryGetValue(commandId, out var category) ? category : "Unknown";
        }

        /// <summary>
        /// 등록된 모든 커맨드 ID 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<string> GetAllCommands()
        {
            return _categories.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// 해당 커맨드가 사용자에 의해 리매핑 가능한지 여부를 반환합니다.
        /// </summary>
        public static bool IsRemappable(string commandId)
        {
            return _categories.ContainsKey(commandId) && !_nonRemappable.Contains(commandId);
        }

        /// <summary>
        /// 특정 카테고리에 속한 커맨드 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<string> GetCommandsByCategory(string category)
        {
            return _categories
                .Where(kvp => kvp.Value == category)
                .Select(kvp => kvp.Key)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// 모든 카테고리 이름 목록을 반환합니다.
        /// </summary>
        public static IReadOnlyList<string> GetAllCategories()
        {
            return _categories.Values.Distinct().ToList().AsReadOnly();
        }
    }
}
