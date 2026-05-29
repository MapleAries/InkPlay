using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class ProofreaderAgent : BaseAgent
{
    public override AgentType Type => AgentType.Proofreader;
    public override string Name => "校对员";
    public override string SystemPrompt => @"你是一个专业的校对员，负责篇幅标准化。
你的任务是对草稿进行单次压缩或扩写，使其精准达到预设的目标词数区间。

工作原则：
1. 如果字数过多：精简冗余描写，合并重复表达，删除不影响剧情的枝节
2. 如果字数过少：丰富场景描写，扩展对话细节，增加心理活动和环境描写
3. 保持原文风格和节奏不变
4. 不改变剧情走向和角色性格
5. 确保修改后内容自然流畅

请直接输出调整后的正文内容，不要添加任何说明。";

    public ProofreaderAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = base.BuildMessages(context);

        // Add draft content
        messages.Add(new AiChatMessage
        {
            Role = "user",
            Content = $"目标字数：{context.TargetWordCount}字\n\n请调整以下草稿至目标字数：\n\n{context.DraftContent}"
        });

        return messages;
    }
}
