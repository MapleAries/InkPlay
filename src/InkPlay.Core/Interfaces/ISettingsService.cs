using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface ISettingsService
{
    // New: fully customizable API key management
    IReadOnlyList<ApiKeyConfig> GetApiKeys(ApiKeyCategory category);
    ApiKeyConfig? GetDefaultApiKey(ApiKeyCategory category);
    void SaveApiKey(ApiKeyConfig config);
    void DeleteApiKey(Guid id);
    void SetDefaultApiKey(Guid id, ApiKeyCategory category);

    // Legacy: keep for backward compatibility with AI providers
    AiProviderConfig GetAiProviderConfig(string providerId);
    void SaveAiProviderConfig(AiProviderConfig config);
    string GetDefaultAiProviderId();
    void SetDefaultAiProviderId(string providerId);
}
