using System.Text.Json;
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

    public ScriptPage(ScriptViewModel viewModel, NavigationService navigationService)
    {
        ViewModel = viewModel;
        _navigationService = navigationService;
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Load the Markdown editor HTML
        var htmlPath = System.IO.Path.Combine(
            AppContext.BaseDirectory, "Assets", "markdown-editor.html");
        if (System.IO.File.Exists(htmlPath))
        {
            MarkdownEditor.CoreWebView2?.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
        }
        else
        {
            // Fallback: load from package
            MarkdownEditor.CoreWebView2?.NavigateToString(GetFallbackHtml());
        }

        MarkdownEditor.CoreWebView2Initialized += (_, _) =>
        {
            MarkdownEditor.CoreWebView2.WebMessageReceived += (s, args) =>
            {
                try
                {
                    var json = args.WebMessageAsJson;
                    var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("type", out var typeProp))
                    {
                        var type = typeProp.GetString();
                        if (type == "contentChanged" && root.TryGetProperty("markdown", out var mdProp))
                        {
                            var markdown = mdProp.GetString() ?? "";
                            ViewModel.EpisodeContent = markdown;
                        }
                    }
                }
                catch { }
            };
        };
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
            // Load content into WebView2
            if (ViewModel.CurrentEpisode is not null)
            {
                LoadContentToEditor(ViewModel.EpisodeContent);
            }
        }
    }

    private async void LoadContentToEditor(string markdown)
    {
        if (MarkdownEditor.CoreWebView2 is null) return;

        var escaped = markdown.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "");
        await MarkdownEditor.CoreWebView2.ExecuteScriptAsync($"loadMarkdown('{escaped}')");
    }

    private void SaveOutline_Click(object sender, RoutedEventArgs e)
    {
        // Get content from WebView2 before saving
        _ = GetContentFromEditor();
    }

    private async Task GetContentFromEditor()
    {
        if (MarkdownEditor.CoreWebView2 is null) return;

        var result = await MarkdownEditor.CoreWebView2.ExecuteScriptAsync("htmlToMd(editor.innerHTML)");
        if (!string.IsNullOrEmpty(result) && result != "null")
        {
            // Remove JSON quotes
            var markdown = System.Text.Json.JsonSerializer.Deserialize<string>(result) ?? "";
            ViewModel.EpisodeContent = markdown;
        }
        ViewModel.SaveEpisodeContentCommand.Execute(null);
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

    private static string GetFallbackHtml()
    {
        return @"<!DOCTYPE html><html><body style='background:#1e1e1e;color:#d4d4d4;font-family:monospace;padding:16px;'>
<div contenteditable='true' id='editor' style='width:100%;height:100%;outline:none;white-space:pre-wrap;'></div>
<script>
function loadMarkdown(md){document.getElementById('editor').innerText=md;}
function htmlToMd(h){return h;}
</script></body></html>";
    }
}
