namespace Audlet.Tests;

public class CoreBoundaryTests
{
    // Audlet.Core must build and run without a UI framework or the BASS audio engine.
    [Fact]
    public void CoreReferencesNoUiFrameworkOrAudioEngine()
    {
        var core = typeof(Audlet.ViewModels.MainViewModel).Assembly;
        string[] banned = ["PresentationFramework", "PresentationCore", "WindowsBase", "System.Windows.Forms", "ManagedBass"];
        var references = core.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        Assert.Equal("Audlet.Core", core.GetName().Name);
        Assert.Contains("System.Runtime", references);
        Assert.DoesNotContain(references, r => banned.Any(b => r.StartsWith(b, StringComparison.OrdinalIgnoreCase)));
    }
}
