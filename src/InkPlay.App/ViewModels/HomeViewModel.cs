using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IProjectRepository _projectRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IProjectContext _projectContext;
    private readonly INavigationService _navigationService;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly ISettingsService _settingsService;
    private readonly IFileProjectService _fileProjectService;

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    [ObservableProperty]
    private Project? _selectedProject;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showCreateDialog;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private bool _createDialogOpen;

    [ObservableProperty]
    private string _createStatusMessage = string.Empty;

    // Step tracking
    [ObservableProperty]
    private int _creationStep = 1; // 1=选择方式, 2=输入内容, 3=选择目录

    // Step 1: Creation mode
    [ObservableProperty]
    private string _creationMode = "inspiration"; // "inspiration" / "outline" / "none"

    // Step 2: Content input
    [ObservableProperty]
    private string _inspirationText = string.Empty;

    [ObservableProperty]
    private string _outlineText = string.Empty;

    [ObservableProperty]
    private string _novelType = "玄幻";

    [ObservableProperty]
    private string _novelTags = string.Empty;

    // Step 3: Directory
    [ObservableProperty]
    private string _selectedParentDirectory = string.Empty;

    // Edit dialog
    [ObservableProperty]
    private string _editProjectTitle = string.Empty;

    [ObservableProperty]
    private string _editProjectDescription = string.Empty;

    public HomeViewModel(
        IProjectRepository projectRepository,
        IDocumentRepository documentRepository,
        ICharacterRepository characterRepository,
        IProjectContext projectContext,
        INavigationService navigationService,
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService,
        IFileProjectService fileProjectService)
    {
        _projectRepository = projectRepository;
        _documentRepository = documentRepository;
        _characterRepository = characterRepository;
        _projectContext = projectContext;
        _navigationService = navigationService;
        _aiProviderFactory = aiProviderFactory;
        _settingsService = settingsService;
        _fileProjectService = fileProjectService;
    }

    public override async void NavigatedTo(object? parameter)
    {
        await LoadProjectsAsync();
    }

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        IsLoading = true;
        try
        {
            var projects = await _projectRepository.GetAllAsync();
            Projects = new ObservableCollection<Project>(projects);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool IsProjectDirectoryExists(Project project)
    {
        return !string.IsNullOrEmpty(project.ProjectPath) && Directory.Exists(project.ProjectPath);
    }

    [RelayCommand]
    private async Task RemoveProjectFromIndexAsync(Project project)
    {
        await _projectRepository.DeleteAsync(project.Id);
        Projects.Remove(project);
    }

    // --- Creation Flow ---

    [RelayCommand]
    private void ShowCreateProjectDialog()
    {
        CreationStep = 1;
        CreationMode = "inspiration";
        InspirationText = string.Empty;
        OutlineText = string.Empty;
        NovelType = "玄幻";
        NovelTags = string.Empty;
        SelectedParentDirectory = string.Empty;
        CreateStatusMessage = string.Empty;
        CreateDialogOpen = true;
    }

    [RelayCommand]
    private void NextStep()
    {
        CreateStatusMessage = string.Empty;

        if (CreationStep == 1)
        {
            CreationStep = 2;
        }
        else if (CreationStep == 2)
        {
            // Validate step 2 input
            if (CreationMode == "inspiration" && string.IsNullOrWhiteSpace(InspirationText))
            {
                CreateStatusMessage = "请输入创作灵感";
                return;
            }
            if (CreationMode == "outline" && string.IsNullOrWhiteSpace(OutlineText))
            {
                CreateStatusMessage = "请粘贴大纲内容";
                return;
            }
            if (CreationMode == "none" && string.IsNullOrWhiteSpace(NovelTags))
            {
                CreateStatusMessage = "请输入至少一个标签";
                return;
            }
            CreationStep = 3;
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        CreateStatusMessage = string.Empty;
        if (CreationStep > 1)
        {
            CreationStep--;
        }
    }

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedParentDirectory))
        {
            CreateStatusMessage = "请选择小说保存目录";
            return;
        }

        CreateStatusMessage = string.Empty;
        IsCreating = true;

        try
        {
            string? outline = null;
            string title = "";
            string summary = "";

            switch (CreationMode)
            {
                case "inspiration":
                    outline = await GenerateOutlineFromInspirationAsync();
                    if (outline is null)
                    {
                        CreateStatusMessage = "AI 生成大纲失败，请检查 API 设置或网络连接";
                        return;
                    }
                    var extractedInsp = ExtractTitleAndSummary(outline);
                    title = extractedInsp.Title;
                    summary = extractedInsp.Summary;
                    break;

                case "outline":
                    outline = OutlineText.Trim();
                    var extractedOutline = ExtractTitleAndSummary(outline);
                    title = extractedOutline.Title;
                    summary = extractedOutline.Summary;
                    // If local extraction failed, try AI
                    if (string.IsNullOrEmpty(title))
                    {
                        var aiExtracted = await ExtractTitleAndSummaryViaAiAsync(outline);
                        title = aiExtracted.Title;
                        summary = aiExtracted.Summary;
                    }
                    break;

                case "none":
                    outline = await GenerateOutlineFromTagsAsync();
                    if (outline is null)
                    {
                        CreateStatusMessage = "AI 生成大纲失败，请检查 API 设置或网络连接";
                        return;
                    }
                    var extractedTags = ExtractTitleAndSummary(outline);
                    title = extractedTags.Title;
                    summary = extractedTags.Summary;
                    break;
            }

            if (string.IsNullOrEmpty(title)) title = "未命名小说";
            if (string.IsNullOrEmpty(summary)) summary = "";

            // Check duplicate
            var duplicate = Projects.FirstOrDefault(p =>
                p.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
            {
                title = $"{title} {DateTime.Now:yyyyMMddHHmm}";
            }

            // Create project
            var project = new Project
            {
                Title = title,
                Description = summary,
                Genre = "网文"
            };

            project = await _fileProjectService.CreateProjectAsync(SelectedParentDirectory, project, outline);
            await _projectRepository.CreateAsync(project);

            // Split outline into documents
            if (!string.IsNullOrWhiteSpace(outline))
            {
                var sections = SplitOutlineBySections(outline);
                for (int i = 0; i < sections.Count; i++)
                {
                    var (sectionTitle, content) = sections[i];
                    var doc = new Document
                    {
                        ProjectId = project.Id,
                        Title = sectionTitle,
                        Type = DocumentType.Outline,
                        Content = content,
                        SortOrder = i
                    };
                    await _documentRepository.CreateAsync(doc);
                }

                await ExtractCharactersAsync(project, outline);
            }

            CreateDialogOpen = false;
            _projectContext.SetCurrentProject(project.Id);
            _navigationService.NavigateTo("Script", project.Id);
        }
        catch (Exception ex)
        {
            CreateStatusMessage = $"创建失败: {ex.Message}";
        }
        finally
        {
            IsCreating = false;
        }
    }

    private async Task<string?> GenerateOutlineFromInspirationAsync()
    {
        try
        {
            var apiKeyConfig = _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
            if (apiKeyConfig is null) return null;

            var provider = _aiProviderFactory.GetProviderForApiKey(apiKeyConfig);
            var messages = new List<AiChatMessage>
            {
                new()
                {
                    Role = "system",
                    Content = "你是一个专业的网文大纲规划助手。请将用户的创作灵感扩展为完整的故事大纲。\n" +
                              "大纲格式要求：\n" +
                              "# 书名\n" +
                              "> 一句话简介（20字以内）\n" +
                              "## 故事简介\n" +
                              "...\n" +
                              "## 主要角色\n" +
                              "...\n" +
                              "## 分章大纲\n" +
                              "### 第一章：标题\n" +
                              "...\n\n" +
                              "请用中文回复，使用 Markdown 格式。"
                },
                new()
                {
                    Role = "user",
                    Content = $"我的创作灵感：\n{InspirationText}\n\n请帮我扩展为完整的故事大纲。"
                }
            };

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages))
            {
                response.Append(chunk);
            }

            return response.Length > 0 ? response.ToString() : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GenerateOutlineFromTagsAsync()
    {
        try
        {
            var apiKeyConfig = _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
            if (apiKeyConfig is null) return null;

            var provider = _aiProviderFactory.GetProviderForApiKey(apiKeyConfig);
            var messages = new List<AiChatMessage>
            {
                new()
                {
                    Role = "system",
                    Content = "你是一个专业的网文大纲规划助手。请根据用户提供的小说类型和标签，生成一个完整的故事大纲。\n" +
                              "大纲格式要求：\n" +
                              "# 书名\n" +
                              "> 一句话简介（20字以内）\n" +
                              "## 故事简介\n" +
                              "...\n" +
                              "## 主要角色\n" +
                              "...\n" +
                              "## 分章大纲\n" +
                              "### 第一章：标题\n" +
                              "...\n\n" +
                              "请用中文回复，使用 Markdown 格式。"
                },
                new()
                {
                    Role = "user",
                    Content = $"类型：{NovelType}\n标签：{NovelTags}\n\n请生成一个完整的故事大纲。"
                }
            };

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages))
            {
                response.Append(chunk);
            }

            return response.Length > 0 ? response.ToString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static (string Title, string Summary) ExtractTitleAndSummary(string outline)
    {
        var title = "";
        var summary = "";

        var lines = outline.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Extract title from # heading
            if (string.IsNullOrEmpty(title) && trimmed.StartsWith("# ") && !trimmed.StartsWith("## "))
            {
                title = trimmed[2..].Trim();
                continue;
            }

            // Extract summary from > quote
            if (string.IsNullOrEmpty(summary) && trimmed.StartsWith("> ") && !trimmed.StartsWith(">> "))
            {
                summary = trimmed[2..].Trim();
                continue;
            }

            // Extract summary from first paragraph after title
            if (!string.IsNullOrEmpty(title) && string.IsNullOrEmpty(summary)
                && !string.IsNullOrWhiteSpace(trimmed)
                && !trimmed.StartsWith("#") && !trimmed.StartsWith(">") && !trimmed.StartsWith("-")
                && !trimmed.StartsWith("*") && trimmed != "---")
            {
                summary = trimmed.Length > 50 ? trimmed[..50] + "..." : trimmed;
                break;
            }
        }

        return (title, summary);
    }

    private async Task<(string Title, string Summary)> ExtractTitleAndSummaryViaAiAsync(string outline)
    {
        try
        {
            var apiKeyConfig = _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
            if (apiKeyConfig is null) return ("", "");

            var provider = _aiProviderFactory.GetProviderForApiKey(apiKeyConfig);
            var messages = new List<AiChatMessage>
            {
                new()
                {
                    Role = "system",
                    Content = "请从以下大纲中提取书名和简介，返回JSON格式：\n" +
                              "{\"title\": \"书名\", \"summary\": \"一句话简介\"}\n" +
                              "只返回JSON，不要有其他内容。"
                },
                new()
                {
                    Role = "user",
                    Content = outline
                }
            };

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages))
            {
                response.Append(chunk);
            }

            var json = response.ToString().Trim();
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                json = json[start..(end + 1)];
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return (result?.GetValueOrDefault("title") ?? "", result?.GetValueOrDefault("summary") ?? "");
        }
        catch
        {
            return ("", "");
        }
    }

    private static List<(string Title, string Content)> SplitOutlineBySections(string outline)
    {
        var sections = new List<(string Title, string Content)>();
        var lines = outline.Split('\n');
        var currentTitle = "故事大纲";
        var currentContent = new System.Text.StringBuilder();
        var firstSection = true;

        foreach (var line in lines)
        {
            if (line.StartsWith("## "))
            {
                if (currentContent.Length > 0)
                {
                    sections.Add((currentTitle, currentContent.ToString().Trim()));
                    currentContent.Clear();
                }
                currentTitle = line[3..].Trim();
                firstSection = false;
            }
            else if (line.StartsWith("# ") && firstSection)
            {
                // Skip the title line (already extracted)
                continue;
            }
            else if (line.StartsWith("> ") && firstSection && currentContent.Length == 0)
            {
                // Skip the summary line (already extracted)
                continue;
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentContent.Length > 0)
        {
            sections.Add((currentTitle, currentContent.ToString().Trim()));
        }

        if (sections.Count == 0)
        {
            sections.Add(("故事大纲", outline));
        }

        return sections;
    }

    private async Task ExtractCharactersAsync(Project project, string outline)
    {
        try
        {
            var apiKeyConfig = _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
            if (apiKeyConfig is null) return;

            var provider = _aiProviderFactory.GetProviderForApiKey(apiKeyConfig);
            var messages = new List<AiChatMessage>
            {
                new()
                {
                    Role = "system",
                    Content = "你是一个角色信息提取助手。请从用户提供的故事大纲中提取所有提到的角色信息。" +
                              "请严格按以下JSON格式返回，不要添加其他内容：\n" +
                              "[{\"Name\":\"角色名\",\"Alias\":\"别名\",\"Gender\":\"性别\",\"Role\":\"角色定位\",\"Appearance\":\"外貌\",\"Personality\":\"性格\",\"Motivation\":\"动机\",\"Weakness\":\"弱点\",\"Backstory\":\"背景\"}]\n" +
                              "如果某个字段信息不足，留空字符串即可。只返回JSON数组，不要有其他文字。"
                },
                new()
                {
                    Role = "user",
                    Content = outline
                }
            };

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages))
            {
                response.Append(chunk);
            }

            var json = response.ToString().Trim();
            var start = json.IndexOf('[');
            var end = json.LastIndexOf(']');
            if (start >= 0 && end > start)
            {
                json = json[start..(end + 1)];
            }

            var extracted = System.Text.Json.JsonSerializer.Deserialize<List<ExtractedCharacter>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (extracted is not null)
            {
                foreach (var c in extracted)
                {
                    var character = new Character
                    {
                        ProjectId = project.Id,
                        Name = c.Name ?? "",
                        Alias = c.Alias ?? "",
                        Gender = c.Gender ?? "",
                        Role = c.Role ?? "",
                        Appearance = c.Appearance ?? "",
                        Personality = c.Personality ?? "",
                        Motivation = c.Motivation ?? "",
                        Weakness = c.Weakness ?? "",
                        Backstory = c.Backstory ?? ""
                    };
                    await _characterRepository.CreateAsync(character);
                }
            }
        }
        catch
        {
            // Extraction failure doesn't affect project creation
        }
    }

    private class ExtractedCharacter
    {
        public string? Name { get; set; }
        public string? Alias { get; set; }
        public string? Gender { get; set; }
        public string? Role { get; set; }
        public string? Appearance { get; set; }
        public string? Personality { get; set; }
        public string? Motivation { get; set; }
        public string? Weakness { get; set; }
        public string? Backstory { get; set; }
    }

    [RelayCommand]
    private void CancelCreateProject()
    {
        CreateDialogOpen = false;
    }

    [RelayCommand]
    private void NavigateToFeature(string feature)
    {
        if (SelectedProject is null) return;

        _projectContext.SetCurrentProject(SelectedProject.Id);
        _navigationService.NavigateTo(feature, SelectedProject.Id);
    }

    [RelayCommand]
    private async Task UpdateProjectAsync()
    {
        if (SelectedProject is null) return;

        SelectedProject.Title = EditProjectTitle.Trim();
        SelectedProject.Description = EditProjectDescription.Trim();
        await _projectRepository.UpdateAsync(SelectedProject);

        var index = Projects.IndexOf(SelectedProject);
        if (index >= 0)
        {
            Projects[index] = SelectedProject;
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedProjectAsync()
    {
        if (SelectedProject is null) return;

        DeleteProjectFiles(SelectedProject);
        await _projectRepository.DeleteAsync(SelectedProject.Id);
        Projects.Remove(SelectedProject);
        SelectedProject = null;
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(Project project)
    {
        DeleteProjectFiles(project);
        await _projectRepository.DeleteAsync(project.Id);
        Projects.Remove(project);
    }

    private static void DeleteProjectFiles(Project project)
    {
        if (!string.IsNullOrEmpty(project.ProjectPath) && Directory.Exists(project.ProjectPath))
        {
            try
            {
                Directory.Delete(project.ProjectPath, recursive: true);
            }
            catch
            {
                // File deletion failure doesn't affect database deletion
            }
        }
    }
}
