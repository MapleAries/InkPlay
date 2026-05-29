using InkPlay.App.Services;
using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace InkPlay.App.Views.Pages;

public sealed partial class ScriptPage : Page, IParameterizedPage
{
    public ScriptViewModel ViewModel { get; }
    private readonly NavigationService _navigationService;
    private bool _isEditMode;

    public ScriptPage(ScriptViewModel viewModel, NavigationService navigationService)
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

    private void AddEpisode_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateEpisodeCommand.Execute(null);
    }

    private void EpisodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            ViewModel.SelectEpisodeCommand.Execute(listView.SelectedItem);
            _isEditMode = false;
            UpdateEditorVisibility();
        }
    }

    private void EditToggle_Click(object sender, RoutedEventArgs e)
    {
        _isEditMode = !_isEditMode;
        UpdateEditorVisibility();
    }

    private void UpdateEditorVisibility()
    {
        if (PreviewArea != null)
            PreviewArea.Visibility = _isEditMode ? Visibility.Collapsed : Visibility.Visible;
        if (EditPanel != null)
            EditPanel.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;
        if (EditToggleBtn != null)
            EditToggleBtn.Content = _isEditMode ? "预览" : "编辑";
    }

    private void SaveOutline_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveEpisodeContentCommand.Execute(null);
        _isEditMode = false;
        UpdateEditorVisibility();
    }

    private void AddScene_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateSceneCommand.Execute(null);
    }

    private void SceneList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            ViewModel.SelectSceneCommand.Execute(listView.SelectedItem);
        }
    }

    private void SaveScene_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveSceneCommand.Execute(null);
    }

    private void DeleteScene_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteSceneCommand.Execute(null);
    }

    private void AddDialogue_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddDialogueAsyncCommand.Execute(null);
    }

    private void RemoveDialogue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is SceneDialogue dialogue)
        {
            ViewModel.RemoveDialogueAsyncCommand.Execute(dialogue);
        }
    }

    private void QuickAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string action)
        {
            switch (action)
            {
                case "outline":
                    ViewModel.GenerateOutlineCommand.Execute(null);
                    break;
                case "expand":
                    ViewModel.ExpandPlotCommand.Execute(null);
                    break;
                case "scene":
                    ViewModel.GenerateSceneDescriptionCommand.Execute(null);
                    break;
            }
        }
    }

    private void SendAi_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SendAiMessageCommand.Execute(null);
    }

    private void AiInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.SendAiMessageCommand.Execute(null);
            e.Handled = true;
        }
    }
}
