using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class VideoGenerationViewModel : ViewModelBase
{
    private readonly IVideoProvider _videoProvider;
    private readonly ISettingsService _settingsService;
    private readonly IProjectContext _projectContext;
    private readonly IProjectRepository _projectRepository;
    private readonly NavigationService _navigationService;
    private CancellationTokenSource? _pollingCts;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private string _negativePrompt = string.Empty;

    [ObservableProperty]
    private int _duration = 5;

    [ObservableProperty]
    private string _resolution = "1080p";

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _generationStatus = string.Empty;

    [ObservableProperty]
    private string _currentVideoUrl = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VideoGenerationResult> _history = new();

    public VideoGenerationViewModel(
        IVideoProvider videoProvider,
        ISettingsService settingsService,
        IProjectContext projectContext,
        IProjectRepository projectRepository,
        NavigationService navigationService)
    {
        _videoProvider = videoProvider;
        _settingsService = settingsService;
        _projectContext = projectContext;
        _projectRepository = projectRepository;
        _navigationService = navigationService;
    }

    public override async void NavigatedTo(object? parameter)
    {
        var projectId = parameter as Guid? ?? _projectContext.CurrentProjectId;
        if (projectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            HasProject = project is not null;
        }
        else
        {
            HasProject = false;
        }
    }

    [RelayCommand]
    private async Task GenerateVideoAsync()
    {
        if (string.IsNullOrWhiteSpace(Prompt)) return;

        var config = _settingsService.GetDefaultApiKey(ApiKeyCategory.Video);
        if (config is null)
        {
            GenerationStatus = "请先在设置中配置视频生成 API Key";
            return;
        }

        IsGenerating = true;
        GenerationStatus = "正在提交生成任务...";
        _pollingCts?.Cancel();
        _pollingCts = new CancellationTokenSource();

        try
        {
            var request = new VideoGenerationRequest
            {
                Prompt = Prompt,
                NegativePrompt = NegativePrompt,
                Duration = Duration,
                Resolution = Resolution
            };

            var result = await _videoProvider.GenerateVideoAsync(config, request, _pollingCts.Token);

            if (string.IsNullOrEmpty(result.TaskId))
            {
                GenerationStatus = "任务提交失败";
                IsGenerating = false;
                return;
            }

            GenerationStatus = $"任务已提交，ID: {result.TaskId}";

            // Poll for status
            await PollVideoStatusAsync(config, result.TaskId);
        }
        catch (OperationCanceledException)
        {
            GenerationStatus = "已取消";
        }
        catch (Exception ex)
        {
            GenerationStatus = $"生成失败: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task PollVideoStatusAsync(ApiKeyConfig config, string taskId)
    {
        var maxAttempts = 60; // 5 minutes max
        var delay = TimeSpan.FromSeconds(5);

        for (int i = 0; i < maxAttempts; i++)
        {
            _pollingCts?.Token.ThrowIfCancellationRequested();

            await Task.Delay(delay, _pollingCts?.Token ?? CancellationToken.None);

            var result = await _videoProvider.CheckStatusAsync(config, taskId, _pollingCts?.Token ?? CancellationToken.None);

            GenerationStatus = result.Status switch
            {
                "processing" => $"生成中... ({i + 1}/{maxAttempts})",
                "completed" => "生成完成",
                "failed" => $"生成失败: {result.ErrorMessage}",
                _ => $"状态: {result.Status}"
            };

            if (result.Status == "completed")
            {
                CurrentVideoUrl = result.VideoUrl;
                History.Insert(0, result);
                return;
            }

            if (result.Status == "failed")
            {
                History.Insert(0, result);
                return;
            }
        }

        GenerationStatus = "轮询超时，请稍后查看结果";
    }

    [RelayCommand]
    private void CancelGeneration()
    {
        _pollingCts?.Cancel();
    }

    [RelayCommand]
    private void ClearHistory()
    {
        History.Clear();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
