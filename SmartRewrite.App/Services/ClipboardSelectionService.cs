using SmartRewrite.App.Native;
using SmartRewrite.App.Models;
using WpfClipboard = System.Windows.Clipboard;

namespace SmartRewrite.App.Services;

public sealed class ClipboardSelectionService
{
    public async Task<SelectionCaptureResult?> TryCaptureSelectionAsync(int captureDelayMs, CancellationToken cancellationToken)
    {
        var targetWindowHandle = NativeMethods.GetForegroundWindow();
        if (targetWindowHandle == IntPtr.Zero)
        {
            return null;
        }

        var originalClipboardText = TryGetClipboardText();
        var editableSelection = NativeMethods.TryCaptureEditableSelection();

        try
        {
            NativeMethods.SendCtrlPlusKey('C');
            await Task.Delay(captureDelayMs, cancellationToken);

            if (!ClipboardContainsText())
            {
                return null;
            }

            var text = TryGetClipboardText();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return new SelectionCaptureResult
            {
                Text = text.Trim(),
                TargetWindowHandle = targetWindowHandle,
                FocusedControlHandle = editableSelection.ControlHandle,
                SelectionStart = editableSelection.SelectionStart,
                SelectionEnd = editableSelection.SelectionEnd
            };
        }
        finally
        {
            await Task.Delay(50, CancellationToken.None);

            if (originalClipboardText is not null)
            {
                TrySetClipboardText(originalClipboardText);
            }
        }
    }

    private static bool ClipboardContainsText()
    {
        return ExecuteClipboardOperation(() => WpfClipboard.ContainsText()) == true;
    }

    private static string? TryGetClipboardText()
    {
        return ExecuteClipboardOperation(() => WpfClipboard.ContainsText() ? WpfClipboard.GetText() : null);
    }

    private static void TrySetClipboardText(string text)
    {
        ExecuteClipboardOperation(() =>
        {
            WpfClipboard.SetText(text);
            return true;
        });
    }

    private static T? ExecuteClipboardOperation<T>(Func<T> operation)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return operation();
            }
            catch
            {
                Thread.Sleep(40);
            }
        }

        return default;
    }
}
