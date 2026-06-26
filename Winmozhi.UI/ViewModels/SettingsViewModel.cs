using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Threading.Tasks;
using Winmozhi.Core.Interfaces;
using Winmozhi.Core.Utilities;

namespace Winmozhi.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IKeyboardHookService _hookService;
    private readonly IHistoryDatabase _historyDatabase;

    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial bool IsOnlineEngineEnabled { get; set; }
    [ObservableProperty] public partial bool IsFmlFontModeEnabled { get; set; }
    [ObservableProperty] public partial bool IsMlFontModeEnabled { get; set; }
    [ObservableProperty] public partial string HistoryStatusMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial string PopupBackgroundColor { get; set; }
    [ObservableProperty] public partial string PopupTextColor { get; set; }
    [ObservableProperty] public partial int PopupFontSize { get; set; }
    [ObservableProperty] public partial double PopupOpacity { get; set; }

    public double PopupFontSizeDouble
    {
        get => PopupFontSize;
        set { if (PopupFontSize != (int)value) { PopupFontSize = (int)value; OnPropertyChanged(nameof(PopupFontSizeDouble)); } }
    }

    private bool _runAtStartup;
    public bool RunAtStartup
    {
        get => _runAtStartup;
        set
        {
            if (_runAtStartup != value)
            {
                _runAtStartup = value;
                SetRunAtStartupAsync(value);
                OnPropertyChanged(nameof(RunAtStartup));
            }
        }
    }

    public Windows.UI.Color PopupBackgroundColorColor
    {
        get => HexToColor(PopupBackgroundColor, Windows.UI.Color.FromArgb(255, 0, 0, 0));
        set => PopupBackgroundColor = ColorToHex(value);
    }
    public SolidColorBrush PopupBackgroundColorBrush => new(PopupBackgroundColorColor);

    public Windows.UI.Color PopupTextColorColor
    {
        get => HexToColor(PopupTextColor, Windows.UI.Color.FromArgb(255, 255, 255, 255));
        set => PopupTextColor = ColorToHex(value);
    }
    public SolidColorBrush PopupTextColorBrush => new(PopupTextColorColor);

    public string PopupOpacityPercentage => $"{(PopupOpacity * 100):F0}%";

    public IRelayCommand ShowSettingsCommand { get; }
    public Action? RequestShowSettings { get; set; }

    public SettingsViewModel(IKeyboardHookService hookService, IHistoryDatabase historyDatabase)
    {
        _hookService = hookService;
        _historyDatabase = historyDatabase;

        ShowSettingsCommand = new RelayCommand(() => RequestShowSettings?.Invoke());

        LocalPreferences.Load();
        IsEnabled = LocalPreferences.IsHookEnabled;
        IsOnlineEngineEnabled = LocalPreferences.IsOnlineEngineEnabled;
        IsFmlFontModeEnabled = LocalPreferences.IsFmlFontModeEnabled;
        IsMlFontModeEnabled = LocalPreferences.IsMlFontModeEnabled;
        PopupBackgroundColor = LocalPreferences.PopupBackgroundColor;
        PopupTextColor = LocalPreferences.PopupTextColor;
        PopupFontSize = LocalPreferences.PopupFontSize;
        PopupOpacity = LocalPreferences.PopupOpacity;

        _hookService.IsTransliterationEnabled = IsEnabled;

        _hookService.OnStateChanged += (s, isEnabled) =>
        {
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            dispatcher?.TryEnqueue(() =>
            {
                IsEnabled = isEnabled;
                LocalPreferences.IsHookEnabled = isEnabled;
                LocalPreferences.Save();
            });
        };

        // Initialize Startup Task status correctly for MSIX apps and Unpackaged apps
        _ = InitStartupStateAsync();
    }

    partial void OnIsEnabledChanged(bool value) { LocalPreferences.IsHookEnabled = value; LocalPreferences.Save(); _hookService.IsTransliterationEnabled = value; }
    partial void OnIsOnlineEngineEnabledChanged(bool value) { LocalPreferences.IsOnlineEngineEnabled = value; LocalPreferences.Save(); }
    partial void OnIsFmlFontModeEnabledChanged(bool value) { LocalPreferences.IsFmlFontModeEnabled = value; if (value && IsMlFontModeEnabled) IsMlFontModeEnabled = false; LocalPreferences.Save(); }
    partial void OnIsMlFontModeEnabledChanged(bool value) { LocalPreferences.IsMlFontModeEnabled = value; if (value && IsFmlFontModeEnabled) IsFmlFontModeEnabled = false; LocalPreferences.Save(); }
    partial void OnPopupBackgroundColorChanged(string value) { LocalPreferences.PopupBackgroundColor = value; LocalPreferences.Save(); OnPropertyChanged(nameof(PopupBackgroundColorColor)); OnPropertyChanged(nameof(PopupBackgroundColorBrush)); }
    partial void OnPopupTextColorChanged(string value) { LocalPreferences.PopupTextColor = value; LocalPreferences.Save(); OnPropertyChanged(nameof(PopupTextColorColor)); OnPropertyChanged(nameof(PopupTextColorBrush)); }
    partial void OnPopupFontSizeChanged(int value) { LocalPreferences.PopupFontSize = value; LocalPreferences.Save(); }
    partial void OnPopupOpacityChanged(double value) { LocalPreferences.PopupOpacity = value; LocalPreferences.Save(); OnPropertyChanged(nameof(PopupOpacityPercentage)); }

    // Helper to detect if running as a Store App (Packaged) or GitHub Release (Unpackaged)
    private static bool IsRunningAsPackaged()
    {
        try
        {
            // This throws an exception if the app is unpackaged
            return Windows.ApplicationModel.Package.Current != null;
        }
        catch
        {
            return false;
        }
    }

    private async Task InitStartupStateAsync()
    {
        try
        {
            if (IsRunningAsPackaged())
            {
                // Packaged: Use native WinRT API (Required for Microsoft Store)
                var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("WinmozhiStartup");
                _runAtStartup = startupTask.State == Windows.ApplicationModel.StartupTaskState.Enabled;
            }
            else
            {
                // Unpackaged: Use Windows Registry
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
                _runAtStartup = key?.GetValue("WinmozhiStartup") != null;
            }
            OnPropertyChanged(nameof(RunAtStartup));
        }
        catch { }
    }

    private static async void SetRunAtStartupAsync(bool enable)
    {
        try
        {
            if (IsRunningAsPackaged())
            {
                // Packaged: Use native WinRT API
                var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("WinmozhiStartup");
                if (enable) await startupTask.RequestEnableAsync();
                else startupTask.Disable();
            }
            else
            {
                // Unpackaged: Use Windows Registry
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (enable)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("WinmozhiStartup", $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue("WinmozhiStartup", false);
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _historyDatabase.ClearHistoryAsync();
        HistoryStatusMessage = "History cleared successfully!";
        await Task.Delay(3000);
        HistoryStatusMessage = string.Empty;
    }

    [RelayCommand]
    private void ExitApp() { _hookService.StopHook(); Application.Current.Exit(); }

    private static Windows.UI.Color HexToColor(string hex, Windows.UI.Color fallback) { try { if (string.IsNullOrWhiteSpace(hex)) return fallback; hex = hex.TrimStart('#'); if (hex.Length == 6) return Windows.UI.Color.FromArgb(255, byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber), byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber), byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber)); } catch { } return fallback; }
    private static string ColorToHex(Windows.UI.Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}