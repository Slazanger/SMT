//-----------------------------------------------------------------------
// Service Collection Extensions for Dependency Injection Setup
//-----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SMT.EVEData.Configuration;
using SMT.EVEData.Services;

namespace SMT.EVEData.Extensions
{
    /// <summary>
    /// Extension methods for setting up EVE Data services in DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add EVE Data services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEveDataServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Configure EVE settings
            services.Configure<EveConfiguration>(
                configuration.GetSection(EveConfiguration.SectionName));

            // Register core services
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Register EveManager as singleton
            services.AddEveManager();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
                builder.AddDebug();
            });

            return services;
        }

        /// <summary>
        /// Create and configure an IConfiguration instance for EVE Data
        /// </summary>
        /// <param name="basePath">Optional base path for configuration files</param>
        /// <returns>Configured IConfiguration instance</returns>
        public static IConfiguration CreateEveDataConfiguration(string? basePath = null)
        {
            var builder = new ConfigurationBuilder();

            if (!string.IsNullOrEmpty(basePath))
            {
                builder.SetBasePath(basePath);
            }

            return builder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("SMT_")
                .AddUserSecrets<EveConfiguration>() // For storing sensitive settings like ClientId
                .Build();
        }

        /// <summary>
        /// Validate EVE configuration at startup
        /// </summary>
        /// <param name="serviceProvider">The configured service provider</param>
        /// <throws>InvalidOperationException if configuration is invalid</throws>
        public static void ValidateEveConfiguration(this IServiceProvider serviceProvider)
        {
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();
            
            if (!configService.IsConfigurationValid(out var errors))
            {
                var errorMessage = "Invalid EVE configuration:\n" + string.Join("\n", errors);
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}
