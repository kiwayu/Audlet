using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowsAudiobookPlayer.ViewModels;

public sealed class BookmarksViewModel(MainViewModel main) : ObservableObject { public MainViewModel Main => main; }
public sealed class StatisticsViewModel(MainViewModel main) : ObservableObject { public MainViewModel Main => main; }
public sealed class SettingsViewModel(MainViewModel main) : ObservableObject { public MainViewModel Main => main; }
public sealed class CharactersViewModel(MainViewModel main) : ObservableObject { public MainViewModel Main => main; }
