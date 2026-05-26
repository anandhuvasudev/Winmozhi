using System;
using System.IO;
using System.Text.Json;

namespace Winmozhi.Core.Utilities;

public static class LocalPreferences
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Winmozhi", "settings.json");

    // ── Feature Toggles ──────────────────────────────────────────────────────
    public static bool IsFirstRun { get; set; } = true; // NEW FLAG
    public static bool IsHookEnabled { get; set; } = true;
    public static bool IsOnlineEngineEnabled { get; set; } = false;
    public static bool IsFmlFontModeEnabled { get; set; } = false;
    public static bool IsMlFontModeEnabled { get; set; } = false;

    // ── Popup Styling ────────────────────────────────────────────────────────
    public static string PopupBackgroundColor { get; set; } = GetSystemAccentColorHex();
    public static string PopupTextColor { get; set; } = "#FFFFFF";
    public static int PopupFontSize { get; set; } = 18;
    public static double PopupOpacity { get; set; } = 0.90;

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var doc = JsonDocument.Parse(json);

                IsFirstRun = GetBoolProperty(doc, nameof(IsFirstRun), true);
                IsHookEnabled = GetBoolProperty(doc, nameof(IsHookEnabled), true);
                IsOnlineEngineEnabled = GetBoolProperty(doc, nameof(IsOnlineEngineEnabled), true);
                IsFmlFontModeEnabled = GetBoolProperty(doc, nameof(IsFmlFontModeEnabled), false);
                IsMlFontModeEnabled = GetBoolProperty(doc, nameof(IsMlFontModeEnabled), false);

                PopupBackgroundColor = GetStringProperty(doc, nameof(PopupBackgroundColor), GetSystemAccentColorHex());
                PopupTextColor = GetStringProperty(doc, nameof(PopupTextColor), "#FFFFFF");
                PopupFontSize = GetIntProperty(doc, nameof(PopupFontSize), 18);
                PopupOpacity = GetDoubleProperty(doc, nameof(PopupOpacity), 0.90);
            }
        }
        catch { /* Fallback to defaults */ }
    }

    private static readonly JsonSerializerOptions _cachedOptions = new() { WriteIndented = true };

    public static event Action? PreferencesChanged;

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var settings = new
            {
                IsFirstRun,
                IsHookEnabled,
                IsOnlineEngineEnabled,
                IsFmlFontModeEnabled,
                IsMlFontModeEnabled,
                PopupBackgroundColor,
                PopupTextColor,
                PopupFontSize,
                PopupOpacity
            };
            var json = JsonSerializer.Serialize(settings, _cachedOptions);
            File.WriteAllText(SettingsPath, json);

            PreferencesChanged?.Invoke();
        }
        catch { }
    }

    private static string GetSystemAccentColorHex()
    {
        try
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        catch { return "#0078D7"; }
    }

    private static bool GetBoolProperty(JsonDocument doc, string propertyName, bool defaultValue)
    {
        try
        {
            if (doc.RootElement.TryGetProperty(propertyName, out var element) && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False))
                return element.GetBoolean();
        }
        catch { }
        return defaultValue;
    }

    private static string GetStringProperty(JsonDocument doc, string propertyName, string defaultValue)
    {
        try
        {
            if (doc.RootElement.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? defaultValue;
        }
        catch { }
        return defaultValue;
    }

    private static int GetIntProperty(JsonDocument doc, string propertyName, int defaultValue)
    {
        try
        {
            if (doc.RootElement.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Number)
                return element.GetInt32();
        }
        catch { }
        return defaultValue;
    }

    private static double GetDoubleProperty(JsonDocument doc, string propertyName, double defaultValue)
    {
        try
        {
            if (doc.RootElement.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Number)
                return element.GetDouble();
        }
        catch { }
        return defaultValue;
    }
}