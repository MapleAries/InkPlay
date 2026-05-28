using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    }

    private void CreateProject_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowCreateProjectDialogCommand.Execute(null);
        CreateDialog.Visibility = Visibility.Visible;
    }

    private void CancelCreate_Click(object sender, RoutedEventArgs e)
    {
        CreateDialog.Visibility = Visibility.Collapsed;
        ViewModel.CancelCreateProjectCommand.Execute(null);
    }

    private async void ConfirmCreate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.NewProjectTitle)) return;

        CreateForm.Visibility = Visibility.Collapsed;
        CreateLoading.Visibility = Visibility.Visible;

        await ViewModel.CreateProjectCommand.ExecuteAsync(null);

        CreateDialog.Visibility = Visibility.Collapsed;
        CreateForm.Visibility = Visibility.Visible;
        CreateLoading.Visibility = Visibility.Collapsed;
    }

    private void ProjectItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Project project)
        {
            ViewModel.SelectedProject = project;
            ActionsDialog.Visibility = Visibility.Visible;
        }
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
}
