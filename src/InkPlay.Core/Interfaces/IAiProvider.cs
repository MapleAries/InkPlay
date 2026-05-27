using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IAiProvider
{
    string ProviderName { get; }
    string ProviderId { get; }

    IAsyncEnumerable<string> StreamCompletionAsync(
        AiProviderConfig config,
        IReadOnlyList<AiChatMessage> messages,
        CancellationToken cancellationToken = default);

    Task<string> GetCompletionAsync(
        AiProviderConfig config,
        IReadOnlyList<AiChatMessage> messages,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateConfigurationAsync(AiProviderConfig config);
}
