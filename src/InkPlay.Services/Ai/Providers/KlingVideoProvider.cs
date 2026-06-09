using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Ai.Providers;

public class KlingVideoProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderName => "可灵 AI";
    public string ProviderId => "kling";

    public KlingVideoProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VideoGenerationResult> GenerateVideoAsync(
        ApiKeyConfig config,
        VideoGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = config.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/v1/videos/generations";

        var body = new
        {
            prompt = request.Prompt,
            negative_prompt = request.NegativePrompt,
            image_url = string.IsNullOrEmpty(request.ImageUrl) ? null : request.ImageUrl,
            duration = request.Duration,
            resolution = request.Resolution
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        httpRequest.Content = JsonContent.Create(body);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        var root = result?.RootElement;

        return new VideoGenerationResult
        {
            TaskId = root?.TryGetProperty("task_id", out var taskIdProp) == true ? taskIdProp.GetString() ?? "" : "",
            Status = root?.TryGetProperty("status", out var statusProp) == true ? statusProp.GetString() ?? "pending" : "pending"
        };
    }

    public async Task<VideoGenerationResult> CheckStatusAsync(
        ApiKeyConfig config,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = config.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/v1/videos/generations/{taskId}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        var root = result?.RootElement;

        var status = root?.TryGetProperty("status", out var statusProp) == true ? statusProp.GetString() ?? "unknown" : "unknown";
        var videoUrl = string.Empty;
        var errorMessage = string.Empty;

        if (status == "completed")
        {
            videoUrl = root?.TryGetProperty("video_url", out var urlProp) == true ? urlProp.GetString() ?? "" : "";
        }
        else if (status == "failed")
        {
            errorMessage = root?.TryGetProperty("error", out var errProp) == true ? errProp.GetString() ?? "生成失败" : "生成失败";
        }

        return new VideoGenerationResult
        {
            TaskId = taskId,
            Status = status,
            VideoUrl = videoUrl,
            ErrorMessage = errorMessage
        };
    }
}
