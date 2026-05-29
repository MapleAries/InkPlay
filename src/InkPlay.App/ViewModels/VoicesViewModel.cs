using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class VoicesViewModel : ViewModelBase
{
    private readonly IVoiceRepository _voiceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly ISettingsService _settingsService;
    private readonly IProjectContext _projectContext;
    private readonly NavigationService _navigationService;
    private CancellationTokenSource? _aiCts;

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private ObservableCollection<Voice> _voices = new();

    [ObservableProperty]
    private Voice? _currentVoice;

    [ObservableProperty]
    private bool _isVoiceSelected;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string _voiceName = string.Empty;

    [ObservableProperty]
    private string _voiceDescription = string.Empty;

    [ObservableProperty]
    private string _voiceGender = string.Empty;

    [ObservableProperty]
    private string _voiceAgeRange = string.Empty;

    [ObservableProperty]
    private string _voiceTone = string.Empty;

    [ObservableProperty]
    private string _voiceSpeed = string.Empty;

    [ObservableProperty]
    private string _voicePitch = string.Empty;

    [ObservableProperty]
    private string _voiceSampleText = string.Empty;

    [ObservableProperty]
    private bool _isAiProcessing;

    [ObservableProperty]
    private string _aiResponse = string.Empty;

    [ObservableProperty]
    private string _aiUserInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AiChatMessage> _aiMessages = new();

    public VoicesViewModel(
        IVoiceRepository voiceRepository,
        IProjectRepository projectRepository,
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService,
        IProjectContext projectContext,
        NavigationService navigationService)
    {
        _voiceRepository = voiceRepository;
        _projectRepository = projectRepository;
        _aiProviderFactory = aiProviderFactory;
        _settingsService = settingsService;
        _projectContext = projectContext;
        _navigationService = navigationService;
    }

    public override async void NavigatedTo(object? parameter)
    {
        var projectId = parameter as Guid? ?? _projectContext.CurrentProjectId;
        if (projectId.HasValue)
        {
            CurrentProject = await _projectRepository.GetByIdAsync(projectId.Value);
            HasProject = CurrentProject is not null;
            if (CurrentProject is not null)
            {
                await LoadVoicesAsync();
            }
        }
        else
        {
            HasProject = false;
        }
    }

    [RelayCommand]
    private async Task LoadVoicesAsync()
    {
        if (CurrentProject is null) return;

        var voices = await _voiceRepository.GetByProjectIdAsync(CurrentProject.Id);
        Voices = new ObservableCollection<Voice>(voices);
        CurrentVoice = null;
        IsVoiceSelected = false;
        ClearVoiceFields();
    }

    [RelayCommand]
    private async Task CreateVoiceAsync()
    {
        if (CurrentProject is null) return;

        var voice = new Voice
        {
            ProjectId = CurrentProject.Id,
            Name = $"音色 {Voices.Count + 1}"
        };

        await _voiceRepository.CreateAsync(voice);
        Voices.Add(voice);
        SelectVoice(voice);
    }

    [RelayCommand]
    private async Task SaveVoiceAsync()
    {
        if (CurrentVoice is null) return;

        CurrentVoice.Name = VoiceName;
        CurrentVoice.Description = VoiceDescription;
        CurrentVoice.Gender = VoiceGender;
        CurrentVoice.AgeRange = VoiceAgeRange;
        CurrentVoice.Tone = VoiceTone;
        CurrentVoice.Speed = VoiceSpeed;
        CurrentVoice.Pitch = VoicePitch;
        CurrentVoice.SampleText = VoiceSampleText;

        await _voiceRepository.UpdateAsync(CurrentVoice);

        var index = Voices.IndexOf(CurrentVoice);
        if (index >= 0)
        {
            Voices[index] = CurrentVoice;
        }
    }

    [RelayCommand]
    private async Task DeleteVoiceAsync()
    {
        if (CurrentVoice is null) return;

        await _voiceRepository.DeleteAsync(CurrentVoice.Id);
        Voices.Remove(CurrentVoice);

        CurrentVoice = null;
        IsVoiceSelected = false;
        ClearVoiceFields();
    }

    [RelayCommand]
    private void SelectVoice(Voice? voice)
    {
        if (voice is null)
        {
            CurrentVoice = null;
            IsVoiceSelected = false;
            ClearVoiceFields();
            return;
        }

        CurrentVoice = voice;
        IsVoiceSelected = true;
        LoadVoiceFields();
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
    private async Task QuickActionAsync(string action)
    {
        var prompt = action switch
        {
            "description" => $"请为以下角色设计一个详细的音色描述：\n姓名：{VoiceName}\n性别：{VoiceGender}\n年龄段：{VoiceAgeRange}\n语调：{VoiceTone}",
            "sample" => $"请为以下音色生成一段示例文本，展示该音色的特点：\n音色名称：{VoiceName}\n音色描述：{VoiceDescription}\n语调：{VoiceTone}\n语速：{VoiceSpeed}",
            _ => action
        };

        await SendAiRequestAsync(prompt);
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
                Content = "你是一个专业的音色设计助手。你的任务是帮助用户为小说角色设计合适的音色。" +
                          "你应该根据角色的性格、年龄、性别等特征，设计音色的描述、语速、音调等参数，" +
                          "并生成能体现该音色特点的示例文本。请用中文回复。"
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
            VoiceDescription += "\n" + AiResponse;
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
        _navigationService.NavigateTo("Home");
    }

    private void ClearVoiceFields()
    {
        VoiceName = string.Empty;
        VoiceDescription = string.Empty;
        VoiceGender = string.Empty;
        VoiceAgeRange = string.Empty;
        VoiceTone = string.Empty;
        VoiceSpeed = string.Empty;
        VoicePitch = string.Empty;
        VoiceSampleText = string.Empty;
    }

    private void LoadVoiceFields()
    {
        if (CurrentVoice is null)
        {
            ClearVoiceFields();
            return;
        }

        VoiceName = CurrentVoice.Name;
        VoiceDescription = CurrentVoice.Description;
        VoiceGender = CurrentVoice.Gender;
        VoiceAgeRange = CurrentVoice.AgeRange;
        VoiceTone = CurrentVoice.Tone;
        VoiceSpeed = CurrentVoice.Speed;
        VoicePitch = CurrentVoice.Pitch;
        VoiceSampleText = CurrentVoice.SampleText;
    }
}
