using InkPlay.App.Services;
using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace InkPlay.App.Views.Pages;

public sealed partial class CharactersPage : Page, IParameterizedPage
{
    public CharactersViewModel ViewModel { get; }
    private readonly NavigationService _navigationService;

    public CharactersPage(CharactersViewModel viewModel, NavigationService navigationService)
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

    private void AddCharacter_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateCharacterCommand.Execute(null);
    }

    private void CharacterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            ViewModel.SelectCharacterCommand.Execute(listView.SelectedItem);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveCharacterCommand.Execute(null);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteCharacterCommand.Execute(null);
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

    private void GenerateVoice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Guid characterId)
        {
            var character = ViewModel.Characters.FirstOrDefault(c => c.Id == characterId);
            if (character is not null)
            {
                ViewModel.GenerateVoiceCommand.Execute(character);
            }
        }
    }

    private void DeleteCharacterMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Guid characterId)
        {
            var character = ViewModel.Characters.FirstOrDefault(c => c.Id == characterId);
            if (character is not null)
            {
                ViewModel.SelectCharacterCommand.Execute(character);
                ViewModel.DeleteCharacterCommand.Execute(null);
            }
        }
    }
}
