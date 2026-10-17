namespace Ritm;

public enum AppLanguage
{
    Ru = 0,
    En = 1,
    De = 2,
}

/// <summary>
/// UI strings. Each row is Russian, English, German.
/// Russian is the default. Read a row only through Loc.T.
/// </summary>
public static class Loc
{
    public static event Action? Changed;

    public static AppLanguage Language { get; private set; } = AppLanguage.Ru;

    static readonly Dictionary<string, string[]> Map = new()
    {
        ["app.name"] = ["Ритм", "Ritm", "Ritm"],
        ["app.already"] = [
            "Ритм уже запущен. Значок находится у часов, в трее.",
            "Ritm is already running. The icon is by the clock, in the tray.",
            "Ritm läuft bereits. Das Symbol liegt bei der Uhr, im Infobereich.",
        ],
        ["app.failed"] = [
            "Ритм — не удалось запустить",
            "Ritm — could not start",
            "Ritm — Start fehlgeschlagen",
        ],
        ["menu.open"] = ["Открыть", "Open", "Öffnen"],
        ["menu.pause"] = ["Приостановить", "Pause", "Anhalten"],
        ["menu.resume"] = ["Продолжить", "Resume", "Fortsetzen"],
        ["menu.autostart"] = [
            "Автозапуск при входе в Windows",
            "Start with Windows",
            "Mit Windows starten",
        ],
        ["menu.exit"] = ["Выход", "Exit", "Beenden"],
        ["menu.window"] = ["Окно", "Window", "Fenster"],
        ["menu.language"] = ["Язык", "Language", "Sprache"],
        ["tray.hidden"] = [
            "Окно закрыто. Счётчики продолжают идти в трее у часов.",
            "The window is closed. Counters keep running in the tray by the clock.",
            "Das Fenster ist geschlossen. Die Zähler laufen im Infobereich bei der Uhr weiter.",
        ],
        ["autostart.open.failed"] = [
            "Не удалось открыть автозагрузку Windows.",
            "Could not open the Windows startup key.",
            "Der Windows-Autostart konnte nicht geöffnet werden.",
        ],
        ["button.pause"] = ["Пауза", "Pause", "Pause"],
        ["button.resume"] = ["Продолжить", "Resume", "Fortsetzen"],
        ["subtitle"] = [
            "Режим — окно времени. Внутри него несколько счётчиков, каждый со своим сообщением.",
            "A schedule is a time window. Inside it, several counters, each with its own message.",
            "Ein Modus ist ein Zeitfenster. Darin liegen mehrere Zähler, jeder mit einer eigenen Meldung.",
        ],
        ["button.data"] = ["Папка данных", "Data folder", "Datenordner"],
        ["button.new.schedule"] = ["Новый режим", "New schedule", "Neuer Modus"],
        ["button.delete"] = ["Удалить", "Delete", "Löschen"],
        ["caption.schedules"] = ["РЕЖИМЫ", "SCHEDULES", "MODI"],
        ["empty"] = [
            "Добавьте режим — например, с 9:00 до 19:00.\nВнутри него заведите счётчики в минутах.",
            "Add a schedule — for example, from 9:00 to 19:00.\nInside it, add counters in minutes.",
            "Fügen Sie einen Modus hinzu — zum Beispiel von 9:00 bis 19:00.\nLegen Sie darin Zähler in Minuten an.",
        ],
        ["label.schedule"] = ["Режим", "Schedule", "Modus"],
        ["check.enabled"] = ["Включён", "Enabled", "Aktiv"],
        ["label.from"] = ["С", "From", "Von"],
        ["label.to"] = ["до", "to", "bis"],
        ["button.reset.counters"] = ["Сбросить счётчики", "Reset counters", "Zähler zurücksetzen"],
        ["check.reset.window"] = [
            "Обнулять в начале окна",
            "Reset at the start of the window",
            "Zu Beginn des Fensters auf null",
        ],
        ["label.counters"] = ["Счётчики", "Counters", "Zähler"],
        ["button.new.counter"] = ["Новый счётчик", "New counter", "Neuer Zähler"],
        ["hint.no.days"] = [
            "Дни не выбраны — режим не включится.",
            "No days selected — the schedule will not run.",
            "Keine Tage gewählt — der Modus startet nicht.",
        ],
        ["hint.empty.window"] = [
            "Начало и конец совпали — окно пустое.",
            "Start and end are the same — the window is empty.",
            "Beginn und Ende sind gleich — das Fenster ist leer.",
        ],
        ["hint.overnight"] = ["Через полночь.  {0}", "Across midnight.  {0}", "Über Mitternacht.  {0}"],
        ["hint.inside"] = [
            "{0}  ·  счётчики идут только здесь",
            "{0}  ·  counters run only here",
            "{0}  ·  Zähler laufen nur hier",
        ],
        ["default.untitled"] = ["Без названия", "Untitled", "Ohne Namen"],
        ["default.new.schedule"] = ["Новый режим", "New schedule", "Neuer Modus"],
        ["confirm.delete"] = [
            "Удалить режим «{0}» и все его счётчики?",
            "Delete the schedule “{0}” and all of its counters?",
            "Modus „{0}“ und alle seine Zähler löschen?",
        ],
        ["default.break"] = ["Перерыв", "Break", "Pause"],
        ["seed.workday"] = ["Рабочий день", "Work day", "Arbeitstag"],
        ["seed.eyes"] = ["Глаза", "Eyes", "Augen"],
        ["seed.break.message"] = [
            "Пора сделать перерыв. Встаньте, пройдитесь, посмотрите вдаль.",
            "Time for a break. Stand up, walk a little, look into the distance.",
            "Zeit für eine Pause. Stehen Sie auf, gehen Sie ein paar Schritte, schauen Sie in die Ferne.",
        ],
        ["seed.eyes.message"] = [
            "Отведите взгляд от экрана. Двадцать секунд смотрите вдаль.",
            "Look away from the screen. For twenty seconds, look into the distance.",
            "Nehmen Sie den Blick vom Bildschirm. Schauen Sie zwanzig Sekunden in die Ferne.",
        ],
        ["default.break.message"] = [
            "Пора сделать перерыв. Встаньте и отойдите от стола.",
            "Time for a break. Stand up and step away from the desk.",
            "Zeit für eine Pause. Stehen Sie auf und gehen Sie vom Tisch weg.",
        ],
        ["error.tick"] = [
            "Сбой отсчёта. Подробности в error.log в папке данных. Перезапустите Ритм.",
            "The timer failed. Details are in error.log in the data folder. Restart Ritm.",
            "Der Zähler ist ausgefallen. Einzelheiten stehen in error.log im Datenordner. Starten Sie Ritm neu.",
        ],
        ["status.pause.tip"] = ["Ритм · пауза", "Ritm · paused", "Ritm · Pause"],
        ["status.pause"] = [
            "Пауза. Окна не проверяются, счётчики стоят.",
            "Paused. Windows are not checked, counters stand still.",
            "Pause. Fenster werden nicht geprüft, die Zähler stehen.",
        ],
        ["status.outside.tip"] = ["Ритм · вне окна", "Ritm · outside the window", "Ritm · außerhalb des Fensters"],
        ["status.outside"] = [
            "{0}   ·   Вне всех режимов — счётчики стоят.",
            "{0}   ·   Outside every schedule — counters stand still.",
            "{0}   ·   Außerhalb aller Modi — die Zähler stehen.",
        ],
        ["status.none.tip"] = ["Ритм · нет счётчиков", "Ritm · no counters", "Ritm · keine Zähler"],
        ["status.none"] = [
            "{0}   ·   Окно открыто, включённых счётчиков нет.",
            "{0}   ·   A window is open, no counters are enabled.",
            "{0}   ·   Ein Fenster ist offen, kein Zähler ist eingeschaltet.",
        ],
        ["status.next.tip"] = ["Ритм · {0} через {1}", "Ritm · {0} in {1}", "Ritm · {0} in {1}"],
        ["status.next"] = [
            "{0}   ·   {1}: «{2}» через {3}",
            "{0}   ·   {1}: “{2}” in {3}",
            "{0}   ·   {1}: „{2}“ in {3}",
        ],
        ["state.off"] = ["выключен", "off", "aus"],
        ["state.pause"] = ["пауза", "paused", "Pause"],
        ["state.live"] = ["сейчас идёт", "running now", "läuft jetzt"],
        ["state.outside"] = ["вне окна", "outside", "außerhalb"],
        ["unit.min"] = ["мин", "min", "Min"],
        ["unit.sec"] = ["с", "s", "s"],
        ["check.on"] = ["вкл", "on", "ein"],
        ["check.sound"] = ["звук", "sound", "Ton"],
        ["button.test"] = ["Проверить", "Test", "Prüfen"],
        ["progress"] = [
            "прошло {0} из {1}   ·   ещё {2}",
            "{0} of {1} elapsed   ·   {2} left",
            "{0} von {1} vergangen   ·   noch {2}",
        ],
        ["cue.message"] = [
            "Сообщение, которое появится вместе со звуком",
            "The message that appears with the sound",
            "Die Meldung, die zusammen mit dem Ton erscheint",
        ],
        ["default.counter"] = ["Счётчик", "Counter", "Zähler"],
        ["alert.ok"] = ["Понятно", "OK", "Verstanden"],
        ["alert.snooze"] = ["Отложить на 5 мин", "Snooze 5 min", "5 Min. verschieben"],
        ["alert.default"] = ["Пора сделать перерыв.", "Time for a break.", "Zeit für eine Pause."],
        ["days.none"] = ["дни не выбраны", "no days selected", "keine Tage gewählt"],
        ["days.weekdays"] = ["пн–пт", "Mon–Fri", "Mo–Fr"],
        ["days.weekend"] = ["сб–вс", "Sat–Sun", "Sa–So"],
        ["days.every"] = ["каждый день", "every day", "jeden Tag"],
        ["day.short.0"] = ["пн", "Mon", "Mo"],
        ["day.short.1"] = ["вт", "Tue", "Di"],
        ["day.short.2"] = ["ср", "Wed", "Mi"],
        ["day.short.3"] = ["чт", "Thu", "Do"],
        ["day.short.4"] = ["пт", "Fri", "Fr"],
        ["day.short.5"] = ["сб", "Sat", "Sa"],
        ["day.short.6"] = ["вс", "Sun", "So"],
        ["day.0"] = ["Пн", "Mon", "Mo"],
        ["day.1"] = ["Вт", "Tue", "Di"],
        ["day.2"] = ["Ср", "Wed", "Mi"],
        ["day.3"] = ["Чт", "Thu", "Do"],
        ["day.4"] = ["Пт", "Fri", "Fr"],
        ["day.5"] = ["Сб", "Sat", "Sa"],
        ["day.6"] = ["Вс", "Sun", "So"],
    };

    public static string T(string key)
    {
        if (!Map.TryGetValue(key, out var row)) return key;
        var index = (int)Language;
        if (index < 0 || index >= row.Length) index = 0;
        return row[index];
    }

    public static string T(string key, params object?[] args) =>
        string.Format(T(key), args);

    public static void Load()
    {
        try
        {
            var path = LanguagePath();
            if (!File.Exists(path)) return;
            Language = Parse(File.ReadAllText(path));
        }
        catch
        {
            Language = AppLanguage.Ru;
        }
    }

    public static void UseDatabase(Database database)
    {
        var stored = database.GetSetting("language");
        if (string.IsNullOrWhiteSpace(stored))
        {
            database.SetSetting("language", Code(Language));
            SaveFile();
            return;
        }

        var parsed = Parse(stored);
        SaveFile(parsed);
        if (parsed == Language) return;
        Language = parsed;
        Changed?.Invoke();
    }

    public static void Select(AppLanguage language, Database database)
    {
        database.SetSetting("language", Code(language));
        SaveFile(language);
        if (language == Language) return;
        Language = language;
        Changed?.Invoke();
    }

    public static AppLanguage Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "en" or "english" => AppLanguage.En,
        "de" or "deutsch" or "german" => AppLanguage.De,
        _ => AppLanguage.Ru,
    };

    public static string Code(AppLanguage language) => language switch
    {
        AppLanguage.En => "en",
        AppLanguage.De => "de",
        _ => "ru",
    };

    static void SaveFile() => SaveFile(Language);

    static void SaveFile(AppLanguage language)
    {
        try
        {
            File.WriteAllText(LanguagePath(), Code(language));
        }
        catch
        {
            // The menu can still switch the language for this run.
        }
    }

    static string LanguagePath() => Path.Combine(AppData.DirectoryPath(), "language.txt");
}
