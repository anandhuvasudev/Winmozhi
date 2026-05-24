using System.Collections.Generic;

namespace Winmozhi.Core.Engines;

public static class CommonWordsDictionary
{
    public static Dictionary<string, List<string>> GetStarterWords()
    {
        return new Dictionary<string, List<string>>
        {
            { "njan", ["ഞാൻ", "ഞാന്"] },
            { "enth", ["എന്ത്", "എന്താണ്"] },
            { "entha", ["എന്താ", "എന്താണ്"] },
            { "sukhamano", ["സുഖമാണോ"] },
            { "evide", ["എവിടെ", "എവിടേ"] },
            { "varunnu", ["വരുന്നു"] },
            { "pookunnu", ["പോകുന്നു"] },
            { "pokunnu", ["പോകുന്നു"] },
            { "shari", ["ശരി"] },
            { "sari", ["ശരി"] },
            { "athe", ["അതെ", "അതേ"] },
            { "illa", ["ഇല്ല"] },
            { "koodi", ["കൂടി"] },
            { "ippol", ["ഇപ്പോൾ"] },
            { "naale", ["നാളെ"] },
            { "nale", ["നാളെ"] },
            { "innu", ["ഇന്ന്"] },
            { "aare", ["ആരെ"] },
            { "ethu", ["ഏതു", "ഏത്"] },
            { "namaskaram", ["നമസ്കാരം"] },
            { "parayu", ["പറയു", "പറയൂ"] }
        };
    }
}