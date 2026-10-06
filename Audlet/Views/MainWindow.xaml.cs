using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Audlet.Models;
using Audlet.ViewModels;

namespace Audlet.Views;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;
    private TrayIcon? _tray;
    private GlobalHotkeys? _hotkeys;
    private bool _exiting;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        PreviewKeyDown += OnKeyDown;
        PreviewMouseDown += (o, a) => Vm.Controller.ResetSleep();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var s = Vm.Store.Settings;
        if (!double.IsNaN(s.WindowLeft)) { Left = s.WindowLeft; Top = s.WindowTop; }
        Width = s.WindowWidth; Height = s.WindowHeight;
        if (s.WindowMaximized) WindowState = WindowState.Maximized;
        SidebarCol.Width = new GridLength(s.SidebarWidth);
        _hotkeys = new GlobalHotkeys(this, Vm.Controller);
        _hotkeys.Enable(s.GlobalHotkeys);
        Vm.HotkeysToggled += on => _hotkeys.Enable(on);
        _ = new TaskbarButtons(this, Vm.Controller);
        _tray = new TrayIcon(Vm.Controller);
        _tray.ShowRequested += Restore;
        _tray.ExitRequested += ForceClose;
        Vm.PropertyChanged += (o, a) => { if (a.PropertyName is nameof(MainViewModel.Section) or nameof(MainViewModel.FirstRun)) ShowSection(); };
        Vm.Controller.PropertyChanged += (o, a) => { if (a.PropertyName == nameof(Player.PlaybackController.CurrentBook)) UpdatePlayerBar(); };
        ShowSection();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var s = Vm.Store.Settings;
        if (s.CloseToTray && !_exiting)
        {
            // Keep playing from the tray; Exit in the tray menu quits for real.
            e.Cancel = true;
            Hide();
            return;
        }
        _tray?.Dispose();
        _hotkeys?.Dispose();
        s.WindowMaximized = WindowState == WindowState.Maximized;
        if (WindowState == WindowState.Normal) { s.WindowLeft = Left; s.WindowTop = Top; s.WindowWidth = Width; s.WindowHeight = Height; }
        s.SidebarWidth = SidebarCol.ActualWidth;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        Vm.Controller.ResetSleep();
        // Text boxes keep their keys, and so does a shortcut button in Settings while it waits for a key press.
        if (Keyboard.FocusedElement is TextBox or Button { Tag: "key" }) return;
        var c = Vm.Controller; var s = Vm.Store.Settings;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        switch (key)
        {
            case Key.Escape when Vm.Section == Section.NowPlaying: Vm.Section = Section.Library; e.Handled = true; return;
            case Key.MediaPlayPause: c.TogglePlay(); e.Handled = true; return;
            case Key.MediaNextTrack: c.NextChapter(); e.Handled = true; return;
            case Key.MediaPreviousTrack: c.PrevChapter(); e.Handled = true; return;
        }
        if (Keybindings.IsModifier(key)) return;
        switch (Keybindings.ActionFor(s, Keybindings.Format(key, Keyboard.Modifiers)))
        {
            case "PlayPause": c.TogglePlay(); break;
            case "Rewind": c.Jump(-s.SmallRewindSec); break;
            case "Forward": c.Jump(s.SmallRewindSec); break;
            case "BigRewind": c.Jump(-s.BigRewindSec); break;
            case "BigForward": c.Jump(s.BigRewindSec); break;
            case "PrevChapter": c.PrevChapter(); break;
            case "NextChapter": c.NextChapter(); break;
            case "SpeedUp": c.AdjustSpeed(0.05); break;
            case "SpeedDown": c.AdjustSpeed(-0.05); break;
            case "Bookmark": c.AddBookmark(""); break;
            case "Lock": c.Locked = !c.Locked; break;
            default: return;
        }
        e.Handled = true;
    }

    private void ShowSection()
    {
        if (Vm.FirstRun && Vm.Section != Section.Settings) { Content.Content = new FirstRunView { DataContext = Vm }; return; }
        Content.Content = Vm.Section switch
        {
            Section.NowPlaying => new NowPlayingView { DataContext = new NowPlayingViewModel(Vm) },
            Section.Bookmarks => new BookmarksView { DataContext = new BookmarksViewModel(Vm) },
            Section.Statistics => new StatisticsView { DataContext = new StatisticsViewModel(Vm) },
            Section.Settings => new SettingsView { DataContext = new SettingsViewModel(Vm) },
            _ => new LibraryView { DataContext = new LibraryViewModel(Vm, Vm.Section) },
        };
        var tag = Vm.Section == Section.NowPlaying ? null : Vm.Section.ToString();
        foreach (ListBoxItem item in Nav.Items) item.IsSelected = (string)item.Tag == tag;
        UpdatePlayerBar();
    }

    private void UpdatePlayerBar() =>
        PlayerBar.Visibility = Vm.Controller.CurrentBook is not null && Vm.Section != Section.NowPlaying ? Visibility.Visible : Visibility.Collapsed;

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Nav.SelectedItem is ListBoxItem { Tag: string tag } && Enum.TryParse<Section>(tag, out var s) && s != Vm.Section) Vm.Section = s;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) await Vm.AddDroppedAsync(paths);
    }

    public void Restore()
    {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }

    public void ForceClose() { _exiting = true; Close(); }

    private void Speed_Click(object s, RoutedEventArgs e) { Vm.Controller.CycleSpeed(); e.Handled = true; }
    private void DozeKeep_Click(object s, RoutedEventArgs e) => Vm.Controller.ClearDoze();
    private void DozeRewind_Click(object s, RoutedEventArgs e) => Vm.Controller.RewindToDoze();

    private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object s, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object s, RoutedEventArgs e) => Close();

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }

    private void Sidebar_DragCompleted(object sender, DragCompletedEventArgs e) => Vm.Store.Settings.SidebarWidth = SidebarCol.ActualWidth;
    private void PlayerBar_Click(object sender, MouseButtonEventArgs e) { if (e.OriginalSource is not Button) Vm.ToggleNowPlaying(); }
    private void Play_Click(object s, RoutedEventArgs e) { Vm.Controller.TogglePlay(); e.Handled = true; }
    private void SmallBack_Click(object s, RoutedEventArgs e) { Vm.Controller.Jump(-Vm.Store.Settings.SmallRewindSec); e.Handled = true; }
    private void SmallFwd_Click(object s, RoutedEventArgs e) { Vm.Controller.Jump(Vm.Store.Settings.SmallRewindSec); e.Handled = true; }
    private void BigBack_Click(object s, RoutedEventArgs e) { Vm.Controller.Jump(-Vm.Store.Settings.BigRewindSec); e.Handled = true; }
    private void BigFwd_Click(object s, RoutedEventArgs e) { Vm.Controller.Jump(Vm.Store.Settings.BigRewindSec); e.Handled = true; }
    private void Sleep_Click(object s, RoutedEventArgs e)
    {
        if (Vm.Controller.SleepActive) Vm.Controller.StopSleep(); else Vm.Controller.StartSleep(Vm.Store.Settings.SleepDefaultMin);
        e.Handled = true;
    }
}
