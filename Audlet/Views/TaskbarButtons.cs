using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Audlet.Player;

namespace Audlet.Views;

// Media controls on the taskbar thumbnail (hover the taskbar button), plus book progress on the taskbar button itself.
public sealed class TaskbarButtons
{
    private readonly PlaybackController _controller;
    private readonly TaskbarItemInfo _info = new();
    private readonly ThumbButtonInfo _play;

    public TaskbarButtons(Window window, PlaybackController controller)
    {
        _controller = controller;
        var back = new ThumbButtonInfo { Description = "Rewind", ImageSource = Glyph("") };
        _play = new ThumbButtonInfo { Description = "Play", ImageSource = Glyph("") };
        var fwd = new ThumbButtonInfo { Description = "Forward", ImageSource = Glyph("") };
        back.Click += (o, a) => _controller.Jump(-_controller.Settings.SmallRewindSec);
        _play.Click += (o, a) => _controller.TogglePlay();
        fwd.Click += (o, a) => _controller.Jump(_controller.Settings.SmallRewindSec);
        _info.ThumbButtonInfos.Add(back);
        _info.ThumbButtonInfos.Add(_play);
        _info.ThumbButtonInfos.Add(fwd);
        window.TaskbarItemInfo = _info;
        controller.PropertyChanged += (o, a) =>
        {
            if (a.PropertyName is nameof(PlaybackController.IsPlaying) or nameof(PlaybackController.BookPositionSec) or nameof(PlaybackController.CurrentBook))
                Update();
        };
        Update();
    }

    private void Update()
    {
        _play.Description = _controller.IsPlaying ? "Pause" : "Play";
        _play.ImageSource = Glyph(_controller.IsPlaying ? "" : "");
        var total = _controller.BookTotalSec;
        _info.ProgressState = _controller.CurrentBook is not null && total > 0 ? TaskbarItemProgressState.Normal : TaskbarItemProgressState.None;
        _info.ProgressValue = total > 0 ? Math.Clamp(_controller.BookPositionSec / total, 0, 1) : 0;
        _info.Description = _controller.CurrentBook?.Title ?? "Audlet";
    }

    private static readonly Dictionary<string, ImageSource> Cache = new();

    // Renders a Segoe MDL2 glyph to a small bitmap; thumb buttons need an ImageSource, not text.
    private static ImageSource Glyph(string glyph)
    {
        if (Cache.TryGetValue(glyph, out var cached)) return cached;
        const int size = 16;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var text = new FormattedText(glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe MDL2 Assets"), 12, Brushes.White, 1.0);
            dc.DrawText(text, new Point((size - text.Width) / 2, (size - text.Height) / 2));
        }
        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();
        Cache[glyph] = bmp;
        return bmp;
    }
}
