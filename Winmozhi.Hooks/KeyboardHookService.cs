using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Winmozhi.Core.Interfaces;
using Winmozhi.Hooks.Native;

namespace Winmozhi.Hooks;

public partial class KeyboardHookService : IKeyboardHookService
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
    private const int VK_CAPITAL = 0x14;
    private const int VK_M = 0x4D;

    private volatile bool _isPopupVisible;
    private bool _isTransliterationEnabled = true;

    [LibraryImport("user32.dll")]
    private static partial short GetKeyState(int keyCode);

    public bool IsPopupVisible { get => _isPopupVisible; set => _isPopupVisible = value; }
    public bool IsTransliterationEnabled { get => _isTransliterationEnabled; set => _isTransliterationEnabled = value; }

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
        if (_hookId != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_hookId); _hookId = IntPtr.Zero; }
        if (_mouseHookId != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_mouseHookId); _mouseHookId = IntPtr.Zero; }
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
        bool isAltDown = (NativeMethods.GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
        bool isWinDown = (NativeMethods.GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 || (NativeMethods.GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;

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
        if ((kbdStruct.flags & 0x10) != 0) return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

        bool isModifierKey = key is VK_SHIFT or VK_CONTROL or VK_MENU or VK_LWIN or VK_RWIN or >= 0xA0 and <= 0xA5 or VK_CAPITAL;

        if (isCtrlDown || isAltDown || isWinDown)
        {
            if (!isModifierKey)
            {
                bool shouldNotify = false;
                using (_wordLock.EnterScope())
                {
                    if (_currentWord.Length > 0 || _isPopupVisible)
                    {
                        _currentWord.Clear();
                        _isPopupVisible = false;
                        shouldNotify = true;
                    }
                }
                if (shouldNotify) OnWordTyped?.Invoke(this, string.Empty);
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        bool hasWord = false;
        using (_wordLock.EnterScope()) { hasWord = _currentWord.Length > 0; }

        if (hasWord || _isPopupVisible)
        {
            switch (key)
            {
                case 0x09:
                case 0x20:
                    using (_wordLock.EnterScope()) { _currentWord.Clear(); }
                    _isPopupVisible = false;
                    // Fired synchronously to guarantee chronological order!
                    OnInsertRequested?.Invoke(this, key == 0x20 ? " " : string.Empty);
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
                case 0x1B:
                    if (_isPopupVisible)
                    {
                        using (_wordLock.EnterScope()) { _currentWord.Clear(); }
                        _isPopupVisible = false;
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
                bool isCapsOn = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
                bool useUpper = isShiftDown ^ isCapsOn;
                _currentWord.Append(useUpper ? (char)key : char.ToLowerInvariant((char)key));
                wordSnapshot = _currentWord.ToString();
            }
            else if (key == 0x08)
            {
                if (_currentWord.Length > 0)
                {
                    _currentWord.Length--;
                    wordSnapshot = _currentWord.ToString();
                    if (_currentWord.Length == 0) _isPopupVisible = false;
                }
                else if (_isPopupVisible)
                {
                    _isPopupVisible = false;
                    wordSnapshot = string.Empty;
                }
            }
            else if (!isModifierKey)
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
        {
            OnWordTyped?.Invoke(this, wordSnapshot);
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void ReplaceWord(int backspaceCount, string malayalamWord, string trailingText = "")
    {
        string fullText = malayalamWord + trailingText;
        var inputs = new List<NativeMethods.INPUT>(backspaceCount * 2 + (fullText.Length * 2));

        for (int i = 0; i < backspaceCount; i++)
        {
            inputs.Add(CreateVirtualKeyInput(0x08, false));
            inputs.Add(CreateVirtualKeyInput(0x08, true));
        }

        foreach (char c in fullText)
        {
            if (c == '\n')
            {
                inputs.Add(CreateVirtualKeyInput(0x0D, false));
                inputs.Add(CreateVirtualKeyInput(0x0D, true));
            }
            else
            {
                inputs.Add(CreateUnicodeInput(c, false));
                inputs.Add(CreateUnicodeInput(c, true));
            }
        }

        if (inputs.Count > 0)
        {
            _ = NativeMethods.SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<NativeMethods.INPUT>());
        }

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
        catch { return string.Empty; }
    }

    private static NativeMethods.INPUT CreateVirtualKeyInput(ushort vk, bool isKeyUp) =>
        new() { type = NativeMethods.INPUT_KEYBOARD, u = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0 } } };

    private static NativeMethods.INPUT CreateUnicodeInput(char c, bool isKeyUp) =>
        new() { type = NativeMethods.INPUT_KEYBOARD, u = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wScan = c, dwFlags = NativeMethods.KEYEVENTF_UNICODE | (isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0) } } };
}