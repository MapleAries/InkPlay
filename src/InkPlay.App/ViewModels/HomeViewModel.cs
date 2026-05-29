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
    private string _newProjectTitle = string.Empty;

    [ObservableProperty]
    private string _newProjectDescription = string.Empty;

    [ObservableProperty]
    private string _inspirationText = string.Empty;

    [ObservableProperty]
    private bool _goToOutlineAfterCreate = true;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private bool _createDialogOpen;

    [ObservableProperty]
    private string _editProjectTitle = string.Empty;

    [ObservableProperty]
    private string _editProjectDescription = string.Empty;

    [ObservableProperty]
    private string _createStatusMessage = string.Empty;

    [ObservableProperty]
    private string _selectedParentDirectory = string.Empty;

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

    [RelayCommand]
    private void ShowCreateProjectDialog()
    {
        NewProjectTitle = string.Empty;
        NewProjectDescription = string.Empty;
        InspirationText = string.Empty;
        CreateStatusMessage = string.Empty;
        GoToOutlineAfterCreate = true;
        CreateDialogOpen = true;
    }

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProjectTitle)) return;
        if (string.IsNullOrWhiteSpace(SelectedParentDirectory))
        {
            CreateStatusMessage = "请选择项目保存目录";
            return;
        }

        CreateStatusMessage = string.Empty;

        // 检查重名
        var trimmedTitle = NewProjectTitle.Trim();
        var duplicate = Projects.FirstOrDefault(p =>
            p.Title.Equals(trimmedTitle, StringComparison.OrdinalIgnoreCase));
        if (duplicate is not null)
        {
            CreateStatusMessage = $"项目 \"{trimmedTitle}\" 已存在，请使用其他名称";
            return;
        }

        IsCreating = true;

        string? generatedOutline = null;

        // 如果有灵感文本，先用 AI 生成大纲
        if (!string.IsNullOrWhiteSpace(InspirationText))
        {
            generatedOutline = await GenerateOutlineAsync(InspirationText, trimmedTitle);
            if (generatedOutline is null)
            {
                CreateStatusMessage = "AI 生成大纲失败，请检查 API 设置或网络连接";
                IsCreating = false;
                return;
            }
        }

        // AI 成功或无灵感，创建项目文件和索引
        var project = new Project
        {
            Title = trimmedTitle,
            Description = NewProjectDescription.Trim(),
            Genre = "网文"
        };

        // 创建项目文件目录
        project = await _fileProjectService.CreateProjectAsync(SelectedParentDirectory, project, generatedOutline);

        // 保存到 LiteDB 索引
        await _projectRepository.CreateAsync(project);

        // 拆分大纲并保存为多个文档
        if (generatedOutline is not null)
        {
            var sections = SplitOutlineBySections(generatedOutline);
            for (int i = 0; i < sections.Count; i++)
            {
                var (title, content) = sections[i];
                var outline = new Document
                {
                    ProjectId = project.Id,
                    Title = title,
                    Type = DocumentType.Outline,
                    Content = content,
                    SortOrder = i
                };
                await _documentRepository.CreateAsync(outline);
            }

            // 从大纲中提取角色信息并保存
            await ExtractCharactersAsync(project, generatedOutline);
        }

        IsCreating = false;
        CreateDialogOpen = false;

        if (GoToOutlineAfterCreate)
        {
            _projectContext.SetCurrentProject(project.Id);
            _navigationService.NavigateTo("Script", project.Id);
        }
        else
        {
            await LoadProjectsAsync();
        }
    }

    private async Task<string?> GenerateOutlineAsync(string inspiration, string projectTitle)
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
                    Content = "你是一个专业的网文大纲规划助手。用户会给你一些创作灵感或初步想法，你需要将其扩展为一个完整的故事大纲。" +
                              "大纲应包含：故事简介、主要角色设定、世界观设定、分卷/分章大纲（至少3-5章的标题和简要内容）。" +
                              "请用中文回复，使用 Markdown 格式。"
                },
                new()
                {
                    Role = "user",
                    Content = $"项目标题：{projectTitle}\n\n我的创作灵感：\n{inspiration}\n\n请帮我扩展为完整的故事大纲。"
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

    private static List<(string Title, string Content)> SplitOutlineBySections(string outline)
    {
        var sections = new List<(string Title, string Content)>();
        var lines = outline.Split('\n');
        var currentTitle = "故事大纲";
        var currentContent = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("## "))
            {
                // 保存上一个 section
                if (currentContent.Length > 0)
                {
                    sections.Add((currentTitle, currentContent.ToString().Trim()));
                    currentContent.Clear();
                }
                currentTitle = line[3..].Trim();
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }

        // 保存最后一个 section
        if (currentContent.Length > 0)
        {
            sections.Add((currentTitle, currentContent.ToString().Trim()));
        }

        // 如果没有拆分出多个 section，返回整体
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
            // 提取JSON部分（可能被```包裹）
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
            // 提取失败不影响项目创建
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

        // Refresh the list
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
                // 文件删除失败不影响数据库删除
            }
        }
    }
}
