using System.Diagnostics;
using System.Text.Json;
using System.Windows.Automation;

namespace Audlet.Tests.E2E;

[Trait("Category", "E2E")]
public class DesktopFeatureE2E
{
    // A two-book series of short tone files, prescanned into library.json so the app can restore book one at startup.
    private static AppSession OpenSeries(double positionSec, string endOfBook = "Stop", object? extra = null)
    {
        var app = new AppSession();
        var seriesDir = Path.Combine(app.LibraryDir, "Series");
        var one = Path.Combine(seriesDir, "Dune 01"); var two = Path.Combine(seriesDir, "Dune 02");
        AppSession.WriteTone(Path.Combine(one, "01.wav"), 12, 300);
        AppSession.WriteTone(Path.Combine(two, "01.wav"), 12, 400);
        object Book(string id, string title, double index) => new
        {
            id, title, author = "", breadcrumb = "Series", series = "Dune", seriesIndex = index,
            files = new[] { new { path = Path.Combine(id, "01.wav"), durationSec = 12.0, title = "01" } },
            chapters = new[] { new { title = "01", fileIndex = 0, startSec = 0.0, endSec = 12.0 } },
            state = "New", addedUtc = "2026-09-20T00:00:00Z",
            positionSec = id == one ? positionSec : 0.0,
            dozeFileIndex = extra is null ? (int?)null : 0,
            dozePositionSec = extra is null ? (double?)null : 3.0,
        };
        File.WriteAllText(Path.Combine(app.DataDir, "library.json"), JsonSerializer.Serialize(new { books = new[] { Book(one, "Dune 01", 1), Book(two, "Dune 02", 2) } }));
        app.WriteSettings(new { rootFolders = new[] { app.LibraryDir }, lastBookId = one, endOfBookAction = endOfBook, closeToTray = true });
        app.Launch();
        return app;
    }

    [Fact]
    public void SpeedPillCyclesThroughPresets()
    {
        using var app = OpenSeries(0);
        app.WaitFor("Play");
        app.SelectNav("All books");
        app.WaitFor("BarSpeed");
        // The automation name carries the raw value; the visible label adds the x through ContentStringFormat.
        Assert.Equal("1", app.Text("BarSpeed"));
        app.Invoke("BarSpeed");
        Assert.Equal("1.25", app.WaitForText("BarSpeed", t => t == "1.25"));
        app.Invoke("BarSpeed");
        Assert.Equal("1.5", app.WaitForText("BarSpeed", t => t == "1.5"));
    }

    [Fact]
    public void ClosingTheWindowKeepsTheAppRunningInTheTray()
    {
        using var app = OpenSeries(0);
        app.WaitFor("Play");
        var process = app.Process;
        ((WindowPattern)app.Root.GetCurrentPattern(WindowPattern.Pattern)).Close();
        Thread.Sleep(2000);
        process.Refresh();
        Assert.False(process.HasExited);
        Assert.Equal(IntPtr.Zero, process.MainWindowHandle);   // hidden, not just minimised
    }

    [Fact]
    public void WakePromptOffersRewindAfterSleepFade()
    {
        using var app = OpenSeries(9, extra: new { });
        app.WaitFor("Play");
        app.Invoke("Play");
        app.WaitFor("DozeRewind", 5000);
        app.Invoke("DozeRewind");
        var pos = app.WaitForText("Position", t => t.StartsWith("0:0"), 5000);
        Assert.StartsWith("0:0", pos);
        app.Invoke("Play");
    }

    [Fact]
    public void SeriesLabelsAppearOnCards()
    {
        using var app = OpenSeries(0);
        app.WaitFor("Play");
        app.SelectNav("All books");
        Assert.NotNull(app.WaitForName("Dune 1 of 2"));
        Assert.NotNull(app.WaitForName("Dune 2 of 2"));
    }

    [Fact]
    public void NextBookInSeriesStartsWhenOneEnds()
    {
        using var app = OpenSeries(10.5, endOfBook: "NextBookPlay");
        app.WaitFor("Play");
        Assert.Equal("Dune 01", app.Text("BookTitle"));
        app.Invoke("Play");
        var title = app.WaitForText("BookTitle", t => t == "Dune 02", 15000);
        Assert.Equal("Dune 02", title);
        app.Invoke("Play");
    }
}
