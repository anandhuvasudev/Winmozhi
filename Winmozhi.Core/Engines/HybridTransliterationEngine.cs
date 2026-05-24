using Microsoft.Extensions.Logging;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

public class HybridTransliterationEngine(
    IOfflineEngine offlineEngine,
    IOnlineEngine onlineEngine,
    IHistoryDatabase historyDatabase,
    ILogger<HybridTransliterationEngine> logger) : ITransliterationEngine
{
    private const int MaxSuggestions = 5;

    // 400 ms is enough for most network conditions while not blocking the UI.
    // The caller's CancellationToken (debounce) is linked in, so a new keystroke
    // also cancels this call automatically.
    private const int OnlineTimeoutMs = 400;

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetInstantSuggestionsAsync(
        string manglishText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];

        // 1. History (User's prior choices)
        var historyResults = await historyDatabase.GetUserSuggestionsAsync(manglishText).ConfigureAwait(false);

        // 2. Offline Trie Engine (Exact dictionary matches)
        var offlineResults = offlineEngine.GetSuggestions(manglishText);

        // 3. Algorithmic Fallback Engine (Guesses words mathematically)
        var algorithmicGuess = SimpleMozhiParser.Parse(manglishText);

        // Combine them: History takes top priority, then exact dict, then the algorithm's guess
        var combined = historyResults.Concat(offlineResults).ToList();

        if (!string.IsNullOrEmpty(algorithmicGuess) && !combined.Contains(algorithmicGuess))
        {
            combined.Add(algorithmicGuess);
        }

        return combined.Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxSuggestions);
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

            return await onlineEngine
                .FetchSuggestionsAsync(manglishText, timeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A new keystroke cancelled us, or the network timed out. Both are expected.
            logger.LogTrace("Online engine cancelled/timed out for: {Text}", manglishText);
            return [];
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                logger.LogWarning(ex, "Unexpected error in online engine for: {Text}", manglishText);
            }
            return [];
        }
    }
}