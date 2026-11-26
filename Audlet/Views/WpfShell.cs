using System.Windows;
using Microsoft.Win32;
using Audlet.Library;
using Audlet.ViewModels;

namespace Audlet.Views;

public sealed class WpfShell : IShell
{
    public string? Ask(string title, string prompt, string initial = "") => InputDialog.Ask(title, prompt, initial);

    public string? PickFolder(string title, string? initialDirectory = null)
    {
        var dlg = new OpenFolderDialog { Title = title };
        if (initialDirectory is not null) dlg.InitialDirectory = initialDirectory;
        return dlg.ShowDialog() == true ? dlg.FolderName : null;
    }

    public string? PickFile(string? title, string filter)
    {
        var dlg = new OpenFileDialog();
        if (title is not null) dlg.Title = title;
        dlg.Filter = filter;
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? PickSaveFile(string title, string filter, string fileName)
    {
        var dlg = new SaveFileDialog { Title = title, Filter = filter, FileName = fileName };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public (CoverCandidate Pick, bool ApplyDescription)? FindCover(string title, string author)
    {
        var dlg = new CoverSearchDialog(title, author);
        return dlg.ShowDialog() == true && dlg.Chosen is { } pick ? (pick, dlg.ApplyDescription) : null;
    }

    public bool Confirm(string text, string caption) =>
        MessageBox.Show(text, caption, MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;

    public void ApplyTheme(string name) => ThemeService.Apply(name);

    public void Shutdown() => Application.Current.Shutdown();
}
