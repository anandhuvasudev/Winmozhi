namespace Winmozhi.Core.Interfaces;

public interface IKeyboardHookService
{
    void StartHook();
    void StopHook();

    // UI sets this to true when suggestions are visible so Tab is intercepted
    bool IsPopupVisible { get; set; }

    event EventHandler<string> OnWordTyped; // Fires when the Manglish buffer changes
    event EventHandler OnInsertRequested;   // Fires when Tab is pressed

    // Methods for text injection
    void ReplaceWord(int backspaceCount, string malayalamWord);
    (double X, double Y) GetCaretPosition();
}