using InkPlay.Core.Enums;
using LiteDB;

namespace InkPlay.Core.Models;

public class Document
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DocumentType Type { get; set; } = DocumentType.Chapter;
    public int SortOrder { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? EpisodeNumber { get; set; }
    public List<ScriptScene> Scenes { get; set; } = new();
}
