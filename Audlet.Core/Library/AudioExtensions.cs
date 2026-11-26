namespace Audlet.Library;

public static class AudioExtensions
{
    public static readonly HashSet<string> Audio = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".m4a", ".m4b", ".aac", ".ogg", ".opus", ".flac", ".wav", ".wv", ".wma" };

    public static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    public static bool IsAudio(string path) => Audio.Contains(Path.GetExtension(path));

    public static string? PickCoverFile(string folder)
    {
        if (!Directory.Exists(folder)) return null;
        var images = Directory.EnumerateFiles(folder).Where(f => Images.Contains(Path.GetExtension(f)))
            .OrderBy(f => f, NaturalComparer.Instance).ToList();
        return images.FirstOrDefault(f =>
                   Path.GetFileNameWithoutExtension(f).Equals("cover", StringComparison.OrdinalIgnoreCase) ||
                   Path.GetFileNameWithoutExtension(f).Equals("folder", StringComparison.OrdinalIgnoreCase))
               ?? images.FirstOrDefault();
    }
}

public sealed class NaturalComparer : IComparer<string>
{
    public static readonly NaturalComparer Instance = new();

    [System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string a, string b);

    public int Compare(string? a, string? b) => StrCmpLogicalW(a ?? "", b ?? "");
}
