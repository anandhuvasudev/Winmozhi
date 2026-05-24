namespace Winmozhi.Core.Interfaces;

public interface IOfflineEngine
{
    List<string> GetSuggestions(string manglishText);
    void LoadDictionary(IEnumerable<KeyValuePair<string, List<string>>> wordPairs);
}