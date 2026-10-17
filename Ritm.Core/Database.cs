using Microsoft.Data.Sqlite;

namespace Ritm;

public sealed class Database : IDisposable
{
    readonly SqliteConnection _connection;
    bool _disposed;

    public Database(string path)
    {
        Batteries.Init();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        };
        _connection = new SqliteConnection(builder.ToString());
        _connection.Open();
        Migrate();
        EnsureSeed();
    }

    public string DataSource => _connection.DataSource;

    public List<WorkSchedule> LoadAll()
    {
        var list = new List<WorkSchedule>();
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, name, start_minutes, end_minutes, days_mask, enabled, reset_on_start, session_key
                FROM schedules
                ORDER BY id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new WorkSchedule
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    StartMinutes = reader.GetInt32(2),
                    EndMinutes = reader.GetInt32(3),
                    DaysMask = reader.GetInt32(4),
                    Enabled = reader.GetInt32(5) != 0,
                    ResetOnWindowStart = reader.GetInt32(6) != 0,
                    SessionKey = reader.IsDBNull(7) ? null : reader.GetString(7),
                });
            }
        }

        foreach (var schedule in list)
            schedule.Counters = LoadCounters(schedule.Id);
        return list;
    }

    public long InsertSchedule(WorkSchedule schedule)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            INSERT INTO schedules (name, start_minutes, end_minutes, days_mask, enabled, reset_on_start, session_key)
            VALUES ($name, $start, $end, $days, $enabled, $reset, $session);
            SELECT last_insert_rowid();
            """;
        BindSchedule(command, schedule);
        return (long)command.ExecuteScalar()!;
    }

    public void UpdateSchedule(WorkSchedule schedule)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            UPDATE schedules
            SET name = $name,
                start_minutes = $start,
                end_minutes = $end,
                days_mask = $days,
                enabled = $enabled,
                reset_on_start = $reset
            WHERE id = $id;
            """;
        BindSchedule(command, schedule);
        command.Parameters.AddWithValue("$id", schedule.Id);
        command.ExecuteNonQuery();
    }

    public void SaveSession(WorkSchedule schedule)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "UPDATE schedules SET session_key = $session WHERE id = $id;";
        command.Parameters.AddWithValue("$session", (object?)schedule.SessionKey ?? DBNull.Value);
        command.Parameters.AddWithValue("$id", schedule.Id);
        command.ExecuteNonQuery();
    }

    public void DeleteSchedule(long id)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM schedules WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public long InsertCounter(Counter counter)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            INSERT INTO counters
                (schedule_id, name, interval_minutes, message, enabled, sound, elapsed_seconds, sort_order)
            VALUES
                ($schedule, $name, $interval, $message, $enabled, $sound, $elapsed, $sort);
            SELECT last_insert_rowid();
            """;
        BindCounter(command, counter);
        return (long)command.ExecuteScalar()!;
    }

    public void UpdateCounter(Counter counter)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            UPDATE counters
            SET name = $name,
                interval_minutes = $interval,
                message = $message,
                enabled = $enabled,
                sound = $sound,
                sort_order = $sort
            WHERE id = $id;
            """;
        BindCounter(command, counter);
        command.Parameters.AddWithValue("$id", counter.Id);
        command.ExecuteNonQuery();
    }

    public void DeleteCounter(long id)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM counters WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void SaveElapsed(IEnumerable<Counter> counters)
    {
        using var tx = _connection.BeginTransaction();
        using var command = _connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "UPDATE counters SET elapsed_seconds = $elapsed WHERE id = $id;";
        var elapsed = command.Parameters.Add("$elapsed", SqliteType.Integer);
        var id = command.Parameters.Add("$id", SqliteType.Integer);
        foreach (var counter in counters)
        {
            elapsed.Value = counter.ElapsedSeconds;
            id.Value = counter.Id;
            command.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
    }

    List<Counter> LoadCounters(long scheduleId)
    {
        var list = new List<Counter>();
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT id, schedule_id, name, interval_minutes, message, enabled, sound, elapsed_seconds, sort_order
            FROM counters
            WHERE schedule_id = $id
            ORDER BY sort_order, id;
            """;
        command.Parameters.AddWithValue("$id", scheduleId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Counter
            {
                Id = reader.GetInt64(0),
                ScheduleId = reader.GetInt64(1),
                Name = reader.GetString(2),
                IntervalMinutes = reader.GetInt32(3),
                Message = reader.GetString(4),
                Enabled = reader.GetInt32(5) != 0,
                Sound = reader.GetInt32(6) != 0,
                ElapsedSeconds = reader.GetInt32(7),
                SortOrder = reader.GetInt32(8),
            });
        }
        return list;
    }

    static void BindSchedule(SqliteCommand command, WorkSchedule schedule)
    {
        command.Parameters.AddWithValue("$name", schedule.Name);
        command.Parameters.AddWithValue("$start", schedule.StartMinutes);
        command.Parameters.AddWithValue("$end", schedule.EndMinutes);
        command.Parameters.AddWithValue("$days", schedule.DaysMask);
        command.Parameters.AddWithValue("$enabled", schedule.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("$reset", schedule.ResetOnWindowStart ? 1 : 0);
        command.Parameters.AddWithValue("$session", (object?)schedule.SessionKey ?? DBNull.Value);
    }

    static void BindCounter(SqliteCommand command, Counter counter)
    {
        command.Parameters.AddWithValue("$schedule", counter.ScheduleId);
        command.Parameters.AddWithValue("$name", counter.Name);
        command.Parameters.AddWithValue("$interval", counter.IntervalMinutes);
        command.Parameters.AddWithValue("$message", counter.Message);
        command.Parameters.AddWithValue("$enabled", counter.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("$sound", counter.Sound ? 1 : 0);
        command.Parameters.AddWithValue("$elapsed", counter.ElapsedSeconds);
        command.Parameters.AddWithValue("$sort", counter.SortOrder);
    }

    void Migrate()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 5000;
            CREATE TABLE IF NOT EXISTS schedules (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                start_minutes INTEGER NOT NULL,
                end_minutes INTEGER NOT NULL,
                days_mask INTEGER NOT NULL,
                enabled INTEGER NOT NULL,
                reset_on_start INTEGER NOT NULL,
                session_key TEXT
            );
            CREATE TABLE IF NOT EXISTS counters (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                schedule_id INTEGER NOT NULL REFERENCES schedules(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                interval_minutes INTEGER NOT NULL,
                message TEXT NOT NULL,
                enabled INTEGER NOT NULL,
                sound INTEGER NOT NULL,
                elapsed_seconds INTEGER NOT NULL DEFAULT 0,
                sort_order INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    void EnsureSeed()
    {
        if (GetSetting("seeded") == "1") return;
        using (var count = _connection.CreateCommand())
        {
            count.CommandText = "SELECT COUNT(*) FROM schedules;";
            var existing = (long)count.ExecuteScalar()!;
            if (existing == 0)
            {
                var schedule = new WorkSchedule
                {
                    Name = "Рабочий день",
                    StartMinutes = 9 * 60,
                    EndMinutes = 19 * 60,
                    DaysMask = 0b0011111,
                    Enabled = true,
                    ResetOnWindowStart = true,
                };
                schedule.Id = InsertSchedule(schedule);
                InsertCounter(new Counter
                {
                    ScheduleId = schedule.Id,
                    Name = "Перерыв",
                    IntervalMinutes = 45,
                    Message = "Пора сделать перерыв. Встаньте, пройдитесь, посмотрите вдаль.",
                    Enabled = true,
                    Sound = true,
                    SortOrder = 0,
                });
                InsertCounter(new Counter
                {
                    ScheduleId = schedule.Id,
                    Name = "Глаза",
                    IntervalMinutes = 20,
                    Message = "Отведите взгляд от экрана. Двадцать секунд смотрите вдаль.",
                    Enabled = true,
                    Sound = true,
                    SortOrder = 1,
                });
            }
        }
        SetSetting("seeded", "1");
    }

    string? GetSetting(string key)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT value FROM settings WHERE key = $key;";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() as string;
    }

    void SetSetting(string key, string value)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            INSERT INTO settings (key, value) VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }
}

static class Batteries
{
    static bool _ready;

    public static void Init()
    {
        if (_ready) return;
        SQLitePCL.Batteries_V2.Init();
        _ready = true;
    }
}

public static class AppData
{
    public static string DirectoryPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ritm");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string DatabasePath() => Path.Combine(DirectoryPath(), "ritm.db");

    public static void Log(Exception exception)
    {
        try
        {
            var path = Path.Combine(DirectoryPath(), "error.log");
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {exception}{Environment.NewLine}");
        }
        catch
        {
            // Logging must not take the ticker down a second time.
        }
    }
}
