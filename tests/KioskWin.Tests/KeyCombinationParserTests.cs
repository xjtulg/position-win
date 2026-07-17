using System.Windows.Forms;
using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class KeyCombinationParserTests
{
    [Fact]
    public void Parses_full_modifier_combo_with_letter()
    {
        var keys = KeyCombinationParser.Parse("Ctrl+Shift+Alt+Q");
        Assert.Equal(Keys.Control | Keys.Shift | Keys.Alt | Keys.Q, keys);
    }

    [Fact]
    public void Parses_ctrl_only_with_digit()
    {
        var keys = KeyCombinationParser.Parse("Ctrl+1");
        Assert.Equal(Keys.Control | Keys.D1, keys);
    }

    [Fact]
    public void Parses_single_letter()
    {
        Assert.Equal(Keys.Q, KeyCombinationParser.Parse("Q"));
    }

    [Fact]
    public void Empty_returns_none()
    {
        Assert.Equal(Keys.None, KeyCombinationParser.Parse(""));
        Assert.Equal(Keys.None, KeyCombinationParser.Parse("   "));
    }

    [Fact]
    public void Unknown_token_is_ignored()
    {
        var keys = KeyCombinationParser.Parse("Ctrl+Banana+Q");
        Assert.Equal(Keys.Control | Keys.Q, keys);
    }
}
