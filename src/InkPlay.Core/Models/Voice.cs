using LiteDB;

namespace InkPlay.Core.Models;

public class Voice
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string AgeRange { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string Speed { get; set; } = string.Empty;
    public string Pitch { get; set; } = string.Empty;
    public string SampleText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
