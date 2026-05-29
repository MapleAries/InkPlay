using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;

namespace InkPlay.Services.Data.Repositories;

public class VoiceRepository : IVoiceRepository
{
    private readonly InkPlayDbContext _db;

    public VoiceRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<Voice?> GetByIdAsync(Guid id)
    {
        var voice = _db.Voices.FindById(id);
        return Task.FromResult<Voice?>(voice);
    }

    public Task<IReadOnlyList<Voice>> GetByProjectIdAsync(Guid projectId)
    {
        var voices = _db.Voices
            .Find(v => v.ProjectId == projectId)
            .OrderBy(v => v.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Voice>>(voices);
    }

    public Task<Voice> CreateAsync(Voice voice)
    {
        voice.CreatedAt = DateTime.UtcNow;
        voice.UpdatedAt = DateTime.UtcNow;
        _db.Voices.Insert(voice);
        return Task.FromResult(voice);
    }

    public Task UpdateAsync(Voice voice)
    {
        voice.UpdatedAt = DateTime.UtcNow;
        _db.Voices.Update(voice);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _db.Voices.Delete(id);
        return Task.CompletedTask;
    }
}
