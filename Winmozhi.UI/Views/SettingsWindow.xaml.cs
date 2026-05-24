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

        try
        {
            // FIX: Using the XAML Window API correctly themes the titlebar caption buttons (Close/Min/Max) 
            // automatically to match the System Backdrop (Light/Dark themes) instead of forcing them black.
            this.ExtendsContentIntoTitleBar = true;
        }
        catch { }

        AppWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            AppWindow.Hide();
            Winmozhi.Core.Utilities.MemoryOptimizer.TrimMemory();
        };
    }

    private void TrayIcon_DoubleTapped(object sender, RoutedEventArgs e) => ShowSettings();

    private void ShowSettings_Click(object sender, RoutedEventArgs e) => ShowSettings();

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