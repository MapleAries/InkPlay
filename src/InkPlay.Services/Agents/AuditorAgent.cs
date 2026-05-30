using System.Text;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class AuditorAgent : BaseAgent
{
    public override AgentType Type => AgentType.Auditor;
    public override string Name => "审计员";
    public override string SystemPrompt => @"你是一个严格的质量审计员，负责内容质量把控。
你的任务是将草稿与已有素材进行交叉比对，从多维度验证内容质量。

检查维度：
1. 角色一致性（OOC检查）：角色行为是否符合其性格设定
2. 时间线一致性：事件时间顺序是否合理
3. 伏笔回收：前文埋设的伏笔是否被妥善处理
4. 战力/能力一致性：角色能力是否前后一致
5. 世界观一致性：是否违反已建立的世界观规则
6. 情节合理性：剧情发展是否自然合理
7. 对话真实性：对话是否符合角色身份和场景

请严格按以下格式输出审计报告：

## 审计结果：通过/不通过

### 关键问题（必须修复）
1. [问题描述] - [具体位置] - [修复建议]
2. ...

### 次要问题（建议修复）
1. [问题描述] - [具体位置] - [修复建议]
2. ...

### 通过项
- [检查维度]：无异常

如果没有任何问题，关键问题和次要问题留空。";

    public AuditorAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var systemPrompt = SystemPrompt;

        // Add genre-specific audit criteria
        var genre = context.Project.Genre;
        if (!string.IsNullOrWhiteSpace(genre))
        {
            var genreAudit = PromptTemplates.GetAuditorGenrePrompt(genre);
            if (!string.IsNullOrEmpty(genreAudit))
            {
                systemPrompt += "\n\n" + genreAudit;
            }
        }

        var messages = new List<AiChatMessage>
        {
            new() { Role = "system", Content = systemPrompt }
        };

        // Add character profiles for OOC check
        if (context.Characters.Count > 0)
        {
            var charText = new StringBuilder();
            charText.AppendLine("## 角色设定（用于一致性检查）");
            foreach (var c in context.Characters)
            {
                charText.AppendLine($"- {c.Name}：性格={c.Personality}，动机={c.Motivation}，弱点={c.Weakness}");
            }
            messages.Add(new AiChatMessage { Role = "system", Content = charText.ToString() });
        }

        // Add outline for plot consistency
        if (context.Outlines.Count > 0)
        {
            var outlineText = "## 故事大纲（用于剧情一致性检查）\n";
            foreach (var o in context.Outlines)
            {
                outlineText += $"### {o.Title}\n{o.Content}\n";
            }
            messages.Add(new AiChatMessage { Role = "system", Content = outlineText });
        }

        // Add draft to audit
        messages.Add(new AiChatMessage
        {
            Role = "user",
            Content = $"请审计以下章节内容：\n\n{context.DraftContent}\n\n请在报告第一行注明 RESULT: PASS 或 RESULT: FAIL。"
        });

        return messages;
    }
}
