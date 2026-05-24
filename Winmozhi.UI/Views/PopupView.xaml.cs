using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
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

        // Bind UI elements to ViewModel
        CurrentManglishText.DataContext = viewModel;
        SuggestionsListView.DataContext = viewModel;

        // Set bindings
        CurrentManglishText.SetBinding(TextBlock.TextProperty, 
            new Binding { Path = new PropertyPath(nameof(PopupViewModel.CurrentManglish)), Mode = BindingMode.OneWay });
        SuggestionsListView.SetBinding(ListView.ItemsSourceProperty,
            new Binding { Path = new PropertyPath(nameof(PopupViewModel.Suggestions)), Mode = BindingMode.OneWay });

        // Use TwoWay binding for SelectedIndex to ensure arrow key changes update the UI
        SuggestionsListView.SetBinding(Selector.SelectedIndexProperty,
            new Binding { Path = new PropertyPath(nameof(PopupViewModel.SelectedIndex)), Mode = BindingMode.TwoWay });

        // Also add SelectedItem binding for robust selection tracking
        SuggestionsListView.SetBinding(Selector.SelectedItemProperty,
            new Binding { Path = new PropertyPath(nameof(PopupViewModel.Suggestions)), Mode = BindingMode.OneWay });

        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
        }

        MakeWindowTruePopup(_hwnd);
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

                // Show the window, and forcefully push it to the top WITHOUT activating it
                ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
                SetWindowPos(_hwnd, HWND_TOPMOST, (int)x + 10, (int)y + 10, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
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
    const int SW_SHOWNOACTIVATE = 4;

    // Windows Positioning Flags
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOACTIVATE = 0x0010;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int GetWindowLongW(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int SetWindowLongW(IntPtr hWnd, int nIndex, int dwNewLong);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out InteropPoint lpPoint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public struct InteropPoint { public int X; public int Y; }

    private static void MakeWindowTruePopup(IntPtr hwnd)
    {
        SetWindowLongW(hwnd, GWL_STYLE, unchecked((int)WS_POPUP));
        int exStyle = GetWindowLongW(hwnd, GWL_EXSTYLE);
        SetWindowLongW(hwnd, GWL_EXSTYLE, exStyle | unchecked((int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW)));
    }
}