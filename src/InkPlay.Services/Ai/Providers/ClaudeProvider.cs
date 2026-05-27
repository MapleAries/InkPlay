using System.Runtime.CompilerServices;
using System.Text.Json;
using InkPlay.Core.Models;

namespace InkPlay.Services.Ai.Providers;

public class ClaudeProvider : AiProviderBase
{
    public override string ProviderName => "Claude (Anthropic)";
    public override string ProviderId => "claude";

    public ClaudeProvider(HttpClient httpClient) : base(httpClient) { }

    public override async IAsyncEnumerable<string> StreamCompletionAsync(
        AiProviderConfig config,
        IReadOnlyList<AiChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        var chatMessages = messages
            .Where(m => m.Role != "system")
            .Select(m => new { role = m.Role, content = m.Content })
            .ToList();

        var requestBody = new
        {
            model = config.ModelId,
            max_tokens = config.MaxTokens,
            temperature = config.Temperature,
            stream = true,
            system = systemMessage?.Content,
            messages = chatMessages
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/v1/messages")
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Add("x-api-key", config.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await HttpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await foreach (var chunk in ReadSseStreamAsync(response, ExtractClaudeContent, cancellationToken))
        {
            yield return chunk;
        }
    }

    private static string? ExtractClaudeContent(string jsonData)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonData);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var type) && type.GetString() == "content_block_delta")
            {
                return root.GetProperty("delta").GetProperty("text").GetString();
            }
        }
        catch { }
        return null;
    }

    public override async Task<bool> ValidateConfigurationAsync(AiProviderConfig config)
    {
        try
        {
            var messages = new List<AiChatMessage> { new() { Role = "user", Content = "Hi" } };
            var testConfig = new AiProviderConfig
            {
                ProviderId = config.ProviderId,
                ApiKey = config.ApiKey,
                BaseUrl = config.BaseUrl,
                ModelId = config.ModelId,
                Temperature = config.Temperature,
                MaxTokens = 10
            };
            var result = await GetCompletionAsync(testConfig, messages);
            return !string.IsNullOrEmpty(result);
        }
        catch
        {
            return false;
        }
    }
}
