namespace Ritm;

public static class WindowClock
{
    public static int DayBit(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => 1 << 0,
        DayOfWeek.Tuesday => 1 << 1,
        DayOfWeek.Wednesday => 1 << 2,
        DayOfWeek.Thursday => 1 << 3,
        DayOfWeek.Friday => 1 << 4,
        DayOfWeek.Saturday => 1 << 5,
        DayOfWeek.Sunday => 1 << 6,
        _ => 0,
    };

    public static int ClockMinutes(DateTime time) => time.Hour * 60 + time.Minute;

    public static DateTime TodayAt(int minutes)
    {
        minutes = Math.Clamp(minutes, 0, (24 * 60) - 1);
        return DateTime.Today.AddMinutes(minutes);
    }

    public static string FormatMinutes(int minutes)
    {
        minutes = ((minutes % 1440) + 1440) % 1440;
        return $"{minutes / 60:00}:{minutes % 60:00}";
    }

    public static string FormatDuration(int seconds)
    {
        if (seconds < 0) seconds = 0;
        if (seconds < 60) return $"{seconds} с";
        int hours = seconds / 3600;
        int mins = (seconds % 3600) / 60;
        int secs = seconds % 60;
        return hours > 0 ? $"{hours}:{mins:00}:{secs:00}" : $"{mins}:{secs:00}";
    }

    public static string FormatRange(WorkSchedule schedule) =>
        $"{FormatMinutes(schedule.StartMinutes)}–{FormatMinutes(schedule.EndMinutes)}";

    public static string FormatDays(int mask)
    {
        if (mask == 0) return "дни не выбраны";
        if (mask == 0b0011111) return "пн–пт";
        if (mask == 0b1100000) return "сб–вс";
        if ((mask & 0b1111111) == 0b1111111) return "каждый день";
        string[] names = ["пн", "вт", "ср", "чт", "пт", "сб", "вс"];
        var parts = new List<string>(7);
        for (var i = 0; i < 7; i++)
        {
            if ((mask & (1 << i)) != 0) parts.Add(names[i]);
        }
        return string.Join(", ", parts);
    }

    public static bool Contains(WorkSchedule schedule, DateTime now)
    {
        if (schedule.DaysMask == 0) return false;
        if (schedule.StartMinutes == schedule.EndMinutes) return false;

        int nowMin = ClockMinutes(now);
        if (schedule.StartMinutes < schedule.EndMinutes)
        {
            if ((schedule.DaysMask & DayBit(now.DayOfWeek)) == 0) return false;
            return nowMin >= schedule.StartMinutes && nowMin < schedule.EndMinutes;
        }

        if (nowMin >= schedule.StartMinutes)
            return (schedule.DaysMask & DayBit(now.DayOfWeek)) != 0;

        if (nowMin < schedule.EndMinutes)
            return (schedule.DaysMask & DayBit(now.AddDays(-1).DayOfWeek)) != 0;

        return false;
    }

    public static string SessionKey(WorkSchedule schedule, DateTime now)
    {
        if (schedule.StartMinutes < schedule.EndMinutes)
            return now.ToString("yyyy-MM-dd");

        int nowMin = ClockMinutes(now);
        var day = nowMin >= schedule.StartMinutes ? now : now.AddDays(-1);
        return day.ToString("yyyy-MM-dd");
    }
}
