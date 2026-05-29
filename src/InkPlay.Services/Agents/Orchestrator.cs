using System.Runtime.CompilerServices;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class Orchestrator : IOrchestrator
{
    private readonly Dictionary<AgentType, IAgent> _agents;
    private const int MaxRevisionRounds = 3;

    public Orchestrator(IEnumerable<IAgent> agents)
    {
        _agents = agents.ToDictionary(a => a.Type);
    }

    public async Task<AgentResult> ExecuteStepAsync(AgentType type, AgentContext context, CancellationToken ct = default)
    {
        if (!_agents.TryGetValue(type, out var agent))
        {
            return new AgentResult
            {
                Success = false,
                Content = $"Agent {type} not found",
                AgentType = type
            };
        }

        return await agent.ExecuteAsync(context, ct);
    }

    public async IAsyncEnumerable<string> StreamStepAsync(
        AgentType type,
        AgentContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_agents.TryGetValue(type, out var agent))
        {
            yield return $"Agent {type} not found";
            yield break;
        }

        await foreach (var chunk in agent.StreamExecuteAsync(context, ct))
        {
            yield return chunk;
        }
    }

    public async Task<AgentResult> AutoWriteChapterAsync(AgentContext context, CancellationToken ct = default)
    {
        // Step 1: Context Agent - build context brief
        var contextResult = await ExecuteStepAsync(AgentType.Context, context, ct);
        if (contextResult.Success)
        {
            context.ContextBrief = contextResult.Content;
        }

        // Step 2: Screenwriter Agent - build writing rule stack
        var screenwriterResult = await ExecuteStepAsync(AgentType.Screenwriter, context, ct);
        if (screenwriterResult.Success)
        {
            context.WritingRuleStack = screenwriterResult.Content;
        }

        // Step 3: Architect Agent - build chapter skeleton
        var architectResult = await ExecuteStepAsync(AgentType.Architect, context, ct);
        if (architectResult.Success)
        {
            context.ChapterSkeleton = architectResult.Content;
        }

        // Step 4-7: Write → Proofread → Audit → Revise loop
        for (int round = 0; round < MaxRevisionRounds; round++)
        {
            // Step 4: Writer Agent - generate content
            var writerResult = await ExecuteStepAsync(AgentType.Writer, context, ct);
            if (!writerResult.Success)
            {
                return writerResult;
            }
            context.DraftContent = writerResult.Content;

            // Step 5: Proofreader Agent - standardize length
            var proofreaderResult = await ExecuteStepAsync(AgentType.Proofreader, context, ct);
            if (proofreaderResult.Success)
            {
                context.DraftContent = proofreaderResult.Content;
            }

            // Step 6: Auditor Agent - quality check
            var auditorResult = await ExecuteStepAsync(AgentType.Auditor, context, ct);
            if (!auditorResult.Success)
            {
                return auditorResult;
            }
            context.AuditReport = auditorResult.Content;

            // Check if audit passed
            if (!auditorResult.Content.Contains("不通过"))
            {
                // Step 7: Data Agent - update knowledge base
                await ExecuteStepAsync(AgentType.Data, context, ct);

                return new AgentResult
                {
                    Success = true,
                    Content = context.DraftContent,
                    AgentType = AgentType.Writer,
                    Metadata = new Dictionary<string, string>
                    {
                        ["revision_rounds"] = round.ToString(),
                        ["audit_report"] = context.AuditReport
                    }
                };
            }

            // Step 7: Reviser Agent - fix issues
            var reviserResult = await ExecuteStepAsync(AgentType.Reviser, context, ct);
            if (reviserResult.Success)
            {
                context.DraftContent = reviserResult.Content;
            }
        }

        // Max rounds reached, return best effort
        return new AgentResult
        {
            Success = true,
            Content = context.DraftContent,
            AgentType = AgentType.Reviser,
            CriticalIssues = new List<string> { "达到最大修订轮次，部分内容可能仍需人工审核" },
            Metadata = new Dictionary<string, string>
            {
                ["revision_rounds"] = MaxRevisionRounds.ToString(),
                ["audit_report"] = context.AuditReport
            }
        };
    }
}
