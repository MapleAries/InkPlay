using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;

namespace InkPlay.Services.Data.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly InkPlayDbContext _db;

    public ConversationRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<AiConversation>> GetByProjectIdAsync(Guid projectId)
    {
        var conversations = _db.Conversations
            .Find(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<AiConversation>>(conversations);
    }

    public Task<AiConversation?> GetByIdAsync(Guid id)
    {
        var conversation = _db.Conversations.FindById(id);
        return Task.FromResult<AiConversation?>(conversation);
    }

    public Task<AiConversation> CreateAsync(AiConversation conversation)
    {
        conversation.CreatedAt = DateTime.UtcNow;
        _db.Conversations.Insert(conversation);
        return Task.FromResult(conversation);
    }

    public Task UpdateAsync(AiConversation conversation)
    {
        _db.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _db.Conversations.Delete(id);
        return Task.CompletedTask;
    }
}
