using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Data.Repositories;

public class WorldSettingRepository : IWorldSettingRepository
{
    private readonly InkPlayDbContext _db;

    public WorldSettingRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<WorldSetting?> GetByIdAsync(Guid id)
    {
        var setting = _db.WorldSettings.FindById(id);
        return Task.FromResult(setting);
    }

    public Task<IReadOnlyList<WorldSetting>> GetByProjectIdAsync(Guid projectId)
    {
        var settings = _db.WorldSettings
            .Find(w => w.ProjectId == projectId)
            .OrderBy(w => w.Category)
            .ThenBy(w => w.Title)
            .ToList();
        return Task.FromResult<IReadOnlyList<WorldSetting>>(settings);
    }

    public Task<WorldSetting> CreateAsync(WorldSetting setting)
    {
        setting.CreatedAt = DateTime.UtcNow;
        _db.WorldSettings.Insert(setting);
        return Task.FromResult(setting);
    }

    public Task UpdateAsync(WorldSetting setting)
    {
        _db.WorldSettings.Update(setting);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _db.WorldSettings.Delete(id);
        return Task.CompletedTask;
    }
}
