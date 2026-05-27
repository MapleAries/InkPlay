using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAiProviderFactory _aiProviderFactory;

    [ObservableProperty]
    private string _claudeApiKey = string.Empty;

    [ObservableProperty]
    private string _claudeBaseUrl = "https://api.anthropic.com";

    [ObservableProperty]
    private string _claudeModelId = "claude-sonnet-4-20250514";

    [ObservableProperty]
    private string _openAiApiKey = string.Empty;

    [ObservableProperty]
    private string _openAiBaseUrl = "https://api.openai.com";

    [ObservableProperty]
    private string _openAiModelId = "gpt-4o";

    [ObservableProperty]
    private string _qwenApiKey = string.Empty;

    [ObservableProperty]
    private string _qwenBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";

    [ObservableProperty]
    private string _qwenModelId = "qwen-plus";

    [ObservableProperty]
    private string _selectedProviderId = "claude";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, IAiProviderFactory aiProviderFactory)
    {
        _settingsService = settingsService;
        _aiProviderFactory = aiProviderFactory;
    }

    public override void NavigatedTo(object? parameter)
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        var claudeConfig = _settingsService.GetAiProviderConfig("claude");
        ClaudeApiKey = claudeConfig.ApiKey;
        ClaudeBaseUrl = claudeConfig.BaseUrl;
        ClaudeModelId = string.IsNullOrEmpty(claudeConfig.ModelId) ? "claude-sonnet-4-20250514" : claudeConfig.ModelId;

        var openAiConfig = _settingsService.GetAiProviderConfig("openai");
        OpenAiApiKey = openAiConfig.ApiKey;
        OpenAiBaseUrl = openAiConfig.BaseUrl;
        OpenAiModelId = string.IsNullOrEmpty(openAiConfig.ModelId) ? "gpt-4o" : openAiConfig.ModelId;

        var qwenConfig = _settingsService.GetAiProviderConfig("qwen");
        QwenApiKey = qwenConfig.ApiKey;
        QwenBaseUrl = qwenConfig.BaseUrl;
        QwenModelId = string.IsNullOrEmpty(qwenConfig.ModelId) ? "qwen-plus" : qwenConfig.ModelId;

        SelectedProviderId = _settingsService.GetDefaultAiProviderId();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settingsService.SaveAiProviderConfig(new AiProviderConfig
        {
            ProviderId = "claude",
            ApiKey = ClaudeApiKey,
            BaseUrl = ClaudeBaseUrl,
            ModelId = ClaudeModelId
        });

        _settingsService.SaveAiProviderConfig(new AiProviderConfig
        {
            ProviderId = "openai",
            ApiKey = OpenAiApiKey,
            BaseUrl = OpenAiBaseUrl,
            ModelId = OpenAiModelId
        });

        _settingsService.SaveAiProviderConfig(new AiProviderConfig
        {
            ProviderId = "qwen",
            ApiKey = QwenApiKey,
            BaseUrl = QwenBaseUrl,
            ModelId = QwenModelId
        });

        _settingsService.SetDefaultAiProviderId(SelectedProviderId);

        StatusMessage = "设置已保存";
    }

    [RelayCommand]
    private async Task TestConnectionAsync(string providerId)
    {
        StatusMessage = $"正在测试 {providerId} 连接...";
        try
        {
            var config = _settingsService.GetAiProviderConfig(providerId);
            var provider = _aiProviderFactory.GetProvider(providerId);
            var result = await provider.ValidateConfigurationAsync(config);
            StatusMessage = result ? $"{providerId} 连接成功" : $"{providerId} 连接失败";
        }
        catch (Exception ex)
        {
            StatusMessage = $"{providerId} 连接失败: {ex.Message}";
        }
    }
}
