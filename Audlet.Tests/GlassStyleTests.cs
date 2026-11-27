using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Audlet.Tests;

public class GlassStyleTests
{
    private sealed class PressableButton : Button
    {
        public void Press() => IsPressed = true;
    }

    // The pointer is over a button whenever the mouse presses it; input sets this through a key WPF keeps internal.
    private static void Hover(UIElement element) =>
        element.SetValue((DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!, true);

    // Alpha of the button's centre pixel drawn on nothing, so it is the fill's opacity on a 0-255 scale.
    private static byte CentreAlpha(Button button)
    {
        button.Measure(new Size(80, 32));
        button.Arrange(new Rect(0, 0, 80, 32));
        button.UpdateLayout();
        var bitmap = new RenderTargetBitmap(80, 32, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(button);
        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect(40, 16, 1, 1), pixel, 4, 0);
        return pixel[3];
    }

    [Fact]
    public void ThePressedAccentButtonDrawsItsFillAt85Percent() => RescanTests.OnUiThread(() =>
    {
        var glass = (ResourceDictionary)Application.LoadComponent(new Uri("/Audlet;component/Views/Glass.xaml", UriKind.Relative));
        var accent = new PressableButton { Style = (Style)glass["AccentButton"], Resources = { MergedDictionaries = { glass } } };

        Assert.Equal(255, CentreAlpha(accent));
        Hover(accent);
        Assert.Equal(224, CentreAlpha(accent));   // 0.88, the hover look
        accent.Press();
        Assert.Equal(217, CentreAlpha(accent));   // 0.85
        return Task.CompletedTask;
    });
}
