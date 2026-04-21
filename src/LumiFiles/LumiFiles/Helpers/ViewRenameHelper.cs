using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LumiFiles.Helpers;

internal static class ViewRenameHelper
{
    /// <summary>
    /// F2 cycling 선택 영역을 TextBox에 적용.
    /// cycle 0: 이름만 (확장자 제외), cycle 1: 전체, cycle 2: 확장자만.
    /// 폴더일 경우 항상 전체 선택.
    /// </summary>
    internal static void ApplyRenameSelection(TextBox textBox, bool isFolder, int selectionCycle, DispatcherQueue dispatcherQueue)
    {
        textBox.Focus(FocusState.Keyboard);

        // Low 우선순위: GridView(List/Icon 뷰)에서 Focus()가 TextBox에 SelectAll()을
        // 내부적으로 적용하는데, Normal 우선순위는 이보다 먼저 실행될 수 있음.
        // Low로 지연하여 TextBox의 내부 focus 처리가 완료된 후 선택 영역을 덮어씀.
        dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (!isFolder && !string.IsNullOrEmpty(textBox.Text))
            {
                int dotIndex = textBox.Text.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    switch (selectionCycle)
                    {
                        case 0: // Name only (exclude extension)
                            textBox.Select(0, dotIndex);
                            break;
                        case 1: // All (including extension)
                            textBox.SelectAll();
                            break;
                        case 2: // Extension only
                            textBox.Select(dotIndex + 1, textBox.Text.Length - dotIndex - 1);
                            break;
                    }
                }
                else
                {
                    textBox.SelectAll();
                }
            }
            else
            {
                textBox.SelectAll();
            }
        });
    }
}
