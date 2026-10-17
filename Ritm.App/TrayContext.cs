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

        var pause = new ToolStripMenuItem(Loc.T("menu.pause"));
        pause.Click += (_, _) => _main.TogglePause();
        _autostart = new ToolStripMenuItem();
        UiText.Bind(_autostart, "menu.autostart");
        _autostart.Click += (_, _) => ToggleAutostart();

        var open = new ToolStripMenuItem();
        UiText.Bind(open, "menu.open");
        open.Click += (_, _) => _main.ShowFromTray();
        var exit = new ToolStripMenuItem();
        UiText.Bind(exit, "menu.exit");
        exit.Click += (_, _) => CloseApp();

        var menu = new ContextMenuStrip();
        menu.Items.Add(open);
        menu.Items.Add(pause);
        menu.Items.Add(_autostart);
        LanguageMenu.Add(menu.Items, database);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);
        menu.Opening += (_, _) =>
        {
            _autostart.Checked = WindowsAutostart.IsEnabled();
            _main.RefreshAutostartCheck();
        };

        _notify = new NotifyIcon
        {
            Icon = IconFactory.Get(IconKind.Idle),
            Visible = true,
            Text = Loc.T("app.name"),
            ContextMenuStrip = menu,
        };
        _notify.DoubleClick += (_, _) => _main.ShowFromTray();

        _main.TrayUpdate += (tip, kind) =>
        {
            var text = string.IsNullOrWhiteSpace(tip) ? Loc.T("app.name") : tip.Trim();
            if (text.Length > 63) text = text[..63];
            _notify.Text = text;
            if (kind == _kind) return;
            _kind = kind;
            var icon = IconFactory.Get(kind);
            _notify.Icon = icon;
            _main.Icon = icon;
        };
        _main.PauseStateChanged += _ => ApplyPause(pause);
        Loc.Changed += () => ApplyPause(pause);
        _main.HideToTrayHint += () => _notify.ShowBalloonTip(
            2500,
            Loc.T("app.name"),
            Loc.T("tray.hidden"),
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
            MessageBox.Show(ex.Message, Loc.T("app.name"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    void ApplyPause(ToolStripMenuItem pause)
    {
        if (pause.IsDisposed) return;
        pause.Text = _main.IsPaused ? Loc.T("menu.resume") : Loc.T("menu.pause");
    }
}
