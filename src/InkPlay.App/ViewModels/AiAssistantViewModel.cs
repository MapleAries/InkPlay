using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class AiAssistantViewModel : ViewModelBase
{
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly ISettingsService _settingsService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IExportService _exportService;
    private readonly IProjectContext _projectContext;
    private readonly IProjectRepository _projectRepository;
    private readonly IOrchestrator _orchestrator;
    private readonly ICharacterRepository _characterRepository;
    private CancellationTokenSource? _aiCts;
    private AiConversation? _currentConversation;
    private Project? _currentProject;
    private CancellationTokenSource? _autoSaveCts;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string _currentProjectTitle = string.Empty;

    // Chapter management
    [ObservableProperty]
    private ObservableCollection<Document> _chapters = new();

    [ObservableProperty]
    private Document? _currentChapter;

    [ObservableProperty]
    private bool _isChapterSelected;

    [ObservableProperty]
    private bool _hasChapterContent;

    [ObservableProperty]
    private string _chapterTitle = string.Empty;

    [ObservableProperty]
    private string _chapterContent = string.Empty;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private int _wordCountTarget = 3000;

    [ObservableProperty]
    private string _saveStatus = "未保存";

    // AI chat
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

    // Model switching
    [ObservableProperty]
    private ObservableCollection<ApiKeyConfig> _availableModels = new();

    [ObservableProperty]
    private ApiKeyConfig? _selectedModel;

    // Pipeline state
    [ObservableProperty]
    private bool _isAutoWriting;

    [ObservableProperty]
    private string _agentProgressText = string.Empty;

    [ObservableProperty]
    private string _chapterProgressText = string.Empty;

    [ObservableProperty]
    private string _revisionProgressText = string.Empty;

    [ObservableProperty]
    private bool _isRevising;

    [ObservableProperty]
    private string _currentAgentName = string.Empty;

    [ObservableProperty]
    private int _pipelineStep;

    [ObservableProperty]
    private int _pipelineTotalSteps;

    [ObservableProperty]
    private string _pipelineStreamingContent = string.Empty;

    [ObservableProperty]
    private int _totalChapters;

    [ObservableProperty]
    private int _completedChapters;

    [ObservableProperty]
    private string _currentWritingChapter = string.Empty;

    // Cost estimation
    [ObservableProperty]
    private bool _showCostEstimate;

    [ObservableProperty]
    private string _costEstimateMessage = string.Empty;

    // Batch writing settings
    [ObservableProperty]
    private bool _showBatchDialog;

    [ObservableProperty]
    private int _batchChapterCount = 5;

    [ObservableProperty]
    private int _batchWordCountPerChapter = 3000;

    public AiAssistantViewModel(
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService,
        IDocumentRepository documentRepository,
        IConversationRepository conversationRepository,
        IExportService exportService,
        IProjectContext projectContext,
        IProjectRepository projectRepository,
        IOrchestrator orchestrator,
        ICharacterRepository characterRepository)
    {
        _aiProviderFactory = aiProviderFactory;
        _settingsService = settingsService;
        _documentRepository = documentRepository;
        _conversationRepository = conversationRepository;
        _exportService = exportService;
        _projectContext = projectContext;
        _projectRepository = projectRepository;
        _orchestrator = orchestrator;
        _characterRepository = characterRepository;
    }

    public override async void NavigatedTo(object? parameter)
    {
        try
        {
            var projectId = parameter as Guid? ?? _projectContext.CurrentProjectId;
            if (projectId.HasValue)
            {
                _currentProject = await _projectRepository.GetByIdAsync(projectId.Value);
                HasProject = _currentProject is not null;
                CurrentProjectTitle = _currentProject?.Title ?? "";

                if (_currentProject is not null)
                {
                    await LoadChaptersAsync();
                    LoadAvailableModels();
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

    private async Task DebounceSaveAsync()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
        _autoSaveCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(2000, _autoSaveCts.Token);
            await SaveChapterSilentAsync();
        }
        catch (OperationCanceledException) { }
    }

    private void LoadAvailableModels()
    {
        var keys = _settingsService.GetApiKeys(ApiKeyCategory.Text);
        AvailableModels = new ObservableCollection<ApiKeyConfig>(keys);

        // Select default or project preferred
        if (_currentProject?.PreferredAiProvider is { Length: > 0 })
        {
            SelectedModel = keys.FirstOrDefault(k =>
                k.BaseUrl.Contains(_currentProject.PreferredAiProvider, StringComparison.OrdinalIgnoreCase))
                ?? keys.FirstOrDefault(k => k.IsDefault)
                ?? keys.FirstOrDefault();
        }
        else
        {
            SelectedModel = keys.FirstOrDefault(k => k.IsDefault) ?? keys.FirstOrDefault();
        }

    }

    // --- Chapter Management ---

    [RelayCommand]
    private async Task LoadChaptersAsync()
    {
        if (_currentProject is null) return;

        var chapterDocs = await _documentRepository.GetByProjectIdAndTypeAsync(_currentProject.Id, DocumentType.Chapter);
        Chapters.Clear();
        foreach (var chapter in chapterDocs)
        {
            Chapters.Add(chapter);
        }
        CurrentChapter = null;
        IsChapterSelected = false;
        ChapterContent = string.Empty;
        WordCount = 0;
    }

    [RelayCommand]
    private async Task CreateChapterAsync()
    {
        if (_currentProject is null) return;

        var chapterCount = Chapters.Count(c => c.Title != "目录");

        var chapter = new Document
        {
            ProjectId = _currentProject.Id,
            Title = $"第 {chapterCount + 1} 章",
            Type = DocumentType.Chapter,
            SortOrder = Chapters.Count
        };

        await _documentRepository.CreateAsync(chapter);
        Chapters.Add(chapter);
        SelectChapter(chapter);
    }

    [RelayCommand]
    private void SelectChapter(Document? chapter)
    {
        if (chapter is null)
        {
            CurrentChapter = null;
            IsChapterSelected = false;
            HasChapterContent = false;
            ChapterTitle = string.Empty;
            ChapterContent = string.Empty;
            WordCount = 0;
            SaveStatus = "未保存";
            return;
        }

        CurrentChapter = chapter;
        IsChapterSelected = true;
        ChapterTitle = chapter.Title;
        ChapterContent = ExtractContent(chapter.Content);
        HasChapterContent = !string.IsNullOrWhiteSpace(ChapterContent);
        WordCount = chapter.WordCount;
        SaveStatus = "已保存";

        // Load context for AI
        ContextText = chapter.Content;
    }

    private static string ExtractContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        // Remove leading # title if present
        var lines = content.Split('\n');
        if (lines.Length > 0 && lines[0].StartsWith("# "))
        {
            return string.Join('\n', lines.Skip(1)).TrimStart('\n', '\r');
        }
        return content;
    }

    private static string ExtractTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        var lines = content.Split('\n');
        if (lines.Length > 0 && lines[0].StartsWith("# "))
        {
            return lines[0][2..].Trim();
        }
        return string.Empty;
    }

    private static string CombineTitleAndContent(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title)) return content;
        return $"# {title}\n\n{content}";
    }

    [RelayCommand]
    private async Task SaveChapterAsync()
    {
        if (CurrentChapter is null) return;

        SaveStatus = "保存中...";
        CurrentChapter.Title = ChapterTitle;
        CurrentChapter.Content = CombineTitleAndContent(ChapterTitle, ChapterContent);
        await _documentRepository.UpdateAsync(CurrentChapter, Constants.ChangeSource.ManualEdit, "手动保存");
        WordCount = CurrentChapter.WordCount;
        SaveStatus = "已保存";
    }

    private async Task SaveChapterSilentAsync()
    {
        if (CurrentChapter is null) return;

        CurrentChapter.Title = ChapterTitle;
        CurrentChapter.Content = CombineTitleAndContent(ChapterTitle, ChapterContent);
        await _documentRepository.UpdateAsync(CurrentChapter, Constants.ChangeSource.AutoSave, "自动保存");
        WordCount = CurrentChapter.WordCount;
        SaveStatus = "已保存";
    }

    [RelayCommand]
    private async Task DeleteChapterAsync()
    {
        if (CurrentChapter is null) return;

        await _documentRepository.DeleteAsync(CurrentChapter.Id);
        Chapters.Remove(CurrentChapter);
        CurrentChapter = null;
        IsChapterSelected = false;
        ChapterContent = string.Empty;
        WordCount = 0;
    }

    public void OnContentChanged()
    {
        if (CurrentChapter is null) return;

        SaveStatus = "未保存";
        WordCount = CalculateWordCount(ChapterContent);
        HasChapterContent = !string.IsNullOrWhiteSpace(ChapterContent);
        _ = DebounceSaveAsync();
    }

    private static int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        var count = 0;
        var inWord = false;
        foreach (var c in content)
        {
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                count++;
                inWord = false;
            }
            else if (char.IsLetterOrDigit(c))
            {
                if (!inWord)
                {
                    count++;
                    inWord = true;
                }
            }
            else
            {
                inWord = false;
            }
        }
        return count;
    }

    // --- AI Chat ---

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput)) return;

        var userMessage = UserInput;
        UserInput = string.Empty;

        // Add chapter content as context if available
        var fullPrompt = userMessage;
        if (!string.IsNullOrWhiteSpace(ChapterContent))
        {
            fullPrompt = $"以下是当前章节内容：\n\n{ChapterContent}\n\n{userMessage}";
        }

        Messages.Add(new AiChatMessage { Role = "user", Content = userMessage });
        await SendToAiAsync(fullPrompt);
    }

    private async Task SendToAiAsync(string prompt)
    {
        IsProcessing = true;
        AiResponse = string.Empty;
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = new CancellationTokenSource();

        try
        {
            var apiKeyConfig = SelectedModel ?? _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
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
        if (Messages.Count == 0 || _currentProject is null) return;

        if (_currentConversation is null)
        {
            _currentConversation = new AiConversation
            {
                ProjectId = _currentProject.Id,
                Title = $"写作对话 - {DateTime.Now:yyyy-MM-dd HH:mm}"
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
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(AiResponse);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    [RelayCommand]
    private void ApplyToEditor()
    {
        if (!string.IsNullOrEmpty(AiResponse))
        {
            ChapterContent = AiResponse;
            AiResponse = string.Empty;
            OnContentChanged();
        }
    }

    // --- Auto Write Pipeline ---

    [RelayCommand]
    private async Task EstimateCostAsync()
    {
        if (_currentProject is null || CurrentChapter is null) return;

        var allDocs = await _documentRepository.GetByProjectIdAsync(_currentProject.Id);
        var characters = await _characterRepository.GetByProjectIdAsync(_currentProject.Id);
        var outlines = allDocs.Where(d => d.Type == DocumentType.Outline).ToList();
        var chapters = allDocs.Where(d => d.Type == DocumentType.Chapter && d.Id != CurrentChapter.Id)
                              .OrderBy(d => d.SortOrder)
                              .ToList();

        var agentContext = new AgentContext
        {
            Project = _currentProject,
            Characters = characters,
            Outlines = outlines,
            Chapters = chapters,
            CurrentDocument = CurrentChapter,
            TargetWordCount = WordCountTarget,
            UserRequest = $"请为章节「{ChapterTitle}」生成完整内容"
        };

        var estimate = _orchestrator.EstimateCost(agentContext);
        CostEstimateMessage = $"预估消耗:\n" +
                              $"- 输入 Token: ~{estimate.EstimatedInputTokens:N0}\n" +
                              $"- 输出 Token: ~{estimate.EstimatedOutputTokens:N0}\n" +
                              $"- 预估费用: ~${estimate.EstimatedCostUsd:F4}";
        ShowCostEstimate = true;
    }

    [RelayCommand]
    private void CloseCostEstimate()
    {
        ShowCostEstimate = false;
    }

    [RelayCommand]
    private async Task AutoWriteChapterAsync()
    {
        if (_currentProject is null || CurrentChapter is null) return;

        IsAutoWriting = true;
        AgentProgressText = "准备中...";
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = new CancellationTokenSource();

        try
        {
            // Build AgentContext from current project state
            var allDocs = await _documentRepository.GetByProjectIdAsync(_currentProject.Id);
            var characters = await _characterRepository.GetByProjectIdAsync(_currentProject.Id);
            var outlines = allDocs.Where(d => d.Type == DocumentType.Outline).ToList();
            var chapters = allDocs.Where(d => d.Type == DocumentType.Chapter && d.Id != CurrentChapter.Id)
                                  .OrderBy(d => d.SortOrder)
                                  .ToList();

            var agentContext = new AgentContext
            {
                Project = _currentProject,
                Characters = characters,
                Outlines = outlines,
                Chapters = chapters,
                CurrentDocument = CurrentChapter,
                TargetWordCount = WordCountTarget,
                UserRequest = $"请为章节「{ChapterTitle}」生成完整内容"
            };

            var progress = new Progress<PipelineProgress>(p =>
            {
                PipelineStep = p.StepNumber;
                PipelineTotalSteps = p.TotalSteps;
                CurrentAgentName = p.AgentName;

                // Agent progress
                if (!string.IsNullOrEmpty(p.StatusMessage))
                {
                    AgentProgressText = p.StatusMessage;
                }
                else
                {
                    AgentProgressText = p.Status switch
                    {
                        "running" => $"{p.AgentName} 正在工作...",
                        "completed" => $"{p.AgentName} 完成",
                        "failed" => $"{p.AgentName} 失败",
                        _ => p.Status
                    };
                }

                // Revision progress - set when revision starts, clear when audit passes
                if (p.Status == "revision")
                {
                    IsRevising = true;
                    RevisionProgressText = $"审计未通过，正在返工（第 {p.RevisionRound} 轮）";
                }
                else if (p.Status == "completed" && p.AgentType == AgentType.Data)
                {
                    // Data agent runs after audit passes - revision loop is done
                    IsRevising = false;
                    RevisionProgressText = "";
                }
            });

            var result = await _orchestrator.AutoWriteChapterAsync(agentContext, progress, _aiCts.Token);

            if (result.Success)
            {
                // Extract title from AI result if present
                var aiTitle = ExtractTitle(result.Content);
                if (!string.IsNullOrWhiteSpace(aiTitle))
                {
                    ChapterTitle = aiTitle;
                }

                ChapterContent = ExtractContent(result.Content);
                HasChapterContent = !string.IsNullOrWhiteSpace(ChapterContent);
                OnContentChanged();
                AgentProgressText = "写作完成！";
            }
            else
            {
                AgentProgressText = $"写作失败: {result.Content}";
            }
            IsRevising = false;
            RevisionProgressText = "";
        }
        catch (OperationCanceledException)
        {
            AgentProgressText = "已取消";
            RevisionProgressText = "";
        }
        catch (Exception ex)
        {
            AgentProgressText = $"错误: {ex.Message}";
            RevisionProgressText = "";
        }
        finally
        {
            IsAutoWriting = false;
            IsRevising = false;
        }
    }

    [RelayCommand]
    private void OpenBatchDialog()
    {
        if (_currentProject is null) return;
        ShowBatchDialog = true;
    }

    [RelayCommand]
    private void CloseBatchDialog()
    {
        ShowBatchDialog = false;
    }

    [RelayCommand]
    private async Task ConfirmBatchWriteAsync()
    {
        if (_currentProject is null || BatchChapterCount <= 0) return;

        ShowBatchDialog = false;
        IsAutoWriting = true;
        AgentProgressText = $"准备批量写作 {BatchChapterCount} 个章节...";
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = new CancellationTokenSource();

        try
        {
            // Create new chapters
            var newChapters = new List<Document>();
            var existingChapterCount = Chapters.Count(c => c.Title != "目录");

            for (int i = 0; i < BatchChapterCount; i++)
            {
                var chapter = new Document
                {
                    ProjectId = _currentProject.Id,
                    Title = $"第 {existingChapterCount + i + 1} 章",
                    Type = DocumentType.Chapter,
                    SortOrder = existingChapterCount + i
                };
                await _documentRepository.CreateAsync(chapter);
                newChapters.Add(chapter);
                Chapters.Add(chapter);
            }

            // Load context
            var allDocs = await _documentRepository.GetByProjectIdAsync(_currentProject.Id);
            var characters = await _characterRepository.GetByProjectIdAsync(_currentProject.Id);
            var outlines = allDocs.Where(d => d.Type == DocumentType.Outline).ToList();
            var existingChapters = allDocs.Where(d => d.Type == DocumentType.Chapter && d.Id != newChapters[0].Id && d.WordCount > 0)
                                          .OrderBy(d => d.SortOrder)
                                          .ToList();

            var baseContext = new AgentContext
            {
                Project = _currentProject,
                Characters = characters,
                Outlines = outlines,
                Chapters = existingChapters,
                TargetWordCount = BatchWordCountPerChapter
            };

            var progress = new Progress<PipelineProgress>(p =>
            {
                PipelineStep = p.StepNumber;
                PipelineTotalSteps = p.TotalSteps;
                CurrentAgentName = p.AgentName;

                // Chapter progress (batch writing) - when TotalChapters is set
                if (p.TotalChapters > 0)
                {
                    TotalChapters = p.TotalChapters;
                    CompletedChapters = p.CompletedChapters;
                    // Use StatusMessage for chapter progress if it contains chapter info
                    if (!string.IsNullOrEmpty(p.StatusMessage) && p.StatusMessage.Contains("「"))
                    {
                        ChapterProgressText = p.StatusMessage;
                    }
                }

                // Agent progress - use AgentName for agent-specific info
                AgentProgressText = p.Status switch
                {
                    "running" => $"{p.AgentName} 正在工作...",
                    "completed" => $"{p.AgentName} 完成",
                    "failed" => $"{p.AgentName} 失败",
                    "revision" => $"审计未通过，正在返工（第 {p.RevisionRound} 轮）",
                    _ => p.AgentName
                };

                // Revision progress - set when revision starts, clear when audit passes
                if (p.Status == "revision")
                {
                    IsRevising = true;
                    RevisionProgressText = $"审计未通过，正在返工（第 {p.RevisionRound} 轮）";
                }
                else if (p.Status == "completed" && p.AgentType == AgentType.Data)
                {
                    // Data agent runs after audit passes - revision loop is done
                    IsRevising = false;
                    RevisionProgressText = "";
                }
            });

            int completed = 0;
            await foreach (var result in _orchestrator.AutoWriteBatchAsync(newChapters, baseContext, progress, _aiCts.Token))
            {
                if (result.Success)
                {
                    completed++;
                    // Save the completed chapter
                    var chapter = newChapters[completed - 1];
                    chapter.Content = result.Content;
                    await _documentRepository.UpdateAsync(chapter, Constants.ChangeSource.AiGenerate, $"批量写作第 {completed} 章");
                    CompletedChapters = completed;
                }
            }

            // Reload chapters
            await LoadChaptersAsync();
            AgentProgressText = $"批量写作完成！共完成 {completed} 个章节";
            ChapterProgressText = "";
            RevisionProgressText = "";
        }
        catch (OperationCanceledException)
        {
            AgentProgressText = "批量写作已取消";
            ChapterProgressText = "";
        }
        catch (Exception ex)
        {
            AgentProgressText = $"批量写作错误: {ex.Message}";
            ChapterProgressText = "";
        }
        finally
        {
            IsAutoWriting = false;
            IsRevising = false;
        }
    }

    // --- Export ---

    [ObservableProperty]
    private bool _showExportDialog;

    [ObservableProperty]
    private string _exportDialogTitle = string.Empty;

    [ObservableProperty]
    private string _exportDialogMessage = string.Empty;

    [RelayCommand]
    private async Task ExportToMarkdownAsync()
    {
        if (_currentProject is null) return;

        try
        {
            var markdown = await _exportService.ExportProjectToMarkdownAsync(_currentProject);

            if (!string.IsNullOrEmpty(_currentProject.ProjectPath))
            {
                var fileName = $"{SanitizeFileName(_currentProject.Title)}.md";
                var filePath = Path.Combine(_currentProject.ProjectPath, fileName);
                await File.WriteAllTextAsync(filePath, markdown);
                ExportDialogTitle = "导出成功";
                ExportDialogMessage = $"已导出到:\n{filePath}";
                ShowExportDialog = true;
            }
        }
        catch (Exception ex)
        {
            ExportDialogTitle = "导出失败";
            ExportDialogMessage = ex.Message;
            ShowExportDialog = true;
        }
    }

    [RelayCommand]
    private void CloseExportDialog()
    {
        ShowExportDialog = false;
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
    }

    // --- Table of Contents ---

    [RelayCommand]
    private async Task GenerateTocAsync()
    {
        if (_currentProject is null) return;

        var chapters = await _documentRepository.GetByProjectIdAndTypeAsync(_currentProject.Id, DocumentType.Chapter);

        if (chapters.Count == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine("# 目录");
        sb.AppendLine();
        for (int i = 0; i < chapters.Count; i++)
        {
            var wc = chapters[i].WordCount > 0 ? $" ({chapters[i].WordCount}字)" : "";
            sb.AppendLine($"{i + 1}. {chapters[i].Title}{wc}");
        }

        var tocContent = sb.ToString().TrimEnd();

        // Check if TOC already exists
        var existingToc = Chapters.FirstOrDefault(c => c.Title == "目录");

        if (existingToc is not null)
        {
            // Update existing TOC
            existingToc.Content = tocContent;
            existingToc.WordCount = CalculateWordCount(tocContent);
            await _documentRepository.UpdateAsync(existingToc);

            // Select it
            SelectChapter(existingToc);
        }
        else
        {
            // Create new TOC at the beginning
            var toc = new Document
            {
                ProjectId = _currentProject.Id,
                Title = "目录",
                Type = DocumentType.Chapter,
                Content = tocContent,
                WordCount = CalculateWordCount(tocContent),
                SortOrder = -1
            };

            await _documentRepository.CreateAsync(toc);

            // Reload chapters to get the TOC at the top
            await LoadChaptersAsync();
            SelectChapter(toc);
        }
    }

    // --- Search & Replace ---

    [ObservableProperty]
    private bool _isSearchVisible;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _replaceText = string.Empty;

    [ObservableProperty]
    private int _matchCount;

    [ObservableProperty]
    private int _currentMatchIndex;

    [ObservableProperty]
    private bool _matchCase;

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
        if (!IsSearchVisible)
        {
            SearchText = string.Empty;
            ReplaceText = string.Empty;
            MatchCount = 0;
            CurrentMatchIndex = 0;
        }
    }

    [RelayCommand]
    private void SearchNext()
    {
        if (string.IsNullOrEmpty(SearchText) || string.IsNullOrEmpty(ChapterContent)) return;

        var matches = FindMatches();
        if (matches.Count == 0) return;

        MatchCount = matches.Count;
        CurrentMatchIndex = (CurrentMatchIndex + 1) % matches.Count;
    }

    [RelayCommand]
    private void SearchPrevious()
    {
        if (string.IsNullOrEmpty(SearchText) || string.IsNullOrEmpty(ChapterContent)) return;

        var matches = FindMatches();
        if (matches.Count == 0) return;

        MatchCount = matches.Count;
        CurrentMatchIndex = (CurrentMatchIndex - 1 + matches.Count) % matches.Count;
    }

    [RelayCommand]
    private void ReplaceCurrent()
    {
        if (string.IsNullOrEmpty(SearchText) || string.IsNullOrEmpty(ChapterContent)) return;

        var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var index = ChapterContent.IndexOf(SearchText, comparison);

        if (index >= 0)
        {
            ChapterContent = ChapterContent.Remove(index, SearchText.Length).Insert(index, ReplaceText);
            OnContentChanged();
        }
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        if (string.IsNullOrEmpty(SearchText) || string.IsNullOrEmpty(ChapterContent)) return;

        var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var result = ChapterContent.Replace(SearchText, ReplaceText, comparison);

        if (result != ChapterContent)
        {
            ChapterContent = result;
            OnContentChanged();
        }
    }

    private List<int> FindMatches()
    {
        var matches = new List<int>();
        if (string.IsNullOrEmpty(SearchText) || string.IsNullOrEmpty(ChapterContent)) return matches;

        var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var index = 0;
        while ((index = ChapterContent.IndexOf(SearchText, index, comparison)) >= 0)
        {
            matches.Add(index);
            index += SearchText.Length;
        }

        return matches;
    }
}
