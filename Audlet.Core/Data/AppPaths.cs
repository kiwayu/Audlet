namespace Audlet.Data;

public static class AppPaths
{
    // AUDLET_DATA_DIR overrides the store location, which keeps test runs away from real data.
    public static string Root { get; } =
        Environment.GetEnvironmentVariable("AUDLET_DATA_DIR") is { Length: > 0 } dir ? dir :
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Audlet");

    public static string Covers => Path.Combine(Root, "covers");

    // Earlier builds stored data under the old app name; carry it over once.
    public static void MigrateLegacy()
    {
        var old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsAudiobookPlayer");
        if (Directory.Exists(old) && !Directory.Exists(Root)) Directory.Move(old, Root);
    }
}
