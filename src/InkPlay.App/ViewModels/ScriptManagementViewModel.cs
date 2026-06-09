using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class ScriptManagementViewModel : ViewModelBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly ISettingsService _settingsService;
    private readonly IProjectContext _projectContext;
    private readonly NavigationService _navigationService;
    private CancellationTokenSource? _aiCts;

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private string _currentProjectTitle = string.Empty;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private ObservableCollection<Document> _chapters = new();

    [ObservableProperty]
    private Document? _selectedChapter;

    [ObservableProperty]
    private bool _isChapterSelected;

    [ObservableProperty]
    private string _chapterContent = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Character> _characters = new();

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private bool _isNotConverting = true;

    partial void OnIsConvertingChanged(bool value) => IsNotConverting = !value;

    [ObservableProperty]
    private string _conversionStatus = string.Empty;

    [ObservableProperty]
    private bool _isAiProcessing;

    [ObservableProperty]
    private string _aiResponse = string.Empty;

    [ObservableProperty]
    private string _aiUserInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AiChatMessage> _aiMessages = new();

    public ScriptManagementViewModel(
        IDocumentRepository documentRepository,
        IProjectRepository projectRepository,
        ICharacterRepository characterRepository,
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService,
        IProjectContext projectContext,
        NavigationService navigationService)
    {
        _documentRepository = documentRepository;
        _projectRepository = projectRepository;
        _characterRepository = characterRepository;
        _aiProviderFactory = aiProviderFactory;
        _settingsService = settingsService;
        _projectContext = projectContext;
        _navigationService = navigationService;
    }

    public override async void NavigatedTo(object? parameter)
    {
        try
        {
            var projectId = parameter as Guid? ?? _projectContext.CurrentProjectId;
            if (projectId.HasValue)
            {
                CurrentProject = await _projectRepository.GetByIdAsync(projectId.Value);
                HasProject = CurrentProject is not null;
                CurrentProjectTitle = CurrentProject?.Title ?? "";
                if (CurrentProject is not null)
                {
                    await LoadChaptersAsync();
                    await LoadCharactersAsync();
                }
            }
            else
            {
                HasProject = false;
                CurrentProjectTitle = "";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NavigatedTo failed: {ex.Message}");
        }
    }

    public override void NavigatedFrom()
    {
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = null;
    }

    [RelayCommand]
    private async Task LoadChaptersAsync()
    {
        if (CurrentProject is null) return;

        var docs = await _documentRepository.GetByProjectIdAsync(CurrentProject.Id);
        var chapterDocs = docs.Where(d => d.Type == DocumentType.Chapter)
                              .OrderBy(d => d.SortOrder)
                              .ToList();
        Chapters = new ObservableCollection<Document>(chapterDocs);
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        if (CurrentProject is null) return;

        var characters = await _characterRepository.GetByProjectIdAsync(CurrentProject.Id);
        Characters = new ObservableCollection<Character>(characters);
    }

    [RelayCommand]
    private void SelectChapter(Document? chapter)
    {
        if (chapter is null)
        {
            SelectedChapter = null;
            IsChapterSelected = false;
            ChapterContent = string.Empty;
            return;
        }

        SelectedChapter = chapter;
        IsChapterSelected = true;
        ChapterContent = chapter.Content;
    }

    [RelayCommand]
    private async Task ConvertToScriptAsync()
    {
        if (SelectedChapter is null || CurrentProject is null) return;

        IsConverting = true;
        ConversionStatus = "正在转换...";
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = new CancellationTokenSource();

        try
        {
            var apiKeyConfig = _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
            if (apiKeyConfig is null)
            {
                ConversionStatus = "请先在设置中配置文本生成 API Key";
                return;
            }

            var provider = _aiProviderFactory.GetProviderForApiKey(apiKeyConfig);

            var characterInfo = string.Join("\n", Characters.Select(c => $"- {c.Name}: {c.Role}, {c.Personality}"));

            var prompt = $@"请将以下章节内容转换为剧本格式。

角色信息：
{characterInfo}

章节内容：
{SelectedChapter.Content}

请按以下格式输出：
场景1: [场景标题]
地点: [地点]
时间: [时间]
[动作描述]

角色名: ""对话内容""
角色名: ""对话内容""

场景2: ...
";

            var messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = "你是一个专业的剧本改编助手。请将小说章节内容转换为标准剧本格式，包括场景标题、地点、时间、动作描述和对话。请用中文回复。" },
                new() { Role = "user", Content = prompt }
            };

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages, _aiCts?.Token ?? CancellationToken.None))
            {
                response.Append(chunk);
            }

            // Save as script document
            var script = new Document
            {
                ProjectId = CurrentProject.Id,
                Title = $"{SelectedChapter.Title} - 剧本",
                Type = DocumentType.Script,
                Content = response.ToString(),
                SortOrder = SelectedChapter.SortOrder
            };

            await _documentRepository.CreateAsync(script);

            ConversionStatus = $"已转换: {script.Title}";
            AiMessages.Add(new AiChatMessage { Role = "assistant", Content = $"已将「{SelectedChapter.Title}」转换为剧本" });
        }
        catch (OperationCanceledException)
        {
            ConversionStatus = "已取消";
        }
        catch (Exception ex)
        {
            ConversionStatus = $"转换失败: {ex.Message}";
        }
        finally
        {
            IsConverting = false;
        }
    }

    [RelayCommand]
    private async Task SendAiMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(AiUserInput)) return;

        var userMessage = AiUserInput;
        AiUserInput = string.Empty;

        IsAiProcessing = true;
        AiResponse = string.Empty;
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = new CancellationTokenSource();

        try
        {
            var apiKeyConfig = _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
            if (apiKeyConfig is null)
            {
                AiResponse = "请先在设置中配置文本生成 API Key";
                return;
            }
            var provider = _aiProviderFactory.GetProviderForApiKey(apiKeyConfig);

            var messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = "你是一个专业的剧本创作助手，帮助用户将小说内容改编为剧本。请用中文回复。" }
            };

            if (!string.IsNullOrEmpty(ChapterContent))
            {
                messages.Add(new AiChatMessage { Role = "system", Content = $"当前章节内容：\n{ChapterContent}" });
            }

            messages.Add(new AiChatMessage { Role = "user", Content = userMessage });
            AiMessages.Add(new AiChatMessage { Role = "user", Content = userMessage });

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages, _aiCts.Token))
            {
                response.Append(chunk);
                AiResponse = response.ToString();
            }

            AiMessages.Add(new AiChatMessage { Role = "assistant", Content = AiResponse });
        }
        catch (OperationCanceledException)
        {
            AiResponse += "\n[已取消]";
        }
        catch (Exception ex)
        {
            AiResponse = $"AI请求失败: {ex.Message}";
        }
        finally
        {
            IsAiProcessing = false;
        }
    }

    [RelayCommand]
    private void CancelAiOperation()
    {
        _aiCts?.Cancel();
    }

    [RelayCommand]
    private void ClearAiChat()
    {
        AiMessages.Clear();
        AiResponse = string.Empty;
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateTo("Home");
    }
}
