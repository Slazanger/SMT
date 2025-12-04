//-----------------------------------------------------------------------
// EVE Manager Extensions - Helper methods for migration
//-----------------------------------------------------------------------

#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SMT.EVEData.Configuration;
using SMT.EVEData.Services;
using SMT.EVEData.Services;

namespace SMT.EVEData.Extensions
{
    /// <summary>
    /// Extension methods to help with EveManager migration and setup
    /// </summary>
    public static class EveManagerExtensions
    {
        /// <summary>
        /// Add EveManager to the service collection and register it as singleton
        /// </summary>
        public static IServiceCollection AddEveManager(this IServiceCollection services)
        {
            services.AddSingleton<EveManager>(provider =>
            {
                var configService = provider.GetRequiredService<IConfigurationService>();
                var logger = provider.GetRequiredService<ILogger<EveManager>>();
                var fileMonitoringService = provider.GetRequiredService<IFileMonitoringService>();
                return new EveManager(configService, logger, fileMonitoringService);
            });
            return services;
        }

        /// <summary>
        /// Initialize EveManagerProvider for backward compatibility
        /// Call this once after building the service provider
        /// </summary>
        public static IServiceProvider InitializeEveManagerProvider(this IServiceProvider serviceProvider)
        {
            EveManagerProvider.Initialize(serviceProvider);
            return serviceProvider;
        }

        /// <summary>
        /// Get EveManager from service provider and ensure it's initialized
        /// </summary>
        public static EveManager GetEveManager(this IServiceProvider serviceProvider)
        {
            var eveManager = serviceProvider.GetRequiredService<EveManager>();
            
            // Initialize the provider if not already done
            if (!EveManagerProvider.IsInitialized)
            {
                EveManagerProvider.Initialize(serviceProvider);
            }
            
            return eveManager;
        }
    }
}
