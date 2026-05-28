using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IVideoProvider
{
    string ProviderName { get; }
    string ProviderId { get; }

    Task<VideoGenerationResult> GenerateVideoAsync(
        ApiKeyConfig config,
        VideoGenerationRequest request,
        CancellationToken cancellationToken = default);

    Task<VideoGenerationResult> CheckStatusAsync(
        ApiKeyConfig config,
        string taskId,
        CancellationToken cancellationToken = default);
}
