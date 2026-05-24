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
        AppWindow.Resize(new Windows.Graphics.SizeInt32(180, 260));

        // Apply stored styling preferences
        ApplyStoredStyles();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Subscribe to preference changes so the popup updates immediately when settings change
        Winmozhi.Core.Utilities.LocalPreferences.PreferencesChanged += () =>
        {
            // Re-apply stored styles on UI thread
            this.DispatcherQueue.TryEnqueue(() => ApplyStoredStyles());
        };
    }

    private void ApplyStoredStyles()
    {
        try
        {
            // Get the border element (main background)
            var grid = this.Content as Grid;
            if (grid?.Children.Count > 0 && grid.Children[0] is Border border)
            {
                // Apply background color
                string bgColorHex = LocalPreferences.PopupBackgroundColor;
                if (TryParseColor(bgColorHex, out var bgColor))
                {
                    border.Background = new SolidColorBrush(bgColor);
                }
                else
                {
                    border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 26)); // Dark default
                }

                // Apply text color and font size
                string textColorHex = LocalPreferences.PopupTextColor;
                if (TryParseColor(textColorHex, out var textColor))
                {
                    CurrentManglishText.Foreground = new SolidColorBrush(textColor);

                    // Also apply to list items
                    ApplyTextColorToListItems(textColor);
                }
                else
                {
                    CurrentManglishText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)); // White default
                }

                // Apply font size
                double fontSize = LocalPreferences.PopupFontSize;
                if (fontSize >= 10 && fontSize <= 32)
                {
                    CurrentManglishText.FontSize = fontSize;
                }

                // Apply opacity to border (and let Mica show through)
                double opacity = LocalPreferences.PopupOpacity;
                if (opacity >= 0.3 && opacity <= 1.0)
                {
                    border.Opacity = opacity;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying styles: {ex.Message}");
        }
    }

    private void ApplyTextColorToListItems(Windows.UI.Color textColor)
    {
        // Apply the text color to all items in the ListView
        if (SuggestionsListView?.Items != null)
        {
            foreach (var item in SuggestionsListView.Items)
            {
                if (SuggestionsListView.ContainerFromItem(item) is ListViewItem container)
                {
                    if (container.ContentTemplateRoot is TextBlock textBlock)
                    {
                        textBlock.Foreground = new SolidColorBrush(textColor);
                        textBlock.FontSize = LocalPreferences.PopupFontSize;
                    }
                }
            }
        }
    }

    private static bool TryParseColor(string hexColor, out Windows.UI.Color color)
    {
        color = Windows.UI.Color.FromArgb(255, 255, 255, 255);  // Default to white
        try
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                return false;

            hexColor = hexColor.TrimStart('#');
            if (hexColor.Length != 6 && hexColor.Length != 8)
                return false;

            uint hex = uint.Parse(hexColor, System.Globalization.NumberStyles.HexNumber);

            if (hexColor.Length == 6)
            {
                // RGB format
                color = Windows.UI.Color.FromArgb(
                    255,
                    (byte)((hex >> 16) & 0xFF),
                    (byte)((hex >> 8) & 0xFF),
                    (byte)(hex & 0xFF)
                );
            }
            else
            {
                // ARGB format
                color = Windows.UI.Color.FromArgb(
                    (byte)((hex >> 24) & 0xFF),
                    (byte)((hex >> 16) & 0xFF),
                    (byte)((hex >> 8) & 0xFF),
                    (byte)(hex & 0xFF)
                );
            }
            return true;
        }
        catch
        {
            return false;
        }
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
                    y = mousePos.Y;
                }

                int screenH = GetSystemMetrics(1); // SM_CYSCREEN
                int screenW = GetSystemMetrics(0); // SM_CXSCREEN

                int popupWidth = 180;
                int popupHeight = 260;

                // Push down by default to clear the line of text
                int finalY = (int)y + 30;
                int finalX = (int)x;

                // FIX: If popping down goes off the screen (e.g. typing in Taskbar), push it UP above the cursor
                if (finalY + popupHeight > screenH)
                {
                    finalY = (int)y - popupHeight - 10;
                }

                // FIX: If it goes off the right edge of screen
                if (finalX + popupWidth > screenW)
                {
                    finalX = screenW - popupWidth - 10;
                }

                // Prevent negative coordinates
                if (finalY < 0) finalY = 10;
                if (finalX < 0) finalX = 10;

                ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
                SetWindowPos(_hwnd, HWND_TOPMOST, finalX, finalY, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
            }
            else
            {
                AppWindow.Hide();
            }
        }
    }

    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;
    const uint WS_POPUP = 0x80000000;
    const uint WS_EX_NOACTIVATE = 0x08000000;
    const uint WS_EX_TOOLWINDOW = 0x00000080;
    const int SW_SHOWNOACTIVATE = 4;
    static readonly IntPtr HWND_TOPMOST = new(-1);
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