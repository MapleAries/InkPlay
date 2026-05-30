using InkPlay.Core.Interfaces;
using InkPlay.Services;
using InkPlay.Services.Agents;
using InkPlay.Services.Ai;
using InkPlay.Services.Ai.Providers;
using InkPlay.Services.Data;
using InkPlay.Services.Data.Repositories;
using InkPlay.Services.Export;
using InkPlay.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace InkPlay.App;

public partial class App : Application
{
    public IHost Host { get; }
    public static Window? MainWindow { get; private set; }

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
                services.AddSingleton<IVoiceRepository, VoiceRepository>();
                services.AddSingleton<IGlossaryRepository, GlossaryRepository>();
                services.AddSingleton<IWorldSettingRepository, WorldSettingRepository>();
                services.AddSingleton<IDocumentVersionRepository, DocumentVersionRepository>();

                // AI
                services.AddHttpClient<ClaudeProvider>();
                services.AddHttpClient<OpenAiProvider>();
                services.AddHttpClient<QwenProvider>();
                services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

                // Video
                services.AddSingleton<IVideoProvider>(sp =>
                {
                    var httpClient = new HttpClient();
                    return new KlingVideoProvider(httpClient);
                });

                // Context
                services.AddSingleton<IProjectContext, ProjectContext>();

                // Export
                services.AddSingleton<IExportService, ExportService>();

                // Agents
                services.AddSingleton<IAgent, ContextAgent>();
                services.AddSingleton<IAgent, DataAgent>();
                services.AddSingleton<IAgent, PlannerAgent>();
                services.AddSingleton<IAgent, ScreenwriterAgent>();
                services.AddSingleton<IAgent, ArchitectAgent>();
                services.AddSingleton<IAgent, WriterAgent>();
                services.AddSingleton<IAgent, ProofreaderAgent>();
                services.AddSingleton<IAgent, AuditorAgent>();
                services.AddSingleton<IAgent, ReviserAgent>();
                services.AddSingleton<IOrchestrator, Orchestrator>();

                // File Service
                services.AddSingleton<IFileProjectService, FileProjectService>();

                // Settings
                services.AddSingleton<ISettingsService, SettingsService>();

                // Navigation
                services.AddSingleton<Services.INavigationService, Services.NavigationService>();
                services.AddSingleton<Services.NavigationService>(sp =>
                    (Services.NavigationService)sp.GetRequiredService<Services.INavigationService>());

                // ViewModels
                services.AddTransient<ViewModels.MainViewModel>();
                services.AddTransient<ViewModels.HomeViewModel>();
                services.AddTransient<ViewModels.AiAssistantViewModel>();
                services.AddTransient<ViewModels.SettingsViewModel>();
                services.AddTransient<ViewModels.CharactersViewModel>();
                services.AddTransient<ViewModels.ScriptViewModel>();
                services.AddTransient<ViewModels.VideoGenerationViewModel>();
                services.AddTransient<ViewModels.ScriptManagementViewModel>();
                services.AddTransient<ViewModels.VoicesViewModel>();
                services.AddTransient<ViewModels.AssetsViewModel>();

                // Views
                services.AddTransient<Views.MainWindow>();
                services.AddTransient<Views.Pages.HomePage>();
                services.AddTransient<Views.Pages.AiAssistantPage>();
                services.AddTransient<Views.Pages.SettingsPage>();
                services.AddTransient<Views.Pages.CharactersPage>();
                services.AddTransient<Views.Pages.ScriptPage>();
                services.AddTransient<Views.Pages.VideoGenerationPage>();
                services.AddTransient<Views.Pages.ScriptManagementPage>();
                services.AddTransient<Views.Pages.VoicesPage>();
                services.AddTransient<Views.Pages.AssetsPage>();
            })
            .Build();

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var db = Host.Services.GetRequiredService<InkPlayDbContext>();
        DatabaseInitializer.Initialize(db);

        var mainWindow = Host.Services.GetRequiredService<Views.MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Activate();
    }
}
