namespace Ritm;

static class LanguageMenu
{
    public static void Add(ToolStripItemCollection items, Database database)
    {
        var root = new ToolStripMenuItem();
        UiText.Bind(root, "menu.language");

        var russian = Item("Русский", AppLanguage.Ru, database);
        var english = Item("English", AppLanguage.En, database);
        var german = Item("Deutsch", AppLanguage.De, database);

        void Mark()
        {
            russian.Checked = Loc.Language == AppLanguage.Ru;
            english.Checked = Loc.Language == AppLanguage.En;
            german.Checked = Loc.Language == AppLanguage.De;
        }

        Loc.Changed += Mark;
        Mark();
        root.DropDownItems.Add(russian);
        root.DropDownItems.Add(english);
        root.DropDownItems.Add(german);
        items.Add(root);
    }

    static ToolStripMenuItem Item(string caption, AppLanguage language, Database database)
    {
        var item = new ToolStripMenuItem(caption);
        item.Click += (_, _) => Loc.Select(language, database);
        return item;
    }
}
