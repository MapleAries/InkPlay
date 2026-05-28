using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IConversationRepository
{
    Task<IReadOnlyList<AiConversation>> GetByProjectIdAsync(Guid projectId);
    Task<AiConversation?> GetByIdAsync(Guid id);
    Task<AiConversation> CreateAsync(AiConversation conversation);
    Task UpdateAsync(AiConversation conversation);
    Task DeleteAsync(Guid id);
}
