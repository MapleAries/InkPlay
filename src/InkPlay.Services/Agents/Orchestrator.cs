using System.Runtime.CompilerServices;
using System.Text.Json;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class Orchestrator : IOrchestrator
{
    private readonly Dictionary<AgentType, IAgent> _agents;
    private readonly ICharacterRepository _characterRepository;
    private const int MaxRevisionRounds = 3;

    public Orchestrator(IEnumerable<IAgent> agents, ICharacterRepository characterRepository)
    {
        _agents = agents.ToDictionary(a => a.Type);
        _characterRepository = characterRepository;
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

    public Task<AgentResult> AutoWriteChapterAsync(AgentContext context, CancellationToken ct = default)
    {
        return AutoWriteChapterAsync(context, null, ct);
    }

    public async Task<AgentResult> AutoWriteChapterAsync(AgentContext context, IProgress<PipelineProgress>? progress, CancellationToken ct = default)
    {
        const int totalSteps = 7; // Context + Screenwriter + Architect + Writer + Proofreader + Auditor + Data/Reviser

        // Step 1: Context Agent - build context brief
        var contextResult = await ExecuteStepWithProgressAsync(AgentType.Context, context, 1, totalSteps, progress, ct);
        if (contextResult.Success)
        {
            context.ContextBrief = contextResult.Content;
        }

        // Step 2: Screenwriter Agent - build writing rule stack
        var screenwriterResult = await ExecuteStepWithProgressAsync(AgentType.Screenwriter, context, 2, totalSteps, progress, ct);
        if (screenwriterResult.Success)
        {
            context.WritingRuleStack = screenwriterResult.Content;
        }

        // Step 3: Architect Agent - build chapter skeleton
        var architectResult = await ExecuteStepWithProgressAsync(AgentType.Architect, context, 3, totalSteps, progress, ct);
        if (architectResult.Success)
        {
            context.ChapterSkeleton = architectResult.Content;
        }

        // Step 4-7: Write → Proofread → Audit → Revise loop
        for (int round = 0; round < MaxRevisionRounds; round++)
        {
            // Step 4: Writer Agent - generate content
            var writerResult = await ExecuteStepWithProgressAsync(AgentType.Writer, context, 4, totalSteps, progress, ct);
            if (!writerResult.Success)
            {
                return writerResult;
            }
            context.DraftContent = writerResult.Content;

            // Step 5: Proofreader Agent - standardize length
            var proofreaderResult = await ExecuteStepWithProgressAsync(AgentType.Proofreader, context, 5, totalSteps, progress, ct);
            if (proofreaderResult.Success)
            {
                context.DraftContent = proofreaderResult.Content;
            }

            // Step 6: Auditor Agent - quality check
            var auditorResult = await ExecuteStepWithProgressAsync(AgentType.Auditor, context, 6, totalSteps, progress, ct);
            if (!auditorResult.Success)
            {
                return auditorResult;
            }
            context.AuditReport = auditorResult.Content;

            // Check if audit passed
            bool auditPassed = auditorResult.Content.StartsWith("RESULT: PASS")
                || auditorResult.Content.Contains("## 审计结果：通过")
                || (!auditorResult.Content.Contains("不通过") && auditorResult.Content.Contains("通过"));

            if (auditPassed)
            {
                // Step 7: Data Agent - update knowledge base
                var dataResult = await ExecuteStepWithProgressAsync(AgentType.Data, context, 7, totalSteps, progress, ct);

                // Persist extracted data
                if (dataResult.Success)
                {
                    await PersistExtractedDataAsync(dataResult.Content, context);
                }

                progress?.Report(new PipelineProgress
                {
                    CurrentAgent = AgentType.Data,
                    AgentName = "数据智能体",
                    StepNumber = 7,
                    TotalSteps = totalSteps,
                    Status = "completed"
                });

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
            var reviserResult = await ExecuteStepWithProgressAsync(AgentType.Reviser, context, 7, totalSteps, progress, ct);
            if (reviserResult.Success)
            {
                context.DraftContent = reviserResult.Content;
            }
        }

        // Max rounds reached, return best effort
        progress?.Report(new PipelineProgress
        {
            CurrentAgent = AgentType.Reviser,
            AgentName = "修订员",
            StepNumber = totalSteps,
            TotalSteps = totalSteps,
            Status = "completed"
        });

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

    private async Task<AgentResult> ExecuteStepWithProgressAsync(
        AgentType type,
        AgentContext context,
        int stepNumber,
        int totalSteps,
        IProgress<PipelineProgress>? progress,
        CancellationToken ct)
    {
        var agentName = _agents.TryGetValue(type, out var agent) ? agent.Name : type.ToString();

        progress?.Report(new PipelineProgress
        {
            CurrentAgent = type,
            AgentName = agentName,
            StepNumber = stepNumber,
            TotalSteps = totalSteps,
            Status = "running"
        });

        var result = await ExecuteStepAsync(type, context, ct);

        progress?.Report(new PipelineProgress
        {
            CurrentAgent = type,
            AgentName = agentName,
            StepNumber = stepNumber,
            TotalSteps = totalSteps,
            Status = result.Success ? "completed" : "failed"
        });

        return result;
    }

    private async Task PersistExtractedDataAsync(string dataAgentOutput, AgentContext context)
    {
        try
        {
            // Parse JSON output from DataAgent
            using var doc = JsonDocument.Parse(dataAgentOutput);
            var root = doc.RootElement;

            // Extract new characters
            if (root.TryGetProperty("newCharacters", out var newChars) && newChars.ValueKind == JsonValueKind.Array)
            {
                foreach (var charElement in newChars.EnumerateArray())
                {
                    var character = new Character
                    {
                        ProjectId = context.Project.Id,
                        Name = charElement.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                        Role = charElement.TryGetProperty("role", out var role) ? role.GetString() ?? "" : "",
                        Personality = charElement.TryGetProperty("personality", out var personality) ? personality.GetString() ?? "" : "",
                        Backstory = charElement.TryGetProperty("backstory", out var backstory) ? backstory.GetString() ?? "" : ""
                    };

                    if (!string.IsNullOrWhiteSpace(character.Name))
                    {
                        await _characterRepository.CreateAsync(character);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // DataAgent output may not be valid JSON — skip persistence
        }
        catch (Exception)
        {
            // Log but don't fail the pipeline
        }
    }
}
