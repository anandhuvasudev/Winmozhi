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
    private IntPtr _mouseHookId = IntPtr.Zero;

    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private readonly NativeMethods.LowLevelMouseProc _mouseProc;

    private readonly StringBuilder _currentWord = new();
    private readonly Lock _wordLock = new();

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
        _proc = HookCallback;
        _mouseProc = MouseHookCallback; // Bind mouse hook delegate
    }

    public void StartHook()
    {
        if (_hookId != IntPtr.Zero) return;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule == null) return;

        // Start both Keyboard and Mouse hooks
        _hookId = NativeMethods.SetWindowsHookExW(NativeMethods.WH_KEYBOARD_LL, _proc, curModule.BaseAddress, 0);
        _mouseHookId = NativeMethods.SetWindowsHookExW(NativeMethods.WH_MOUSE_LL, _mouseProc, curModule.BaseAddress, 0);
    }

    public void StopHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        if (_mouseHookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
    }

    // ── Mouse Click Hook ──────────────────────────────────────────────────────────
    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // If the user clicks Left, Right, or Middle mouse button anywhere on screen
        if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_LBUTTONDOWN ||
                           wParam == (IntPtr)NativeMethods.WM_RBUTTONDOWN ||
                           wParam == (IntPtr)NativeMethods.WM_MBUTTONDOWN))
        {
            string? wordSnapshot = null;
            using (_wordLock.EnterScope())
            {
                if (_currentWord.Length > 0 || _isPopupVisible)
                {
                    _currentWord.Clear();
                    _isPopupVisible = false;
                    wordSnapshot = string.Empty;
                }
            }
            if (wordSnapshot is not null)
                OnWordTyped?.Invoke(this, wordSnapshot);
        }

        return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    // ── Keyboard Hook ─────────────────────────────────────────────────────────────
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_KEYDOWN)
        {
            var kbdStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            var key = kbdStruct.vkCode;

            if ((kbdStruct.flags & 0x10) != 0)
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

            if (_isPopupVisible)
            {
                switch (key)
                {
                    case 0x09: OnInsertRequested?.Invoke(this, ""); return (IntPtr)1;
                    case 0x20: OnInsertRequested?.Invoke(this, " "); return (IntPtr)1;
                    case 0x0D: OnInsertRequested?.Invoke(this, "\n"); return (IntPtr)1;
                    case 0x28: OnSelectionChangedRequested?.Invoke(this, 1); return (IntPtr)1;
                    case 0x26: OnSelectionChangedRequested?.Invoke(this, -1); return (IntPtr)1;
                }
            }

            string? wordSnapshot = null;
            using (_wordLock.EnterScope())
            {
                if (key is >= 0x41 and <= 0x5A)
                {
                    _currentWord.Append(char.ToLowerInvariant((char)key));
                    wordSnapshot = _currentWord.ToString();
                }
                else if (key == 0x08 && _currentWord.Length > 0)
                {
                    _currentWord.Length--;
                    wordSnapshot = _currentWord.ToString();
                }
                else if (key is 0x20 or 0x0D || key is >= 0xBA and <= 0xE2)
                {
                    _currentWord.Clear();
                    _isPopupVisible = false;
                    wordSnapshot = string.Empty;
                }
                // Cancel popup and reset buffer if they press ESC, Tab (when popup is hidden), 
                // Arrow keys, Alt, Ctrl, or Windows key.
                else if (key == 0x1B || key == 0x09 || key is >= 0x21 and <= 0x28 || key == 0x11 || key == 0x12 || key is 0x5B or 0x5C)
                {
                    if (_currentWord.Length > 0 || _isPopupVisible)
                    {
                        _currentWord.Clear();
                        _isPopupVisible = false;
                        wordSnapshot = string.Empty;
                    }
                }
            }

            if (wordSnapshot is not null)
                OnWordTyped?.Invoke(this, wordSnapshot);
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void ReplaceWord(int backspaceCount, string malayalamWord, string trailingText = "")
    {
        var inputs = new List<NativeMethods.INPUT>(backspaceCount * 2 + 50);
        for (int i = 0; i < backspaceCount; i++)
        {
            inputs.Add(CreateKeyInput(0x08, false));
            inputs.Add(CreateKeyInput(0x08, true));
        }

        for (int i = 0; i < 5; i++) inputs.Add(CreateKeyInput(0, false));

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

        NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<NativeMethods.INPUT>());

        using (_wordLock.EnterScope()) { _currentWord.Clear(); }
        _isPopupVisible = false;
        OnWordTyped?.Invoke(this, string.Empty);
    }

    public (double X, double Y) GetCaretPosition()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return (-1, -1);

        var threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        var guiInfo = new NativeMethods.GUITHREADINFO { cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>() };

        if (NativeMethods.GetGUIThreadInfo(threadId, ref guiInfo) && guiInfo.hwndCaret != IntPtr.Zero)
        {
            var point = new NativeMethods.InteropPoint { X = guiInfo.rcCaret.Left, Y = guiInfo.rcCaret.Bottom };
            NativeMethods.ClientToScreen(guiInfo.hwndCaret, ref point);
            return (point.X, point.Y);
        }
        return (-1, -1);
    }

    private static NativeMethods.INPUT CreateKeyInput(ushort vk, bool isKeyUp) =>
        new() { type = NativeMethods.INPUT_KEYBOARD, u = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wVk = vk, dwFlags = isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0 } } };

    private static NativeMethods.INPUT CreateUnicodeInput(char c, bool isKeyUp) =>
        new() { type = NativeMethods.INPUT_KEYBOARD, u = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wScan = c, dwFlags = NativeMethods.KEYEVENTF_UNICODE | (isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0) } } };
}