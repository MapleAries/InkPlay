using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IVoiceRepository
{
    Task<Voice?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Voice>> GetByProjectIdAsync(Guid projectId);
    Task<Voice> CreateAsync(Voice voice);
    Task UpdateAsync(Voice voice);
    Task DeleteAsync(Guid id);
}
