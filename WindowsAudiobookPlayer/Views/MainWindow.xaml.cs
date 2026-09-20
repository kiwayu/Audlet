using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WindowsAudiobookPlayer.ViewModels;

namespace WindowsAudiobookPlayer.Views;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        PreviewKeyDown += (o, a) => Vm.Controller.ResetSleep();
        PreviewMouseDown += (o, a) => Vm.Controller.ResetSleep();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var s = Vm.Store.Settings;
        if (!double.IsNaN(s.WindowLeft)) { Left = s.WindowLeft; Top = s.WindowTop; }
        Width = s.WindowWidth; Height = s.WindowHeight;
        if (s.WindowMaximized) WindowState = WindowState.Maximized;
        SidebarCol.Width = new GridLength(s.SidebarWidth);
        Vm.PropertyChanged += (o, a) => { if (a.PropertyName is nameof(MainViewModel.Section) or nameof(MainViewModel.FirstRun)) ShowSection(); };
        Vm.Controller.PropertyChanged += (o, a) => { if (a.PropertyName == nameof(Player.PlaybackController.CurrentBook)) UpdatePlayerBar(); };
        ShowSection();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var s = Vm.Store.Settings;
        s.WindowMaximized = WindowState == WindowState.Maximized;
        if (WindowState == WindowState.Normal) { s.WindowLeft = Left; s.WindowTop = Top; s.WindowWidth = Width; s.WindowHeight = Height; }
        s.SidebarWidth = SidebarCol.ActualWidth;
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
