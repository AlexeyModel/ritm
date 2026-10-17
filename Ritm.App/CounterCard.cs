using System.Runtime.InteropServices;

namespace Ritm;

sealed class CounterCard : UserControl
{
    const int CueBanner = 0x1501;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, string lParam);

    readonly Database _database;
    readonly TextBox _name;
    readonly NumericUpDown _minutes;
    readonly TextBox _message;
    readonly CheckBox _enabled;
    readonly CheckBox _sound;
    readonly Label _progress;
    readonly Panel _fill;
    bool _binding;

    public Counter Counter { get; }
    public event Action<Counter>? RemoveRequested;
    public event Action<Counter>? TestRequested;
    public event Action? Edited;

    public CounterCard(Database database, Counter counter)
    {
        _database = database;
        Counter = counter;
        Height = 148;
        MinimumSize = new Size(420, 148);
        MaximumSize = new Size(4000, 148);
        Margin = new Padding(0, 0, 0, 10);
        BackColor = Theme.Card;
        Padding = new Padding(1);
        Font = Theme.Ui;

        var shell = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Line, Padding = new Padding(1) };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 4,
            BackColor = Theme.Card,
            Padding = new Padding(10, 8, 10, 8),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));

        _name = Theme.MakeField();
        _name.Dock = DockStyle.Fill;
        _minutes = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 720,
            DecimalPlaces = 0,
            Increment = 5,
            Dock = DockStyle.Fill,
            Font = Theme.Ui,
            BackColor = Theme.Card,
            ForeColor = Theme.Ink,
            BorderStyle = BorderStyle.FixedSingle,
        };
        var unit = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            BackColor = Theme.Card,
            Font = Theme.Small,
        };
        UiText.Bind(unit, "unit.min");
        _enabled = Box("check.on");
        _sound = Box("check.sound");
        var remove = UiText.Button("button.delete", false);
        remove.Dock = DockStyle.Fill;
        remove.ForeColor = Theme.Clay;

        _message = Theme.MakeField();
        _message.Multiline = true;
        _message.Dock = DockStyle.Fill;
        _message.ScrollBars = ScrollBars.None;
        _message.MaxLength = 280;

        _progress = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            BackColor = Theme.Card,
            Font = Theme.Small,
        };
        var test = UiText.Button("button.test", false);
        test.Dock = DockStyle.Fill;

        var track = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Line, Margin = new Padding(0, 4, 0, 0) };
        _fill = new Panel { BackColor = Theme.Pine, Height = 4, Width = 0 };
        track.Controls.Add(_fill);
        track.Resize += (_, _) => RefreshProgress();

        grid.Controls.Add(_name, 0, 0);
        grid.Controls.Add(_minutes, 1, 0);
        grid.Controls.Add(unit, 2, 0);
        grid.Controls.Add(_enabled, 3, 0);
        grid.Controls.Add(_sound, 4, 0);
        grid.Controls.Add(remove, 5, 0);
        grid.Controls.Add(_message, 0, 1);
        grid.SetColumnSpan(_message, 6);
        grid.Controls.Add(_progress, 0, 2);
        grid.SetColumnSpan(_progress, 4);
        grid.Controls.Add(test, 4, 2);
        grid.SetColumnSpan(test, 2);
        grid.Controls.Add(track, 0, 3);
        grid.SetColumnSpan(track, 6);

        shell.Controls.Add(grid);
        Controls.Add(shell);

        _binding = true;
        _name.Text = counter.Name;
        _minutes.Value = Math.Clamp(counter.IntervalMinutes, 1, 720);
        _message.Text = counter.Message;
        _enabled.Checked = counter.Enabled;
        _sound.Checked = counter.Sound;
        _binding = false;
        RefreshProgress();

        _name.TextChanged += (_, _) => Push(false);
        _name.Leave += (_, _) => Push(true);
        _minutes.ValueChanged += (_, _) => Push(true);
        _message.TextChanged += (_, _) => Push(false);
        _enabled.CheckedChanged += (_, _) => Push(true);
        _sound.CheckedChanged += (_, _) => Push(true);
        remove.Click += (_, _) => RemoveRequested?.Invoke(Counter);
        test.Click += (_, _) =>
        {
            Push(true);
            TestRequested?.Invoke(Counter);
        };
        Loc.Changed += ApplyCue;
    }

    public void RefreshProgress()
    {
        int limit = Counter.LimitSeconds;
        int elapsed = Math.Clamp(Counter.ElapsedSeconds, 0, limit);
        int left = Math.Max(0, limit - elapsed);
        _progress.Text = Loc.T(
            "progress",
            WindowClock.FormatDuration(elapsed),
            WindowClock.FormatDuration(limit),
            WindowClock.FormatDuration(left));
        if (_fill.Parent is { Width: > 0 } track)
        {
            int width = limit == 0 ? 0 : (int)Math.Round(track.Width * (elapsed / (double)limit));
            _fill.SetBounds(0, 0, Math.Max(0, width), track.Height);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyCue();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Loc.Changed -= ApplyCue;
        base.Dispose(disposing);
    }

    void Push(bool finalize)
    {
        if (_binding) return;
        var name = _name.Text.Trim();
        if (name.Length == 0)
        {
            if (!finalize) return;
            name = Loc.T("default.counter");
            _binding = true;
            _name.Text = name;
            _binding = false;
        }

        Counter.Name = name;
        Counter.IntervalMinutes = (int)_minutes.Value;
        Counter.Message = _message.Text.Trim();
        Counter.Enabled = _enabled.Checked;
        Counter.Sound = _sound.Checked;
        if (Counter.ElapsedSeconds >= Counter.LimitSeconds)
            Counter.ElapsedSeconds = Counter.LimitSeconds - 1;
        _database.UpdateCounter(Counter);
        _database.SaveElapsed([Counter]);
        RefreshProgress();
        Edited?.Invoke();
    }

    static CheckBox Box(string key)
    {
        var box = new CheckBox
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.Ink,
            BackColor = Theme.Card,
            Font = Theme.Small,
            Margin = new Padding(0, 6, 0, 0),
        };
        UiText.Bind(box, key);
        return box;
    }

    void ApplyCue()
    {
        if (IsDisposed || !_message.IsHandleCreated) return;
        SendMessage(_message.Handle, CueBanner, (IntPtr)1, Loc.T("cue.message"));
    }
}
