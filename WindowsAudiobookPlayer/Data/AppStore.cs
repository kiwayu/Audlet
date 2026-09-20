using System.Text.Json;
using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.Data;

public sealed class AppStore
{
    private readonly string _folder;
    private readonly object _gate = new();
    private CancellationTokenSource? _pending;

    public AppStore(string folder) => _folder = folder;

    public int DebounceMs { get; init; } = 2000;
    public LibraryFile Library { get; private set; } = new();
    public Settings Settings { get; private set; } = new();
    public StatsFile Stats { get; private set; } = new();

    public void Load()
    {
        Directory.CreateDirectory(_folder);
        Library = Read<LibraryFile>("library.json");
        Settings = Read<Settings>("settings.json");
        Stats = Read<StatsFile>("stats.json");
    }

    public void SaveNow()
    {
        lock (_gate)
        {
            _pending?.Cancel();
            _pending = null;
            Directory.CreateDirectory(_folder);
            Write("library.json", Library);
            Write("settings.json", Settings);
            Write("stats.json", Stats);
        }
    }

    public void SaveSoon()
    {
        lock (_gate)
        {
            _pending?.Cancel();
            var cts = _pending = new CancellationTokenSource();
            var token = cts.Token;
            Task.Delay(DebounceMs, token).ContinueWith(t =>
            {
                if (!t.IsCanceled) SaveNow();
            }, TaskScheduler.Default);
        }
    }

    private T Read<T>(string name) where T : new()
    {
        var path = Path.Combine(_folder, name);
        if (!File.Exists(path)) return new T();
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json.Options) ?? new T();
        }
        catch (JsonException)
        {
            File.Move(path, path + ".bad", overwrite: true);
            return new T();
        }
    }

    private void Write<T>(string name, T value)
    {
        var path = Path.Combine(_folder, name);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, Json.Options));
        File.Move(tmp, path, overwrite: true);
    }
}
