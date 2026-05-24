using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.UI.ViewModels;

// Primary Constructor gracefully injects IKeyboardHookService
public partial class SettingsViewModel(IKeyboardHookService hookService) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    partial void OnIsEnabledChanged(bool value)
    {
        if (value) hookService.StartHook();
        else hookService.StopHook();
    }

    [RelayCommand]
    private void ExitApp()
    {
        hookService.StopHook();
        Application.Current.Exit();
    }
}