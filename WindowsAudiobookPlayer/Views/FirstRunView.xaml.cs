using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WindowsAudiobookPlayer.ViewModels;

namespace WindowsAudiobookPlayer.Views;

public partial class FirstRunView : UserControl
{
    public FirstRunView() => InitializeComponent();

    private async void Choose_Click(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        var dlg = new OpenFolderDialog { Title = "Choose your audiobooks folder" };
        if (dlg.ShowDialog() != true) return;
        vm.Store.Settings.RootFolders.Add(dlg.FolderName);
        vm.FirstRun = false;
        await vm.RescanAsync();
        vm.Section = Section.Library;
    }
}
