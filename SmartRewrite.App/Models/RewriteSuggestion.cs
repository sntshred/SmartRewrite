namespace SmartRewrite.App.Models;

public sealed class RewriteSuggestion
{
    public string Title { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}

public sealed class RewriteSuggestionsResponse
{
    public List<RewriteSuggestion> Suggestions { get; set; } = [];
}
