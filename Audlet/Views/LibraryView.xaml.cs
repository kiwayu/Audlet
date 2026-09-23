using System.Windows.Controls;
using System.Windows.Input;
using Audlet.Models;
using Audlet.ViewModels;

namespace Audlet.Views;

public partial class LibraryView : UserControl
{
    public LibraryView() => InitializeComponent();

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { DataContext: Book book } && DataContext is LibraryViewModel vm) vm.OpenCommand.Execute(book);
    }
}
