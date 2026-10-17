namespace Ritm;

public sealed class DueAlert
{
    public required WorkSchedule Schedule { get; init; }
    public required Counter Counter { get; init; }
}

public sealed class TickOutcome
{
    public List<DueAlert> Alerts { get; } = new();
    public List<WorkSchedule> SessionsStarted { get; } = new();
}

public static class Ticker
{
    public static TickOutcome Advance(IReadOnlyList<WorkSchedule> schedules, DateTime now, int seconds, bool paused)
    {
        var outcome = new TickOutcome();
        if (paused || seconds <= 0) return outcome;
        if (seconds > 5) seconds = 1;

        foreach (var schedule in schedules)
        {
            if (!schedule.Enabled) continue;
            if (!WindowClock.Contains(schedule, now)) continue;

            if (schedule.ResetOnWindowStart)
            {
                var key = WindowClock.SessionKey(schedule, now);
                if (!string.Equals(schedule.SessionKey, key, StringComparison.Ordinal))
                {
                    schedule.SessionKey = key;
                    foreach (var counter in schedule.Counters)
                        counter.ElapsedSeconds = 0;
                    outcome.SessionsStarted.Add(schedule);
                }
            }

            foreach (var counter in schedule.Counters)
            {
                if (!counter.Enabled) continue;
                int limit = counter.LimitSeconds;
                if (counter.ElapsedSeconds < 0) counter.ElapsedSeconds = 0;
                if (counter.ElapsedSeconds >= limit) counter.ElapsedSeconds %= limit;

                int next = counter.ElapsedSeconds + seconds;
                if (next >= limit)
                {
                    counter.ElapsedSeconds = next % limit;
                    outcome.Alerts.Add(new DueAlert { Schedule = schedule, Counter = counter });
                }
                else
                {
                    counter.ElapsedSeconds = next;
                }
            }
        }

        return outcome;
    }
}
