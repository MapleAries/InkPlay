using InkPlay.App.ViewModels;
using InkPlay.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InkPlay.App.Views.Pages;

public sealed partial class AssetsPage : Page
{
    public AssetsViewModel ViewModel { get; }

    public AssetsPage(AssetsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void WorldSetting_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is WorldSetting setting)
        {
            ViewModel.SelectWorldSettingCommand.Execute(setting);
        }
    }

    private void GlossaryEntry_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is GlossaryEntry entry)
        {
            ViewModel.SelectGlossaryEntryCommand.Execute(entry);
        }
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            switch (item.Tag?.ToString())
            {
                case "World":
                    WorldPanel.Visibility = Visibility.Visible;
                    CharactersPanel.Visibility = Visibility.Collapsed;
                    GlossaryPanel.Visibility = Visibility.Collapsed;
                    SamplesPanel.Visibility = Visibility.Collapsed;
                    break;
                case "Characters":
                    WorldPanel.Visibility = Visibility.Collapsed;
                    CharactersPanel.Visibility = Visibility.Visible;
                    GlossaryPanel.Visibility = Visibility.Collapsed;
                    SamplesPanel.Visibility = Visibility.Collapsed;
                    break;
                case "Glossary":
                    WorldPanel.Visibility = Visibility.Collapsed;
                    CharactersPanel.Visibility = Visibility.Collapsed;
                    GlossaryPanel.Visibility = Visibility.Visible;
                    SamplesPanel.Visibility = Visibility.Collapsed;
                    break;
                case "Samples":
                    WorldPanel.Visibility = Visibility.Collapsed;
                    CharactersPanel.Visibility = Visibility.Collapsed;
                    GlossaryPanel.Visibility = Visibility.Collapsed;
                    SamplesPanel.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
