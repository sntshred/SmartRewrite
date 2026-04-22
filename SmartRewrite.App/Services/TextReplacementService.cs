using SmartRewrite.App.Native;
using SmartRewrite.App.Models;
using WpfClipboard = System.Windows.Clipboard;

namespace SmartRewrite.App.Services;

public sealed class TextReplacementService
{
    public async Task ReplaceSelectedTextAsync(SelectionCaptureResult capture, string newText, CancellationToken cancellationToken)
    {
        if (TryReplaceInEditableControl(capture, newText))
        {
            return;
        }

        var originalClipboardText = TryGetClipboardText();

        try
        {
            TrySetClipboardText(newText);
            await Task.Delay(50, cancellationToken);
            NativeMethods.SetForegroundWindow(capture.TargetWindowHandle);
            await Task.Delay(90, cancellationToken);
            NativeMethods.SendCtrlPlusKey('V');
        }
        finally
        {
            await Task.Delay(100, CancellationToken.None);

            if (originalClipboardText is not null)
            {
                TrySetClipboardText(originalClipboardText);
            }
        }
    }

    private static bool TryReplaceInEditableControl(SelectionCaptureResult capture, string newText)
    {
        if (capture.FocusedControlHandle == IntPtr.Zero ||
            capture.SelectionStart is null ||
            capture.SelectionEnd is null)
        {
            return false;
        }

        if (!NativeMethods.IsSupportedEditableControl(capture.FocusedControlHandle))
        {
            return false;
        }

        NativeMethods.SetForegroundWindow(capture.TargetWindowHandle);
        Thread.Sleep(60);

        return NativeMethods.TryReplaceEditSelection(
            capture.FocusedControlHandle,
            capture.SelectionStart.Value,
            capture.SelectionEnd.Value,
            newText);
    }

    private static string? TryGetClipboardText()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return WpfClipboard.ContainsText() ? WpfClipboard.GetText() : null;
            }
            catch
            {
                Thread.Sleep(40);
            }
        }

        return null;
    }

    private static void TrySetClipboardText(string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                WpfClipboard.SetText(text);
                return;
            }
            catch
            {
                Thread.Sleep(40);
            }
        }
    }
}
