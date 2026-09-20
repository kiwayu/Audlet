namespace WindowsAudiobookPlayer.Data;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsAudiobookPlayer");

    public static string Covers => Path.Combine(Root, "covers");
}
