using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using Winmozhi.Core.Engines;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.UI;

// FIX: Explicitly specify Microsoft.UI.Xaml.Application
public partial class App : Microsoft.UI.Xaml.Application
{
    public IHost Host { get; }

    public App()
    {
        this.InitializeComponent();
        HookTrayMenuEvents();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddDebug(); // We will swap this with Serilog/NLog later
            })
            .ConfigureServices((context, services) =>
            {
                // Use HttpClientFactory to prevent socket exhaustion
                services.AddHttpClient<IOnlineEngine, GoogleOnlineEngine>(client =>
                {
                    // Pretend to be a browser to avoid Google API blocking
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                });

                // Register Engines as Singletons
                services.AddSingleton<IOfflineEngine, TrieOfflineEngine>();
                services.AddSingleton<ITransliterationEngine, HybridTransliterationEngine>();

                // Register Hook Service
                services.AddSingleton<IKeyboardHookService, Winmozhi.Hooks.KeyboardHookService>();

                // Register History DB (Mock)
                services.AddSingleton<IHistoryDatabase, Winmozhi.Core.Engines.MockHistoryDatabase>();
            })
            .Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var logger = Host.Services.GetRequiredService<ILogger<App>>();
        logger.LogCritical(e.Exception, "A fatal unhandled exception occurred.");
        e.Handled = true;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var hookService = Host.Services.GetRequiredService<IKeyboardHookService>();
        hookService.StartHook();
    }

    private void HookTrayMenuEvents()
    {
        if (Resources["TrayIcon"] is H.NotifyIcon.TaskbarIcon trayIcon &&
            trayIcon.ContextFlyout is Microsoft.UI.Xaml.Controls.MenuFlyout flyout)
        {
            if (flyout.Items.Count > 0 && flyout.Items[0] is Microsoft.UI.Xaml.Controls.MenuFlyoutItem settingsItem)
            {
                settingsItem.Click += Settings_Click;
            }

            if (flyout.Items.Count > 2 && flyout.Items[2] is Microsoft.UI.Xaml.Controls.MenuFlyoutItem exitItem)
            {
                exitItem.Click += Exit_Click;
            }
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // Settings Window Phase 4
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Current.Exit();
    }
}