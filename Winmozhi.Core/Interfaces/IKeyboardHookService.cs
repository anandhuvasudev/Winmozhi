using System;

namespace Winmozhi.Core.Interfaces;

public interface IKeyboardHookService
{
    void StartHook();
    void StopHook();
    bool IsPopupVisible { get; set; }

    event EventHandler<string> OnWordTyped;

    // THIS MUST HAVE <string>
    event EventHandler<string> OnInsertRequested;
    event EventHandler<int> OnSelectionChangedRequested;

    // THIS MUST HAVE THE THIRD ARGUMENT
    void ReplaceWord(int backspaceCount, string malayalamWord, string trailingText = "");

    (double X, double Y) GetCaretPosition();
}