using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowsAudiobookPlayer.ViewModels;

public sealed class SettingsViewModel(MainViewModel main) : ObservableObject { public MainViewModel Main => main; }
