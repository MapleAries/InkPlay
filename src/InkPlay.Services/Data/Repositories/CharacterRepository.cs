using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;

namespace InkPlay.Services.Data.Repositories;

public class CharacterRepository : ICharacterRepository
{
    private readonly InkPlayDbContext _db;

    public CharacterRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<Character?> GetByIdAsync(Guid id)
    {
        var character = _db.Characters.FindById(id);
        return Task.FromResult<Character?>(character);
    }

    public Task<IReadOnlyList<Character>> GetByProjectIdAsync(Guid projectId)
    {
        var characters = _db.Characters
            .Find(c => c.ProjectId == projectId)
            .OrderBy(c => c.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Character>>(characters);
    }

    public Task<Character> CreateAsync(Character character)
    {
        character.CreatedAt = DateTime.UtcNow;
        character.UpdatedAt = DateTime.UtcNow;
        _db.Characters.Insert(character);
        return Task.FromResult(character);
    }

    public Task UpdateAsync(Character character)
    {
        character.UpdatedAt = DateTime.UtcNow;
        _db.Characters.Update(character);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _db.Characters.Delete(id);
        return Task.CompletedTask;
    }
}
