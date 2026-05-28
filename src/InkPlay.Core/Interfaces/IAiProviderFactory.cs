using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string providerId);
    IAiProvider GetProviderForApiKey(ApiKeyConfig apiKeyConfig);
    IReadOnlyList<string> GetAvailableProviders();
}
