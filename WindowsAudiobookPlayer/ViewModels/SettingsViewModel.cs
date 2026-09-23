using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WindowsAudiobookPlayer.Models;
using WindowsAudiobookPlayer.Views;

namespace WindowsAudiobookPlayer.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private Settings S => _main.Store.Settings;
    public int[] RewindChoices => Settings.RewindChoices;
    public int[] SleepChoices => Settings.SleepChoices;
    public EndOfBookAction[] EndOfBookActions { get; } = Enum.GetValues<EndOfBookAction>();
    public ThemeOverride[] Themes { get; } = Enum.GetValues<ThemeOverride>();
    public ObservableCollection<string> RootFolders { get; }
    public ObservableCollection<string> LooseFiles { get; }

    public SettingsViewModel(MainViewModel main) { _main = main; RootFolders = new(S.RootFolders); LooseFiles = new(S.LooseFiles); }

    public int SmallRewind { get => S.SmallRewindSec; set { S.SmallRewindSec = value; Apply(); } }
    public int BigRewind { get => S.BigRewindSec; set { S.BigRewindSec = value; Apply(); } }
    public bool ShowPrevNext { get => S.ShowPrevNextFile; set { S.ShowPrevNextFile = value; Apply(); } }
    public bool AutoRewind { get => S.AutoRewind; set { S.AutoRewind = value; Apply(); } }
    public bool SkipSilence { get => S.SkipSilence; set { S.SkipSilence = value; Apply(); } }
    public bool Mono { get => S.Mono; set { S.Mono = value; Apply(); } }
    public double Balance { get => S.Balance; set { S.Balance = value; Apply(); } }
    public EndOfBookAction EndOfBook { get => S.EndOfBookAction; set { S.EndOfBookAction = value; Apply(); } }
    public int SleepDefault { get => S.SleepDefaultMin; set { S.SleepDefaultMin = value; Apply(); } }
    public bool SleepFadeout { get => S.SleepFadeout; set { S.SleepFadeout = value; Apply(); } }
    public bool ScheduleEnabled { get => S.SleepSchedule.Enabled; set { S.SleepSchedule.Enabled = value; Apply(); } }
    public string ScheduleFrom { get => S.SleepSchedule.FromTime.ToString("HH:mm"); set { if (TimeOnly.TryParse(value, out var t)) { S.SleepSchedule.FromTime = t; Apply(); } } }
    public string ScheduleTo { get => S.SleepSchedule.ToTime.ToString("HH:mm"); set { if (TimeOnly.TryParse(value, out var t)) { S.SleepSchedule.ToTime = t; Apply(); } } }
    public ThemeOverride Theme { get => S.Theme; set { S.Theme = value; ThemeService.Apply(value); Apply(); } }

    private void Apply() { _main.Store.SaveSoon(); _main.Controller.ApplySoundSettings(); }

    [RelayCommand]
    private async Task AddRoot()
    {
        var dlg = new OpenFolderDialog { Title = "Choose your audiobooks folder" };
        if (dlg.ShowDialog() != true || S.RootFolders.Contains(dlg.FolderName, StringComparer.OrdinalIgnoreCase)) return;
        S.RootFolders.Add(dlg.FolderName); RootFolders.Add(dlg.FolderName);
        _main.FirstRun = false;
        await _main.RescanAsync();
    }

    [RelayCommand] private async Task RemoveRoot(string folder) { S.RootFolders.Remove(folder); RootFolders.Remove(folder); await _main.RescanAsync(); }
    [RelayCommand] private async Task RemoveLoose(string file) { S.LooseFiles.Remove(file); LooseFiles.Remove(file); await _main.RescanAsync(); }
    [RelayCommand] private async Task Rescan() => await _main.RescanAsync();
}
