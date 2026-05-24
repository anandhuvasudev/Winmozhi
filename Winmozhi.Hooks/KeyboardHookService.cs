using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Winmozhi.Core.Interfaces;
using Winmozhi.Hooks.Native;

namespace Winmozhi.Hooks;

public class KeyboardHookService : IKeyboardHookService
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private readonly StringBuilder _currentWord = new();

    public bool IsPopupVisible { get; set; }

    public event EventHandler<string>? OnWordTyped;
    public event EventHandler<string>? OnInsertRequested; // Updated to pass trailing text
    public event EventHandler<int>? OnSelectionChangedRequested;

    public KeyboardHookService()
    {
        _proc = HookCallback;
    }

    public void StartHook()
    {
        if (_hookId != IntPtr.Zero) return;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule == null) return;
        _hookId = NativeMethods.SetWindowsHookExW(NativeMethods.WH_KEYBOARD_LL, _proc, curModule.BaseAddress, 0);
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

            // IGNORE INJECTED KEYS (Prevents infinite loops)
            if ((kbdStruct.flags & 0x10) != 0)
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

            // 1. If Popup is visible, intercept action keys!
            if (IsPopupVisible)
            {
                if (key == 0x09) { OnInsertRequested?.Invoke(this, ""); return (IntPtr)1; }   // Tab (Just inject word)
                if (key == 0x20) { OnInsertRequested?.Invoke(this, " "); return (IntPtr)1; }  // Space (Inject word + Space)
                if (key == 0x0D) { OnInsertRequested?.Invoke(this, "\n"); return (IntPtr)1; } // Enter (Inject word + Enter)
                if (key == 0x28) { OnSelectionChangedRequested?.Invoke(this, 1); return (IntPtr)1; }  // Down Arrow
                if (key == 0x26) { OnSelectionChangedRequested?.Invoke(this, -1); return (IntPtr)1; } // Up Arrow
            }

            // 2. Build the word
            if (key is >= 0x41 and <= 0x5A)
            {
                _currentWord.Append(char.ToLowerInvariant((char)key));
                OnWordTyped?.Invoke(this, _currentWord.ToString());
            }
            else if (key == 0x08 && _currentWord.Length > 0)
            {
                _currentWord.Length--;
                OnWordTyped?.Invoke(this, _currentWord.ToString());
            }
            else if (key is 0x20 or 0x0D || (key is >= 0xBA and <= 0xE2))
            {
                _currentWord.Clear();
                OnWordTyped?.Invoke(this, string.Empty);
                IsPopupVisible = false;
            }
        }
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void ReplaceWord(int backspaceCount, string malayalamWord, string trailingText = "")
    {
        var inputs = new List<NativeMethods.INPUT>();

        // 1. Delete the Manglish text
        for (int i = 0; i < backspaceCount; i++)
        {
            inputs.Add(CreateKeyInput(0x08, false));
            inputs.Add(CreateKeyInput(0x08, true));
        }

        // 2. Inject Malayalam + Space/Enter
        string fullText = malayalamWord + trailingText;
        foreach (var c in fullText)
        {
            if (c == '\n')
            {
                inputs.Add(CreateKeyInput(0x0D, false)); // Real Enter Key
                inputs.Add(CreateKeyInput(0x0D, true));
            }
            else
            {
                inputs.Add(CreateUnicodeInput(c, false));
                inputs.Add(CreateUnicodeInput(c, true));
            }
        }

        NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<NativeMethods.INPUT>());

        _currentWord.Clear();
        IsPopupVisible = false;
        OnWordTyped?.Invoke(this, string.Empty);
    }

    private static NativeMethods.INPUT CreateKeyInput(ushort vk, bool isKeyUp) => new() { type = NativeMethods.INPUT_KEYBOARD, u = new() { ki = new() { wVk = vk, wScan = 0, dwFlags = isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0, dwExtraInfo = IntPtr.Zero } } };
    private static NativeMethods.INPUT CreateUnicodeInput(char c, bool isKeyUp) => new() { type = NativeMethods.INPUT_KEYBOARD, u = new() { ki = new() { wVk = 0, wScan = c, dwFlags = NativeMethods.KEYEVENTF_UNICODE | (isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0), dwExtraInfo = IntPtr.Zero } } };

    public (double X, double Y) GetCaretPosition()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
            var threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
            var guiInfo = new NativeMethods.GUITHREADINFO { cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>() };
            if (NativeMethods.GetGUIThreadInfo(threadId, ref guiInfo) && guiInfo.hwndCaret != IntPtr.Zero)
            {
                var point = new NativeMethods.InteropPoint { X = guiInfo.rcCaret.Left, Y = guiInfo.rcCaret.Bottom };
                NativeMethods.ClientToScreen(guiInfo.hwndCaret, ref point);
                return (point.X, point.Y);
            }
        }
        return (-1, -1);
    }
}