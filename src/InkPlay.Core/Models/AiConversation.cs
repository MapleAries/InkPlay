using LiteDB;

namespace InkPlay.Core.Models;

public class AiConversation
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid? DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<AiChatMessage> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
