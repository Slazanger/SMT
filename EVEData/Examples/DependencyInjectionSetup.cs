//-----------------------------------------------------------------------
// Example of how to setup Dependency Injection for EVE Data
//-----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SMT.EVEData.Extensions;
using SMT.EVEData.Services;

namespace SMT.EVEData.Examples
{
    /// <summary>
    /// Example showing how to setup dependency injection for the EVE Data library
    /// This replaces the singleton EveManager.Instance pattern
    /// </summary>
    public class DependencyInjectionSetup
    {
        /// <summary>
        /// Example 1: Console Application Setup
        /// </summary>
        public static async Task<int> ConsoleAppExample()
        {
            // Create configuration
            var configuration = ServiceCollectionExtensions.CreateEveDataConfiguration();

            // Setup DI container
            var services = new ServiceCollection();
            services.AddEveDataServices(configuration);
            
            // Register your modernized EveManager (when ready)
            // services.AddSingleton<EveManagerModern>();

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            try
            {
                // Validate configuration at startup
                serviceProvider.ValidateEveConfiguration();

                // Get your services
                var configService = serviceProvider.GetRequiredService<IConfigurationService>();
                var logger = serviceProvider.GetRequiredService<ILogger<DependencyInjectionSetup>>();

                logger.LogInformation("Application started with version {Version}", 
                    configService.EveSettings.Application.Version);

                // Use your services...
                
                return 0;
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<DependencyInjectionSetup>>();
                logger?.LogError(ex, "Application startup failed");
                return 1;
            }
            finally
            {
                if (serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Example 2: WPF Application Setup (in App.xaml.cs)
        /// </summary>
        public static class WpfAppSetup
        {
            public static ServiceProvider ConfigureServices()
            {
                var configuration = ServiceCollectionExtensions.CreateEveDataConfiguration();

                var services = new ServiceCollection();
                services.AddEveDataServices(configuration);
                
                // Add your WPF-specific services
                // services.AddSingleton<MainWindow>();
                // services.AddSingleton<EveManagerModern>();

                var serviceProvider = services.BuildServiceProvider();
                
                // Validate configuration
                serviceProvider.ValidateEveConfiguration();

                return serviceProvider;
            }
        }

        /// <summary>
        /// Example 3: Generic Host Setup (recommended for background services)
        /// </summary>
        public static IHost CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddEveDataServices(context.Configuration);
                    
                    // Add background services
                    // services.AddHostedService<CharacterUpdateService>();
                    // services.AddHostedService<IntelProcessingService>();
                })
                .Build();
    }

    /// <summary>
    /// Migration helper for existing code that uses EveManager.Instance
    /// </summary>
    public static class EveManagerMigrationHelper
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Initialize the service provider (call this once at startup)
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Temporary bridge for existing code that uses EveManager.Instance
        /// Use this to gradually migrate existing code
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("Service provider not initialized. Call Initialize() first.");
            }

            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Get configuration service (replaces EveAppConfig usage)
        /// </summary>
        public static IConfigurationService GetConfigurationService()
        {
            return GetService<IConfigurationService>();
        }
    }
}
