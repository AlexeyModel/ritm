using Microsoft.Win32;

namespace Ritm;

static class WindowsAutostart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "Ritm";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("Не удалось открыть автозагрузку Windows.");
        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var exe = Application.ExecutablePath;
        key.SetValue(ValueName, $"\"{exe}\" --tray");
    }

    public static void RefreshInstalledPath()
    {
        if (IsEnabled()) SetEnabled(true);
    }
}
