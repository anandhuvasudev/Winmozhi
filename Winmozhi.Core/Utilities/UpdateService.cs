using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Winmozhi.Core.Utilities;

public static class UpdateService
{
    // REPLACE THIS WITH YOUR GITHUB USERNAME
    private const string GitHubRepoApiUrl = "https://api.github.com/repos/anandhuvasudev/Winmozhi/releases/latest";
    public const string CurrentVersion = "v1.0.0"; // You will bump this locally as needed

    public static async Task<(bool UpdateAvailable, string LatestVersion, string DownloadUrl)> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Winmozhi-Updater");

            var response = await client.GetStringAsync(GitHubRepoApiUrl);
            using var doc = JsonDocument.Parse(response);

            var latestVersion = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

            // Fixed CA1866: Changed "v" to 'v'
            if (latestVersion != CurrentVersion && latestVersion.StartsWith('v'))
            {
                var assets = doc.RootElement.GetProperty("assets");
                if (assets.GetArrayLength() > 0)
                {
                    string downloadUrl = assets[0].GetProperty("browser_download_url").GetString() ?? "";
                    return (true, latestVersion, downloadUrl);
                }
            }
        }
        catch { /* Network error, ignore silently */ }
        return (false, string.Empty, string.Empty);
    }

    public static async Task ApplyUpdateAsync(string downloadUrl)
    {
        try
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), "Winmozhi_Update.zip");
            string currentAppFolder = AppContext.BaseDirectory;

            // 1. Download the new zip file
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Winmozhi-Updater");
            var response = await client.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(tempZipPath, response);

            // 2. Create a PowerShell script that waits for Winmozhi to close, extracts the update, and restarts it
            string psScript = $@"
                Start-Sleep -Seconds 3;
                Expand-Archive -Path '{tempZipPath}' -DestinationPath '{currentAppFolder}' -Force;
                Remove-Item '{tempZipPath}' -Force;
                Start-Process '{Path.Combine(currentAppFolder, "Winmozhi.UI.exe")}';
            ";

            // 3. Launch the invisible background updater
            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"{psScript}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(processInfo);

            // 4. Forcefully exit the app immediately so the script can overwrite the files
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update Failed: {ex.Message}");
        }
    }
}