using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;

namespace InkPlay.Services.Data.Repositories;

public class CharacterRelationshipRepository : ICharacterRelationshipRepository
{
    private readonly InkPlayDbContext _db;

    public CharacterRelationshipRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<CharacterRelationship>> GetByProjectIdAsync(Guid projectId)
    {
        var relationships = _db.Relationships
            .Find(r => r.ProjectId == projectId)
            .ToList();
        return Task.FromResult<IReadOnlyList<CharacterRelationship>>(relationships);
    }

    public Task<IReadOnlyList<CharacterRelationship>> GetByCharacterIdAsync(Guid characterId)
    {
        var relationships = _db.Relationships
            .Find(r => r.FromCharacterId == characterId || r.ToCharacterId == characterId)
            .ToList();
        return Task.FromResult<IReadOnlyList<CharacterRelationship>>(relationships);
    }

    public Task<CharacterRelationship> CreateAsync(CharacterRelationship relationship)
    {
        _db.Relationships.Insert(relationship);
        return Task.FromResult(relationship);
    }

    public Task UpdateAsync(CharacterRelationship relationship)
    {
        _db.Relationships.Update(relationship);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _db.Relationships.Delete(id);
        return Task.CompletedTask;
    }
}
