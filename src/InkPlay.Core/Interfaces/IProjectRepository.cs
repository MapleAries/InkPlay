using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Project>> GetAllAsync();
    Task<Project> CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Guid id);
}
