using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Winmozhi.Core.Interfaces;

/// <summary>
/// The brain of Winmozhi. Split into two methods to enable progressive popup loading:
/// instant results first, Google results merged in when the network call returns.
/// </summary>
public interface ITransliterationEngine
{
    /// <summary>
    /// Returns suggestions from history (SQLite) + offline Trie only.
    /// Executes in under 5 ms. Call this first to show the popup immediately after the debounce.
    /// </summary>
    Task<IEnumerable<string>> GetInstantSuggestionsAsync(
        string manglishText, CancellationToken cancellationToken);

    /// <summary>
    /// Returns suggestions from the Google Input Tools API.
    /// Has network latency (typically 100–400 ms). Call this after displaying instant results,
    /// then merge the response to update the popup without disrupting the user's selection.
    /// </summary>
    Task<IEnumerable<string>> GetOnlineSuggestionsAsync(
        string manglishText, CancellationToken cancellationToken);
}