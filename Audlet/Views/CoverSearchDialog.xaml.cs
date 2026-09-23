using System.Windows;
using System.Windows.Controls;
using Audlet.Library;

namespace Audlet.Views;

public partial class CoverSearchDialog : Window
{
    private CancellationTokenSource? _cts;

    public CoverCandidate? Chosen { get; private set; }
    public bool ApplyDescription => UseDescription.IsChecked == true;

    public CoverSearchDialog(string title, string author)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        TitleBox.Text = title;
        AuthorBox.Text = author;
        Loaded += async (o, a) => await RunSearchAsync();
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await RunSearchAsync();

    private async Task RunSearchAsync()
    {
        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        Results.ItemsSource = null;
        UseButton.IsEnabled = false;
        Status.Text = "Searching Open Library and Google Books...";
        try
        {
            var result = await CoverSearch.SearchAsync(TitleBox.Text.Trim(), AuthorBox.Text.Trim(), cts.Token);
            if (cts.IsCancellationRequested) return;
            var hits = result.Hits;
            Results.ItemsSource = hits;
            var summary = hits.Count == 0 ? "Nothing found. Try a shorter title or leave the author blank."
                                          : hits.Count + " candidates. Pick one, then Use this cover.";
            Status.Text = result.Warnings.Count == 0 ? summary : summary + "  (" + string.Join("; ", result.Warnings) + ")";
        }
        catch (Exception ex) when (!cts.IsCancellationRequested)
        {
            Status.Text = "Search failed: " + ex.Message;
        }
    }

    private void Results_SelectionChanged(object sender, SelectionChangedEventArgs e) => UseButton.IsEnabled = Results.SelectedItem is not null;

    private void Use_Click(object sender, RoutedEventArgs e)
    {
        Chosen = Results.SelectedItem as CoverCandidate;
        DialogResult = Chosen is not null;
    }
}
