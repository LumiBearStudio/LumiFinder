// =============================================================================
//  MainWindow.LocalizationHandler.cs (S-3.40)
//
//  Centralizes all i18n updates for MainWindow XAML elements that don't auto-
//  bind to LocalizationService. Includes:
//    - Titlebar caption tooltips (minimize / maximize / close)
//    - Sidebar tooltips (Workspaces, Recycle Bin, etc.)
//    - LumiToolbar tooltips (back / forward / view mode / sort / etc.)
//    - Sort menu flyout items
//
//  Pattern: ToolTipService.SetToolTip(element, _loc.Get("...")) for tooltips,
//  element.Text/Content = _loc.Get("...") for text content.
//
//  Called once after _loc is ready (init flow) and re-called on language
//  change (LocalizationService.LanguageChanged event).
// =============================================================================

using Microsoft.UI.Xaml.Controls;
using LumiFiles.Services;

namespace LumiFiles
{
    public sealed partial class MainWindow
    {
        /// <summary>
        /// MainWindow 의 모든 hard-coded 사용자 노출 문자열을 i18n 으로 갱신.
        /// _loc 이 준비된 시점(앱 시작 + 언어 변경) 에 호출.
        /// </summary>
        private void LoadMainWindowLocalization()
        {
            if (_loc == null) return;

            try
            {
                // ── Titlebar caption buttons ──────────────────────────────
                if (CaptionMinimizeButton != null)
                    ToolTipService.SetToolTip(CaptionMinimizeButton, _loc.Get("Caption_Minimize"));
                if (CaptionMaximizeButton != null)
                    ToolTipService.SetToolTip(CaptionMaximizeButton, _loc.Get("Caption_Maximize"));
                if (CaptionCloseButton != null)
                    ToolTipService.SetToolTip(CaptionCloseButton, _loc.Get("Caption_Close"));

                // ── LumiSidebar 우상단 / 하단 ─────────────────────────────
                if (LumiSidebarWorkspaceRow != null)
                    ToolTipService.SetToolTip(LumiSidebarWorkspaceRow, _loc.Get("Toolbar_Workspaces"));

                // ── LumiToolbar nav 버튼 (Back/Forward/Up) ─────────────────
                if (BackButton != null)
                    ToolTipService.SetToolTip(BackButton, _loc.Get("Shortcut_NavigateBack") + " (Alt+Left)");
                if (ForwardButton != null)
                    ToolTipService.SetToolTip(ForwardButton, _loc.Get("Shortcut_NavigateForward") + " (Alt+Right)");
                if (UpButton != null)
                    ToolTipService.SetToolTip(UpButton, _loc.Get("Shortcut_NavigateUp"));
                if (CopyPathButton != null)
                    ToolTipService.SetToolTip(CopyPathButton, _loc.Get("Toolbar_CopyPath"));

                // ── LumiToolbar 액션 (Cut/Copy/Paste/Rename/Delete) ────────
                if (NewFolderButton != null)
                    ToolTipService.SetToolTip(NewFolderButton, _loc.Get("Shortcut_NewFolder") + " (Ctrl+Shift+N)");
                if (NewItemDropdown != null)
                    ToolTipService.SetToolTip(NewItemDropdown, _loc.Get("Toolbar_NewItem"));
                if (ToolbarCutButton != null)
                    ToolTipService.SetToolTip(ToolbarCutButton, _loc.Get("Shortcut_Cut") + " (Ctrl+X)");
                if (ToolbarCopyButton != null)
                    ToolTipService.SetToolTip(ToolbarCopyButton, _loc.Get("Shortcut_Copy") + " (Ctrl+C)");
                if (ToolbarPasteButton != null)
                    ToolTipService.SetToolTip(ToolbarPasteButton, _loc.Get("Shortcut_Paste") + " (Ctrl+V)");
                if (ToolbarRenameButton != null)
                    ToolTipService.SetToolTip(ToolbarRenameButton, _loc.Get("Shortcut_Rename") + " (F2)");
                if (ToolbarDeleteButton != null)
                    ToolTipService.SetToolTip(ToolbarDeleteButton, _loc.Get("Shortcut_Delete") + " (Del)");

                // ── Sort & View Mode ─────────────────────────────────────
                if (SortButton != null)
                    ToolTipService.SetToolTip(SortButton, _loc.Get("Toolbar_Sort"));
                if (ViewModeButton != null)
                    ToolTipService.SetToolTip(ViewModeButton, _loc.Get("Toolbar_ViewMode"));
                if (LumiPreviewButton != null)
                    ToolTipService.SetToolTip(LumiPreviewButton, _loc.Get("Shortcut_TogglePreview") + " (Ctrl+Shift+P)");
                if (LumiSplitButton != null)
                    ToolTipService.SetToolTip(LumiSplitButton, _loc.Get("Shortcut_ToggleSplitView") + " (Ctrl+Shift+E)");

                // ── Sort menu flyout items ────────────────────────────────
                if (SortByNameItem != null) SortByNameItem.Text = _loc.Get("Sort_Name");
                if (SortByDateItem != null) SortByDateItem.Text = _loc.Get("Sort_Date");
                if (SortBySizeItem != null) SortBySizeItem.Text = _loc.Get("Sort_Size");
                if (SortByTypeItem != null) SortByTypeItem.Text = _loc.Get("Sort_Type");
                if (SortAscendingItem != null) SortAscendingItem.Text = _loc.Get("Sort_Ascending");
                if (SortDescendingItem != null) SortDescendingItem.Text = _loc.Get("Sort_Descending");

                // ── 휴지통 모드 툴바 ──────────────────────────────────────
                if (ToolbarRestoreButton != null)
                    ToolTipService.SetToolTip(ToolbarRestoreButton, _loc.Get("Toolbar_RecycleBinRestore") + " (Ctrl+Z)");
                if (ToolbarDeletePermButton != null)
                    ToolTipService.SetToolTip(ToolbarDeletePermButton, _loc.Get("Toolbar_RecycleBinDeletePermanent") + " (Delete)");
                if (ToolbarEmptyBinButton != null)
                    ToolTipService.SetToolTip(ToolbarEmptyBinButton, _loc.Get("Toolbar_RecycleBinEmpty"));
                // 휴지통 모드 새로고침 버튼은 x:Name 없음 — 추후 필요 시 XAML 추가

                // ── Search ──────────────────────────────────────────────
                if (LumiSearchButton != null)
                    ToolTipService.SetToolTip(LumiSearchButton, _loc.Get("Shortcut_Search") + " (Ctrl+F)");
                if (NewTabButton != null)
                    ToolTipService.SetToolTip(NewTabButton, _loc.Get("Shortcut_NewTab") + " (Ctrl+T)");
            }
            catch (System.Exception ex)
            {
                Helpers.DebugLogger.Log($"[MainWindow.Localization] LoadMainWindowLocalization failed: {ex.Message}");
            }
        }
    }
}
