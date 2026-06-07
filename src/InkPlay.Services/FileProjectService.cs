using System.Text.Json;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services;

public class FileProjectService : IFileProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IProjectRepository _projectRepository;

    public FileProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Project> CreateProjectAsync(string parentDirectory, Project project, string? outlineContent = null)
    {
        var projectDir = Path.Combine(parentDirectory, SanitizeFileName(project.Title));
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "大纲"));
        Directory.CreateDirectory(Path.Combine(projectDir, "章节"));
        Directory.CreateDirectory(Path.Combine(projectDir, "角色"));
        Directory.CreateDirectory(Path.Combine(projectDir, "对话历史"));

        project.ProjectPath = projectDir;
        await SaveProjectMetaAsync(project);

        if (!string.IsNullOrWhiteSpace(outlineContent))
        {
            var outlinePath = Path.Combine(projectDir, "大纲", "故事大纲.md");
            await File.WriteAllTextAsync(outlinePath, outlineContent);
        }

        return project;
    }

    public async Task SaveDocumentAsync(Document document)
    {
        var project = await LoadProjectMetaAsync(document.ProjectId);
        if (project is null) return;

        var subDir = document.Type switch
        {
            DocumentType.Outline => "大纲",
            DocumentType.Chapter => "章节",
            _ => "章节"
        };

        var dir = Path.Combine(project.ProjectPath, subDir);
        Directory.CreateDirectory(dir);

        var fileName = SanitizeFileName(document.Title) + ".md";
        var filePath = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(filePath, document.Content);
    }

    public async Task SaveCharacterAsync(Character character)
    {
        var project = await LoadProjectMetaAsync(character.ProjectId);
        if (project is null) return;

        var dir = Path.Combine(project.ProjectPath, "角色");
        Directory.CreateDirectory(dir);

        var fileName = SanitizeFileName(character.Name) + ".json";
        var filePath = Path.Combine(dir, fileName);
        var json = JsonSerializer.Serialize(character, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task SaveCharactersAsync(IEnumerable<Character> characters)
    {
        foreach (var character in characters)
        {
            await SaveCharacterAsync(character);
        }
    }

    public async Task DeleteDocumentAsync(Guid documentId, Guid projectId, string title, DocumentType type)
    {
        var project = await LoadProjectMetaAsync(projectId);
        if (project is null) return;

        var subDir = type switch
        {
            DocumentType.Outline => "大纲",
            DocumentType.Chapter => "章节",
            _ => "章节"
        };

        var dir = Path.Combine(project.ProjectPath, subDir);
        var fileName = SanitizeFileName(title) + ".md";
        var filePath = Path.Combine(dir, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public async Task DeleteCharacterAsync(Guid characterId, Guid projectId, string name)
    {
        var project = await LoadProjectMetaAsync(projectId);
        if (project is null) return;

        var dir = Path.Combine(project.ProjectPath, "角色");
        var fileName = SanitizeFileName(name) + ".json";
        var filePath = Path.Combine(dir, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private async Task SaveProjectMetaAsync(Project project)
    {
        if (string.IsNullOrEmpty(project.ProjectPath)) return;

        var metaPath = Path.Combine(project.ProjectPath, "project.json");
        var json = JsonSerializer.Serialize(project, JsonOptions);
        await File.WriteAllTextAsync(metaPath, json);
    }

    private async Task<Project?> LoadProjectMetaAsync(Guid projectId)
    {
        return await _projectRepository.GetByIdAsync(projectId);
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "未命名" : sanitized;
    }
}
