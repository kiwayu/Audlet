using WindowsAudiobookPlayer.Library;
using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.Tests;

public class LibraryScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "wap-scan-" + Guid.NewGuid().ToString("N"));

    public LibraryScannerTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);

    private void Touch(params string[] parts)
    {
        var p = Path.Combine([_root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, "");
    }

    private static FileMeta Meta(string path) => new(60, Path.GetFileNameWithoutExtension(path), null, "Author", []);

    [Fact]
    public void FolderWithAudioIsOneBookIncludingSubfolders()
    {
        Touch("Dune", @"CD1", "01.mp3"); Touch("Dune", @"CD1", "02.mp3"); Touch("Dune", @"CD2", "01.mp3");
        Touch("Dune", "cover.jpg");
        Touch("Dune", "notes.txt");
        var books = new LibraryScanner(Meta).Scan([_root]);
        var dune = Assert.Single(books);
        Assert.Equal(Path.Combine(_root, "Dune"), dune.Id);
        Assert.Equal([@"CD1\01.mp3", @"CD1\02.mp3", @"CD2\01.mp3"], dune.Files.Select(f => Path.GetRelativePath(dune.Id, f.Path)));
        Assert.Equal(180, dune.TotalDurationSec);
        Assert.Equal(3, dune.Chapters.Count);
        Assert.Equal(new Chapter("02", 1, 0, 60), dune.Chapters[1]);
    }

    [Fact]
    public void GroupingFolderIsNotABookAndBecomesBreadcrumb()
    {
        Touch("Weir", "Hail Mary", "a.m4b"); Touch("Weir", "Martian", "b.m4b");
        var books = new LibraryScanner(Meta).Scan([_root]).OrderBy(b => b.Id).ToList();
        Assert.Equal(2, books.Count);
        Assert.Equal("Weir", books[0].Breadcrumb);
        Assert.Equal("Hail Mary", books[0].Title);
    }

    [Fact]
    public void LooseFileNextToBookFoldersIsItsOwnBook()
    {
        Touch("Piranesi.m4b"); Touch("Dune", "01.mp3");
        var books = new LibraryScanner(Meta).Scan([_root]).OrderBy(b => b.Id).ToList();
        Assert.Equal(2, books.Count);
        Assert.Equal(Path.Combine(_root, "Piranesi.m4b"), books[1].Id);
        Assert.Equal("Piranesi", books[1].Title);
    }

    [Fact]
    public void FilesSortNaturally()
    {
        Touch("B", "2.mp3"); Touch("B", "10.mp3"); Touch("B", "1.mp3");
        var b = new LibraryScanner(Meta).Scan([_root]).Single();
        Assert.Equal(["1.mp3", "2.mp3", "10.mp3"], b.Files.Select(f => Path.GetFileName(f.Path)));
    }

    [Fact]
    public void TagChaptersAreOffsetPerFile()
    {
        Touch("C", "1.m4b"); Touch("C", "2.m4b");
        FileMeta WithChapters(string p) => new(100, "T", "Album", "A", [new Chapter("x", 0, 0, 50), new Chapter("y", 0, 50, 100)]);
        var b = new LibraryScanner(WithChapters).Scan([_root]).Single();
        Assert.Equal(4, b.Chapters.Count);
        Assert.Equal(new Chapter("y", 1, 50, 100), b.Chapters[3]);
        Assert.Equal("Album", b.Title);
    }

    [Fact]
    public void CoverFilePreference()
    {
        Touch("D", "zzz.png"); Touch("D", "Folder.JPG"); Touch("D", "01.mp3");
        Assert.Equal("Folder.JPG", Path.GetFileName(AudioExtensions.PickCoverFile(Path.Combine(_root, "D"))));
        Touch("E", "b.png"); Touch("E", "a.webp"); Touch("E", "01.mp3");
        Assert.Equal("a.webp", Path.GetFileName(AudioExtensions.PickCoverFile(Path.Combine(_root, "E"))));
    }
}
