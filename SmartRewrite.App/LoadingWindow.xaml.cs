using System.Windows;

namespace SmartRewrite.App;

public partial class LoadingWindow : Window
{
    public LoadingWindow(int screenX, int screenY)
    {
        InitializeComponent();
        Left = Math.Max(12, screenX - 150);
        Top = Math.Max(12, screenY + 18);
    }
}
