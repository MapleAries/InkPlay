using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class ReviserAgent : BaseAgent
{
    public override AgentType Type => AgentType.Reviser;
    public override string Name => "修订员";
    public override string SystemPrompt => @"你是一个专业的修订员，负责自动化修复。
你的任务是根据审计报告对草稿进行定点修复。

工作原则：
1. 关键问题必须修复：按照审计报告中的修复建议进行定点修改
2. 次要问题标记待审：在相关位置添加 [待审：问题描述] 标记
3. 修复时保持原文风格和节奏不变
4. 不引入新的问题
5. 修复后内容应自然流畅

请直接输出修复后的正文内容。对于次要问题，在相应位置插入 [待审：xxx] 标记。";

    public ReviserAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = base.BuildMessages(context);

        // Add draft and audit report
        var userMsg = $"## 审计报告\n{context.AuditReport}\n\n## 待修订内容\n{context.DraftContent}\n\n请根据审计报告修复上述内容中的问题。";
        messages.Add(new AiChatMessage { Role = "user", Content = userMsg });

        return messages;
    }
}
