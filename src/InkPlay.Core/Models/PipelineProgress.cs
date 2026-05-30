using InkPlay.Core.Enums;

namespace InkPlay.Core.Models;

public class PipelineProgress
{
    public AgentType CurrentAgent { get; set; }
    public AgentType AgentType { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public int TotalSteps { get; set; }
    public string Status { get; set; } = string.Empty; // running/completed/failed/retrying/revision
    public string StatusMessage { get; set; } = string.Empty;
    public string? StreamingContent { get; set; }
    public int RetryAttempt { get; set; }
    public int MaxRetries { get; set; }
    public int RevisionRound { get; set; }
    public int MaxRevisionRounds { get; set; }
    public int ChapterIndex { get; set; }
    public int TotalChapters { get; set; }
    public int CompletedChapters { get; set; }
}
