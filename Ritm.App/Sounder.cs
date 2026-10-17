using System.Runtime.InteropServices;

namespace Ritm;

static class Sounder
{
    const uint Alias = 0x00010000;
    const uint Async = 0x0001;
    const uint NoDefault = 0x0002;

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    static extern bool PlaySound(string? name, IntPtr module, uint flags);

    public static void PlayAlert()
    {
        try
        {
            if (!PlaySound("SystemExclamation", IntPtr.Zero, Alias | Async | NoDefault))
                System.Media.SystemSounds.Exclamation.Play();
        }
        catch
        {
            try { System.Media.SystemSounds.Exclamation.Play(); }
            catch { /* The message still has to appear if the sound device is missing. */ }
        }
    }
}
