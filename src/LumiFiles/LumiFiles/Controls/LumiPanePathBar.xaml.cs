using LumiFiles.Models;
using LumiFiles.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;

namespace LumiFiles.Controls
{
    /// <summary>
    /// Pane breadcrumb path bar — used by both panes in split view AND the outer
    /// single-mode bar. The Mode DP picks which explorer to navigate when a segment
    /// is clicked (Single/Left use the left/active explorer, Right uses the right
    /// explorer). PathSegments is bound to whatever the host passes in.
    /// </summary>
    public sealed partial class LumiPanePathBar : UserControl
    {
        public LumiPanePathBar()
        {
            this.InitializeComponent();
        }

        // ── Mode (Single / Left / Right) ────────────────────────────────────
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(PaneMode), typeof(LumiPanePathBar),
                new PropertyMetadata(PaneMode.Single));
        public PaneMode Mode
        {
            get => (PaneMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        // ── PathSegments (the actual ObservableCollection from the explorer) ─
        public static readonly DependencyProperty PathSegmentsProperty =
            DependencyProperty.Register(nameof(PathSegments), typeof(object), typeof(LumiPanePathBar),
                new PropertyMetadata(null));
        public object? PathSegments
        {
            get => GetValue(PathSegmentsProperty);
            set => SetValue(PathSegmentsProperty, value);
        }

        // Segment click — find the right explorer via Mode and navigate.
        private async void OnSegmentClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not string fullPath || string.IsNullOrEmpty(fullPath)) return;

            var explorer = ResolveExplorer();
            if (explorer == null) return;

            try { await explorer.NavigateToPath(fullPath); }
            catch (System.Exception ex)
            {
                Helpers.DebugLogger.Log($"[LumiPanePathBar:{Mode}] segment click '{fullPath}' failed: {ex.Message}");
            }
        }

        private ExplorerViewModel? ResolveExplorer()
        {
            var vm = (App.Current as App)?.Services?.GetService(typeof(MainViewModel)) as MainViewModel;
            if (vm == null) return null;
            return Mode switch
            {
                PaneMode.Right => vm.RightExplorer,
                _ => vm.Explorer
            };
        }

        // ── Overflow handling (Stage S-3.4) ─────────────────────────────────
        // Ported from AddressBarControl. When the breadcrumb content is wider
        // than the scroller, auto-scroll to the right edge so the leaf segment
        // (current folder) stays visible, and show OverflowIndicator on the
        // left so the cut-off is explicit.

        private void OnScrollerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                sv.ChangeView(sv.ScrollableWidth, null, null, true);
                DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() => UpdateOverflow(sv));
            }
        }

        private void OnContentSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var sv = SegmentsScroller;
            sv.ChangeView(sv.ScrollableWidth, null, null, true);
            DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() => UpdateOverflow(sv));
        }

        private void OnScrollerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            if (sender is ScrollViewer sv)
                UpdateOverflow(sv);
        }

        private void UpdateOverflow(ScrollViewer sv)
        {
            OverflowIndicator.Visibility = sv.HorizontalOffset > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
