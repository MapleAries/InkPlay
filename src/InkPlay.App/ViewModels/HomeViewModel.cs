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
        GoToOutlineAfterCreate = true;
        ShowCreateDialog = true;
    }

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProjectTitle)) return;

        IsCreating = true;

        var project = new Project
        {
            Title = NewProjectTitle.Trim(),
            Description = NewProjectDescription.Trim(),
            Genre = "网文"
        };

        await _projectRepository.CreateAsync(project);

        // 如果有灵感文本，用 AI 扩展为大纲
        if (!string.IsNullOrWhiteSpace(InspirationText))
        {
            await ExpandInspirationAsync(project, InspirationText);
        }

        ShowCreateDialog = false;
        IsCreating = false;

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

    private async Task ExpandInspirationAsync(Project project, string inspiration)
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
        }
        catch
        {
            // AI 扩展失败不影响项目创建
        }
    }

    [RelayCommand]
    private void CancelCreateProject()
    {
        ShowCreateDialog = false;
    }

    [RelayCommand]
    private void NavigateToFeature(string feature)
    {
        if (SelectedProject is null) return;

        _projectContext.SetCurrentProject(SelectedProject.Id);
        _navigationService.NavigateTo(feature, SelectedProject.Id);
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(Project project)
    {
        await _projectRepository.DeleteAsync(project.Id);
        Projects.Remove(project);
    }
}
