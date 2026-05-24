namespace Winmozhi.Core.Interfaces;

public interface ITransliterationEngine
{
    Task<IEnumerable<string>> GetSuggestionsAsync(string manglishText, CancellationToken cancellationToken);
}