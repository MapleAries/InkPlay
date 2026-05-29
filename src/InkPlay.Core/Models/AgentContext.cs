namespace InkPlay.Core.Models;

public class AgentContext
{
    public Project Project { get; set; } = new();
    public IReadOnlyList<Character> Characters { get; set; } = Array.Empty<Character>();
    public IReadOnlyList<Document> Outlines { get; set; } = Array.Empty<Document>();
    public IReadOnlyList<Document> Chapters { get; set; } = Array.Empty<Document>();
    public Document? CurrentDocument { get; set; }
    public string UserRequest { get; set; } = string.Empty;
    public int TargetWordCount { get; set; } = 3000;
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Context Agent output - used by downstream agents
    public string ContextBrief { get; set; } = string.Empty;
    public string WritingRuleStack { get; set; } = string.Empty;
    public string ChapterSkeleton { get; set; } = string.Empty;
    public string DraftContent { get; set; } = string.Empty;
    public string AuditReport { get; set; } = string.Empty;
}
