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

    // Ctrl, Alt or Win plus a key. Whether the key name is a real key is checked where gestures become keys.
    public static bool IsGlobalGesture(string gesture)
    {
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Any(p => p is "Ctrl" or "Alt" or "Win") && parts.Any(p => p is not ("Ctrl" or "Alt" or "Shift" or "Win"));
    }

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
}
