using System.Windows;
using System.Windows.Threading;
using SmartRewrite.App.Models;
using WpfApplication = System.Windows.Application;

namespace SmartRewrite.App.Services;

public sealed class SuggestionPopupService
{
    private ActionLauncherWindow? _actionLauncherWindow;
    private SuggestionWindow? _currentWindow;
    private LoadingWindow? _loadingWindow;

    public void CloseCurrent()
    {
        var dispatcher = WpfApplication.Current.Dispatcher;
        dispatcher.Invoke(() =>
        {
            _actionLauncherWindow?.Close();
            _actionLauncherWindow = null;
            _loadingWindow?.Close();
            _loadingWindow = null;
            _currentWindow?.Close();
            _currentWindow = null;
        });
    }

    public Task<bool> ShowLauncherAsync(
        string selectedText,
        int screenX,
        int screenY,
        CancellationToken cancellationToken)
    {
        var dispatcher = WpfApplication.Current.Dispatcher;
        var tcs = new TaskCompletionSource<bool>();

        dispatcher.Invoke(() =>
        {
            _actionLauncherWindow?.Close();
            _actionLauncherWindow = new ActionLauncherWindow(screenX, screenY);
            _actionLauncherWindow.LaunchRequested += (_, _) => tcs.TrySetResult(true);
            _actionLauncherWindow.WindowClosed += (_, _) =>
            {
                _actionLauncherWindow = null;
                tcs.TrySetResult(false);
            };
            _actionLauncherWindow.Show();
        });

        cancellationToken.Register(() =>
        {
            dispatcher.Invoke(() =>
            {
                _actionLauncherWindow?.Close();
                _actionLauncherWindow = null;
            });
            tcs.TrySetCanceled(cancellationToken);
        });

        return tcs.Task;
    }

    public void ShowLoading(int screenX, int screenY)
    {
        var dispatcher = WpfApplication.Current.Dispatcher;
        dispatcher.Invoke(() =>
        {
            _loadingWindow?.Close();
            _loadingWindow = new LoadingWindow(screenX, screenY);
            _loadingWindow.Show();
        });
    }

    public void CloseLoading()
    {
        var dispatcher = WpfApplication.Current.Dispatcher;
        dispatcher.Invoke(() =>
        {
            _loadingWindow?.Close();
            _loadingWindow = null;
        });
    }

    public Task<RewriteSuggestion?> ShowAsync(
        IReadOnlyList<RewriteSuggestion> suggestions,
        string originalText,
        int screenX,
        int screenY,
        CancellationToken cancellationToken)
    {
        var dispatcher = WpfApplication.Current.Dispatcher;
        var tcs = new TaskCompletionSource<RewriteSuggestion?>();

        dispatcher.Invoke(() =>
        {
            _actionLauncherWindow?.Close();
            _actionLauncherWindow = null;
            _loadingWindow?.Close();
            _loadingWindow = null;
            _currentWindow?.Close();
            _currentWindow = new SuggestionWindow(originalText, suggestions, screenX, screenY);
            _currentWindow.OptionConfirmed += (_, suggestion) => tcs.TrySetResult(suggestion);
            _currentWindow.WindowClosed += (_, _) =>
            {
                _currentWindow = null;
                tcs.TrySetResult(null);
            };
            _currentWindow.Show();
        });

        cancellationToken.Register(() =>
        {
            dispatcher.Invoke(() =>
            {
                _currentWindow?.Close();
                _currentWindow = null;
            });
            tcs.TrySetCanceled(cancellationToken);
        });

        return tcs.Task;
    }
}
