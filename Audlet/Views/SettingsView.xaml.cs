using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Audlet.Models;
using Audlet.ViewModels;

namespace Audlet.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private static KeyRow Row(object sender) => (KeyRow)((Button)sender).DataContext;

    private void KeyButton_Click(object sender, RoutedEventArgs e) { Row(sender).Capturing = true; ((Button)sender).Focus(); }

    private void KeyButton_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var row = Row(sender);
        if (!row.Capturing) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (Keybindings.IsModifier(key)) return;
        row.Capturing = false;
        if (key != Key.Escape) ((SettingsViewModel)DataContext).SetKey(row, Keybindings.Format(key, Keyboard.Modifiers));
    }

    private void KeyButton_LostFocus(object sender, KeyboardFocusChangedEventArgs e) => Row(sender).Capturing = false;
}
