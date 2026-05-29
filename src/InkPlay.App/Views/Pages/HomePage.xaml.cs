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
            if (ViewModel.CreateDialogOpen)
            {
                ShowStep(1);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.CreationStep))
        {
            ShowStep(ViewModel.CreationStep);
        }
    }

    private void ShowStep(int step)
    {
        Step1Panel.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        CreateLoading.Visibility = Visibility.Collapsed;
    }

    private void CreationMode_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string mode)
        {
            ViewModel.CreationMode = mode;

            // Update Step 2 UI based on mode
            InspirationPanel.Visibility = mode == "inspiration" ? Visibility.Visible : Visibility.Collapsed;
            OutlinePanel.Visibility = mode == "outline" ? Visibility.Visible : Visibility.Collapsed;
            TagsPanel.Visibility = mode == "none" ? Visibility.Visible : Visibility.Collapsed;

            Step2Title.Text = mode switch
            {
                "inspiration" => "输入创作灵感",
                "outline" => "粘贴大纲内容",
                "none" => "选择小说类型和标签",
                _ => "输入内容"
            };
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

    private void NextStep_Click(object sender, RoutedEventArgs e)
    {
        // Update novel type from ComboBox before moving to next step
        if (ViewModel.CreationMode == "none" && TypeComboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.NovelType = item.Content?.ToString() ?? "玄幻";
        }
        ViewModel.NextStepCommand.Execute(null);
    }

    private void PrevStep_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PreviousStepCommand.Execute(null);
    }

    private async void ConfirmCreate_Click(object sender, RoutedEventArgs e)
    {
        // Show loading
        Step3Panel.Visibility = Visibility.Collapsed;
        CreateLoading.Visibility = Visibility.Visible;

        await ViewModel.CreateProjectCommand.ExecuteAsync(null);

        // If still creating (error occurred), restore step 3
        if (!ViewModel.CreateDialogOpen)
        {
            CreateLoading.Visibility = Visibility.Collapsed;
        }
        else
        {
            CreateLoading.Visibility = Visibility.Collapsed;
            Step3Panel.Visibility = Visibility.Visible;
        }
    }

    private void ProjectItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Project project)
        {
            if (!ViewModel.IsProjectDirectoryExists(project))
            {
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

    private async void DeleteToRecycleBin_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteSelectedProjectCommand.ExecuteAsync(true);
        DeleteDialog.Visibility = Visibility.Collapsed;
    }

    private async void DeletePermanent_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteSelectedProjectCommand.ExecuteAsync(false);
        DeleteDialog.Visibility = Visibility.Collapsed;
    }
}
