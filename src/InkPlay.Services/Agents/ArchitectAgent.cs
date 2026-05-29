using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class ArchitectAgent : BaseAgent
{
    public override AgentType Type => AgentType.Architect;
    public override string Name => "架构师";
    public override string SystemPrompt => @"你是一个专业的架构师，负责搭建单章骨架。
你的任务是规划具体的章节结构，包括场景的起承转合、叙事节奏以及本章需要达到的情绪目标。

你需要输出：
1. 场景列表（每个场景的地点、时间、参与角色）
2. 每个场景的冲突点和转折点
3. 叙事节奏安排（紧张/舒缓/高潮的分布）
4. 情绪曲线（开头情绪 → 过程变化 → 结尾情绪）
5. 章节目标（本章需要推进的剧情点）

输出格式：
## 章节骨架：[章节名]
### 场景一：[场景名]
- 地点：
- 时间：
- 角色：
- 冲突：
- 转折：
- 情绪：

### 场景二：...

### 叙事节奏
[节奏安排说明]

### 情绪曲线
[情绪变化说明]

### 章节目标
[本章需推进的剧情点]";

    public ArchitectAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = base.BuildMessages(context);

        // Add current chapter outline if available
        if (context.CurrentDocument is not null)
        {
            var chapterInfo = $"当前章节：{context.CurrentDocument.Title}";
            if (!string.IsNullOrEmpty(context.CurrentDocument.Content))
            {
                chapterInfo += $"\n章节大纲：{context.CurrentDocument.Content}";
            }
            messages.Insert(messages.Count - 1, new AiChatMessage { Role = "user", Content = chapterInfo });
        }

        // Add target word count
        messages.Add(new AiChatMessage
        {
            Role = "user",
            Content = $"目标字数：{context.TargetWordCount}字。请据此安排场景数量和篇幅。"
        });

        return messages;
    }
}
