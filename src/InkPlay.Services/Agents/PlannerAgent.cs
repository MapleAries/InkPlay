using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class PlannerAgent : BaseAgent
{
    public override AgentType Type => AgentType.Planner;
    public override string Name => "规划师";
    public override string SystemPrompt => @"你是一个专业的网文规划师，是故事的'总导演'。
你的任务是规划故事大纲和设计整体结构。

你应该：
1. 设计引人入胜的故事核心概念和主题
2. 规划分卷结构和章节大纲
3. 设计核心冲突和高潮节点
4. 安排伏笔和悬念的埋设/回收节奏
5. 确保故事结构的完整性和吸引力

大纲格式要求：
# 书名
> 一句话简介
## 故事简介
## 世界观设定
## 主要角色
## 分卷大纲
### 第一卷：卷名
#### 第一章：章节名 - 简要内容
#### 第二章：章节名 - 简要内容
...

请用中文回复，使用 Markdown 格式。";

    public PlannerAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = base.BuildMessages(context);

        // Add character info if available
        if (context.Characters.Count > 0)
        {
            var characterInfo = "已有角色设定：\n";
            foreach (var c in context.Characters)
            {
                characterInfo += $"- {c.Name}（{c.Role}）：{c.Personality}\n";
            }
            messages.Insert(messages.Count - 1, new AiChatMessage { Role = "system", Content = characterInfo });
        }

        return messages;
    }
}
