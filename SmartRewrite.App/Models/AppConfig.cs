namespace SmartRewrite.App.Models;

public sealed class AppConfig
{
    public OpenAiConfig OpenAI { get; set; } = new();

    public SelectionConfig Selection { get; set; } = new();
}

public sealed class OpenAiConfig
{
    public string Model { get; set; } = "gpt-5-mini";

    public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";

    public string Instructions { get; set; } =
        "You improve selected text for professional writing. Return exactly 3 concise rewrite options as JSON with the schema {\"suggestions\":[{\"title\":\"...\",\"text\":\"...\"}]}.";
}

public sealed class SelectionConfig
{
    public int CaptureDelayMs { get; set; } = 250;

    public int MinimumTextLength { get; set; } = 3;

    public int MaximumTextLength { get; set; } = 2000;
}
