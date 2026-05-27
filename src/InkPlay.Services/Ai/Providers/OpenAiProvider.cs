using System.Runtime.CompilerServices;
using System.Text.Json;
using InkPlay.Core.Models;

namespace InkPlay.Services.Ai.Providers;

public class OpenAiProvider : AiProviderBase
{
    public override string ProviderName => "OpenAI GPT";
    public override string ProviderId => "openai";

    public OpenAiProvider(HttpClient httpClient) : base(httpClient) { }

    public override async IAsyncEnumerable<string> StreamCompletionAsync(
        AiProviderConfig config,
        IReadOnlyList<AiChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = config.ModelId,
            temperature = config.Temperature,
            max_tokens = config.MaxTokens,
            stream = true,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList()
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/v1/chat/completions")
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

        var response = await HttpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await foreach (var chunk in ReadSseStreamAsync(response, ExtractOpenAiContent, cancellationToken))
        {
            yield return chunk;
        }
    }

    private static string? ExtractOpenAiContent(string jsonData)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonData);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("delta")
                .GetProperty("content")
                .GetString();
        }
        catch { }
        return null;
    }

    public override Task<bool> ValidateConfigurationAsync(AiProviderConfig config)
    {
        // OpenAI validation can be done by listing models
        return Task.FromResult(!string.IsNullOrEmpty(config.ApiKey));
    }
}
