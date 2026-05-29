using InkPlay.App.Services;
using InkPlay.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace InkPlay.App.Views.Pages;

public sealed partial class VoicesPage : Page, IParameterizedPage
{
    public VoicesViewModel ViewModel { get; }
    private readonly NavigationService _navigationService;

    public VoicesPage(VoicesViewModel viewModel, NavigationService navigationService)
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

    private void AddVoice_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateVoiceCommand.Execute(null);
    }

    private void VoiceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            ViewModel.SelectVoiceCommand.Execute(listView.SelectedItem);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveVoiceCommand.Execute(null);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteVoiceCommand.Execute(null);
    }

    private void QuickAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string action)
        {
            ViewModel.QuickActionCommand.Execute(action);
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
