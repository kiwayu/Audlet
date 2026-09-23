using Audlet.Audio;
using Audlet.Data;
using Audlet.Library;
using Audlet.Player;
using Audlet.ViewModels;

namespace Audlet.Tests;

public class LibraryDropTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-drop-" + Guid.NewGuid().ToString("N"));

    public LibraryDropTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, true);

    private string Wav(params string[] parts)
    {
        var p = Path.Combine([_dir, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        const int rate = 8000; var frames = rate;
        using var w = new BinaryWriter(File.Create(p));
        w.Write("RIFF"u8); w.Write(36 + frames * 2); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write("data"u8); w.Write(frames * 2);
        for (var i = 0; i < frames; i++) w.Write((short)(3000 * Math.Sin(i / 10.0)));
        return p;
    }

    [Fact]
    public async Task DroppedFolderAndFileJoinTheLibraryAndSurviveRescan()
    {
        var bookFolder = Path.GetDirectoryName(Wav("Books", "Dune", "01.wav"))!;
        var loose = Wav("Downloads", "Piranesi.wav");
        Wav("Downloads", "Unrelated.wav");
        var store = new AppStore(Path.Combine(_dir, "data"));
        store.Load();
        using var player = new BassPlayer();
        var vm = new MainViewModel(store, new PlaybackController(player, store), new CoverCache(Path.Combine(_dir, "covers")));
        Assert.True(vm.FirstRun);

        await vm.AddDroppedAsync([bookFolder, loose, Path.Combine(_dir, "nope.txt")]);

        Assert.False(vm.FirstRun);
        Assert.Equal([bookFolder], store.Settings.RootFolders);
        Assert.Equal([loose], store.Settings.LooseFiles);
        Assert.Equal(["Dune", "Piranesi"], store.Library.Books.Select(b => b.Title).OrderBy(t => t));
        Assert.Equal("Added 2 items to the library", vm.Toast);

        await vm.RescanAsync();
        Assert.Equal(2, store.Library.Books.Count);
        Assert.All(store.Library.Books, b => Assert.False(b.Missing));

        await vm.AddDroppedAsync([loose]);
        Assert.Equal("Nothing to add: drop folders or audio files", vm.Toast);
    }
}
