//-----------------------------------------------------------------------
// ZKill Feed Service Interface
//-----------------------------------------------------------------------

#nullable enable
using static SMT.EVEData.ZKillRedisQ;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Service for managing ZKillboard RedisQ feed
    /// Provides kill data updates from ZKillboard
    /// </summary>
    public interface IZKillFeedService
    {
        /// <summary>
        /// Start the ZKill feed service
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stop the ZKill feed service
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Gets whether the service is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the kill stream data
        /// </summary>
        List<ZKBDataSimple> KillStream { get; }

        /// <summary>
        /// Kill expiration time in minutes
        /// </summary>
        int KillExpireTimeMinutes { get; set; }

        /// <summary>
        /// Pause/resume updates
        /// </summary>
        bool PauseUpdate { get; set; }

        /// <summary>
        /// Event raised when new kills are added
        /// </summary>
        event EventHandler? KillsAddedEvent;

        /// <summary>
        /// Force an immediate update of the kill feed
        /// </summary>
        Task ForceUpdateAsync();
    }
}