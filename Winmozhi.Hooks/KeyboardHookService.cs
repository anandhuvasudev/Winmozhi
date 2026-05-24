using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Winmozhi.Core.Interfaces;
using Winmozhi.Hooks.Native;

namespace Winmozhi.Hooks;

public class KeyboardHookService : IKeyboardHookService
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;

    // ── Word Buffer ──────────────────────────────────────────────────────────────
    // _currentWord is written by the hook thread (HookCallback) and cleared by the
    // UI thread (ReplaceWord via TryEnqueue). _wordLock guards both accesses.
    private readonly StringBuilder _currentWord = new();
    private readonly Lock _wordLock = new();

    // ── IsPopupVisible ────────────────────────────────────────────────────────────
    // BUG FIX: This field MUST be volatile.
    //
    // The hook callback runs on a dedicated background thread (the hook thread).
    // IsPopupVisible is written by the UI thread (inside TryEnqueue callbacks).
    // Without volatile, the CPU may cache the value per-thread, so the hook thread
    // could read a stale `false` even after the UI thread has set it to `true`.
    // That makes Tab/Enter fall through as if the popup wasn't showing.
    private volatile bool _isPopupVisible;

    public bool IsPopupVisible
    {
        get => _isPopupVisible;
        set => _isPopupVisible = value;
    }

    public event EventHandler<string>? OnWordTyped;
    public event EventHandler<string>? OnInsertRequested;
    public event EventHandler<int>? OnSelectionChangedRequested;

    public KeyboardHookService()
    {
        // Keep a strong reference to the delegate; GC must never collect it while
        // the hook is installed or Windows will call a dangling function pointer.
        _proc = HookCallback;
    }

    // ── Hook Lifecycle ────────────────────────────────────────────────────────────

    public void StartHook()
    {
        if (_hookId != IntPtr.Zero) return;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule == null) return;
        _hookId = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_KEYBOARD_LL, _proc, curModule.BaseAddress, 0);
    }

    public void StopHook()
    {
        if (_hookId == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    // ── Hook Callback (runs on dedicated hook thread) ─────────────────────────────

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_KEYDOWN)
        {
            var kbdStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            var key = kbdStruct.vkCode;

            // Skip injected keys (LLKHF_INJECTED = 0x10). SendInput sets this flag
            // automatically, preventing an infinite loop when ReplaceWord fires.
            if ((kbdStruct.flags & 0x10) != 0)
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

            // ── Popup action keys ─────────────────────────────────────────────────
            // Reading _isPopupVisible here is safe without a lock because the field
            // is volatile — the CPU always fetches the latest value from main memory.
            if (_isPopupVisible)
            {
                switch (key)
                {
                    case 0x09: // Tab   — accept top suggestion (no trailing character)
                        OnInsertRequested?.Invoke(this, "");
                        return (IntPtr)1;  // Consume key; don't forward to the app

                    case 0x20: // Space — accept + insert space
                        OnInsertRequested?.Invoke(this, " ");
                        return (IntPtr)1;

                    case 0x0D: // Enter — accept + insert newline
                        OnInsertRequested?.Invoke(this, "\n");
                        return (IntPtr)1;

                    case 0x28: // Down Arrow — next suggestion
                        OnSelectionChangedRequested?.Invoke(this, 1);
                        return (IntPtr)1;

                    case 0x26: // Up Arrow — previous suggestion
                        OnSelectionChangedRequested?.Invoke(this, -1);
                        return (IntPtr)1;
                }
            }

            // ── Word buffer management ────────────────────────────────────────────
            string? wordSnapshot = null;

            using (_wordLock.EnterScope())
            {
                if (key is >= 0x41 and <= 0x5A)  // A–Z (virtual key codes are uppercase)
                {
                    _currentWord.Append(char.ToLowerInvariant((char)key));
                    wordSnapshot = _currentWord.ToString();
                }
                else if (key == 0x08 && _currentWord.Length > 0)  // Backspace
                {
                    _currentWord.Length--;
                    wordSnapshot = _currentWord.ToString();
                }
                else if (key is 0x20 or 0x0D || key is >= 0xBA and <= 0xE2)
                {
                    // Space, Enter, or punctuation/symbol: end of word, dismiss popup
                    _currentWord.Clear();
                    _isPopupVisible = false;
                    wordSnapshot = string.Empty;
                }
                // All other keys (Ctrl, Alt, Tab when popup hidden, etc.) are ignored.
                // Crucially, Tab (0x09) is NOT cleared here — it is only handled above
                // when the popup is visible.
            }

            if (wordSnapshot is not null)
                OnWordTyped?.Invoke(this, wordSnapshot);
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    // ── Text Replacement (called from UI thread via TryEnqueue) ───────────────────

    // ── Text Replacement (called from UI thread via TryEnqueue) ───────────────────

    public void ReplaceWord(int backspaceCount, string malayalamWord, string trailingText = "")
    {
        var inputs = new List<NativeMethods.INPUT>();

        // STEP 1: Hold down the SHIFT key
        inputs.Add(CreateKeyInput(0x10, false)); // VK_SHIFT = 0x10

        // STEP 2: Send 'Left Arrow' rapidly to highlight the entire Manglish word
        for (int i = 0; i < backspaceCount; i++)
        {
            inputs.Add(CreateKeyInput(0x25, false)); // VK_LEFT = 0x25 (Key Down)
            inputs.Add(CreateKeyInput(0x25, true));  // VK_LEFT (Key Up)
        }

        // STEP 3: Release the SHIFT key (Word is now fully highlighted)
        inputs.Add(CreateKeyInput(0x10, true)); // VK_SHIFT Up

        // STEP 4: Inject the Malayalam Unicode text (Instantly overwrites highlighted text)
        string fullText = malayalamWord + trailingText;
        foreach (char c in fullText)
        {
            if (c == '\n')
            {
                inputs.Add(CreateKeyInput(0x0D, false));
                inputs.Add(CreateKeyInput(0x0D, true));
            }
            else
            {
                inputs.Add(CreateUnicodeInput(c, false));
                inputs.Add(CreateUnicodeInput(c, true));
            }
        }

        // Send all commands to Windows simultaneously
        NativeMethods.SendInput(
            (uint)inputs.Count,
            inputs.ToArray(),
            Marshal.SizeOf<NativeMethods.INPUT>());

        // Step 5: Reset hook state cleanly
        using (_wordLock.EnterScope())
        {
            _currentWord.Clear();
        }
        _isPopupVisible = false;
        OnWordTyped?.Invoke(this, string.Empty);
    }

    // ── Caret Position ────────────────────────────────────────────────────────────

    public (double X, double Y) GetCaretPosition()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return (-1, -1);

        var threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        var guiInfo = new NativeMethods.GUITHREADINFO
        {
            cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>()
        };

        if (NativeMethods.GetGUIThreadInfo(threadId, ref guiInfo)
            && guiInfo.hwndCaret != IntPtr.Zero)
        {
            var point = new NativeMethods.InteropPoint
            {
                X = guiInfo.rcCaret.Left,
                Y = guiInfo.rcCaret.Bottom
            };
            NativeMethods.ClientToScreen(guiInfo.hwndCaret, ref point);
            return (point.X, point.Y);
        }

        return (-1, -1);
    }

    // ── SendInput Helpers ─────────────────────────────────────────────────────────

    private static NativeMethods.INPUT CreateKeyInput(ushort vk, bool isKeyUp) =>
        new()
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

    private static NativeMethods.INPUT CreateUnicodeInput(char c, bool isKeyUp) =>
        new()
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = NativeMethods.KEYEVENTF_UNICODE
                             | (isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0),
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
}