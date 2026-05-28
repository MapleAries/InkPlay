using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkPlay.App.Services;
using InkPlay.Core.Interfaces;
using InkPlay.Core.Models;

namespace InkPlay.App.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectContext _projectContext;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    [ObservableProperty]
    private Project? _selectedProject;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showCreateDialog;

    [ObservableProperty]
    private string _newProjectTitle = string.Empty;

    [ObservableProperty]
    private string _newProjectDescription = string.Empty;

    [ObservableProperty]
    private string _newProjectGenre = "短剧";

    public HomeViewModel(
        IProjectRepository projectRepository,
        IProjectContext projectContext,
        INavigationService navigationService)
    {
        _projectRepository = projectRepository;
        _projectContext = projectContext;
        _navigationService = navigationService;
    }

    public override async void NavigatedTo(object? parameter)
    {
        await LoadProjectsAsync();
    }

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        IsLoading = true;
        try
        {
            var projects = await _projectRepository.GetAllAsync();
            Projects = new ObservableCollection<Project>(projects);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowCreateProjectDialog()
    {
        NewProjectTitle = string.Empty;
        NewProjectDescription = string.Empty;
        NewProjectGenre = "短剧";
        ShowCreateDialog = true;
    }

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProjectTitle)) return;

        var project = new Project
        {
            Title = NewProjectTitle.Trim(),
            Description = NewProjectDescription.Trim(),
            Genre = NewProjectGenre
        };

        await _projectRepository.CreateAsync(project);
        ShowCreateDialog = false;
        await LoadProjectsAsync();
    }

    [RelayCommand]
    private void CancelCreateProject()
    {
        ShowCreateDialog = false;
    }

    [RelayCommand]
    private void NavigateToFeature(string feature)
    {
        if (SelectedProject is null) return;

        _projectContext.SetCurrentProject(SelectedProject.Id);
        _navigationService.NavigateTo(feature, SelectedProject.Id);
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(Project project)
    {
        await _projectRepository.DeleteAsync(project.Id);
        Projects.Remove(project);
    }
}
