using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Ai.Providers;
using Microsoft.Extensions.DependencyInjection;

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

    public IAiProvider GetProviderForApiKey(ApiKeyConfig apiKeyConfig)
    {
        var baseUrl = apiKeyConfig.BaseUrl?.ToLowerInvariant() ?? "";
        var providerId = baseUrl switch
        {
            var u when u.Contains("anthropic") => "claude",
            var u when u.Contains("openai") => "openai",
            var u when u.Contains("dashscope") => "qwen",
            _ => "openai" // 默认使用 OpenAI 兼容格式
        };
        return GetProvider(providerId);
    }

    public IReadOnlyList<string> GetAvailableProviders()
        => _providerFactories.Keys.ToList().AsReadOnly();
}
