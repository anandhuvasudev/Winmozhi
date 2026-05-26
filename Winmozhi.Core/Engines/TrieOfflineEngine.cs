using System;
using System.Collections.Generic;
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
        // Fix 1: Deconstruct KeyValuePair into (key, suggestions)
        foreach (var (key, suggestions) in wordPairs)
        {
            string lowerKey = key.ToLowerInvariant();
            Insert(lowerKey, suggestions);
            _flatDictionary.Add(new KeyValuePair<string, List<string>>(lowerKey, suggestions));
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

        var results = new List<string>(10);
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

        // 1. Exact Match via Trie
        if (exactPrefixFound && current != null)
        {
            results.AddRange(current.Suggestions);
            CollectDescendants(current, results, 5);
        }

        // 2. Ultra-Fast Zero-Allocation Fuzzy Match
        if (results.Count < 3 && searchKey.Length > 2)
        {
            string normalizedSearch = NormalizeManglish(searchKey);

            // We use a tuple list instead of LINQ to avoid massive memory allocations on every keystroke
            var candidates = new List<(int Distance, int LengthDiff, List<string> Suggestions)>();

            // Fix 2: Deconstruct KeyValuePair into (key, suggestions)
            foreach (var (key, suggestions) in _flatDictionary)
            {
                if (string.IsNullOrEmpty(key)) continue;

                int dist = ComputeLevenshteinDistance(normalizedSearch.AsSpan(), NormalizeManglish(key).AsSpan());

                if (dist <= 2)
                {
                    candidates.Add((dist, Math.Abs(key.Length - searchKey.Length), suggestions));
                }
            }

            // High-speed inline sort
            candidates.Sort((a, b) =>
            {
                int cmp = a.Distance.CompareTo(b.Distance);
                if (cmp != 0) return cmp;
                return a.LengthDiff.CompareTo(b.LengthDiff);
            });

            // Fix 3 (Line 96): Deconstruct Tuple into (_, _, suggestions) since Distance/LengthDiff are unused here
            foreach (var (_, _, suggestions) in candidates)
            {
                foreach (var sug in suggestions)
                {
                    if (!results.Contains(sug)) results.Add(sug);
                    if (results.Count >= 5) goto EndFuzzySearch; // Fast exit
                }
            }
        }

    EndFuzzySearch:
        return results;
    }

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

    /// <summary>
    /// Computes string distance with ZERO heap allocations by utilizing stack memory (stackalloc).
    /// This prevents the Garbage Collector from stuttering the user's keyboard.
    /// </summary>
    private static int ComputeLevenshteinDistance(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        if (s.Length == 0) return t.Length;
        if (t.Length == 0) return s.Length;

        // Allocate memory directly on the CPU stack. Extremely fast, zero garbage.
        Span<int> v0 = stackalloc int[t.Length + 1];
        Span<int> v1 = stackalloc int[t.Length + 1];

        for (int i = 0; i < v0.Length; i++) v0[i] = i;

        for (int i = 0; i < s.Length; i++)
        {
            v1[0] = i + 1;
            for (int j = 0; j < t.Length; j++)
            {
                int cost = (s[i] == t[j]) ? 0 : 1;
                v1[j + 1] = Math.Min(Math.Min(v1[j] + 1, v0[j + 1] + 1), v0[j] + cost);
            }
            for (int j = 0; j < v0.Length; j++) v0[j] = v1[j];
        }

        return v1[t.Length];
    }
}