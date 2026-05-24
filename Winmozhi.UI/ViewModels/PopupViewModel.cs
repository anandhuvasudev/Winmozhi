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
    private readonly IHistoryDatabase _historyDatabase; // ADDED
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    public partial string CurrentManglish { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    public ObservableCollection<string> Suggestions { get; } = [];

    // ADDED IHistoryDatabase to Constructor
    public PopupViewModel(ITransliterationEngine engine, IKeyboardHookService hookService, IHistoryDatabase historyDatabase)
    {
        _engine = engine;
        _hookService = hookService;
        _historyDatabase = historyDatabase;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        CurrentManglish = string.Empty;

        _hookService.OnWordTyped += HookService_OnWordTyped;
        _hookService.OnInsertRequested += HookService_OnInsertRequested;
        _hookService.OnSelectionChangedRequested += HookService_OnSelectionChangedRequested;
    }

    private void HookService_OnSelectionChangedRequested(object? sender, int direction)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (Suggestions.Count == 0) return;

            int newIndex = SelectedIndex + direction;
            if (newIndex < 0) newIndex = Suggestions.Count - 1;
            if (newIndex >= Suggestions.Count) newIndex = 0;

            SelectedIndex = newIndex;
        });
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
        var token = _cts.Token;

        try
        {
            await Task.Delay(150, token); // Debounce
            var results = await _engine.GetSuggestionsAsync(word, token);

            _dispatcher.TryEnqueue(() =>
            {
                Suggestions.Clear();
                foreach (var res in results) Suggestions.Add(res);

                SelectedIndex = 0;
                IsVisible = Suggestions.Count > 0;
                _hookService.IsPopupVisible = IsVisible;
            });
        }
        catch (TaskCanceledException) { }
    }

    private void HookService_OnInsertRequested(object? sender, string e)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (Suggestions.Count > 0 && SelectedIndex >= 0 && SelectedIndex < Suggestions.Count)
            {
                var selectedWord = Suggestions[SelectedIndex];

                // Save preference
                _ = _historyDatabase.UpdateWordFrequencyAsync(CurrentManglish, selectedWord);

                // Inject Word + Trailing Space/Enter!
                _hookService.ReplaceWord(CurrentManglish.Length, selectedWord, e);

                Suggestions.Clear();
                IsVisible = false;
                _hookService.IsPopupVisible = false;
            }
        });
    }
}