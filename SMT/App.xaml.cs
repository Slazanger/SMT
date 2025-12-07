#nullable enable
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SMT.EVEData;
using SMT.EVEData.Extensions;

namespace SMT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        public static bool IsInitialized { get; private set; } = false;

        public App()
        {
            System.Console.WriteLine("=== APP CONSTRUCTOR CALLED ===");
            System.Diagnostics.Debug.WriteLine("=== APP CONSTRUCTOR CALLED ===");
        }

        /// <summary>
        /// Get EveManager instance from DI container (replaces EveManagerProvider.Current)
        /// </summary>
        public static EveManager? GetEveManager()
        {
            if (ServiceProvider == null || !IsInitialized)
                return null;
            return ServiceProvider.GetRequiredService<EveManager>();
        }

        /// <summary>
        /// Check if the application is fully initialized and ready to use
        /// </summary>
        public static bool IsReady => ServiceProvider != null && IsInitialized;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Add immediate console output before try-catch
            System.Console.WriteLine("=== OnStartup METHOD ENTERED ===");
            System.Diagnostics.Debug.WriteLine("=== OnStartup METHOD ENTERED ===");
            
            try
            {
                // Make debug output more visible
                System.Diagnostics.Debug.WriteLine("=== APP.ONSTARTUP STARTING ===");
                System.Console.WriteLine("=== APP.ONSTARTUP STARTING ===");
                
                System.Console.WriteLine("Calling base.OnStartup(e)...");
                base.OnStartup(e);
                System.Console.WriteLine("base.OnStartup(e) completed");

                System.Diagnostics.Debug.WriteLine("Creating configuration...");
                System.Console.WriteLine("Creating configuration...");
                
                // Setup dependency injection container
                var configuration = ServiceCollectionExtensions.CreateEveDataConfiguration();
                
                System.Diagnostics.Debug.WriteLine("Configuration created successfully");
                System.Console.WriteLine("Configuration created successfully");

                System.Diagnostics.Debug.WriteLine("Setting up services...");
                var services = new ServiceCollection();
                services.AddEveDataServices(configuration);
                System.Diagnostics.Debug.WriteLine("Services configured successfully");

                System.Diagnostics.Debug.WriteLine("Building ServiceProvider...");
                ServiceProvider = services.BuildServiceProvider();
                System.Diagnostics.Debug.WriteLine($"ServiceProvider created: {ServiceProvider != null}");

                System.Diagnostics.Debug.WriteLine("Initializing EveManagerProvider...");
                // Initialize EveManagerProvider with the DI container
                SMT.EVEData.EveManagerProvider.Initialize(ServiceProvider);
                System.Diagnostics.Debug.WriteLine("EveManagerProvider initialized successfully");

                try
                {
                    System.Diagnostics.Debug.WriteLine("Validating configuration...");
                    // Validate configuration but don't fail hard - show warning instead
                    ServiceProvider.ValidateEveConfiguration();
                    System.Diagnostics.Debug.WriteLine("Configuration validation passed");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuration validation warning: {ex.Message}");
                    MessageBox.Show(
                        $"Configuration validation warning: {ex.Message}\n\n" +
                        "Some features may not work correctly. Please setup user secrets:\n" +
                        "dotnet user-secrets set \"Eve:Authentication:ClientId\" \"your-client-id\"",
                        "Configuration Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                // Mark as initialized
                IsInitialized = true;
                System.Diagnostics.Debug.WriteLine($"=== APP.ONSTARTUP COMPLETED - ServiceProvider: {ServiceProvider != null}, IsInitialized: {IsInitialized} ===");
                System.Console.WriteLine($"=== APP.ONSTARTUP COMPLETED - ServiceProvider: {ServiceProvider != null}, IsInitialized: {IsInitialized} ===");
            }
            catch (Exception ex)
            {
                var errorMsg = $"FATAL ERROR in App.OnStartup: {ex.Message}";
                var stackTrace = $"Stack trace: {ex.StackTrace}";
                
                System.Diagnostics.Debug.WriteLine($"=== {errorMsg} ===");
                System.Diagnostics.Debug.WriteLine(stackTrace);
                System.Console.WriteLine($"=== {errorMsg} ===");
                System.Console.WriteLine(stackTrace);
                
                // Show error immediately
                MessageBox.Show(
                    $"Critical startup error: {ex.Message}\n\n" +
                    $"Inner Exception: {ex.InnerException?.Message}\n\n" +
                    $"Stack trace:\n{ex.StackTrace}\n\n" +
                    "The application cannot start. Please check the configuration and try again.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Application.Current.Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceProvider?.GetService<IServiceScope>()?.Dispose();
            base.OnExit(e);
        }
    }
}