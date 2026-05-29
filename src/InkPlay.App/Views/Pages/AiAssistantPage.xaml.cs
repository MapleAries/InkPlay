using InkPlay.App.Services;
using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace InkPlay.App.Views.Pages;

public sealed partial class AiAssistantPage : Page, IParameterizedPage
{
    public AiAssistantViewModel ViewModel { get; }
    private readonly NavigationService _navigationService;

    public AiAssistantPage(AiAssistantViewModel viewModel, NavigationService navigationService)
    {
        ViewModel = viewModel;
        _navigationService = navigationService;
        InitializeComponent();
    }

    public void SetParameter(object? parameter)
    {
        ViewModel.NavigatedTo(parameter);
    }

    private void GoHome_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo("Home");
    }

    // Chapter management
    private void AddChapter_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateChapterCommand.Execute(null);
    }

    private void ChapterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            ViewModel.SelectChapterCommand.Execute(listView.SelectedItem);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveChapterCommand.Execute(null);
    }

    // Format toolbar
    private void Format_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string format)
        {
            ViewModel.InsertMarkdownCommand.Execute(format);
        }
    }

    private void EditorBox_TextChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.OnContentChanged();
    }

    // AI panel
    private void QuickAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string action)
        {
            ViewModel.QuickActionCommand.Execute(action);
        }
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SendMessageCommand.Execute(null);
    }

    private void ApplyToEditor_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyToEditorCommand.Execute(null);
    }

    private void GenerateToc_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GenerateTocCommand.Execute(null);
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExportToMarkdownCommand.Execute(null);
    }

    private void UserInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.SendMessageCommand.Execute(null);
        }
    }
}
