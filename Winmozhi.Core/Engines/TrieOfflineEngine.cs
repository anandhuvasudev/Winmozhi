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
    [Column("manglish")]
    public string Manglish { get; set; } = string.Empty;

    [Column("malayalam")]
    public string Malayalam { get; set; } = string.Empty;

    [Column("Frequency")]
    public int Frequency { get; set; }
}

public partial class TrieOfflineEngine : IOfflineEngine
{
    private readonly SQLiteConnection? _db;
    private readonly Trie _l1Cache = new();
    private const int MaxSuggestions = 7;

    // SYSLIB1045 Fix: Zero-Allocation Source Generated Regexes
    [GeneratedRegex(@"(.)\1+")] private static partial Regex DuplicateLetterRegex();
    [GeneratedRegex(@"(?<=[a-z])k")] private static partial Regex MidKRegex();
    [GeneratedRegex(@"(?<=[a-z])t")] private static partial Regex MidTRegex();
    [GeneratedRegex(@"(?<=[a-z])p")] private static partial Regex MidPRegex();
    [GeneratedRegex(@"(?<=[a-z])l")] private static partial Regex MidLRegex();
    [GeneratedRegex(@"(?<=[a-z])m")] private static partial Regex MidMRegex();
    [GeneratedRegex(@"(?<=[a-z])n")] private static partial Regex MidNRegex();

    public TrieOfflineEngine()
    {
        try
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Winmozhi");
            Directory.CreateDirectory(appData);
            string dbPath = Path.Combine(appData, "OfflineCorpus.db");

            if (File.Exists(dbPath) && new FileInfo(dbPath).Length < 1024)
            {
                File.Delete(dbPath);
            }

            if (!File.Exists(dbPath))
            {
                var assembly = typeof(TrieOfflineEngine).Assembly;
                string? resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("OfflineCorpus.db", StringComparison.OrdinalIgnoreCase));

                if (resourceName != null)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var fileStream = File.Create(dbPath);
                        stream.CopyTo(fileStream);
                    }
                }
            }

            if (File.Exists(dbPath))
            {
                _db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
                LoadTopWordsIntoRam();
            }
        }
        catch { _db = null; }
    }

    private void LoadTopWordsIntoRam()
    {
        if (_db == null) return;
        try
        {
            var topWords = _db.Query<WordEntry>("SELECT * FROM Words ORDER BY Frequency DESC LIMIT 2000");
            foreach (var word in topWords)
            {
                _l1Cache.Insert(word.Manglish.ToLowerInvariant(), word.Malayalam);
            }
            _l1Cache.Compact();
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

    private static List<string> GetManglishVariations(string text)
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { text };

        string deduped = DuplicateLetterRegex().Replace(text, "$1");
        results.Add(deduped);

        // CA1845 & IDE0057 Fixes: Used 'text[2..]' instead of 'text.Substring(2)'
        if (text.StartsWith("ae", StringComparison.OrdinalIgnoreCase)) results.Add("e" + text[2..]);
        if (text.StartsWith("ye", StringComparison.OrdinalIgnoreCase)) results.Add("e" + text[2..]);
        if (text.StartsWith("ya", StringComparison.OrdinalIgnoreCase)) results.Add("a" + text[2..]);
        if (text.StartsWith("y", StringComparison.OrdinalIgnoreCase) && text.Length > 1 && !"aeiou".Contains(text[1]))
            results.Add("i" + text[1..]);

        // CA1845 Fix: Used 'text[..^2]' to slice off the last 2 characters allocation-free
        if (text.EndsWith("am", StringComparison.OrdinalIgnoreCase)) results.Add(text[..^2] + "um");
        if (text.EndsWith("um", StringComparison.OrdinalIgnoreCase)) results.Add(text[..^2] + "am");

        results.Add(text.Replace("a", "aa"));
        results.Add(deduped.Replace("a", "aa"));
        results.Add(text.Replace("e", "ee"));
        results.Add(text.Replace("i", "ee"));
        results.Add(text.Replace("u", "oo"));
        results.Add(text.Replace("o", "oo"));

        results.Add(MidKRegex().Replace(text, "kk"));
        results.Add(MidTRegex().Replace(text, "tt"));
        results.Add(MidPRegex().Replace(text, "pp"));
        results.Add(MidLRegex().Replace(text, "ll"));
        results.Add(MidMRegex().Replace(text, "mm"));
        results.Add(MidNRegex().Replace(text, "nn"));

        results.Add(text.Replace("ch", "cch"));
        results.Add(text.Replace("th", "tth"));
        results.Add(text.Replace("ny", "nj"));
        results.Add(deduped.Replace("ny", "nj"));
        results.Add(text.Replace("v", "w"));
        results.Add(text.Replace("w", "v"));
        results.Add(text.Replace("z", "zh"));

        // IDE0305 Fix: Used collection expression [.. ]
        return [.. results.Where(r => r.Length is > 0 and < 25).Take(20)];
    }

    public List<string> GetSuggestions(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return [];
        prefix = prefix.ToLowerInvariant();

        var results = new List<string>();
        string directPhonetic = SimpleMozhiParser.Parse(prefix);
        var variations = GetManglishVariations(prefix);

        foreach (var variation in variations)
        {
            var l1Results = _l1Cache.Search(variation);
            foreach (var res in l1Results)
            {
                if (!results.Contains(res)) results.Add(res);
            }
            if (results.Count >= MaxSuggestions) break;
        }

        if (!string.IsNullOrWhiteSpace(directPhonetic) && !results.Contains(directPhonetic))
        {
            if (results.Count > 0) results.Insert(1, directPhonetic);
            else results.Add(directPhonetic);
        }

        if (_db != null && results.Count < MaxSuggestions)
        {
            try
            {
                var queryBuilder = new System.Text.StringBuilder("SELECT * FROM Words WHERE ");
                var parameters = new List<object>();

                for (int i = 0; i < variations.Count; i++)
                {
                    queryBuilder.Append(i == 0 ? "manglish LIKE ?" : " OR manglish LIKE ?");
                    parameters.Add(variations[i] + "%");
                }

                queryBuilder.Append($" ORDER BY Frequency DESC LIMIT {MaxSuggestions}");

                // IDE0305 Fix: [.. ] instead of .ToArray()
                var dbResults = _db.Query<WordEntry>(queryBuilder.ToString(), [.. parameters]);

                foreach (var entry in dbResults)
                {
                    if (!results.Contains(entry.Malayalam)) results.Add(entry.Malayalam);
                    if (results.Count >= MaxSuggestions) return [.. results.Take(MaxSuggestions)];
                }
            }
            catch { /* Failsafe */ }
        }

        if (_db != null && results.Count < MaxSuggestions && prefix.Length >= 3)
        {
            try
            {
                var consonants = prefix.Where(c => "bcdfghjklmnpqrstvwxyz".Contains(c)).ToArray();
                if (consonants.Length > 0)
                {
                    string fuzzyPattern;

                    // CA1845 Fix: consonants[1..]
                    if ("aeiou".Contains(prefix[0])) fuzzyPattern = "%" + string.Join("%", consonants) + "%";
                    else fuzzyPattern = prefix[0] + "%" + string.Join("%", consonants[1..]) + "%";

                    string fuzzyQuery = "SELECT * FROM Words WHERE manglish LIKE ? ORDER BY Frequency DESC, LENGTH(manglish) ASC LIMIT 10";
                    var fuzzyResults = _db.Query<WordEntry>(fuzzyQuery, fuzzyPattern);

                    foreach (var entry in fuzzyResults)
                    {
                        if (!results.Contains(entry.Malayalam))
                        {
                            results.Add(entry.Malayalam);
                            if (results.Count >= MaxSuggestions) break;
                        }
                    }
                }
            }
            catch { /* Failsafe */ }
        }

        return [.. results.Take(MaxSuggestions)];
    }
}

public class Trie
{
    private class TrieNode
    {
        public KeyValuePair<char, TrieNode>[]? Children;
        public string[]? MalayalamWords;
        public Dictionary<char, TrieNode>? TempChildren;
        public List<string>? TempWords;
    }

    private readonly TrieNode _root = new();

    public void Insert(string manglish, string malayalam)
    {
        var node = _root;
        foreach (char c in manglish)
        {
            node.TempChildren ??= [];
            if (!node.TempChildren.TryGetValue(c, out var nextNode))
            {
                nextNode = new TrieNode();
                node.TempChildren[c] = nextNode;
            }
            node = nextNode;
        }
        node.TempWords ??= [];
        if (!node.TempWords.Contains(malayalam)) node.TempWords.Add(malayalam);
    }

    public void Compact() { CompactNode(_root); }

    // CA1822 Fix: Made this method static
    private static void CompactNode(TrieNode node)
    {
        if (node.TempWords != null)
        {
            node.MalayalamWords = [.. node.TempWords];
            node.TempWords = null;
        }
        if (node.TempChildren != null)
        {
            node.Children = [.. node.TempChildren];
            node.TempChildren = null;
            foreach (var child in node.Children) CompactNode(child.Value);
        }
    }

    public List<string> Search(string prefix)
    {
        var node = _root;
        foreach (char c in prefix)
        {
            if (node.Children == null) return [];
            bool found = false;
            foreach (var child in node.Children)
            {
                if (child.Key == c) { node = child.Value; found = true; break; }
            }
            if (!found) return [];
        }
        var results = new List<string>(7);
        CollectWords(node, results);
        return results;
    }

    private static void CollectWords(TrieNode node, List<string> results)
    {
        if (results.Count >= 7) return;
        if (node.MalayalamWords != null)
        {
            foreach (var word in node.MalayalamWords)
            {
                if (!results.Contains(word)) results.Add(word);
                if (results.Count >= 7) return;
            }
        }
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                CollectWords(child.Value, results);
                if (results.Count >= 7) return;
            }
        }
    }
}