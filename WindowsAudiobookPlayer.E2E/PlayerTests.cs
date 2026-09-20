using System.Text.Json;

namespace WindowsAudiobookPlayer.E2E;

public class PlayerTests
{
    private static AppSession OpenDune()
    {
        var app = new AppSession();
        app.WriteSettings(new { rootFolders = new[] { app.LibraryDir }, lastBookId = app.DuneId });
        // A pre-scanned library lets the app restore the book at startup.
        var files = new[] { Path.Combine(app.DuneId, "CD1", "01.wav"), Path.Combine(app.DuneId, "CD2", "01.wav") };
        var book = new
        {
            id = app.DuneId, title = "Dune", author = "", breadcrumb = "Frank Herbert",
            files = files.Select(f => new { path = f, durationSec = 15.0, title = "01" }),
            chapters = files.Select((f, i) => new { title = "01", fileIndex = i, startSec = 0.0, endSec = 15.0 }),
            state = "New", addedUtc = "2026-09-20T00:00:00Z",
        };
        File.WriteAllText(Path.Combine(app.DataDir, "library.json"), JsonSerializer.Serialize(new { books = new[] { book } }));
        app.Launch();
        return app;
    }

    [Fact]
    public void RestoredBookOpensOnNowPlaying()
    {
        using var app = OpenDune();
        Assert.Equal("Dune", app.Text("BookTitle"));
        Assert.Equal("Chapter 1 of 2", app.WaitForText("ChapterLabel", t => t.StartsWith("Chapter")));
        Assert.Equal("0:00", app.Text("Position"));
    }

    [Fact]
    public void PlayAdvancesAndForwardJumps()
    {
        using var app = OpenDune();
        app.WaitFor("Play");
        app.Invoke("Play");
        app.WaitForText("Position", t => t != "0:00", 8000);
        app.Invoke("SmallForward");
        var after = app.WaitForText("Position", t => t.StartsWith("0:1"), 8000);
        Assert.StartsWith("0:1", after);
        app.Invoke("Play");
    }

    [Fact]
    public void SleepButtonStartsCountdown()
    {
        using var app = OpenDune();
        app.WaitFor("Play");
        app.SelectNav("Library");
        app.WaitFor("BarSleep");
        app.Invoke("BarSleep");
        app.Invoke("BarPlay");
        var label = app.WaitForText("BarSleep", t => t.Contains("29:"), 8000);
        Assert.Contains("29:", label);
        app.Invoke("BarSleep");
        Assert.DoesNotContain(":", app.WaitForText("BarSleep", t => !t.Contains(":"), 5000));
        app.Invoke("BarPlay");
    }

    [Fact]
    public void QuickBookmarkShowsToastAndPersists()
    {
        using var app = OpenDune();
        app.WaitFor("QuickBookmark");
        app.Invoke("QuickBookmark");
        Assert.Equal("Bookmark added", app.WaitForText("Toast", t => t.Length > 0, 5000));
        app.CloseAndWait();
        var dune = app.ReadBook(app.DuneId);
        Assert.Equal(1, dune.GetProperty("bookmarks").GetArrayLength());
    }

    [Fact]
    public void PositionSurvivesClose()
    {
        using var app = OpenDune();
        app.WaitFor("Play");
        app.Invoke("Play");
        app.WaitForText("Position", t => t != "0:00" && t != "0:01", 10000);
        app.CloseAndWait();
        var dune = app.ReadBook(app.DuneId);
        Assert.True(dune.GetProperty("positionSec").GetDouble() > 1.0, "position was not saved");
        Assert.Equal("Started", dune.GetProperty("state").GetString());
    }
}
