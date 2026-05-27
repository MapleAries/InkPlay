using LiteDB;

namespace InkPlay.Core.Models;

public class Character
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Appearance { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public string Motivation { get; set; } = string.Empty;
    public string Weakness { get; set; } = string.Empty;
    public string Backstory { get; set; } = string.Empty;
    public string AvatarImagePath { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
