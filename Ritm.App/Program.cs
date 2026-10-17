namespace Ritm;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, thread) => AppData.Log(thread.Exception);
        Loc.Load();

        using var mutex = new Mutex(true, @"Local\Ritm.SingleInstance.v1", out var created);
        if (!created)
        {
            MessageBox.Show(
                Loc.T("app.already"),
                Loc.T("app.name"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var startInTray = args.Any(arg => arg.Equals("--tray", StringComparison.OrdinalIgnoreCase));
        try
        {
            WindowsAutostart.RefreshInstalledPath();
        }
        catch (Exception ex)
        {
            AppData.Log(ex);
        }

        try
        {
            using var database = new Database(AppData.DatabasePath());
            Loc.UseDatabase(database);
            Application.Run(new TrayContext(database, startInTray));
        }
        catch (Exception ex)
        {
            AppData.Log(ex);
            MessageBox.Show(
                ex.Message,
                Loc.T("app.failed"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
