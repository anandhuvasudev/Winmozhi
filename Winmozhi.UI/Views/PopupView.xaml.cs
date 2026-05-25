#pragma warning disable CA1822
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using Winmozhi.Core.Interfaces;
using Winmozhi.Core.Utilities;
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

        CurrentManglishText.DataContext = viewModel;
        SuggestionsListView.DataContext = viewModel;

        CurrentManglishText.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(PopupViewModel.CurrentManglish)), Mode = BindingMode.OneWay });
        SuggestionsListView.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath(nameof(PopupViewModel.Suggestions)), Mode = BindingMode.OneWay });
        SuggestionsListView.SetBinding(Selector.SelectedIndexProperty, new Binding { Path = new PropertyPath(nameof(PopupViewModel.SelectedIndex)), Mode = BindingMode.TwoWay });
        SuggestionsListView.SetBinding(Selector.SelectedItemProperty, new Binding { Path = new PropertyPath(nameof(PopupViewModel.Suggestions)), Mode = BindingMode.OneWay });

        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
        }

        MakeWindowTruePopup(_hwnd);

        int cornerPreference = DWMWCP_ROUND;
        DwmSetWindowAttribute(_hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, 4);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(180, 260));

        ApplyStoredStyles();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        LocalPreferences.PreferencesChanged += () =>
        {
            this.DispatcherQueue.TryEnqueue(() => ApplyStoredStyles());
        };
    }

    private void ApplyStoredStyles()
    {
        try
        {
            var grid = this.Content as Grid;
            if (grid?.Children.Count > 0 && grid.Children[0] is Border border)
            {
                double opacity = Math.Clamp(LocalPreferences.PopupOpacity, 0.1, 1.0);
                byte alpha = (byte)(opacity * 255);

                if (TryParseColor(LocalPreferences.PopupBackgroundColor, out var bgColor))
                    border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, bgColor.R, bgColor.G, bgColor.B));
                else
                    border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 26, 26, 26));

                border.Opacity = 1.0;

                if (TryParseColor(LocalPreferences.PopupTextColor, out var textColor))
                    CurrentManglishText.Foreground = new SolidColorBrush(textColor);
                else
                    CurrentManglishText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));

                double fontSize = LocalPreferences.PopupFontSize;
                if (fontSize >= 10 && fontSize <= 32) CurrentManglishText.FontSize = fontSize - 4;
            }
        }
        catch { }
    }

    private void SuggestionsListView_ContainerContentChanging(ListViewBase _, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer.ContentTemplateRoot is TextBlock textBlock)
        {
            if (TryParseColor(LocalPreferences.PopupTextColor, out var color))
                textBlock.Foreground = new SolidColorBrush(color);
            textBlock.FontSize = LocalPreferences.PopupFontSize;
        }
    }

    private static bool TryParseColor(string hexColor, out Windows.UI.Color color)
    {
        color = Windows.UI.Color.FromArgb(255, 255, 255, 255);
        try
        {
            if (string.IsNullOrWhiteSpace(hexColor)) return false;
            hexColor = hexColor.TrimStart('#');
            if (hexColor.Length != 6 && hexColor.Length != 8) return false;

            uint hex = uint.Parse(hexColor, System.Globalization.NumberStyles.HexNumber);

            if (hexColor.Length == 6)
                color = Windows.UI.Color.FromArgb(255, (byte)((hex >> 16) & 0xFF), (byte)((hex >> 8) & 0xFF), (byte)(hex & 0xFF));
            else
                color = Windows.UI.Color.FromArgb((byte)((hex >> 24) & 0xFF), (byte)((hex >> 16) & 0xFF), (byte)((hex >> 8) & 0xFF), (byte)(hex & 0xFF));

            return true;
        }
        catch { return false; }
    }

    private void ViewModel_PropertyChanged(object? _, System.ComponentModel.PropertyChangedEventArgs e)
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
                    y = mousePos.Y;
                }

                int screenH = GetSystemMetrics(1);
                int screenW = GetSystemMetrics(0);

                int popupWidth = 180;
                int popupHeight = 260;

                int finalY = (int)y + 30;
                int finalX = (int)x;

                if (finalY + popupHeight > screenH) finalY = (int)y - popupHeight - 10;
                if (finalX + popupWidth > screenW) finalX = screenW - popupWidth - 10;
                if (finalY < 0) finalY = 10;
                if (finalX < 0) finalX = 10;

                SetWindowPos(_hwnd, HWND_TOPMOST, finalX, finalY, popupWidth, popupHeight, SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else
            {
                // Fix: Move off-screen instead of AppWindow.Hide() to prevent compositor sleep bug
                SetWindowPos(_hwnd, IntPtr.Zero, -10000, -10000, 0, 0, SWP_NOACTIVATE | SWP_NOSIZE | SWP_NOZORDER);
            }
        }
    }

    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;
    const uint WS_POPUP = 0x80000000;
    const uint WS_EX_NOACTIVATE = 0x08000000;
    const uint WS_EX_TOOLWINDOW = 0x00000080;
    static readonly IntPtr HWND_TOPMOST = new(-1);
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOZORDER = 0x0004;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint SWP_SHOWWINDOW = 0x0040; // Crucial to force UI wake!

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int GetWindowLongW(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int SetWindowLongW(IntPtr hWnd, int nIndex, int dwNewLong);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out InteropPoint lpPoint);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int nIndex);

    public struct InteropPoint { public int X; public int Y; }

    private static void MakeWindowTruePopup(IntPtr hwnd)
    {
        SetWindowLongW(hwnd, GWL_STYLE, unchecked((int)WS_POPUP));
        int exStyle = GetWindowLongW(hwnd, GWL_EXSTYLE);
        SetWindowLongW(hwnd, GWL_EXSTYLE, exStyle | unchecked((int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW)));
    }
}