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
    private readonly IProjectContext _projectContext;
    private readonly INavigationService _navigationService;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly ISettingsService _settingsService;

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

    public HomeViewModel(
        IProjectRepository projectRepository,
        IDocumentRepository documentRepository,
        IProjectContext projectContext,
        INavigationService navigationService,
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService)
    {
        _projectRepository = projectRepository;
        _documentRepository = documentRepository;
        _projectContext = projectContext;
        _navigationService = navigationService;
        _aiProviderFactory = aiProviderFactory;
        _settingsService = settingsService;
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

        var project = new Project
        {
            Title = trimmedTitle,
            Description = NewProjectDescription.Trim(),
            Genre = "网文"
        };

        await _projectRepository.CreateAsync(project);

        // 如果有灵感文本，用 AI 扩展为大纲
        if (!string.IsNullOrWhiteSpace(InspirationText))
        {
            var success = await ExpandInspirationAsync(project, InspirationText);
            if (!success)
            {
                CreateStatusMessage = "AI 生成大纲失败，请检查 API 设置或网络连接。项目已创建，可稍后在大纲规划页面手动生成。";
            }
        }

        IsCreating = false;

        if (string.IsNullOrEmpty(CreateStatusMessage))
        {
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
        else
        {
            await LoadProjectsAsync();
        }
    }

    private async Task<bool> ExpandInspirationAsync(Project project, string inspiration)
    {
        try
        {
            var apiKeyConfig = _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
            if (apiKeyConfig is null)
            {
                return false;
            }

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
                    Content = $"项目标题：{project.Title}\n类型：{project.Genre}\n\n我的创作灵感：\n{inspiration}\n\n请帮我扩展为完整的故事大纲。"
                }
            };

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages))
            {
                response.Append(chunk);
            }

            if (response.Length == 0) return false;

            // 保存为大纲文档
            var outline = new Document
            {
                ProjectId = project.Id,
                Title = "故事大纲",
                Type = DocumentType.Outline,
                Content = response.ToString(),
                SortOrder = 0
            };
            await _documentRepository.CreateAsync(outline);
            return true;
        }
        catch
        {
            return false;
        }
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

        await _projectRepository.DeleteAsync(SelectedProject.Id);
        Projects.Remove(SelectedProject);
        SelectedProject = null;
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(Project project)
    {
        await _projectRepository.DeleteAsync(project.Id);
        Projects.Remove(project);
    }
}
