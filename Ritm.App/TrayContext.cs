namespace Ritm;

sealed class TrayContext : ApplicationContext
{
    readonly Database _database;
    readonly NotifyIcon _notify;
    readonly MainForm _main;
    readonly ToolStripMenuItem _autostart;
    IconKind _kind = IconKind.Idle;
    bool _closed;

    public TrayContext(Database database, bool startInTray)
    {
        _database = database;
        _main = new MainForm(database)
        {
            Icon = IconFactory.Get(IconKind.Idle),
        };

        var pause = new ToolStripMenuItem("Приостановить");
        pause.Click += (_, _) => _main.TogglePause();
        _autostart = new ToolStripMenuItem("Автозапуск при входе в Windows");
        _autostart.Click += (_, _) => ToggleAutostart();

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Открыть", null, (_, _) => _main.ShowFromTray()));
        menu.Items.Add(pause);
        menu.Items.Add(_autostart);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Выход", null, (_, _) => CloseApp()));
        menu.Opening += (_, _) =>
        {
            _autostart.Checked = WindowsAutostart.IsEnabled();
            _main.RefreshAutostartCheck();
        };

        _notify = new NotifyIcon
        {
            Icon = IconFactory.Get(IconKind.Idle),
            Visible = true,
            Text = "Ритм",
            ContextMenuStrip = menu,
        };
        _notify.DoubleClick += (_, _) => _main.ShowFromTray();

        _main.TrayUpdate += (tip, kind) =>
        {
            var text = string.IsNullOrWhiteSpace(tip) ? "Ритм" : tip.Trim();
            if (text.Length > 63) text = text[..63];
            _notify.Text = text;
            if (kind == _kind) return;
            _kind = kind;
            var icon = IconFactory.Get(kind);
            _notify.Icon = icon;
            _main.Icon = icon;
        };
        _main.PauseStateChanged += paused => pause.Text = paused ? "Продолжить" : "Приостановить";
        _main.HideToTrayHint += () => _notify.ShowBalloonTip(
            2500,
            "Ритм",
            "Окно закрыто. Счётчики продолжают идти в трее у часов.",
            ToolTipIcon.Info);
        _main.FormClosed += (_, _) => CloseApp();
        _autostart.Checked = WindowsAutostart.IsEnabled();
        _main.StartEngine();
        if (!startInTray) _main.Show();
    }

    void ToggleAutostart()
    {
        try
        {
            _autostart.Checked = _main.ToggleAutostart();
        }
        catch (Exception ex)
        {
            AppData.Log(ex);
            _autostart.Checked = WindowsAutostart.IsEnabled();
            MessageBox.Show(ex.Message, "Ритм", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void CloseApp()
    {
        if (_closed) return;
        _closed = true;
        _notify.Visible = false;
        if (!_main.IsDisposed)
            _main.Shutdown();
        _notify.Dispose();
        _database.Dispose();
        ExitThread();
    }
}
