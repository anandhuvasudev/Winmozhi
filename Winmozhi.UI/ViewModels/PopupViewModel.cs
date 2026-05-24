using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.UI.ViewModels;

public partial class PopupViewModel : ObservableObject
{
    private readonly ITransliterationEngine _engine;
    private readonly IKeyboardHookService _hookService;
    private readonly IHistoryDatabase _historyDatabase;
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    public partial string CurrentManglish { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    public ObservableCollection<string> Suggestions { get; } = [];

    public PopupViewModel(
        ITransliterationEngine engine,
        IKeyboardHookService hookService,
        IHistoryDatabase historyDatabase)
    {
        _engine = engine;
        _hookService = hookService;
        _historyDatabase = historyDatabase;

        // GetForCurrentThread() must be called on the UI thread.
        // Throwing here is intentional: a null dispatcher would cause silent
        // TryEnqueue failures that are extremely hard to diagnose.
        _dispatcher = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException(
                $"{nameof(PopupViewModel)} must be constructed on the UI thread. " +
                "Ensure it is resolved from the DI container inside OnLaunched.");

        CurrentManglish = string.Empty;

        _hookService.OnWordTyped += HookService_OnWordTyped;
        _hookService.OnInsertRequested += HookService_OnInsertRequested;
        _hookService.OnSelectionChangedRequested += HookService_OnSelectionChangedRequested;
    }

    // ── Suggestion Navigation ─────────────────────────────────────────────────────

    private void HookService_OnSelectionChangedRequested(object? sender, int direction)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (Suggestions.Count == 0) return;
            int newIndex = SelectedIndex + direction;
            if (newIndex < 0) newIndex = Suggestions.Count - 1;
            else if (newIndex >= Suggestions.Count) newIndex = 0;
            SelectedIndex = newIndex;
        });
    }

    // ── Word Typed — Two-Phase Progressive Loading ────────────────────────────────
    //
    // THE FIX FOR "Tab/Enter doesn't work":
    //
    // The original code awaited the full GetSuggestionsAsync (which includes the
    // Google API call, up to 1500 ms) before showing the popup or setting
    // IsPopupVisible = true.  If the user pressed Tab before that completed,
    // IsPopupVisible was still false on the hook thread, so Tab fell through
    // without triggering OnInsertRequested.
    //
    // The new design:
    //   Phase 1 — After the 150 ms debounce, fetch history + offline results.
    //             These complete in < 5 ms. The popup appears and IsPopupVisible
    //             is set to true within ~155 ms of the last keystroke.
    //   Phase 2 — The Google API call runs concurrently. When it resolves, the
    //             popup is updated with merged results without disturbing the
    //             user's current selection.
    //
    // Because the popup is now visible (and IsPopupVisible = true) before the
    // user can realistically press Tab, the hook intercepts Tab correctly and
    // the replacement works.

    // --- PopupViewModel.cs (Updated HookService_OnWordTyped method) ---

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
            }
        });

        if (string.IsNullOrWhiteSpace(word)) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            // ---> FIX: PHASE 1 (INSTANT) HAPPENS IMMEDIATELY, NO DELAY <---
            var instantResults = (await _engine
                .GetInstantSuggestionsAsync(word, token)
                .ConfigureAwait(false))
                .ToList();

            _dispatcher.TryEnqueue(() =>
            {
                if (token.IsCancellationRequested) return;
                ApplySuggestions(instantResults, preserveSelection: false);
            });

            // ---> FIX: DEBOUNCE ONLY THE NETWORK CALL <---
            await Task.Delay(150, token).ConfigureAwait(false);

            // Phase 2: Google results (non-blocking)
            var onlineResults = (await _engine
                .GetOnlineSuggestionsAsync(word, token)
                .ConfigureAwait(false))
                .ToList();

            if (onlineResults.Count == 0) return;

            var merged = MergeSuggestions(instantResults, onlineResults);

            _dispatcher.TryEnqueue(() =>
            {
                if (token.IsCancellationRequested) return;
                ApplySuggestions(merged, preserveSelection: true);
            });
        }
        catch (OperationCanceledException) { /* Ignored */ }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PopupViewModel] {ex}");
        }
    }

    // ── Insert Requested (Tab / Space / Enter) ────────────────────────────────────

    private void HookService_OnInsertRequested(object? sender, string trailingText)
    {
        _dispatcher.TryEnqueue(() =>
        {
            // Guard: if somehow the popup was dismissed between the key press and
            // this lambda running, do nothing.
            if (Suggestions.Count == 0
                || SelectedIndex < 0
                || SelectedIndex >= Suggestions.Count)
            {
                return;
            }

            var selectedWord = Suggestions[SelectedIndex];
            var manglish = CurrentManglish;

            // Persist preference asynchronously. Fire-and-forget is intentional;
            // a write failure must never block or crash the UI.
            _ = _historyDatabase.UpdateWordFrequencyAsync(manglish, selectedWord);

            // Replace the Manglish text in the active app with the Malayalam word
            _hookService.ReplaceWord(manglish.Length, selectedWord, trailingText);

            // Dismiss the popup immediately
            Suggestions.Clear();
            IsVisible = false;
            _hookService.IsPopupVisible = false;
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Merges instant (history + offline) and online (Google) lists.
    /// History words always lead; Google fills the remaining slots up to 5.
    /// </summary>
    private static List<string> MergeSuggestions(
        IList<string> instant, IList<string> online)
    {
        var result = new List<string>(instant);
        foreach (var word in online)
        {
            if (result.Count >= 5) break;
            if (!result.Contains(word, StringComparer.OrdinalIgnoreCase))
                result.Add(word);
        }
        return result;
    }

    /// <summary>
    /// Updates the Suggestions collection and popup visibility.
    /// When <paramref name="preserveSelection"/> is true, attempts to keep the
    /// currently highlighted item selected after a Google update.
    /// </summary>
    private void ApplySuggestions(IList<string> suggestions, bool preserveSelection)
    {
        string? highlighted = preserveSelection
            && SelectedIndex >= 0
            && SelectedIndex < Suggestions.Count
            ? Suggestions[SelectedIndex]
            : null;

        Suggestions.Clear();
        foreach (var s in suggestions) Suggestions.Add(s);

        int restoredIndex = highlighted is not null
            ? Suggestions.IndexOf(highlighted)
            : -1;

        SelectedIndex = restoredIndex >= 0 ? restoredIndex : 0;
        IsVisible = Suggestions.Count > 0;
        _hookService.IsPopupVisible = IsVisible;
    }
}