using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IGlossaryRepository
{
    Task<GlossaryEntry?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<GlossaryEntry>> GetByProjectIdAsync(Guid projectId);
    Task<IReadOnlyList<GlossaryEntry>> GetByCategoryAsync(Guid projectId, string category);
    Task<GlossaryEntry> CreateAsync(GlossaryEntry entry);
    Task UpdateAsync(GlossaryEntry entry);
    Task DeleteAsync(Guid id);
}
