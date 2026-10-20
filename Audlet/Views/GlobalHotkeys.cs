using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Audlet.Models;
using Audlet.Player;

namespace Audlet.Views;

// System-wide shortcuts that work while another app has focus. The gestures come from Settings and can be changed there.
public sealed class GlobalHotkeys : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 1, ModControl = 2, ModShift = 4, ModWin = 8, ModNoRepeat = 0x4000;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source;
    private readonly PlaybackController _controller;
    private bool _registered;

    public GlobalHotkeys(Window window, PlaybackController controller)
    {
        _controller = controller;
        _source = (HwndSource)PresentationSource.FromVisual(window)!;
        _source.AddHook(Hook);
    }

    public void Enable(bool on)
    {
        if (on == _registered) return;
        var h = _source.Handle;
        if (on)
        {
            for (var i = 0; i < Keybindings.GlobalActions.Length; i++)
            {
                var gesture = Keybindings.GlobalGesture(_controller.Settings, Keybindings.GlobalActions[i].Action);
                if (!Keybindings.IsGlobalGesture(gesture) || !Keybindings.TryParse(gesture, out var mods, out var key)) continue;
                RegisterHotKey(h, i + 1, Mods(mods) | ModNoRepeat, (uint)KeyInterop.VirtualKeyFromKey(key));
            }
        }
        else for (var id = 1; id <= Keybindings.GlobalActions.Length; id++) UnregisterHotKey(h, id);
        _registered = on;
    }

    // Re-registers after a gesture changes in Settings.
    public void Reload() { var was = _registered; Enable(false); Enable(was); }

    private static uint Mods(ModifierKeys m) =>
        (m.HasFlag(ModifierKeys.Control) ? ModControl : 0) | (m.HasFlag(ModifierKeys.Alt) ? ModAlt : 0)
        | (m.HasFlag(ModifierKeys.Shift) ? ModShift : 0) | (m.HasFlag(ModifierKeys.Windows) ? ModWin : 0);

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey) return IntPtr.Zero;
        var id = wParam.ToInt32();
        if (id < 1 || id > Keybindings.GlobalActions.Length) return IntPtr.Zero;
        var s = _controller.Settings;
        switch (Keybindings.GlobalActions[id - 1].Action)
        {
            case "PlayPause": _controller.TogglePlay(); break;
            case "Rewind": _controller.Jump(-s.SmallRewindSec); break;
            case "Forward": _controller.Jump(s.SmallRewindSec); break;
            case "BigRewind": _controller.Jump(-s.BigRewindSec); break;
            case "BigForward": _controller.Jump(s.BigRewindSec); break;
            default: return IntPtr.Zero;
        }
        handled = true;
        return IntPtr.Zero;
    }

    public void Dispose() { Enable(false); _source.RemoveHook(Hook); }
}
