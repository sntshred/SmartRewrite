namespace SmartRewrite.App.Models;

public sealed class SelectionCaptureResult
{
    public string Text { get; init; } = string.Empty;

    public IntPtr TargetWindowHandle { get; init; }

    public IntPtr FocusedControlHandle { get; init; }

    public int? SelectionStart { get; init; }

    public int? SelectionEnd { get; init; }

    public bool UsedDirectSelection { get; init; }
}
