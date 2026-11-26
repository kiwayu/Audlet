using System.Windows.Input;

namespace Audlet.Views;

// Converts between gesture text as Settings stores it, such as "Ctrl+Shift+Left", and WPF keys.
public static class KeyGestures
{
    public static bool TryParse(string gesture, out ModifierKeys mods, out Key key)
    {
        mods = ModifierKeys.None; key = Key.None;
        foreach (var part in gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part)
            {
                case "Ctrl": mods |= ModifierKeys.Control; break;
                case "Alt": mods |= ModifierKeys.Alt; break;
                case "Shift": mods |= ModifierKeys.Shift; break;
                case "Win": mods |= ModifierKeys.Windows; break;
                default: if (!Enum.TryParse(part, out key) || IsModifier(key)) return false; break;
            }
        }
        return key != Key.None;
    }

    public static bool IsModifier(Key key) => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
        or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System or Key.None;

    public static string Format(Key key, ModifierKeys mods)
    {
        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
