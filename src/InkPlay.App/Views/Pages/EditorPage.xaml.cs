using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace InkPlay.App.Views.Pages;

public sealed partial class EditorPage : Page, IParameterizedPage
{
    public EditorViewModel ViewModel { get; }

    public EditorPage(EditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }

    public void SetParameter(object? parameter)
    {
        ViewModel.NavigatedTo(parameter);
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
            Editor?.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, doc.Content);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorContent();
        ViewModel.SaveDocumentCommand.Execute(null);
    }

    private void Editor_TextChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RichEditBox richEditBox)
        {
            string text = string.Empty;
            richEditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out text);
            ViewModel.DocumentContent = text ?? string.Empty;
        }
    }

    private void ContinueWriting_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorContent();
        ViewModel.AiContinueWritingCommand.Execute(null);
    }

    private void Rewrite_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorContent();
        ViewModel.AiRewriteCommand.Execute(null);
    }

    private void Polish_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorContent();
        ViewModel.AiPolishCommand.Execute(null);
    }

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorContent();
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

    private void SyncEditorContent()
    {
        string text = string.Empty;
        Editor?.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out text);
        ViewModel.DocumentContent = text ?? string.Empty;
    }
}
