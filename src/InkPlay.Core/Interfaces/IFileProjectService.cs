using InkPlay.Core.Enums;
using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IFileProjectService
{
    Task<Project> CreateProjectAsync(string parentDirectory, Project project, string? outlineContent = null);
    Task SaveDocumentAsync(Document document);
    Task SaveCharacterAsync(Character character);
    Task SaveCharactersAsync(IEnumerable<Character> characters);
    Task DeleteDocumentAsync(Guid documentId, Guid projectId, string title, DocumentType type);
    Task DeleteCharacterAsync(Guid characterId, Guid projectId, string name);
}
