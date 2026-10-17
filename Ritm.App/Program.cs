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

        using var mutex = new Mutex(true, @"Local\Ritm.SingleInstance.v1", out var created);
        if (!created)
        {
            MessageBox.Show(
                "Ритм уже запущен. Значок находится у часов, в трее.",
                "Ритм",
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
            Application.Run(new TrayContext(database, startInTray));
        }
        catch (Exception ex)
        {
            AppData.Log(ex);
            MessageBox.Show(
                ex.Message,
                "Ритм — не удалось запустить",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
