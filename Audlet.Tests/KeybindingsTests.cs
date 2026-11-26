using System.Windows.Input;
using Audlet.Models;
using Audlet.Views;

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
        Assert.True(KeyGestures.TryParse(gesture, out var m, out var k));
        Assert.Equal(mods, m);
        Assert.Equal(key, k);
        Assert.Equal(gesture, KeyGestures.Format(k, m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Banana")]
    [InlineData("LeftCtrl")]
    public void RejectsJunk(string gesture) => Assert.False(KeyGestures.TryParse(gesture, out _, out _));

    [Fact]
    public void GlobalGesturesNeedAModifierThatIsNotJustShift()
    {
        Assert.True(Keybindings.IsGlobalGesture("Ctrl+Alt+Space"));
        Assert.True(Keybindings.IsGlobalGesture("Win+F9"));
        Assert.False(Keybindings.IsGlobalGesture("Space"));
        Assert.False(Keybindings.IsGlobalGesture("Shift+Left"));
    }

    // Settings checks the stored text; GlobalHotkeys parses it into keys. Both must agree on every gesture a key press can record.
    [Fact]
    public void GlobalRuleAgreesWithTheParsedKeysForEveryRecordableGesture()
    {
        var global = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows;
        foreach (var key in Enum.GetValues<Key>().Where(k => !KeyGestures.IsModifier(k)))
            for (var m = 0; m < 16; m++)
            {
                var gesture = KeyGestures.Format(key, (ModifierKeys)m);
                Assert.True(KeyGestures.TryParse(gesture, out var mods, out _), gesture);
                Assert.Equal((mods & global) != 0, Keybindings.IsGlobalGesture(gesture));
            }
    }

    [Fact]
    public void MissingGlobalEntriesFallBackToDefaults()
    {
        var s = new Settings { GlobalKeys = new() { ["PlayPause"] = "Win+P" } };
        Assert.Equal("Win+P", Keybindings.GlobalGesture(s, "PlayPause"));
        Assert.Equal("Ctrl+Alt+Left", Keybindings.GlobalGesture(s, "Rewind"));
    }
}
