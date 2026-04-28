using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartRewrite.App.Native;

internal static class NativeMethods
{
    internal const int WH_KEYBOARD_LL = 13;
    internal const int WH_MOUSE_LL = 14;
    internal const int EM_GETSEL = 0x00B0;
    internal const int EM_SETSEL = 0x00B1;
    internal const int EM_REPLACESEL = 0x00C2;
    internal const int WM_GETTEXT = 0x000D;
    internal const int WM_GETTEXTLENGTH = 0x000E;
    internal const int VK_A = 0x41;
    internal const int VK_APPS = 0x5D;
    internal const int VK_CONTROL = 0x11;
    internal const int VK_DOWN = 0x28;
    internal const int VK_END = 0x23;
    internal const int VK_F10 = 0x79;
    internal const int VK_HOME = 0x24;
    internal const int VK_LEFT = 0x25;
    internal const int VK_RIGHT = 0x27;
    internal const int VK_SHIFT = 0x10;
    internal const int VK_UP = 0x26;
    internal static readonly IntPtr WM_KEYUP = (IntPtr)0x0101;
    internal static readonly IntPtr WM_LBUTTONUP = (IntPtr)0x0202;
    internal static readonly IntPtr WM_RBUTTONUP = (IntPtr)0x0205;
    internal static readonly IntPtr WM_SYSKEYUP = (IntPtr)0x0105;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, StringBuilder lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    internal static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
    }

    internal static IntPtr SetMouseHook(LowLevelMouseProc proc)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(module.ModuleName), 0);
    }

    internal static POINT GetCurrentCursorPosition()
    {
        GetCursorPos(out var point);
        return point;
    }

    internal static int ReadVirtualKeyCode(IntPtr lParam)
    {
        return Marshal.ReadInt32(lParam);
    }

    internal static bool IsKeyDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    internal static EditableSelectionInfo TryCaptureEditableSelection()
    {
        var guiInfo = new GUITHREADINFO
        {
            cbSize = Marshal.SizeOf<GUITHREADINFO>()
        };

        if (!GetGUIThreadInfo(0, ref guiInfo) || guiInfo.hwndFocus == IntPtr.Zero)
        {
            return EditableSelectionInfo.Empty;
        }

        var handle = guiInfo.hwndFocus;
        if (!IsSupportedEditableControl(handle))
        {
            return new EditableSelectionInfo(handle, null, null, null);
        }

        var selection = SendMessage(handle, EM_GETSEL, IntPtr.Zero, IntPtr.Zero).ToInt32();
        var start = selection & 0xFFFF;
        var end = selection >> 16;
        var selectedText = TryGetSelectedText(handle, start, end);

        return new EditableSelectionInfo(handle, start, end, selectedText);
    }

    internal static bool IsSupportedEditableControl(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        var className = GetWindowClassName(hWnd);
        return className.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("RichEdit", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool TryReplaceEditSelection(IntPtr hWnd, int selectionStart, int selectionEnd, string text)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        SendMessage(hWnd, EM_SETSEL, (IntPtr)selectionStart, (IntPtr)selectionEnd);
        SendMessage(hWnd, EM_REPLACESEL, (IntPtr)1, text);
        return true;
    }

    private static string? TryGetSelectedText(IntPtr hWnd, int selectionStart, int selectionEnd)
    {
        if (selectionEnd <= selectionStart)
        {
            return null;
        }

        var textLength = SendMessage(hWnd, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero).ToInt32();
        if (textLength <= 0)
        {
            return null;
        }

        var buffer = new StringBuilder(textLength + 1);
        _ = SendMessage(hWnd, WM_GETTEXT, (IntPtr)buffer.Capacity, buffer);
        var fullText = buffer.ToString();

        if (string.IsNullOrEmpty(fullText) || selectionStart >= fullText.Length)
        {
            return null;
        }

        var safeEnd = Math.Min(selectionEnd, fullText.Length);
        return safeEnd <= selectionStart
            ? null
            : fullText.Substring(selectionStart, safeEnd - selectionStart);
    }

    internal static void SendCtrlPlusKey(char key)
    {
        var virtualKey = char.ToUpperInvariant(key);

        var inputs = new[]
        {
            KeyboardInput(VK_CONTROL, 0),
            KeyboardInput((ushort)virtualKey, 0),
            KeyboardInput((ushort)virtualKey, KEYEVENTF_KEYUP),
            KeyboardInput(VK_CONTROL, KEYEVENTF_KEYUP)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static string GetWindowClassName(IntPtr hWnd)
    {
        var builder = new StringBuilder(256);
        _ = GetClassName(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static INPUT KeyboardInput(ushort keyCode, uint flags)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = keyCode,
                    dwFlags = flags
                }
            }
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

internal readonly record struct EditableSelectionInfo(
    IntPtr ControlHandle,
    int? SelectionStart,
    int? SelectionEnd,
    string? SelectedText)
{
    public static EditableSelectionInfo Empty { get; } = new(IntPtr.Zero, null, null, null);
}
