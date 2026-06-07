namespace InkPlay.Core;

/// <summary>
/// Application-wide constants to eliminate magic strings.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Pipeline progress status values used in PipelineProgress.Status.
    /// </summary>
    public static class PipelineStatus
    {
        public const string Running = "running";
        public const string Completed = "completed";
        public const string Failed = "failed";
        public const string Revision = "revision";
    }

    /// <summary>
    /// Audit result markers expected from the AuditorAgent.
    /// </summary>
    public static class AuditResult
    {
        public const string PassPrefix = "RESULT: PASS";
        public const string FailPrefix = "RESULT: FAIL";
        public const string PassChinese = "通过";
        public const string FailChinese = "不通过";
        public const string PassSectionHeader = "## 审计结果：通过";
    }

    /// <summary>
    /// Change source values for DocumentVersion tracking.
    /// </summary>
    public static class ChangeSource
    {
        public const string ManualEdit = "ManualEdit";
        public const string AutoSave = "AutoSave";
        public const string AiGenerate = "AiGenerate";
        public const string DataAgent = "DataAgent";
    }

    /// <summary>
    /// File system subdirectory names for project structure.
    /// </summary>
    public static class ProjectDirs
    {
        public const string Outline = "大纲";
        public const string Chapter = "章节";
        public const string Character = "角色";
        public const string Conversation = "对话历史";
    }
}
