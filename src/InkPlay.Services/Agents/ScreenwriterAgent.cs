using System.Text;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class ScreenwriterAgent : BaseAgent
{
    public override AgentType Type => AgentType.Screenwriter;
    public override string Name => "编剧";
    public override string SystemPrompt => @"你是一个专业的编剧，负责上下文构建与素材调度。
你的任务是根据当前章节需要，从已有素材中提取相关信息，编译出'写作规则栈'。

写作规则栈包含：
1. 本章需遵循的设定约束（角色性格、世界观规则）
2. 风格指南（叙述视角、语言风格、节奏要求）
3. 前后文衔接点（前章结尾、本章需承接的内容）
4. 情绪基调（本章应传达的情感氛围）
5. 禁止事项（不能出现的OOC行为、不能违反的设定）

请以结构化的方式输出写作规则栈，供架构师和作家参考。";

    public ScreenwriterAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = new List<AiChatMessage>
        {
            new() { Role = "system", Content = SystemPrompt }
        };

        // Add outline context
        if (context.Outlines.Count > 0)
        {
            var outlineText = new StringBuilder();
            outlineText.AppendLine("## 故事大纲");
            foreach (var o in context.Outlines)
            {
                outlineText.AppendLine($"### {o.Title}\n{o.Content}\n");
            }
            messages.Add(new AiChatMessage { Role = "system", Content = outlineText.ToString() });
        }

        // Add character profiles
        if (context.Characters.Count > 0)
        {
            var charText = new StringBuilder();
            charText.AppendLine("## 角色档案");
            foreach (var c in context.Characters)
            {
                charText.AppendLine($"### {c.Name}");
                charText.AppendLine($"- 角色定位：{c.Role}");
                charText.AppendLine($"- 性格：{c.Personality}");
                charText.AppendLine($"- 动机：{c.Motivation}");
                charText.AppendLine($"- 弱点：{c.Weakness}");
                charText.AppendLine($"- 背景：{c.Backstory}\n");
            }
            messages.Add(new AiChatMessage { Role = "system", Content = charText.ToString() });
        }

        // Add recent chapters for continuity
        if (context.Chapters.Count > 0)
        {
            var recent = context.Chapters.TakeLast(2);
            var chapterText = new StringBuilder();
            chapterText.AppendLine("## 前序章节");
            foreach (var ch in recent)
            {
                var preview = ch.Content.Length > 800 ? ch.Content[..800] + "..." : ch.Content;
                chapterText.AppendLine($"### {ch.Title}\n{preview}\n");
            }
            messages.Add(new AiChatMessage { Role = "system", Content = chapterText.ToString() });
        }

        // Current chapter request
        var userMsg = "请为即将创作的章节编译写作规则栈。";
        if (context.CurrentDocument is not null)
        {
            userMsg += $"\n当前章节：{context.CurrentDocument.Title}";
            if (!string.IsNullOrEmpty(context.CurrentDocument.Content))
            {
                userMsg += $"\n章节大纲：{context.CurrentDocument.Content}";
            }
        }
        messages.Add(new AiChatMessage { Role = "user", Content = userMsg });

        return messages;
    }
}
