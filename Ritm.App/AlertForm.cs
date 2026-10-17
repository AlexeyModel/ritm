namespace Ritm;

sealed class AlertForm : Form
{
    public event Action? Snoozed;

    static readonly List<AlertForm> OpenForms = new();

    public AlertForm(string schedule, string counter, string message)
    {
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Theme.Line;
        Font = Theme.Ui;
        ForeColor = Theme.Ink;
        Padding = new Padding(1);

        var measure = TextRenderer.MeasureText(
            message,
            Theme.Ui,
            new Size(348, 500),
            TextFormatFlags.WordBreak);
        ClientSize = new Size(420, Math.Max(210, 132 + measure.Height));

        var bar = new Panel { Dock = DockStyle.Left, Width = 8, BackColor = Theme.Clay };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Card,
            Padding = new Padding(16, 12, 16, 10),
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var kicker = MakeLabel(schedule, Theme.Small, Theme.Muted);
        var title = MakeLabel(counter, Theme.UiBold, Theme.Ink);
        var body = MakeLabel(message, Theme.Ui, Theme.Ink);
        body.AutoEllipsis = false;

        var buttons = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Card };
        var ok = Theme.MakeButton("Понятно", true);
        ok.SetBounds(0, 6, 112, 32);
        var later = Theme.MakeButton("Отложить на 5 мин", false);
        later.SetBounds(122, 6, 168, 32);
        ok.Click += (_, _) => Close();
        later.Click += (_, _) =>
        {
            Snoozed?.Invoke();
            Close();
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(later);

        grid.Controls.Add(kicker, 0, 0);
        grid.Controls.Add(title, 0, 1);
        grid.Controls.Add(body, 0, 2);
        grid.Controls.Add(buttons, 0, 3);
        Controls.Add(grid);
        Controls.Add(bar);

        foreach (Control control in new Control[] { grid, kicker, title, body })
            control.MouseDown += BeginDrag;

        Place();
    }

    static Label MakeLabel(string text, Font font, Color color) => new()
    {
        Text = text,
        Font = font,
        ForeColor = color,
        BackColor = Theme.Card,
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        UseMnemonic = false,
    };

    void Place()
    {
        OpenForms.Add(this);
        FormClosed += (_, _) => OpenForms.Remove(this);
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800);
        int index = Math.Max(0, OpenForms.Count - 1) % 4;
        Location = new Point(area.Right - Width - 18, area.Bottom - Height - 18 - index * (Height + 12));
    }

    void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (sender is Control control) control.Capture = false;
        var message = Message.Create(Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
        WndProc(ref message);
    }
}
