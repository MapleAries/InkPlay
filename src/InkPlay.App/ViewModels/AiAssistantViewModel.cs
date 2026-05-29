using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class AiAssistantViewModel : ViewModelBase
{
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly ISettingsService _settingsService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IProjectContext _projectContext;
    private readonly IProjectRepository _projectRepository;
    private CancellationTokenSource? _aiCts;
    private AiConversation? _currentConversation;
    private Project? _currentProject;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private string _aiResponse = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private WritingStyle _selectedStyle = WritingStyle.ContinueWriting;

    [ObservableProperty]
    private ObservableCollection<AiChatMessage> _messages = new();

    [ObservableProperty]
    private string _contextText = string.Empty;

    public AiAssistantViewModel(
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService,
        IConversationRepository conversationRepository,
        IProjectContext projectContext,
        IProjectRepository projectRepository)
    {
        _aiProviderFactory = aiProviderFactory;
        _settingsService = settingsService;
        _conversationRepository = conversationRepository;
        _projectContext = projectContext;
        _projectRepository = projectRepository;
    }

    public override async void NavigatedTo(object? parameter)
    {
        if (_projectContext.CurrentProjectId.HasValue)
        {
            _currentProject = await _projectRepository.GetByIdAsync(_projectContext.CurrentProjectId.Value);
            HasProject = _currentProject is not null;
        }
        else
        {
            HasProject = false;
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput)) return;

        var userMessage = UserInput;
        UserInput = string.Empty;

        var fullPrompt = BuildPrompt(userMessage);
        Messages.Add(new AiChatMessage { Role = "user", Content = userMessage });

        await SendToAiAsync(fullPrompt);
    }

    [RelayCommand]
    private async Task QuickActionAsync(string action)
    {
        var prompt = action switch
        {
            "continue" => "请根据以上内容继续写作，保持风格一致",
            "rewrite" => "请重写以下内容，使其更加生动有趣",
            "polish" => "请润色以下内容，提升文笔质量",
            "expand" => "请扩写以下内容，增加更多细节描写",
            "summarize" => "请缩写以下内容，保留核心信息",
            _ => action
        };

        if (!string.IsNullOrWhiteSpace(ContextText))
        {
            prompt = $"以下是参考内容：\n\n{ContextText}\n\n{prompt}";
        }

        Messages.Add(new AiChatMessage { Role = "user", Content = prompt });
        await SendToAiAsync(prompt);
    }

    private string BuildPrompt(string userMessage)
    {
        var stylePrefix = SelectedStyle switch
        {
            WritingStyle.ContinueWriting => "请继续写作：",
            WritingStyle.Rewrite => "请重写以下内容：",
            WritingStyle.Polish => "请润色以下内容：",
            WritingStyle.StyleTransform => "请转换以下内容的风格：",
            WritingStyle.Expand => "请扩写以下内容：",
            WritingStyle.Summarize => "请缩写以下内容：",
            WritingStyle.DialogueGenerate => "请为以下场景生成对话：",
            WritingStyle.OutlineGenerate => "请为以下内容生成大纲：",
            _ => ""
        };

        if (!string.IsNullOrWhiteSpace(ContextText))
        {
            return $"以下是参考内容：\n\n{ContextText}\n\n{stylePrefix}\n{userMessage}";
        }

        return $"{stylePrefix}\n{userMessage}";
    }

    private async Task SendToAiAsync(string prompt)
    {
        IsProcessing = true;
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
                Content = "你是一个专业的网文写作助手。你的任务是帮助用户进行网文/小说的章节创作。" +
                          "你应该根据用户的指令进行续写、重写、润色、扩写等操作，保持文风的一致性和连贯性，" +
                          "注重故事节奏、人物刻画和情节推进，提供引人入胜的章节内容。请用中文回复。"
            });

            if (_currentProject?.SystemPrompt is { Length: > 0 } systemPrompt)
            {
                messages.Add(new AiChatMessage { Role = "system", Content = systemPrompt });
            }

            messages.Add(new AiChatMessage { Role = "user", Content = prompt });

            var response = new StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages, _aiCts.Token))
            {
                response.Append(chunk);
                AiResponse = response.ToString();
            }

            Messages.Add(new AiChatMessage { Role = "assistant", Content = AiResponse });

            await SaveConversationAsync();
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
            IsProcessing = false;
        }
    }

    private async Task SaveConversationAsync()
    {
        if (Messages.Count == 0) return;

        if (_currentConversation is null)
        {
            _currentConversation = new AiConversation
            {
                Title = $"AI助手对话 - {DateTime.Now:yyyy-MM-dd HH:mm}"
            };
            await _conversationRepository.CreateAsync(_currentConversation);
        }

        _currentConversation.Messages = Messages.ToList();
        await _conversationRepository.UpdateAsync(_currentConversation);
    }

    [RelayCommand]
    private void CancelOperation()
    {
        _aiCts?.Cancel();
    }

    [RelayCommand]
    private async Task ClearChatAsync()
    {
        Messages.Clear();
        AiResponse = string.Empty;

        if (_currentConversation is not null)
        {
            await _conversationRepository.DeleteAsync(_currentConversation.Id);
            _currentConversation = null;
        }
    }

    [RelayCommand]
    private void CopyResponse()
    {
        if (!string.IsNullOrEmpty(AiResponse))
        {
            // WinUI 3 clipboard
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(AiResponse);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }
}
