using System;
using System.Collections.Generic;
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

    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_SHIFT = 0x10;
    private const int VK_M = 0x4D;

    private volatile bool _isPopupVisible;
    private bool _isTransliterationEnabled = true;

    public bool IsPopupVisible
    {
        get => _isPopupVisible;
        set => _isPopupVisible = value;
    }

    public bool IsTransliterationEnabled
    {
        get => _isTransliterationEnabled;
        set => _isTransliterationEnabled = value;
    }

    public event EventHandler<string>? OnWordTyped;
    public event EventHandler<string>? OnInsertRequested;
    public event EventHandler<int>? OnSelectionChangedRequested;
    public event EventHandler<bool>? OnStateChanged;

    public KeyboardHookService()
    {
        _proc = HookCallback;
        _mouseProc = MouseHookCallback;
    }

    public void StartHook()
    {
        if (_hookId != IntPtr.Zero) return;
        IntPtr moduleHandle = NativeMethods.GetModuleHandleW(IntPtr.Zero);
        _hookId = NativeMethods.SetWindowsHookExW(NativeMethods.WH_KEYBOARD_LL, _proc, moduleHandle, 0);
        _mouseHookId = NativeMethods.SetWindowsMouseHookExW(NativeMethods.WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
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

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (!IsTransliterationEnabled) return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);

        if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_LBUTTONDOWN || wParam == (IntPtr)NativeMethods.WM_RBUTTONDOWN || wParam == (IntPtr)NativeMethods.WM_MBUTTONDOWN))
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
            if (wordSnapshot is not null) OnWordTyped?.Invoke(this, wordSnapshot);
        }
        return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || wParam != (IntPtr)NativeMethods.WM_KEYDOWN)
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

        var kbdStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
        var key = kbdStruct.vkCode;

        bool isCtrlDown = (NativeMethods.GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
        bool isShiftDown = (NativeMethods.GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

        if (isCtrlDown && isShiftDown && key == VK_M)
        {
            IsTransliterationEnabled = !IsTransliterationEnabled;
            OnStateChanged?.Invoke(this, IsTransliterationEnabled);

            using (_wordLock.EnterScope()) { _currentWord.Clear(); }
            _isPopupVisible = false;
            OnWordTyped?.Invoke(this, string.Empty);
            return (IntPtr)1;
        }

        if (!IsTransliterationEnabled) return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

        if ((kbdStruct.flags & 0x10) != 0 || IsSystemShortcutActive())
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

        bool hasWord = false;
        using (_wordLock.EnterScope()) { hasWord = _currentWord.Length > 0; }

        if (hasWord || _isPopupVisible)
        {
            switch (key)
            {
                case 0x09:
                    using (_wordLock.EnterScope()) { _currentWord.Clear(); }
                    _isPopupVisible = false;
                    OnInsertRequested?.Invoke(this, string.Empty);
                    OnWordTyped?.Invoke(this, string.Empty);
                    return (IntPtr)1;
                case 0x20:
                    using (_wordLock.EnterScope()) { _currentWord.Clear(); }
                    _isPopupVisible = false;
                    OnInsertRequested?.Invoke(this, " ");
                    OnWordTyped?.Invoke(this, string.Empty);
                    return (IntPtr)1;
                case 0x0D:
                    if (_isPopupVisible)
                    {
                        using (_wordLock.EnterScope()) { _currentWord.Clear(); }
                        _isPopupVisible = false;
                        OnInsertRequested?.Invoke(this, "\n");
                        OnWordTyped?.Invoke(this, string.Empty);
                        return (IntPtr)1;
                    }
                    break;
                case 0x28:
                    if (_isPopupVisible) { OnSelectionChangedRequested?.Invoke(this, 1); return (IntPtr)1; }
                    break;
                case 0x26:
                    if (_isPopupVisible) { OnSelectionChangedRequested?.Invoke(this, -1); return (IntPtr)1; }
                    break;
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
            else if (key == 0x1B || key == 0x09 || key is >= 0x21 and <= 0x28)
            {
                if (_currentWord.Length > 0 || _isPopupVisible)
                {
                    _currentWord.Clear();
                    _isPopupVisible = false;
                    wordSnapshot = string.Empty;
                }
            }
        }

        if (wordSnapshot is not null) OnWordTyped?.Invoke(this, wordSnapshot);
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void ReplaceWord(int backspaceCount, string malayalamWord, string trailingText = "")
    {
        string fullText = malayalamWord + trailingText;
        var inputs = new List<NativeMethods.INPUT>(backspaceCount * 2 + (fullText.Length * 4) + 10);

        for (int i = 0; i < backspaceCount; i++)
        {
            inputs.Add(CreateVirtualKeyInput(0x08, false));
            inputs.Add(CreateVirtualKeyInput(0x08, true));
        }

        bool preferPaste = ShouldPreferPaste(fullText);
        if (preferPaste && TrySetClipboardUnicodeText(fullText))
        {
            inputs.Add(CreateVirtualKeyInput(VK_CONTROL, false));
            inputs.Add(CreateVirtualKeyInput(0x56, false));
            inputs.Add(CreateVirtualKeyInput(0x56, true));
            inputs.Add(CreateVirtualKeyInput(VK_CONTROL, true));
        }
        else
        {
            foreach (char c in fullText) AddCharInputs(inputs, c);
        }

        // Fixed Warning CA1806
        if (inputs.Count > 0) _ = NativeMethods.SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<NativeMethods.INPUT>());

        using (_wordLock.EnterScope()) { _currentWord.Clear(); }
        _isPopupVisible = false;
        OnWordTyped?.Invoke(this, string.Empty);
    }

    private static void AddCharInputs(List<NativeMethods.INPUT> inputs, char c)
    {
        if (c == '\n')
        {
            inputs.Add(CreateVirtualKeyInput(0x0D, false));
            inputs.Add(CreateVirtualKeyInput(0x0D, true));
            return;
        }
        inputs.Add(CreateUnicodeInput(c, false));
        inputs.Add(CreateUnicodeInput(c, true));
    }

    private static bool ShouldPreferPaste(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text) if (c >= '\u0D00' && c <= '\u0D7F') return false;
        return true;
    }

    private static bool TrySetClipboardUnicodeText(string text)
    {
        if (!NativeMethods.OpenClipboard(IntPtr.Zero)) return false;
        IntPtr hGlobal = IntPtr.Zero;
        try
        {
            if (!NativeMethods.EmptyClipboard()) return false;
            byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");
            hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            if (hGlobal == IntPtr.Zero) return false;

            IntPtr target = NativeMethods.GlobalLock(hGlobal);
            if (target == IntPtr.Zero) return false;
            try { Marshal.Copy(bytes, 0, target, bytes.Length); } finally { NativeMethods.GlobalUnlock(hGlobal); }

            IntPtr result = NativeMethods.SetClipboardData(NativeMethods.CF_UNICODETEXT, hGlobal);
            if (result == IntPtr.Zero) return false;
            hGlobal = IntPtr.Zero;
            return true;
        }
        finally
        {
            if (hGlobal != IntPtr.Zero) NativeMethods.GlobalFree(hGlobal);
            NativeMethods.CloseClipboard();
        }
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

    // Fixes CS0535
    public string GetForegroundProcessName()
    {
        try
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return string.Empty;

            _ = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return string.Empty;

            using var process = System.Diagnostics.Process.GetProcessById((int)processId);
            return process.ProcessName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsSystemShortcutActive() => IsKeyDown(VK_CONTROL) || IsKeyDown(VK_MENU) || IsKeyDown(VK_LWIN) || IsKeyDown(VK_RWIN);
    private static bool IsKeyDown(int vKey) => (NativeMethods.GetAsyncKeyState(vKey) & 0x8000) != 0;

    private static NativeMethods.INPUT CreateVirtualKeyInput(ushort vk, bool isKeyUp) =>
        new() { type = NativeMethods.INPUT_KEYBOARD, u = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0 } } };

    private static NativeMethods.INPUT CreateUnicodeInput(char c, bool isKeyUp) =>
        new() { type = NativeMethods.INPUT_KEYBOARD, u = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wScan = c, dwFlags = NativeMethods.KEYEVENTF_UNICODE | (isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0) } } };
}