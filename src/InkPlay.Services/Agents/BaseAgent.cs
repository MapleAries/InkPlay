using System.Runtime.CompilerServices;
using System.Text;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public abstract class BaseAgent : IAgent
{
    protected readonly IAiProviderFactory AiProviderFactory;
    protected readonly ISettingsService SettingsService;

    public abstract AgentType Type { get; }
    public abstract string Name { get; }
    public abstract string SystemPrompt { get; }

    protected virtual int MaxRetries => 3;

    protected BaseAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
    {
        AiProviderFactory = aiProviderFactory;
        SettingsService = settingsService;
    }

    public virtual async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var sb = new StringBuilder();
                await foreach (var chunk in StreamExecuteAsync(context, ct))
                {
                    sb.Append(chunk);
                }
                return new AgentResult
                {
                    Success = true,
                    Content = sb.ToString(),
                    AgentType = Type
                };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // User cancellation — do not retry
                return new AgentResult
                {
                    Success = false,
                    Content = "操作已取消",
                    AgentType = Type
                };
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries && IsTransientError(ex))
            {
                // Transient error — retry with exponential backoff
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                return new AgentResult
                {
                    Success = false,
                    Content = $"Agent {Name} 执行失败: {ex.Message}",
                    AgentType = Type
                };
            }
        }

        return new AgentResult
        {
            Success = false,
            Content = $"Agent {Name} 在 {MaxRetries} 次重试后仍然失败",
            AgentType = Type
        };
    }

    private static bool IsTransientError(HttpRequestException ex)
    {
        // Retry on network errors and server errors (5xx)
        if (ex.StatusCode is null) return true; // Network-level error
        var code = (int)ex.StatusCode;
        return code >= 500 || code == 429; // Server error or rate limit
    }

    public virtual async IAsyncEnumerable<string> StreamExecuteAsync(
        AgentContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var apiKeyConfig = SettingsService.GetDefaultApiKey(ApiKeyCategory.Text);
        if (apiKeyConfig is null)
        {
            yield return "错误：未配置文本生成 API Key";
            yield break;
        }

        var provider = AiProviderFactory.GetProviderForApiKey(apiKeyConfig);
        var messages = BuildMessages(context);

        await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages, ct))
        {
            yield return chunk;
        }
    }

    protected virtual List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = new List<AiChatMessage>();

        // System prompt
        messages.Add(new AiChatMessage { Role = "system", Content = SystemPrompt });

        // Project system prompt if available
        if (context.Project.SystemPrompt is { Length: > 0 } projectPrompt)
        {
            messages.Add(new AiChatMessage { Role = "system", Content = $"项目设定：{projectPrompt}" });
        }

        // Context brief if available
        if (!string.IsNullOrEmpty(context.ContextBrief))
        {
            messages.Add(new AiChatMessage { Role = "system", Content = $"创作简报：\n{context.ContextBrief}" });
        }

        // Writing rules if available
        if (!string.IsNullOrEmpty(context.WritingRuleStack))
        {
            messages.Add(new AiChatMessage { Role = "system", Content = $"写作规则栈：\n{context.WritingRuleStack}" });
        }

        // User request
        if (!string.IsNullOrEmpty(context.UserRequest))
        {
            messages.Add(new AiChatMessage { Role = "user", Content = context.UserRequest });
        }

        return messages;
    }

    protected async Task<string> GetCompletionAsync(AgentContext context, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        await foreach (var chunk in StreamExecuteAsync(context, ct))
        {
            sb.Append(chunk);
        }
        return sb.ToString();
    }

    public int EstimateTokens(AgentContext context)
    {
        var messages = BuildMessages(context);

        // Rough estimation: Chinese chars ~2 tokens each, English words ~1 token
        var chineseChars = 0;
        var englishWords = 0;
        var inWord = false;

        foreach (var msg in messages)
        {
            if (msg.Content is null) continue;
            foreach (var c in msg.Content)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)
                {
                    chineseChars++;
                    inWord = false;
                }
                else if (char.IsLetterOrDigit(c))
                {
                    if (!inWord)
                    {
                        englishWords++;
                        inWord = true;
                    }
                }
                else
                {
                    inWord = false;
                }
            }
        }

        return chineseChars * 2 + englishWords;
    }
}
