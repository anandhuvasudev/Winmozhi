using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Winmozhi.Core.Interfaces;
using Winmozhi.Hooks.Native;

namespace Winmozhi.Hooks;

public class KeyboardHookService : IKeyboardHookService
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelKeyboardProc _proc; // Kept as class member to prevent GC
    private readonly StringBuilder _currentWord = new();

    public bool IsPopupVisible { get; set; }

    public event EventHandler<string>? OnWordTyped;
    public event EventHandler? OnInsertRequested;

    public KeyboardHookService()
    {
        _proc = HookCallback;
    }

    public void StartHook()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        // NativeLibrary.GetMainProgramHandle() is the .NET 10 preferred way to get the module handle
        _hookId = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            NativeLibrary.GetMainProgramHandle(),
            0);
    }

    public void StopHook()
    {
        if (_hookId == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_KEYDOWN)
        {
            var kbdStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            var key = kbdStruct.vkCode;

            // 1. Intercept TAB if popup is visible
            if (key == 0x09) // Virtual-Key Code for Tab
            {
                if (IsPopupVisible)
                {
                    OnInsertRequested?.Invoke(this, EventArgs.Empty);
                    return (IntPtr)1; // Swallow the Tab key (prevents jumping to next UI element)
                }
            }

            // 2. Handle Letters (A-Z)
            if (key is >= 0x41 and <= 0x5A)
            {
                var ch = (char)key;
                _currentWord.Append(char.ToLowerInvariant(ch));
                OnWordTyped?.Invoke(this, _currentWord.ToString());
            }
            // 3. Handle Backspace
            else if (key == 0x08 && _currentWord.Length > 0)
            {
                _currentWord.Length--;
                OnWordTyped?.Invoke(this, _currentWord.ToString());
            }
            // 4. Handle Space, Enter, Punctuation (End of word)
            else if (key is 0x20 or 0x0D || (key is >= 0xBA and <= 0xE2))
            {
                _currentWord.Clear();
                OnWordTyped?.Invoke(this, string.Empty);
                IsPopupVisible = false;
            }
        }

        // Pass along to next app
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    // --- PHASE 3 TEXT INJECTION ---
    public void ReplaceWord(int backspaceCount, string malayalamWord)
    {
        var inputs = new List<NativeMethods.INPUT>();

        // 1. Send Backspaces to clear "Manglish"
        for (int i = 0; i < backspaceCount; i++)
        {
            inputs.Add(CreateKeyInput(0x08, false)); // Backspace Down
            inputs.Add(CreateKeyInput(0x08, true));  // Backspace Up
        }

        // 2. Send Unicode Malayalam Characters
        foreach (var c in malayalamWord)
        {
            inputs.Add(CreateUnicodeInput(c, false));
            inputs.Add(CreateUnicodeInput(c, true));
        }

        NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<NativeMethods.INPUT>());
        // Reset state
        _currentWord.Clear();
        IsPopupVisible = false;
        OnWordTyped?.Invoke(this, string.Empty);
    }

    private static NativeMethods.INPUT CreateKeyInput(ushort vk, bool isKeyUp)
    {
        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private static NativeMethods.INPUT CreateUnicodeInput(char c, bool isKeyUp)
    {
        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = NativeMethods.KEYEVENTF_UNICODE | (isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0),
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    // --- PHASE 3 CARET TRACKING ---
    public (double X, double Y) GetCaretPosition()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
            var threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
            var guiInfo = new NativeMethods.GUITHREADINFO { cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>() };

            if (NativeMethods.GetGUIThreadInfo(threadId, ref guiInfo) && guiInfo.hwndCaret != IntPtr.Zero)
            {
                // Convert relative Caret position to Absolute Screen Coordinates
                var point = new NativeMethods.InteropPoint
                {
                    X = guiInfo.rcCaret.Left,
                    Y = guiInfo.rcCaret.Bottom // Bottom so popup appears BELOW the text
                };

                NativeMethods.ClientToScreen(guiInfo.hwndCaret, ref point);
                return (point.X, point.Y);
            }
        }

        // Fallback: If we can't find the caret (e.g., Chrome/Edge render their own text), 
        // return (-1, -1). In Phase 4, UI will handle this by showing popup near mouse cursor.
        return (-1, -1);
    }
}