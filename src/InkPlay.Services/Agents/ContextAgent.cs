using System.Text;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class ContextAgent : BaseAgent
{
    public override AgentType Type => AgentType.Context;
    public override string Name => "上下文智能体";
    public override string SystemPrompt => @"你是一个专业的创作上下文分析师，扮演'读者'角色。
你的任务是从已有的故事素材中精准检索出与当前章节相关的信息，组装成一份'创作简报'。

你需要提取：
1. 与当前章节相关的核心角色信息（性格、关系、当前状态）
2. 前文埋设的伏笔和悬念
3. 世界观设定和规则
4. 前几章的情节走向和情绪基调
5. 需要注意的连续性细节

输出格式：
## 创作简报
### 核心角色
- [角色名]: [当前状态和本章需要关注的点]
### 待回收伏笔
- [伏笔描述] - 建议在[时机]回收
### 世界观规则
- [相关规则]
### 情节走向
- [前文摘要和本章衔接点]
### 注意事项
- [需要保持一致的细节]";

    public ContextAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = new List<AiChatMessage>
        {
            new() { Role = "system", Content = SystemPrompt }
        };

        // Add character info
        if (context.Characters.Count > 0)
        {
            var characterInfo = new StringBuilder();
            characterInfo.AppendLine("## 已有角色档案");
            foreach (var c in context.Characters)
            {
                characterInfo.AppendLine($"- {c.Name}（{c.Role}）：{c.Personality}，{c.Appearance}");
            }
            messages.Add(new AiChatMessage { Role = "system", Content = characterInfo.ToString() });
        }

        // Add outline info
        if (context.Outlines.Count > 0)
        {
            var outlineInfo = new StringBuilder();
            outlineInfo.AppendLine("## 故事大纲");
            foreach (var o in context.Outlines)
            {
                outlineInfo.AppendLine($"### {o.Title}\n{o.Content}\n");
            }
            messages.Add(new AiChatMessage { Role = "system", Content = outlineInfo.ToString() });
        }

        // Add recent chapters
        if (context.Chapters.Count > 0)
        {
            var chapterInfo = new StringBuilder();
            chapterInfo.AppendLine("## 已完成章节");
            var recentChapters = context.Chapters.TakeLast(3);
            foreach (var ch in recentChapters)
            {
                var preview = ch.Content.Length > 500 ? ch.Content[..500] + "..." : ch.Content;
                chapterInfo.AppendLine($"### {ch.Title}\n{preview}\n");
            }
            messages.Add(new AiChatMessage { Role = "system", Content = chapterInfo.ToString() });
        }

        // User request
        var userMsg = $"请为即将创作的章节生成创作简报。";
        if (context.CurrentDocument is not null)
        {
            userMsg += $"\n当前章节：{context.CurrentDocument.Title}";
        }
        if (!string.IsNullOrEmpty(context.UserRequest))
        {
            userMsg += $"\n用户需求：{context.UserRequest}";
        }
        messages.Add(new AiChatMessage { Role = "user", Content = userMsg });

        return messages;
    }
}
