using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Winmozhi.UI.ViewModels;

namespace Winmozhi.UI.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();

        try { this.ExtendsContentIntoTitleBar = true; } catch { }

        AppWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            AppWindow.Hide();
            Winmozhi.Core.Utilities.MemoryOptimizer.TrimMemory();
        };
    }

    // Fix IDE0060: Used '_' to indicate unused parameters
    private void TrayIcon_DoubleTapped(object _, RoutedEventArgs _1) => ShowSettings();

    private void ShowSettings_Click(object _, RoutedEventArgs _1) => ShowSettings();

    private void ShowSettings()
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            int width = 700;
            int height = 550;
            int x = (displayArea.WorkArea.Width - width) / 2;
            int y = (displayArea.WorkArea.Height - height) / 2;

            AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
            AppWindow.Show();
            this.Activate();
        });
    }
}