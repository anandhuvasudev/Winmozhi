#pragma warning disable CA1873
using Microsoft.Extensions.Logging;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

/// <summary>
/// Hybrid transliteration engine combining history, offline dictionary, algorithmic fallback,
/// and online (Google) suggestions. Results are merged intelligently with proper prioritization.
/// </summary>
public class HybridTransliterationEngine(
    IOfflineEngine offlineEngine,
    IOnlineEngine onlineEngine,
    IHistoryDatabase historyDatabase,
    ILogger<HybridTransliterationEngine> logger) : ITransliterationEngine
{
    private const int MaxSuggestions = 5;

    // 2000ms timeout allows sufficient time for network requests including retries.
    // Google API needs time for DNS lookup, connection establishment, and response.
    // Debounce at 150ms ensures user doesn't see lag from this timeout.
    private const int OnlineTimeoutMs = 2000;

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetInstantSuggestionsAsync(
        string manglishText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];

        // 1. History (User's prior choices) - HIGHEST PRIORITY
        var historyResults = await historyDatabase.GetUserSuggestionsAsync(manglishText).ConfigureAwait(false);

        // 2. Offline Trie Engine (Exact dictionary matches)
        var offlineResults = offlineEngine.GetSuggestions(manglishText);

        // 3. Algorithmic Fallback Engine (Guesses words mathematically)
        var algorithmicGuess = SimpleMozhiParser.Parse(manglishText);

        // Combine intelligently: History > Offline > Algorithm
        // Use LinkedHashSet-like behavior to preserve order and avoid duplicates
        var combined = new List<string>();

        // Add history results first (most reliable user preferences)
        foreach (var word in historyResults)
        {
            if (!combined.Contains(word, StringComparer.OrdinalIgnoreCase))
                combined.Add(word);
        }

        // Add offline results (exact matches from dictionary)
        foreach (var word in offlineResults)
        {
            if (!combined.Contains(word, StringComparer.OrdinalIgnoreCase))
                combined.Add(word);
        }

        // Add algorithmic guess only if we don't already have good matches
        if (!string.IsNullOrEmpty(algorithmicGuess) && !combined.Contains(algorithmicGuess, StringComparer.OrdinalIgnoreCase))
        {
            combined.Add(algorithmicGuess);
        }

        return combined.Take(MaxSuggestions);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetOnlineSuggestionsAsync(
        string manglishText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(OnlineTimeoutMs));

            var onlineResults = await onlineEngine
                .FetchSuggestionsAsync(manglishText, timeoutCts.Token)
                .ConfigureAwait(false);

            if (onlineResults.Count > 0)
            {
                logger.LogTrace("Online engine returned {Count} suggestions for: {Text}", onlineResults.Count, manglishText);
            }

            return onlineResults;
        }
        catch (OperationCanceledException)
        {
            // A new keystroke cancelled us, or the network timed out. Both are expected.
            logger.LogTrace("Online engine cancelled/timed out for: {Text}", manglishText);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error in online engine for: {Text}", manglishText);
            return [];
        }
    }
}