using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Winmozhi.Core.Engines;
using Winmozhi.Core.Interfaces;
using Winmozhi.Core.Utilities;
using Microsoft.Windows.AppLifecycle;

namespace Winmozhi.UI;

public partial class App : Microsoft.UI.Xaml.Application
{
    public IHost? Host { get; private set; }
    private Window? _popupWindow;
    private static Winmozhi.UI.Views.SettingsWindow? _settingsWindow;

    public App()
    {
        // 1. Check for existing instance (Phase 1 Fix already present)
        var mainInstance = AppInstance.FindOrRegisterForKey("Winmozhi_Main_Instance");
        if (!mainInstance.IsCurrent)
        {
            var args = AppInstance.GetCurrent().GetActivatedEventArgs();
            mainInstance.RedirectActivationToAsync(args).AsTask().Wait();
            Environment.Exit(0);
            return;
        }

        mainInstance.Activated += MainInstance_Activated;

        this.InitializeComponent();

        // FIX (Phase 5.3): Safely catch UI Thread exceptions
        this.UnhandledException += (s, e) =>
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Winmozhi", "Logs");
                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "crash.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] UI FATAL ERROR: {e.Exception?.Message}\n{e.Exception?.StackTrace}\n\n");
            }
            catch { /* Failsafe */ }
        };

        // FIX (Phase 5.3): Safely catch Background Thread exceptions to prevent silent crashes
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Winmozhi", "Logs");
                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "crash.log");
                var ex = e.ExceptionObject as Exception;
                File.AppendAllText(logFile, $"[{DateTime.Now}] BACKGROUND FATAL ERROR: {ex?.Message}\n{ex?.StackTrace}\n\n");
            }
            catch { /* Failsafe */ }
        };

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient<IOnlineEngine, GoogleOnlineEngine>(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Winmozhi/1.0 (Windows NT 10.0; Win64; x64)");
                    client.Timeout = TimeSpan.FromSeconds(3);
                });

                services.AddSingleton<IOfflineEngine, TrieOfflineEngine>();
                services.AddSingleton<ITransliterationEngine, HybridTransliterationEngine>();
                services.AddSingleton<IKeyboardHookService, Winmozhi.Hooks.KeyboardHookService>();
                services.AddSingleton<IHistoryDatabase, SqliteHistoryDatabase>();

                services.AddSingleton<Winmozhi.UI.ViewModels.PopupViewModel>();
                services.AddSingleton<Winmozhi.UI.Views.PopupView>();

                services.AddTransient<Winmozhi.UI.ViewModels.SettingsViewModel>();
                services.AddSingleton<Winmozhi.UI.Views.SettingsWindow>();
            })
            .Build();
    }

    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
        var dispatcher = _settingsWindow?.DispatcherQueue ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        dispatcher?.TryEnqueue(() =>
        {
            _settingsWindow?.ShowSettings();
        });
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        LocalPreferences.Load();
        await Host!.StartAsync();

        var hookService = Host.Services.GetRequiredService<IKeyboardHookService>();
        hookService.StartHook();

        _popupWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.PopupView>();
        _popupWindow.AppWindow.Move(new Windows.Graphics.PointInt32(-10000, -10000));
        _popupWindow.AppWindow.Show();

        if (_settingsWindow == null)
            _settingsWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.SettingsWindow>();

        _settingsWindow.ShowSettings();

        // Note: Phase 3 limits this to top 2,000 words only
        _ = Task.Run(() =>
        {
            var offlineEngine = Host.Services.GetRequiredService<IOfflineEngine>();
            offlineEngine.LoadDictionary(CommonWordsDictionary.GetStarterWords());
        });
    }
}