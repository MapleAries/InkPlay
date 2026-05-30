using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using LiteDB;

namespace InkPlay.Services.Data.Repositories;

public class GlossaryRepository : IGlossaryRepository
{
    private readonly InkPlayDbContext _db;

    public GlossaryRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<GlossaryEntry?> GetByIdAsync(Guid id)
    {
        var entry = _db.GlossaryEntries.FindById(id);
        return Task.FromResult(entry);
    }

    public Task<IReadOnlyList<GlossaryEntry>> GetByProjectIdAsync(Guid projectId)
    {
        var entries = _db.GlossaryEntries
            .Find(e => e.ProjectId == projectId)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Term)
            .ToList();
        return Task.FromResult<IReadOnlyList<GlossaryEntry>>(entries);
    }

    public Task<IReadOnlyList<GlossaryEntry>> GetByCategoryAsync(Guid projectId, string category)
    {
        var entries = _db.GlossaryEntries
            .Find(e => e.ProjectId == projectId && e.Category == category)
            .OrderBy(e => e.Term)
            .ToList();
        return Task.FromResult<IReadOnlyList<GlossaryEntry>>(entries);
    }

    public Task<GlossaryEntry> CreateAsync(GlossaryEntry entry)
    {
        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        _db.GlossaryEntries.Insert(entry);
        return Task.FromResult(entry);
    }

    public Task UpdateAsync(GlossaryEntry entry)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        _db.GlossaryEntries.Update(entry);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _db.GlossaryEntries.Delete(id);
        return Task.CompletedTask;
    }
}
