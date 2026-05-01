using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace WindowsTrayTasks.Hotkeys;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0x0000,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
    NoRepeat = 0x4000,
}

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _handlers = new();
    private readonly List<int> _registered = new();
    private int _nextId = 9000;

    public GlobalHotkeyService()
    {
        var parameters = new HwndSourceParameters("WindowsTrayTasks.Hotkeys")
        {
            HwndSourceHook = WndProc,
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
        };
        _source = new HwndSource(parameters);
    }

    public bool TryRegister(HotkeyModifiers modifiers, Key key, Action handler, out string? error)
    {
        var id = _nextId++;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        var mods = (uint)(modifiers | HotkeyModifiers.NoRepeat);
        if (!RegisterHotKey(_source.Handle, id, mods, vk))
        {
            error = $"Could not register hotkey {modifiers}+{key} (already in use?)";
            return false;
        }
        _handlers[id] = handler;
        _registered.Add(id);
        error = null;
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && _handlers.TryGetValue(wParam.ToInt32(), out var h))
        {
            try { h(); } catch { /* swallow to keep message loop alive */ }
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _registered)
        {
            try { UnregisterHotKey(_source.Handle, id); } catch { }
        }
        _registered.Clear();
        _source.Dispose();
    }
}
