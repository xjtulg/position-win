using System.Windows.Forms;

namespace KioskWin.Core;

[Flags]
public enum HotKeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
}

public readonly record struct HotKeyRegistration(HotKeyModifiers Modifiers, uint VirtualKey)
{
    public static bool TryCreate(Keys keys, out HotKeyRegistration hotKey)
    {
        var modifiers = HotKeyModifiers.None;
        if ((keys & Keys.Alt) == Keys.Alt)
            modifiers |= HotKeyModifiers.Alt;
        if ((keys & Keys.Control) == Keys.Control)
            modifiers |= HotKeyModifiers.Control;
        if ((keys & Keys.Shift) == Keys.Shift)
            modifiers |= HotKeyModifiers.Shift;

        var keyCode = keys & Keys.KeyCode;
        if (keyCode == Keys.None || keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu)
        {
            hotKey = default;
            return false;
        }

        hotKey = new HotKeyRegistration(modifiers, (uint)keyCode);
        return true;
    }
}
