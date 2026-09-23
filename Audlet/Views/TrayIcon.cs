using System.Drawing;
using System.Windows.Forms;
using Audlet.Converters;
using Audlet.Player;

namespace Audlet.Views;

// Tray icon with a control menu. The tooltip shows what is playing and where.
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly PlaybackController _controller;
    private readonly ToolStripMenuItem _playItem;

    public event Action? ShowRequested;
    public event Action? ExitRequested;

    public TrayIcon(PlaybackController controller)
    {
        _controller = controller;
        var menu = new ContextMenuStrip();
        _playItem = new ToolStripMenuItem("Play", null, (o, a) => _controller.TogglePlay());
        menu.Items.Add(_playItem);
        menu.Items.Add(new ToolStripMenuItem("Rewind", null, (o, a) => _controller.Jump(-_controller.Settings.SmallRewindSec)));
        menu.Items.Add(new ToolStripMenuItem("Forward", null, (o, a) => _controller.Jump(_controller.Settings.SmallRewindSec)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Open Audlet", null, (o, a) => ShowRequested?.Invoke()));
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (o, a) => ExitRequested?.Invoke()));

        _icon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
            Text = "Audlet",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (o, a) => { if (a.Button == MouseButtons.Left) ShowRequested?.Invoke(); };
        controller.PropertyChanged += (o, a) =>
        {
            if (a.PropertyName is nameof(PlaybackController.IsPlaying) or nameof(PlaybackController.CurrentBook) or nameof(PlaybackController.PositionSec))
                Update();
        };
        Update();
    }

    private void Update()
    {
        _playItem.Text = _controller.IsPlaying ? "Pause" : "Play";
        var book = _controller.CurrentBook;
        var text = book is null ? "Audlet"
            : book.Title + " - " + TimeConverter.Format(_controller.BookPositionSec) + " of " + TimeConverter.Format(_controller.BookTotalSec) + (_controller.IsPlaying ? "" : " (paused)");
        _icon.Text = text.Length > 127 ? text[..127] : text;   // NotifyIcon caps the tooltip length
    }

    public void Dispose() { _icon.Visible = false; _icon.Dispose(); }
}
