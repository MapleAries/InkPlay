using System.Text;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Export;

public class ExportService : IExportService
{
    private readonly IDocumentRepository _documentRepository;

    public ExportService(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public Task<byte[]> ExportAsync(Document document, ExportFormat format)
    {
        var content = format switch
        {
            ExportFormat.Markdown => ExportToMarkdown(document),
            _ => ExportToMarkdown(document)
        };
        return Task.FromResult(Encoding.UTF8.GetBytes(content));
    }

    public Task<string> ExportToMarkdownAsync(Document document)
    {
        return Task.FromResult(ExportToMarkdown(document));
    }

    public async Task<string> ExportProjectToMarkdownAsync(Project project)
    {
        var sb = new StringBuilder();

        // Title
        sb.AppendLine($"# {project.Title}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(project.Description))
        {
            sb.AppendLine($"> {project.Description}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();

        // Load all chapters
        var docs = await _documentRepository.GetByProjectIdAsync(project.Id);
        var chapters = docs
            .Where(d => d.Type == DocumentType.Chapter)
            .OrderBy(d => d.SortOrder)
            .ToList();

        if (chapters.Count == 0)
        {
            sb.AppendLine("暂无章节内容。");
            return sb.ToString();
        }

        // Table of Contents
        sb.AppendLine("## 目录");
        sb.AppendLine();
        for (int i = 0; i < chapters.Count; i++)
        {
            var wordCount = chapters[i].WordCount > 0 ? $" ({chapters[i].WordCount}字)" : "";
            sb.AppendLine($"{i + 1}. {chapters[i].Title}{wordCount}");
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Chapters
        foreach (var chapter in chapters)
        {
            if (!string.IsNullOrWhiteSpace(chapter.Content))
            {
                sb.AppendLine(chapter.Content);
            }
            else
            {
                sb.AppendLine($"## {chapter.Title}");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Summary
        var totalWords = chapters.Sum(c => c.WordCount);
        sb.AppendLine($"*全书共 {chapters.Count} 章，约 {totalWords:N0} 字*");

        return sb.ToString();
    }

    private static string ExportToMarkdown(Document document)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {document.Title}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(document.Content))
        {
            sb.AppendLine(document.Content);
        }

        return sb.ToString();
    }
}
