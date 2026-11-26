using Audlet.Audio;
using Audlet.Data;
using Audlet.Library;
using Audlet.Models;
using Audlet.Player;
using Audlet.Views;
using Audlet.ViewModels;

namespace Audlet.Tests;

// Rebinding a shortcut in Settings: a gesture belongs to one action at a time.
public class KeyRebindingTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-keys-" + Guid.NewGuid().ToString("N"));
    private readonly BassPlayer _player = new();
    private readonly AppStore _store;
    private readonly MainViewModel _main;
    private readonly SettingsViewModel _vm;

    public KeyRebindingTests()
    {
        // Debounced saves never fire, so nothing is written after the folder is deleted.
        _store = new AppStore(_dir) { DebounceMs = Timeout.Infinite };
        _store.Load();
        _main = new MainViewModel(_store, new PlaybackController(_player, _store, UiTimer.Every), new CoverCache(Path.Combine(_dir, "covers")), new WpfShell());
        _vm = new SettingsViewModel(_main);
    }

    public void Dispose()
    {
        _player.Dispose();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private KeyRow Row(string action) => _vm.KeyRows.Single(r => r.Action == action);
    private KeyRow GlobalRow(string action) => _vm.GlobalRows.Single(r => r.Action == action);

    [Fact]
    public void TakingAGestureUnbindsTheActionThatHadIt()
    {
        _vm.SetKey(Row("Forward"), "Left");

        Assert.Equal("Left", Row("Forward").Gesture);
        Assert.Equal("Not set", Row("Rewind").Display);
        Assert.Equal("Left", Keybindings.Gesture(_store.Settings, "Forward"));
        Assert.Equal("", Keybindings.Gesture(_store.Settings, "Rewind"));
        Assert.Equal("Forward", Keybindings.ActionFor(_store.Settings, "Left"));
        Assert.Equal("Space", Keybindings.Gesture(_store.Settings, "PlayPause"));
    }

    [Fact]
    public void AGlobalShortcutTakesItsGestureFromTheOtherGlobalAction()
    {
        var reloads = 0;
        _main.HotkeysChanged += () => reloads++;

        _vm.SetKey(GlobalRow("Forward"), "Ctrl+Alt+Left");

        Assert.Equal("Ctrl+Alt+Left", Keybindings.GlobalGesture(_store.Settings, "Forward"));
        Assert.Equal("", Keybindings.GlobalGesture(_store.Settings, "Rewind"));
        Assert.Equal("Not set", GlobalRow("Rewind").Display);
        Assert.Equal(1, reloads);
        // In-app shortcuts are a separate list and keep their own gestures.
        Assert.Equal("Left", Keybindings.Gesture(_store.Settings, "Rewind"));
    }

    [Fact]
    public void AGlobalShortcutWithoutCtrlAltOrWinIsRefused()
    {
        var reloads = 0;
        _main.HotkeysChanged += () => reloads++;

        _vm.SetKey(GlobalRow("PlayPause"), "Shift+Space");

        Assert.Equal("A global shortcut needs Ctrl, Alt or Win in it", _main.Toast);
        Assert.Equal("Ctrl+Alt+Space", GlobalRow("PlayPause").Gesture);
        Assert.Equal("Ctrl+Alt+Space", Keybindings.GlobalGesture(_store.Settings, "PlayPause"));
        Assert.Equal(0, reloads);
    }
}
