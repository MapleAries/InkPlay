using System.Collections.Concurrent;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;
using Microsoft.Extensions.Logging;

namespace InkPlay.Services.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly InkPlayDbContext _db;
    private readonly IProjectRepository _projectRepository;
    private readonly IFileProjectService _fileProjectService;
    private readonly IDocumentVersionRepository _versionRepository;
    private readonly ILogger<DocumentRepository> _logger;
    private static readonly ConcurrentDictionary<Guid, DateTime> _lastSnapshotTime = new();
    private static readonly TimeSpan MinSnapshotInterval = TimeSpan.FromSeconds(30);

    public DocumentRepository(InkPlayDbContext db, IProjectRepository projectRepository, IFileProjectService fileProjectService, IDocumentVersionRepository versionRepository, ILogger<DocumentRepository> logger)
    {
        _db = db;
        _projectRepository = projectRepository;
        _fileProjectService = fileProjectService;
        _versionRepository = versionRepository;
        _logger = logger;
    }

    public Task<Document?> GetByIdAsync(Guid id)
    {
        var doc = _db.Documents.FindById(id);
        return Task.FromResult<Document?>(doc);
    }

    public Task<IReadOnlyList<Document>> GetByProjectIdAsync(Guid projectId)
    {
        var docs = _db.Documents
            .Find(d => d.ProjectId == projectId)
            .OrderBy(d => d.SortOrder)
            .ToList();
        return Task.FromResult<IReadOnlyList<Document>>(docs);
    }

    public Task<IReadOnlyList<Document>> GetByProjectIdAndTypeAsync(Guid projectId, DocumentType type)
    {
        var docs = _db.Documents
            .Find(d => d.ProjectId == projectId && d.Type == type)
            .OrderBy(d => d.SortOrder)
            .ToList();
        return Task.FromResult<IReadOnlyList<Document>>(docs);
    }

    public async Task<Document> CreateAsync(Document document)
    {
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = DateTime.UtcNow;
        document.WordCount = CalculateWordCount(document.Content);
        _db.Documents.Insert(document);

        await SaveToFileSystemAsync(document);

        return document;
    }

    public async Task UpdateAsync(Document document, string changeSource = "ManualEdit", string changeSummary = "")
    {
        var existing = _db.Documents.FindById(document.Id);
        if (existing != null && existing.Content != document.Content)
        {
            // Rate-limit version snapshots to avoid excessive growth during auto-save
            var shouldSnapshot = !_lastSnapshotTime.TryGetValue(document.Id, out var lastTime)
                || DateTime.UtcNow - lastTime >= MinSnapshotInterval;

            if (shouldSnapshot)
            {
                var version = new DocumentVersion
                {
                    DocumentId = document.Id,
                    ProjectId = document.ProjectId,
                    Content = existing.Content,
                    Title = existing.Title,
                    WordCount = existing.WordCount,
                    ChangeSource = changeSource,
                    ChangeSummary = changeSummary
                };
                await _versionRepository.CreateAsync(version);
                _lastSnapshotTime[document.Id] = DateTime.UtcNow;
            }
        }

        document.UpdatedAt = DateTime.UtcNow;
        document.WordCount = CalculateWordCount(document.Content);
        _db.Documents.Update(document);

        await SaveToFileSystemAsync(document);
    }

    public Task DeleteAsync(Guid id)
    {
        _db.Documents.Delete(id);
        return Task.CompletedTask;
    }

    private async Task SaveToFileSystemAsync(Document document)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(document.ProjectId);
            if (project is not null && !string.IsNullOrEmpty(project.ProjectPath))
            {
                await _fileProjectService.SaveDocumentAsync(document);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save document '{Title}' to file system, database update still applied", document.Title);
        }
    }

    private static int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        var count = 0;
        var inWord = false;
        foreach (var c in content)
        {
            if (c >= 0x4E00 && c <= 0x9FFF)
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
