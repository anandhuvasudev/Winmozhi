using Microsoft.Extensions.Logging;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

public class HybridTransliterationEngine(
    IOfflineEngine offlineEngine,
    IOnlineEngine onlineEngine,
    IHistoryDatabase historyDatabase, // We will build this in Phase 5
    ILogger<HybridTransliterationEngine> logger) : ITransliterationEngine
{
    public async Task<IEnumerable<string>> GetSuggestionsAsync(string manglishText, CancellationToken cancellationToken)
    {
        var finalSuggestions = new List<string>();

        // 1. Get Offline Dictionary Suggestions (Instantly)
        var offlineResults = offlineEngine.GetSuggestions(manglishText);

        // 2. Get User History / Frequency (Phase 5 - Returns instantly from SQLite)
        var historyResults = await historyDatabase.GetUserSuggestionsAsync(manglishText);

        // Merge local instantly so UI can show SOMETHING immediately
        finalSuggestions.AddRange(historyResults);
        finalSuggestions.AddRange(offlineResults);

        try
        {
            // 3. Fire Google API with a strict 300ms timeout wrapper to avoid UI stutter
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(300));

            var onlineResults = await onlineEngine.FetchSuggestionsAsync(manglishText, timeoutCts.Token);

            // Insert Online results, but History takes absolute precedence
            finalSuggestions.AddRange(onlineResults);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Online engine timed out or was cancelled for: {Text}", manglishText);
        }

        // Return distinct results while maintaining ranked order
        return finalSuggestions.Distinct().Take(5);
    }
}