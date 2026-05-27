using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface ISettingsService
{
    AiProviderConfig GetAiProviderConfig(string providerId);
    void SaveAiProviderConfig(AiProviderConfig config);
    string GetDefaultAiProviderId();
    void SetDefaultAiProviderId(string providerId);
}
