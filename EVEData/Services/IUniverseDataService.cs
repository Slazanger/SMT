//-----------------------------------------------------------------------
// Universe Data Service Interface
//-----------------------------------------------------------------------

#nullable enable

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Service for managing EVE universe data updates from ESI
    /// Handles kills, jumps, SOV, incursions, server info, etc.
    /// </summary>
    public interface IUniverseDataService
    {
        /// <summary>
        /// Start the universe data update service
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stop the universe data update service
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Gets whether the service is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Get the update interval for low frequency data
        /// </summary>
        TimeSpan LowFrequencyUpdateInterval { get; }

        /// <summary>
        /// Get the update interval for SOV campaign data
        /// </summary>
        TimeSpan SovCampaignUpdateInterval { get; }

        /// <summary>
        /// Force an immediate update of all universe data
        /// </summary>
        Task ForceUniverseDataUpdateAsync();

        /// <summary>
        /// Force an immediate update of SOV campaign data
        /// </summary>
        Task ForceSovCampaignUpdateAsync();

        /// <summary>
        /// Force an immediate update of Dotlan data
        /// </summary>
        Task ForceDotlanUpdateAsync();
    }
}
