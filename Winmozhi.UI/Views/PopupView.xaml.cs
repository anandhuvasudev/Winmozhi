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

    // Restored to original size
    private const int PopupWidth = 180;
    private const int PopupHeight = 260;

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

        AppWindow.Resize(new Windows.Graphics.SizeInt32(PopupWidth, PopupHeight));

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
            double opacity = Math.Clamp(LocalPreferences.PopupOpacity, 0.1, 1.0);
            byte alpha = (byte)(opacity * 255);

            if (TryParseColor(LocalPreferences.PopupBackgroundColor, out var bgColor))
                RootBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, bgColor.R, bgColor.G, bgColor.B));
            else
                RootBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 26, 26, 26));

            if (TryParseColor(LocalPreferences.PopupTextColor, out var textColor))
                CurrentManglishText.Foreground = new SolidColorBrush(textColor);
            else
                CurrentManglishText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));

            double fontSize = LocalPreferences.PopupFontSize;
            if (fontSize >= 10 && fontSize <= 32) CurrentManglishText.FontSize = fontSize - 4;
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

    private void SuggestionsListView_ItemClick(object _, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string selectedWord)
        {
            int index = ViewModel.Suggestions.IndexOf(selectedWord);
            if (index >= 0)
            {
                // Update the selection and force the ViewModel to insert it
                ViewModel.SelectedIndex = index;

                // We pass a space " " so that typing continues naturally after insertion
                ViewModel.InsertCurrentSelection(" ");
            }
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
                bool isMouseFallback = false;

                if (x == -1)
                {
                    GetCursorPos(out var mousePos);
                    x = mousePos.X;
                    y = mousePos.Y;
                    isMouseFallback = true;
                }

                // Multi-Monitor Intelligence
                var point = new InteropPoint { X = (int)x, Y = (int)y };
                IntPtr hMonitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);

                var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
                GetMonitorInfoW(hMonitor, ref monitorInfo);

                int finalX = (int)x;
                int finalY = (int)y;

                if (isMouseFallback)
                {
                    finalX += 15;
                    finalY += 20;
                }
                else
                {
                    finalY += 25;
                }

                // Smart Bounds Checking
                if (finalX + PopupWidth > monitorInfo.rcWork.Right)
                    finalX = monitorInfo.rcWork.Right - PopupWidth - 5;

                if (finalX < monitorInfo.rcWork.Left)
                    finalX = monitorInfo.rcWork.Left + 5;

                // Auto-Flipping if hitting the bottom
                if (finalY + PopupHeight > monitorInfo.rcWork.Bottom)
                {
                    if (isMouseFallback) finalY = (int)y - PopupHeight - 10;
                    else finalY = (int)y - PopupHeight - 5;
                }

                if (finalY < monitorInfo.rcWork.Top)
                    finalY = monitorInfo.rcWork.Top + 5;

                SetWindowPos(_hwnd, HWND_TOPMOST, finalX, finalY, PopupWidth, PopupHeight, SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else
            {
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
    const uint SWP_SHOWWINDOW = 0x0040;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

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

    // Modern Multi-Monitor APIs
    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromPoint(InteropPoint pt, uint dwFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFO lpmi);

    public struct InteropPoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public InteropRect rcMonitor;
        public InteropRect rcWork;
        public uint dwFlags;
    }

    private static void MakeWindowTruePopup(IntPtr hwnd)
    {
        SetWindowLongW(hwnd, GWL_STYLE, unchecked((int)WS_POPUP));
        int exStyle = GetWindowLongW(hwnd, GWL_EXSTYLE);
        SetWindowLongW(hwnd, GWL_EXSTYLE, exStyle | unchecked((int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW)));
    }
}