using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace InkPlay.App.Views.Pages;

public sealed partial class EditorPage : Page
{
    public EditorViewModel ViewModel { get; }

    public EditorPage(EditorViewModel viewModel)
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

    private void AddDocument_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateDocumentCommand.Execute(null);
    }

    private void DocumentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is Document doc)
        {
            ViewModel.SelectDocumentCommand.Execute(doc);
            // Update RichEditBox content
            var editor = Editor;
            if (editor?.Document != null)
            {
                editor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, doc.Content);
            }
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Get text from RichEditBox
        Editor?.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out var text);
        ViewModel.DocumentContent = text ?? string.Empty;
        await ViewModel.SaveDocumentAsync();
    }

    private void Editor_TextChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RichEditBox richEditBox)
        {
            richEditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out var text);
            ViewModel.DocumentContent = text ?? string.Empty;
        }
    }

    private void ContinueWriting_Click(object sender, RoutedEventArgs e)
    {
        Editor?.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out var text);
        ViewModel.DocumentContent = text ?? string.Empty;
        ViewModel.AiContinueWritingCommand.Execute(null);
    }

    private void Rewrite_Click(object sender, RoutedEventArgs e)
    {
        Editor?.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out var text);
        ViewModel.DocumentContent = text ?? string.Empty;
        ViewModel.AiRewriteCommand.Execute(null);
    }

    private void Polish_Click(object sender, RoutedEventArgs e)
    {
        Editor?.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out var text);
        ViewModel.DocumentContent = text ?? string.Empty;
        ViewModel.AiPolishCommand.Execute(null);
    }

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        Editor?.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out var text);
        ViewModel.DocumentContent = text ?? string.Empty;
        ViewModel.AiExpandCommand.Execute(null);
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
        }
    }
}
