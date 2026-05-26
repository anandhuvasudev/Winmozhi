using System;

namespace Winmozhi.Core.Interfaces;

public interface IKeyboardHookService
{
    void StartHook();
    void StopHook();

    // Toggles the transliteration interceptor without killing the hotkey listener
    bool IsTransliterationEnabled { get; set; }
    event EventHandler<bool> OnStateChanged;

    bool IsPopupVisible { get; set; }

    event EventHandler<string> OnWordTyped;
    event EventHandler<string> OnInsertRequested;
    event EventHandler<int> OnSelectionChangedRequested;

    void ReplaceWord(int backspaceCount, string malayalamWord, string trailingText = "");

    (double X, double Y) GetCaretPosition();

    // Process name getter for app-specific logic (like Photoshop detection)
    string GetForegroundProcessName();
}