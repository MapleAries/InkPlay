using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Document>> GetByProjectIdAsync(Guid projectId);
    Task<Document> CreateAsync(Document document);
    Task UpdateAsync(Document document, string changeSource = "ManualEdit", string changeSummary = "");
    Task DeleteAsync(Guid id);
}
