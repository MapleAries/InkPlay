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
        this.DataContext = ViewModel;
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

    private void SampleChapter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is Document doc)
        {
            ViewModel.SelectedSampleChapter = doc;
        }
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            WorldPanel.Visibility = Visibility.Collapsed;
            CharactersPanel.Visibility = Visibility.Collapsed;
            GlossaryPanel.Visibility = Visibility.Collapsed;
            SamplesPanel.Visibility = Visibility.Collapsed;

            switch (item.Tag?.ToString())
            {
                case "World":
                    WorldPanel.Visibility = Visibility.Visible;
                    break;
                case "Characters":
                    CharactersPanel.Visibility = Visibility.Visible;
                    break;
                case "Glossary":
                    GlossaryPanel.Visibility = Visibility.Visible;
                    break;
                case "Samples":
                    SamplesPanel.Visibility = Visibility.Visible;
                    break;
            }
        }
    }

    private void AddWorldSetting_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddWorldSettingCommand.Execute(null);
    }

    private void SaveWorldSetting_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveWorldSettingCommand.Execute(null);
    }

    private void DeleteWorldSetting_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteWorldSettingCommand.Execute(null);
    }

    private void AddGlossaryEntry_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddGlossaryEntryCommand.Execute(null);
    }

    private void SaveGlossaryEntry_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveGlossaryEntryCommand.Execute(null);
    }

    private void DeleteGlossaryEntry_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteGlossaryEntryCommand.Execute(null);
    }

    private async void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem item)
        {
            var category = item.Content?.ToString() ?? "全部";
            await ViewModel.FilterByCategoryAsync(category);
        }
    }
}
