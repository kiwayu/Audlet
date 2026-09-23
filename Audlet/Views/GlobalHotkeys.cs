using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Audlet.Player;

namespace Audlet.Views;

// System-wide shortcuts that work while another app has focus: Ctrl+Alt+Space, Ctrl+Alt+Left/Right, plus Shift for the big jumps.
public sealed class GlobalHotkeys : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 1, ModControl = 2, ModShift = 4, ModNoRepeat = 0x4000;
    private const uint VkSpace = 0x20, VkLeft = 0x25, VkRight = 0x27;

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
            RegisterHotKey(h, 1, ModControl | ModAlt | ModNoRepeat, VkSpace);
            RegisterHotKey(h, 2, ModControl | ModAlt | ModNoRepeat, VkLeft);
            RegisterHotKey(h, 3, ModControl | ModAlt | ModNoRepeat, VkRight);
            RegisterHotKey(h, 4, ModControl | ModAlt | ModShift | ModNoRepeat, VkLeft);
            RegisterHotKey(h, 5, ModControl | ModAlt | ModShift | ModNoRepeat, VkRight);
        }
        else for (var id = 1; id <= 5; id++) UnregisterHotKey(h, id);
        _registered = on;
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey) return IntPtr.Zero;
        var s = _controller.Settings;
        switch (wParam.ToInt32())
        {
            case 1: _controller.TogglePlay(); break;
            case 2: _controller.Jump(-s.SmallRewindSec); break;
            case 3: _controller.Jump(s.SmallRewindSec); break;
            case 4: _controller.Jump(-s.BigRewindSec); break;
            case 5: _controller.Jump(s.BigRewindSec); break;
            default: return IntPtr.Zero;
        }
        handled = true;
        return IntPtr.Zero;
    }

    public void Dispose() { Enable(false); _source.RemoveHook(Hook); }
}
