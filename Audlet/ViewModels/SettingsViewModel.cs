using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using Audlet.Data;
using Audlet.Models;
using Audlet.Views;

namespace Audlet.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private Settings S => _main.Store.Settings;
    public int[] RewindChoices => Settings.RewindChoices;
    public int[] SleepChoices => Settings.SleepChoices;
    public EndOfBookAction[] EndOfBookActions { get; } = Enum.GetValues<EndOfBookAction>();
    public IReadOnlyList<string> Themes => ThemeCatalog.Names;
    public ObservableCollection<string> RootFolders { get; }
    public ObservableCollection<string> LooseFiles { get; }
    public ObservableCollection<KeyRow> KeyRows { get; } = [];
    public ObservableCollection<KeyRow> GlobalRows { get; } = [];

    public SettingsViewModel(MainViewModel main) { _main = main; RootFolders = new(S.RootFolders); LooseFiles = new(S.LooseFiles); LoadKeys(); }

    private void LoadKeys()
    {
        KeyRows.Clear();
        foreach (var (action, label) in Keybindings.Actions) KeyRows.Add(new KeyRow(action, label) { Gesture = Keybindings.Gesture(S, action) });
        GlobalRows.Clear();
        foreach (var (action, label) in Keybindings.GlobalActions) GlobalRows.Add(new KeyRow(action, label, global: true) { Gesture = Keybindings.GlobalGesture(S, action) });
    }

    // Binding a gesture already used elsewhere takes it away from the other action, so a key never does two things.
    public void SetKey(KeyRow row, string gesture)
    {
        if (row.Global)
        {
            if (!Keybindings.IsGlobalGesture(gesture)) { _main.ShowToast("A global shortcut needs Ctrl, Alt or Win in it"); return; }
            foreach (var other in GlobalRows.Where(r => r != row && r.Gesture == gesture)) { other.Gesture = ""; S.GlobalKeys[other.Action] = ""; }
            row.Gesture = gesture; S.GlobalKeys[row.Action] = gesture;
            Apply();
            _main.ReloadHotkeys();
            return;
        }
        foreach (var other in KeyRows.Where(r => r != row && r.Gesture == gesture)) { other.Gesture = ""; S.Keys[other.Action] = ""; }
        row.Gesture = gesture; S.Keys[row.Action] = gesture;
        Apply();
    }

    [RelayCommand] private void ResetKeys() { S.Keys = Keybindings.Defaults(); S.GlobalKeys = Keybindings.GlobalDefaults(); LoadKeys(); Apply(); _main.ReloadHotkeys(); }

    public int SmallRewind { get => S.SmallRewindSec; set { S.SmallRewindSec = value; Apply(); } }
    public int BigRewind { get => S.BigRewindSec; set { S.BigRewindSec = value; Apply(); } }
    public bool ShowPrevNext { get => S.ShowPrevNextFile; set { S.ShowPrevNextFile = value; Apply(); } }
    public bool AutoRewind { get => S.AutoRewind; set { S.AutoRewind = value; Apply(); } }
    public bool CloseToTray { get => S.CloseToTray; set { S.CloseToTray = value; Apply(); } }
    public bool CheckForUpdates { get => S.CheckForUpdates; set { S.CheckForUpdates = value; Apply(); } }
    public bool CoverLookup { get => S.AllowCoverLookup; set { S.AllowCoverLookup = value; Apply(); } }
    public string BackupFolderLabel => S.BackupFolder ?? "no second folder";
    public bool GlobalHotkeys { get => S.GlobalHotkeys; set { _main.SetHotkeys(value); Apply(); } }
    public bool PauseOnDeviceLoss { get => S.PauseOnDeviceLoss; set { S.PauseOnDeviceLoss = value; Apply(); } }
    public bool SkipSilence { get => S.SkipSilence; set { S.SkipSilence = value; Apply(); } }
    public bool Mono { get => S.Mono; set { S.Mono = value; Apply(); } }
    public double Balance { get => S.Balance; set { S.Balance = value; Apply(); } }
    public EndOfBookAction EndOfBook { get => S.EndOfBookAction; set { S.EndOfBookAction = value; Apply(); } }
    public int SleepDefault { get => S.SleepDefaultMin; set { S.SleepDefaultMin = value; Apply(); } }
    public bool SleepFadeout { get => S.SleepFadeout; set { S.SleepFadeout = value; Apply(); } }
    public bool ScheduleEnabled { get => S.SleepSchedule.Enabled; set { S.SleepSchedule.Enabled = value; Apply(); } }
    public string ScheduleFrom { get => S.SleepSchedule.FromTime.ToString("HH:mm"); set { if (TimeOnly.TryParse(value, out var t)) { S.SleepSchedule.FromTime = t; Apply(); } } }
    public string ScheduleTo { get => S.SleepSchedule.ToTime.ToString("HH:mm"); set { if (TimeOnly.TryParse(value, out var t)) { S.SleepSchedule.ToTime = t; Apply(); } } }
    public string Theme { get => S.Theme; set { S.Theme = value; ThemeService.Apply(value); Apply(); } }

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

    [RelayCommand]
    private void BackUp()
    {
        var dlg = new SaveFileDialog { Title = "Save a backup", Filter = "Zip archive|*.zip", FileName = "Audlet-backup-" + DateTime.Today.ToString("yyyy-MM-dd") + ".zip" };
        if (dlg.ShowDialog() != true) return;
        _main.Store.SaveNow();
        try { Backup.Create(AppPaths.Root, dlg.FileName); _main.ShowToast("Backup saved"); }
        catch (Exception ex) { ErrorLog.Write(AppPaths.Root, "backup", ex); _main.ShowToast("Could not save the backup: " + ex.Message); }
    }

    [RelayCommand]
    private void Restore()
    {
        var dlg = new OpenFileDialog { Title = "Restore a backup", Filter = "Zip archive|*.zip" };
        if (dlg.ShowDialog() != true) return;
        var answer = MessageBox.Show("Replace your library, settings and statistics with this backup? Audlet will restart.", "Restore", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.OK) return;
        try
        {
            Backup.Restore(AppPaths.Root, dlg.FileName);
            _main.Store.Load();
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true });
            Application.Current.Shutdown();
        }
        catch (Exception ex) { ErrorLog.Write(AppPaths.Root, "restore", ex); _main.ShowToast("Could not restore: " + ex.Message); }
    }

    [RelayCommand] private void OpenDataFolder() => Process.Start("explorer.exe", AppPaths.Root);

    [RelayCommand]
    private void ChooseBackupFolder()
    {
        var dlg = new OpenFolderDialog { Title = "Choose a second folder for daily backups" };
        if (dlg.ShowDialog() != true) return;
        S.BackupFolder = dlg.FolderName;
        OnPropertyChanged(nameof(BackupFolderLabel));
        _main.Store.SaveNow();
        try { Backup.Daily(AppPaths.Root, DateTime.Today, S.BackupFolder); _main.ShowToast("Backup written to " + Path.GetFileName(S.BackupFolder)); }
        catch (Exception ex) { ErrorLog.Write(AppPaths.Root, "backup", ex); _main.ShowToast("Could not write there: " + ex.Message); }
    }

    [RelayCommand] private void ClearBackupFolder() { S.BackupFolder = null; OnPropertyChanged(nameof(BackupFolderLabel)); Apply(); }
}

public sealed partial class KeyRow(string action, string label, bool global = false) : ObservableObject
{
    public string Action { get; } = action;
    public string Label { get; } = label;
    public bool Global { get; } = global;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Display))] private string _gesture = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Display))] private bool _capturing;
    public string Display => Capturing ? "Press a key..." : Gesture == "" ? "Not set" : Gesture;
}
