using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Winmozhi.Core.Engines;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.UI;

public partial class App : Microsoft.UI.Xaml.Application
{
    public IHost? Host { get; private set; }
    private Window? _popupWindow;

    // --- Native Win32 Hide Method ---
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;

    public App()
    {
        this.InitializeComponent();

        // Catch global rendering crashes and write them to desktop
        this.UnhandledException += (s, e) =>
        {
            e.Handled = true;
            WriteCrashLog("Global_Crash", e.Exception.ToString());
        };

        try
        {
            // Configure HttpClient with optimized settings for Google API
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // HttpClient with optimized pool settings for better performance
                    services.AddHttpClient<IOnlineEngine, GoogleOnlineEngine>(client =>
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        client.Timeout = TimeSpan.FromSeconds(3); // Total timeout including retries
                        client.DefaultRequestHeaders.ConnectionClose = false; // Enable connection reuse
                    });

                    services.AddSingleton<IOfflineEngine, TrieOfflineEngine>();
                    services.AddSingleton<ITransliterationEngine, HybridTransliterationEngine>();
                    services.AddSingleton<IKeyboardHookService, Winmozhi.Hooks.KeyboardHookService>();
                    services.AddSingleton<IHistoryDatabase, SqliteHistoryDatabase>();
                    services.AddSingleton<Winmozhi.UI.ViewModels.PopupViewModel>();
                    services.AddSingleton<Winmozhi.UI.Views.PopupView>();
                    services.AddSingleton<Winmozhi.UI.ViewModels.SettingsViewModel>();
                    services.AddSingleton<Winmozhi.UI.Views.SettingsWindow>();
                })
                .Build();
        }
        catch (Exception ex) { WriteCrashLog("Constructor_Crash", ex.ToString()); }
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            if (Host != null)
            {
                await Host.StartAsync();

                // Initialize Popup first (critical path)
                _popupWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.PopupView>();
                _popupWindow.AppWindow.Hide();

                // Start keyboard hook immediately (critical for functionality)
                var hookService = Host.Services.GetRequiredService<IKeyboardHookService>();
                hookService.StartHook();

                // Load dictionary on background thread to avoid blocking UI
                _ = LoadDictionaryAsync(Host.Services);

                // Lazy-load settings window on background thread (not critical)
                _ = InitializeSettingsWindowAsync(Host.Services);

                // Warm up engines on background thread
                _ = WarmUpEnginesAsync(Host.Services);
            }
        }
        catch (Exception ex) { WriteCrashLog("OnLaunched_Crash", ex.ToString()); }
    }

    /// <summary>
    /// Load dictionary on background thread to avoid blocking UI thread.
    /// </summary>
    private async Task LoadDictionaryAsync(IServiceProvider services)
    {
        try
        {
            await Task.Run(() =>
            {
                var offlineEngine = services.GetRequiredService<IOfflineEngine>();
                offlineEngine.LoadDictionary(Winmozhi.Core.Engines.CommonWordsDictionary.GetStarterWords());
            });
        }
        catch (Exception ex)
        {
            WriteCrashLog("LoadDictionary_Error", ex.ToString());
        }
    }

    /// <summary>
    /// Initialize settings window on background thread.
    /// </summary>
    private async Task InitializeSettingsWindowAsync(IServiceProvider services)
    {
        try
        {
            await Task.Delay(500); // Small delay to ensure UI is responsive first

            var settingsWindow = services.GetRequiredService<Winmozhi.UI.Views.SettingsWindow>();

            // Show the window on-screen briefly so XAML renders everything including the tray icon
            settingsWindow.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(100, 100, 500, 600));
            settingsWindow.Activate(); // Forces XAML to render the Tray Icon

            // Small delay to ensure rendering is complete
            await Task.Delay(100);

            // Now hide it safely after rendering is done
            settingsWindow.AppWindow.Hide();
        }
        catch (Exception ex)
        {
            WriteCrashLog("SettingsWindow_Init", ex.ToString());
        }
    }

    /// <summary>
    /// Warm up translation engines to pre-compile code paths.
    /// </summary>
    private async Task WarmUpEnginesAsync(IServiceProvider services)
    {
        try
        {
            // Small delay to ensure critical paths are loaded
            await Task.Delay(1000);

            var engine = services.GetRequiredService<ITransliterationEngine>();

            // Warm up instant suggestions (fast path)
            await engine.GetInstantSuggestionsAsync("a", System.Threading.CancellationToken.None);

            // Warm up online suggestions (network path) with timeout to not block
            using var cts = new System.Threading.CancellationTokenSource(2000);
            try
            {
                await engine.GetOnlineSuggestionsAsync("a", cts.Token);
            }
            catch { } // Ignore warmup failures
        }
        catch (Exception ex)
        {
            WriteCrashLog("WarmupEngines_Error", ex.ToString());
        }
    }

    private static void WriteCrashLog(string fileName, string error)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string errorFile = System.IO.Path.Combine(desktopPath, $"{fileName}.txt");
        System.IO.File.WriteAllText(errorFile, error);
    }
}