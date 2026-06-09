using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class WriterAgent : BaseAgent
{
    public override AgentType Type => AgentType.Writer;
    public override string Name => "作家";
    public override string SystemPrompt => @"你是一个专业的网文作家，专注于高质量的文字输出。

你的写作原则：
1. 严格控制字数，确保达到目标词数区间
2. 对话驱动：用对话推进剧情，展现角色性格
3. 去AI味：避免机械化的表达，追求自然流畅的文笔
4. 细节描写：注重五感描写，让读者身临其境
5. 节奏把控：张弛有度，紧张与舒缓交替
6. 角色一致性：严格按照角色设定写作，不OOC

写作要求：
- 使用第三人称视角
- 对话要符合角色性格和身份
- 场景转换自然流畅
- 适当使用修辞手法增强表现力
- 注意段落划分，保持阅读舒适度

请直接输出小说正文内容，不要添加任何元信息或说明。";

    public WriterAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = new List<AiChatMessage>();

        // System prompt
        messages.Add(new AiChatMessage { Role = "system", Content = SystemPrompt });

        // Project system prompt
        if (context.Project.SystemPrompt is { Length: > 0 } projectPrompt)
        {
            messages.Add(new AiChatMessage { Role = "system", Content = $"项目设定：{projectPrompt}" });
        }

        // Genre-specific writing guidelines
        var genre = context.Project.Genre;
        if (!string.IsNullOrWhiteSpace(genre))
        {
            var genrePrompt = PromptTemplates.GetWriterGenrePrompt(genre);
            if (!string.IsNullOrEmpty(genrePrompt))
            {
                messages.Add(new AiChatMessage { Role = "system", Content = genrePrompt });
            }
        }

        // User-defined style notes
        if (!string.IsNullOrWhiteSpace(context.Project.WritingStyleNotes))
        {
            messages.Add(new AiChatMessage
            {
                Role = "system",
                Content = $"用户自定义风格要求：\n{context.Project.WritingStyleNotes}"
            });
        }

        // Context brief
        if (!string.IsNullOrEmpty(context.ContextBrief))
        {
            messages.Add(new AiChatMessage { Role = "system", Content = $"创作简报：\n{context.ContextBrief}" });
        }

        // Writing rules
        if (!string.IsNullOrEmpty(context.WritingRuleStack))
        {
            messages.Add(new AiChatMessage { Role = "system", Content = $"写作规则栈：\n{context.WritingRuleStack}" });
        }

        // Character profiles
        if (context.Characters.Count > 0)
        {
            var charInfo = new System.Text.StringBuilder("角色参考：\n");
            foreach (var c in context.Characters)
            {
                charInfo.AppendLine($"- {c.Name}（{c.Role}）：{c.Personality}，说话风格参考");
            }
            messages.Add(new AiChatMessage { Role = "system", Content = charInfo.ToString() });
        }

        // Chapter skeleton
        if (!string.IsNullOrEmpty(context.ChapterSkeleton))
        {
            messages.Add(new AiChatMessage { Role = "user", Content = $"章节骨架：\n{context.ChapterSkeleton}" });
        }

        // User request + target word count
        var userRequest = context.UserRequest;
        if (!string.IsNullOrEmpty(userRequest))
        {
            messages.Add(new AiChatMessage { Role = "user", Content = userRequest });
        }
        messages.Add(new AiChatMessage
        {
            Role = "user",
            Content = $"目标字数：{context.TargetWordCount}字左右。请直接开始写作正文。"
        });

        return messages;
    }
}
