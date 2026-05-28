using InkPlay.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InkPlay.App.Views.Pages;

public sealed partial class VideoGenerationPage : Page
{
    public VideoGenerationViewModel ViewModel { get; }

    public VideoGenerationPage(VideoGenerationViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += (_, _) => ViewModel.NavigatedTo(null);
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GenerateVideoCommand.Execute(null);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelGenerationCommand.Execute(null);
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearHistoryCommand.Execute(null);
    }

    private void Duration_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            var duration = item.Content.ToString() switch
            {
                "3 秒" => 3,
                "5 秒" => 5,
                "10 秒" => 10,
                _ => 5
            };
            ViewModel.Duration = duration;
        }
    }

    private void Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.Resolution = item.Content.ToString() ?? "1080p";
        }
    }
}
