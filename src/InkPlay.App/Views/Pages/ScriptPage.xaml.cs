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
            // Exit edit mode when switching items
            _isEditMode = false;
            UpdateEditorVisibility();
        }
    }

    private void EditToggle_Click(object sender, RoutedEventArgs e)
    {
        _isEditMode = !_isEditMode;
        UpdateEditorVisibility();

        if (!_isEditMode)
        {
            // Switching from edit to preview: update preview
            LoadPreview();
        }
    }

    private void UpdateEditorVisibility()
    {
        if (PreviewView != null)
            PreviewView.Visibility = _isEditMode ? Visibility.Collapsed : Visibility.Visible;
        if (EditPanel != null)
            EditPanel.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;
        if (EditToggleBtn != null)
            EditToggleBtn.Content = _isEditMode ? "预览" : "编辑";
    }

    private void PreviewView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is WebView2 webView)
        {
            webView.CoreWebView2Initialized += (_, _) =>
            {
                LoadPreview();
            };
        }
    }

    private void LoadPreview()
    {
        if (PreviewView?.CoreWebView2 is null) return;
        if (string.IsNullOrEmpty(ViewModel.EpisodeContent))
        {
            PreviewView.CoreWebView2.NavigateToString("<html><body style='background:#1e1e1e;color:#888;padding:16px;font-family:sans-serif;'>暂无内容</body></html>");
            return;
        }

        var html = MarkdownToHtml(ViewModel.EpisodeContent);
        PreviewView.CoreWebView2.NavigateToString(html);
    }

    private static string MarkdownToHtml(string md)
    {
        var html = md
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            // Headers
            .Replace("### ", "<h3>")
            .Replace("## ", "<h2>")
            .Replace("# ", "<h1>")
            // Bold/italic
            .Replace("***", "<strong><em>")
            .Replace("**", "<strong>")
            .Replace("*", "<em>")
            .Replace("~~", "<del>")
            // Newlines
            .Replace("\n\n", "</p><p>")
            .Replace("\n", "<br>");

        // Close tags (simple approach)
        html = html
            .Replace("<h1>", "</p><h1>")
            .Replace("<h2>", "</p><h2>")
            .Replace("<h3>", "</p><h3>");

        return $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
body {{ background:#1e1e1e; color:#d4d4d4; font-family:'Microsoft YaHei',sans-serif; padding:20px; line-height:1.8; font-size:15px; }}
h1 {{ font-size:24px; color:#fff; margin:20px 0 10px; }}
h2 {{ font-size:20px; color:#fff; margin:18px 0 8px; }}
h3 {{ font-size:17px; color:#fff; margin:14px 0 6px; }}
p {{ margin:8px 0; }}
strong {{ color:#fff; }}
em {{ font-style:italic; }}
del {{ color:#888; }}
blockquote {{ border-left:3px solid #569cd6; padding-left:12px; color:#999; margin:8px 0; }}
hr {{ border:none; border-top:1px solid #3c3c3c; margin:16px 0; }}
code {{ background:#2d2d2d; padding:2px 6px; border-radius:3px; font-family:Consolas,monospace; }}
pre {{ background:#2d2d2d; padding:12px; border-radius:4px; overflow-x:auto; }}
</style></head><body><p>{html}</p></body></html>";
    }

    private void SaveOutline_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveEpisodeContentCommand.Execute(null);
        // Switch back to preview after saving
        _isEditMode = false;
        UpdateEditorVisibility();
        LoadPreview();
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
