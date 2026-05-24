using Microsoft.Extensions.Logging;
using System.Text.Json;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

// Using C# Primary Constructor for cleaner DI injection
public class GoogleOnlineEngine(HttpClient httpClient, ILogger<GoogleOnlineEngine> logger) : IOnlineEngine
{
    private const string ApiUrl = "https://inputtools.google.com/request?text={0}&itc=ml-t-i0-und&num=5&cp=0&cs=1&ie=utf-8&oe=utf-8";

    public async Task<List<string>> FetchSuggestionsAsync(string manglishText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];

        try
        {
            string url = string.Format(ApiUrl, Uri.EscapeDataString(manglishText));

            // Send request and respect the CancellationToken for Debouncing
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var root = document.RootElement;

            // Ensure API returned SUCCESS
            if (root.GetArrayLength() > 0 && root[0].GetString() == "SUCCESS")
            {
                var transliterations = root[1][0][1];
                var results = new List<string>(transliterations.GetArrayLength());

                foreach (var item in transliterations.EnumerateArray())
                {
                    var word = item.GetString();
                    if (!string.IsNullOrEmpty(word))
                        results.Add(word);
                }

                return results;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during typing (debouncing). Do not log as an error.
            logger.LogTrace("Google API call cancelled for: {Text}", manglishText);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch online suggestions for: {Text}", manglishText);
        }

        return [];
    }
}