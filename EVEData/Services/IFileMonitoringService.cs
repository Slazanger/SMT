//-----------------------------------------------------------------------
// File Monitoring Service Interface
//-----------------------------------------------------------------------

#nullable enable
using SMT.EVEData.Events;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Service for monitoring EVE log files (chat logs and game logs)
    /// Replaces the file monitoring functionality that was embedded in EveManager
    /// </summary>
    public interface IFileMonitoringService
    {
        /// <summary>
        /// Start monitoring EVE log files for changes
        /// </summary>
        /// <param name="eveLogFolder">Base EVE log folder (usually Documents/EVE/Logs)</param>
        /// <param name="intelFilters">Channel names to monitor for intel</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartMonitoringAsync(string eveLogFolder, IEnumerable<string> intelFilters, CancellationToken cancellationToken);

        /// <summary>
        /// Stop monitoring and cleanup resources
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Raised when an intel file (chat log) is changed
        /// </summary>
        event EventHandler<IntelFileChangedEventArgs>? IntelFileChanged;

        /// <summary>
        /// Raised when a game log file is changed
        /// </summary>
        event EventHandler<GameLogFileChangedEventArgs>? GameLogFileChanged;

        /// <summary>
        /// Gets whether the service is currently monitoring files
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Gets the current EVE log folder being monitored
        /// </summary>
        string? CurrentLogFolder { get; }
    }
}
