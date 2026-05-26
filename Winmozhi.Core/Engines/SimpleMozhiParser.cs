using System;
using System.Text;

namespace Winmozhi.Core.Engines;

public static class SimpleMozhiParser
{
    /// <summary>
    /// Parses Manglish text into algorithmic Malayalam using high-performance 
    /// zero-allocation span slicing and intelligent phonetic digraph rules.
    /// </summary>
    public static string Parse(string manglish)
    {
        if (string.IsNullOrWhiteSpace(manglish)) return string.Empty;

        var text = manglish.ToLowerInvariant().AsSpan();
        var sb = new StringBuilder(text.Length * 2);
        bool lastWasConsonant = false;

        int i = 0;
        while (i < text.Length)
        {
            // 1. Check for Vowels
            int vowelLen = TryMatchVowel(text[i..], out string indVowel, out string signVowel);
            if (vowelLen > 0)
            {
                if (lastWasConsonant)
                {
                    // Remove trailing virama (്) before adding a vowel sign
                    if (sb.Length > 0 && sb[^1] == '്') sb.Length--;

                    if (signVowel != "a_implied")
                    {
                        sb.Append(signVowel);
                    }
                }
                else
                {
                    sb.Append(indVowel);
                }
                lastWasConsonant = false;
                i += vowelLen;
                continue;
            }

            // 2. Check for Consonants and Phonetic Clusters
            int consLen = TryMatchConsonant(text[i..], out string baseConsonant, out string? chillu);
            if (consLen > 0)
            {
                bool isLastChar = (i + consLen == text.Length);
                bool nextIsConsonant = !isLastChar && TryMatchVowel(text[(i + consLen)..], out _, out _) == 0;

                // Apply Chillu if it's the end of a word, or immediately followed by another consonant
                if ((isLastChar || nextIsConsonant) && chillu != null)
                {
                    sb.Append(chillu);
                    lastWasConsonant = false; // Chillu cannot accept a vowel sign
                }
                else
                {
                    sb.Append(baseConsonant).Append('്'); // Append base character + virama
                    lastWasConsonant = true;
                }
                i += consLen;
                continue;
            }

            // 3. Unrecognized characters (Numbers, Punctuation, Spaces)
            sb.Append(text[i]);
            lastWasConsonant = false;
            i++;
        }

        return sb.ToString();
    }

    private static int TryMatchVowel(ReadOnlySpan<char> span, out string ind, out string sign)
    {
        ind = string.Empty;
        sign = string.Empty;

        if (span.Length >= 2)
        {
            var two = span[..2];
            if (two.SequenceEqual("aa")) { ind = "ആ"; sign = "ാ"; return 2; }
            if (two.SequenceEqual("ee")) { ind = "ഈ"; sign = "ീ"; return 2; }
            if (two.SequenceEqual("oo")) { ind = "ഊ"; sign = "ൂ"; return 2; }
            if (two.SequenceEqual("au")) { ind = "ഔ"; sign = "ൌ"; return 2; }
            if (two.SequenceEqual("ou")) { ind = "ഔ"; sign = "ൌ"; return 2; }
            if (two.SequenceEqual("ai")) { ind = "ഐ"; sign = "ൈ"; return 2; }
            if (two.SequenceEqual("ei")) { ind = "ഐ"; sign = "ൈ"; return 2; }
            if (two.SequenceEqual("ae")) { ind = "ഏ"; sign = "േ"; return 2; }
            if (two.SequenceEqual("oa")) { ind = "ഓ"; sign = "ോ"; return 2; }
            if (two.SequenceEqual("am")) { ind = "അം"; sign = "ം"; return 2; }
            if (two.SequenceEqual("um")) { ind = "ഉം"; sign = "ും"; return 2; }
            if (two.SequenceEqual("ah")) { ind = "അഃ"; sign = "ഃ"; return 2; }
        }

        char c = span[0];
        switch (c)
        {
            case 'a': ind = "അ"; sign = "a_implied"; return 1; // 'a' kills virama but has no visual sign
            case 'e': ind = "എ"; sign = "െ"; return 1;
            case 'i': ind = "ഇ"; sign = "ി"; return 1;
            case 'o': ind = "ഒ"; sign = "ൊ"; return 1;
            case 'u': ind = "ഉ"; sign = "ു"; return 1;
        }

        return 0;
    }

    private static int TryMatchConsonant(ReadOnlySpan<char> span, out string baseConsonant, out string? chillu)
    {
        baseConsonant = string.Empty;
        chillu = null;

        // 3-Letter Complex Clusters
        if (span.Length >= 3)
        {
            var three = span[..3];
            if (three.SequenceEqual("ksh")) { baseConsonant = "ക്ഷ"; return 3; }
            if (three.SequenceEqual("nth")) { baseConsonant = "ന്ത"; return 3; }
            if (three.SequenceEqual("nch")) { baseConsonant = "ഞ്ച"; return 3; }
            if (three.SequenceEqual("shh")) { baseConsonant = "ഷ"; return 3; }
            if (three.SequenceEqual("cch")) { baseConsonant = "ച്ച"; return 3; }
            if (three.SequenceEqual("sth")) { baseConsonant = "സ്ഥ"; return 3; }
        }

        // 2-Letter Digraphs & Malayalam Specialties
        if (span.Length >= 2)
        {
            var two = span[..2];

            // Nuanced Malayalam Clusters
            if (two.SequenceEqual("nj")) { baseConsonant = "ഞ"; return 2; }
            if (two.SequenceEqual("ng")) { baseConsonant = "ങ"; return 2; }
            if (two.SequenceEqual("nk")) { baseConsonant = "ങ്ക"; return 2; }
            if (two.SequenceEqual("nd")) { baseConsonant = "ണ്ട"; return 2; }
            if (two.SequenceEqual("nt")) { baseConsonant = "ന്റ"; return 2; } // Handles standard "ente" -> എന്റെ
            if (two.SequenceEqual("mb")) { baseConsonant = "മ്പ"; return 2; }
            if (two.SequenceEqual("mp")) { baseConsonant = "മ്പ"; return 2; }

            // Standard Aspirated Consonants
            if (two.SequenceEqual("ch")) { baseConsonant = "ച"; return 2; }
            if (two.SequenceEqual("th")) { baseConsonant = "ത"; return 2; }
            if (two.SequenceEqual("dh")) { baseConsonant = "ധ"; return 2; }
            if (two.SequenceEqual("ph")) { baseConsonant = "ഫ"; return 2; }
            if (two.SequenceEqual("bh")) { baseConsonant = "ഭ"; return 2; }
            if (two.SequenceEqual("sh")) { baseConsonant = "ശ"; return 2; }
            if (two.SequenceEqual("zh")) { baseConsonant = "ഴ"; return 2; }
            if (two.SequenceEqual("kh")) { baseConsonant = "ഖ"; return 2; }
            if (two.SequenceEqual("gh")) { baseConsonant = "ഘ"; return 2; }
            if (two.SequenceEqual("jh")) { baseConsonant = "ഝ"; return 2; }

            // Double Consonants
            if (two.SequenceEqual("ll")) { baseConsonant = "ല്ല"; chillu = "ൾ"; return 2; } // ll gracefully handles ൾ chillu
            if (two.SequenceEqual("rr")) { baseConsonant = "റ്റ"; return 2; }
            if (two.SequenceEqual("nn")) { baseConsonant = "ന്ന"; return 2; }
            if (two.SequenceEqual("mm")) { baseConsonant = "മ്മ"; return 2; }
            if (two.SequenceEqual("tt")) { baseConsonant = "ട്ട"; return 2; }
            if (two.SequenceEqual("kk")) { baseConsonant = "ക്ക"; return 2; }
            if (two.SequenceEqual("pp")) { baseConsonant = "പ്പ"; return 2; }
        }

        // 1-Letter Base Consonants
        char c = span[0];
        switch (c)
        {
            case 'k': baseConsonant = "ക"; break;
            case 'g': baseConsonant = "ഗ"; break;
            case 'c': baseConsonant = "ച"; break;
            case 'j': baseConsonant = "ജ"; break;
            case 't': baseConsonant = "ട"; break;
            case 'd': baseConsonant = "ഡ"; break;
            case 'n': baseConsonant = "ന"; chillu = "ൻ"; break; // Defaults to ൻ for context
            case 'p': baseConsonant = "പ"; break;
            case 'f': baseConsonant = "ഫ"; break;
            case 'b': baseConsonant = "ബ"; break;
            case 'm': baseConsonant = "മ"; chillu = "ം"; break; // Defaults to Anuswaram ം context
            case 'y': baseConsonant = "യ"; break;
            case 'r': baseConsonant = "ര"; chillu = "ർ"; break; // Defaults to ർ for context
            case 'l': baseConsonant = "ല"; chillu = "ൽ"; break; // Defaults to ൽ for context
            case 'v':
            case 'w': baseConsonant = "വ"; break;
            case 's': baseConsonant = "സ"; break;
            case 'h': baseConsonant = "ഹ"; break;
            case 'z': baseConsonant = "സ"; break;
            case 'x': baseConsonant = "ക്സ"; break;
            case 'q': baseConsonant = "ക്യു"; break; // Phonetic map for 'Q'
            default: return 0;
        }

        return 1;
    }
}