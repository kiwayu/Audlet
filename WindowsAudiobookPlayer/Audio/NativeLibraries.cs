namespace WindowsAudiobookPlayer.Audio;

public static class NativeLibraries
{
    public static readonly string[] Required = ["bass.dll", "bass_fx.dll", "bassflac.dll", "bassopus.dll", "basswv.dll"];

    public static IReadOnlyList<string> Missing() =>
        Required.Where(n => !File.Exists(Path.Combine(AppContext.BaseDirectory, n))).ToList();
}
