using System.Windows;
using Audlet.Audio;
using Audlet.Data;
using Audlet.Library;
using Audlet.Player;
using Audlet.ViewModels;
using Audlet.Views;

namespace Audlet;

public partial class App : Application
{
    private PlaybackController? _controller;
    private MainViewModel? _vm;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var missing = NativeLibraries.Missing();
        if (missing.Count > 0)
        {
            var nl = Environment.NewLine;
            MessageBox.Show("Missing audio libraries next to the app:" + nl + string.Join(nl, missing) + nl + nl + "Run tools" + Path.DirectorySeparatorChar + "get-bass.ps1 and start again.",
                "Audlet", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        AppPaths.MigrateLegacy();
        var store = new AppStore(AppPaths.Root);
        store.Load();
        ThemeService.Start(() => store.Settings.Theme);
        var player = new BassPlayer();
        _controller = new PlaybackController(player, store);
        _ = new MediaControls(_controller);
        var devices = new DeviceWatcher();
        devices.DeviceLost += () => { if (store.Settings.PauseOnDeviceLoss) _controller!.PauseForDeviceLoss(); };
        _vm = new MainViewModel(store, _controller, new CoverCache(AppPaths.Covers));
        var window = new MainWindow { DataContext = _vm };
        MainWindow = window;
        window.Show();
        if (store.Settings.LastBookId is { } id && store.Library.Books.FirstOrDefault(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) is { } last)
        {
            _controller.LoadBook(last, play: false);
            _vm.Section = Section.NowPlaying;
        }
        if (!_vm.FirstRun) _ = _vm.RescanAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Shutdown();
        base.OnExit(e);
    }
}
