using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
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

    private void AddTextKey_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddTextKeyCommand.Execute(null);
    }

    private void AddVideoKey_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddVideoKeyCommand.Execute(null);
    }

    private void EditKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string idStr && Guid.TryParse(idStr, out var id))
        {
            var config = FindKeyById(id);
            ViewModel.EditKeyCommand.Execute(config);
        }
    }

    private void SetDefault_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string idStr && Guid.TryParse(idStr, out var id))
        {
            var config = FindKeyById(id);
            ViewModel.SetDefaultCommand.Execute(config);
        }
    }

    private void DeleteKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string idStr && Guid.TryParse(idStr, out var id))
        {
            var config = FindKeyById(id);
            ViewModel.DeleteKeyCommand.Execute(config);
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelEditCommand.Execute(null);
    }

    private void SaveKey_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveKeyCommand.Execute(null);
    }

    private ApiKeyConfig? FindKeyById(Guid id)
    {
        return ViewModel.TextApiKeys.FirstOrDefault(k => k.Id == id)
            ?? ViewModel.VideoApiKeys.FirstOrDefault(k => k.Id == id);
    }
}
