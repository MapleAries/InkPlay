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
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.NavigatedTo(e.Parameter);
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
        await ViewModel.CreateProjectAsync();
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
