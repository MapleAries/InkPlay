using LiteDB;

namespace InkPlay.Core.Models;

public class DocumentVersion
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid ProjectId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string ChangeSource { get; set; } = "ManualEdit";
    public string ChangeSummary { get; set; } = string.Empty;
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;
}
