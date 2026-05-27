using System.Text.Json;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private List<ApiKeyConfig> _apiKeys;
    private Dictionary<string, AiProviderConfig> _providerConfigs;
    private string _defaultProviderId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InkPlay");
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");

        _apiKeys = new List<ApiKeyConfig>();
        _providerConfigs = new Dictionary<string, AiProviderConfig>(StringComparer.OrdinalIgnoreCase);
        _defaultProviderId = "claude";

        LoadSettings();
    }

    // --- New API Key Management ---

    public IReadOnlyList<ApiKeyConfig> GetApiKeys(ApiKeyCategory category)
    {
        return _apiKeys.Where(k => k.Category == category).ToList().AsReadOnly();
    }

    public ApiKeyConfig? GetDefaultApiKey(ApiKeyCategory category)
    {
        return _apiKeys.FirstOrDefault(k => k.Category == category && k.IsDefault)
            ?? _apiKeys.FirstOrDefault(k => k.Category == category);
    }

    public void SaveApiKey(ApiKeyConfig config)
    {
        var existing = _apiKeys.FirstOrDefault(k => k.Id == config.Id);
        if (existing is not null)
        {
            _apiKeys.Remove(existing);
        }
        _apiKeys.Add(config);

        // Ensure only one default per category
        if (config.IsDefault)
        {
            foreach (var key in _apiKeys.Where(k => k.Category == config.Category && k.Id != config.Id))
            {
                key.IsDefault = false;
            }
        }

        SaveSettings();
    }

    public void DeleteApiKey(Guid id)
    {
        _apiKeys.RemoveAll(k => k.Id == id);
        SaveSettings();
    }

    public void SetDefaultApiKey(Guid id, ApiKeyCategory category)
    {
        foreach (var key in _apiKeys.Where(k => k.Category == category))
        {
            key.IsDefault = key.Id == id;
        }
        SaveSettings();
    }

    // --- Legacy AI Provider Config ---

    public AiProviderConfig GetAiProviderConfig(string providerId)
    {
        if (_providerConfigs.TryGetValue(providerId, out var config))
            return config;

        return new AiProviderConfig
        {
            ProviderId = providerId,
            BaseUrl = GetDefaultBaseUrl(providerId)
        };
    }

    public void SaveAiProviderConfig(AiProviderConfig config)
    {
        _providerConfigs[config.ProviderId] = config;
        SaveSettings();
    }

    public string GetDefaultAiProviderId() => _defaultProviderId;

    public void SetDefaultAiProviderId(string providerId)
    {
        _defaultProviderId = providerId;
        SaveSettings();
    }

    // --- Persistence ---

    private void LoadSettings()
    {
        if (!File.Exists(_settingsPath)) return;

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data is not null)
            {
                _apiKeys = data.ApiKeys ?? new();
                _providerConfigs = data.ProviderConfigs ?? new();
                _defaultProviderId = data.DefaultProviderId ?? "claude";
            }
        }
        catch
        {
            // Settings corrupted, use defaults
        }
    }

    private void SaveSettings()
    {
        var data = new SettingsData
        {
            ApiKeys = _apiKeys,
            ProviderConfigs = _providerConfigs,
            DefaultProviderId = _defaultProviderId
        };
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static string GetDefaultBaseUrl(string providerId) => providerId.ToLowerInvariant() switch
    {
        "claude" => "https://api.anthropic.com",
        "openai" => "https://api.openai.com",
        "qwen" => "https://dashscope.aliyuncs.com/compatible-mode/v1",
        _ => ""
    };

    private class SettingsData
    {
        public List<ApiKeyConfig>? ApiKeys { get; set; }
        public Dictionary<string, AiProviderConfig>? ProviderConfigs { get; set; }
        public string? DefaultProviderId { get; set; }
    }
}
