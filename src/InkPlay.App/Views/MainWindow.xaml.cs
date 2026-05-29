using InkPlay.App.Services;
using InkPlay.App.ViewModels;
using InkPlay.Core.Interfaces;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InkPlay.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly NavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private bool _isNavigating;

    public MainWindow(MainViewModel viewModel, NavigationService navigationService, ISettingsService settingsService)
    {
        _viewModel = viewModel;
        _navigationService = navigationService;
        _settingsService = settingsService;

        InitializeComponent();

        _navigationService.RegisterPage("Home", typeof(Pages.HomePage));
        _navigationService.RegisterPage("Characters", typeof(Pages.CharactersPage));
        _navigationService.RegisterPage("Script", typeof(Pages.ScriptPage));
        _navigationService.RegisterPage("VideoGeneration", typeof(Pages.VideoGenerationPage));
        _navigationService.RegisterPage("Voices", typeof(Pages.VoicesPage));
        _navigationService.RegisterPage("ScriptManagement", typeof(Pages.ScriptManagementPage));
        _navigationService.RegisterPage("AiAssistant", typeof(Pages.AiAssistantPage));
        _navigationService.RegisterPage("Settings", typeof(Pages.SettingsPage));

        _navigationService.Initialize(ContentFrame);
        _navigationService.Navigated += OnNavigated;

        NavView.SelectedItem = NavView.MenuItems[0];
        _navigationService.NavigateTo("Home");

        Title = "墨戏 - AI创作助手";

        // Apply saved theme
        ApplyTheme(_settingsService.GetTheme());
    }

    public void ApplyTheme(string theme)
    {
        var elementTheme = theme switch
        {
            "Dark" => ElementTheme.Dark,
            "Light" => ElementTheme.Light,
            _ => ElementTheme.Default
        };

        // Apply to NavigationView (root element)
        NavView.RequestedTheme = elementTheme;

        // Apply to window title bar
        var titleBar = AppWindow?.TitleBar;
        if (titleBar is not null)
        {
            var isDark = theme == "Dark" || (theme == "Default" && Application.Current.RequestedTheme == ApplicationTheme.Dark);
            titleBar.ButtonForegroundColor = isDark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
        }
    }

    private void OnNavigated(string pageKey, object? parameter)
    {
        _isNavigating = true;
        foreach (NavigationViewItem item in NavView.MenuItems)
        {
            if (item.Tag is string tag && tag == pageKey)
            {
                NavView.SelectedItem = item;
                _isNavigating = false;
                return;
            }
        }

        // Check footer items
        foreach (NavigationViewItem item in NavView.FooterMenuItems)
        {
            if (item.Tag is string tag && tag == pageKey)
            {
                NavView.SelectedItem = item;
                _isNavigating = false;
                return;
            }
        }
        _isNavigating = false;
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
        if (_isNavigating) return;
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            _navigationService.NavigateTo(tag);
        }
    }
}
