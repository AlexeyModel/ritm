using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace Ritm;

sealed class MainForm : Form
{
    readonly Database _database;
    readonly List<WorkSchedule> _schedules;
    readonly System.Windows.Forms.Timer _timer;
    readonly ListBox _list = new();
    readonly TextBox _name = Theme.MakeField();
    readonly DateTimePicker _from = Theme.MakeTime();
    readonly DateTimePicker _to = Theme.MakeTime();
    readonly CheckBox _enabled = new();
    readonly CheckBox _reset = new();
    readonly CheckBox[] _days = new CheckBox[7];
    readonly Label _hint = new();
    readonly Label _windowState = new();
    readonly FlowLayoutPanel _flow = new();
    readonly Panel _editor = new();
    readonly Label _empty = new();
    readonly Label _clock = new();
    readonly Label _status = new();
    readonly Button _pause;
    readonly ToolStripMenuItem _autostartMenu = new("Автозапуск при входе в Windows");

    WorkSchedule? _selected;
    bool _binding;
    bool _paused;
    bool _shuttingDown;
    bool _hinted;
    double _bank;
    long _stamp;
    int _persistEvery;
    DateTime _alertUntil = DateTime.MinValue;

    public event Action<string, IconKind>? TrayUpdate;
    public event Action<bool>? PauseStateChanged;
    public event Action? HideToTrayHint;

    public MainForm(Database database)
    {
        _database = database;
        _schedules = database.LoadAll();
        _pause = Theme.MakeButton("Пауза", false);
        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += OnTick;

        DoubleBuffered = true;
        Text = "Ритм";
        Font = Theme.Ui;
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1080, 720);
        MinimumSize = new Size(920, 600);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;

        Build();
        LoadList();
        _pause.Click += (_, _) => TogglePause();
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    public void StartEngine()
    {
        if (_timer.Enabled) return;
        _stamp = Stopwatch.GetTimestamp();
        _timer.Start();
        RefreshLive();
    }

    public void TogglePause()
    {
        _paused = !_paused;
        _pause.Text = _paused ? "Продолжить" : "Пауза";
        _stamp = Stopwatch.GetTimestamp();
        _bank = 0;
        PauseStateChanged?.Invoke(_paused);
        RefreshLive();
    }

    public bool ToggleAutostart()
    {
        WindowsAutostart.SetEnabled(!WindowsAutostart.IsEnabled());
        RefreshAutostartCheck();
        return WindowsAutostart.IsEnabled();
    }

    public void RefreshAutostartCheck()
    {
        _autostartMenu.Checked = WindowsAutostart.IsEnabled();
    }

    public void Shutdown()
    {
        if (_shuttingDown || IsDisposed) return;
        _shuttingDown = true;
        Close();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        FitCards();
        StartEngine();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
            HideToTray();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_shuttingDown && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        _shuttingDown = true;
        _timer.Stop();
        PersistAll();
        base.OnFormClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) HideToTray();
    }

    void HideToTray()
    {
        Hide();
        if (_hinted) return;
        _hinted = true;
        HideToTrayHint?.Invoke();
    }

    void Build()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Theme.Bg, Padding = new Padding(20, 12, 16, 8) };
        var rule = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Line };
        var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Theme.Panel };
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(16, 12, 16, 12) };

        _pause.Dock = DockStyle.Right;
        _pause.Width = 128;
        _clock.Dock = DockStyle.Right;
        _clock.Width = 120;
        _clock.TextAlign = ContentAlignment.MiddleRight;
        _clock.Font = Theme.Clock;
        _clock.ForeColor = Theme.Ink;
        _clock.Text = "--:--:--";
        var titles = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
        var title = new Label
        {
            Text = "Ритм",
            Dock = DockStyle.Top,
            Height = 32,
            Font = Theme.Title,
            ForeColor = Theme.Ink,
        };
        var subtitle = new Label
        {
            Text = "Режим — окно времени. Внутри него несколько счётчиков, каждый со своим сообщением.",
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Theme.Muted,
            Font = Theme.Small,
        };
        titles.Controls.Add(subtitle);
        titles.Controls.Add(title);
        header.Controls.Add(titles);
        header.Controls.Add(_clock);
        header.Controls.Add(_pause);

        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Padding = new Padding(16, 0, 8, 0);
        _status.ForeColor = Theme.Muted;
        _status.Font = Theme.Small;
        var data = Theme.MakeButton("Папка данных", false);
        data.Dock = DockStyle.Right;
        data.Width = 140;
        data.Click += (_, _) => OpenDataFolder();
        statusBar.Controls.Add(_status);
        statusBar.Controls.Add(data);

        var left = new Panel { Dock = DockStyle.Left, Width = 308, BackColor = Theme.Panel, Padding = new Padding(12, 10, 12, 10) };
        var gap = new Panel { Dock = DockStyle.Left, Width = 14, BackColor = Theme.Bg };
        var right = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };

        var buttons = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Theme.Panel };
        var addMode = Theme.MakeButton("Новый режим", true);
        addMode.Dock = DockStyle.Left;
        addMode.Width = 148;
        var deleteMode = Theme.MakeButton("Удалить", false);
        deleteMode.Dock = DockStyle.Right;
        deleteMode.Width = 104;
        deleteMode.ForeColor = Theme.Clay;
        addMode.Click += (_, _) => AddSchedule();
        deleteMode.Click += (_, _) => DeleteSchedule();
        buttons.Controls.Add(addMode);
        buttons.Controls.Add(deleteMode);

        var caption = new Label
        {
            Text = "РЕЖИМЫ",
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Theme.Muted,
            Font = Theme.Small,
            BackColor = Theme.Panel,
        };
        _list.Dock = DockStyle.Fill;
        _list.BorderStyle = BorderStyle.None;
        _list.DrawMode = DrawMode.OwnerDrawFixed;
        _list.ItemHeight = 58;
        _list.IntegralHeight = false;
        _list.BackColor = Theme.Panel;
        _list.ForeColor = Theme.Ink;
        _list.DrawItem += DrawModeItem;
        _list.SelectedIndexChanged += (_, _) =>
        {
            if (!_binding) BindSelected();
        };
        left.Controls.Add(_list);
        left.Controls.Add(buttons);
        left.Controls.Add(caption);

        _empty.Dock = DockStyle.Fill;
        _empty.TextAlign = ContentAlignment.MiddleCenter;
        _empty.ForeColor = Theme.Muted;
        _empty.Text = "Добавьте режим — например, с 9:00 до 19:00.\nВнутри него заведите счётчики в минутах.";
        _empty.Visible = false;

        BuildEditor();
        right.Controls.Add(_editor);
        right.Controls.Add(_empty);

        body.Controls.Add(right);
        body.Controls.Add(gap);
        body.Controls.Add(left);

        Controls.Add(body);
        Controls.Add(statusBar);
        Controls.Add(rule);
        Controls.Add(header);

        var windowMenu = new ToolStripMenuItem("Окно");
        _autostartMenu.Click += (_, _) =>
        {
            try
            {
                ToggleAutostart();
            }
            catch (Exception ex)
            {
                AppData.Log(ex);
                RefreshAutostartCheck();
                MessageBox.Show(ex.Message, "Ритм", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        windowMenu.DropDownOpening += (_, _) => RefreshAutostartCheck();
        windowMenu.DropDownItems.Add(_autostartMenu);
        var bar = new MenuStrip
        {
            BackColor = Theme.Bg,
            ForeColor = Theme.Ink,
            Font = Theme.Ui,
            Dock = DockStyle.Top,
        };
        bar.Items.Add(windowMenu);
        MainMenuStrip = bar;
        RefreshAutostartCheck();
        Controls.Add(bar);
    }

    void BuildEditor()
    {
        _editor.Dock = DockStyle.Fill;
        _editor.BackColor = Theme.Bg;

        var head = new Panel { Dock = DockStyle.Top, Height = 168, BackColor = Theme.Bg };
        var nameRow = Row(40);
        var timeRow = Row(40);
        var dayRow = Row(40);
        var optionRow = Row(36);

        var nameLabel = Inline("Режим", 64);
        _name.Dock = DockStyle.Fill;
        _enabled.Text = "Включён";
        _enabled.Dock = DockStyle.Right;
        _enabled.Width = 100;
        _enabled.TextAlign = ContentAlignment.MiddleRight;
        _enabled.ForeColor = Theme.Ink;
        _enabled.BackColor = Theme.Bg;
        _enabled.CheckedChanged += (_, _) => SaveSchedule(false);
        _name.TextChanged += (_, _) => SaveSchedule(false);
        _name.Leave += (_, _) => SaveSchedule(true);
        nameRow.Controls.Add(_name);
        nameRow.Controls.Add(_enabled);
        nameRow.Controls.Add(nameLabel);

        var fromLabel = Inline("С", 24);
        var toLabel = Inline("до", 32);
        _from.Dock = DockStyle.Left;
        _to.Dock = DockStyle.Left;
        _from.ValueChanged += (_, _) => SaveSchedule(false);
        _to.ValueChanged += (_, _) => SaveSchedule(false);
        var resetButton = Theme.MakeButton("Сбросить счётчики", false);
        resetButton.Dock = DockStyle.Right;
        resetButton.Width = 168;
        resetButton.Click += (_, _) => ResetCounters();
        toLabel.Padding = new Padding(10, 0, 0, 0);
        toLabel.Width = 42;
        timeRow.Controls.Add(resetButton);
        timeRow.Controls.Add(_to);
        timeRow.Controls.Add(toLabel);
        timeRow.Controls.Add(_from);
        timeRow.Controls.Add(fromLabel);

        var dayFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Bg,
        };
        string[] dayNames = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"];
        for (var i = 0; i < dayNames.Length; i++)
        {
            var box = new CheckBox
            {
                Text = dayNames[i],
                AutoSize = true,
                Margin = new Padding(0, 8, 16, 0),
                ForeColor = Theme.Ink,
                BackColor = Theme.Bg,
                Font = Theme.Ui,
            };
            box.CheckedChanged += (_, _) => SaveSchedule(false);
            _days[i] = box;
            dayFlow.Controls.Add(box);
        }
        dayRow.Controls.Add(dayFlow);

        _reset.Text = "Обнулять в начале окна";
        _reset.AutoSize = false;
        _reset.Width = 230;
        _reset.Dock = DockStyle.Left;
        _reset.TextAlign = ContentAlignment.MiddleLeft;
        _reset.ForeColor = Theme.Ink;
        _reset.BackColor = Theme.Bg;
        _reset.CheckedChanged += (_, _) => SaveSchedule(false);
        _windowState.Dock = DockStyle.Right;
        _windowState.Width = 120;
        _windowState.TextAlign = ContentAlignment.MiddleRight;
        _windowState.Font = Theme.UiBold;
        _windowState.ForeColor = Theme.Pine;
        _hint.Dock = DockStyle.Fill;
        _hint.TextAlign = ContentAlignment.MiddleLeft;
        _hint.ForeColor = Theme.Muted;
        _hint.Font = Theme.Small;
        _hint.Padding = new Padding(16, 0, 0, 0);
        optionRow.Controls.Add(_hint);
        optionRow.Controls.Add(_windowState);
        optionRow.Controls.Add(_reset);

        head.Controls.Add(optionRow);
        head.Controls.Add(dayRow);
        head.Controls.Add(timeRow);
        head.Controls.Add(nameRow);

        var counterHead = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Theme.Bg };
        var counterTitle = new Label
        {
            Text = "Счётчики",
            Dock = DockStyle.Left,
            Width = 140,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.UiBold,
            ForeColor = Theme.Ink,
        };
        var addCounter = Theme.MakeButton("Новый счётчик", true);
        addCounter.Dock = DockStyle.Right;
        addCounter.Width = 160;
        addCounter.Click += (_, _) => AddCounter();
        counterHead.Controls.Add(counterTitle);
        counterHead.Controls.Add(addCounter);

        _flow.Dock = DockStyle.Fill;
        _flow.FlowDirection = FlowDirection.TopDown;
        _flow.WrapContents = false;
        _flow.AutoScroll = true;
        _flow.BackColor = Theme.Bg;
        _flow.Padding = new Padding(0, 4, 0, 0);
        _flow.SizeChanged += (_, _) => FitCards();

        _editor.Controls.Add(_flow);
        _editor.Controls.Add(counterHead);
        _editor.Controls.Add(head);
    }

    static Panel Row(int height) => new()
    {
        Dock = DockStyle.Top,
        Height = height,
        BackColor = Theme.Bg,
        Padding = new Padding(0, 4, 0, 4),
    };

    static Label Inline(string text, int width) => new()
    {
        Text = text,
        Dock = DockStyle.Left,
        Width = width,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Theme.Muted,
        Font = Theme.Small,
    };

    void LoadList()
    {
        _binding = true;
        _list.Items.Clear();
        foreach (var schedule in _schedules) _list.Items.Add(schedule);
        _binding = false;
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        else BindSelected();
    }

    void BindSelected()
    {
        _selected = _list.SelectedItem as WorkSchedule;
        _editor.Visible = _selected != null;
        _empty.Visible = _selected == null;
        if (_selected == null)
        {
            ClearCards();
            return;
        }

        var schedule = _selected;
        _binding = true;
        _name.Text = schedule.Name;
        _from.Value = WindowClock.TodayAt(schedule.StartMinutes);
        _to.Value = WindowClock.TodayAt(schedule.EndMinutes);
        _enabled.Checked = schedule.Enabled;
        _reset.Checked = schedule.ResetOnWindowStart;
        for (var i = 0; i < _days.Length; i++)
            _days[i].Checked = (schedule.DaysMask & (1 << i)) != 0;
        _binding = false;
        RebuildCards();
        UpdateHint();
        RefreshLive();
    }

    void SaveSchedule(bool finalize)
    {
        if (_binding || _selected == null) return;
        var schedule = _selected;
        var name = _name.Text.Trim();
        if (name.Length == 0)
        {
            if (!finalize) return;
            name = "Без названия";
            _binding = true;
            _name.Text = name;
            _binding = false;
        }

        schedule.Name = name;
        schedule.StartMinutes = WindowClock.ClockMinutes(_from.Value);
        schedule.EndMinutes = WindowClock.ClockMinutes(_to.Value);
        schedule.Enabled = _enabled.Checked;
        schedule.ResetOnWindowStart = _reset.Checked;
        var mask = 0;
        for (var i = 0; i < _days.Length; i++)
        {
            if (_days[i].Checked) mask |= 1 << i;
        }
        schedule.DaysMask = mask;
        _database.UpdateSchedule(schedule);
        _list.Invalidate();
        UpdateHint();
        RefreshLive();
    }

    void UpdateHint()
    {
        if (_selected == null) return;
        var schedule = _selected;
        if (schedule.DaysMask == 0)
            _hint.Text = "Дни не выбраны — режим не включится.";
        else if (schedule.StartMinutes == schedule.EndMinutes)
            _hint.Text = "Начало и конец совпали — окно пустое.";
        else if (schedule.StartMinutes > schedule.EndMinutes)
            _hint.Text = "Через полночь.  " + WindowClock.FormatDays(schedule.DaysMask);
        else
            _hint.Text = WindowClock.FormatDays(schedule.DaysMask) + "  ·  счётчики идут только здесь";
    }

    void AddSchedule()
    {
        var schedule = new WorkSchedule
        {
            Name = "Новый режим",
            StartMinutes = 9 * 60,
            EndMinutes = 19 * 60,
            DaysMask = 0b0011111,
            Enabled = true,
            ResetOnWindowStart = true,
        };
        schedule.Id = _database.InsertSchedule(schedule);
        _schedules.Add(schedule);
        _list.Items.Add(schedule);
        _list.SelectedItem = schedule;
    }

    void DeleteSchedule()
    {
        if (_selected == null) return;
        var schedule = _selected;
        var answer = MessageBox.Show(
            $"Удалить режим «{schedule.Name}» и все его счётчики?",
            "Ритм",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        _database.DeleteSchedule(schedule.Id);
        _binding = true;
        _schedules.Remove(schedule);
        _list.Items.Remove(schedule);
        _binding = false;
        if (_list.Items.Count > 0 && _list.SelectedIndex < 0)
            _list.SelectedIndex = 0;
        BindSelected();
    }

    void AddCounter()
    {
        if (_selected == null) return;
        var counter = new Counter
        {
            ScheduleId = _selected.Id,
            Name = "Перерыв",
            IntervalMinutes = 45,
            Message = "Пора сделать перерыв. Встаньте и отойдите от стола.",
            Enabled = true,
            Sound = true,
            SortOrder = _selected.Counters.Count,
        };
        counter.Id = _database.InsertCounter(counter);
        _selected.Counters.Add(counter);
        var card = CreateCard(counter);
        _flow.Controls.Add(card);
        FitCards();
        _flow.ScrollControlIntoView(card);
        _list.Invalidate();
    }

    void ResetCounters()
    {
        if (_selected == null) return;
        foreach (var counter in _selected.Counters) counter.ElapsedSeconds = 0;
        _database.SaveElapsed(_selected.Counters);
        RefreshCards();
        RefreshLive();
    }

    void RebuildCards()
    {
        ClearCards();
        if (_selected == null) return;
        foreach (var counter in _selected.Counters.OrderBy(c => c.SortOrder).ThenBy(c => c.Id))
            _flow.Controls.Add(CreateCard(counter));
        FitCards();
    }

    CounterCard CreateCard(Counter counter)
    {
        var schedule = _selected ?? throw new InvalidOperationException("No schedule.");
        var card = new CounterCard(_database, counter);
        card.Edited += () =>
        {
            _list.Invalidate();
            RefreshLive();
        };
        card.RemoveRequested += removed =>
        {
            _database.DeleteCounter(removed.Id);
            schedule.Counters.Remove(removed);
            _flow.Controls.Remove(card);
            card.Dispose();
            FitCards();
            _list.Invalidate();
            RefreshLive();
        };
        card.TestRequested += current =>
        {
            Sounder.PlayAlert();
            ShowAlert(schedule.Name, current.Name, current.AlertMessage, () => Snooze(current));
        };
        return card;
    }

    void ClearCards()
    {
        var leftover = _flow.Controls.Cast<Control>().ToArray();
        _flow.Controls.Clear();
        foreach (var control in leftover) control.Dispose();
    }

    void FitCards()
    {
        int width = _flow.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8;
        if (width < 420) width = 420;
        foreach (Control control in _flow.Controls)
            control.Width = width;
    }

    void OnTick(object? sender, EventArgs e)
    {
        try
        {
            var now = DateTime.Now;
            int seconds = TakeSeconds();
            if (!_paused && seconds > 0)
            {
                var outcome = Ticker.Advance(_schedules, now, seconds, paused: false);
                foreach (var schedule in outcome.SessionsStarted)
                    _database.SaveSession(schedule);

                bool due = outcome.Alerts.Count > 0 || outcome.SessionsStarted.Count > 0;
                if (due || ++_persistEvery >= 5)
                {
                    _persistEvery = 0;
                    _database.SaveElapsed(_schedules.SelectMany(schedule => schedule.Counters));
                }

                if (outcome.Alerts.Count > 0)
                {
                    if (outcome.Alerts.Exists(alert => alert.Counter.Sound))
                        Sounder.PlayAlert();
                    foreach (var alert in outcome.Alerts)
                    {
                        var counter = alert.Counter;
                        ShowAlert(alert.Schedule.Name, counter.Name, counter.AlertMessage, () => Snooze(counter));
                    }
                    _alertUntil = now.AddSeconds(8);
                }
            }

            RefreshLive();
        }
        catch (Exception ex)
        {
            _timer.Stop();
            AppData.Log(ex);
            _status.Text = "Сбой отсчёта. Подробности в error.log в папке данных. Перезапустите Ритм.";
        }
    }

    int TakeSeconds()
    {
        var stamp = Stopwatch.GetTimestamp();
        if (_stamp == 0) _stamp = stamp;
        var delta = (stamp - _stamp) / (double)Stopwatch.Frequency;
        _stamp = stamp;
        if (delta > 5) 
        {
            _bank = 0;
            return 1;
        }

        _bank += delta;
        var whole = (int)Math.Floor(_bank);
        if (whole <= 0) return 0;
        _bank -= whole;
        return whole > 5 ? 1 : whole;
    }

    void RefreshLive()
    {
        var now = DateTime.Now;
        _clock.Text = now.ToString("HH:mm:ss");
        Describe(now, out var status, out var tip, out var kind);
        _status.Text = status;
        TrayUpdate?.Invoke(tip, kind);
        _list.Invalidate();
        RefreshCards();
        PaintWindowState(now);
    }

    void Describe(DateTime now, out string status, out string tip, out IconKind kind)
    {
        if (now < _alertUntil) kind = IconKind.Alert;
        else if (_paused) kind = IconKind.Paused;
        else if (_schedules.Exists(schedule => schedule.Enabled && WindowClock.Contains(schedule, now))) kind = IconKind.Active;
        else kind = IconKind.Idle;

        if (_paused)
        {
            tip = "Ритм · пауза";
            status = "Пауза. Окна не проверяются, счётчики стоят.";
            return;
        }

        Counter? nearest = null;
        WorkSchedule? owner = null;
        var left = int.MaxValue;
        var active = 0;
        foreach (var schedule in _schedules)
        {
            if (!schedule.Enabled || !WindowClock.Contains(schedule, now)) continue;
            active++;
            foreach (var counter in schedule.Counters)
            {
                if (!counter.Enabled) continue;
                int remain = counter.LimitSeconds - Math.Clamp(counter.ElapsedSeconds, 0, counter.LimitSeconds);
                if (remain < left)
                {
                    left = remain;
                    nearest = counter;
                    owner = schedule;
                }
            }
        }

        if (active == 0)
        {
            tip = "Ритм · вне окна";
            status = $"{now:HH:mm:ss}   ·   Вне всех режимов — счётчики стоят.";
            return;
        }

        if (nearest == null || owner == null)
        {
            tip = "Ритм · нет счётчиков";
            status = $"{now:HH:mm:ss}   ·   Окно открыто, включённых счётчиков нет.";
            return;
        }

        var remainText = WindowClock.FormatDuration(left);
        tip = $"Ритм · {nearest.Name} через {remainText}";
        status = $"{now:HH:mm:ss}   ·   {owner.Name}: «{nearest.Name}» через {remainText}";
    }

    void PaintWindowState(DateTime now)
    {
        if (_selected == null)
        {
            _windowState.Text = "";
            return;
        }

        if (!_selected.Enabled)
        {
            _windowState.Text = "выключен";
            _windowState.ForeColor = Theme.Muted;
        }
        else if (_paused)
        {
            _windowState.Text = "пауза";
            _windowState.ForeColor = Theme.Muted;
        }
        else if (WindowClock.Contains(_selected, now))
        {
            _windowState.Text = "сейчас идёт";
            _windowState.ForeColor = Theme.Pine;
        }
        else
        {
            _windowState.Text = "вне окна";
            _windowState.ForeColor = Theme.Muted;
        }
    }

    void RefreshCards()
    {
        foreach (Control control in _flow.Controls)
        {
            if (control is CounterCard card) card.RefreshProgress();
        }
    }

    void ShowAlert(string schedule, string counter, string message, Action snooze)
    {
        var form = new AlertForm(schedule, counter, message);
        form.Snoozed += snooze;
        form.Show();
    }

    void Snooze(Counter counter)
    {
        int limit = counter.LimitSeconds;
        int delay = Math.Min(5 * 60, Math.Max(1, limit - 1));
        counter.ElapsedSeconds = Math.Max(0, limit - delay);
        _database.SaveElapsed([counter]);
        RefreshCards();
        RefreshLive();
    }

    void PersistAll()
    {
        try
        {
            foreach (var schedule in _schedules)
                _database.SaveSession(schedule);
            _database.SaveElapsed(_schedules.SelectMany(schedule => schedule.Counters));
        }
        catch (Exception ex)
        {
            AppData.Log(ex);
        }
    }

    void DrawModeItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _list.Items.Count) return;
        if (_list.Items[e.Index] is not WorkSchedule schedule) return;

        bool selected = (e.State & DrawItemState.Selected) != 0;
        using var back = new SolidBrush(selected ? Theme.PineSoft : Theme.Panel);
        e.Graphics.FillRectangle(back, e.Bounds);
        bool live = schedule.Enabled && !_paused && WindowClock.Contains(schedule, DateTime.Now);
        using var dot = new SolidBrush(!schedule.Enabled ? Theme.Line : live ? Theme.Pine : Theme.Muted);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillEllipse(dot, e.Bounds.Left + 12, e.Bounds.Top + 18, 9, 9);

        var title = new Rectangle(e.Bounds.Left + 30, e.Bounds.Top + 6, e.Bounds.Width - 42, 24);
        var sub = new Rectangle(e.Bounds.Left + 30, e.Bounds.Top + 30, e.Bounds.Width - 42, 18);
        TextRenderer.DrawText(e.Graphics, schedule.Name, Theme.UiBold, title, Theme.Ink,
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
        var caption = $"{WindowClock.FormatRange(schedule)}   ·   {WindowClock.FormatDays(schedule.DaysMask)}   ·   {schedule.Counters.Count}";
        TextRenderer.DrawText(e.Graphics, caption, Theme.Small, sub, Theme.Muted,
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
    }

    static void OpenDataFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{AppData.DirectoryPath()}\"",
            UseShellExecute = true,
        });
    }
}
