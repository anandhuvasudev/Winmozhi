

namespace Winmozhi.Core.Engines;

using System.Collections.Generic;
public static class CommonWordsDictionary
{
    public static Dictionary<string, List<string>> GetStarterWords()
    {
        return new Dictionary<string, List<string>>
        {
            // ── Pronouns & Common Greetings ──────────────────────────────────
            { "njan", ["ഞാൻ", "ഞാന്"] },
            { "ni", ["നീ", "നി"] },
            { "avan", ["അവൻ"] },
            { "avar", ["അവർ", "അവര്‍"] },
            { "ava", ["അവ"] },
            { "yal", ["യാൾ"] },
            { "nam", ["നാം"] },
            { "ningal", ["നിങ്ങൾ"] },
            { "evidan", ["എവിടാൻ"] },

            // ── Basic Sentences & Questions ──────────────────────────────────
            { "enth", ["എന്ത്", "എന്താണ്"] },
            { "entha", ["എന്താ", "എന്താണ്"] },
            { "enkil", ["എങ്കിൽ"] },
            { "enthu", ["എന്ത്", "എന്ത്ത്"] },
            { "evide", ["എവിടെ", "എവിടേ"] },
            { "ethra", ["എത്ര"] },
            { "eppol", ["എപ്പോൾ"] },
            { "ippol", ["ഇപ്പോൾ", "ഇപ്പോ"] },
            { "innu", ["ഇന്ന്"] },
            { "naale", ["നാളെ"] },
            { "nale", ["നാളെ"] },
            { "netram", ["നെത്രം"] },

            // ── Actions & Verbs ──────────────────────────────────────────────
            { "varunnu", ["വരുന്നു"] },
            { "pookunnu", ["പോകുന്നു"] },
            { "pokunnu", ["പോകുന്നു"] },
            { "cheyunnu", ["ചെയ്യുന്നു"] },
            { "undakum", ["ഉണ്ടാകും"] },
            { "illa", ["ഇല്ല"] },
            { "undo", ["ഉണ്ടോ"] },
            { "onda", ["ഉണ്ട"] },
            { "parayu", ["പറയൂ", "പറയു"] },
            { "cheyya", ["ചെയ്യാ"] },
            { "kanum", ["കാണും"] },
            { "ketum", ["കേടും"] },
            { "vendum", ["വേണ്ടും"] },

            // ── Responses & Common Phrases ───────────────────────────────────
            { "shari", ["ശരി"] },
            { "sari", ["ശരി"] },
            { "athe", ["അതെ", "അതേ"] },
            { "athe", ["അതെ", "അതേ"] },
            { "koodi", ["കൂടി"] },
            { "koode", ["കൂടെ"] },
            { "kayari", ["കയ്യരി"] },
            { "valla", ["വല്ല"] },
            { "namaskaram", ["നമസ്കാരം"] },
            { "sukhamano", ["സുഖമാണോ", "സുഖമാണ്"] },

            // ── Numbers & Time ───────────────────────────────────────────────
            { "onnu", ["ഒന്ന്"] },
            { "randu", ["രണ്ട്"] },
            { "moonu", ["മൂന്ന്"] },
            { "naal", ["നാൾ"] },
            { "varsha", ["വർഷം"] },
            { "masa", ["മാസം"] },
            { "anirudum", ["അനിരുദ്ധം"] },

            // ── Food & Common Objects ────────────────────────────────────────
            { "anna", ["അന്ന"] },
            { "paal", ["പാൽ"] },
            { "vellam", ["വെള്ളം"] },
            { "kappi", ["കാപ്പി"] },
            { "chai", ["ചായ", "ചായ്"] },
            { "nandu", ["നണ്ട്"] },
            { "meen", ["മീൻ"] },
            { "kuri", ["കുരി"] },
            { "ericha", ["എരിച്ച"] },

            // ── Adjectives & Descriptors ─────────────────────────────────────
            { "nyayam", ["ന്യായം"] },
            { "thavam", ["താവം"] },
            { "vadakam", ["വടക്കം"] },
            { "kodiyum", ["കോടിയും"] },
            { "kayam", ["കയം"] },

            // ── More Common Words ────────────────────────────────────────────
            { "maman", ["മാമൻ"] },
            { "akka", ["അക്ക"] },
            { "chettan", ["ചേട്ടൻ"] },
            { "amma", ["അമ്മ"] },
            { "appa", ["അപ്പ"] },
            { "pulli", ["പുല്ലി"] },
            { "makal", ["മകൾ"] },
            { "makan", ["മകൻ"] },
            { "vendi", ["വെണ്ടി"] },
            { "vendippu", ["വെണ്ടിപ്പ"] },
            { "thazhvaram", ["താഴ്വരം"] },
            { "kalam", ["കാലം"] },
            { "thrum", ["ത്രും"] },
            { "ila", ["ഇല"] },
            { "il", ["ിൽ"] },
            { "um", ["ും"] },
            { "kuttam", ["കുട്ടം"] },
            { "yathra", ["യാത്ര"] },
            { "venam", ["വേണം"] },
            { "karavaan", ["കരവാൻ"] },
            { "kadam", ["കാടം"] },
            { "thanam", ["തനം"] },
            { "moolam", ["മൂലം"] },
            { "payyan", ["പയ്യൻ"] },
            { "muthassil", ["മുതശ്ശിൽ"] },
        };
    }
}