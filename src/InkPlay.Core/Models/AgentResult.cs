using InkPlay.Core.Enums;

namespace InkPlay.Core.Models;

public class AgentResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public AgentType AgentType { get; set; }
    public List<string> CriticalIssues { get; set; } = new();
    public List<string> MinorIssues { get; set; } = new();
    public List<Character> NewCharacters { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
