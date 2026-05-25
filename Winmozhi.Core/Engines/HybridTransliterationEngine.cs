#pragma warning disable CA1873
using Microsoft.Extensions.Logging;
using Winmozhi.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Winmozhi.Core.Engines;

public class HybridTransliterationEngine(
    IOfflineEngine offlineEngine,
    IOnlineEngine onlineEngine,
    IHistoryDatabase historyDatabase,
    ILogger<HybridTransliterationEngine> logger) : ITransliterationEngine
{
    private const int MaxSuggestions = 5;
    private const int OnlineTimeoutMs = 2000;

    public async Task<IEnumerable<string>> GetInstantSuggestionsAsync(
        string manglishText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];

        var historyResults = await historyDatabase.GetUserSuggestionsAsync(manglishText).ConfigureAwait(false);
        var offlineResults = offlineEngine.GetSuggestions(manglishText);
        var algorithmicGuess = SimpleMozhiParser.Parse(manglishText);

        var combined = new List<string>();

        foreach (var word in historyResults)
            if (!combined.Contains(word, StringComparer.OrdinalIgnoreCase))
                combined.Add(word);

        foreach (var word in offlineResults)
            if (!combined.Contains(word, StringComparer.OrdinalIgnoreCase))
                combined.Add(word);

        if (!string.IsNullOrEmpty(algorithmicGuess) && !combined.Contains(algorithmicGuess, StringComparer.OrdinalIgnoreCase))
            combined.Add(algorithmicGuess);

        var results = combined.Take(MaxSuggestions - 1).ToList();

        // ALWAYS append the raw English text as the final option.
        if (!results.Contains(manglishText.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
            results.Add(manglishText.ToLowerInvariant());

        return results;
    }

    public async Task<IEnumerable<string>> GetOnlineSuggestionsAsync(
        string manglishText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];
        if (!Winmozhi.Core.Utilities.LocalPreferences.IsOnlineEngineEnabled) return [];

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
            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error in online engine for: {Text}", manglishText);
            return [];
        }
    }
}