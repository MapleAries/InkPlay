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

    private void TitleBox_TextChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.OnContentChanged();
    }

    private void EditorBox_TextChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.OnContentChanged();
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

    private void RenameChapter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Guid chapterId)
        {
            var chapter = ViewModel.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter is not null)
            {
                ViewModel.SelectChapterCommand.Execute(chapter);
                RenameInput.Text = chapter.Title;
                RenameDialog.Visibility = Visibility.Visible;
            }
        }
    }

    private void CancelRename_Click(object sender, RoutedEventArgs e)
    {
        RenameDialog.Visibility = Visibility.Collapsed;
    }

    private async void ConfirmRename_Click(object sender, RoutedEventArgs e)
    {
        var newTitle = RenameInput.Text?.Trim();
        if (!string.IsNullOrEmpty(newTitle) && ViewModel.CurrentChapter is not null)
        {
            ViewModel.CurrentChapter.Title = newTitle;
            await ViewModel.SaveChapterCommand.ExecuteAsync(null);
            // Refresh the item in the list
            var index = ViewModel.Chapters.IndexOf(ViewModel.CurrentChapter);
            if (index >= 0)
            {
                ViewModel.Chapters[index] = ViewModel.CurrentChapter;
            }
        }
        RenameDialog.Visibility = Visibility.Collapsed;
    }

    private void DeleteChapterMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Guid chapterId)
        {
            var chapter = ViewModel.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter is not null)
            {
                ViewModel.SelectChapterCommand.Execute(chapter);
                ViewModel.DeleteChapterCommand.Execute(null);
            }
        }
    }

    private void UserInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.SendMessageCommand.Execute(null);
        }
    }
}
