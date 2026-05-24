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

        // 1. Tell WinUI 3 to hide its default title bar
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
        }

        // 2. Win32 Magic: Make it a True Popup & Don't steal focus
        MakeWindowTruePopup(_hwnd);

        // 3. Set exact size so there is no empty space
        AppWindow.Resize(new Windows.Graphics.SizeInt32(180, 260));

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsVisible))
        {
            if (ViewModel.IsVisible)
            {
                var (x, y) = _hookService.GetCaretPosition();

                if (x == -1)
                {
                    GetCursorPos(out var mousePos);
                    x = mousePos.X;
                    y = mousePos.Y + 20;
                }

                AppWindow.Move(new Windows.Graphics.PointInt32((int)x + 10, (int)y + 10));
                AppWindow.Show(false);
                AppWindow.MoveInZOrderAtTop();
            }
            else
            {
                AppWindow.Hide();
            }
        }
    }

    // --- WIN32 NATIVE METHODS ---
    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;
    const uint WS_POPUP = 0x80000000;
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

    private static void MakeWindowTruePopup(IntPtr hwnd)
    {
        // Use 'unchecked' to safely cast the massive uint down to an int
        SetWindowLongW(hwnd, GWL_STYLE, unchecked((int)WS_POPUP));

        // Add EX Styles (Don't steal focus, hide from Alt+Tab)
        int exStyle = GetWindowLongW(hwnd, GWL_EXSTYLE);
        SetWindowLongW(hwnd, GWL_EXSTYLE, exStyle | unchecked((int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW)));
    }
}