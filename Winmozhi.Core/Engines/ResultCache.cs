using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Winmozhi.Core.Engines;

public class ResultCache(int maxEntries = 500, int expirationMinutes = 60)
{
    private class CacheEntry
    {
        public List<string> Results { get; set; } = [];
        public DateTime ExpiresAt { get; set; }
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _expirationTime = TimeSpan.FromMinutes(expirationMinutes);
    private readonly int _maxEntries = maxEntries;

    public bool TryGet(string manglishText, out List<string> results)
    {
        if (string.IsNullOrWhiteSpace(manglishText))
        {
            results = [];
            return false;
        }

        if (_cache.TryGetValue(manglishText.ToLowerInvariant(), out var entry))
        {
            if (DateTime.UtcNow < entry.ExpiresAt)
            {
                results = [.. entry.Results];
                return true;
            }
            else
            {
                _cache.TryRemove(manglishText.ToLowerInvariant(), out _);
            }
        }

        results = [];
        return false;
    }

    public void Set(string manglishText, List<string> results)
    {
        if (string.IsNullOrWhiteSpace(manglishText) || results == null || results.Count == 0)
            return;

        string key = manglishText.ToLowerInvariant();

        if (_cache.Count >= _maxEntries)
        {
            ClearExpiredEntries();
            if (_cache.Count >= _maxEntries * 0.9)
            {
                _cache.Clear();
            }
        }

        _cache[key] = new CacheEntry
        {
            Results = [.. results],
            ExpiresAt = DateTime.UtcNow.Add(_expirationTime)
        };
    }

    public void Clear() => _cache.Clear();

    private void ClearExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache.Where(x => x.Value.ExpiresAt < now).Select(x => x.Key).ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public int Count => _cache.Count;
}