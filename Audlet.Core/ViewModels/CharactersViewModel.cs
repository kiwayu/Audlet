using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Audlet.Models;

namespace Audlet.ViewModels;

public sealed partial class CharactersViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private Book? Book => _main.Controller.CurrentBook;
    public ObservableCollection<Character> Characters { get; } = [];
    public List<Book> FinishedBooks { get; }
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private Book? _importSource;

    public CharactersViewModel(MainViewModel main)
    {
        _main = main;
        FinishedBooks = main.Store.Library.Books.Where(b => b.State == BookState.Finished && b.Characters.Count > 0 && b != Book).ToList();
        Refresh();
    }

    private void Refresh() { Characters.Clear(); if (Book is { } b) foreach (var c in b.Characters) Characters.Add(c); }

    [RelayCommand]
    private void Add()
    {
        if (Book is null || string.IsNullOrWhiteSpace(Name)) return;
        Book.Characters.Add(new Character(Name.Trim(), Description.Trim()));
        Name = ""; Description = "";
        _main.Store.SaveSoon(); Refresh();
    }

    [RelayCommand] private void Delete(Character c) { Book?.Characters.Remove(c); _main.Store.SaveSoon(); Refresh(); }

    [RelayCommand]
    private void Import()
    {
        if (Book is null || ImportSource is null) return;
        foreach (var c in ImportSource.Characters.Where(c => Book.Characters.All(x => x.Name != c.Name))) Book.Characters.Add(c);
        _main.Store.SaveSoon(); Refresh();
    }
}
