using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.Core.Enums;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class AssetsViewModel : ObservableObject
{
    private readonly IWorldSettingRepository _worldSettingRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IGlossaryRepository _glossaryRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IProjectContext _projectContext;
    private readonly IProjectRepository _projectRepository;

    [ObservableProperty]
    private Project? _currentProject;

    // 世界观
    [ObservableProperty]
    private ObservableCollection<WorldSetting> _worldSettings = new();

    [ObservableProperty]
    private WorldSetting? _selectedWorldSetting;

    // 角色
    [ObservableProperty]
    private ObservableCollection<Character> _characters = new();

    // 术语表
    [ObservableProperty]
    private ObservableCollection<GlossaryEntry> _glossaryEntries = new();

    [ObservableProperty]
    private GlossaryEntry? _selectedGlossaryEntry;

    [ObservableProperty]
    private string _selectedCategory = "全部";

    // 样章
    [ObservableProperty]
    private ObservableCollection<Document> _sampleChapters = new();

    [ObservableProperty]
    private Document? _selectedSampleChapter;

    // 当前 Tab
    [ObservableProperty]
    private int _selectedTabIndex;

    // 编辑状态
    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private string _editContent = string.Empty;

    [ObservableProperty]
    private string _editCategory = string.Empty;

    [ObservableProperty]
    private string _editTerm = string.Empty;

    [ObservableProperty]
    private string _editDefinition = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public AssetsViewModel(
        IWorldSettingRepository worldSettingRepository,
        ICharacterRepository characterRepository,
        IGlossaryRepository glossaryRepository,
        IDocumentRepository documentRepository,
        IProjectContext projectContext,
        IProjectRepository projectRepository)
    {
        _worldSettingRepository = worldSettingRepository;
        _characterRepository = characterRepository;
        _glossaryRepository = glossaryRepository;
        _documentRepository = documentRepository;
        _projectContext = projectContext;
        _projectRepository = projectRepository;
    }

    public async Task InitializeAsync()
    {
        var projectId = _projectContext.CurrentProjectId;
        if (projectId.HasValue)
        {
            CurrentProject = await _projectRepository.GetByIdAsync(projectId.Value);
            await LoadAllDataAsync();
        }
    }

    private async Task LoadAllDataAsync()
    {
        if (CurrentProject == null) return;

        var worldSettings = await _worldSettingRepository.GetByProjectIdAsync(CurrentProject.Id);
        WorldSettings = new ObservableCollection<WorldSetting>(worldSettings);

        var characters = await _characterRepository.GetByProjectIdAsync(CurrentProject.Id);
        Characters = new ObservableCollection<Character>(characters);

        var glossaryEntries = await _glossaryRepository.GetByProjectIdAsync(CurrentProject.Id);
        GlossaryEntries = new ObservableCollection<GlossaryEntry>(glossaryEntries);

        var sampleChapters = await _documentRepository.GetByProjectIdAsync(CurrentProject.Id);
        SampleChapters = new ObservableCollection<Document>(
            sampleChapters.Where(d => d.Type == DocumentType.Note || d.Type == DocumentType.Script));
    }

    // 世界观 CRUD
    [RelayCommand]
    private async Task AddWorldSettingAsync()
    {
        if (CurrentProject == null) return;

        var setting = new WorldSetting
        {
            ProjectId = CurrentProject.Id,
            Title = "新世界观设定",
            Category = "通用",
            Content = string.Empty
        };

        await _worldSettingRepository.CreateAsync(setting);
        WorldSettings.Add(setting);
        SelectedWorldSetting = setting;
    }

    [RelayCommand]
    private async Task SaveWorldSettingAsync()
    {
        if (SelectedWorldSetting == null) return;

        SelectedWorldSetting.Title = EditTitle;
        SelectedWorldSetting.Category = EditCategory;
        SelectedWorldSetting.Content = EditContent;
        await _worldSettingRepository.UpdateAsync(SelectedWorldSetting);

        var index = WorldSettings.IndexOf(SelectedWorldSetting);
        if (index >= 0) WorldSettings[index] = SelectedWorldSetting;
        IsEditing = false;
    }

    [RelayCommand]
    private async Task DeleteWorldSettingAsync()
    {
        if (SelectedWorldSetting == null) return;

        await _worldSettingRepository.DeleteAsync(SelectedWorldSetting.Id);
        WorldSettings.Remove(SelectedWorldSetting);
        SelectedWorldSetting = null;
    }

    // 术语表 CRUD
    [RelayCommand]
    private async Task AddGlossaryEntryAsync()
    {
        if (CurrentProject == null) return;

        var entry = new GlossaryEntry
        {
            ProjectId = CurrentProject.Id,
            Term = "新术语",
            Definition = string.Empty,
            Category = "其他"
        };

        await _glossaryRepository.CreateAsync(entry);
        GlossaryEntries.Add(entry);
        SelectedGlossaryEntry = entry;
    }

    [RelayCommand]
    private async Task SaveGlossaryEntryAsync()
    {
        if (SelectedGlossaryEntry == null) return;

        SelectedGlossaryEntry.Term = EditTerm;
        SelectedGlossaryEntry.Definition = EditDefinition;
        SelectedGlossaryEntry.Category = EditCategory;
        await _glossaryRepository.UpdateAsync(SelectedGlossaryEntry);

        var index = GlossaryEntries.IndexOf(SelectedGlossaryEntry);
        if (index >= 0) GlossaryEntries[index] = SelectedGlossaryEntry;
        IsEditing = false;
    }

    [RelayCommand]
    private async Task DeleteGlossaryEntryAsync()
    {
        if (SelectedGlossaryEntry == null) return;

        await _glossaryRepository.DeleteAsync(SelectedGlossaryEntry.Id);
        GlossaryEntries.Remove(SelectedGlossaryEntry);
        SelectedGlossaryEntry = null;
    }

    [RelayCommand]
    public async Task FilterByCategoryAsync(string category)
    {
        if (CurrentProject == null) return;

        SelectedCategory = category;
        IReadOnlyList<GlossaryEntry> entries;

        if (category == "全部")
        {
            entries = await _glossaryRepository.GetByProjectIdAsync(CurrentProject.Id);
        }
        else
        {
            entries = await _glossaryRepository.GetByCategoryAsync(CurrentProject.Id, category);
        }

        GlossaryEntries = new ObservableCollection<GlossaryEntry>(entries);
    }

    // 选择编辑
    [RelayCommand]
    private void SelectWorldSetting(WorldSetting? setting)
    {
        SelectedWorldSetting = setting;
        if (setting != null)
        {
            EditTitle = setting.Title;
            EditCategory = setting.Category;
            EditContent = setting.Content;
            IsEditing = true;
        }
    }

    [RelayCommand]
    private void SelectGlossaryEntry(GlossaryEntry? entry)
    {
        SelectedGlossaryEntry = entry;
        if (entry != null)
        {
            EditTerm = entry.Term;
            EditDefinition = entry.Definition;
            EditCategory = entry.Category;
            IsEditing = true;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }
}
