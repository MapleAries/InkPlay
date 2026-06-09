using System.Text.Json;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
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
        _lock.Wait();
        try
        {
            return _apiKeys.Where(k => k.Category == category).ToList().AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    public ApiKeyConfig? GetDefaultApiKey(ApiKeyCategory category)
    {
        _lock.Wait();
        try
        {
            return _apiKeys.FirstOrDefault(k => k.Category == category && k.IsDefault)
                ?? _apiKeys.FirstOrDefault(k => k.Category == category);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void SaveApiKey(ApiKeyConfig config)
    {
        _lock.Wait();
        try
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
        finally
        {
            _lock.Release();
        }
    }

    public void DeleteApiKey(Guid id)
    {
        _lock.Wait();
        try
        {
            _apiKeys.RemoveAll(k => k.Id == id);
            SaveSettingsInternal();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void SetDefaultApiKey(Guid id, ApiKeyCategory category)
    {
        _lock.Wait();
        try
        {
            foreach (var key in _apiKeys.Where(k => k.Category == category))
            {
                key.IsDefault = key.Id == id;
            }
            SaveSettingsInternal();
        }
        finally
        {
            _lock.Release();
        }
    }

    // --- Legacy AI Provider Config ---

    public AiProviderConfig GetAiProviderConfig(string providerId)
    {
        _lock.Wait();
        try
        {
            if (_providerConfigs.TryGetValue(providerId, out var config))
                return config;

            return new AiProviderConfig
            {
                ProviderId = providerId,
                BaseUrl = GetDefaultBaseUrl(providerId)
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public void SaveAiProviderConfig(AiProviderConfig config)
    {
        _lock.Wait();
        try
        {
            _providerConfigs[config.ProviderId] = config;
            SaveSettingsInternal();
        }
        finally
        {
            _lock.Release();
        }
    }

    public string GetDefaultAiProviderId()
    {
        _lock.Wait();
        try
        {
            return _defaultProviderId;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void SetDefaultAiProviderId(string providerId)
    {
        _lock.Wait();
        try
        {
            _defaultProviderId = providerId;
            SaveSettingsInternal();
        }
        finally
        {
            _lock.Release();
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
