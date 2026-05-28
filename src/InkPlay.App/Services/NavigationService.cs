using InkPlay.App.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InkPlay.App.Services;

public interface INavigationService
{
    bool CanGoBack { get; }
    event Action<string, object?>? Navigated;
    void Initialize(Frame frame);
    bool NavigateTo(string pageKey, object? parameter = null);
    void GoBack();
}

public class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _pageTypes = new();

    public bool CanGoBack => _frame?.CanGoBack ?? false;
    public event Action<string, object?>? Navigated;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;
        _frame.Navigated += OnNavigated;
    }

    public void RegisterPage(string key, Type pageType)
    {
        _pageTypes[key] = pageType;
    }

    public bool NavigateTo(string pageKey, object? parameter = null)
    {
        if (_frame is null || !_pageTypes.TryGetValue(pageKey, out var pageType))
            return false;

        // Resolve page from DI
        var page = _serviceProvider.GetService(pageType) as Page;
        if (page is null)
            return false;

        // Pass parameter to page if it supports it
        if (page is IParameterizedPage parameterizedPage)
        {
            parameterizedPage.SetParameter(parameter);
        }

        _frame.Content = page;
        Navigated?.Invoke(pageKey, parameter);
        return true;
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        // Update back button visibility if needed
    }
}
