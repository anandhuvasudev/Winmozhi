using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Winmozhi.Core.Interfaces;
using Winmozhi.Core.Models;

namespace Winmozhi.Core.Engines;

public class SqliteHistoryDatabase : IHistoryDatabase
{
    private SQLiteAsyncConnection? _database;

    public async Task InitializeAsync()
    {
        if (_database != null) return;

        // Save the DB in the standard Windows AppData/Local folder
        // This is perfectly safe and writable for MSIX Store Apps!
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(appData, "Winmozhi", "history.db3");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        _database = new SQLiteAsyncConnection(dbPath);

        // Creates the table if it doesn't exist
        await _database.CreateTableAsync<UserDictionaryEntry>();
    }

    public async Task UpdateWordFrequencyAsync(string manglish, string malayalam)
    {
        await InitializeAsync();

        var entry = await _database!.Table<UserDictionaryEntry>()
            .Where(x => x.ManglishWord == manglish && x.MalayalamWord == malayalam)
            .FirstOrDefaultAsync();

        if (entry == null)
        {
            // First time using this specific word pair
            entry = new UserDictionaryEntry
            {
                ManglishWord = manglish,
                MalayalamWord = malayalam,
                Frequency = 1,
                LastUsed = DateTime.UtcNow
            };
            await _database.InsertAsync(entry);
        }
        else
        {
            // Word exists! Increment frequency so it ranks higher next time
            entry.Frequency++;
            entry.LastUsed = DateTime.UtcNow;
            await _database.UpdateAsync(entry);
        }
    }

    public async Task<List<string>> GetUserSuggestionsAsync(string manglish)
    {
        await InitializeAsync();

        // 1. Find exact matches first
        var exactMatches = await _database!.Table<UserDictionaryEntry>()
            .Where(x => x.ManglishWord == manglish)
            .OrderByDescending(x => x.Frequency)
            .ThenByDescending(x => x.LastUsed)
            .ToListAsync();

        var results = new List<string>();
        foreach (var entry in exactMatches)
        {
            results.Add(entry.MalayalamWord);
        }

        return results;
    }

    public async Task ClearHistoryAsync()
    {
        await InitializeAsync();
        if (_database != null)
        {
            await _database.DeleteAllAsync<UserDictionaryEntry>();
        }
    }
}