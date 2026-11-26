using Audlet.Library;

namespace Audlet.ViewModels;

// What the view models ask of the window side: dialogs, file and folder pickers, the theme, and quitting. The app's is WpfShell.
// The pickers return null when cancelled.
public interface IShell
{
    string? Ask(string title, string prompt, string initial = "");   // the trimmed text, or null when cancelled or empty
    string? PickFolder(string title, string? initialDirectory = null);
    string? PickFile(string? title, string filter);
    string? PickSaveFile(string title, string filter, string fileName);
    (CoverCandidate Pick, bool ApplyDescription)? FindCover(string title, string author);
    bool Confirm(string text, string caption);   // OK or Cancel, with a warning icon
    void ApplyTheme(string name);
    void Shutdown();
}
