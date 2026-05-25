using System;
using System.Text;

namespace Winmozhi.Core.Engines;

public static class SimpleMozhiParser
{
    // Caching arrays at the class level prevents massive memory allocations per keystroke!
    private static readonly string[] Vowels = ["aa", "ee", "oo", "au", "ou", "ai", "ei", "ae", "oa", "am", "um", "ah", "a", "e", "i", "o", "u"];
    private static readonly string[] Consonants = [
        "ksh", "cch", "tth", "nth", "nch", "sth", "shh", "chh",
        "kk", "gg", "ch", "jj", "tt", "dd", "nn", "th", "dh", "pp", "bb", "mm",
        "yy", "rr", "ll", "vv", "sh", "ss", "hh", "zh",
        "nj", "ng", "nk", "nd", "nt", "mb", "mp", "ph", "bh", "gh", "kh", "jh",
        "k", "g", "c", "j", "t", "d", "n", "p", "f", "b", "m", "y", "r", "l", "v", "w", "s", "h", "z", "x", "q"
    ];

    public static string Parse(string manglish)
    {
        if (string.IsNullOrWhiteSpace(manglish)) return string.Empty;

        manglish = manglish.ToLowerInvariant();
        var sb = new StringBuilder();
        bool lastWasConsonant = false;

        int i = 0;
        while (i < manglish.Length)
        {
            var vowelMatch = MatchVowel(manglish, i);
            if (vowelMatch != null)
            {
                if (lastWasConsonant)
                {
                    if (sb.Length > 0 && sb[^1] == '്') sb.Length--;
                    sb.Append(GetVowelSign(vowelMatch));
                }
                else
                {
                    sb.Append(GetIndependentVowel(vowelMatch));
                }
                lastWasConsonant = false;
                i += vowelMatch.Length;
                continue;
            }

            var consonantMatch = MatchConsonant(manglish, i);
            if (consonantMatch != null)
            {
                bool isLastChar = (i + consonantMatch.Length == manglish.Length);
                bool nextIsConsonant = !isLastChar && MatchVowel(manglish, i + consonantMatch.Length) == null;

                if ((isLastChar || nextIsConsonant) && GetChillu(consonantMatch, out string chillu))
                {
                    sb.Append(chillu);
                    lastWasConsonant = false;
                }
                else
                {
                    sb.Append(GetConsonantBase(consonantMatch)).Append('്');
                    lastWasConsonant = true;
                }
                i += consonantMatch.Length;
                continue;
            }

            sb.Append(manglish[i]);
            lastWasConsonant = false;
            i++;
        }

        return sb.ToString();
    }

    private static string? MatchVowel(string text, int index)
    {
        foreach (var v in Vowels)
        {
            if (index + v.Length <= text.Length && text.AsSpan(index, v.Length).SequenceEqual(v.AsSpan()))
                return v;
        }
        return null;
    }

    private static string? MatchConsonant(string text, int index)
    {
        foreach (var c in Consonants)
        {
            if (index + c.Length <= text.Length && text.AsSpan(index, c.Length).SequenceEqual(c.AsSpan()))
                return c;
        }
        return null;
    }

    private static string GetIndependentVowel(string v) => v switch
    {
        "a" => "അ",
        "aa" => "ആ",
        "i" => "ഇ",
        "ee" => "ഈ",
        "u" => "ഉ",
        "oo" => "ഊ",
        "e" => "എ",
        "ae" => "ഏ",
        "ai" => "ഐ",
        "ei" => "ഐ",
        "o" => "ഒ",
        "oa" => "ഓ",
        "au" => "ഔ",
        "ou" => "ഔ",
        "am" => "അം",
        "um" => "ഉം",
        "ah" => "അഃ",
        _ => ""
    };

    private static string GetVowelSign(string v) => v switch
    {
        "a" => "",
        "aa" => "ാ",
        "i" => "ി",
        "ee" => "ീ",
        "u" => "ു",
        "oo" => "ൂ",
        "e" => "െ",
        "ae" => "േ",
        "ai" => "ൈ",
        "ei" => "ൈ",
        "o" => "ൊ",
        "oa" => "ോ",
        "au" => "ൌ",
        "ou" => "ൌ",
        "am" => "ം",
        "um" => "ും",
        "ah" => "ഃ",
        _ => ""
    };

    private static string GetConsonantBase(string c) => c switch
    {
        "ksh" => "ക്ഷ",
        "cch" => "ച്ച",
        "tth" => "ത്ത",
        "nth" => "ന്ത",
        "nch" => "ഞ്ച",
        "sth" => "സ്ഥ",
        "shh" => "ഷ",
        "chh" => "ഛ",
        "kk" => "ക്ക",
        "gg" => "ഗ്ഗ",
        "ch" => "ച",
        "jj" => "ജ്ജ",
        "tt" => "ട്ട",
        "dd" => "ഡ്ഡ",
        "nn" => "ന്ന",
        "th" => "ത",
        "dh" => "ധ",
        "pp" => "പ്പ",
        "bb" => "ബ്ബ",
        "mm" => "മ്മ",
        "yy" => "യ്യ",
        "rr" => "റ്റ",
        "ll" => "ല്ല",
        "vv" => "വ്വ",
        "sh" => "ശ",
        "ss" => "സ്സ",
        "hh" => "ഹ്ന",
        "zh" => "ഴ",
        "nj" => "ഞ",
        "ng" => "ങ",
        "nk" => "ങ്ക",
        "nd" => "ണ്ട",
        "nt" => "ന്റ",
        "mb" => "മ്പ",
        "mp" => "മ്പ",
        "ph" => "ഫ",
        "bh" => "ഭ",
        "gh" => "ഘ",
        "kh" => "ഖ",
        "jh" => "ഝ",
        "k" => "ക",
        "g" => "ഗ",
        "c" => "ച",
        "j" => "ജ",
        "t" => "ട",
        "d" => "ഡ",
        "n" => "ന",
        "p" => "പ",
        "f" => "ഫ",
        "b" => "ബ",
        "m" => "മ",
        "y" => "യ",
        "r" => "ര",
        "l" => "ല",
        "v" => "വ",
        "w" => "വ",
        "s" => "സ",
        "h" => "ഹ",
        "z" => "സ",
        "x" => "ക്സ",
        "q" => "ക",
        _ => "ക"
    };

    private static bool GetChillu(string c, out string chillu)
    {
        chillu = c switch { "l" => "ൽ", "n" => "ൻ", "r" => "ർ", "m" => "ം", "ll" => "ൾ", _ => "" };
        return chillu != "";
    }
}