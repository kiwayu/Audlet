namespace WindowsAudiobookPlayer.Data;

public static class AppPaths
{
    // WAP_DATA_DIR overrides the store location, which keeps test runs away from real data.
    public static string Root { get; } =
        Environment.GetEnvironmentVariable("WAP_DATA_DIR") is { Length: > 0 } dir ? dir :
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsAudiobookPlayer");

    public static string Covers => Path.Combine(Root, "covers");
}
