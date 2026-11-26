using System.Windows.Controls;
using System.Windows.Input;
using Audlet.Converters;
using Audlet.Models;
using Audlet.ViewModels;

namespace Audlet.Views;

public partial class LibraryView : UserControl
{
    public LibraryView() => InitializeComponent();

    private void Cards_Upcoming(IReadOnlyList<object> items) => CoverImage.Prefetch(items.OfType<Book>().Select(b => b.CoverPath));

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { DataContext: Book book } && DataContext is LibraryViewModel vm) vm.OpenCommand.Execute(book);
    }
}
