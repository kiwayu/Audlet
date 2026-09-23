namespace WindowsAudiobookPlayer.E2E;

public class LibraryTests
{
    [Fact]
    public void FirstRunAsksForAFolder()
    {
        using var app = new AppSession();
        app.Launch();
        Assert.NotNull(app.WaitForName("Browse..."));
    }

    [Fact]
    public void ScansRootFolderIntoThreeBooks()
    {
        using var app = new AppSession();
        app.WriteSettings(new { rootFolders = new[] { app.LibraryDir } });
        app.Launch();
        app.WaitForName("Dune");
        app.WaitForName("Project Hail Mary");
        app.WaitForName("Piranesi");
        Assert.Equal(3, app.CountByAutomationId("CardTitle"));
    }

    [Fact]
    public void SidebarNavigatesBetweenSections()
    {
        using var app = new AppSession();
        app.WriteSettings(new { rootFolders = new[] { app.LibraryDir } });
        app.Launch();
        app.WaitForName("Dune");
        app.SelectNav("New");
        Assert.Equal("New", app.WaitForText("Heading", t => t == "New"));
        app.SelectNav("Finished");
        Assert.Equal("Finished", app.WaitForText("Heading", t => t == "Finished"));
        Assert.Equal(0, app.CountByAutomationId("CardTitle"));
        app.SelectNav("All books");
        Assert.Equal("Library", app.WaitForText("Heading", t => t == "Library"));
    }
}
