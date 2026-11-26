namespace Audlet.Views;

// A theme is a background gradient plus the tints the glass panels sit on. Colours are ARGB hex. Orb tints the colour blobs behind the glass; it defaults to the accent.
public sealed record Theme(string Name, bool IsDark, string Bg1, string Bg2, string Bg3, string Text, string Muted, string Accent, string AccentText, string? Orb = null);

public static class ThemeCatalog
{
    public const string System = "System";

    public static readonly Theme DarkGlass = new("Dark glass", true, "#FF141826", "#FF1C2333", "#FF23203A", "#FFFFFFFF", "#99FFFFFF", "#FFFFFFFF", "#FF202020", "#FF6E7CF5");
    public static readonly Theme LightGlass = new("Light glass", false, "#FFE9EDF5", "#FFDCE3F0", "#FFE6DDF2", "#FF1A1A1A", "#991A1A1A", "#FF1A1A1A", "#FFFFFFFF", "#FF7C8CFF");

    public static readonly IReadOnlyList<Theme> All =
    [
        DarkGlass,
        LightGlass,
        new("Catppuccin Mocha", true, "#FF11111B", "#FF1E1E2E", "#FF2A2440", "#FFCDD6F4", "#FFA6ADC8", "#FFCBA6F7", "#FF1E1E2E"),
        new("Catppuccin Latte", false, "#FFE6E9EF", "#FFEFF1F5", "#FFE9E0F5", "#FF4C4F69", "#FF6C6F85", "#FF8839EF", "#FFFFFFFF"),
        new("Nord", true, "#FF242933", "#FF2E3440", "#FF3B4252", "#FFECEFF4", "#FFB8C2D1", "#FF88C0D0", "#FF2E3440"),
        new("Dracula", true, "#FF21222C", "#FF282A36", "#FF383A59", "#FFF8F8F2", "#FF9BA5CC", "#FFBD93F9", "#FF282A36"),
        new("Tokyo Night", true, "#FF16161E", "#FF1A1B26", "#FF24283B", "#FFC0CAF5", "#FF8089B3", "#FF7AA2F7", "#FF1A1B26"),
        new("Rose Pine", true, "#FF191724", "#FF1F1D2E", "#FF2E2540", "#FFE0DEF4", "#FF908CAA", "#FFEBBCBA", "#FF191724"),
        new("Gruvbox Dark", true, "#FF1D2021", "#FF282828", "#FF3C3836", "#FFEBDBB2", "#FFA89984", "#FFFABD2F", "#FF282828"),
        new("Solarized Dark", true, "#FF002B36", "#FF073642", "#FF0B3E4A", "#FFEEE8D5", "#FF93A1A1", "#FF268BD2", "#FFFDF6E3"),
    ];

    public static IReadOnlyList<string> Names { get; } = [System, .. All.Select(t => t.Name)];

    public static Theme Resolve(string? name, bool systemIsDark) =>
        All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? (systemIsDark ? DarkGlass : LightGlass);
}
