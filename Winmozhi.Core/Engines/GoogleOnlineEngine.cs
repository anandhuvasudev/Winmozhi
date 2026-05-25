using Microsoft.Extensions.Logging;
using System.Text.Json;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

public class GoogleOnlineEngine(HttpClient httpClient, ILogger<GoogleOnlineEngine> logger) : IOnlineEngine
{
    private const string ApiUrl = "https://inputtools.google.com/request?text={0}&itc=ml-t-i0-und&num=5&cp=0&cs=1&ie=utf-8&oe=utf-8";
    private const int MaxRetries = 2;

    private readonly ResultCache _cache = new(maxEntries: 1000, expirationMinutes: 120);

    public async Task<List<string>> FetchSuggestionsAsync(string manglishText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];

        if (_cache.TryGet(manglishText, out var cachedResults))
        {
            if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Cache hit for: {Text}", manglishText);
            return cachedResults;
        }

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                string url = string.Format(ApiUrl, Uri.EscapeDataString(manglishText));

                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Google API HTTP error {StatusCode} for: {Text}", response.StatusCode, manglishText);
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(100 * (attempt + 1), cancellationToken);
                        continue;
                    }
                    return [];
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var result = ParseJsonResponse(document);
                if (result.Count > 0)
                {
                    _cache.Set(manglishText, result);
                    if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Google API returned {Count} suggestions for: {Text}", result.Count, manglishText);
                    return result;
                }

                if (attempt < MaxRetries)
                {
                    await Task.Delay(50 * (attempt + 1), cancellationToken);
                    continue;
                }

                return [];
            }
            catch (OperationCanceledException)
            {
                if (attempt < MaxRetries && !cancellationToken.IsCancellationRequested)
                {
                    if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Google API timeout on attempt {Attempt}, retrying for: {Text}", attempt + 1, manglishText);
                    await Task.Delay(100, cancellationToken);
                    continue;
                }
                if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("Google API call cancelled for: {Text}", manglishText);
                return [];
            }
            catch (HttpRequestException ex)
            {
                if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "HTTP error on attempt {Attempt} for: {Text}", attempt + 1, manglishText);
                if (attempt < MaxRetries)
                {
                    await Task.Delay(100 * (attempt + 1), cancellationToken);
                    continue;
                }
                return [];
            }
            catch (JsonException ex)
            {
                if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "JSON parsing error for: {Text}", manglishText);
                return [];
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Warning)) logger.LogWarning(ex, "Unexpected error fetching suggestions for: {Text}", manglishText);
                return [];
            }
        }

        return [];
    }

    private static List<string> ParseJsonResponse(JsonDocument document)
    {
        try
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2) return [];

            var firstElement = root[0];
            if (firstElement.ValueKind != JsonValueKind.String || firstElement.GetString() != "SUCCESS") return [];

            var secondElement = root[1];
            if (secondElement.ValueKind != JsonValueKind.Array || secondElement.GetArrayLength() == 0) return [];

            var firstResult = secondElement[0];
            if (firstResult.ValueKind != JsonValueKind.Array || firstResult.GetArrayLength() < 2) return [];

            var transliterations = firstResult[1];
            if (transliterations.ValueKind != JsonValueKind.Array) return [];

            var results = new List<string>();
            foreach (var item in transliterations.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var word = item.GetString();
                    if (!string.IsNullOrWhiteSpace(word) && !results.Contains(word, StringComparer.Ordinal))
                    {
                        results.Add(word);
                    }
                }
            }

            return results;
        }
        catch
        {
            return [];
        }
    }
}