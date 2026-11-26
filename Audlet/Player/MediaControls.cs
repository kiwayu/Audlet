using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Audlet.Player;

public sealed class MediaControls
{
    private readonly PlaybackController _controller;
    private MediaPlayer? _mediaPlayer;
    private SystemMediaTransportControls? _smtc;
    private string? _thumbnail;   // the cover file and write time the thumbnail was last set from

    public MediaControls(PlaybackController controller)
    {
        _controller = controller;
        try
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.CommandManager.IsEnabled = false;
            _smtc = _mediaPlayer.SystemMediaTransportControls;
            _smtc.IsEnabled = true;
            _smtc.IsPlayEnabled = _smtc.IsPauseEnabled = _smtc.IsNextEnabled = _smtc.IsPreviousEnabled = true;
            _smtc.IsFastForwardEnabled = _smtc.IsRewindEnabled = true;
            _smtc.ButtonPressed += OnButton;
            controller.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(PlaybackController.CurrentBook) or nameof(PlaybackController.IsPlaying) or nameof(PlaybackController.CurrentChapter))
                    Update();
            };
        }
        catch (Exception) { _smtc = null; }
    }

    private void OnButton(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            switch (e.Button)
            {
                case SystemMediaTransportControlsButton.Play: _controller.Play(); break;
                case SystemMediaTransportControlsButton.Pause: _controller.Pause(); break;
                case SystemMediaTransportControlsButton.Next: _controller.NextChapter(); break;
                case SystemMediaTransportControlsButton.Previous: _controller.PrevChapter(); break;
                case SystemMediaTransportControlsButton.FastForward: _controller.Jump(_controller.Settings.SmallRewindSec); break;
                case SystemMediaTransportControlsButton.Rewind: _controller.Jump(-_controller.Settings.SmallRewindSec); break;
            }
        });
    }

    private async void Update()
    {
        if (_smtc is null) return;
        try
        {
            _smtc.PlaybackStatus = _controller.IsPlaying ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
            var u = _smtc.DisplayUpdater;
            u.Type = MediaPlaybackType.Music;
            u.MusicProperties.Title = _controller.CurrentBook?.Title ?? "";
            u.MusicProperties.Artist = _controller.CurrentBook?.Author ?? "";
            u.MusicProperties.AlbumTitle = _controller.CurrentChapter?.Title ?? "";
            // The thumbnail is looked up only when the cover file changes, and without blocking the UI thread on it.
            var cover = _controller.CurrentBook?.CoverPath;
            var key = cover is not null && File.Exists(cover) ? cover + "|" + File.GetLastWriteTimeUtc(cover).Ticks : null;
            if (key is not null && key != _thumbnail)
            {
                _thumbnail = key;
                var file = await StorageFile.GetFileFromPathAsync(cover!);
                if (key != _thumbnail) return;   // a newer book took over while the file was looked up
                u.Thumbnail = RandomAccessStreamReference.CreateFromFile(file);
            }
            u.Update();
        }
        catch (Exception) { }
    }
}
