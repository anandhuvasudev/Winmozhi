using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Winmozhi.UI.ViewModels;

namespace Winmozhi.UI.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }
    private readonly IntPtr _hwnd;

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
        this.DispatcherQueue.TryEnqueue(async () =>
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

            // First-Run Logic: Automatically show tutorial if first time opening
            if (Winmozhi.Core.Utilities.LocalPreferences.IsFirstRun)
            {
                Winmozhi.Core.Utilities.LocalPreferences.IsFirstRun = false;
                Winmozhi.Core.Utilities.LocalPreferences.Save();

                // Slight delay to ensure the UI has finished drawing before throwing the dialog
                await Task.Delay(200);
                await ShowHowToUseDialogAsync();
            }
        });
    }

    private void ShowHowToUse_Click(object sender, RoutedEventArgs e)
    {
        _ = ShowHowToUseDialogAsync();
    }

    private async Task ShowHowToUseDialogAsync()
    {
        var stackPanel = new StackPanel { Spacing = 12 };

        stackPanel.Children.Add(new TextBlock { Text = "Getting Started", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 16 });
        stackPanel.Children.Add(new TextBlock { Text = "1. Type anywhere in Manglish (e.g., 'njan').", TextWrapping = TextWrapping.Wrap });
        stackPanel.Children.Add(new TextBlock { Text = "2. A popup will instantly appear with Malayalam suggestions.", TextWrapping = TextWrapping.Wrap });
        stackPanel.Children.Add(new TextBlock { Text = "3. Press Space or Enter to insert the highlighted word.", TextWrapping = TextWrapping.Wrap });
        stackPanel.Children.Add(new TextBlock { Text = "4. Use Up/Down Arrow keys to choose alternative suggestions.", TextWrapping = TextWrapping.Wrap });

        stackPanel.Children.Add(new TextBlock { Text = "Global Shortcut", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 16, Margin = new Thickness(0, 16, 0, 0) });
        stackPanel.Children.Add(new TextBlock { Text = "Press Ctrl + Shift + M at any time to temporarily pause or resume Winmozhi across your entire system.", TextWrapping = TextWrapping.Wrap });

        var dialog = new ContentDialog
        {
            Title = "Welcome to Winmozhi! 🎉",
            Content = stackPanel,
            CloseButtonText = "Got it!",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot // Required in WinUI 3
        };

        await dialog.ShowAsync();
    }
}