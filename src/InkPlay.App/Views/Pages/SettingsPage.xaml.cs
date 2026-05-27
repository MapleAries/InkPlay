using InkPlay.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InkPlay.App.Views.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel)
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

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveSettingsCommand.Execute(null);
    }

    private void TestClaude_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TestConnectionCommand.Execute("claude");
    }

    private void TestOpenAi_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TestConnectionCommand.Execute("openai");
    }

    private void TestQwen_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TestConnectionCommand.Execute("qwen");
    }
}
