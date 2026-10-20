using System.IO.Compression;
using Audlet.Data;

namespace Audlet.Tests;

public class BackupTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-backup-" + Guid.NewGuid().ToString("N"));
    public BackupTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, true);

    private string Root(string name)
    {
        var root = Path.Combine(_dir, name);
        Directory.CreateDirectory(Path.Combine(root, "covers"));
        File.WriteAllText(Path.Combine(root, "library.json"), "{\"books\":[]}");
        File.WriteAllText(Path.Combine(root, "settings.json"), "{\"theme\":\"Nord\"}");
        File.WriteAllText(Path.Combine(root, "stats.json"), "{}");
        File.WriteAllBytes(Path.Combine(root, "covers", "abc.jpg"), [1, 2, 3]);
        return root;
    }

    [Fact]
    public void ZipRoundTripsDataAndCovers()
    {
        var source = Root("source");
        var zip = Path.Combine(_dir, "backup.zip");
        Backup.Create(source, zip);

        var target = Path.Combine(_dir, "target");
        Directory.CreateDirectory(target);
        Backup.Restore(target, zip);

        Assert.Equal("{\"theme\":\"Nord\"}", File.ReadAllText(Path.Combine(target, "settings.json")));
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(target, "covers", "abc.jpg")));
    }

    [Fact]
    public void RestoreIgnoresEntriesOutsideTheKnownNames()
    {
        var zip = Path.Combine(_dir, "odd.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            archive.CreateEntry("library.json").Open().Close();
            using (var w = new StreamWriter(archive.CreateEntry("../escape.json").Open())) w.Write("x");
            using (var w = new StreamWriter(archive.CreateEntry("covers/../escape.jpg").Open())) w.Write("x");
            using (var w = new StreamWriter(archive.CreateEntry("other.txt").Open())) w.Write("x");
        }
        var target = Path.Combine(_dir, "restore");
        Directory.CreateDirectory(target);

        Backup.Restore(target, zip);

        Assert.False(File.Exists(Path.Combine(_dir, "escape.json")));
        Assert.False(File.Exists(Path.Combine(target, "escape.jpg")));
        Assert.False(File.Exists(Path.Combine(target, "other.txt")));
    }

    [Fact]
    public void RestoreRejectsAZipThatIsNotABackup()
    {
        var zip = Path.Combine(_dir, "random.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            using (var w = new StreamWriter(archive.CreateEntry("notes.txt").Open())) w.Write("x");
        Assert.Throws<InvalidDataException>(() => Backup.Restore(Path.Combine(_dir, "x"), zip));
    }

    [Fact]
    public void DailyCopyIsKeptForSevenDaysOnly()
    {
        var root = Root("daily");
        for (var i = 0; i < 10; i++) Backup.Daily(root, new DateTime(2026, 10, 1).AddDays(i));

        var kept = Directory.GetDirectories(Backup.DailyFolder(root)).Select(Path.GetFileName).OrderBy(n => n).ToList();
        Assert.Equal(Backup.KeepDays, kept.Count);
        Assert.Equal("2026-10-04", kept[0]);
        Assert.True(File.Exists(Path.Combine(Backup.DailyFolder(root), "2026-10-10", "library.json")));
    }

    [Fact]
    public void DailyAlsoWritesAZipToTheSecondFolder()
    {
        var root = Root("twofold");
        var second = Path.Combine(_dir, "synced");
        Directory.CreateDirectory(second);
        for (var i = 0; i < 9; i++) Backup.Daily(root, new DateTime(2026, 11, 1).AddDays(i), second);

        var zips = Directory.GetFiles(second, "Audlet-backup-*.zip").Select(Path.GetFileName).OrderBy(n => n).ToList();
        Assert.Equal(Backup.KeepDays, zips.Count);
        Assert.Equal("Audlet-backup-2026-11-03.zip", zips[0]);
    }

    [Fact]
    public void RestoreKeepsACopyOfWhatItReplaces()
    {
        var root = Root("keep");
        var zip = Path.Combine(_dir, "incoming.zip");
        Backup.Create(Root("other"), zip);
        File.WriteAllText(Path.Combine(root, "settings.json"), "{\"theme\":\"Dracula\"}");

        Backup.Restore(root, zip);

        var snapshot = Directory.GetDirectories(Backup.DailyFolder(root)).Single(d => d.Contains("before-restore"));
        Assert.Equal("{\"theme\":\"Dracula\"}", File.ReadAllText(Path.Combine(snapshot, "settings.json")));
        Assert.Equal("{\"theme\":\"Nord\"}", File.ReadAllText(Path.Combine(root, "settings.json")));
    }

    [Fact]
    public void ErrorLogAppendsAndStaysBounded()
    {
        var root = Path.Combine(_dir, "log");
        ErrorLog.Write(root, "test", new InvalidOperationException("first"));
        File.AppendAllText(ErrorLog.FilePath(root), new string('x', ErrorLog.MaxBytes + 10));
        ErrorLog.Write(root, "test", new InvalidOperationException("second"));

        var text = File.ReadAllText(ErrorLog.FilePath(root));
        Assert.Contains("second", text);
        Assert.True(text.Length < ErrorLog.MaxBytes);
    }
}
