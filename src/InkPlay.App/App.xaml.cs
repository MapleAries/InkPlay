using InkPlay.Core.Interfaces;
using InkPlay.Services.Ai;
using InkPlay.Services.Ai.Providers;
using InkPlay.Services.Data;
using InkPlay.Services.Data.Repositories;
using InkPlay.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace InkPlay.App;

public partial class App : Application
{
    public IHost Host { get; }

    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Database
                services.AddSingleton<InkPlayDbContext>();

                // Repositories
                services.AddSingleton<IProjectRepository, ProjectRepository>();
                services.AddSingleton<IDocumentRepository, DocumentRepository>();
                services.AddSingleton<ICharacterRepository, CharacterRepository>();
                services.AddSingleton<ICharacterRelationshipRepository, CharacterRelationshipRepository>();
                services.AddSingleton<IConversationRepository, ConversationRepository>();

                // AI
                services.AddHttpClient<ClaudeProvider>();
                services.AddHttpClient<OpenAiProvider>();
                services.AddHttpClient<QwenProvider>();
                services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

                // Video
                services.AddHttpClient<KlingVideoProvider>();
                services.AddSingleton<IVideoProvider, KlingVideoProvider>();

                // Settings
                services.AddSingleton<ISettingsService, SettingsService>();

                // Navigation
                services.AddSingleton<Services.INavigationService, Services.NavigationService>();
                services.AddSingleton<Services.NavigationService>(sp =>
                    (Services.NavigationService)sp.GetRequiredService<Services.INavigationService>());

                // ViewModels
                services.AddTransient<ViewModels.MainViewModel>();
                services.AddTransient<ViewModels.HomeViewModel>();
                services.AddTransient<ViewModels.EditorViewModel>();
                services.AddTransient<ViewModels.AiAssistantViewModel>();
                services.AddTransient<ViewModels.SettingsViewModel>();
                services.AddTransient<ViewModels.CharactersViewModel>();
                services.AddTransient<ViewModels.ScriptViewModel>();
                services.AddTransient<ViewModels.VideoGenerationViewModel>();

                // Views
                services.AddTransient<Views.MainWindow>();
                services.AddTransient<Views.Pages.HomePage>();
                services.AddTransient<Views.Pages.EditorPage>();
                services.AddTransient<Views.Pages.AiAssistantPage>();
                services.AddTransient<Views.Pages.SettingsPage>();
                services.AddTransient<Views.Pages.CharactersPage>();
                services.AddTransient<Views.Pages.ScriptPage>();
                services.AddTransient<Views.Pages.VideoGenerationPage>();
            })
            .Build();

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var db = Host.Services.GetRequiredService<InkPlayDbContext>();
        DatabaseInitializer.Initialize(db);

        var mainWindow = Host.Services.GetRequiredService<Views.MainWindow>();
        mainWindow.Activate();
    }
}
