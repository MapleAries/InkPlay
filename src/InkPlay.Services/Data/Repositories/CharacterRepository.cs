using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;
using Microsoft.Extensions.Logging;

namespace InkPlay.Services.Data.Repositories;

public class CharacterRepository : ICharacterRepository
{
    private readonly InkPlayDbContext _db;
    private readonly IProjectRepository _projectRepository;
    private readonly IFileProjectService _fileProjectService;
    private readonly ILogger<CharacterRepository> _logger;

    public CharacterRepository(InkPlayDbContext db, IProjectRepository projectRepository, IFileProjectService fileProjectService, ILogger<CharacterRepository> logger)
    {
        _db = db;
        _projectRepository = projectRepository;
        _fileProjectService = fileProjectService;
        _logger = logger;
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

    public async Task<Character> CreateAsync(Character character)
    {
        character.CreatedAt = DateTime.UtcNow;
        character.UpdatedAt = DateTime.UtcNow;
        _db.Characters.Insert(character);

        await SaveToFileSystemAsync(character);

        return character;
    }

    public async Task UpdateAsync(Character character)
    {
        character.UpdatedAt = DateTime.UtcNow;
        _db.Characters.Update(character);

        await SaveToFileSystemAsync(character);
    }

    public Task DeleteAsync(Guid id)
    {
        _db.Characters.Delete(id);
        return Task.CompletedTask;
    }

    private async Task SaveToFileSystemAsync(Character character)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(character.ProjectId);
            if (project is not null && !string.IsNullOrEmpty(project.ProjectPath))
            {
                await _fileProjectService.SaveCharacterAsync(character);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save character '{Name}' to file system, database update still applied", character.Name);
        }
    }
}
