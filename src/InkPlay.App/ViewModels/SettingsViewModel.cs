using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;

namespace InkPlay.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<ApiKeyConfig> _textApiKeys = new();

    [ObservableProperty]
    private ObservableCollection<ApiKeyConfig> _videoApiKeys = new();

    [ObservableProperty]
    private ObservableCollection<ApiKeyConfig> _voiceApiKeys = new();

    [ObservableProperty]
    private ApiKeyConfig? _editingKey;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isNewKey;

    // Edit form fields
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editApiKey = string.Empty;

    [ObservableProperty]
    private string _editBaseUrl = string.Empty;

    [ObservableProperty]
    private string _editModelId = string.Empty;

    [ObservableProperty]
    private ApiKeyCategory _editCategory = ApiKeyCategory.Text;

    [ObservableProperty]
    private bool _editIsDefault;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _selectedThemeIndex;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadTheme();
    }

    public override void NavigatedTo(object? parameter)
    {
        LoadApiKeys();
    }

    private void LoadApiKeys()
    {
        var textKeys = _settingsService.GetApiKeys(ApiKeyCategory.Text);
        TextApiKeys = new ObservableCollection<ApiKeyConfig>(textKeys);

        var videoKeys = _settingsService.GetApiKeys(ApiKeyCategory.Video);
        VideoApiKeys = new ObservableCollection<ApiKeyConfig>(videoKeys);

        var voiceKeys = _settingsService.GetApiKeys(ApiKeyCategory.Voice);
        VoiceApiKeys = new ObservableCollection<ApiKeyConfig>(voiceKeys);
    }

    [RelayCommand]
    private void AddTextKey()
    {
        StartEdit(new ApiKeyConfig { Category = ApiKeyCategory.Text }, isNew: true);
    }

    [RelayCommand]
    private void AddVideoKey()
    {
        StartEdit(new ApiKeyConfig { Category = ApiKeyCategory.Video }, isNew: true);
    }

    [RelayCommand]
    private void AddVoiceKey()
    {
        StartEdit(new ApiKeyConfig { Category = ApiKeyCategory.Voice }, isNew: true);
    }

    [RelayCommand]
    private void EditKey(ApiKeyConfig? config)
    {
        if (config is null) return;
        StartEdit(config, isNew: false);
    }

    private void StartEdit(ApiKeyConfig config, bool isNew)
    {
        EditingKey = config;
        IsNewKey = isNew;
        EditName = config.Name;
        EditApiKey = config.ApiKey;
        EditBaseUrl = config.BaseUrl;
        EditModelId = config.ModelId;
        EditCategory = config.Category;
        EditIsDefault = config.IsDefault;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingKey = null;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void SaveKey()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            StatusMessage = "请输入名称";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditApiKey))
        {
            StatusMessage = "请输入 API Key";
            return;
        }

        var trimmedName = EditName.Trim();

        // Check for duplicate name in the same category (exclude current when editing)
        var existingKeys = _settingsService.GetApiKeys(EditCategory);
        var duplicate = existingKeys.FirstOrDefault(k =>
            k.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)
            && k.Id != EditingKey?.Id);

        if (duplicate is not null)
        {
            StatusMessage = $"名称 \"{trimmedName}\" 已存在，请使用其他名称";
            return;
        }

        var config = EditingKey ?? new ApiKeyConfig();
        config.Name = trimmedName;
        config.ApiKey = EditApiKey.Trim();
        config.BaseUrl = EditBaseUrl.Trim();
        config.ModelId = EditModelId.Trim();
        config.Category = EditCategory;
        config.IsDefault = EditIsDefault;

        if (IsNewKey && config.Id == Guid.Empty)
        {
            config.Id = Guid.NewGuid();
        }

        _settingsService.SaveApiKey(config);
        IsEditing = false;
        EditingKey = null;
        StatusMessage = "已保存";
        LoadApiKeys();
    }

    [RelayCommand]
    private void DeleteKey(ApiKeyConfig? config)
    {
        if (config is null) return;
        _settingsService.DeleteApiKey(config.Id);
        StatusMessage = "已删除";
        LoadApiKeys();
    }

    [RelayCommand]
    private void SetDefault(ApiKeyConfig? config)
    {
        if (config is null) return;
        _settingsService.SetDefaultApiKey(config.Id, config.Category);
        StatusMessage = $"已设为默认: {config.Name}";
        LoadApiKeys();
    }

    private void LoadTheme()
    {
        var theme = _settingsService.GetTheme();
        SelectedThemeIndex = theme switch
        {
            "Dark" => 0,
            "Light" => 1,
            _ => 2 // System
        };
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            0 => "Dark",
            1 => "Light",
            _ => "Default"
        };
        _settingsService.SetTheme(theme);
        ApplyTheme(theme);
    }

    private static void ApplyTheme(string theme)
    {
        if (App.MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                "Dark" => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _ => ElementTheme.Default
            };
        }
    }
}
