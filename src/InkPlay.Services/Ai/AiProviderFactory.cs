using InkPlay.Core.Interfaces;
using InkPlay.Services.Ai.Providers;

namespace InkPlay.Services.Ai;

public class AiProviderFactory : IAiProviderFactory
{
    private readonly Dictionary<string, Func<IAiProvider>> _providerFactories;

    public AiProviderFactory(IServiceProvider serviceProvider)
    {
        _providerFactories = new Dictionary<string, Func<IAiProvider>>(StringComparer.OrdinalIgnoreCase)
        {
            ["claude"] = () => serviceProvider.GetRequiredService<ClaudeProvider>(),
            ["openai"] = () => serviceProvider.GetRequiredService<OpenAiProvider>(),
            ["qwen"] = () => serviceProvider.GetRequiredService<QwenProvider>(),
        };
    }

    public IAiProvider GetProvider(string providerId)
    {
        if (_providerFactories.TryGetValue(providerId, out var factory))
            return factory();
        throw new ArgumentException($"Unknown AI provider: {providerId}");
    }

    public IReadOnlyList<string> GetAvailableProviders()
        => _providerFactories.Keys.ToList().AsReadOnly();
}
