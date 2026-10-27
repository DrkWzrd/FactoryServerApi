namespace FactoryServerUi;

/// <summary>
/// Minimal single-field input dialog for text or password entry.
/// Returns the entered text, or null when the user cancels.
/// </summary>
internal static class Prompts
{
    public static string? Input(Form owner, string title, string label, string initial = "", bool password = false)
    {
        using Form form = new()
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(380, 120)
        };

        Label lbl = new() { Text = label, AutoSize = true, Location = new Point(12, 12) };
        TextBox box = new()
        {
            Text = initial,
            UseSystemPasswordChar = password,
            Location = new Point(12, 38),
            Size = new Size(356, 23)
        };
        Button ok = new() { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(196, 80), Size = new Size(78, 27) };
        Button cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(282, 80), Size = new Size(78, 27) };

        form.Controls.AddRange(new Control[] { lbl, box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog(owner) == DialogResult.OK ? box.Text : null;
    }

    /// <summary>Like <see cref="Input"/> with password masking; returns the characters entered.</summary>
    public static char[]? Password(Form owner, string title, string label)
    {
        string? text = Input(owner, title, label, initial: "", password: true);
        return text is null ? null : text.ToCharArray();
    }

    /// <summary>
    /// Multi-line key/value editor: one <c>key=value</c> pair per line.
    /// Returns the parsed pairs, or null when the user cancels.
    /// </summary>
    public static Dictionary<string, string>? KeyValue(Form owner, string title, string label)
    {
        using Form form = new()
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(420, 260)
        };

        Label lbl = new() { Text = label, AutoSize = true, Location = new Point(12, 10) };
        TextBox box = new()
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = false,
            Font = new Font("Consolas", 9F),
            Location = new Point(12, 34),
            Size = new Size(396, 160)
        };
        Button ok = new() { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(234, 208), Size = new Size(80, 27) };
        Button cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(324, 208), Size = new Size(80, 27) };

        form.Controls.AddRange(new Control[] { lbl, box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        if (form.ShowDialog(owner) != DialogResult.OK)
            return null;

        Dictionary<string, string> result = [];
        foreach (string raw in box.Text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0)
                continue;
            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            result[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }
        return result;
    }
}
