using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using Winmozhi.Core.Interfaces;
using Winmozhi.Core.Utilities;

namespace Winmozhi.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IKeyboardHookService _hookService;
    private readonly IHistoryDatabase _historyDatabase;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsOnlineEngineEnabled { get; set; }

    [ObservableProperty]
    public partial string HistoryStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PopupBackgroundColor { get; set; }

    [ObservableProperty]
    public partial string PopupTextColor { get; set; }

    [ObservableProperty]
    public partial int PopupFontSize { get; set; }

    [ObservableProperty]
    public partial double PopupOpacity { get; set; }

    // ── Color Picker Conversions ──────────────────────────────────────────────────

    public Windows.UI.Color PopupBackgroundColorColor
    {
        get => HexToColor(PopupBackgroundColor, Windows.UI.Color.FromArgb(255, 26, 26, 26));
        set => PopupBackgroundColor = ColorToHex(value);
    }
    public SolidColorBrush PopupBackgroundColorBrush => new(PopupBackgroundColorColor);

    public Windows.UI.Color PopupTextColorColor
    {
        get => HexToColor(PopupTextColor, Windows.UI.Color.FromArgb(255, 255, 255, 255));
        set => PopupTextColor = ColorToHex(value);
    }
    public SolidColorBrush PopupTextColorBrush => new(PopupTextColorColor);

    public string PopupOpacityPercentage => $"{(PopupOpacity * 100):F0}% opacity";

    public SettingsViewModel(IKeyboardHookService hookService, IHistoryDatabase historyDatabase)
    {
        _hookService = hookService;
        _historyDatabase = historyDatabase;

        LocalPreferences.Load();
        IsEnabled = LocalPreferences.IsHookEnabled;
        IsOnlineEngineEnabled = LocalPreferences.IsOnlineEngineEnabled;
        PopupBackgroundColor = LocalPreferences.PopupBackgroundColor;
        PopupTextColor = LocalPreferences.PopupTextColor;
        PopupFontSize = LocalPreferences.PopupFontSize;
        PopupOpacity = LocalPreferences.PopupOpacity;

        if (IsEnabled) _hookService.StartHook();
    }

    partial void OnIsEnabledChanged(bool value)
    {
        LocalPreferences.IsHookEnabled = value;
        LocalPreferences.Save();
        if (value) _hookService.StartHook();
        else _hookService.StopHook();
    }

    partial void OnIsOnlineEngineEnabledChanged(bool value)
    {
        LocalPreferences.IsOnlineEngineEnabled = value;
        LocalPreferences.Save();
    }

    partial void OnPopupBackgroundColorChanged(string value)
    {
        LocalPreferences.PopupBackgroundColor = value;
        LocalPreferences.Save();
        OnPropertyChanged(nameof(PopupBackgroundColorColor));
        OnPropertyChanged(nameof(PopupBackgroundColorBrush));
    }

    partial void OnPopupTextColorChanged(string value)
    {
        LocalPreferences.PopupTextColor = value;
        LocalPreferences.Save();
        OnPropertyChanged(nameof(PopupTextColorColor));
        OnPropertyChanged(nameof(PopupTextColorBrush));
    }

    partial void OnPopupFontSizeChanged(int value)
    {
        LocalPreferences.PopupFontSize = value;
        LocalPreferences.Save();
    }

    partial void OnPopupOpacityChanged(double value)
    {
        LocalPreferences.PopupOpacity = value;
        LocalPreferences.Save();
        OnPropertyChanged(nameof(PopupOpacityPercentage));
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
    private void ExitApp()
    {
        _hookService.StopHook();
        Application.Current.Exit();
    }

    // ── Hex Parsers ───────────────────────────────────────────────────────────────
    private static Windows.UI.Color HexToColor(string hex, Windows.UI.Color fallback)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                return Windows.UI.Color.FromArgb(255,
                    byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber));
            }
        }
        catch { }
        return fallback;
    }

    private static string ColorToHex(Windows.UI.Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}