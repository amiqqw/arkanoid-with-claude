using System.Collections.Generic;
using Godot;

public class HighScoreEntry
{
    public string Name { get; set; }
    public int Score { get; set; }
}

public partial class HighScoreTable : Node
{
    public static HighScoreTable Instance { get; private set; }

    public const int MaxEntries = 10;
    private const string FilePath = "user://highscores.txt";

    public List<HighScoreEntry> Entries { get; private set; } = new();

    public override void _Ready()
    {
        Instance = this;
        Load();
    }

    /// <summary>
    /// Подходит ли счёт для попадания в топ?
    /// </summary>
    public bool IsHighScore(int score)
    {
        if (score <= 0) return false;
        if (Entries.Count < MaxEntries) return true;
        return score > Entries[Entries.Count - 1].Score;   // больше, чем у последнего в топе
    }

    /// <summary>
    /// Добавляет запись, сортирует, обрезает до MaxEntries, сохраняет в файл.
    /// Возвращает индекс новой записи (для подсветки в UI), или -1 если не попала.
    /// </summary>
    public int AddEntry(string name, int score)
    {
        if (string.IsNullOrWhiteSpace(name))
            name = "Player";

        var entry = new HighScoreEntry { Name = name.Trim(), Score = score };
        Entries.Add(entry);
        Entries.Sort((a, b) => b.Score.CompareTo(a.Score));

        if (Entries.Count > MaxEntries)
            Entries.RemoveRange(MaxEntries, Entries.Count - MaxEntries);

        Save();
        return Entries.IndexOf(entry);
    }

    public int HighestScore => Entries.Count > 0 ? Entries[0].Score : 0;

    private void Load()
    {
        Entries.Clear();

        if (!FileAccess.FileExists(FilePath))
            return;

        try
        {
            using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"HighScoreTable: cannot open '{FilePath}' for reading. Error: {FileAccess.GetOpenError()}");
                return;
            }

            while (!file.EofReached())
            {
                string line = file.GetLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',', 2);
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[1], out int score)) continue;

                Entries.Add(new HighScoreEntry { Name = parts[0], Score = score });
            }

            Entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"HighScoreTable: failed to load scores. {e.Message}");
            Entries.Clear();
        }
    }

    private void Save()
    {
        try
        {
            using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"HighScoreTable: cannot open '{FilePath}' for writing. Error: {FileAccess.GetOpenError()}");
                return;
            }

            foreach (var entry in Entries)
            {
                file.StoreLine($"{entry.Name},{entry.Score}");
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"HighScoreTable: failed to save scores. {e.Message}");
        }
    }

    public void Clear()
    {
        Entries.Clear();
        Save();
    }
}