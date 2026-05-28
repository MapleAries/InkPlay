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

    private void ConfirmCreate_Click(object sender, RoutedEventArgs e)
    {
        if (GenreComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            ViewModel.NewProjectGenre = selectedItem.Content?.ToString() ?? "短剧";
        }
        ViewModel.CreateProjectCommand.Execute(null);
        CreateDialog.Visibility = Visibility.Collapsed;
    }

    private void ProjectItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Project project)
        {
            ViewModel.OpenProjectCommand.Execute(project);
        }
    }
}
