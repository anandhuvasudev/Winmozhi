using System;
using System.Collections.Generic;
using System.Linq;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

public class TrieOfflineEngine : IOfflineEngine
{
    private class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = [];
        public List<string> Suggestions { get; set; } = [];
    }

    private readonly TrieNode _root = new();
    private readonly List<KeyValuePair<string, List<string>>> _flatDictionary = [];

    public void LoadDictionary(IEnumerable<KeyValuePair<string, List<string>>> wordPairs)
    {
        foreach (var pair in wordPairs)
        {
            string key = pair.Key.ToLowerInvariant();
            Insert(key, pair.Value);
            _flatDictionary.Add(new KeyValuePair<string, List<string>>(key, pair.Value));
        }
    }

    private void Insert(string word, List<string> suggestions)
    {
        var current = _root;
        foreach (var ch in word)
        {
            if (!current.Children.TryGetValue(ch, out var nextNode))
            {
                nextNode = new TrieNode();
                current.Children[ch] = nextNode;
            }
            current = nextNode;
        }
        current.Suggestions = suggestions;
    }

    public List<string> GetSuggestions(string manglishText)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];
        string searchKey = manglishText.ToLowerInvariant();

        var results = new List<string>();
        var current = _root;
        bool exactPrefixFound = true;

        foreach (var ch in searchKey)
        {
            if (!current.Children.TryGetValue(ch, out current))
            {
                exactPrefixFound = false;
                break;
            }
        }

        if (exactPrefixFound && current != null)
        {
            results.AddRange(current.Suggestions);
            CollectDescendants(current, results, 5);
        }

        if (results.Count < 3 && searchKey.Length > 2)
        {
            string normalizedSearch = NormalizeManglish(searchKey);

            var fuzzyMatches = _flatDictionary
                .Where(x => !string.IsNullOrEmpty(x.Key)) // Fix CS8602 Null Dereference Protection
                .Select(x => new
                {
                    x.Key, // Fix IDE0037 Member Simplification
                    Suggestions = x.Value,
                    Distance = ComputeLevenshteinDistance(normalizedSearch, NormalizeManglish(x.Key))
                })
                .Where(x => x.Distance <= 2)
                .OrderBy(x => x.Distance)
                .ThenBy(x => Math.Abs(x.Key.Length - searchKey.Length))
                .SelectMany(x => x.Suggestions)
                .Where(x => !results.Contains(x));

            foreach (var match in fuzzyMatches)
            {
                results.Add(match);
                if (results.Count >= 5) break;
            }
        }

        return results;
    }

    // Fix CA1822: Marked method as static for performance optimization
    private static void CollectDescendants(TrieNode node, List<string> results, int max)
    {
        if (results.Count >= max) return;

        foreach (var child in node.Children.Values)
        {
            foreach (var sug in child.Suggestions)
            {
                if (!results.Contains(sug)) results.Add(sug);
                if (results.Count >= max) return;
            }
            CollectDescendants(child, results, max);
        }
    }

    private static string NormalizeManglish(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        return input.Replace("gh", "k")
                    .Replace("g", "k")
                    .Replace("dh", "th")
                    .Replace("d", "th")
                    .Replace("zha", "la")
                    .Replace("zh", "l")
                    .Replace("ee", "i")
                    .Replace("oo", "u")
                    .Replace("njn", "nj");
    }

    private static int ComputeLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}