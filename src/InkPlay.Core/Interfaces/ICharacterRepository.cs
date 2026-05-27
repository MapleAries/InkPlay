using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Character>> GetByProjectIdAsync(Guid projectId);
    Task<Character> CreateAsync(Character character);
    Task UpdateAsync(Character character);
    Task DeleteAsync(Guid id);
}
