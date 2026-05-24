using Microsoft.UI.Xaml;
using Winmozhi.UI.ViewModels;

namespace Winmozhi.UI.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;

        AppWindow.Resize(new Windows.Graphics.SizeInt32(500, 600));

        AppWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            AppWindow.Hide();
            Winmozhi.Core.Utilities.MemoryOptimizer.TrimMemory();
        };
    }

    private void TrayIcon_DoubleTapped(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void ShowSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void ShowSettings()
    {
        AppWindow.Show();
        this.Activate();
    }
}