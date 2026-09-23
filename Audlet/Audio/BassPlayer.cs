using ManagedBass;
using ManagedBass.Fx;

namespace Audlet.Audio;

public sealed class BassPlayer : IDisposable
{
    public static readonly double[] EqCenters = [60, 230, 910, 3600, 14000];

    private int _decode;      // decoding source stream
    private int _handle;      // tempo stream, the one that plays
    private readonly int[] _eq = new int[5];
    private readonly double[] _eqGains = new double[5];
    private double _speed = 1.0;
    private int _pitch;
    private double _boostDb;
    private double _balance;
    private double _volume = 1.0;
    private SyncProcedure? _endSync;   // kept alive for the native callback
    private readonly SynchronizationContext? _ctx = SynchronizationContext.Current;

    public event Action? Ended;
    public bool Mono { get; set; }
    public bool IsOpen => _handle != 0;
    public bool IsPlaying => _handle != 0 && Bass.ChannelIsActive(_handle) == PlaybackState.Playing;

    public BassPlayer()
    {
        if (!Bass.Init()) throw new InvalidOperationException("Audio device init failed: " + Bass.LastError);
        foreach (var plugin in new[] { "bassflac.dll", "bassopus.dll", "basswv.dll" })
            Bass.PluginLoad(Path.Combine(AppContext.BaseDirectory, plugin));
        Bass.Configure(Configuration.UpdatePeriod, 50);
    }

    public bool Open(string path)
    {
        Close();
        var flags = BassFlags.Decode | BassFlags.Float | BassFlags.Prescan;
        if (Mono) flags |= BassFlags.Mono;
        _decode = Bass.CreateStream(path, 0, 0, flags);
        if (_decode == 0) return false;
        _handle = BassFx.TempoCreate(_decode, BassFlags.FxFreeSource);
        if (_handle == 0) { Bass.StreamFree(_decode); _decode = 0; return false; }
        _endSync = (_, _, _, _) => Post(() => Ended?.Invoke());
        Bass.ChannelSetSync(_handle, SyncFlags.End, 0, _endSync);
        for (var i = 0; i < 5; i++)
        {
            _eq[i] = Bass.ChannelSetFX(_handle, EffectType.PeakEQ, 0);
            ApplyEq(i);
        }
        ApplySpeed(); ApplyPitch(); ApplyVolume(); ApplyBalance();
        return true;
    }

    public void Play() { if (_handle != 0) Bass.ChannelPlay(_handle); }
    public void Pause() { if (_handle != 0) Bass.ChannelPause(_handle); }

    public double DurationSec => _handle == 0 ? 0 : Bass.ChannelBytes2Seconds(_decode, Bass.ChannelGetLength(_decode));

    public double PositionSec
    {
        get => _handle == 0 ? 0 : Bass.ChannelBytes2Seconds(_decode, Bass.ChannelGetPosition(_decode));
        set
        {
            if (_handle == 0) return;
            var clamped = Math.Clamp(value, 0, Math.Max(0, DurationSec - 0.05));
            Bass.ChannelSetPosition(_handle, Bass.ChannelSeconds2Bytes(_decode, clamped));
        }
    }

    public double Speed { get => _speed; set { _speed = Math.Clamp(value, 0.5, 3.0); ApplySpeed(); } }
    public int PitchSemitones { get => _pitch; set { _pitch = Math.Clamp(value, -12, 12); ApplyPitch(); } }
    public double BoostDb { get => _boostDb; set { _boostDb = Math.Clamp(value, 0, 12); ApplyVolume(); } }
    public double Balance { get => _balance; set { _balance = Math.Clamp(value, -1, 1); ApplyBalance(); } }
    public double Volume { get => _volume; set { _volume = Math.Clamp(value, 0, 1); ApplyVolume(); } }

    public void SetEq(double[] gainsDb)
    {
        for (var i = 0; i < 5; i++) { _eqGains[i] = Math.Clamp(gainsDb[i], -15, 15); ApplyEq(i); }
    }

    public int Level()
    {
        if (_handle == 0) return 0;
        var level = Bass.ChannelGetLevel(_handle);
        if (level == -1) return 0;
        return Math.Max(level & 0xFFFF, (level >> 16) & 0xFFFF);
    }

    private void ApplySpeed() { if (_handle != 0) Bass.ChannelSetAttribute(_handle, ChannelAttribute.Tempo, (_speed - 1.0) * 100.0); }
    private void ApplyPitch() { if (_handle != 0) Bass.ChannelSetAttribute(_handle, ChannelAttribute.Pitch, _pitch); }
    private void ApplyBalance() { if (_handle != 0) Bass.ChannelSetAttribute(_handle, ChannelAttribute.Pan, _balance); }
    private void ApplyVolume()
    {
        // BASS accepts volume above 1.0 for amplification. 12 dB is x3.98.
        // ponytail: boost clips at full scale, add a limiter DSP if it audibly distorts
        if (_handle != 0) Bass.ChannelSetAttribute(_handle, ChannelAttribute.Volume, _volume * Math.Pow(10, _boostDb / 20.0));
    }
    private void ApplyEq(int band)
    {
        if (_eq[band] == 0) return;
        Bass.FXSetParameters(_eq[band], new PeakEQParameters
        {
            lBand = band, fCenter = (float)EqCenters[band], fBandwidth = 1.0f, fGain = (float)_eqGains[band], lChannel = FXChannelFlags.All,
        });
    }

    private void Post(Action a) { if (_ctx is null) a(); else _ctx.Post(_ => a(), null); }

    public void Close()
    {
        if (_handle != 0) { Bass.ChannelStop(_handle); Bass.StreamFree(_handle); }
        _handle = 0; _decode = 0; _endSync = null;
        Array.Clear(_eq);
    }

    public void Dispose() { Close(); Bass.Free(); }
}
