using Audlet.Library;
using Audlet.Models;

namespace Audlet.Tests;

public class BookMoverTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "wap-move-" + Guid.NewGuid().ToString("N"));
    public BookMoverTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);

    private string Touch(params string[] parts)
    {
        var p = Path.Combine([_root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, "");
        return p;
    }

    [Fact]
    public void RenameFolderBookKeepsStateAndRewritesPaths()
    {
        var f1 = Touch("Dune", "CD1", "01.mp3"); var f2 = Touch("Dune", "CD2", "01.mp3");
        var book = new Book { Id = Path.Combine(_root, "Dune"), Title = "Dune", State = BookState.Started, PositionSec = 42, FileIndex = 1,
            Files = [new AudioFile(f1, 10, "a"), new AudioFile(f2, 10, "b")], CoverPath = "x" };
        book.Bookmarks.Add(new Bookmark(0, 1, "n", DateTime.UtcNow));

        var target = BookMover.Rename(book, "Dune Messiah");

        Assert.Equal(Path.Combine(_root, "Dune Messiah"), target);
        Assert.Equal(target, book.Id);
        Assert.Equal("Dune Messiah", book.Title);
        Assert.True(Directory.Exists(target)); Assert.False(Directory.Exists(Path.Combine(_root, "Dune")));
        Assert.Equal(Path.Combine(target, "CD2", "01.mp3"), book.Files[1].Path);
        Assert.True(File.Exists(book.Files[1].Path));
        Assert.Equal(42, book.PositionSec); Assert.Equal(1, book.FileIndex); Assert.Single(book.Bookmarks);
        Assert.Null(book.CoverPath);
    }

    [Fact]
    public void MoveStandaloneFileIntoFolderKeepsExtension()
    {
        var file = Touch("Piranesi.m4b");
        var book = new Book { Id = file, Title = "Piranesi", Files = [new AudioFile(file, 10, "a")] };
        var genre = Path.Combine(_root, "Fantasy");

        var target = BookMover.MoveInto(book, genre);

        Assert.Equal(Path.Combine(genre, "Piranesi.m4b"), target);
        Assert.True(File.Exists(target)); Assert.False(File.Exists(file));
        Assert.Equal(target, book.Files[0].Path);
    }

    [Fact]
    public void RenameStandaloneFileKeepsExtension()
    {
        var file = Touch("Piranesi.m4b");
        var book = new Book { Id = file, Files = [new AudioFile(file, 10, "a")] };
        Assert.Equal(Path.Combine(_root, "Piranesi (Clarke).m4b"), BookMover.Rename(book, "Piranesi (Clarke)"));
    }

    [Theory]
    [InlineData(@"..\outside")]
    [InlineData(@"C:\elsewhere")]
    [InlineData("sub/dir")]
    [InlineData("..")]
    public void RejectsNamesThatAreNotASingleSegment(string name)
    {
        var dir = Path.Combine(_root, "Keep Me");
        Directory.CreateDirectory(dir);
        var book = new Book { Id = dir, Files = [new AudioFile(Path.Combine(dir, "01.mp3"), 1, "x")] };
        Assert.Throws<ArgumentException>(() => BookMover.Rename(book, name));
        Assert.True(Directory.Exists(dir));
    }

    [Fact]
    public void RefusesToOverwrite()
    {
        Touch("A", "01.mp3"); Touch("B", "01.mp3");
        var book = new Book { Id = Path.Combine(_root, "A") };
        Assert.Throws<IOException>(() => BookMover.Rename(book, "B"));
        Assert.Equal(Path.Combine(_root, "A"), book.Id);
    }
}
