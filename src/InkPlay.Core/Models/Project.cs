using LiteDB;

namespace InkPlay.Core.Models;

public class Project
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CoverImagePath { get; set; } = string.Empty;
    public List<Guid> DocumentIds { get; set; } = new();
    public List<Guid> CharacterIds { get; set; } = new();
    public string PreferredAiProvider { get; set; } = "claude";
    public string PreferredModelId { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
}
