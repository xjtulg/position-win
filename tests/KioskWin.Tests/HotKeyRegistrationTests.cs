using System.Windows.Forms;
using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class HotKeyRegistrationTests
{
    [Fact]
    public void TryCreate_maps_default_shortcut_to_win32_hotkey_parts()
    {
        var created = HotKeyRegistration.TryCreate(Keys.Control | Keys.Shift | Keys.Alt | Keys.Q, out var hotKey);

        Assert.True(created);
        Assert.Equal(HotKeyModifiers.Control | HotKeyModifiers.Shift | HotKeyModifiers.Alt, hotKey.Modifiers);
        Assert.Equal((uint)Keys.Q, hotKey.VirtualKey);
    }

    [Fact]
    public void TryCreate_rejects_modifier_only_shortcut()
    {
        var created = HotKeyRegistration.TryCreate(Keys.Control | Keys.Alt, out _);

        Assert.False(created);
    }
}
