//-----------------------------------------------------------------------
// EVE Manager Provider - Temporary Bridge Pattern
// This provides a migration path from singleton to dependency injection
//-----------------------------------------------------------------------

#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace SMT.EVEData
{
    /// <summary>
    /// Temporary bridge to provide backward compatibility during migration from singleton to DI.
    /// This allows existing code to continue working while gradually migrating to proper dependency injection.
    /// 
    /// Usage during migration:
    /// 1. Initialize once: EveManagerProvider.Initialize(serviceProvider)
    /// 2. Existing code continues to work: EveManagerProvider.Current.SomeMethod()
    /// 3. Gradually replace with proper DI: constructor(EveManager eveManager)
    /// 4. Remove this class when migration is complete
    /// </summary>
    public static class EveManagerProvider
    {
        private static IServiceProvider? _serviceProvider;
        private static EveManager? _currentInstance;

        /// <summary>
        /// Initialize the provider with the service provider (call once at startup)
        /// </summary>
        /// <param name="serviceProvider">The configured service provider containing EveManager</param>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _currentInstance = null; // Reset cached instance
        }

        /// <summary>
        /// Get the current EveManager instance (replaces EveManager.Instance)
        /// </summary>
        public static EveManager Current
        {
            get
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException(
                        "EveManagerProvider not initialized. Call EveManagerProvider.Initialize(serviceProvider) at startup.");
                }

                // Use lazy initialization to get the instance from DI
                if (_currentInstance == null)
                {
                    _currentInstance = _serviceProvider.GetRequiredService<EveManager>();
                }

                return _currentInstance;
            }
        }

        /// <summary>
        /// Check if the provider has been initialized
        /// </summary>
        public static bool IsInitialized => _serviceProvider != null;

        /// <summary>
        /// Reset the provider (useful for testing)
        /// </summary>
        internal static void Reset()
        {
            _serviceProvider = null;
            _currentInstance = null;
        }


    }
}
