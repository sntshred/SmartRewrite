using System.IO;
using System.Threading;
using System.Windows;
using SmartRewrite.App.Services;
using Forms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace SmartRewrite.App;

public partial class App : WpfApplication
{
    private Mutex? _singleInstanceMutex;
    private Forms.NotifyIcon? _notifyIcon;
    private AppCoordinator? _coordinator;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _singleInstanceMutex = new Mutex(true, "SmartRewrite.SingleInstance", out var createdNew);
            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    "SmartRewrite is already running in the system tray.",
                    "SmartRewrite",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            Directory.CreateDirectory(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SmartRewrite"));

            var configService = new AppConfigService();
            var clipboardService = new ClipboardSelectionService();
            var rewriteService = new OpenAiRewriteService(configService);
            var replacementService = new TextReplacementService();
            var popupService = new SuggestionPopupService();
            var selectionMonitor = new GlobalSelectionMonitor();

            _coordinator = new AppCoordinator(
                clipboardService,
                rewriteService,
                replacementService,
                popupService,
                selectionMonitor,
                configService);

            _coordinator.Start();
            InitializeTrayIcon();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"SmartRewrite could not start.\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _coordinator?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.OnExit(e);
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Text = "SmartRewrite",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => ShowSettingsWindow();
    }

    private Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Settings", null, (_, _) => ShowSettingsWindow());
        menu.Items.Add("Refresh Config", null, (_, _) => _coordinator?.ReloadConfig());
        menu.Items.Add("Exit", null, (_, _) => Shutdown());
        return menu;
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }
}
