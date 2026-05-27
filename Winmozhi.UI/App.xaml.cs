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

namespace Winmozhi.UI;

public partial class App : Microsoft.UI.Xaml.Application
{
    public IHost? Host { get; private set; }
    private Window? _popupWindow;
    private static Winmozhi.UI.Views.SettingsWindow? _settingsWindow;

    // PRODUCTION FIX: Prevent multiple instances of the app
    private static Mutex? _mutex;
    // ADDED: Event handle for cross-instance communication
    private static EventWaitHandle? _showSettingsEvent;

    public App()
    {
        // 1. Check if Winmozhi is already running.
        _mutex = new Mutex(true, "Winmozhi_Global_Single_Instance", out bool isNewInstance);
        if (!isNewInstance)
        {
            // Tell the FIRST instance to show the settings window!
            if (EventWaitHandle.TryOpenExisting("Winmozhi_Show_Settings_Event", out var existingEvent))
            {
                existingEvent.Set(); // Send the signal
            }
            Environment.Exit(0);
            return;
        }

        // 1.5 Create the event listener so FUTURE instances can wake this one up
        _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Winmozhi_Show_Settings_Event");
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        Task.Run(() =>
        {
            while (true)
            {
                // This thread waits quietly in the background until the 2nd instance signals it
                _showSettingsEvent.WaitOne();

                // Switch back to the UI thread to safely open the Settings window
                dispatcher?.TryEnqueue(() =>
                {
                    _settingsWindow?.ShowSettings();
                });
            }
        });

        this.InitializeComponent();

        // 2. PRODUCTION FIX: Crash Logger
        this.UnhandledException += (s, e) =>
        {
            e.Handled = true; // Prevent Windows from showing the ugly crash dialog
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Winmozhi", "Logs");
                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "crash.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] FATAL ERROR: {e.Exception?.Message}\n{e.Exception?.StackTrace}\n\n");
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

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        LocalPreferences.Load();
        await Host!.StartAsync();

        var hookService = Host.Services.GetRequiredService<IKeyboardHookService>();
        hookService.StartHook();

        _popupWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.PopupView>();

        // Force WinUI to build the swapchain off-screen to avoid the invisible window bug
        _popupWindow.AppWindow.Move(new Windows.Graphics.PointInt32(-10000, -10000));
        _popupWindow.AppWindow.Show();

        if (_settingsWindow == null)
            _settingsWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.SettingsWindow>();

        _settingsWindow.ShowSettings();

        _ = Task.Run(() =>
        {
            var offlineEngine = Host.Services.GetRequiredService<IOfflineEngine>();
            offlineEngine.LoadDictionary(CommonWordsDictionary.GetStarterWords());
        });
    }
}