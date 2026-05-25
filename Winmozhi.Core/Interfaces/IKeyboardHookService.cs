using System;

namespace Winmozhi.Core.Interfaces;

public interface IKeyboardHookService
{
    void StartHook();
    void StopHook();
    bool IsPopupVisible { get; set; }

    event EventHandler<string> OnWordTyped;
    event EventHandler<string> OnInsertRequested;
    event EventHandler<int> OnSelectionChangedRequested;

    void ReplaceWord(int backspaceCount, string malayalamWord, string trailingText = "");

    (double X, double Y) GetCaretPosition();
}
