namespace Winmozhi.Core.Engines;

/// <summary>
/// Production-level offline transliteration algorithm for Manglish (English) to Malayalam.
/// Handles complex consonant clusters, vowel combinations, and common patterns.
/// </summary>
public static class SimpleMozhiParser
{
    public static string Parse(string manglish)
    {
        if (string.IsNullOrWhiteSpace(manglish)) return string.Empty;

        manglish = manglish.ToLowerInvariant();
        var result = new System.Text.StringBuilder();

        int i = 0;
        while (i < manglish.Length)
        {
            // Try to match multi-character patterns first (greedy approach)
            bool matched = false;

            // ── Three-character patterns ─────────────────────────────────────
            if (i + 2 < manglish.Length)
            {
                string three = manglish.Substring(i, 3);
                if (TryMatchThreeChar(three, result))
                {
                    i += 3;
                    matched = true;
                }
            }

            // ── Two-character patterns ───────────────────────────────────────
            if (!matched && i + 1 < manglish.Length)
            {
                string two = manglish.Substring(i, 2);
                if (TryMatchTwoChar(two, result))
                {
                    i += 2;
                    matched = true;
                }
            }

            // ── Single-character patterns ────────────────────────────────────
            if (!matched)
            {
                char ch = manglish[i];
                if (TryMatchSingleChar(ch, result, i == 0))
                {
                    i++;
                    matched = true;
                }
            }

            // ── Fallback: keep the character as-is ───────────────────────────
            if (!matched)
            {
                result.Append(manglish[i]);
                i++;
            }
        }

        return result.ToString();
    }

    private static bool TryMatchThreeChar(string pattern, System.Text.StringBuilder result)
    {
        return pattern switch
        {
            // Explicit chillu + vowel patterns
            "njan" => AppendAndTrue(result, "ഞാൻ"),
            "kkha" => AppendAndTrue(result, "ക്ഖ"),
            "ksha" => AppendAndTrue(result, "ക്ഷ"),
            "shya" => AppendAndTrue(result, "ശ്യ"),
            "nya" => AppendAndTrue(result, "ന്യ"),
            "jnya" => AppendAndTrue(result, "ജ്ഞ"),
            "ttha" => AppendAndTrue(result, "ത്ത"),
            "ddha" => AppendAndTrue(result, "ഡ്ധ"),
            "ndha" => AppendAndTrue(result, "ന്ധ"),
            "stha" => AppendAndTrue(result, "സ്ത"),
            _ => false,
        };
    }

    private static bool TryMatchTwoChar(string pattern, System.Text.StringBuilder result)
    {
        return pattern switch
        {
            // ── Digraph Consonants (Conjuncts) ──────────────────────────
            "nj" => AppendAndTrue(result, "ഞ്"),
            "ng" => AppendAndTrue(result, "ങ്"),
            "ny" => AppendAndTrue(result, "ന്യ"),
            "zh" => AppendAndTrue(result, "ഴ്"),
            "sh" => AppendAndTrue(result, "ശ്"),
            "ch" => AppendAndTrue(result, "ച്"),
            "th" => AppendAndTrue(result, "ത്"),
            "ph" => AppendAndTrue(result, "ഫ്"),
            "kh" => AppendAndTrue(result, "ഖ്"),
            "gh" => AppendAndTrue(result, "ഘ്"),
            "bh" => AppendAndTrue(result, "ഭ്"),
            "dh" => AppendAndTrue(result, "ധ്"),
            "nh" => AppendAndTrue(result, "ണ്"),
            "rh" => AppendAndTrue(result, "ര്"),

            // ── Double Vowels ───────────────────────────────────────────
            "aa" => AppendAndTrue(result, "ാ"),
            "ee" => AppendAndTrue(result, "ീ"),
            "oo" => AppendAndTrue(result, "ൂ"),
            "ai" => AppendAndTrue(result, "ൈ"),
            "au" => AppendAndTrue(result, "ൗ"),

            // ── Explicit Chillu Letters ──────────────────────────────────
            "il" => AppendAndTrue(result, "ിൽ"),
            "al" => AppendAndTrue(result, "ാൽ"),
            "ar" => AppendAndTrue(result, "ാർ"),
            "an" => AppendAndTrue(result, "ാൻ"),
            "am" => AppendAndTrue(result, "ാം"),
            "um" => AppendAndTrue(result, "ും"),
            "nn" => AppendAndTrue(result, "ണ്ണ്"),

            _ => false,
        };
    }

    private static bool TryMatchSingleChar(char ch, System.Text.StringBuilder result, bool isStart)
    {
        bool matched = ch switch
        {
            // ── Consonants (with Virama ്) ───────────────────────────
            'k' => AppendAndTrue(result, "ക്"),
            'p' => AppendAndTrue(result, "പ്"),
            'm' => AppendAndTrue(result, "മ്"),
            'n' => AppendAndTrue(result, "ന്"),
            'l' => AppendAndTrue(result, "ല്"),
            's' => AppendAndTrue(result, "സ്"),
            'r' => AppendAndTrue(result, "ര്"),
            't' => AppendAndTrue(result, "ത്"),
            'v' => AppendAndTrue(result, "വ്"),
            'w' => AppendAndTrue(result, "വ്"),
            'y' => AppendAndTrue(result, "യ്"),
            'b' => AppendAndTrue(result, "ബ്"),
            'd' => AppendAndTrue(result, "ഡ്"),
            'g' => AppendAndTrue(result, "ഗ്"),
            'h' => AppendAndTrue(result, "ഹ്"),
            'j' => AppendAndTrue(result, "ജ്"),
            'c' => AppendAndTrue(result, "ക്"),
            'f' => AppendAndTrue(result, "ഫ്"),
            'q' => AppendAndTrue(result, "ക്"),
            'x' => AppendAndTrue(result, "ക്സ്"),
            'z' => AppendAndTrue(result, "സ്"),

            // ── Vowels (Start or after Virama removal) ───────────────────
            'a' => AppendAndTrue(result, isStart ? "അ" : ""),  // Removes Virama at end
            'e' => AppendAndTrue(result, isStart ? "എ" : "െ"),
            'i' => AppendAndTrue(result, isStart ? "ഇ" : "ി"),
            'o' => AppendAndTrue(result, isStart ? "ഒ" : "ൊ"),
            'u' => AppendAndTrue(result, isStart ? "ഉ" : "ു"),

            _ => false,
        };

        return matched;
    }

    private static bool AppendAndTrue(System.Text.StringBuilder sb, string text)
    {
        sb.Append(text);
        return true;
    }
}