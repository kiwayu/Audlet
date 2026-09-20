using System.Windows.Controls;
using System.Windows.Input;
using WindowsAudiobookPlayer.ViewModels;

namespace WindowsAudiobookPlayer.Views;

public partial class BookmarksView : UserControl
{
    public BookmarksView() => InitializeComponent();

    private void Row_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button) return;
        if (sender is Border { DataContext: BookmarkRow r } && DataContext is BookmarksViewModel vm) vm.OpenCommand.Execute(r);
    }
}
