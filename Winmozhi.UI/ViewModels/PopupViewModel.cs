using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.UI.ViewModels;

public partial class PopupViewModel : ObservableObject
{
    private readonly ITransliterationEngine _engine;
    private readonly IKeyboardHookService _hookService;
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _cts;

    // REMOVED the "= string.Empty;" from here
    [ObservableProperty]
    public partial string CurrentManglish { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    public ObservableCollection<string> Suggestions { get; } = [];

    public PopupViewModel(ITransliterationEngine engine, IKeyboardHookService hookService)
    {
        _engine = engine;
        _hookService = hookService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        // ADDED INITIALIZATION HERE
        CurrentManglish = string.Empty;

        // Subscribe to global typing events
        _hookService.OnWordTyped += HookService_OnWordTyped;
        _hookService.OnInsertRequested += HookService_OnInsertRequested;
    }

    private async void HookService_OnWordTyped(object? sender, string word)
    {
        _dispatcher.TryEnqueue(() =>
        {
            CurrentManglish = word;

            if (string.IsNullOrWhiteSpace(word))
            {
                Suggestions.Clear();
                IsVisible = false;
                _hookService.IsPopupVisible = false;
                return;
            }
        });

        if (string.IsNullOrWhiteSpace(word)) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var results = await _engine.GetSuggestionsAsync(word, _cts.Token);

            _dispatcher.TryEnqueue(() =>
            {
                Suggestions.Clear();
                foreach (var res in results)
                {
                    Suggestions.Add(res);
                }

                IsVisible = Suggestions.Count > 0;
                _hookService.IsPopupVisible = IsVisible;
            });
        }
        catch (TaskCanceledException) { /* Ignored (Debouncing) */ }
    }

    private void HookService_OnInsertRequested(object? sender, EventArgs e)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (Suggestions.Count > 0)
            {
                var selectedWord = Suggestions[0];
                _hookService.ReplaceWord(CurrentManglish.Length, selectedWord);

                Suggestions.Clear();
                IsVisible = false;
                _hookService.IsPopupVisible = false;
            }
        });
    }
}