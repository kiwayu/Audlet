using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.Views;

public static class ThemeService
{
    public static bool IsDark { get; private set; } = true;

    public static void Start(Func<ThemeOverride> overrideGetter)
    {
        Apply(overrideGetter());
        SystemEvents.UserPreferenceChanged += (_, _) => Application.Current.Dispatcher.BeginInvoke(() => Apply(overrideGetter()));
    }

    public static void Apply(ThemeOverride mode)
    {
        IsDark = mode switch { ThemeOverride.Dark => true, ThemeOverride.Light => false, _ => SystemIsDark() };
        Application.Current.ThemeMode = mode switch { ThemeOverride.Dark => ThemeMode.Dark, ThemeOverride.Light => ThemeMode.Light, _ => ThemeMode.System };
        var r = Application.Current.Resources;
        Set(r, "GlassBrush", IsDark ? "#66000000" : "#99FFFFFF");
        Set(r, "GlassBorderBrush", IsDark ? "#33FFFFFF" : "#22000000");
        Set(r, "GlassHoverBrush", IsDark ? "#40FFFFFF" : "#22000000");
        Set(r, "GlassTextBrush", IsDark ? "#FFFFFFFF" : "#FF1A1A1A");
        Set(r, "GlassMutedBrush", IsDark ? "#99FFFFFF" : "#991A1A1A");
        Set(r, "AccentFillBrush", IsDark ? "#FFFFFFFF" : "#FF1A1A1A");
        Set(r, "AccentTextBrush", IsDark ? "#FF202020" : "#FFFFFFFF");
        Set(r, "ScrimBrush", IsDark ? "#88000000" : "#66FFFFFF");
    }

    private static void Set(ResourceDictionary r, string key, string hex) =>
        r[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

    private static bool SystemIsDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int v ? v == 0 : true;
    }
}
