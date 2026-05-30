using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using InkPlay.Services.Data;

namespace InkPlay.Services.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly InkPlayDbContext _db;
    private readonly IProjectRepository _projectRepository;
    private readonly IFileProjectService _fileProjectService;
    private readonly IDocumentVersionRepository _versionRepository;

    public DocumentRepository(InkPlayDbContext db, IProjectRepository projectRepository, IFileProjectService fileProjectService, IDocumentVersionRepository versionRepository)
    {
        _db = db;
        _projectRepository = projectRepository;
        _fileProjectService = fileProjectService;
        _versionRepository = versionRepository;
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

    public async Task<Document> CreateAsync(Document document)
    {
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = DateTime.UtcNow;
        document.WordCount = CalculateWordCount(document.Content);
        _db.Documents.Insert(document);

        // 保存到文件系统
        await SaveToFileSystemAsync(document);

        return document;
    }

    public async Task UpdateAsync(Document document, string changeSource = "ManualEdit", string changeSummary = "")
    {
        // 获取旧版本用于快照
        var existing = _db.Documents.FindById(document.Id);
        if (existing != null && existing.Content != document.Content)
        {
            // 创建版本快照（保存旧内容）
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
        }

        document.UpdatedAt = DateTime.UtcNow;
        document.WordCount = CalculateWordCount(document.Content);
        _db.Documents.Update(document);

        // 保存到文件系统
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
        catch
        {
            // 文件保存失败不影响 LiteDB 操作
        }
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
