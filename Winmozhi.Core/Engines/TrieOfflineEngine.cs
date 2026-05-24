using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

public class TrieOfflineEngine : IOfflineEngine
{
    private class TrieNode
    {
        // Dictionary for O(1) child lookup. 
        public Dictionary<char, TrieNode> Children { get; } = [];
        public List<string> Suggestions { get; set; } = [];
    }

    private readonly TrieNode _root = new();

    public void LoadDictionary(IEnumerable<KeyValuePair<string, List<string>>> wordPairs)
    {
        foreach (var pair in wordPairs)
        {
            Insert(pair.Key.ToLowerInvariant(), pair.Value);
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
        // At the end of the English prefix, store the Malayalam equivalents
        current.Suggestions = suggestions;
    }

    public List<string> GetSuggestions(string manglishText)
    {
        if (string.IsNullOrWhiteSpace(manglishText)) return [];

        var current = _root;
        foreach (var ch in manglishText.ToLowerInvariant())
        {
            if (!current.Children.TryGetValue(ch, out current))
            {
                // Fallback: Phase 5 we will add algorithm-based mapping here if Trie misses
                return [];
            }
        }

        return current.Suggestions;
    }
}