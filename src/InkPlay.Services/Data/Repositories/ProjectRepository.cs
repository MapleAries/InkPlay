using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;

namespace InkPlay.Services.Data.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly InkPlayDbContext _db;

    public ProjectRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<Project?> GetByIdAsync(Guid id)
    {
        var project = _db.Projects.FindById(id);
        return Task.FromResult(project);
    }

    public Task<IReadOnlyList<Project>> GetAllAsync()
    {
        var projects = _db.Projects
            .FindAll()
            .OrderByDescending(p => p.UpdatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<Project>>(projects);
    }

    public Task<Project> CreateAsync(Project project)
    {
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        _db.Projects.Insert(project);
        return Task.FromResult(project);
    }

    public Task UpdateAsync(Project project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        _db.Projects.Update(project);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _db.Projects.Delete(id);
        return Task.CompletedTask;
    }
}
