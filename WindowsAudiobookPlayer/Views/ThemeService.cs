using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace WindowsAudiobookPlayer.Views;

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
    }

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    private static Brush Frozen(Brush b) { b.Freeze(); return b; }
    private static void Set(ResourceDictionary r, string key, string hex) => r[key] = Frozen(new SolidColorBrush(C(hex)));

    private static bool SystemIsDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int v ? v == 0 : true;
    }
}
