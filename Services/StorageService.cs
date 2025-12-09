using Microsoft.Data.Sqlite;
using Math.Models;
using System.Globalization;

namespace Math.Services;

public interface IStorageService
{
    Task InitializeAsync();
    Task SavePlayerAsync(Player player);
    Task<Player?> LoadPlayerAsync();
    Task LogAnswerAsync(string category, string question, double correctAnswer, double userAnswer, bool isCorrect, int difficulty, int streakBefore, int pointsAwarded);
    Task<IReadOnlyList<AnswerLog>> GetRecentAnswersAsync(int take = 100);
}

public class AnswerLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public double CorrectAnswer { get; set; }
    public double UserAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int Difficulty { get; set; }
    public int StreakBefore { get; set; }
    public int PointsAwarded { get; set; }
}

public class StorageService : IStorageService
{
    private string _dbPath => Path.Combine(FileSystem.AppDataDirectory, "math_app.db");
    private bool _initialized;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS PlayerProfile (
    Id INTEGER PRIMARY KEY CHECK (Id=1),
    Name TEXT NOT NULL,
    Grade INTEGER NOT NULL,
    Points INTEGER NOT NULL,
    Avatar TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS AnswerLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp TEXT NOT NULL,
    Category TEXT NOT NULL,
    Question TEXT NOT NULL,
    CorrectAnswer REAL NOT NULL,
    UserAnswer REAL NOT NULL,
    IsCorrect INTEGER NOT NULL,
    Difficulty INTEGER NOT NULL,
    StreakBefore INTEGER NOT NULL,
    PointsAwarded INTEGER NOT NULL
);
";
        await cmd.ExecuteNonQueryAsync();

        // Migrate: ensure Language column exists
        if (!await ColumnExistsAsync(conn, "PlayerProfile", "Language"))
        {
            var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE PlayerProfile ADD COLUMN Language TEXT NOT NULL DEFAULT ''";
            await alter.ExecuteNonQueryAsync();
        }

        _initialized = true;
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection conn, string table, string column)
    {
        await using var pragma = conn.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info('{table}')";
        await using var reader = await pragma.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public async Task SavePlayerAsync(Player player)
    {
        await InitializeAsync();
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO PlayerProfile (Id, Name, Grade, Points, Avatar, Language) VALUES (1, $name, $grade, $points, $avatar, $lang)
            ON CONFLICT(Id) DO UPDATE SET Name=$name, Grade=$grade, Points=$points, Avatar=$avatar, Language=$lang";
        cmd.Parameters.AddWithValue("$name", player.Name);
        cmd.Parameters.AddWithValue("$grade", player.Grade);
        cmd.Parameters.AddWithValue("$points", player.Points);
        cmd.Parameters.AddWithValue("$avatar", player.Avatar);
        cmd.Parameters.AddWithValue("$lang", player.Language ?? string.Empty);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Player?> LoadPlayerAsync()
    {
        await InitializeAsync();
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Name, Grade, Points, Avatar, Language FROM PlayerProfile WHERE Id=1";
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var player = new Player
            {
                Name = reader.GetString(0),
                Grade = reader.GetInt32(1),
                Points = reader.GetInt32(2),
                Avatar = reader.GetString(3)
            };
            // Language column added via migration; handle older DBs just in case
            if (reader.FieldCount >= 5 && !reader.IsDBNull(4))
            {
                player.Language = reader.GetString(4);
            }
            return player;
        }
        return null;
    }

    public async Task LogAnswerAsync(string category, string question, double correctAnswer, double userAnswer, bool isCorrect, int difficulty, int streakBefore, int pointsAwarded)
    {
        await InitializeAsync();
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO AnswerLog (Timestamp, Category, Question, CorrectAnswer, UserAnswer, IsCorrect, Difficulty, StreakBefore, PointsAwarded)
VALUES ($ts,$cat,$q,$ca,$ua,$ic,$diff,$st,$pts)";
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$cat", category);
        cmd.Parameters.AddWithValue("$q", question);
        cmd.Parameters.AddWithValue("$ca", correctAnswer);
        cmd.Parameters.AddWithValue("$ua", userAnswer);
        cmd.Parameters.AddWithValue("$ic", isCorrect ? 1 : 0);
        cmd.Parameters.AddWithValue("$diff", difficulty);
        cmd.Parameters.AddWithValue("$st", streakBefore);
        cmd.Parameters.AddWithValue("$pts", pointsAwarded);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<AnswerLog>> GetRecentAnswersAsync(int take = 100)
    {
        await InitializeAsync();
        var list = new List<AnswerLog>();
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT Id, Timestamp, Category, Question, CorrectAnswer, UserAnswer, IsCorrect, Difficulty, StreakBefore, PointsAwarded FROM AnswerLog ORDER BY Id DESC LIMIT {take}";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new AnswerLog
            {
                Id = reader.GetInt64(0),
                Timestamp = DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
                Category = reader.GetString(2),
                Question = reader.GetString(3),
                CorrectAnswer = reader.GetDouble(4),
                UserAnswer = reader.GetDouble(5),
                IsCorrect = reader.GetInt32(6) == 1,
                Difficulty = reader.GetInt32(7),
                StreakBefore = reader.GetInt32(8),
                PointsAwarded = reader.GetInt32(9)
            });
        }
        return list;
    }
}
