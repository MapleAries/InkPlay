using InkPlay.Core.Enums;
using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IAgent
{
    AgentType Type { get; }
    string Name { get; }
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamExecuteAsync(AgentContext context, CancellationToken ct = default);
}
