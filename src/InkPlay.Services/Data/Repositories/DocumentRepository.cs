using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;

namespace InkPlay.Services.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly InkPlayDbContext _db;

    public DocumentRepository(InkPlayDbContext db)
    {
        _db = db;
    }

    public Task<Document?> GetByIdAsync(Guid id)
    {
        var doc = _db.Documents.FindById(id);
        return Task.FromResult(doc);
    }

    public Task<IReadOnlyList<Document>> GetByProjectIdAsync(Guid projectId)
    {
        var docs = _db.Documents
            .Find(d => d.ProjectId == projectId)
            .OrderBy(d => d.SortOrder)
            .ToList();
        return Task.FromResult<IReadOnlyList<Document>>(docs);
    }

    public Task<Document> CreateAsync(Document document)
    {
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = DateTime.UtcNow;
        document.WordCount = CalculateWordCount(document.Content);
        _db.Documents.Insert(document);
        return Task.FromResult(document);
    }

    public Task UpdateAsync(Document document)
    {
        document.UpdatedAt = DateTime.UtcNow;
        document.WordCount = CalculateWordCount(document.Content);
        _db.Documents.Update(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _db.Documents.Delete(id);
        return Task.CompletedTask;
    }

    private static int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        // Count Chinese characters + English words
        var count = 0;
        var inWord = false;
        foreach (var c in content)
        {
            if (c >= 0x4E00 && c <= 0x9FFF) // CJK Unified Ideographs
            {
                count++;
                inWord = false;
            }
            else if (char.IsLetterOrDigit(c))
            {
                if (!inWord)
                {
                    count++;
                    inWord = true;
                }
            }
            else
            {
                inWord = false;
            }
        }
        return count;
    }
}
