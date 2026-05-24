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
                    services.AddSingleton<IHistoryDatabase, Winmozhi.Core.Engines.SqliteHistoryDatabase>();
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

                var hookService = Host.Services.GetRequiredService<IKeyboardHookService>();
                hookService.StartHook();

                _popupWindow = Host.Services.GetRequiredService<Winmozhi.UI.Views.PopupView>();
                _popupWindow.Activate();
                _popupWindow.AppWindow.Hide();

                // ---> NEW: WARM UP THE ENGINE IN THE BACKGROUND <---
                // This forces .NET to load HTTP, JSON, and Google API handlers into memory 
                // so the user experiences zero lag when they actually start typing.
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var engine = Host.Services.GetRequiredService<ITransliterationEngine>();
                    await engine.GetSuggestionsAsync("a", System.Threading.CancellationToken.None);
                });
            }
        }
        catch (Exception ex)
        {
            WriteCrashLog("OnLaunched_Crash", ex.ToString());
        }
    }

    // Fixed CA1822 Warning by making this static
    private static void WriteCrashLog(string fileName, string error)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string errorFile = System.IO.Path.Combine(desktopPath, $"{fileName}.txt");
        System.IO.File.WriteAllText(errorFile, error);
    }
}