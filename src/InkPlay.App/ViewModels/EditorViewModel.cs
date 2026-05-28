using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly ISettingsService _settingsService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IProjectContext _projectContext;
    private readonly NavigationService _navigationService;
    private CancellationTokenSource? _aiCts;
    private AiConversation? _currentConversation;

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private ObservableCollection<Document> _documents = new();

    [ObservableProperty]
    private Document? _currentDocument;

    [ObservableProperty]
    private string _documentContent = string.Empty;

    [ObservableProperty]
    private bool _isAiProcessing;

    [ObservableProperty]
    private string _aiResponse = string.Empty;

    [ObservableProperty]
    private string _aiUserInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AiChatMessage> _aiMessages = new();

    public EditorViewModel(
        IDocumentRepository documentRepository,
        IProjectRepository projectRepository,
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService,
        IConversationRepository conversationRepository,
        IProjectContext projectContext,
        NavigationService navigationService)
    {
        _documentRepository = documentRepository;
        _projectRepository = projectRepository;
        _aiProviderFactory = aiProviderFactory;
        _settingsService = settingsService;
        _conversationRepository = conversationRepository;
        _projectContext = projectContext;
        _navigationService = navigationService;
    }

    public override async void NavigatedTo(object? parameter)
    {
        if (parameter is Guid projectId)
        {
            _projectContext.SetCurrentProject(projectId);
            CurrentProject = await _projectRepository.GetByIdAsync(projectId);
            if (CurrentProject is not null)
            {
                await LoadDocumentsAsync();
                await LoadConversationAsync();
            }
        }
    }

    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        if (CurrentProject is null) return;

        var docs = await _documentRepository.GetByProjectIdAsync(CurrentProject.Id);
        Documents = new ObservableCollection<Document>(docs);

        if (Documents.Count > 0 && CurrentDocument is null)
        {
            SelectDocument(Documents[0]);
        }
    }

    private async Task LoadConversationAsync()
    {
        if (CurrentProject is null) return;

        var conversations = await _conversationRepository.GetByProjectIdAsync(CurrentProject.Id);
        _currentConversation = conversations.FirstOrDefault(c => c.DocumentId == CurrentDocument?.Id);

        if (_currentConversation is not null)
        {
            AiMessages = new ObservableCollection<AiChatMessage>(_currentConversation.Messages);
        }
    }

    private async Task SaveConversationAsync()
    {
        if (CurrentProject is null || AiMessages.Count == 0) return;

        if (_currentConversation is null)
        {
            _currentConversation = new AiConversation
            {
                ProjectId = CurrentProject.Id,
                DocumentId = CurrentDocument?.Id,
                Title = $"编辑器对话 - {DateTime.Now:yyyy-MM-dd HH:mm}"
            };
            await _conversationRepository.CreateAsync(_currentConversation);
        }

        _currentConversation.Messages = AiMessages.ToList();
        await _conversationRepository.UpdateAsync(_currentConversation);
    }

    [RelayCommand]
    private async Task CreateDocumentAsync()
    {
        if (CurrentProject is null) return;

        var doc = new Document
        {
            ProjectId = CurrentProject.Id,
            Title = $"新文档 {Documents.Count + 1}",
            SortOrder = Documents.Count
        };

        await _documentRepository.CreateAsync(doc);
        Documents.Add(doc);
        SelectDocument(doc);
    }

    [RelayCommand]
    private async Task SaveDocumentAsync()
    {
        if (CurrentDocument is null) return;

        CurrentDocument.Content = DocumentContent;
        await _documentRepository.UpdateAsync(CurrentDocument);
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(Document? document)
    {
        if (document is null) return;

        await _documentRepository.DeleteAsync(document.Id);
        Documents.Remove(document);

        if (CurrentDocument?.Id == document.Id)
        {
            CurrentDocument = Documents.FirstOrDefault();
            DocumentContent = CurrentDocument?.Content ?? string.Empty;
        }
    }

    [RelayCommand]
    private void SelectDocument(Document? document)
    {
        if (document is null) return;

        // Save current document before switching
        if (CurrentDocument is not null)
        {
            CurrentDocument.Content = DocumentContent;
        }

        CurrentDocument = document;
        DocumentContent = document.Content;
    }

    [RelayCommand]
    private async Task AiContinueWritingAsync()
    {
        await SendAiRequestAsync("请根据以上内容继续写作，保持风格一致：");
    }

    [RelayCommand]
    private async Task AiRewriteAsync()
    {
        await SendAiRequestAsync("请重写以下内容，使其更加生动有趣：");
    }

    [RelayCommand]
    private async Task AiPolishAsync()
    {
        await SendAiRequestAsync("请润色以下内容，提升文笔质量：");
    }

    [RelayCommand]
    private async Task AiExpandAsync()
    {
        await SendAiRequestAsync("请扩写以下内容，增加更多细节描写：");
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
        if (string.IsNullOrWhiteSpace(DocumentContent) && string.IsNullOrWhiteSpace(prompt)) return;

        IsAiProcessing = true;
        AiResponse = string.Empty;
        _aiCts?.Cancel();
        _aiCts = new CancellationTokenSource();

        try
        {
            var providerId = _settingsService.GetDefaultAiProviderId();
            var config = _settingsService.GetAiProviderConfig(providerId);
            var provider = _aiProviderFactory.GetProvider(providerId);

            var messages = new List<AiChatMessage>();

            if (CurrentProject?.SystemPrompt is { Length: > 0 } systemPrompt)
            {
                messages.Add(new AiChatMessage { Role = "system", Content = systemPrompt });
            }

            if (!string.IsNullOrWhiteSpace(DocumentContent))
            {
                messages.Add(new AiChatMessage
                {
                    Role = "user",
                    Content = $"以下是当前文档内容：\n\n{DocumentContent}\n\n{prompt}"
                });
            }
            else
            {
                messages.Add(new AiChatMessage { Role = "user", Content = prompt });
            }

            AiMessages.Add(new AiChatMessage { Role = "user", Content = prompt });

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(config, messages, _aiCts.Token))
            {
                response.Append(chunk);
                AiResponse = response.ToString();
            }

            AiMessages.Add(new AiChatMessage { Role = "assistant", Content = AiResponse });

            // Save conversation after each AI response
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
            IsAiProcessing = false;
        }
    }

    [RelayCommand]
    private void ApplyAiResponse()
    {
        if (!string.IsNullOrEmpty(AiResponse))
        {
            DocumentContent += "\n" + AiResponse;
            AiResponse = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ClearAiChatAsync()
    {
        AiMessages.Clear();
        AiResponse = string.Empty;

        if (_currentConversation is not null)
        {
            await _conversationRepository.DeleteAsync(_currentConversation.Id);
            _currentConversation = null;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
