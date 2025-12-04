//-----------------------------------------------------------------------
// Configuration Service Interface
//-----------------------------------------------------------------------

#nullable enable
using SMT.EVEData.Configuration;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Service for accessing application configuration settings
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// EVE Online related configuration settings
        /// </summary>
        EveConfiguration EveSettings { get; }

        /// <summary>
        /// Get the storage root path, creating it if it doesn't exist
        /// </summary>
        string GetStorageRoot();

        /// <summary>
        /// Get the versioned storage path, creating it if it doesn't exist
        /// </summary>
        string GetVersionedStoragePath();

        /// <summary>
        /// Get the user agent string for HTTP requests
        /// </summary>
        string GetUserAgent();

        /// <summary>
        /// Check if the configuration is valid (required values are present)
        /// </summary>
        bool IsConfigurationValid(out List<string> errors);

        /// <summary>
        /// Check if we're running in legacy compatibility mode
        /// </summary>
        bool IsLegacyMode();
    }
}
