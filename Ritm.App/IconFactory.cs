using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Ritm;

enum IconKind
{
    Idle,
    Active,
    Alert,
    Paused,
}

static class IconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool DestroyIcon(IntPtr handle);

    static readonly Dictionary<IconKind, Icon> Cache = new();

    public static Icon Get(IconKind kind)
    {
        if (Cache.TryGetValue(kind, out var existing)) return existing;
        var created = Make(kind);
        Cache[kind] = created;
        return created;
    }

    static Icon Make(IconKind kind)
    {
        using var bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            var fill = kind switch
            {
                IconKind.Active => Theme.Pine,
                IconKind.Alert => Theme.Clay,
                IconKind.Paused => Theme.Muted,
                _ => Color.FromArgb(138, 132, 120),
            };
            using var brush = new SolidBrush(fill);
            graphics.FillEllipse(brush, 1, 1, 30, 30);
            using var ink = new SolidBrush(Theme.Card);
            graphics.FillRectangle(ink, 11, 8, 4, 16);
            graphics.FillRectangle(ink, 17, 8, 4, 16);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
