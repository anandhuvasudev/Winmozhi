namespace Winmozhi.Core.Interfaces;

public interface IOnlineEngine
{
    Task<List<string>> FetchSuggestionsAsync(string manglishText, CancellationToken cancellationToken);
}