namespace Ritm;

static class UiText
{
    public static void Bind(Control control, string key)
    {
        Action apply = null!;
        apply = () =>
        {
            if (control.IsDisposed)
            {
                Loc.Changed -= apply;
                return;
            }

            control.Text = Loc.T(key);
        };
        apply();
        Loc.Changed += apply;
        control.Disposed += (_, _) => Loc.Changed -= apply;
    }

    public static void Bind(ToolStripItem item, string key)
    {
        Action apply = null!;
        apply = () =>
        {
            if (item.IsDisposed)
            {
                Loc.Changed -= apply;
                return;
            }

            item.Text = Loc.T(key);
        };
        apply();
        Loc.Changed += apply;
        item.Disposed += (_, _) => Loc.Changed -= apply;
    }

    public static Button Button(string key, bool primary)
    {
        var button = Theme.MakeButton(Loc.T(key), primary);
        Bind(button, key);
        return button;
    }
}
