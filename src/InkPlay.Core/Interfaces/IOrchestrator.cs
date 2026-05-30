using InkPlay.Core.Enums;
using InkPlay.Core.Models;

namespace InkPlay.Core.Interfaces;

public interface IOrchestrator
{
    Task<AgentResult> ExecuteStepAsync(AgentType type, AgentContext context, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamStepAsync(AgentType type, AgentContext context, CancellationToken ct = default);
    Task<AgentResult> AutoWriteChapterAsync(AgentContext context, CancellationToken ct = default);
    Task<AgentResult> AutoWriteChapterAsync(AgentContext context, IProgress<PipelineProgress>? progress, CancellationToken ct = default);
    IAsyncEnumerable<AgentResult> AutoWriteBatchAsync(IReadOnlyList<Document> chapters, AgentContext baseContext, IProgress<PipelineProgress>? progress = null, CancellationToken ct = default);
    CostEstimate EstimateCost(AgentContext context);
}
