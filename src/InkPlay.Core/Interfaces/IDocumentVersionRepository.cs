using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IDocumentVersionRepository
{
    Task<IReadOnlyList<DocumentVersion>> GetByDocumentIdAsync(Guid documentId);
    Task<DocumentVersion?> GetLatestVersionAsync(Guid documentId);
    Task<DocumentVersion> CreateAsync(DocumentVersion version);
    Task DeleteByDocumentIdAsync(Guid documentId);
}
