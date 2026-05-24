using Microsoft.Extensions.Logging;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

public class HybridTransliterationEngine(
    IOfflineEngine offlineEngine,
    IOnlineEngine onlineEngine,
    IHistoryDatabase historyDatabase,
    ILogger<HybridTransliterationEngine> logger) : ITransliterationEngine
{
    public async Task<IEnumerable<string>> GetSuggestionsAsync(string manglishText, CancellationToken cancellationToken)
    {
        var finalSuggestions = new List<string>();

        var offlineResults = offlineEngine.GetSuggestions(manglishText);
        var historyResults = await historyDatabase.GetUserSuggestionsAsync(manglishText);

        finalSuggestions.AddRange(historyResults);
        finalSuggestions.AddRange(offlineResults);

        try
        {
            // INCREASED TIMEOUT TO 1.5 SECONDS
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(1500));

            var onlineResults = await onlineEngine.FetchSuggestionsAsync(manglishText, timeoutCts.Token);
            finalSuggestions.AddRange(onlineResults);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Online engine timed out or was cancelled for: {Text}", manglishText);
        }

        // FORCE THE UI TO SHOW UP EVEN IF OFFLINE AND GOOGLE FAILS
        if (finalSuggestions.Count == 0)
        {
            finalSuggestions.Add(manglishText);
            finalSuggestions.Add("Google API Timeout");
        }

        return finalSuggestions.Distinct().Take(5);
    }
}