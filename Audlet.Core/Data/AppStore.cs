using System.Text.Json;
using Audlet.Models;

namespace Audlet.Data;

public sealed class AppStore
{
    private readonly string _folder;
    private readonly object _gate = new();
    private CancellationTokenSource? _pending;
    private long _taken, _written;   // numbers each save in the order its state was read, and the newest one written

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
            _written = Interlocked.Increment(ref _taken);
            WriteAll(Library, Settings, Stats);
        }
    }

    public Task SaveCopyAsync() => Task.Run(TakeCopy());

    // Copies on the calling thread, which owns the lists, and returns the write of that copy for a worker: copying takes
    // about a millisecond where writing takes tens, and a worker reading lists while they change writes a torn file.
    // A save that read newer state may run before the worker does (the exit save); the older copy is then dropped.
    private Action TakeCopy()
    {
        var library = new LibraryFile { Books = Library.Books.Select(b => b.Copy()).ToList() };
        var settings = Settings.Copy();
        var stats = Stats.Copy();
        var taken = Interlocked.Increment(ref _taken);
        return () =>
        {
            lock (_gate)
            {
                if (taken < _written) return;
                _written = taken;
                WriteAll(library, settings, stats);
            }
        };
    }

    private void WriteAll(LibraryFile library, Settings settings, StatsFile stats)
    {
        Directory.CreateDirectory(_folder);
        Write("library.json", library);
        Write("settings.json", settings);
        Write("stats.json", stats);
    }

    // The copy is taken now, on the calling thread; only its write waits out the debounce.
    public void SaveSoon()
    {
        var write = TakeCopy();
        lock (_gate)
        {
            _pending?.Cancel();
            var cts = _pending = new CancellationTokenSource();
            var token = cts.Token;
            Task.Delay(DebounceMs, token).ContinueWith(t =>
            {
                if (!t.IsCanceled) write();
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
