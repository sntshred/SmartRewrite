using System.IO;
using System.Text.Json;
using SmartRewrite.App.Models;

namespace SmartRewrite.App.Services;

public sealed class AppConfigService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public AppConfig Current { get; private set; } = new();

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            Current = new AppConfig();
            File.WriteAllText(_configPath, JsonSerializer.Serialize(Current, _serializerOptions));
            return Current;
        }

        var json = File.ReadAllText(_configPath);
        Current = JsonSerializer.Deserialize<AppConfig>(json, _serializerOptions) ?? new AppConfig();
        return Current;
    }

    public void Reload() => Load();

    public string? GetApiKey() => Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public string ConfigPath => _configPath;
}
