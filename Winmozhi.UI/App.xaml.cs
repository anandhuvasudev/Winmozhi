using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using Winmozhi.Core.Engines;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.UI;

public partial class App : Microsoft.UI.Xaml.Application
{
    public IHost? Host { get; private set; }
    private Window? _popupWindow;

    public App()
    {
        try
        {
            this.InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient<IOnlineEngine, GoogleOnlineEngine>(client =>
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                    });

                    services.AddSingleton<IOfflineEngine, TrieOfflineEngine>();
                    services.AddSingleton<ITransliterationEngine, HybridTransliterationEngine>();
                    services.AddSingleton<IKeyboardHookService, Winmozhi.Hooks.KeyboardHookService>();
                    services.AddSingleton<IHistoryDatabase, MockHistoryDatabase>();
                    services.AddSingleton<Winmozhi.UI.ViewModels.PopupViewModel>();
                    services.AddSingleton<Winmozhi.UI.Views.PopupView>();
                })
                .Build();
        }
        catch (Exception ex)
        {
            WriteCrashLog("Constructor_Crash", ex.ToString());
        }
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            if (Host != null)
            {
                await Host.StartAsync();

                // ONLY show the window. Don't start hooks yet to isolate the bug.
                _popupWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.PopupView>();
                _popupWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            WriteCrashLog("OnLaunched_Crash", ex.ToString());
        }
    }

    private void WriteCrashLog(string fileName, string error)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string errorFile = System.IO.Path.Combine(desktopPath, $"{fileName}.txt");
        System.IO.File.WriteAllText(errorFile, error);
    }
}