using System.Runtime.CompilerServices;
using System.Text;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Ai;

public abstract class AiProviderBase : IAiProvider
{
    protected readonly HttpClient HttpClient;

    public abstract string ProviderName { get; }
    public abstract string ProviderId { get; }

    protected AiProviderBase(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    public abstract IAsyncEnumerable<string> StreamCompletionAsync(
        AiProviderConfig config,
        IReadOnlyList<AiChatMessage> messages,
        CancellationToken cancellationToken = default);

    public async Task<string> GetCompletionAsync(
        AiProviderConfig config,
        IReadOnlyList<AiChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        await foreach (var chunk in StreamCompletionAsync(config, messages, cancellationToken))
        {
            sb.Append(chunk);
        }
        return sb.ToString();
    }

    public IAsyncEnumerable<string> StreamCompletionAsync(
        ApiKeyConfig apiKeyConfig,
        IReadOnlyList<AiChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var config = ConvertApiKeyConfig(apiKeyConfig);
        return StreamCompletionAsync(config, messages, cancellationToken);
    }

    public Task<string> GetCompletionAsync(
        ApiKeyConfig apiKeyConfig,
        IReadOnlyList<AiChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var config = ConvertApiKeyConfig(apiKeyConfig);
        return GetCompletionAsync(config, messages, cancellationToken);
    }

    public abstract Task<bool> ValidateConfigurationAsync(AiProviderConfig config);

    protected static AiProviderConfig ConvertApiKeyConfig(ApiKeyConfig apiKeyConfig)
    {
        return new AiProviderConfig
        {
            ApiKey = apiKeyConfig.ApiKey,
            BaseUrl = apiKeyConfig.BaseUrl,
            ModelId = apiKeyConfig.ModelId
        };
    }

    protected static async IAsyncEnumerable<string> ReadSseStreamAsync(
        HttpResponseMessage response,
        Func<string, string?> extractContent,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            var content = extractContent(data);
            if (content is not null)
                yield return content;
        }
    }
}
