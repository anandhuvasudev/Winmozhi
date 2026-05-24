using System.Text;

namespace Winmozhi.Core.Engines;

public static class SimpleMozhiParser
{
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
        string[] vowels = ["aa", "ee", "oo", "au", "ou", "ai", "ei", "ae", "oa", "am", "um", "ah", "a", "e", "i", "o", "u"];
        foreach (var v in vowels)
        {
            if (index + v.Length <= text.Length && text.Substring(index, v.Length) == v)
                return v;
        }
        return null;
    }

    private static string? MatchConsonant(string text, int index)
    {
        string[] consonants = ["shh", "chh", "nth", "nch", "sth", "nd", "nj", "ng", "th", "dh", "ph", "bh", "sh", "ch", "jh", "gh", "kh", "zh", "kk", "mm", "nn", "ll", "rr", "tt", "pp", "k", "g", "c", "j", "t", "d", "n", "p", "f", "b", "m", "y", "r", "l", "v", "w", "s", "h", "z", "x", "q"];
        foreach (var c in consonants)
        {
            if (index + c.Length <= text.Length && text.Substring(index, c.Length) == c)
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
        "k" => "ക",
        "kk" => "ക്ക",
        "kh" => "ഖ",
        "g" => "ഗ",
        "gh" => "ഘ",
        "ng" => "ങ",
        "c" => "ച",
        "ch" => "ച",
        "chh" => "ഛ",
        "j" => "ജ",
        "jh" => "ഝ",
        "nj" => "ഞ",
        "nch" => "ഞ്ച",
        "t" => "ട",
        "tt" => "ട്ട",
        "th" => "ത",
        "nth" => "ന്ത",
        "d" => "ഡ",
        "dh" => "ധ",
        "nd" => "ണ്ട",
        "n" => "ന",
        "nn" => "ന്ന",
        "p" => "പ",
        "pp" => "പ്പ",
        "ph" => "ഫ",
        "f" => "ഫ",
        "b" => "ബ",
        "bh" => "ഭ",
        "m" => "മ",
        "mm" => "മ്മ",
        "y" => "യ",
        "r" => "ര",
        "rr" => "റ്റ",
        "l" => "ല",
        "ll" => "ല്ല",
        "v" => "വ",
        "w" => "വ",
        "sh" => "ശ",
        "shh" => "ഷ",
        "s" => "സ",
        "h" => "ഹ",
        "zh" => "ഴ",
        "z" => "സ",
        "x" => "ക്സ",
        "q" => "ക",
        "sth" => "സ്ഥ",
        _ => "ക"
    };

    private static bool GetChillu(string c, out string chillu)
    {
        chillu = c switch
        {
            "l" => "ൽ",
            "n" => "ൻ",
            "r" => "ർ",
            "m" => "ം",
            "ll" => "ൾ",
            _ => ""
        };
        return chillu != "";
    }
}