using InkPlay.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace InkPlay.App.Views.Pages;

public sealed partial class AiAssistantPage : Page
{
    public AiAssistantViewModel ViewModel { get; }

    public AiAssistantPage(AiAssistantViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.QuickActionCommand.Execute("continue");
    }

    private void Rewrite_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.QuickActionCommand.Execute("rewrite");
    }

    private void Polish_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.QuickActionCommand.Execute("polish");
    }

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.QuickActionCommand.Execute("expand");
    }

    private void Summarize_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.QuickActionCommand.Execute("summarize");
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearChatCommand.Execute(null);
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SendMessageCommand.Execute(null);
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyResponseCommand.Execute(null);
    }

    private void UserInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.SendMessageCommand.Execute(null);
        }
    }
}
