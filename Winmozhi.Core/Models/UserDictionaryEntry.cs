using SQLite;
using System;

namespace Winmozhi.Core.Models;

public class UserDictionaryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed] // Indexed for lightning-fast lookups while typing
    public string ManglishWord { get; set; } = string.Empty;

    public string MalayalamWord { get; set; } = string.Empty;

    public int Frequency { get; set; }

    public DateTime LastUsed { get; set; }
}