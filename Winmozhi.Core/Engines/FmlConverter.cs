using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Winmozhi.Core.Engines;

public static partial class FmlConverter
{
    [GeneratedRegex(@"([ക-ഹ](?:്[ക-ഹ])*)ൊ", RegexOptions.Compiled)]
    private static partial Regex VowelOSplitter();

    [GeneratedRegex(@"([ക-ഹ](?:്[ക-ഹ])*)ോ", RegexOptions.Compiled)]
    private static partial Regex VowelOoSplitter();

    [GeneratedRegex(@"([ക-ഹ](?:്[ക-ഹ])*)ൌ", RegexOptions.Compiled)]
    private static partial Regex VowelAuSplitter();

    [GeneratedRegex(@"([ക-ഹ](?:്[ക-ഹ])*)(െ|േ|ൈ)", RegexOptions.Compiled)]
    private static partial Regex LeftModifierReorderer();

    // The genuine ISFOC Keyboard Character Mapping Table for Malayalam ML-TT Fonts
    private static readonly Dictionary<string, string> IsfocMap = new Dictionary<string, string>
    {
        {"ക്ക", "¡"}, {"ങ്ക", "¢"}, {"ങ്ങ", "£"}, {"ച്ച", "¨"}, {"ഞ്ച", "©"},
        {"ഞ്ഞ", "ª"}, {"ട്ട", "«"}, {"ണ്ട", "¬vS"}, {"ണ്ണ", "®"}, {"ത്ത", "¯"},
        {"ന്ത", "´"}, {"ന്ന", "¶"}, {"പ്പ", "¸"}, {"മ്പ", "¼"}, {"മ്മ", "½"},
        {"യ്യ", "¿"}, {"ല്ല", "Ã"}, {"വ്വ", "Æ"}, {"ശ്ശ", "Ç"}, {"സ്സ", "È"},
        {"ക്സ", "Iv"}, {"ക്ഷ", "£"},

        {"അ", "A"}, {"ആ", "B"}, {"ഇ", "C"}, {"ഈ", "D"}, {"ഉ", "E"}, {"ഊ", "F"},
        {"ഋ", "G"}, {"എ", "H"}, {"ഏ", "I"}, {"ഐ", "sF"}, {"ഒ", "H"}, {"ഓ", "Hm"}, {"ഔ", "Hu"},

        {"ക", "I"}, {"ഖ", "J"}, {"ഗ", "K"}, {"ഘ", "L"}, {"ങ", "M"},
        {"ച", "N"}, {"ഛ", "O"}, {"ജ", "P"}, {"ഝ", "Q"}, {"ഞ", "R"},
        {"ട", "S"}, {"ഠ", "T"}, {"ഡ", "U"}, {"ഢ", "V"}, {"ണ", "W"},
        {"ത", "X"}, {"ഥ", "Y"}, {"ദ", "Z"}, {"ധ", "["}, {"ന", "\\"},
        {"പ", "]"}, {"ഫ", "^"}, {"ബ", "_"}, {"ഭ", "`"}, {"മ", "a"},
        {"യ", "b"}, {"ര", "c"}, {"റ", "d"}, {"ല", "e"}, {"ള", "f"},
        {"ഴ", "g"}, {"വ", "h"}, {"ശ", "i"}, {"ഷ", "j"}, {"സ", "k"}, {"ഹ", "l"},

        {"ൺ", "¬"}, {"ൻ", "³"}, {"ർ", "À"}, {"ൽ", "Â"}, {"ൾ", "Ä"},

        {"ാ", "m"}, {"ി", "n"}, {"ീ", "o"}, {"ു", "p"}, {"ൂ", "q"}, {"ൃ", "r"},
        {"െ", "s"}, {"േ", "t"}, {"ൈ", "ss"}, {"ൗ", "u"}, {"ൌ", "u"},

        {"ം", "w"}, {"ഃ", "x"}, {"്", "v"}
    }.OrderByDescending(x => x.Key.Length).ToDictionary(x => x.Key, x => x.Value);

    public static string ConvertToFml(string unicodeText) => ConvertLegacyText(unicodeText);

    public static string ConvertToMl(string unicodeText) => ConvertLegacyText(unicodeText);

    private static string ConvertLegacyText(string unicodeText)
    {
        if (string.IsNullOrEmpty(unicodeText)) return unicodeText;

        string processed = unicodeText;

        processed = VowelOSplitter().Replace(processed, "െ$1ാ");
        processed = VowelOoSplitter().Replace(processed, "േ$1ാ");
        processed = VowelAuSplitter().Replace(processed, "െ$1ൗ");
        processed = LeftModifierReorderer().Replace(processed, "$2$1");

        foreach (var kvp in IsfocMap)
        {
            processed = processed.Replace(kvp.Key, kvp.Value);
        }

        return processed;
    }
}