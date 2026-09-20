using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WindowsAudiobookPlayer.Player;

public sealed class MediaControls
{
    private readonly PlaybackController _controller;
    private MediaPlayer? _mediaPlayer;
    private SystemMediaTransportControls? _smtc;

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
                case SystemMediaTransportControlsButton.Next: _controller.NextFile(); break;
                case SystemMediaTransportControlsButton.Previous: _controller.PrevFile(); break;
                case SystemMediaTransportControlsButton.FastForward: _controller.Jump(_controller.Settings.SmallRewindSec); break;
                case SystemMediaTransportControlsButton.Rewind: _controller.Jump(-_controller.Settings.SmallRewindSec); break;
            }
        });
    }

    private void Update()
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
            if (_controller.CurrentBook?.CoverPath is { } cover && File.Exists(cover))
                u.Thumbnail = RandomAccessStreamReference.CreateFromFile(StorageFile.GetFileFromPathAsync(cover).GetAwaiter().GetResult());
            u.Update();
        }
        catch (Exception) { }
    }
}
