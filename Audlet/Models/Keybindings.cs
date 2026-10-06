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
