//-----------------------------------------------------------------------
// EVE Configuration - Modern Configuration System
//-----------------------------------------------------------------------

namespace SMT.EVEData.Configuration
{
    /// <summary>
    /// Configuration settings for EVE Online integration
    /// </summary>
    public class EveConfiguration
    {
        public const string SectionName = "Eve";

        /// <summary>
        /// EVE Online application settings
        /// </summary>
        public EveApplicationSettings Application { get; set; } = new();

        /// <summary>
        /// OAuth and authentication settings  
        /// </summary>
        public EveAuthenticationSettings Authentication { get; set; } = new();

        /// <summary>
        /// Storage and file system settings
        /// </summary>
        public EveStorageSettings Storage { get; set; } = new();

        /// <summary>
        /// Update intervals and timing settings
        /// </summary>
        public EveTimingSettings Timing { get; set; } = new();
    }

    public class EveApplicationSettings
    {
        /// <summary>
        /// SMT Version Tagline
        /// </summary>
        public string Title { get; set; } = "Rock Whisperer";

        /// <summary>
        /// SMT Version identifier
        /// </summary>
        public string Version { get; set; } = "SMT_143";

        /// <summary>
        /// User agent string for ESI requests
        /// </summary>
        public string UserAgent => $"SMT-map-app : {Version}";
    }

    public class EveAuthenticationSettings
    {
        /// <summary>
        /// Client ID from the EVE Developer setup (should be in user secrets or environment)
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// OAuth callback URL
        /// </summary>
        public string CallbackUrl { get; set; } = "http://localhost:8762/callback/";

        /// <summary>
        /// ESI API base URL
        /// </summary>
        public string EsiUrl { get; set; } = "https://esi.evetech.net";

        /// <summary>
        /// Required ESI scopes for the application
        /// </summary>
        public List<string> RequiredScopes { get; set; } = new()
        {
            "publicData",
            "esi-location.read_location.v1",
            "esi-search.search_structures.v1",
            "esi-clones.read_clones.v1",
            "esi-ui.write_waypoint.v1",
            "esi-characters.read_standings.v1",
            "esi-location.read_online.v1",
            "esi-characters.read_fatigue.v1",
            "esi-corporations.read_contacts.v1",
            "esi-alliances.read_contacts.v1",
            "esi-universe.read_structures.v1",
            "esi-fleets.read_fleet.v1"
        };
    }

    public class EveStorageSettings
    {
        private string _storageRoot = string.Empty;

        /// <summary>
        /// Root folder for storing application data
        /// </summary>
        public string StorageRoot 
        { 
            get => string.IsNullOrWhiteSpace(_storageRoot) ? GetDefaultStorageRoot() : _storageRoot;
            set => _storageRoot = value;
        }

        /// <summary>
        /// Versioned storage folder (combines root + version)
        /// </summary>
        public string VersionStorage => Path.Combine(StorageRoot, ApplicationVersion);

        /// <summary>
        /// Application version for storage path (set by DI)
        /// </summary>
        internal string ApplicationVersion { get; set; } = "SMT_143";

        private static string GetDefaultStorageRoot()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "SMT"
            );
        }
    }

    public class EveTimingSettings
    {
        /// <summary>
        /// How often to update character information
        /// </summary>
        public TimeSpan CharacterUpdateRate { get; set; } = TimeSpan.FromSeconds(4);

        /// <summary>
        /// How often to perform low frequency updates
        /// </summary>
        public TimeSpan LowFrequencyUpdateRate { get; set; } = TimeSpan.FromMinutes(20);

        /// <summary>
        /// How often to update SOV campaign information
        /// </summary>
        public TimeSpan SovCampaignUpdateRate { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How often to update Dotlan information
        /// </summary>
        public TimeSpan DotlanUpdateRate { get; set; } = TimeSpan.FromMinutes(30);
    }
}
