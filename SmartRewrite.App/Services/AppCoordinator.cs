using System.Windows;
using SmartRewrite.App.Models;
using WpfApplication = System.Windows.Application;

namespace SmartRewrite.App.Services;

public sealed class AppCoordinator : IDisposable
{
    private static readonly TimeSpan SelectionDebounceWindow = TimeSpan.FromMilliseconds(900);
    private readonly ClipboardSelectionService _clipboardSelectionService;
    private readonly OpenAiRewriteService _rewriteService;
    private readonly TextReplacementService _replacementService;
    private readonly SuggestionPopupService _popupService;
    private readonly GlobalSelectionMonitor _selectionMonitor;
    private readonly AppConfigService _configService;
    private readonly SemaphoreSlim _processingGate = new(1, 1);
    private CancellationTokenSource? _selectionCts;
    private string? _lastHandledText;
    private DateTimeOffset _lastHandledAt;
    private DateTimeOffset _lastTriggerAt;

    public AppCoordinator(
        ClipboardSelectionService clipboardSelectionService,
        OpenAiRewriteService rewriteService,
        TextReplacementService replacementService,
        SuggestionPopupService popupService,
        GlobalSelectionMonitor selectionMonitor,
        AppConfigService configService)
    {
        _clipboardSelectionService = clipboardSelectionService;
        _rewriteService = rewriteService;
        _replacementService = replacementService;
        _popupService = popupService;
        _selectionMonitor = selectionMonitor;
        _configService = configService;
    }

    public void Start()
    {
        _configService.Load();
        _selectionMonitor.ActionRequested += OnActionRequested;
        _selectionMonitor.Start();
    }

    public void ReloadConfig() => _configService.Reload();

    public void Dispose()
    {
        _selectionMonitor.ActionRequested -= OnActionRequested;
        _selectionMonitor.Dispose();
        _selectionCts?.Cancel();
        _selectionCts?.Dispose();
        _processingGate.Dispose();
    }

    private void OnActionRequested(object? sender, SelectionTriggerEventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastTriggerAt < SelectionDebounceWindow)
        {
            return;
        }

        _lastTriggerAt = now;
        _selectionCts?.Cancel();
        _selectionCts?.Dispose();
        _selectionCts = new CancellationTokenSource();
        _ = ProcessActionAsync(e, _selectionCts.Token);
    }

    private async Task ProcessActionAsync(SelectionTriggerEventArgs trigger, CancellationToken cancellationToken)
    {
        if (!await _processingGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var config = _configService.Current;
            var capture = await _clipboardSelectionService.TryCaptureSelectionAsync(
                config.Selection.CaptureDelayMs,
                cancellationToken);

            if (capture is null || string.IsNullOrWhiteSpace(capture.Text))
            {
                return;
            }

            if (capture.Text.Length < config.Selection.MinimumTextLength ||
                capture.Text.Length > config.Selection.MaximumTextLength)
            {
                return;
            }

            if (capture.Text == _lastHandledText &&
                DateTimeOffset.UtcNow - _lastHandledAt < TimeSpan.FromSeconds(5))
            {
                _lastHandledAt = DateTimeOffset.UtcNow;
            }

            _lastHandledText = capture.Text;
            _lastHandledAt = DateTimeOffset.UtcNow;

            var launchRequested = await _popupService.ShowLauncherAsync(
                capture.Text,
                trigger.ScreenX,
                trigger.ScreenY,
                cancellationToken);

            if (!launchRequested)
            {
                return;
            }

            _popupService.ShowLoading(trigger.ScreenX, trigger.ScreenY);
            var suggestions = await _rewriteService.GetSuggestionsAsync(capture.Text, cancellationToken);
            _popupService.CloseLoading();

            if (suggestions.Count == 0)
            {
                return;
            }

            var chosen = await _popupService.ShowAsync(
                suggestions,
                capture.Text,
                trigger.ScreenX,
                trigger.ScreenY,
                cancellationToken);

            if (chosen is null)
            {
                return;
            }

            await _replacementService.ReplaceSelectedTextAsync(
                capture,
                chosen.Text,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _popupService.CloseLoading();
        }
        catch (Exception ex)
        {
            _popupService.CloseLoading();
            WpfApplication.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(
                    ex.Message,
                    "SmartRewrite",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
        }
        finally
        {
            _processingGate.Release();
        }
    }
}
