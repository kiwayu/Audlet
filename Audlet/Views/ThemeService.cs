using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Audlet.Views;

public static class ThemeService
{
    public static bool IsDark { get; private set; } = true;

    public static void Start(Func<string?> nameGetter)
    {
        Apply(nameGetter());
        SystemEvents.UserPreferenceChanged += (o, a) => Application.Current.Dispatcher.BeginInvoke(() => Apply(nameGetter()));
    }

    public static void Apply(string? name)
    {
        var theme = ThemeCatalog.Resolve(name, SystemIsDark());
        IsDark = theme.IsDark;
        Application.Current.ThemeMode = theme.IsDark ? ThemeMode.Dark : ThemeMode.Light;
        var r = Application.Current.Resources;
        var white = theme.IsDark;
        r["BackgroundBrush"] = Frozen(new LinearGradientBrush(
            [new GradientStop(C(theme.Bg1), 0), new GradientStop(C(theme.Bg2), 0.55), new GradientStop(C(theme.Bg3), 1)],
            new Point(0, 0), new Point(1, 1)));
        Set(r, "GlassBrush", white ? "#1AFFFFFF" : "#66FFFFFF");
        Set(r, "GlassBorderBrush", white ? "#26FFFFFF" : "#1F000000");
        Set(r, "GlassHoverBrush", white ? "#33FFFFFF" : "#1F000000");
        Set(r, "GlassTextBrush", theme.Text);
        Set(r, "GlassMutedBrush", theme.Muted);
        Set(r, "AccentFillBrush", theme.Accent);
        Set(r, "AccentTextBrush", theme.AccentText);
        Set(r, "AccentSoftBrush", "#40" + theme.Accent[3..]);
        Set(r, "ScrimBrush", white ? "#66000000" : "#33FFFFFF");
        // Glass surfaces and the orbs behind them, tinted from the accent so every theme keeps its own character.
        r["GlassPanelBrush"] = Frozen(new LinearGradientBrush(
            [new GradientStop(C(white ? "#2EFFFFFF" : "#B3FFFFFF"), 0), new GradientStop(C(white ? "#14FFFFFF" : "#8AFFFFFF"), 1)], new Point(0, 0), new Point(0, 1)));
        r["GlassRimBrush"] = Frozen(new LinearGradientBrush(
            [new GradientStop(C(white ? "#99FFFFFF" : "#FFFFFFFF"), 0), new GradientStop(C(white ? "#1FFFFFFF" : "#80FFFFFF"), 0.5), new GradientStop(C(white ? "#40FFFFFF" : "#CCFFFFFF"), 1)], new Point(0, 0), new Point(1, 1)));
        r["GlassButtonBrush"] = Frozen(new LinearGradientBrush(
            [new GradientStop(C(white ? "#40FFFFFF" : "#F0FFFFFF"), 0), new GradientStop(C(white ? "#1AFFFFFF" : "#B3FFFFFF"), 1)], new Point(0, 0), new Point(0, 1)));
        var orb = theme.Orb ?? theme.Accent;
        r["Orb1Brush"] = Orb(orb, (byte)(white ? 0xB8 : 0xD0));
        r["Orb2Brush"] = Orb(Shift(orb, 45), (byte)(white ? 0x9C : 0xB8));
        r["Orb3Brush"] = Orb(Shift(orb, -45), (byte)(white ? 0x8C : 0xA6));
    }

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static Brush Orb(string hex, byte alpha)
    {
        var c = C(hex);
        var inner = Color.FromArgb(alpha, c.R, c.G, c.B);
        var outer = Color.FromArgb(0, c.R, c.G, c.B);
        return Frozen(new RadialGradientBrush([new GradientStop(inner, 0), new GradientStop(outer, 1)]));
    }

    // Rotates a colour's hue by the given degrees so the orbs are related but not identical.
    private static string Shift(string hex, double degrees)
    {
        var c = C(hex);
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        double h = d == 0 ? 0 : max == r ? 60 * (((g - b) / d) % 6) : max == g ? 60 * ((b - r) / d + 2) : 60 * ((r - g) / d + 4);
        double s = max == 0 ? 0 : d / max, v = max;
        h = (h + degrees + 360) % 360;
        double cc = v * s, x = cc * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - cc;
        (double r1, double g1, double b1) = h < 60 ? (cc, x, 0.0) : h < 120 ? (x, cc, 0.0) : h < 180 ? (0.0, cc, x) : h < 240 ? (0.0, x, cc) : h < 300 ? (x, 0.0, cc) : (cc, 0.0, x);
        return "#FF" + ((byte)((r1 + m) * 255)).ToString("X2") + ((byte)((g1 + m) * 255)).ToString("X2") + ((byte)((b1 + m) * 255)).ToString("X2");
    }
    private static Brush Frozen(Brush b) { b.Freeze(); return b; }
    private static void Set(ResourceDictionary r, string key, string hex) => r[key] = Frozen(new SolidColorBrush(C(hex)));

    private static bool SystemIsDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int v ? v == 0 : true;
    }
}
