using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Data.Repositories;

public class DocumentVersionRepository : IDocumentVersionRepository
{
    private readonly InkPlayDbContext _db;

    public DocumentVersionRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<DocumentVersion>> GetByDocumentIdAsync(Guid documentId)
    {
        var versions = _db.DocumentVersions
            .Find(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.SnapshotAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<DocumentVersion>>(versions);
    }

    public Task<DocumentVersion?> GetLatestVersionAsync(Guid documentId)
    {
        var version = _db.DocumentVersions
            .Find(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.SnapshotAt)
            .FirstOrDefault();
        return Task.FromResult(version);
    }

    public Task<DocumentVersion> CreateAsync(DocumentVersion version)
    {
        version.SnapshotAt = DateTime.UtcNow;
        _db.DocumentVersions.Insert(version);
        return Task.FromResult(version);
    }

    public Task DeleteByDocumentIdAsync(Guid documentId)
    {
        _db.DocumentVersions.DeleteMany(v => v.DocumentId == documentId);
        return Task.CompletedTask;
    }
}
