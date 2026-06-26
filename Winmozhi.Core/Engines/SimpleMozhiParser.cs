using System;
using System.Collections.Generic;
using System.Text;

namespace Winmozhi.Core.Engines;

public static class SimpleMozhiParser
{
    private static readonly (string Key, string Ind, string Dep)[] Vowels = [
        ("au", "ഔ", "ൌ"), ("ou", "ഔ", "ൌ"), ("ai", "ഐ", "ൈ"), ("ei", "ഏ", "േ"),
        ("aa", "ആ", "ാ"), ("ee", "ഈ", "ീ"), ("oo", "ഊ", "ൂ"), ("ii", "ഈ", "ീ"), ("uu", "ഊ", "ൂ"),
        ("ae", "എ", "െ"), ("ea", "ഈ", "ീ"),
        ("a", "അ", ""), ("i", "ഇ", "ി"), ("u", "ഉ", "ു"), ("e", "എ", "െ"), ("E", "ഏ", "േ"),
        ("o", "ഒ", "ൊ"), ("O", "ഓ", "ോ"), ("I", "ഐ", "ൈ"), ("A", "ആ", "ാ")
    ];

    private static readonly (string Key, string Value)[] Consonants = [
        ("nthr", "ന്ത്ര"), ("sthr", "സ്ത്ര"), ("njnj", "ഞ്ഞ"), ("nngg", "ങ്ങ"),
        ("ksh", "ക്ഷ"), ("sch", "ശ്ച"), ("sth", "സ്ഥ"), ("ndr", "ന്ദ്ര"),
        ("chh", "ഛ"), ("tth", "ത്ത"), ("thh", "ഥ"), ("chch", "ച്ച"),
        ("nch", "ഞ്ച"), ("nth", "ന്ത"), ("mpr", "മ്പ്ര"), ("ndw", "ന്ത്വ"),
        ("kk", "ക്ക"), ("gg", "ഗ്ഗ"), ("ng", "ങ്ങ"), ("cc", "ച്ച"), ("ch", "ച"),
        ("jj", "ജ്ജ"), ("nj", "ഞ"), ("TT", "ട്ട"), ("DD", "ഡ്ഡ"), ("NN", "ണ്ണ"),
        ("tt", "ട്ട"), ("th", "ത"), ("dh", "ധ"), ("nn", "ന്ന"), ("pp", "പ്പ"),
        ("bb", "ബ്ബ"), ("mm", "മ്മ"), ("yy", "യ്യ"), ("ll", "ല്ല"), ("vv", "വ്വ"),
        ("ww", "വ്വ"), ("sh", "ശ"), ("ss", "സ്സ"), ("LL", "ള്ള"), ("rr", "റ്റ"),
        ("zh", "ഴ"), ("ph", "ഫ"), ("bh", "ഭ"), ("kh", "ഖ"), ("gh", "ഘ"),
        ("jh", "ഝ"), ("nd", "ണ്ട"), ("nt", "ന്റ"), ("mb", "മ്പ"), ("mp", "മ്പ"), ("nk", "ങ്ക"),
        ("k", "ക"), ("g", "ഗ"), ("j", "ജ"), ("T", "ട"), ("D", "ഡ"), ("N", "ണ"),
        ("t", "ത"), ("d", "ദ"), ("n", "ന"), ("p", "പ"), ("b", "ബ"), ("m", "മ"),
        ("y", "യ"), ("r", "ര"), ("l", "ല"), ("v", "വ"), ("w", "വ"), ("s", "സ"),
        ("S", "ഷ"), ("h", "ഹ"), ("L", "ള"), ("R", "റ"), ("z", "സ"), ("c", "ക"),
        ("q", "ക്യൂ"), ("x", "ക്സ്"), ("f", "ഫ")
    ];

    private static readonly Dictionary<string, string> Chillus = new(StringComparer.Ordinal) {
        {"m", "ം"}, {"n", "ൻ"}, {"N", "ൺ"}, {"r", "ർ"}, {"l", "ൽ"}, {"L", "ൾ"}
    };

    public static string Parse(string manglish)
    {
        if (string.IsNullOrWhiteSpace(manglish)) return string.Empty;

        var result = new StringBuilder(manglish.Length * 2);
        var span = manglish.AsSpan();
        int i = 0;
        bool lastWasConsonant = false;

        while (i < span.Length)
        {
            var remaining = span[i..]; // IDE0057 Fix: Range slicing instead of .Slice()
            bool matched = false;

            foreach (var (Key, Ind, Dep) in Vowels)
            {
                if (remaining.StartsWith(Key.AsSpan(), StringComparison.Ordinal))
                {
                    result.Append(lastWasConsonant ? Dep : Ind);
                    lastWasConsonant = false;
                    i += Key.Length;
                    matched = true;
                    break;
                }
            }
            if (matched) continue;

            foreach (var (Key, Value) in Consonants)
            {
                if (remaining.StartsWith(Key.AsSpan(), StringComparison.Ordinal))
                {
                    bool isLastCharInWord = (i + Key.Length == span.Length);

                    // IDE0057 Fix: Range slicing 
                    var upcomingText = isLastCharInWord ? ReadOnlySpan<char>.Empty : span[(i + Key.Length)..];

                    bool nextIsVowel = !isLastCharInWord && IsVowelPrefix(upcomingText);
                    bool nextIsYRLV = !isLastCharInWord && IsYRLV(upcomingText);

                    if (Chillus.TryGetValue(Key, out string? chillu) && (isLastCharInWord || (!nextIsVowel && !nextIsYRLV)))
                    {
                        result.Append(chillu);
                        lastWasConsonant = false;
                    }
                    else
                    {
                        if (lastWasConsonant) result.Append('്');
                        result.Append(Value);
                        lastWasConsonant = true;
                    }

                    i += Key.Length;
                    matched = true;
                    break;
                }
            }
            if (matched) continue;

            if (lastWasConsonant)
            {
                result.Append('്');
                lastWasConsonant = false;
            }
            result.Append(span[i]);
            i++;
        }

        if (lastWasConsonant) result.Append('്');
        return result.ToString();
    }

    private static bool IsVowelPrefix(ReadOnlySpan<char> text)
    {
        foreach (var (Key, _, _) in Vowels)
            if (text.StartsWith(Key.AsSpan(), StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool IsYRLV(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return false;
        char c = text[0];
        return c is 'y' or 'r' or 'l' or 'v' or 'w' or 'R' or 'L';
    }
}