using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.UI.ViewModels;

public partial class SettingsViewModel(IKeyboardHookService hookService, IHistoryDatabase historyDatabase) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string HistoryStatusMessage { get; set; } = string.Empty;

    partial void OnIsEnabledChanged(bool value)
    {
        if (value) hookService.StartHook();
        else hookService.StopHook();
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await historyDatabase.ClearHistoryAsync();
        HistoryStatusMessage = "Personalized history cleared successfully!";

        // Hide message after 3 seconds
        await Task.Delay(3000);
        HistoryStatusMessage = string.Empty;
    }

    [RelayCommand]
    private void ExitApp()
    {
        hookService.StopHook();
        Application.Current.Exit();
    }
}