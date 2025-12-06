//-----------------------------------------------------------------------
// Character Update Service Interface
//-----------------------------------------------------------------------

#nullable enable

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Service for managing character updates from ESI
    /// Handles position updates, online status, fleet info, etc.
    /// </summary>
    public interface ICharacterUpdateService
    {
        /// <summary>
        /// Start the character update service
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stop the character update service
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Gets whether the service is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Get the update interval for character data
        /// </summary>
        TimeSpan UpdateInterval { get; }

        /// <summary>
        /// Force an immediate update of all characters
        /// </summary>
        Task ForceUpdateAsync();
    }
}
