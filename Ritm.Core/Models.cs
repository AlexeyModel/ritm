namespace Ritm;

public sealed class WorkSchedule
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int StartMinutes { get; set; }
    public int EndMinutes { get; set; }
    public int DaysMask { get; set; }
    public bool Enabled { get; set; } = true;
    public bool ResetOnWindowStart { get; set; } = true;
    public string? SessionKey { get; set; }
    public List<Counter> Counters { get; set; } = new();
}

public sealed class Counter
{
    public long Id { get; set; }
    public long ScheduleId { get; set; }
    public string Name { get; set; } = "";
    public int IntervalMinutes { get; set; } = 45;
    public string Message { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool Sound { get; set; } = true;
    public int ElapsedSeconds { get; set; }
    public int SortOrder { get; set; }

    public int LimitSeconds => Math.Max(1, IntervalMinutes) * 60;

    public string AlertMessage =>
        string.IsNullOrWhiteSpace(Message) ? "Пора сделать перерыв." : Message.Trim();
}
