using System.Windows.Input;

namespace Audlet.Models;

// In-app keyboard shortcuts. A gesture is stored as text such as "Ctrl+Shift+Left" so it survives in settings.json and reads well in the UI.
public static class Keybindings
{
    public static readonly (string Action, string Label)[] Actions =
    [
        ("PlayPause", "Play / pause"),
        ("Rewind", "Small rewind"),
        ("Forward", "Small forward"),
        ("BigRewind", "Big rewind"),
        ("BigForward", "Big forward"),
        ("PrevChapter", "Start of chapter / previous chapter"),
        ("NextChapter", "Next chapter"),
        ("SpeedUp", "Speed up"),
        ("SpeedDown", "Speed down"),
        ("Bookmark", "Add a bookmark"),
        ("Lock", "Lock / unlock the buttons"),
    ];

    // Shortcuts that reach the app while another window has focus. Each must hold Ctrl, Alt or Win.
    public static readonly (string Action, string Label)[] GlobalActions =
    [
        ("PlayPause", "Play / pause"),
        ("Rewind", "Small rewind"),
        ("Forward", "Small forward"),
        ("BigRewind", "Big rewind"),
        ("BigForward", "Big forward"),
    ];

    public static Dictionary<string, string> GlobalDefaults() => new()
    {
        ["PlayPause"] = "Ctrl+Alt+Space", ["Rewind"] = "Ctrl+Alt+Left", ["Forward"] = "Ctrl+Alt+Right",
        ["BigRewind"] = "Ctrl+Alt+Shift+Left", ["BigForward"] = "Ctrl+Alt+Shift+Right",
    };

    public static string GlobalGesture(Settings s, string action) =>
        s.GlobalKeys.TryGetValue(action, out var g) ? g : GlobalDefaults().GetValueOrDefault(action, "");

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

    public static bool IsGlobalGesture(string gesture) =>
        TryParse(gesture, out var mods, out _) && (mods & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0;

    public static Dictionary<string, string> Defaults() => new()
    {
        ["PlayPause"] = "Space", ["Rewind"] = "Left", ["Forward"] = "Right",
        ["BigRewind"] = "Shift+Left", ["BigForward"] = "Shift+Right",
        ["PrevChapter"] = "Ctrl+Left", ["NextChapter"] = "Ctrl+Right",
        ["SpeedUp"] = "Up", ["SpeedDown"] = "Down", ["Bookmark"] = "B", ["Lock"] = "Ctrl+L",
    };

    // Older settings files lack newer actions, so missing entries fall back to the default rather than going unbound.
    public static string Gesture(Settings s, string action) =>
        s.Keys.TryGetValue(action, out var g) ? g : Defaults().GetValueOrDefault(action, "");

    public static string? ActionFor(Settings s, string gesture) =>
        Actions.Select(a => a.Action).FirstOrDefault(a => Gesture(s, a) == gesture);

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
