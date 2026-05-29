using System.ComponentModel;
using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace InkPlay.App.Views.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage(HomeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += async (_, _) => await ViewModel.LoadProjectsCommand.ExecuteAsync(null);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.CreateDialogOpen))
        {
            CreateDialog.Visibility = ViewModel.CreateDialogOpen ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void CreateProject_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowCreateProjectDialogCommand.Execute(null);
    }

    private async void SelectDirectory_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder is not null)
        {
            ViewModel.SelectedParentDirectory = folder.Path;
        }
    }

    private void CancelCreate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelCreateProjectCommand.Execute(null);
    }

    private async void ConfirmCreate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.NewProjectTitle)) return;

        // 如果有灵感，显示加载状态
        if (!string.IsNullOrWhiteSpace(ViewModel.InspirationText))
        {
            CreateForm.Visibility = Visibility.Collapsed;
            CreateLoading.Visibility = Visibility.Visible;
        }

        await ViewModel.CreateProjectCommand.ExecuteAsync(null);

        // 恢复表单状态（无论成功或失败）
        CreateForm.Visibility = Visibility.Visible;
        CreateLoading.Visibility = Visibility.Collapsed;
    }

    private void ProjectItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Project project)
        {
            if (!ViewModel.IsProjectDirectoryExists(project))
            {
                // 目录不存在，提示用户
                ViewModel.SelectedProject = project;
                ViewModel.EditProjectTitle = project.Title;
                DirectoryNotFoundDialog.Visibility = Visibility.Visible;
                return;
            }

            ViewModel.SelectedProject = project;
            ActionsDialog.Visibility = Visibility.Visible;
        }
    }

    private async void RemoveFromIndex_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProject is not null)
        {
            await ViewModel.RemoveProjectFromIndexCommand.ExecuteAsync(ViewModel.SelectedProject);
        }
        DirectoryNotFoundDialog.Visibility = Visibility.Collapsed;
    }

    private void CancelRemove_Click(object sender, RoutedEventArgs e)
    {
        DirectoryNotFoundDialog.Visibility = Visibility.Collapsed;
    }

    private void NavigateToFeature_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string feature)
        {
            ActionsDialog.Visibility = Visibility.Collapsed;
            ViewModel.NavigateToFeatureCommand.Execute(feature);
        }
    }

    private void CancelActions_Click(object sender, RoutedEventArgs e)
    {
        ActionsDialog.Visibility = Visibility.Collapsed;
    }

    private void EditProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is Guid projectId)
        {
            var project = ViewModel.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project is null) return;

            ViewModel.SelectedProject = project;
            ViewModel.EditProjectTitle = project.Title;
            ViewModel.EditProjectDescription = project.Description;
            EditDialog.Visibility = Visibility.Visible;
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        EditDialog.Visibility = Visibility.Collapsed;
    }

    private async void SaveEdit_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateProjectCommand.ExecuteAsync(null);
        EditDialog.Visibility = Visibility.Collapsed;
    }

    private void DeleteProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is Guid projectId)
        {
            var project = ViewModel.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project is null) return;

            ViewModel.SelectedProject = project;
            ViewModel.EditProjectTitle = project.Title;
            DeleteDialog.Visibility = Visibility.Visible;
        }
    }

    private void CancelDelete_Click(object sender, RoutedEventArgs e)
    {
        DeleteDialog.Visibility = Visibility.Collapsed;
    }

    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteSelectedProjectCommand.ExecuteAsync(null);
        DeleteDialog.Visibility = Visibility.Collapsed;
    }
}
