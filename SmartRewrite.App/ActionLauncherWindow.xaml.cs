using System.Windows;

namespace SmartRewrite.App;

public partial class ActionLauncherWindow : Window
{
    public ActionLauncherWindow(int screenX, int screenY)
    {
        InitializeComponent();
        Left = Math.Max(12, screenX - 130);
        Top = Math.Max(12, screenY + 18);
    }

    public event EventHandler? LaunchRequested;
    public event EventHandler? WindowClosed;

    protected override void OnClosed(EventArgs e)
    {
        WindowClosed?.Invoke(this, EventArgs.Empty);
        base.OnClosed(e);
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchRequested?.Invoke(this, EventArgs.Empty);
        Close();
    }
}
