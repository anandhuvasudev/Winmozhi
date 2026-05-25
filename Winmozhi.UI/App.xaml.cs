using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
    private static Winmozhi.UI.Views.SettingsWindow? _settingsWindow;
    private TaskbarIcon? _trayIcon;

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

        // Fix: Force WinUI to build the swapchain off-screen to avoid the invisible window bug
        _popupWindow.AppWindow.Move(new Windows.Graphics.PointInt32(-10000, -10000));
        _popupWindow.AppWindow.Show();

        if (_settingsWindow == null)
            _settingsWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.SettingsWindow>();

        // Create and initialize taskbar tray icon (kept for the app lifetime)
        try
        {
            _trayIcon = new TaskbarIcon()
            {
                ToolTipText = "Winmozhi Manglish Keyboard",
                };

            var menu = new MenuFlyout();

            var openItem = new MenuFlyoutItem { Text = "Open Settings" };
            openItem.Click += (s, e) => ShowSettingsFromTray();
            menu.Items.Add(openItem);

            menu.Items.Add(new MenuFlyoutSeparator());

            var quitItem = new MenuFlyoutItem { Text = "Quit Winmozhi" };
            quitItem.Click += (s, e) => Environment.Exit(0);
            menu.Items.Add(quitItem);

            _trayIcon.ContextFlyout = menu;
            _trayIcon.DoubleTapped += (s, e) => ShowSettingsFromTray();
        }
        catch { /* best-effort, don't crash startup if tray fails */ }

        _settingsWindow.AppWindow.Show();

        _ = Task.Run(() =>
        {
            var offlineEngine = Host.Services.GetRequiredService<IOfflineEngine>();
            offlineEngine.LoadDictionary(CommonWordsDictionary.GetStarterWords());
        });
    }

    private void ShowSettingsFromTray()
    {
        try
        {
            _settingsWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                var displayArea = DisplayArea.GetFromWindowId(_settingsWindow.AppWindow.Id, DisplayAreaFallback.Primary);
                int width = 700;
                int height = 550;
                int x = (displayArea.WorkArea.Width - width) / 2;
                int y = (displayArea.WorkArea.Height - height) / 2;

                _settingsWindow.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
                _settingsWindow.AppWindow.Show();
                _settingsWindow.Activate();
            });
        }
        catch { }
    }
}
