namespace LumiFiles.Controls
{
    /// <summary>
    /// Identifies which explorer pane a LumiPaneToolbar / LumiPanePathBar instance
    /// is rendering for. Drives explorer selection on click, active-pane indicator
    /// state, and per-mode visibility tweaks (e.g. disk-space text on the path bar).
    /// </summary>
    public enum PaneMode
    {
        /// <summary>Single (non-split) view — uses the active/left explorer.</summary>
        Single = 0,
        /// <summary>Split view, left pane.</summary>
        Left = 1,
        /// <summary>Split view, right pane.</summary>
        Right = 2,
    }
}
