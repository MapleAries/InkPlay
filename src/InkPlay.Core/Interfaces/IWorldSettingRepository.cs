using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IWorldSettingRepository
{
    Task<WorldSetting?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<WorldSetting>> GetByProjectIdAsync(Guid projectId);
    Task<WorldSetting> CreateAsync(WorldSetting setting);
    Task UpdateAsync(WorldSetting setting);
    Task DeleteAsync(Guid id);
}
