using InkPlay.App.Services;
using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
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
            RenderMarkdownPreview();
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

        if (!_isEditMode)
        {
            RenderMarkdownPreview();
        }
    }

    private void RenderMarkdownPreview()
    {
        if (PreviewContent == null) return;
        PreviewContent.Blocks.Clear();

        var markdown = ViewModel.EpisodeContent;
        if (string.IsNullOrWhiteSpace(markdown)) return;

        var lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("###"))
            {
                var text = line.TrimStart('#').TrimStart();
                if (!string.IsNullOrEmpty(text))
                    PreviewContent.Blocks.Add(CreateHeader(text, 17));
            }
            else if (line.StartsWith("##"))
            {
                var text = line.TrimStart('#').TrimStart();
                if (!string.IsNullOrEmpty(text))
                    PreviewContent.Blocks.Add(CreateHeader(text, 20));
            }
            else if (line.StartsWith("#"))
            {
                var text = line.TrimStart('#').TrimStart();
                if (!string.IsNullOrEmpty(text))
                    PreviewContent.Blocks.Add(CreateHeader(text, 24));
            }
            else if (line.Trim() == "---" || line.Trim() == "***")
            {
                var para = new Paragraph();
                para.Inlines.Add(new Run { Text = "────────────────────" });
                PreviewContent.Blocks.Add(para);
            }
            else if (line.StartsWith("> "))
            {
                var para = new Paragraph();
                para.Inlines.Add(new Run { Text = "│ " + line[2..] });
                PreviewContent.Blocks.Add(para);
            }
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                var para = new Paragraph();
                para.Inlines.Add(new Run { Text = "•  " + line[2..] });
                PreviewContent.Blocks.Add(para);
            }
            else
            {
                var para = new Paragraph();
                AddFormattedText(para, line);
                PreviewContent.Blocks.Add(para);
            }
        }
    }

    private static Paragraph CreateHeader(string text, double size)
    {
        var para = new Paragraph();
        para.Margin = new Thickness(0, 8, 0, 4);
        para.Inlines.Add(new Run
        {
            Text = text,
            FontSize = size,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        return para;
    }

    private static void AddFormattedText(Paragraph para, string text)
    {
        var i = 0;
        while (i < text.Length)
        {
            // Bold **text**
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2);
                if (end > i)
                {
                    para.Inlines.Add(new Run { Text = text[(i + 2)..end], FontWeight = Microsoft.UI.Text.FontWeights.Bold });
                    i = end + 2;
                    continue;
                }
            }
            // Italic *text*
            if (text[i] == '*')
            {
                var end = text.IndexOf('*', i + 1);
                if (end > i)
                {
                    para.Inlines.Add(new Run { Text = text[(i + 1)..end], FontStyle = Windows.UI.Text.FontStyle.Italic });
                    i = end + 1;
                    continue;
                }
            }
            // Strikethrough ~~text~~
            if (i + 1 < text.Length && text[i] == '~' && text[i + 1] == '~')
            {
                var end = text.IndexOf("~~", i + 2);
                if (end > i)
                {
                    para.Inlines.Add(new Run { Text = text[(i + 2)..end], TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough });
                    i = end + 2;
                    continue;
                }
            }
            // Inline code `text`
            if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    para.Inlines.Add(new Run { Text = text[(i + 1)..end], FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas") });
                    i = end + 1;
                    continue;
                }
            }
            // Normal text
            var start = i;
            while (i < text.Length && text[i] != '*' && text[i] != '~' && text[i] != '`') i++;
            if (i > start) para.Inlines.Add(new Run { Text = text[start..i] });
        }
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

    private void RenameEpisode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Guid episodeId)
        {
            var episode = ViewModel.Episodes.FirstOrDefault(ep => ep.Id == episodeId);
            if (episode is not null)
            {
                ViewModel.SelectEpisodeCommand.Execute(episode);
                RenameInput.Text = episode.Title;
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
        if (!string.IsNullOrEmpty(newTitle) && ViewModel.CurrentEpisode is not null)
        {
            ViewModel.CurrentEpisode.Title = newTitle;
            await ViewModel.SaveEpisodeContentCommand.ExecuteAsync(null);
            var index = ViewModel.Episodes.IndexOf(ViewModel.CurrentEpisode);
            if (index >= 0)
            {
                ViewModel.Episodes[index] = ViewModel.CurrentEpisode;
            }
        }
        RenameDialog.Visibility = Visibility.Collapsed;
    }

    private void DeleteEpisodeMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Guid episodeId)
        {
            var episode = ViewModel.Episodes.FirstOrDefault(ep => ep.Id == episodeId);
            if (episode is not null)
            {
                ViewModel.SelectEpisodeCommand.Execute(episode);
                ViewModel.DeleteEpisodeCommand.Execute(null);
            }
        }
    }
}
