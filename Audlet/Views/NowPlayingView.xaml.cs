using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Audlet.Models;
using Audlet.ViewModels;

namespace Audlet.Views;

public partial class NowPlayingView : UserControl
{
    private NowPlayingViewModel Vm => (NowPlayingViewModel)DataContext;

    public NowPlayingView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var f = Vm.Settings.ChaptersPanelFraction;
        LeftCol.Width = new GridLength(f, GridUnitType.Star);
        RightCol.Width = new GridLength(1 - f, GridUnitType.Star);
    }

    private void Splitter_DragCompleted(object s, DragCompletedEventArgs e)
    {
        var total = LeftCol.ActualWidth + RightCol.ActualWidth;
        if (total > 0) { Vm.Settings.ChaptersPanelFraction = LeftCol.ActualWidth / total; Vm.Main.Store.SaveSoon(); }
    }

    private void Tab_Checked(object s, RoutedEventArgs e)
    {
        if (s is not RadioButton { Tag: string t } || ChaptersPane is null) return;
        ChaptersPane.Visibility = t == "0" ? Visibility.Visible : Visibility.Collapsed;
        BookmarksPane.Visibility = t == "1" ? Visibility.Visible : Visibility.Collapsed;
        HistoryPane.Visibility = t == "2" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Chapter_Click(object s, MouseButtonEventArgs e) { if (s is Border { DataContext: ChapterRow r }) Vm.JumpToChapterCommand.Execute(r); }
    private void Bookmark_Click(object s, MouseButtonEventArgs e) { if (e.OriginalSource is not Button && s is Border { DataContext: Bookmark b }) Vm.JumpToBookmarkCommand.Execute(b); }
    private void History_Click(object s, MouseButtonEventArgs e) { if (s is Border { DataContext: HistoryEntry h }) Vm.UndoToCommand.Execute(h); }

    private void Seek_MouseMove(object s, MouseEventArgs e)
    {
        var x = e.GetPosition(SeekArea).X;
        var width = SeekArea.ActualWidth;
        if (width <= 0) return;
        var fraction = Math.Clamp(x / width, 0, 1);
        HoverLabel.Text = Vm.ChapterAtFraction(fraction);
        HoverLabel.Margin = new Thickness(Math.Clamp(x - 40, 0, Math.Max(0, width - 160)), -22, 0, 0);
        HoverLabel.Visibility = Visibility.Visible;
    }

    private void Seek_MouseLeave(object s, MouseEventArgs e) => HoverLabel.Visibility = Visibility.Collapsed;

    private void Seek_DragCompleted(object s, DragCompletedEventArgs e) => Vm.SeekToCommand.Execute(Seek.Value);
    private void Seek_Click(object s, MouseButtonEventArgs e) => Dispatcher.BeginInvoke(() => Vm.SeekToCommand.Execute(Seek.Value));

    private void Play_Click(object s, RoutedEventArgs e) => Vm.Controller.TogglePlay();
    private void SmallBack_Click(object s, RoutedEventArgs e) => Vm.Controller.Jump(-Vm.Settings.SmallRewindSec);
    private void SmallFwd_Click(object s, RoutedEventArgs e) => Vm.Controller.Jump(Vm.Settings.SmallRewindSec);
    private void BigBack_Click(object s, RoutedEventArgs e) => Vm.Controller.Jump(-Vm.Settings.BigRewindSec);
    private void BigFwd_Click(object s, RoutedEventArgs e) => Vm.Controller.Jump(Vm.Settings.BigRewindSec);
    private void PrevChapter_Click(object s, RoutedEventArgs e) => Vm.Controller.PrevChapter();
    private void NextChapter_Click(object s, RoutedEventArgs e) => Vm.Controller.NextChapter();
    private void QuickBookmark_Click(object s, RoutedEventArgs e) => Vm.AddBookmarkCommand.Execute(null);

    private void Speed_Click(object s, RoutedEventArgs e) => SpeedPopup.IsOpen = true;
    private void Sleep_Click(object s, RoutedEventArgs e) => SleepPopup.IsOpen = true;
    private void Eq_Click(object s, RoutedEventArgs e) => EqPopup.IsOpen = true;
    private void Boost_Click(object s, RoutedEventArgs e) => BoostPopup.IsOpen = true;
    private void Characters_Click(object s, RoutedEventArgs e) { CharactersPane.DataContext = new CharactersViewModel(Vm.Main); CharactersPopup.IsOpen = true; }
    private void SpeedSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e) { if (s is Slider { IsMouseOver: true } or Slider { IsFocused: true }) Vm.Controller.SetSpeed(e.NewValue); }
}
