namespace Winmozhi.Core.Engines;

public static class SimpleMozhiParser
{
    // A highly optimized, basic fallback transliteration algorithm.
    // Handles common structures without needing internet.
    public static string Parse(string manglish)
    {
        if (string.IsNullOrWhiteSpace(manglish)) return string.Empty;

        manglish = manglish.ToLowerInvariant();

        // Handle explicit chillu letters
        manglish = manglish.Replace("njan", "ഞാൻ")
                           .Replace("il", "ിൽ")
                           .Replace("um", "ും")
                           .Replace("al", "ാൽ")
                           .Replace("ar", "ാർ")
                           .Replace("an", "ാൻ")
                           .Replace("am", "ാം");

        // Basic Digraphs & Consonants
        manglish = manglish.Replace("nj", "ഞ്")
                           .Replace("zh", "ഴ്")
                           .Replace("th", "ത്")
                           .Replace("sh", "ശ്")
                           .Replace("ch", "ച്")
                           .Replace("ph", "ഫ്")
                           .Replace("kh", "ഖ്")
                           .Replace("bh", "ഭ്")
                           .Replace("dh", "ധ്")
                           .Replace("gh", "ഘ്")
                           .Replace("k", "ക്")
                           .Replace("p", "പ്")
                           .Replace("m", "മ്")
                           .Replace("n", "ന്")
                           .Replace("l", "ല്")
                           .Replace("s", "സ്")
                           .Replace("r", "ര്")
                           .Replace("t", "റ്റ്")
                           .Replace("v", "വ്")
                           .Replace("w", "വ്")
                           .Replace("y", "യ്")
                           .Replace("b", "ബ്")
                           .Replace("d", "ഡ്")
                           .Replace("g", "ഗ്")
                           .Replace("h", "ഹ്")
                           .Replace("j", "ജ്")
                           .Replace("c", "ക്");

        // Dependent Vowels (Merging base consonants with vowels)
        manglish = manglish.Replace("്a", "")   // a removes the Virama (്)
                           .Replace("്e", "െ")
                           .Replace("്i", "ി")
                           .Replace("്o", "ൊ")
                           .Replace("്u", "ു")
                           .Replace("aa", "ാ")
                           .Replace("ee", "ീ")
                           .Replace("oo", "ൂ");

        // Standalone Start Vowels - Optimized using Span and Char matching
        if (manglish.StartsWith('a')) manglish = string.Concat("അ", manglish.AsSpan(1));
        else if (manglish.StartsWith('e')) manglish = string.Concat("എ", manglish.AsSpan(1));
        else if (manglish.StartsWith('i')) manglish = string.Concat("ഇ", manglish.AsSpan(1));
        else if (manglish.StartsWith('o')) manglish = string.Concat("ഒ", manglish.AsSpan(1));
        else if (manglish.StartsWith('u')) manglish = string.Concat("ഉ", manglish.AsSpan(1));

        return manglish;
    }
}