namespace Winmozhi.Core.Interfaces;

public interface IHistoryDatabase
{
    Task InitializeAsync();
    Task UpdateWordFrequencyAsync(string manglish, string malayalam);
    Task<List<string>> GetUserSuggestionsAsync(string manglish);
}