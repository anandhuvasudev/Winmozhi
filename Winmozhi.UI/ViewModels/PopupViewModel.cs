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

public partial class PopupViewModel : ObservableObject
{
    private readonly ITransliterationEngine _engine;
    private readonly IKeyboardHookService _hookService;
    private readonly IHistoryDatabase _historyDatabase;
    private readonly DispatcherQueue _dispatcher;
    private int _suggestionRequestVersion;

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
        _dispatcher.TryEnqueue(() =>
        {
            CurrentManglish = word;
            if (string.IsNullOrWhiteSpace(word))
            {
                Suggestions.Clear();
                IsVisible = false;
                _hookService.IsPopupVisible = false;
                Winmozhi.Core.Utilities.MemoryOptimizer.TrimMemory();
            }
        });

        if (string.IsNullOrWhiteSpace(word))
        {
            Interlocked.Increment(ref _suggestionRequestVersion);
            return;
        }

        var requestVersion = Interlocked.Increment(ref _suggestionRequestVersion);

        try
        {
            var instantResults = (await _engine.GetInstantSuggestionsAsync(word, CancellationToken.None).ConfigureAwait(false)).ToList();
            if (requestVersion != _suggestionRequestVersion) return;

            _dispatcher.TryEnqueue(() =>
            {
                if (requestVersion != _suggestionRequestVersion) return;
                ApplySuggestions(instantResults, preserveSelection: false);
            });

            await Task.Delay(150).ConfigureAwait(false);
            if (requestVersion != _suggestionRequestVersion) return;

            var onlineResults = (await _engine.GetOnlineSuggestionsAsync(word, CancellationToken.None).ConfigureAwait(false)).ToList();
            if (requestVersion != _suggestionRequestVersion || onlineResults.Count == 0) return;

            var merged = MergeSuggestions(instantResults, onlineResults);
            _dispatcher.TryEnqueue(() =>
            {
                if (requestVersion != _suggestionRequestVersion) return;
                ApplySuggestions(merged, preserveSelection: true);
            });
        }
        catch (OperationCanceledException) { }
    }

    private void HookService_OnInsertRequested(object? _, string trailingText)
    {
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

    private static List<string> MergeSuggestions(List<string> instant, List<string> online)
    {
        string originalManglish = instant.Last();
        var result = new List<string>();

        if (instant.Count > 1) result.Add(instant[0]);

        foreach (var word in online)
        {
            if (result.Count >= 4) break;
            if (!result.Contains(word, StringComparer.OrdinalIgnoreCase)) result.Add(word);
        }

        if (!result.Contains(originalManglish, StringComparer.OrdinalIgnoreCase)) result.Add(originalManglish);
        return result;
    }

    private void ApplySuggestions(IList<string> suggestions, bool preserveSelection)
    {
        string? highlighted = preserveSelection && SelectedIndex >= 0 && SelectedIndex < Suggestions.Count
            ? Suggestions[SelectedIndex] : null;

        Suggestions.Clear();
        foreach (var s in suggestions) Suggestions.Add(s);

        int restoredIndex = highlighted is not null ? Suggestions.IndexOf(highlighted) : -1;
        SelectedIndex = restoredIndex >= 0 ? restoredIndex : 0;
        IsVisible = Suggestions.Count > 0;
        _hookService.IsPopupVisible = IsVisible;
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
