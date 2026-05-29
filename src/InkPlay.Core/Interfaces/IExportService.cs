using InkPlay.Core.Enums;
using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportAsync(Document document, ExportFormat format);
    Task<string> ExportToMarkdownAsync(Document document);
    Task<string> ExportProjectToMarkdownAsync(Project project);
}
