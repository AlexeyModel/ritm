namespace Ritm;

static class Theme
{
    public static readonly Color Bg = Color.FromArgb(244, 241, 232);
    public static readonly Color Panel = Color.FromArgb(235, 230, 220);
    public static readonly Color Card = Color.FromArgb(251, 249, 244);
    public static readonly Color Ink = Color.FromArgb(28, 27, 23);
    public static readonly Color Muted = Color.FromArgb(94, 90, 82);
    public static readonly Color Line = Color.FromArgb(213, 207, 194);
    public static readonly Color Pine = Color.FromArgb(47, 107, 79);
    public static readonly Color PineDeep = Color.FromArgb(36, 86, 63);
    public static readonly Color PineSoft = Color.FromArgb(223, 235, 226);
    public static readonly Color Clay = Color.FromArgb(156, 61, 47);

    public static readonly Font Ui = new("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font UiBold = new("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font Title = new("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font Clock = new("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font Small = new("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);

    public static Button MakeButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            AutoSize = false,
            Height = 32,
            Cursor = Cursors.Hand,
            Font = Ui,
            BackColor = primary ? Pine : Card,
            ForeColor = primary ? Card : Ink,
            UseVisualStyleBackColor = false,
            Margin = new Padding(0),
        };
        button.FlatAppearance.BorderColor = primary ? Pine : Line;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = primary ? PineDeep : Panel;
        button.FlatAppearance.MouseDownBackColor = primary ? PineDeep : Line;
        return button;
    }

    public static TextBox MakeField()
    {
        return new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Card,
            ForeColor = Ink,
            Font = Ui,
        };
    }

    public static DateTimePicker MakeTime()
    {
        return new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Width = 88,
            Font = Ui,
        };
    }
}
