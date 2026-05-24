using System;

namespace Winmozhi.Core.Interfaces;

public interface IKeyboardHookService
{
    void StartHook();
    void StopHook();
    bool IsPopupVisible { get; set; }

    event EventHandler<string> OnWordTyped;
    event EventHandler OnInsertRequested;

    // THIS IS THE NEW EVENT FOR THE ARROW KEYS
    event EventHandler<int> OnSelectionChangedRequested;

    void ReplaceWord(int backspaceCount, string malayalamWord);
    (double X, double Y) GetCaretPosition();
}