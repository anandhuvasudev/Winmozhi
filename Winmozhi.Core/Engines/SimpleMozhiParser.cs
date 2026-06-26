using System;
using System.Collections.Generic;
using System.Text;

namespace Winmozhi.Core.Engines;

public static class SimpleMozhiParser
{
    // 1. VOWELS (Independent and Dependent/Matra)
    private static readonly (string Key, string Ind, string Dep)[] Vowels = {
        ("au", "ഔ", "ൌ"), ("ou", "ഔ", "ൌ"), ("ai", "ഐ", "ൈ"), ("ei", "ഏ", "േ"),
        ("aa", "ആ", "ാ"), ("ee", "ഈ", "ീ"), ("oo", "ഊ", "ൂ"), ("ii", "ഈ", "ീ"),
        ("uu", "ഊ", "ൂ"),
        ("a", "അ", ""),  // 'a' acts as the inherent vowel remover
        ("i", "ഇ", "ി"), ("u", "ഉ", "ു"), ("e", "എ", "െ"), ("E", "ഏ", "േ"),
        ("o", "ഒ", "ൊ"), ("O", "ഓ", "ോ"), ("I", "ഐ", "ൈ"), ("A", "ആ", "ാ")
    };

    // 2. MASSIVE CONSONANT & CONJUNCT MAP
    private static readonly (string Key, string Value)[] Consonants = {
        // 4-character complex combos
        ("nthr", "ന്ത്ര"), ("sthr", "സ്ത്ര"), ("njnj", "ഞ്ഞ"), ("nngg", "ങ്ങ"),

        // 3-character combos
        ("ksh", "ക്ഷ"), ("sch", "ശ്ച"), ("sth", "സ്ഥ"), ("ndr", "ന്ദ്ര"),
        ("chh", "ഛ"), ("tth", "ത്ത"), ("thh", "ഥ"), ("chch", "ച്ച"),
        ("nch", "ഞ്ച"), ("nth", "ന്ത"), ("mpr", "മ്പ്ര"), ("ndw", "ന്ത്വ"), 

        // 2-character combos
        ("kk", "ക്ക"), ("gg", "ഗ്ഗ"), ("ng", "ങ്ങ"), ("cc", "ച്ച"), ("ch", "ച"),
        ("jj", "ജ്ജ"), ("nj", "ഞ"), ("TT", "ട്ട"), ("DD", "ഡ്ഡ"), ("NN", "ണ്ണ"),
        ("tt", "ട്ട"), ("th", "ത"), ("dh", "ധ"), ("nn", "ന്ന"), ("pp", "പ്പ"),
        ("bb", "ബ്ബ"), ("mm", "മ്മ"), ("yy", "യ്യ"), ("ll", "ല്ല"), ("vv", "വ്വ"),
        ("ww", "വ്വ"), ("sh", "ശ"), ("ss", "സ്സ"), ("LL", "ള്ള"), ("rr", "റ്റ"),
        ("zh", "ഴ"), ("ph", "ഫ"), ("bh", "ഭ"), ("kh", "ഖ"), ("gh", "ഘ"),
        ("jh", "ഝ"), ("nd", "ണ്ട"), ("nt", "ന്റ"), ("mb", "മ്പ"), ("mp", "മ്പ"),
        ("nk", "ങ്ക"),

        // 1-character base letters
        ("k", "ക"), ("g", "ഗ"), ("j", "ജ"), ("T", "ട"), ("D", "ഡ"), ("N", "ണ"),
        ("t", "ത"), ("d", "ദ"), ("n", "ന"), ("p", "പ"), ("b", "ബ"), ("m", "മ"),
        ("y", "യ"), ("r", "ര"), ("l", "ല"), ("v", "വ"), ("w", "വ"), ("s", "സ"),
        ("S", "ഷ"), ("h", "ഹ"), ("L", "ള"), ("R", "റ"), ("z", "സ"), ("c", "ക"),
        ("q", "ക്യൂ"), ("x", "ക്സ്"), ("f", "ഫ")
    };

    // 3. CHILLUS & ANUSVARAM MAP
    private static readonly Dictionary<string, string> Chillus = new(StringComparer.Ordinal)
    {
        {"m", "ം"}, {"n", "ൻ"}, {"N", "ൺ"}, {"r", "ർ"}, {"l", "ൽ"}, {"L", "ൾ"}
    };

    public static string Parse(string manglish)
    {
        if (string.IsNullOrWhiteSpace(manglish)) return string.Empty;

        var result = new StringBuilder();
        int i = 0;
        bool lastWasConsonant = false;

        while (i < manglish.Length)
        {
            int remaining = manglish.Length - i;
            bool matched = false;

            // STEP 1: Attempt to match Vowels
            foreach (var vowel in Vowels)
            {
                if (remaining >= vowel.Key.Length && manglish.Substring(i, vowel.Key.Length) == vowel.Key)
                {
                    result.Append(lastWasConsonant ? vowel.Dep : vowel.Ind);
                    lastWasConsonant = false;
                    i += vowel.Key.Length;
                    matched = true;
                    break;
                }
            }
            if (matched) continue;

            // STEP 2: Attempt to match Consonants
            foreach (var cons in Consonants)
            {
                if (remaining >= cons.Key.Length && manglish.Substring(i, cons.Key.Length) == cons.Key)
                {
                    bool isLastCharInWord = (i + cons.Key.Length == manglish.Length);
                    string upcomingText = isLastCharInWord ? "" : manglish.Substring(i + cons.Key.Length);

                    bool nextIsVowel = !isLastCharInWord && IsVowelPrefix(upcomingText);
                    bool nextIsYRLV = !isLastCharInWord && IsYRLV(upcomingText);

                    // ADVANCED GRAMMAR RULE: Mid-Word Chillus
                    // If it is a Chillu letter (r, l, L, m, n, N), AND the next letter is NOT a vowel,
                    // AND the next letter is NOT y,r,l,v (which create dependent conjuncts like 'rya' or 'lwa')
                    if (Chillus.TryGetValue(cons.Key, out string? chillu) &&
                        (isLastCharInWord || (!nextIsVowel && !nextIsYRLV)))
                    {
                        result.Append(chillu);
                        lastWasConsonant = false; // Chillu acts as a clean break
                    }
                    else
                    {
                        // Standard Consonant Joining Rule (Chandrakkala Injection)
                        if (lastWasConsonant)
                        {
                            result.Append("്");
                        }

                        result.Append(cons.Value);
                        lastWasConsonant = true;
                    }

                    i += cons.Key.Length;
                    matched = true;
                    break;
                }
            }
            if (matched) continue;

            // STEP 3: Fallback for Unknown Characters (Spaces, Numbers, Punctuation)
            if (lastWasConsonant)
            {
                result.Append("്");
                lastWasConsonant = false;
            }

            result.Append(manglish[i]);
            i++;
        }

        // Final cleanup
        if (lastWasConsonant)
        {
            result.Append("്");
        }

        return result.ToString();
    }

    // --- HELPER METHODS FOR LOOK-AHEAD INTELLIGENCE ---

    private static bool IsVowelPrefix(string text)
    {
        foreach (var v in Vowels)
        {
            if (text.StartsWith(v.Key)) return true;
        }
        return false;
    }

    private static bool IsYRLV(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        char c = text[0];
        // 'y', 'r', 'l', 'v', 'w' usually form dependent modifier symbols (്യ, ്ര, ്ല, ്വ) 
        // rather than taking a Chillu before them.
        return c == 'y' || c == 'r' || c == 'l' || c == 'v' || c == 'w' || c == 'R' || c == 'L';
    }
}