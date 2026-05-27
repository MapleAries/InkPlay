using InkPlay.Core.Enums;
using LiteDB;

namespace InkPlay.Core.Models;

public class CharacterRelationship
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid FromCharacterId { get; set; }
    public Guid ToCharacterId { get; set; }
    public CharacterRelationType Type { get; set; }
    public string Description { get; set; } = string.Empty;
}
