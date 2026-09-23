using System.Diagnostics;
using System.Text.Json;
using System.Windows.Automation;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Audlet.E2E;

// Launches the real executable against a throwaway data folder and a generated library of tone files.
public sealed class AppSession : IDisposable
{
    public string DataDir { get; }
    public string LibraryDir { get; }
    public string DuneId => Path.Combine(LibraryDir, "Frank Herbert", "Dune");
    private Process? _process;
    private AutomationElement? _root;

    public AppSession()
    {
        var stamp = Guid.NewGuid().ToString("N");
        DataDir = Path.Combine(Path.GetTempPath(), "wap-e2e-" + stamp, "data");
        LibraryDir = Path.Combine(Path.GetTempPath(), "wap-e2e-" + stamp, "library");
        Directory.CreateDirectory(DataDir);
        Tone(Path.Combine(LibraryDir, "Andy Weir", "Project Hail Mary", "01 - Part One.wav"), 20, 440);
        Tone(Path.Combine(LibraryDir, "Andy Weir", "Project Hail Mary", "02 - Part Two.wav"), 20, 660);
        Tone(Path.Combine(LibraryDir, "Frank Herbert", "Dune", "CD1", "01.wav"), 15, 330);
        Tone(Path.Combine(LibraryDir, "Frank Herbert", "Dune", "CD2", "01.wav"), 15, 550);
        Tone(Path.Combine(LibraryDir, "Piranesi.wav"), 12, 500);
    }

    public void WriteSettings(object settings) =>
        File.WriteAllText(Path.Combine(DataDir, "settings.json"), JsonSerializer.Serialize(settings));

    public AutomationElement Launch()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "Audlet.exe");
        // Redirecting output detaches the app from the test host console, so the runner never waits on it.
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        psi.Environment["AUDLET_DATA_DIR"] = DataDir;
        _process = Process.Start(psi)!;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            _process.Refresh();
            if (_process.HasExited) throw new InvalidOperationException("The app exited during startup with code " + _process.ExitCode);
            if (_process.MainWindowHandle != IntPtr.Zero)
            {
                _root = AutomationElement.FromHandle(_process.MainWindowHandle);
                return _root;
            }
            Thread.Sleep(200);
        }
        throw new TimeoutException("The main window did not appear.");
    }

    public AutomationElement Root => _root ?? throw new InvalidOperationException("Launch first.");

    public AutomationElement WaitFor(string automationId, int timeoutMs = 15000)
    {
        var cond = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var el = Root.FindFirst(TreeScope.Descendants, cond);
            if (el is not null) return el;
            Thread.Sleep(200);
        }
        throw new TimeoutException("No element with automation id " + automationId);
    }

    public AutomationElement WaitForName(string name, int timeoutMs = 15000)
    {
        var cond = new PropertyCondition(AutomationElement.NameProperty, name);
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var el = Root.FindFirst(TreeScope.Descendants, cond);
            if (el is not null) return el;
            Thread.Sleep(200);
        }
        throw new TimeoutException("No element named " + name);
    }

    public string Text(string automationId)
    {
        try { return WaitFor(automationId).Current.Name; }
        catch (System.Runtime.InteropServices.COMException) { return ""; }
        catch (ElementNotAvailableException) { return ""; }
    }

    public AutomationElement WaitForButton(string name, int timeoutMs = 15000)
    {
        var cond = new AndCondition(new PropertyCondition(AutomationElement.NameProperty, name),
                                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        // Popups are separate top-level windows, so look through every window the app owns.
        var windows = new PropertyCondition(AutomationElement.ProcessIdProperty, _process!.Id);
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            foreach (AutomationElement w in AutomationElement.RootElement.FindAll(TreeScope.Children, windows))
            {
                var el = w.FindFirst(TreeScope.Descendants, cond);
                if (el is not null) return el;
            }
            Thread.Sleep(200);
        }
        throw new TimeoutException("No button named " + name);
    }

    public void InvokeButton(string name) =>
        ((InvokePattern)WaitForButton(name).GetCurrentPattern(InvokePattern.Pattern)).Invoke();

    public string WaitForText(string automationId, Func<string, bool> ready, int timeoutMs = 15000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var last = "";
        while (DateTime.UtcNow < deadline)
        {
            last = Text(automationId);
            if (ready(last)) return last;
            Thread.Sleep(200);
        }
        throw new TimeoutException("Text of " + automationId + " stayed at " + last);
    }

    public void Invoke(string automationId) =>
        ((InvokePattern)WaitFor(automationId).GetCurrentPattern(InvokePattern.Pattern)).Invoke();

    public void SelectNav(string label) =>
        ((SelectionItemPattern)WaitForName(label).GetCurrentPattern(SelectionItemPattern.Pattern)).Select();

    public int CountByAutomationId(string automationId) =>
        Root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId)).Count;

    public void CloseAndWait()
    {
        if (_process is null || _process.HasExited) return;
        try { ((WindowPattern)Root.GetCurrentPattern(WindowPattern.Pattern)).Close(); }
        catch (Exception) { _process.CloseMainWindow(); }
        if (!_process.WaitForExit(15000)) throw new TimeoutException("The app did not exit after Close.");
    }

    public JsonDocument ReadLibrary() => JsonDocument.Parse(File.ReadAllText(Path.Combine(DataDir, "library.json")));

    public JsonElement ReadBook(string id) =>
        ReadLibrary().RootElement.GetProperty("books").EnumerateArray()
            .First(b => string.Equals(b.GetProperty("id").GetString(), id, StringComparison.OrdinalIgnoreCase));

    public void Dispose()
    {
        try { if (_process is { HasExited: false }) _process.Kill(); } catch (Exception) { }
        try { Directory.Delete(Path.GetDirectoryName(DataDir)!, true); } catch (Exception) { }
    }

    private static void Tone(string path, int seconds, double hz)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        const int rate = 22050;
        var frames = rate * seconds;
        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);
        w.Write("RIFF"u8); w.Write(36 + frames * 4); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)2); w.Write(rate); w.Write(rate * 4); w.Write((short)4); w.Write((short)16);
        w.Write("data"u8); w.Write(frames * 4);
        for (var i = 0; i < frames; i++)
        {
            var v = (short)(8000 * Math.Sin(2 * Math.PI * hz * i / rate));
            w.Write(v); w.Write(v);
        }
    }
}
