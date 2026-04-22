using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartRewrite.App.Models;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace SmartRewrite.App;

public partial class SuggestionWindow : Window
{
    private readonly IReadOnlyList<RewriteSuggestion> _suggestions;
    private RewriteSuggestion? _selectedSuggestion;

    public SuggestionWindow(string originalText, IReadOnlyList<RewriteSuggestion> suggestions, int screenX, int screenY)
    {
        InitializeComponent();
        _suggestions = suggestions;

        OriginalTextBlock.Text = originalText;
        Left = Math.Max(12, screenX - 220);
        Top = Math.Max(12, screenY + 18);

        BindOption(OptionOneButton, suggestions.ElementAtOrDefault(0), 0);
        BindOption(OptionTwoButton, suggestions.ElementAtOrDefault(1), 1);
        BindOption(OptionThreeButton, suggestions.ElementAtOrDefault(2), 2);
    }

    public event EventHandler<RewriteSuggestion>? OptionConfirmed;

    public event EventHandler? WindowClosed;

    protected override void OnClosed(EventArgs e)
    {
        WindowClosed?.Invoke(this, EventArgs.Empty);
        base.OnClosed(e);
    }

    private void BindOption(WpfRadioButton button, RewriteSuggestion? suggestion, int index)
    {
        if (suggestion is null)
        {
            button.Visibility = Visibility.Collapsed;
            return;
        }

        button.Tag = index;
        button.Content = new WpfStackPanel
        {
            Children =
            {
                new WpfTextBlock
                {
                    Text = suggestion.Title,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = WpfBrushes.White,
                    Margin = new Thickness(0, 0, 0, 6)
                },
                new WpfTextBlock
                {
                    Text = suggestion.Text,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(216, 224, 234)),
                    MaxHeight = 88
                }
            }
        };
    }

    private void OptionButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfRadioButton button || button.Tag is not int index)
        {
            return;
        }

        _selectedSuggestion = _suggestions.ElementAtOrDefault(index);
        ConfirmButton.IsEnabled = _selectedSuggestion is not null;
        SelectionHintText.Text = _selectedSuggestion is null
            ? "Select one variation, then confirm."
            : "Confirm to replace the selected text.";
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSuggestion is null)
        {
            return;
        }

        OptionConfirmed?.Invoke(this, _selectedSuggestion);
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
