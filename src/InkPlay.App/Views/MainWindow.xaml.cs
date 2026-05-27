using InkPlay.App.Services;
using InkPlay.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InkPlay.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly NavigationService _navigationService;

    public MainWindow(MainViewModel viewModel, NavigationService navigationService)
    {
        _viewModel = viewModel;
        _navigationService = navigationService;

        InitializeComponent();

        // Register pages
        _navigationService.RegisterPage("Home", typeof(Pages.HomePage));
        _navigationService.RegisterPage("Editor", typeof(Pages.EditorPage));
        _navigationService.RegisterPage("AiAssistant", typeof(Pages.AiAssistantPage));
        _navigationService.RegisterPage("Settings", typeof(Pages.SettingsPage));

        _navigationService.Initialize(ContentFrame);

        // Navigate to home on startup
        NavView.SelectedItem = NavView.MenuItems[0];
        _navigationService.NavigateTo("Home");

        Title = "InkPlay - AI写作助手";
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            _navigationService.NavigateTo(tag);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            _navigationService.NavigateTo(tag);
        }
    }
}
