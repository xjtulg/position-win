using System.Windows.Forms;

namespace KioskWin.Core;

public static class KeyCombinationParser
{
    public static Keys Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Keys.None;

        Keys result = Keys.None;
        foreach (var raw in text.Split('+'))
        {
            var t = raw.Trim();
            if (t.Length == 0) continue;
            result |= MapToken(t);
        }
        return result;
    }

    private static Keys MapToken(string t) => t.ToLowerInvariant() switch
    {
        "ctrl" or "control" => Keys.Control,
        "shift" => Keys.Shift,
        "alt" or "menu" => Keys.Alt,
        var d when d.Length == 1 && char.IsLetterOrDigit(d[0]) => (Keys)char.ToUpper(d[0]),
        _ => Enum.TryParse(t, ignoreCase: true, out Keys k) ? k : Keys.None,
    };
}
