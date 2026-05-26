using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Winmozhi.UI.ViewModels;

namespace Winmozhi.UI.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }
    private readonly IntPtr _hwnd;

    // Upgraded to modern [LibraryImport] to resolve SYSLIB1054 warnings
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_RESTORE = 9;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        ViewModel.RequestShowSettings = WakeUpAndShowSettings;

        try { this.ExtendsContentIntoTitleBar = true; this.SetTitleBar(AppTitleBar); } catch { }

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        try
        {
            string iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "WindowIcon.ico");
            if (System.IO.File.Exists(iconPath)) AppWindow.SetIcon(iconPath);
        }
        catch { }

        AppWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            ShowWindow(_hwnd, SW_HIDE);
            Winmozhi.Core.Utilities.MemoryOptimizer.TrimMemory();
        };
    }

    public void ShowSettings() => WakeUpAndShowSettings();

    private void WakeUpAndShowSettings()
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);

            int width = 480;
            int height = 760;
            int x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - width) / 2;
            int y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - height) / 2;

            AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));

            ShowWindow(_hwnd, SW_RESTORE);
            SetForegroundWindow(_hwnd);
            this.Activate();
        });
    }
}