//-----------------------------------------------------------------------
// Configuration Service Implementation
//-----------------------------------------------------------------------

#nullable enable
using Microsoft.Extensions.Options;
using SMT.EVEData.Configuration;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Service for accessing application configuration settings
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly EveConfiguration _eveSettings;

        public ConfigurationService(IOptions<EveConfiguration> eveSettings)
        {
            _eveSettings = eveSettings.Value;

            // Set the application version for storage paths
            _eveSettings.Storage.ApplicationVersion = _eveSettings.Application.Version;
        }

        /// <summary>
        /// EVE Online related configuration settings
        /// </summary>
        public EveConfiguration EveSettings => _eveSettings;

        /// <summary>
        /// Get the storage root path, creating it if it doesn't exist
        /// </summary>
        public string GetStorageRoot()
        {
            var path = _eveSettings.Storage.StorageRoot;
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Get the versioned storage path, creating it if it doesn't exist
        /// </summary>
        public string GetVersionedStoragePath()
        {
            var path = _eveSettings.Storage.VersionStorage;
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Get the user agent string for HTTP requests
        /// </summary>
        public string GetUserAgent()
        {
            return _eveSettings.Application.UserAgent;
        }

        /// <summary>
        /// Check if the configuration is valid (required values are present)
        /// </summary>
        public bool IsConfigurationValid(out List<string> errors)
        {
            errors = new List<string>();

            // Allow legacy placeholder for backward compatibility
            if (string.IsNullOrWhiteSpace(_eveSettings.Authentication.ClientId) || 
                _eveSettings.Authentication.ClientId == "LEGACY_MODE_PLEASE_SETUP_USER_SECRETS")
            {
                errors.Add("Eve:Authentication:ClientId is required. Please set this in user secrets or environment variables.");
            }

            if (string.IsNullOrWhiteSpace(_eveSettings.Authentication.CallbackUrl))
            {
                errors.Add("Eve:Authentication:CallbackUrl is required.");
            }

            if (string.IsNullOrWhiteSpace(_eveSettings.Authentication.EsiUrl))
            {
                errors.Add("Eve:Authentication:EsiUrl is required.");
            }

            if (_eveSettings.Authentication.RequiredScopes?.Count == 0)
            {
                errors.Add("Eve:Authentication:RequiredScopes cannot be empty.");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Check if we're running in legacy compatibility mode
        /// </summary>
        public bool IsLegacyMode()
        {
            return _eveSettings.Authentication.ClientId == "LEGACY_MODE_PLEASE_SETUP_USER_SECRETS";
        }
    }
}
