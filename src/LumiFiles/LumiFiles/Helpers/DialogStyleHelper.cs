using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace LumiFiles.Helpers
{
    /// <summary>
    /// S-3.36 (최종): ContentDialog의 모든 버튼에 Lumi 통합 스타일 + 포커스 따라가는 amber 배경.
    ///
    /// 배경:
    ///   1) WinUI 3 generic.xaml의 ContentDialog 템플릿이 `{ThemeResource DefaultButtonStyle}`
    ///      참조를 parse-time에 frozen → 시스템 DefaultButtonStyle x:Key override 적용 안 됨
    ///      → 다이얼로그 인스턴스에 직접 CloseButtonStyle/SecondaryButtonStyle/PrimaryButtonStyle을 세팅
    ///   2) WinUI 3 Button은 자체 FocusStates VisualStateGroup이 없음 (CommonStates만 존재)
    ///      → Focused VisualState로 amber 배경 토글 불가 → 코드에서 GotFocus/LostFocus 이벤트로 직접 토글
    ///   3) ContentDialog DefaultButtonStates VisualState가 PrimaryButton.Style을 AccentButtonStyle로
    ///      swap하는 동작이 있으므로 AccentButtonStyle은 LumiSecondaryButtonStyle의 BasedOn alias로
    ///      두어 swap이 일어나도 visual 동일하게 유지
    ///
    /// 사용:
    ///   ApplyLumiStyle(dialog) 호출만으로 다음 모두 자동 처리:
    ///   - 세 버튼(Primary/Secondary/Close)에 LumiSecondaryButtonStyle 명시 할당
    ///   - dialog.Loaded 시 visual tree에서 버튼 찾아 GotFocus/LostFocus 이벤트 hook
    ///   - 포커스 받은 버튼만 amber Background, 잃으면 Style 기본값(gray)으로 복귀
    /// </summary>
    internal static class DialogStyleHelper
    {
        public static void ApplyLumiStyle(ContentDialog dlg)
        {
            if (dlg == null) return;
            try
            {
                var resources = Application.Current?.Resources;
                if (resources == null) return;

                // 1) 세 버튼 Style을 LumiSecondaryButtonStyle로 명시 할당
                //    (AccentButtonStyle BasedOn alias라 PrimaryAsDefaultButton VisualState가
                //     swap을 시도해도 visual은 동일)
                if (resources.TryGetValue("LumiSecondaryButtonStyle", out var styleObj)
                    && styleObj is Style style)
                {
                    dlg.CloseButtonStyle = style;
                    dlg.SecondaryButtonStyle = style;
                    dlg.PrimaryButtonStyle = style;
                }

                // 2) Loaded 시점에 visual tree에서 버튼 찾아 포커스 hook 부착
                //    (Loaded 전에는 Template이 적용 안 되어 button instance가 없음)
                dlg.Loaded -= OnDialogLoaded;
                dlg.Loaded += OnDialogLoaded;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[DialogStyleHelper] ApplyLumiStyle failed: {ex.Message}");
            }
        }

        private static void OnDialogLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ContentDialog dlg) return;
            try
            {
                // ContentDialog 템플릿의 named parts: PrimaryButton, SecondaryButton, CloseButton
                var primary  = FindDescendantByName(dlg, "PrimaryButton")  as Button;
                var secondary = FindDescendantByName(dlg, "SecondaryButton") as Button;
                var close    = FindDescendantByName(dlg, "CloseButton")   as Button;

                AttachFocusHandler(primary);
                AttachFocusHandler(secondary);
                AttachFocusHandler(close);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[DialogStyleHelper] OnDialogLoaded failed: {ex.Message}");
            }
        }

        private static void AttachFocusHandler(Button? btn)
        {
            if (btn == null) return;
            // 중복 부착 방지
            btn.GotFocus -= OnButtonGotFocus;
            btn.LostFocus -= OnButtonLostFocus;
            btn.GotFocus += OnButtonGotFocus;
            btn.LostFocus += OnButtonLostFocus;
        }

        private static void OnButtonGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            try
            {
                var resources = Application.Current?.Resources;
                if (resources == null) return;

                // amber Background를 Local Value로 설정 → Style/VisualState보다 우선
                if (resources.TryGetValue("LumiAccentSolidBrush", out var bgObj) && bgObj is Brush bg)
                    btn.Background = bg;
                if (resources.TryGetValue("LumiAccentForegroundBrush", out var fgObj) && fgObj is Brush fg)
                    btn.Foreground = fg;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[DialogStyleHelper] OnButtonGotFocus failed: {ex.Message}");
            }
        }

        private static void OnButtonLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            try
            {
                // Local Value 제거 → Style의 Setter 기본값(gray)으로 복귀
                btn.ClearValue(Control.BackgroundProperty);
                btn.ClearValue(Control.ForegroundProperty);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[DialogStyleHelper] OnButtonLostFocus failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Visual Tree를 BFS로 walk해서 x:Name이 일치하는 자손을 찾는다.
        /// Template parts (PrimaryButton, SecondaryButton, CloseButton) 접근용.
        /// </summary>
        private static FrameworkElement? FindDescendantByName(DependencyObject root, string name)
        {
            if (root == null) return null;
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current is FrameworkElement fe && fe.Name == name)
                    return fe;
                int count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(current, i));
                }
            }
            return null;
        }
    }
}
