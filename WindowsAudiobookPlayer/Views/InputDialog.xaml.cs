using System.Windows;

namespace WindowsAudiobookPlayer.Views;

public partial class InputDialog : Window
{
    public InputDialog() => InitializeComponent();

    // Returns the trimmed text, or null when cancelled or empty.
    public static string? Ask(string title, string prompt, string initial = "")
    {
        var dlg = new InputDialog { Title = title, Owner = Application.Current.MainWindow };
        dlg.Prompt.Text = prompt;
        dlg.Input.Text = initial;
        dlg.Input.SelectAll();
        dlg.Input.Focus();
        return dlg.ShowDialog() == true && dlg.Input.Text.Trim() is { Length: > 0 } text ? text : null;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
