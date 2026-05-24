using CommunityToolkit.Mvvm.Input;
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

        // Customise window sizes
        AppWindow.Resize(new Windows.Graphics.SizeInt32(500, 600));

        // When user clicks the "X" (Close button), minimize to System Tray instead of exiting.
        AppWindow.Closing += (s, e) =>
        {
            e.Cancel = true;      // Prevent actual destruction of the window
            AppWindow.Hide();     // Hide it (runs in background)
        };
    }

    // Command to show window from tray double-click
    [RelayCommand]
    private void ShowSettings()
    {
        AppWindow.Show();
        this.Activate(); // Brings window to front
    }

    private void ShowSettings_Click(object _1, RoutedEventArgs _2)
    {
        ShowSettings();
    }
}