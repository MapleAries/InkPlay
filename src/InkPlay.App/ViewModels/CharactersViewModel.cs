using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class CharactersViewModel : ViewModelBase
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IVoiceRepository _voiceRepository;
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
    private ObservableCollection<Character> _characters = new();

    [ObservableProperty]
    private Character? _currentCharacter;

    [ObservableProperty]
    private bool _isCharacterSelected;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _characterAlias = string.Empty;

    [ObservableProperty]
    private string _characterAge = string.Empty;

    [ObservableProperty]
    private string _characterGender = string.Empty;

    [ObservableProperty]
    private string _characterRole = string.Empty;

    [ObservableProperty]
    private string _characterAppearance = string.Empty;

    [ObservableProperty]
    private string _characterPersonality = string.Empty;

    [ObservableProperty]
    private string _characterMotivation = string.Empty;

    [ObservableProperty]
    private string _characterWeakness = string.Empty;

    [ObservableProperty]
    private string _characterBackstory = string.Empty;

    [ObservableProperty]
    private bool _isAiProcessing;

    [ObservableProperty]
    private string _aiResponse = string.Empty;

    [ObservableProperty]
    private string _aiUserInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ApiKeyConfig> _availableModels = new();

    [ObservableProperty]
    private ApiKeyConfig? _selectedModel;

    [ObservableProperty]
    private ObservableCollection<AiChatMessage> _aiMessages = new();

    public CharactersViewModel(
        ICharacterRepository characterRepository,
        IProjectRepository projectRepository,
        IVoiceRepository voiceRepository,
        IAiProviderFactory aiProviderFactory,
        ISettingsService settingsService,
        IProjectContext projectContext,
        NavigationService navigationService)
    {
        _characterRepository = characterRepository;
        _projectRepository = projectRepository;
        _voiceRepository = voiceRepository;
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
            CurrentProjectTitle = CurrentProject?.Title ?? "";
            if (CurrentProject is not null)
            {
                await LoadCharactersAsync();
                LoadAvailableModels();
            }
        }
        else
        {
            HasProject = false;
            CurrentProjectTitle = "";
        }
    }

    private void LoadAvailableModels()
    {
        var keys = _settingsService.GetApiKeys(ApiKeyCategory.Text);
        AvailableModels = new ObservableCollection<ApiKeyConfig>(keys);
        SelectedModel = keys.FirstOrDefault(k => k.IsDefault) ?? keys.FirstOrDefault();
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        if (CurrentProject is null) return;

        var characters = await _characterRepository.GetByProjectIdAsync(CurrentProject.Id);
        Characters = new ObservableCollection<Character>(characters);
        CurrentCharacter = null;
        IsCharacterSelected = false;
        ClearCharacterFields();
    }

    [RelayCommand]
    private async Task CreateCharacterAsync()
    {
        if (CurrentProject is null) return;

        var character = new Character
        {
            ProjectId = CurrentProject.Id,
            Name = $"新角色 {Characters.Count + 1}",
            Role = "未设定"
        };

        await _characterRepository.CreateAsync(character);
        Characters.Add(character);
        SelectCharacter(character);
    }

    [RelayCommand]
    private async Task SaveCharacterAsync()
    {
        if (CurrentCharacter is null) return;

        CurrentCharacter.Name = CharacterName;
        CurrentCharacter.Alias = CharacterAlias;
        CurrentCharacter.Age = int.TryParse(CharacterAge, out var age) ? age : null;
        CurrentCharacter.Gender = CharacterGender;
        CurrentCharacter.Role = CharacterRole;
        CurrentCharacter.Appearance = CharacterAppearance;
        CurrentCharacter.Personality = CharacterPersonality;
        CurrentCharacter.Motivation = CharacterMotivation;
        CurrentCharacter.Weakness = CharacterWeakness;
        CurrentCharacter.Backstory = CharacterBackstory;

        await _characterRepository.UpdateAsync(CurrentCharacter);

        // Refresh the list to reflect name changes
        var index = Characters.IndexOf(CurrentCharacter);
        if (index >= 0)
        {
            Characters[index] = CurrentCharacter;
        }
    }

    [RelayCommand]
    private async Task DeleteCharacterAsync()
    {
        if (CurrentCharacter is null) return;

        await _characterRepository.DeleteAsync(CurrentCharacter.Id);
        Characters.Remove(CurrentCharacter);

        CurrentCharacter = null;
        IsCharacterSelected = false;
        ClearCharacterFields();
    }

    [RelayCommand]
    private void SelectCharacter(Character? character)
    {
        if (character is null)
        {
            CurrentCharacter = null;
            IsCharacterSelected = false;
            ClearCharacterFields();
            return;
        }

        CurrentCharacter = character;
        IsCharacterSelected = true;
        LoadCharacterFields();
    }

    private void ClearCharacterFields()
    {
        CharacterName = string.Empty;
        CharacterAlias = string.Empty;
        CharacterAge = string.Empty;
        CharacterGender = string.Empty;
        CharacterRole = string.Empty;
        CharacterAppearance = string.Empty;
        CharacterPersonality = string.Empty;
        CharacterMotivation = string.Empty;
        CharacterWeakness = string.Empty;
        CharacterBackstory = string.Empty;
    }

    private void LoadCharacterFields()
    {
        if (CurrentCharacter is null)
        {
            ClearCharacterFields();
            return;
        }

        CharacterName = CurrentCharacter.Name;
        CharacterAlias = CurrentCharacter.Alias;
        CharacterAge = CurrentCharacter.Age?.ToString() ?? string.Empty;
        CharacterGender = CurrentCharacter.Gender;
        CharacterRole = CurrentCharacter.Role;
        CharacterAppearance = CurrentCharacter.Appearance;
        CharacterPersonality = CurrentCharacter.Personality;
        CharacterMotivation = CurrentCharacter.Motivation;
        CharacterWeakness = CurrentCharacter.Weakness;
        CharacterBackstory = CurrentCharacter.Backstory;
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
            "personality" => $"请为以下角色设计详细的性格特点：\n姓名：{CharacterName}\n角色：{CharacterRole}\n背景：{CharacterBackstory}",
            "backstory" => $"请为以下角色设计详细的背景故事：\n姓名：{CharacterName}\n角色：{CharacterRole}\n性格：{CharacterPersonality}",
            "appearance" => $"请为以下角色设计详细的外貌描述：\n姓名：{CharacterName}\n性别：{CharacterGender}\n角色：{CharacterRole}",
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
                Content = "你是一个专业的网文角色设计助手。你的任务是帮助用户创建和管理网文/小说中的角色。" +
                          "你应该帮助设计角色的性格特点、背景故事、外貌描述，确保角色设定的内在一致性和逻辑性，" +
                          "提供有深度、有层次的角色塑造建议，考虑角色在故事中的作用和与其他角色的关系，" +
                          "注重角色的读者吸引力和故事功能性。请用中文回复。"
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
            CharacterBackstory += "\n" + AiResponse;
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
    private async Task GenerateVoiceAsync(Character character)
    {
        if (character is null || CurrentProject is null) return;

        try
        {
            var apiKeyConfig = _settingsService.GetDefaultApiKey(ApiKeyCategory.Text);
            if (apiKeyConfig is null) return;

            var provider = _aiProviderFactory.GetProviderForApiKey(apiKeyConfig);

            var prompt = $@"请根据以下角色信息，设计一个详细的音色描述。

角色姓名：{character.Name}
性别：{character.Gender}
年龄：{character.Age?.ToString() ?? "未知"}
角色定位：{character.Role}
性格特点：{character.Personality}
外貌描述：{character.Appearance}

请用以下JSON格式返回（只返回JSON，不要有其他内容）：
{{
  ""description"": ""音色的整体描述"",
  ""tone"": ""语调特征（如温柔、冷酷、活泼等）"",
  ""speed"": ""语速（如缓慢、适中、快速）"",
  ""pitch"": ""音调（如低沉、中等、高亢）"",
  ""ageRange"": ""年龄段（如少年、青年、中年、老年）"",
  ""sampleText"": ""一段能体现该音色特点的示例文本，30-50字""
}}";

            var messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = "你是一个专业的音色设计助手。请根据角色信息设计合适的音色参数。只返回JSON格式，不要有其他内容。" },
                new() { Role = "user", Content = prompt }
            };

            var response = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamCompletionAsync(apiKeyConfig, messages))
            {
                response.Append(chunk);
            }

            var json = response.ToString().Trim();
            // Extract JSON
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                json = json[start..(end + 1)];
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var voice = new Voice
            {
                ProjectId = CurrentProject.Id,
                Name = $"{character.Name}的音色",
                Gender = character.Gender,
                AgeRange = result?.GetValueOrDefault("ageRange") ?? "",
                Tone = result?.GetValueOrDefault("tone") ?? character.Personality,
                Speed = result?.GetValueOrDefault("speed") ?? "适中",
                Pitch = result?.GetValueOrDefault("pitch") ?? "中等",
                Description = result?.GetValueOrDefault("description") ?? $"{character.Gender}，{character.Personality}",
                SampleText = result?.GetValueOrDefault("sampleText") ?? $"我叫{character.Name}，{character.Role}。"
            };

            await _voiceRepository.CreateAsync(voice);

            // Navigate to voices page
            _projectContext.SetCurrentProject(CurrentProject.Id);
            _navigationService.NavigateTo("Voices", CurrentProject.Id);
        }
        catch
        {
            // Fallback: create voice with basic info
            var voice = new Voice
            {
                ProjectId = CurrentProject.Id,
                Name = $"{character.Name}的音色",
                Gender = character.Gender,
                Tone = character.Personality,
                Description = $"{character.Gender}，{character.Personality}",
                SampleText = $"我叫{character.Name}，{character.Role}。{character.Personality}"
            };

            await _voiceRepository.CreateAsync(voice);
            _projectContext.SetCurrentProject(CurrentProject.Id);
            _navigationService.NavigateTo("Voices", CurrentProject.Id);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
