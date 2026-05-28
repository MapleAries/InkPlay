using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class ScriptViewModel : ViewModelBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly ISettingsService _settingsService;
    private readonly NavigationService _navigationService;
    private CancellationTokenSource? _aiCts;

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private ObservableCollection<Document> _episodes = new();

    [ObservableProperty]
    private Document? _currentEpisode;

    [ObservableProperty]
    private ObservableCollection<ScriptScene> _scenes = new();

    [ObservableProperty]
    private ScriptScene? _currentScene;

    [ObservableProperty]
    private ObservableCollection<Character> _characters = new();

    [ObservableProperty]
    private string _sceneHeading = string.Empty;

    [ObservableProperty]
    private string _sceneLocation = string.Empty;

    [ObservableProperty]
    private string _sceneTimeOfDay = string.Empty;

    [ObservableProperty]
    private string _sceneAction = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SceneDialogue> _sceneDialogues = new();

    [ObservableProperty]
    private bool _isAiProcessing;

    [ObservableProperty]
    private string _aiResponse = string.Empty;

    [ObservableProperty]
    private string _aiUserInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AiChatMessage> _aiMessages = new();

    public ScriptViewModel(
        IDocumentRepository documentRepository,
        IProjectRepository projectRepository,
        ICharacterRepository characterRepository,
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService,
        NavigationService navigationService)
    {
        _documentRepository = documentRepository;
        _projectRepository = projectRepository;
        _characterRepository = characterRepository;
        _aiProviderFactory = aiProviderFactory;
        _settingsService = settingsService;
        _navigationService = navigationService;
    }

    public override async void NavigatedTo(object? parameter)
    {
        if (parameter is Guid projectId)
        {
            CurrentProject = await _projectRepository.GetByIdAsync(projectId);
            if (CurrentProject is not null)
            {
                await LoadEpisodesAsync();
                await LoadCharactersAsync();
            }
        }
    }

    [RelayCommand]
    private async Task LoadEpisodesAsync()
    {
        if (CurrentProject is null) return;

        var docs = await _documentRepository.GetByProjectIdAsync(CurrentProject.Id);
        var scriptDocs = docs.Where(d => d.Type == DocumentType.Script)
                             .OrderBy(d => d.EpisodeNumber)
                             .ThenBy(d => d.SortOrder)
                             .ToList();
        Episodes = new ObservableCollection<Document>(scriptDocs);

        if (Episodes.Count > 0 && CurrentEpisode is null)
        {
            SelectEpisode(Episodes[0]);
        }
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        if (CurrentProject is null) return;

        var characters = await _characterRepository.GetByProjectIdAsync(CurrentProject.Id);
        Characters = new ObservableCollection<Character>(characters);
    }

    [RelayCommand]
    private async Task CreateEpisodeAsync()
    {
        if (CurrentProject is null) return;

        var episode = new Document
        {
            ProjectId = CurrentProject.Id,
            Title = $"第 {Episodes.Count + 1} 集",
            Type = DocumentType.Script,
            EpisodeNumber = Episodes.Count + 1,
            SortOrder = Episodes.Count
        };

        await _documentRepository.CreateAsync(episode);
        Episodes.Add(episode);
        SelectEpisode(episode);
    }

    [RelayCommand]
    private void SelectEpisode(Document? episode)
    {
        if (episode is null) return;

        CurrentEpisode = episode;
        Scenes = new ObservableCollection<ScriptScene>(episode.Scenes);

        if (Scenes.Count > 0 && CurrentScene is null)
        {
            SelectScene(Scenes[0]);
        }
        else if (Scenes.Count == 0)
        {
            ClearSceneFields();
        }
    }

    [RelayCommand]
    private async Task CreateSceneAsync()
    {
        if (CurrentEpisode is null) return;

        var scene = new ScriptScene
        {
            SceneHeading = $"场景 {Scenes.Count + 1}",
            Location = "待定",
            TimeOfDay = "日"
        };

        CurrentEpisode.Scenes.Add(scene);
        await _documentRepository.UpdateAsync(CurrentEpisode);

        Scenes.Add(scene);
        SelectScene(scene);
    }

    [RelayCommand]
    private void SelectScene(ScriptScene? scene)
    {
        if (scene is null) return;

        CurrentScene = scene;
        LoadSceneFields();
    }

    [RelayCommand]
    private async Task SaveSceneAsync()
    {
        if (CurrentScene is null || CurrentEpisode is null) return;

        CurrentScene.SceneHeading = SceneHeading;
        CurrentScene.Location = SceneLocation;
        CurrentScene.TimeOfDay = SceneTimeOfDay;
        CurrentScene.Action = SceneAction;
        CurrentScene.Dialogues = SceneDialogues.ToList();

        await _documentRepository.UpdateAsync(CurrentEpisode);

        // Refresh the list
        var index = Scenes.IndexOf(CurrentScene);
        if (index >= 0)
        {
            Scenes[index] = CurrentScene;
        }
    }

    [RelayCommand]
    private async Task DeleteSceneAsync()
    {
        if (CurrentScene is null || CurrentEpisode is null) return;

        CurrentEpisode.Scenes.Remove(CurrentScene);
        await _documentRepository.UpdateAsync(CurrentEpisode);

        Scenes.Remove(CurrentScene);
        CurrentScene = Scenes.FirstOrDefault();
        LoadSceneFields();
    }

    [RelayCommand]
    private void AddDialogueAsync()
    {
        SceneDialogues.Add(new SceneDialogue
        {
            CharacterName = "角色",
            Line = string.Empty
        });
    }

    [RelayCommand]
    private void RemoveDialogueAsync(SceneDialogue? dialogue)
    {
        if (dialogue is null) return;
        SceneDialogues.Remove(dialogue);
    }

    [RelayCommand]
    private async Task GenerateOutlineAsync()
    {
        var prompt = $"请为以下剧本生成分集大纲：\n标题：{CurrentProject?.Title}\n类型：{CurrentProject?.Genre}";
        await SendAiRequestAsync(prompt);
    }

    [RelayCommand]
    private async Task GenerateDialogueAsync()
    {
        var prompt = $"请为以下场景生成对话：\n场景：{SceneHeading}\n地点：{SceneLocation}\n时间：{SceneTimeOfDay}\n动作描述：{SceneAction}";
        await SendAiRequestAsync(prompt);
    }

    [RelayCommand]
    private async Task GenerateSceneDescriptionAsync()
    {
        var prompt = $"请为以下场景生成详细的动作描述：\n场景：{SceneHeading}\n地点：{SceneLocation}\n时间：{SceneTimeOfDay}";
        await SendAiRequestAsync(prompt);
    }

    [RelayCommand]
    private async Task SendAiMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(AiUserInput)) return;

        var userMessage = AiUserInput;
        AiUserInput = string.Empty;
        await SendAiRequestAsync(userMessage);
    }

    [RelayCommand]
    private void CancelAiOperation()
    {
        _aiCts?.Cancel();
    }

    private async Task SendAiRequestAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;

        IsAiProcessing = true;
        AiResponse = string.Empty;
        _aiCts?.Cancel();
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

            var messages = new List<AiChatMessage>();

            messages.Add(new AiChatMessage
            {
                Role = "system",
                Content = "你是一个专业的剧本创作助手。你的任务是帮助用户编写和管理剧本。" +
                          "你应该帮助生成分集大纲、场景描述、对话内容，遵循剧本格式规范（场景标题、动作描写、对话格式），" +
                          "确保对话自然、符合角色性格，考虑节奏、冲突和戏剧张力。请用中文回复。"
            });

            if (CurrentProject?.SystemPrompt is { Length: > 0 } systemPrompt)
            {
                messages.Add(new AiChatMessage { Role = "system", Content = systemPrompt });
            }

            messages.Add(new AiChatMessage { Role = "user", Content = prompt });

            AiMessages.Add(new AiChatMessage { Role = "user", Content = prompt });

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
    private void ApplyAiResponse()
    {
        if (!string.IsNullOrEmpty(AiResponse))
        {
            SceneAction += "\n" + AiResponse;
            AiResponse = string.Empty;
        }
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
        _navigationService.GoBack();
    }

    private void LoadSceneFields()
    {
        if (CurrentScene is null)
        {
            ClearSceneFields();
            return;
        }

        SceneHeading = CurrentScene.SceneHeading;
        SceneLocation = CurrentScene.Location;
        SceneTimeOfDay = CurrentScene.TimeOfDay;
        SceneAction = CurrentScene.Action;
        SceneDialogues = new ObservableCollection<SceneDialogue>(CurrentScene.Dialogues);
    }

    private void ClearSceneFields()
    {
        SceneHeading = string.Empty;
        SceneLocation = string.Empty;
        SceneTimeOfDay = string.Empty;
        SceneAction = string.Empty;
        SceneDialogues = new ObservableCollection<SceneDialogue>();
    }
}
