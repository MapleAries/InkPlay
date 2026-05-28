using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface ICharacterRelationshipRepository
{
    Task<IReadOnlyList<CharacterRelationship>> GetByProjectIdAsync(Guid projectId);
    Task<IReadOnlyList<CharacterRelationship>> GetByCharacterIdAsync(Guid characterId);
    Task<CharacterRelationship> CreateAsync(CharacterRelationship relationship);
    Task UpdateAsync(CharacterRelationship relationship);
    Task DeleteAsync(Guid id);
}
