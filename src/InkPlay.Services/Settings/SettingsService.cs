using System.Text.Json;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly object _lock = new();
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
        lock (_lock)
        {
            return _apiKeys.Where(k => k.Category == category).ToList().AsReadOnly();
        }
    }

    public ApiKeyConfig? GetDefaultApiKey(ApiKeyCategory category)
    {
        lock (_lock)
        {
            return _apiKeys.FirstOrDefault(k => k.Category == category && k.IsDefault)
                ?? _apiKeys.FirstOrDefault(k => k.Category == category);
        }
    }

    public void SaveApiKey(ApiKeyConfig config)
    {
        lock (_lock)
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

            SaveSettingsInternal();
        }
    }

    public void DeleteApiKey(Guid id)
    {
        lock (_lock)
        {
            _apiKeys.RemoveAll(k => k.Id == id);
            SaveSettingsInternal();
        }
    }

    public void SetDefaultApiKey(Guid id, ApiKeyCategory category)
    {
        lock (_lock)
        {
            foreach (var key in _apiKeys.Where(k => k.Category == category))
            {
                key.IsDefault = key.Id == id;
            }
            SaveSettingsInternal();
        }
    }

    // --- Legacy AI Provider Config ---

    public AiProviderConfig GetAiProviderConfig(string providerId)
    {
        lock (_lock)
        {
            if (_providerConfigs.TryGetValue(providerId, out var config))
                return config;

            return new AiProviderConfig
            {
                ProviderId = providerId,
                BaseUrl = GetDefaultBaseUrl(providerId)
            };
        }
    }

    public void SaveAiProviderConfig(AiProviderConfig config)
    {
        lock (_lock)
        {
            _providerConfigs[config.ProviderId] = config;
            SaveSettingsInternal();
        }
    }

    public string GetDefaultAiProviderId()
    {
        lock (_lock)
        {
            return _defaultProviderId;
        }
    }

    public void SetDefaultAiProviderId(string providerId)
    {
        lock (_lock)
        {
            _defaultProviderId = providerId;
            SaveSettingsInternal();
        }
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

    private void SaveSettingsInternal()
    {
        var data = new SettingsData
        {
            ApiKeys = _apiKeys,
            ProviderConfigs = _providerConfigs,
            DefaultProviderId = _defaultProviderId
        };
        var json = JsonSerializer.Serialize(data, JsonOptions);

        // Atomic write: write to temp file, then rename
        var tempPath = _settingsPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _settingsPath, overwrite: true);
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
