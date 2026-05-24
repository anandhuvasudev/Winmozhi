using Winmozhi.Core.Interfaces;

namespace Winmozhi.Core.Engines;

public class MockHistoryDatabase : IHistoryDatabase
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task UpdateWordFrequencyAsync(string manglish, string malayalam) => Task.CompletedTask;
    public Task<List<string>> GetUserSuggestionsAsync(string manglish) => Task.FromResult(new List<string>());
}