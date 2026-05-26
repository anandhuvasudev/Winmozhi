namespace Winmozhi.Core.Engines;

using System.Collections.Generic;
using System.IO;
using System.Reflection;

public static class CommonWordsDictionary
{
    public static Dictionary<string, List<string>> GetStarterWords()
    {
        var dictionary = new Dictionary<string, List<string>>();

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream("Winmozhi.Core.Engines.ManglishCorpus.csv");
            if (stream == null) return dictionary;

            // Fix IDE0090
            using StreamReader reader = new(stream);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    var manglish = parts[0].Trim().ToLowerInvariant();
                    var malayalam = parts[1].Trim();

                    if (!dictionary.ContainsKey(manglish))
                        dictionary[manglish] = []; // Fix IDE0028

                    if (!dictionary[manglish].Contains(malayalam))
                        dictionary[manglish].Add(malayalam);
                }
            }
        }
        catch
        {
            // Return empty dictionary on fail, the engine will still rely on algorithmic parsing
        }

        return dictionary;
    }
}