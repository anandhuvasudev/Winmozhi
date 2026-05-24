using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Winmozhi.Core.Interfaces;
using Winmozhi.UI.ViewModels;

namespace Winmozhi.UI.Views;

public sealed partial class PopupView : Window
{
    public PopupViewModel ViewModel { get; }
    private readonly IKeyboardHookService _hookService;
    private readonly IntPtr _hwnd;

    public PopupView(PopupViewModel viewModel, IKeyboardHookService hookService)
    {
        this.InitializeComponent();
        ViewModel = viewModel;
        _hookService = hookService;

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // 1. Remove Title Bar and Borders completely
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
        }

        // 2. Win32 NO_ACTIVATE (Do not steal focus)
        MakeWindowNoActivate(_hwnd);

        // 3. Set a small size for the popup
        AppWindow.Resize(new Windows.Graphics.SizeInt32(200, 250));

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsVisible))
        {
            if (ViewModel.IsVisible)
            {
                var (x, y) = _hookService.GetCaretPosition();

                // If Chrome/Edge hides the caret, use Mouse Position
                if (x == -1)
                {
                    GetCursorPos(out var mousePos);
                    x = mousePos.X;
                    y = mousePos.Y + 20;
                }

                // Move the window near the cursor
                AppWindow.Move(new Windows.Graphics.PointInt32((int)x + 10, (int)y + 10));

                // Show without taking focus
                AppWindow.Show(false);
            }
            else
            {
                // Hide when no suggestions
                AppWindow.Hide();
            }
        }
    }

    // --- WIN32 NATIVE METHODS ---
    const int GWL_EXSTYLE = -20;
    const uint WS_EX_NOACTIVATE = 0x08000000;
    const uint WS_EX_TOOLWINDOW = 0x00000080;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int GetWindowLongW(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int SetWindowLongW(IntPtr hWnd, int nIndex, int dwNewLong);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out InteropPoint lpPoint);

    public struct InteropPoint { public int X; public int Y; }

    private static void MakeWindowNoActivate(IntPtr hwnd)
    {
        int exStyle = GetWindowLongW(hwnd, GWL_EXSTYLE);
        SetWindowLongW(hwnd, GWL_EXSTYLE, (int)(exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW));
    }
}