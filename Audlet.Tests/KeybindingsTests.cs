using System.Windows.Input;
using Audlet.Models;

namespace Audlet.Tests;

public class KeybindingsTests
{
    [Theory]
    [InlineData("Ctrl+Alt+Space", ModifierKeys.Control | ModifierKeys.Alt, Key.Space)]
    [InlineData("Shift+Left", ModifierKeys.Shift, Key.Left)]
    [InlineData("B", ModifierKeys.None, Key.B)]
    [InlineData("Win+F9", ModifierKeys.Windows, Key.F9)]
    public void ParsesGestures(string gesture, ModifierKeys mods, Key key)
    {
        Assert.True(Keybindings.TryParse(gesture, out var m, out var k));
        Assert.Equal(mods, m);
        Assert.Equal(key, k);
        Assert.Equal(gesture, Keybindings.Format(k, m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Banana")]
    [InlineData("LeftCtrl")]
    public void RejectsJunk(string gesture) => Assert.False(Keybindings.TryParse(gesture, out _, out _));

    [Fact]
    public void GlobalGesturesNeedAModifierThatIsNotJustShift()
    {
        Assert.True(Keybindings.IsGlobalGesture("Ctrl+Alt+Space"));
        Assert.True(Keybindings.IsGlobalGesture("Win+F9"));
        Assert.False(Keybindings.IsGlobalGesture("Space"));
        Assert.False(Keybindings.IsGlobalGesture("Shift+Left"));
    }

    [Fact]
    public void MissingGlobalEntriesFallBackToDefaults()
    {
        var s = new Settings { GlobalKeys = new() { ["PlayPause"] = "Win+P" } };
        Assert.Equal("Win+P", Keybindings.GlobalGesture(s, "PlayPause"));
        Assert.Equal("Ctrl+Alt+Left", Keybindings.GlobalGesture(s, "Rewind"));
    }
}
