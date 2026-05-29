using System.Text;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.Services.Agents;

public class DataAgent : BaseAgent
{
    public override AgentType Type => AgentType.Data;
    public override string Name => "数据智能体";
    public override string SystemPrompt => @"你是一个专业的数据管理员，扮演'图书管理员'角色。
你的任务是从新写完的章节中提取结构化数据，更新到知识库中。

你需要提取：
1. 新出现的角色（姓名、性别、外貌、性格、身份）
2. 已有角色的状态变化（伤势、情绪、位置变化等）
3. 新出现的地点和设定
4. 新埋设的伏笔
5. 已回收的伏笔
6. 角色关系变化

请严格按以下JSON格式返回：
{
  ""newCharacters"": [{""Name"":""名"",""Gender"":""性别"",""Role"":""角色"",""Appearance"":""外貌"",""Personality"":""性格""}],
  ""characterUpdates"": [{""Name"":""名"",""Changes"":""变化描述""}],
  ""newLocations"": [""地点""],
  ""newForeshadowing"": [""伏笔描述""],
  ""resolvedForeshadowing"": [""已回收伏笔""],
  ""relationshipChanges"": [{""From"":""角色A"",""To"":""角色B"",""Change"":""关系变化""}]
}
只返回JSON，不要有其他文字。如果某个类别没有内容，返回空数组。";

    public DataAgent(IAiProviderFactory aiProviderFactory, ISettingsService settingsService)
        : base(aiProviderFactory, settingsService) { }

    protected override List<AiChatMessage> BuildMessages(AgentContext context)
    {
        var messages = new List<AiChatMessage>
        {
            new() { Role = "system", Content = SystemPrompt }
        };

        // Add existing characters for reference
        if (context.Characters.Count > 0)
        {
            var characterNames = string.Join("、", context.Characters.Select(c => c.Name));
            messages.Add(new AiChatMessage { Role = "system", Content = $"已有角色：{characterNames}" });
        }

        // The chapter content to analyze
        var content = context.DraftContent;
        if (string.IsNullOrEmpty(content) && context.CurrentDocument is not null)
        {
            content = context.CurrentDocument.Content;
        }

        messages.Add(new AiChatMessage { Role = "user", Content = $"请分析以下章节内容：\n\n{content}" });

        return messages;
    }
}
