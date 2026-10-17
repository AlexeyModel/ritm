using Ritm;

var failures = new List<string>();

void Expect(bool condition, string message)
{
    if (!condition) failures.Add(message);
}

var thursday = new DateTime(2026, 1, 1);
while (thursday.DayOfWeek != DayOfWeek.Thursday) thursday = thursday.AddDays(1);

var office = new WorkSchedule
{
    Name = "Офис",
    StartMinutes = 9 * 60,
    EndMinutes = 19 * 60,
    DaysMask = WindowClock.DayBit(DayOfWeek.Thursday),
    Enabled = true,
    ResetOnWindowStart = true,
};

Expect(WindowClock.Contains(office, thursday.AddHours(8).AddMinutes(59)) == false, "08:59 outside");
Expect(WindowClock.Contains(office, thursday.AddHours(9)), "09:00 inside");
Expect(WindowClock.Contains(office, thursday.AddHours(18).AddMinutes(59)), "18:59 inside");
Expect(WindowClock.Contains(office, thursday.AddHours(19)) == false, "19:00 outside");
Expect(WindowClock.Contains(office, thursday.AddDays(1).AddHours(12)) == false, "friday outside");

var overnight = new WorkSchedule
{
    Name = "Ночь",
    StartMinutes = 22 * 60,
    EndMinutes = 6 * 60,
    DaysMask = WindowClock.DayBit(DayOfWeek.Thursday),
    Enabled = true,
};
Expect(WindowClock.Contains(overnight, thursday.AddHours(23)), "thu 23:00 inside overnight");
Expect(WindowClock.Contains(overnight, thursday.AddDays(1).AddHours(5)), "fri 05:00 still thursday window");
Expect(WindowClock.Contains(overnight, thursday.AddDays(1).AddHours(12)) == false, "fri noon outside");
Expect(WindowClock.Contains(overnight, thursday.AddDays(1).AddHours(23)) == false, "fri 23:00 not selected");
Expect(WindowClock.SessionKey(overnight, thursday.AddHours(23)) == thursday.ToString("yyyy-MM-dd"), "session evening");
Expect(WindowClock.SessionKey(overnight, thursday.AddDays(1).AddHours(5)) == thursday.ToString("yyyy-MM-dd"), "session after midnight");

var counter = new Counter { Name = "Перерыв", IntervalMinutes = 45, Enabled = true, Message = "Встаньте." };
office.Counters.Add(counter);
var inside = thursday.AddHours(10);
var step = Ticker.Advance([office], inside, 1, paused: false);
Expect(step.Alerts.Count == 0, "first second does not alert");
Expect(counter.ElapsedSeconds == 1, "elapsed is 1");
Expect(step.SessionsStarted.Count == 1, "session started");
Expect(office.SessionKey == thursday.ToString("yyyy-MM-dd"), "session key stored");

counter.ElapsedSeconds = 45 * 60 - 1;
step = Ticker.Advance([office], inside, 1, paused: false);
Expect(step.Alerts.Count == 1 && step.Alerts[0].Counter == counter, "45 minutes fires");
Expect(counter.ElapsedSeconds == 0, "counter restarts");
Expect(step.SessionsStarted.Count == 0, "same session does not reset");

var eyes = new Counter { Name = "Глаза", IntervalMinutes = 20, Enabled = true, ElapsedSeconds = 10 };
office.Counters.Add(eyes);
counter.ElapsedSeconds = 10;
step = Ticker.Advance([office], inside, 5, paused: false);
Expect(counter.ElapsedSeconds == 15 && eyes.ElapsedSeconds == 15, "both counters advance");
Expect(step.Alerts.Count == 0, "no alert yet");

eyes.ElapsedSeconds = 20 * 60 - 2;
counter.ElapsedSeconds = 3;
step = Ticker.Advance([office], inside, 5, paused: false);
Expect(step.Alerts.Count == 1 && step.Alerts[0].Counter == eyes, "only the due counter fires");
Expect(eyes.ElapsedSeconds == 3, "remainder kept");
Expect(counter.ElapsedSeconds == 8, "other counter untouched by the alert");

var before = counter.ElapsedSeconds;
step = Ticker.Advance([office], thursday.AddHours(20), 5, paused: false);
Expect(counter.ElapsedSeconds == before && step.Alerts.Count == 0, "outside window the counter stands");

step = Ticker.Advance([office], inside, 5, paused: true);
Expect(counter.ElapsedSeconds == before && step.Alerts.Count == 0, "pause freezes");

office.SessionKey = "1999-01-01";
counter.ElapsedSeconds = 100;
eyes.Enabled = false;
eyes.ElapsedSeconds = 50;
step = Ticker.Advance([office], inside.AddDays(7), 1, paused: false);
var nextThursday = inside.AddDays(7);
Expect(nextThursday.DayOfWeek == DayOfWeek.Thursday, "fixture stayed on thursday");
Expect(counter.ElapsedSeconds == 1, "new window resets then counts one second");
Expect(eyes.ElapsedSeconds == 0, "disabled counter resets but does not run");
Expect(step.Alerts.Count == 0, "reset does not alert");

office.ResetOnWindowStart = false;
office.SessionKey = "1999-01-01";
counter.ElapsedSeconds = 100;
step = Ticker.Advance([office], nextThursday, 1, paused: false);
Expect(counter.ElapsedSeconds == 101, "without reset the counter continues");

counter.ElapsedSeconds = 30;
step = Ticker.Advance([office], nextThursday, 10_000, paused: false);
Expect(counter.ElapsedSeconds == 31, "long stall counts as one second, not a burst");

office.Enabled = false;
counter.ElapsedSeconds = 10;
step = Ticker.Advance([office], nextThursday, 1, paused: false);
Expect(counter.ElapsedSeconds == 10, "disabled schedule does not run");

var dir = Path.Combine(Path.GetTempPath(), "ritm-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(dir);
var dbPath = Path.Combine(dir, "ritm.db");
using (var db = new Database(dbPath))
{
    var loaded = db.LoadAll();
    Expect(loaded.Count == 1, "seed schedule");
    Expect(loaded[0].Name == "Рабочий день", "seed name");
    Expect(loaded[0].StartMinutes == 9 * 60 && loaded[0].EndMinutes == 19 * 60, "seed window");
    Expect(loaded[0].Counters.Count == 2, "two seed counters");
    Expect(loaded[0].Counters[0].IntervalMinutes == 45, "break is 45 minutes");
    Expect(loaded[0].Counters[0].Message.Contains("перерыв", StringComparison.OrdinalIgnoreCase), "message is on the counter");

    loaded[0].Name = "Смена";
    loaded[0].EndMinutes = 18 * 60;
    db.UpdateSchedule(loaded[0]);
    loaded[0].Counters[0].ElapsedSeconds = 90;
    loaded[0].Counters[0].Message = "Вода.";
    db.UpdateCounter(loaded[0].Counters[0]);
    db.SaveElapsed(loaded[0].Counters);
    loaded[0].SessionKey = "2026-10-15";
    db.SaveSession(loaded[0]);
}
using (var db = new Database(dbPath))
{
    var loaded = db.LoadAll();
    Expect(loaded.Count == 1 && loaded[0].Name == "Смена", "schedule persisted");
    Expect(loaded[0].EndMinutes == 18 * 60, "end persisted");
    Expect(loaded[0].Counters[0].ElapsedSeconds == 90, "elapsed persisted");
    Expect(loaded[0].Counters[0].Message == "Вода.", "message persisted");
    Expect(loaded[0].SessionKey == "2026-10-15", "session persisted");
    Expect(loaded[0].Counters.Count == 2, "seed not duplicated");
    db.DeleteCounter(loaded[0].Counters[1].Id);
    db.DeleteSchedule(loaded[0].Id);
}
using (var db = new Database(dbPath))
{
    Expect(db.LoadAll().Count == 0, "delete cascades and does not reseed");
}

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}

var langPath = Path.Combine(Path.GetTempPath(), "ritm-lang-" + Guid.NewGuid().ToString("n") + ".db");
using (var langDb = new Database(langPath))
{
    Expect(Loc.Language == AppLanguage.Ru, "default language is Russian");
    Expect(Loc.T("menu.language") == "Язык", "russian language menu");
    Expect(WindowClock.FormatDays(0b0011111) == "пн–пт", "russian weekdays");
    Loc.Select(AppLanguage.En, langDb);
    Expect(Loc.T("menu.language") == "Language", "english language menu");
    Expect(WindowClock.FormatDays(0b0011111) == "Mon–Fri", "english weekdays");
    Expect(WindowClock.FormatDuration(12) == "12 s", "english seconds");
    Expect(langDb.GetSetting("language") == "en", "language stored");
    Loc.Select(AppLanguage.De, langDb);
    Expect(Loc.T("menu.language") == "Sprache", "german language menu");
    Expect(WindowClock.FormatDays(0b1100000) == "Sa–So", "german weekend");
    Expect(new Counter { Message = "" }.AlertMessage == "Zeit für eine Pause.", "german fallback message");
    Loc.Select(AppLanguage.Ru, langDb);
    Expect(Loc.T("days.every") == "каждый день", "language restored");
}
File.Delete(langPath);

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}

Console.WriteLine("ritm checks ok");
return 0;
