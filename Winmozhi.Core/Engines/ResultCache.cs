using System.Collections.Concurrent;

namespace Winmozhi.Core.Engines;

/// <summary>
/// Thread-safe cache for transliteration results with automatic expiration.
/// Reduces repeated API calls and improves responsiveness.
/// </summary>
public class ResultCache
{
    private class CacheEntry
    {
        public List<string> Results { get; set; } = [];
        public DateTime ExpiresAt { get; set; }
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _expirationTime;
    private readonly int _maxEntries;

    public ResultCache(int maxEntries = 500, int expirationMinutes = 60)
    {
        _maxEntries = maxEntries;
        _expirationTime = TimeSpan.FromMinutes(expirationMinutes);
    }

    /// <summary>
    /// Try to get cached results for a manglish text.
    /// </summary>
    public bool TryGet(string manglishText, out List<string> results)
    {
        if (string.IsNullOrWhiteSpace(manglishText))
        {
            results = [];
            return false;
        }

        if (_cache.TryGetValue(manglishText.ToLowerInvariant(), out var entry))
        {
            // Check if entry has expired
            if (DateTime.UtcNow < entry.ExpiresAt)
            {
                results = new List<string>(entry.Results);
                return true;
            }
            else
            {
                // Remove expired entry
                _cache.TryRemove(manglishText.ToLowerInvariant(), out _);
            }
        }

        results = [];
        return false;
    }

    /// <summary>
    /// Store results in cache.
    /// </summary>
    public void Set(string manglishText, List<string> results)
    {
        if (string.IsNullOrWhiteSpace(manglishText) || results == null)
            return;

        // Don't cache empty results (allows retry)
        if (results.Count == 0)
            return;

        string key = manglishText.ToLowerInvariant();

        // If cache is getting too large, clear old entries
        if (_cache.Count >= _maxEntries)
        {
            ClearExpiredEntries();
            if (_cache.Count >= _maxEntries * 0.9) // If still over 90% capacity
            {
                _cache.Clear(); // Reset cache
            }
        }

        _cache[key] = new CacheEntry
        {
            Results = new List<string>(results),
            ExpiresAt = DateTime.UtcNow.Add(_expirationTime)
        };
    }

    /// <summary>
    /// Clear all cached results.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Remove expired entries from cache.
    /// </summary>
    private void ClearExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(x => x.Value.ExpiresAt < now)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Get current cache size.
    /// </summary>
    public int Count => _cache.Count;
}
