using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Winmozhi.Core.Engines;
using Winmozhi.Core.Interfaces;
using Winmozhi.Core.Utilities;

namespace Winmozhi.UI;

public partial class App : Microsoft.UI.Xaml.Application
{
    public IHost? Host { get; private set; }
    private Window? _popupWindow;

    // Keep a static reference to the settings window so it persists across activations
    private static Winmozhi.UI.Views.SettingsWindow? _settingsWindow;

    public App()
    {
        this.InitializeComponent();

        this.UnhandledException += (s, e) =>
        {
            e.Handled = true;
        };

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient<IOnlineEngine, GoogleOnlineEngine>(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    client.Timeout = TimeSpan.FromSeconds(3);
                });

                services.AddSingleton<IOfflineEngine, TrieOfflineEngine>();
                services.AddSingleton<ITransliterationEngine, HybridTransliterationEngine>();
                services.AddSingleton<IKeyboardHookService, Winmozhi.Hooks.KeyboardHookService>();
                services.AddSingleton<IHistoryDatabase, SqliteHistoryDatabase>();

                services.AddTransient<Winmozhi.UI.ViewModels.PopupViewModel>();
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

        _popupWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.PopupView>();

        // Create or reuse the settings window
        if (_settingsWindow == null)
        {
            _settingsWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.SettingsWindow>();
        }

        // Show the settings window on launch
        _settingsWindow.AppWindow.Show();

        // Load dictionary on background thread
        _ = Task.Run(() =>
        {
            var offlineEngine = Host.Services.GetRequiredService<IOfflineEngine>();
            offlineEngine.LoadDictionary(CommonWordsDictionary.GetStarterWords());
        });
    }
}