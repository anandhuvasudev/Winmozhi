using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

[Table("Words")]
public class WordEntry
{
    [Column("Manglish")]
    public string Manglish { get; set; } = string.Empty;

    [Column("Malayalam")]
    public string Malayalam { get; set; } = string.Empty;

    [Column("Frequency")]
    public int Frequency { get; set; }
}

public class TrieOfflineEngine : IOfflineEngine
{
    private readonly SQLiteConnection _db;
    private readonly Trie _l1Cache = new();

    public TrieOfflineEngine()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Winmozhi");
        Directory.CreateDirectory(appData);
        string dbPath = Path.Combine(appData, "OfflineCorpus.db");

        if (!File.Exists(dbPath))
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Winmozhi.Core.Engines.OfflineCorpus.db");
            if (stream != null)
            {
                using var fileStream = File.Create(dbPath);
                stream.CopyTo(fileStream);
            }
        }

        _db = new SQLiteConnection(dbPath);
        LoadTopWordsIntoRam();
    }

    private void LoadTopWordsIntoRam()
    {
        try
        {
            var topWords = _db.Query<WordEntry>("SELECT * FROM Words ORDER BY Frequency DESC LIMIT 2000");
            foreach (var word in topWords)
            {
                _l1Cache.Insert(word.Manglish.ToLowerInvariant(), word.Malayalam);
            }
        }
        catch { /* Failsafe */ }
    }

    public void LoadDictionary(IEnumerable<KeyValuePair<string, List<string>>> starterWords)
    {
        foreach (var kvp in starterWords)
        {
            foreach (var malayalamWord in kvp.Value)
            {
                _l1Cache.Insert(kvp.Key.ToLowerInvariant(), malayalamWord);
            }
        }
    }

    // ========================================================================
    // ADVANCED ALGORITHM: MANGLISH TYPO-TOLERANCE & PHONETIC MATCHING
    // ========================================================================
    private static List<string> GetManglishVariations(string text)
    {
        // HashSet prevents us from querying the same word twice
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { text };

        // 1. Remove duplicate letters. (njann -> njan, njaan -> njan, kku -> ku)
        string deduped = Regex.Replace(text, @"(.)\1+", "$1");
        results.Add(deduped);

        // 2. Expand common vowels (If user typed 'njan', they might mean 'njaan' in DB)
        results.Add(text.Replace("a", "aa"));
        results.Add(text.Replace("i", "ee"));
        results.Add(text.Replace("u", "oo"));

        // 3. Handle common Manglish spelling swaps
        results.Add(text.Replace("ny", "nj"));
        results.Add(deduped.Replace("ny", "nj"));

        results.Add(text.Replace("v", "w"));
        results.Add(deduped.Replace("v", "w"));

        results.Add(text.Replace("w", "v"));
        results.Add(deduped.Replace("w", "v"));

        results.Add(text.Replace("z", "zh"));
        results.Add(deduped.Replace("z", "zh"));

        // Only return valid, non-empty variations
        return results.Where(r => r.Length > 0).ToList();
    }

    public List<string> GetSuggestions(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return [];
        prefix = prefix.ToLowerInvariant();

        var results = new List<string>();

        // Generate all possible variations of what the user might have meant
        var variations = GetManglishVariations(prefix);

        // STEP 1: Check L1 RAM Cache for all variations
        foreach (var variation in variations)
        {
            var l1Results = _l1Cache.Search(variation).Take(5);
            foreach (var res in l1Results)
            {
                if (!results.Contains(res)) results.Add(res);
            }
            if (results.Count >= 5) return results.Take(5).ToList();
        }

        // STEP 2: Check L2 SQLite Database using ALL fuzzy variations at once
        try
        {
            // Dynamically build a SQL query: 
            // SELECT * FROM Words WHERE Manglish LIKE 'njaan%' OR Manglish LIKE 'njan%' OR Manglish LIKE 'njann%'
            var queryBuilder = new System.Text.StringBuilder("SELECT * FROM Words WHERE ");
            var parameters = new List<object>();

            for (int i = 0; i < variations.Count; i++)
            {
                queryBuilder.Append(i == 0 ? "Manglish LIKE ?" : " OR Manglish LIKE ?");
                parameters.Add(variations[i] + "%");
            }

            // Order by highest frequency overall
            queryBuilder.Append(" ORDER BY Frequency DESC LIMIT 10");

            var dbResults = _db.Query<WordEntry>(queryBuilder.ToString(), parameters.ToArray());

            foreach (var entry in dbResults)
            {
                if (!results.Contains(entry.Malayalam))
                {
                    results.Add(entry.Malayalam);
                    if (results.Count >= 5) break;
                }
            }
        }
        catch { /* Failsafe */ }

        return results.Take(5).ToList();
    }
}

public class Trie
{
    private class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = [];
        public List<string> MalayalamWords { get; } = [];
    }

    private readonly TrieNode _root = new();

    public void Insert(string manglish, string malayalam)
    {
        var node = _root;
        foreach (char c in manglish)
        {
            if (!node.Children.TryGetValue(c, out var nextNode))
            {
                nextNode = new TrieNode();
                node.Children[c] = nextNode;
            }
            node = nextNode;
        }
        if (!node.MalayalamWords.Contains(malayalam))
        {
            node.MalayalamWords.Add(malayalam);
        }
    }

    public List<string> Search(string prefix)
    {
        var node = _root;
        foreach (char c in prefix)
        {
            if (!node.Children.TryGetValue(c, out node)) return [];
        }
        return GetWordsFromNode(node);
    }

    private static List<string> GetWordsFromNode(TrieNode node)
    {
        var results = new List<string>(node.MalayalamWords);
        foreach (var child in node.Children.Values)
        {
            results.AddRange(GetWordsFromNode(child));
        }
        return results;
    }
}