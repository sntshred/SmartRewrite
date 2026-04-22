using SmartRewrite.App.Native;

namespace SmartRewrite.App.Services;

public sealed class GlobalSelectionMonitor : IDisposable
{
    private NativeMethods.LowLevelKeyboardProc? _keyboardProc;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _mouseProc;
    private IntPtr _mouseHook = IntPtr.Zero;

    public event EventHandler<SelectionTriggerEventArgs>? ActionRequested;

    public void Start()
    {
        _mouseProc = MouseHookCallback;
        _mouseHook = NativeMethods.SetMouseHook(_mouseProc);
        _keyboardProc = KeyboardHookCallback;
        _keyboardHook = NativeMethods.SetKeyboardHook(_keyboardProc);
    }

    public void Dispose()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }

        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == NativeMethods.WM_RBUTTONUP)
        {
            var point = NativeMethods.GetCurrentCursorPosition();
            ActionRequested?.Invoke(this, new SelectionTriggerEventArgs(point.X, point.Y));
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            (wParam == NativeMethods.WM_KEYUP || wParam == NativeMethods.WM_SYSKEYUP))
        {
            var vkCode = NativeMethods.ReadVirtualKeyCode(lParam);
            if (ShouldTriggerContextAction(vkCode))
            {
                var point = NativeMethods.GetCurrentCursorPosition();
                ActionRequested?.Invoke(this, new SelectionTriggerEventArgs(point.X, point.Y));
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private static bool ShouldTriggerContextAction(int vkCode)
    {
        var shiftDown = NativeMethods.IsKeyDown(NativeMethods.VK_SHIFT);
        return vkCode == NativeMethods.VK_APPS ||
               (shiftDown && vkCode == NativeMethods.VK_F10);
    }
}

public sealed class SelectionTriggerEventArgs(int x, int y) : EventArgs
{
    public int ScreenX { get; } = x;

    public int ScreenY { get; } = y;
}
