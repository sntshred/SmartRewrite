using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SmartRewrite.App.Models;

namespace SmartRewrite.App.Services;

public sealed class OpenAiRewriteService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    private readonly AppConfigService _configService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenAiRewriteService(AppConfigService configService)
    {
        _configService = configService;
    }

    public async Task<IReadOnlyList<RewriteSuggestion>> GetSuggestionsAsync(string selectedText, CancellationToken cancellationToken)
    {
        var config = _configService.Current;
        var apiKey = _configService.GetApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY is not set. Add it to your Windows user environment variables.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, config.OpenAI.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = config.OpenAI.Model,
            instructions = config.OpenAI.Instructions,
            reasoning = new
            {
                effort = "minimal"
            },
            max_output_tokens = 220,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "rewrite_suggestions",
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            suggestions = new
                            {
                                type = "array",
                                minItems = 3,
                                maxItems = 3,
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        title = new { type = "string" },
                                        text = new { type = "string" }
                                    },
                                    required = new[] { "title", "text" }
                                }
                            }
                        },
                        required = new[] { "suggestions" }
                    }
                }
            },
            input = $"Rewrite the following selected text into 3 short polished alternatives for a professional writing assistant.\n\nSelected text:\n{selectedText}"
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{json}");
        }

        using var document = JsonDocument.Parse(json);
        var outputText = FindOutputText(document.RootElement);

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new InvalidOperationException("OpenAI returned no structured text.");
        }

        var structured = JsonSerializer.Deserialize<RewriteSuggestionsResponse>(outputText, _jsonOptions)
                         ?? new RewriteSuggestionsResponse();

        return structured.Suggestions
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Take(3)
            .ToList();
    }

    private static string? FindOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString();
                }
            }
        }

        return null;
    }
}
