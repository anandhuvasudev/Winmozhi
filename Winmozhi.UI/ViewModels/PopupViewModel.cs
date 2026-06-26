using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.UI.ViewModels;

public partial class PopupViewModel : ObservableObject, IDisposable
{
    private readonly ITransliterationEngine _engine;
    private readonly IKeyboardHookService _hookService;
    private readonly IHistoryDatabase _historyDatabase;
    private readonly DispatcherQueue _dispatcher;

    private CancellationTokenSource? _typingCts;

    [ObservableProperty] public partial string CurrentManglish { get; set; }
    [ObservableProperty] public partial bool IsVisible { get; set; }
    [ObservableProperty] public partial int SelectedIndex { get; set; }

    public ObservableCollection<string> Suggestions { get; } = [];

    public PopupViewModel(
        ITransliterationEngine engine,
        IKeyboardHookService hookService,
        IHistoryDatabase historyDatabase)
    {
        _engine = engine;
        _hookService = hookService;
        _historyDatabase = historyDatabase;

        _dispatcher = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("ViewModel must be constructed on the UI thread.");

        CurrentManglish = string.Empty;

        _hookService.OnWordTyped += HookService_OnWordTyped;
        _hookService.OnInsertRequested += HookService_OnInsertRequested;
        _hookService.OnSelectionChangedRequested += HookService_OnSelectionChangedRequested;
    }

    public void Dispose()
    {
        _hookService.OnWordTyped -= HookService_OnWordTyped;
        _hookService.OnInsertRequested -= HookService_OnInsertRequested;
        _hookService.OnSelectionChangedRequested -= HookService_OnSelectionChangedRequested;

        _typingCts?.Cancel();
        _typingCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void HookService_OnSelectionChangedRequested(object? sender, int direction)
    {
        _dispatcher.TryEnqueue(() =>
        {
            if (Suggestions.Count == 0) return;
            int currentIndex = SelectedIndex;
            if (currentIndex < 0 || currentIndex >= Suggestions.Count) currentIndex = 0;
            int newIndex = currentIndex + direction;

            if (newIndex < 0) newIndex = Suggestions.Count - 1;
            else if (newIndex >= Suggestions.Count) newIndex = 0;
            SelectedIndex = newIndex;
        });
    }

    private async void HookService_OnWordTyped(object? sender, string word)
    {
        // Cancel previous request to stop network spam
        _typingCts?.Cancel();
        _typingCts?.Dispose();
        _typingCts = new CancellationTokenSource();
        var token = _typingCts.Token;

        _dispatcher.TryEnqueue(() =>
        {
            CurrentManglish = word;
            SelectedIndex = 0;

            if (string.IsNullOrWhiteSpace(word))
            {
                Suggestions.Clear();
                if (IsVisible)
                {
                    IsVisible = false;
                    _hookService.IsPopupVisible = false;
                    Winmozhi.Core.Utilities.MemoryOptimizer.TrimMemory();
                }
            }
        });

        if (string.IsNullOrWhiteSpace(word)) return;

        try
        {
            // FIX: Task.Run forces the SQLite Database lookup onto a background thread.
            // This instantly frees the Windows Keyboard Hook, removing all typing lag!
            var instantResults = await Task.Run(async () =>
            {
                return (await _engine.GetInstantSuggestionsAsync(word, token).ConfigureAwait(false)).ToList();
            }, token).ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            _dispatcher.TryEnqueue(() =>
            {
                if (token.IsCancellationRequested) return;
                ApplySuggestions(instantResults, preserveSelection: false);
            });

            // Wait 150ms BEFORE calling the API to prevent network lag
            await Task.Delay(150, token).ConfigureAwait(false);

            // FIX: Run network parsing on background thread
            var onlineResults = await Task.Run(async () =>
            {
                return (await _engine.GetOnlineSuggestionsAsync(word, token).ConfigureAwait(false)).ToList();
            }, token).ConfigureAwait(false);

            if (token.IsCancellationRequested || onlineResults.Count == 0) return;

            var merged = await MergeSuggestionsAsync(instantResults, onlineResults, word);

            _dispatcher.TryEnqueue(() =>
            {
                if (token.IsCancellationRequested) return;
                ApplySuggestions(merged, preserveSelection: true);
            });
        }
        catch (OperationCanceledException) { /* Ignored, user is typing fast */ }
    }

    private void HookService_OnInsertRequested(object? _, string trailingText)
    {
        InsertCurrentSelection(trailingText);
    }

    public void InsertCurrentSelection(string trailingText)
    {
        // FIX: Cancel background tasks instantly when space/enter is pressed so the popup doesn't randomly reappear
        _typingCts?.Cancel();

        string manglish = CurrentManglish;

        _dispatcher.TryEnqueue(() =>
        {
            if (string.IsNullOrWhiteSpace(manglish))
            {
                if (!string.IsNullOrEmpty(trailingText)) _hookService.ReplaceWord(0, string.Empty, trailingText);
                return;
            }

            string malayalamWord;

            if (SelectedIndex >= 0 && SelectedIndex < Suggestions.Count && CurrentManglish == manglish)
            {
                malayalamWord = Suggestions[SelectedIndex];
            }
            else
            {
                malayalamWord = Winmozhi.Core.Engines.SimpleMozhiParser.Parse(manglish);
                if (string.IsNullOrWhiteSpace(malayalamWord)) malayalamWord = manglish;
            }

            _ = _historyDatabase.UpdateWordFrequencyAsync(manglish, malayalamWord);

            bool fmlEnabled = Winmozhi.Core.Utilities.LocalPreferences.IsFmlFontModeEnabled;
            bool mlEnabled = Winmozhi.Core.Utilities.LocalPreferences.IsMlFontModeEnabled;
            string processName = GetForegroundProcessName();
            bool isPhotoshop = processName.Contains("photoshop", StringComparison.OrdinalIgnoreCase);

            if (fmlEnabled || (mlEnabled && isPhotoshop))
                malayalamWord = Winmozhi.Core.Engines.FmlConverter.ConvertToFml(malayalamWord);
            else if (mlEnabled)
                malayalamWord = Winmozhi.Core.Engines.FmlConverter.ConvertToMl(malayalamWord);

            _hookService.ReplaceWord(manglish.Length, malayalamWord, trailingText);

            Suggestions.Clear();
            IsVisible = false;
            _hookService.IsPopupVisible = false;
            CurrentManglish = string.Empty;

            Winmozhi.Core.Utilities.MemoryOptimizer.TrimMemory();
        });
    }

    private static async Task<List<string>> MergeSuggestionsAsync(List<string> instant, List<string> online, string originalManglish)
    {
        var result = new List<string>();

        if (instant.Count > 1) result.Add(instant[0]);

        foreach (var word in online)
        {
            if (result.Count >= 4) break;
            if (!result.Contains(word, StringComparer.OrdinalIgnoreCase)) result.Add(word);
        }

        if (!result.Contains(originalManglish, StringComparer.OrdinalIgnoreCase)) result.Add(originalManglish);

        return await Task.FromResult(result);
    }

    private void ApplySuggestions(IList<string> suggestions, bool preserveSelection)
    {
        // FIX: If the new suggestions are exactly the same as the old ones, DO NOT redraw the UI.
        // This stops the UI from flickering and heavily reduces UI thread lag.
        if (Suggestions.SequenceEqual(suggestions)) return;

        string? highlighted = preserveSelection && SelectedIndex >= 0 && SelectedIndex < Suggestions.Count
            ? Suggestions[SelectedIndex] : null;

        Suggestions.Clear();
        foreach (var s in suggestions) Suggestions.Add(s);

        int restoredIndex = highlighted is not null ? Suggestions.IndexOf(highlighted) : -1;
        SelectedIndex = restoredIndex >= 0 ? restoredIndex : 0;

        bool shouldBeVisible = Suggestions.Count > 0;
        if (IsVisible != shouldBeVisible)
        {
            IsVisible = shouldBeVisible;
            _hookService.IsPopupVisible = shouldBeVisible;
        }
    }

    private static string GetForegroundProcessName()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return string.Empty;

            _ = GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0) return string.Empty;

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}