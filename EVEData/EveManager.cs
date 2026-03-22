//-----------------------------------------------------------------------
// EVE Manager
//-----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using EVEDataUtils;
using EVEStandard;
using EVEStandard.Enumerations;
using EVEStandard.Models;
using EVEStandard.Models.SSO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SMT.EVEData
{
    /// <summary>
    /// State for one ESI rate-limit token bucket (e.g. error limit or a rate-limit group).
    /// </summary>
    public class EsiRateLimitBucketState
    {
        public string Group { get; set; } = string.Empty;
        public int Remain { get; set; }
        public DateTime ResetAt { get; set; }
    }

    /// <summary>
    /// The main EVE Manager
    /// </summary>
    public partial class EveManager
    {
        // App-wide UI language and English→localized string map (loaded from Translation.csv)
        public static string CurrentLanguage { get; set; } = "en-US";

        public static Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// singleton instance of this class
        /// </summary>
        private static EveManager instance;

        private bool BackgroundThreadShouldTerminate;

        /// <summary>
        /// Thread-safe access lock for LocalCharacters collection
        /// </summary>
        private readonly object _localCharactersLock = new object();

        /// <summary>
        /// Observable collection for LocalCharacters that supports UI binding
        /// </summary>
        private readonly ObservableCollection<LocalCharacter> _localCharacters = new ObservableCollection<LocalCharacter>();

        /// <summary>
        /// Read position map for the intel files
        /// </summary>
        private Dictionary<string, int> intelFileReadPos;

        /// <summary>
        /// Read position map for the intel files
        /// </summary>
        private Dictionary<string, int> gameFileReadPos;

        /// <summary>
        /// Read position map for the intel files
        /// </summary>
        private Dictionary<string, string> gamelogFileCharacterMap;

        /// <summary>
        /// Lock for ESI rate-limit bucket updates and reads.
        /// </summary>
        private readonly object _esiRateLimitLock = new object();

        /// <summary>
        /// Per-group ESI token bucket state (e.g. ErrorLimit). Updated from response headers.
        /// </summary>
        private Dictionary<string, EsiRateLimitBucketState> _esiRateLimitBuckets = new Dictionary<string, EsiRateLimitBucketState>();

        /// <summary>
        /// File system watcher
        /// </summary>
        private FileSystemWatcher intelFileWatcher;

        /// <summary>
        /// File system watcher
        /// </summary>
        private FileSystemWatcher gameLogFileWatcher;

        private string VersionStr;

        private bool WatcherThreadShouldTerminate;

        private TimeSpan CharacterUpdateRate = TimeSpan.FromSeconds(2);
        private TimeSpan LowFreqUpdateRate = TimeSpan.FromMinutes(20);
        private TimeSpan SOVCampaignUpdateRate = TimeSpan.FromSeconds(30);

        private DateTime NextCharacterUpdate = DateTime.MinValue;
        private DateTime NextLowFreqUpdate = DateTime.MinValue;
        private DateTime NextSOVCampaignUpdate = DateTime.MinValue;
        private DateTime NextDotlanUpdate = DateTime.MinValue;
        private DateTime LastDotlanUpdate = DateTime.MinValue;
        private string LastDotlanETAG = "";

        public EveManager(string version)
        {
            // LocalCharacters is now initialized as a private field
            VersionStr = version;

            MigrateOldSettings();

            string SaveDataRoot = EveAppConfig.StorageRoot;
            if(!Directory.Exists(SaveDataRoot))
            {
                Directory.CreateDirectory(SaveDataRoot);
            }

            DataRootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

            SaveDataRootFolder = SaveDataRoot;

            SaveDataVersionFolder = EveAppConfig.VersionStorage;
            if(!Directory.Exists(SaveDataVersionFolder))
            {
                Directory.CreateDirectory(SaveDataVersionFolder);
            }

            string characterSaveFolder = Path.Combine(SaveDataRootFolder, "Portraits");
            if(!Directory.Exists(characterSaveFolder))
            {
                Directory.CreateDirectory(characterSaveFolder);
            }

            CharacterIDToName = new SerializableDictionary<int, string>();
            AllianceIDToName = new SerializableDictionary<int, string>();
            AllianceIDToTicker = new SerializableDictionary<int, string>();
            NameToSystem = new Dictionary<string, System>();
            IDToSystem = new Dictionary<long, System>();

            ServerInfo = new EVEData.Server();

            // Load optional Translation.csv (zh-CN strings keyed by English) at end of init
            LoadTranslations();
        }
        /// <summary>
        /// Intel Updated Event Handler
        /// </summary>
        public delegate void IntelUpdatedEventHandler(List<IntelData> idl);

        /// <summary>
        /// Intel Updated Event
        /// </summary>
        public event IntelUpdatedEventHandler IntelUpdatedEvent;

        /// <summary>
        /// GameLog Added Event Handler
        /// </summary>
        public delegate void GameLogAddedEventHandler(List<GameLogData> gll);

        /// <summary>
        /// Intel Added Event
        /// </summary>
        public event GameLogAddedEventHandler GameLogAddedEvent;

        /// <summary>
        /// Ship Decloak Event Handler
        /// </summary>
        public delegate void ShipDecloakedEventHandler(string pilot, string text);

        /// <summary>
        /// Ship Decloaked
        /// </summary>
        public event ShipDecloakedEventHandler ShipDecloakedEvent;

        /// <summary>
        /// Combat Event Handler
        /// </summary>
        public delegate void CombatEventHandler(string pilot, string text);

        /// <summary>
        /// Combat Events
        /// </summary>
        public event CombatEventHandler CombatEvent;

        public enum JumpShip
        {
            Dread,
            Carrier,
            FAX,
            Super,
            Titan,
            Blops,
            JF,
            Rorqual
        }

        /// <summary>
        /// Gets or sets the Singleton Instance of the EVEManager
        /// </summary>
        public static EveManager Instance
        {
            get
            {
                return instance;
            }

            set
            {
                EveManager.instance = value;
            }
        }

        public string EVELogFolder { get; set; }

        /// <summary>
        /// Sov Campaign Updated Event Handler
        /// </summary>
        public delegate void SovCampaignUpdatedHandler();

        /// <summary>
        /// Sov Campaign updated Added Events
        /// </summary>
        public event SovCampaignUpdatedHandler SovUpdateEvent;

        /// <summary>
        /// Thera Connections Updated Event Handler
        /// </summary>
        public delegate void TheraUpdatedHandler();

        /// <summary>
        /// Thera Updated Added Events
        /// </summary>
        public event TheraUpdatedHandler TheraUpdateEvent;

        /// <summary>
        /// Turnur Connections Updated Event Handler
        /// </summary>
        public delegate void TurnurUpdatedHandler();

        /// <summary>
        /// Turnur Updated Added Events
        /// </summary>
        public event TurnurUpdatedHandler TurnurUpdateEvent;

        /// <summary>
        /// Storms Updated Event Handler
        /// </summary>
        public delegate void StormsUpdatedHandler();

        /// <summary>
        /// Storms Updated Added Events
        /// </summary>
        public event StormsUpdatedHandler StormsUpdateEvent;

        /// <summary>
        /// Local Characters Updated Event Handler
        /// </summary>
        public delegate void LocalCharactersUpdatedHandler();

        /// <summary>
        /// Local Characters Updated Events
        /// </summary>
        public event LocalCharactersUpdatedHandler LocalCharacterUpdateEvent;

        public List<SOVCampaign> ActiveSovCampaigns { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Name dictionary
        /// </summary>
        public SerializableDictionary<int, string> AllianceIDToName { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Alliance Ticker dictionary
        /// </summary>
        public SerializableDictionary<int, string> AllianceIDToTicker { get; set; }

        /// <summary>
        /// Gets or sets the character cache
        /// </summary>
        [XmlIgnoreAttribute]
        public SerializableDictionary<string, Character> CharacterCache { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Name dictionary
        /// </summary>
        public SerializableDictionary<int, string> CharacterIDToName { get; set; }

        public EVEStandardAPI EveApiClient { get; set; }
        public SSOv2 Sso { get; set; }

        public List<string> ESIScopes { get; set; }

        /// <summary>
        /// Updates the ESI rate-limit bucket for the given group from response headers (X-Esi-Error-Limit-Remain, X-Esi-Error-Limit-Reset).
        /// </summary>
        /// <param name="group">Rate-limit group name (e.g. "ErrorLimit" for the global error limit).</param>
        /// <param name="remain">Remaining tokens/errors (e.g. from X-Esi-Error-Limit-Remain).</param>
        /// <param name="resetSeconds">Seconds until the window resets (e.g. from X-Esi-Error-Limit-Reset).</param>
        public void UpdateEsiRateLimit(string group, int remain, int resetSeconds)
        {
            if(string.IsNullOrEmpty(group)) return;
            var resetAt = DateTime.UtcNow.AddSeconds(resetSeconds);
            lock(_esiRateLimitLock)
            {
                _esiRateLimitBuckets[group] = new EsiRateLimitBucketState
                {
                    Group = group,
                    Remain = remain,
                    ResetAt = resetAt
                };
            }
        }

        /// <summary>
        /// Returns a snapshot of current ESI rate-limit bucket state per group (thread-safe).
        /// </summary>
        public List<EsiRateLimitBucketState> GetEsiRateLimitBuckets()
        {
            lock(_esiRateLimitLock)
            {
                return _esiRateLimitBuckets.Values.Select(b => new EsiRateLimitBucketState
                {
                    Group = b.Group,
                    Remain = b.Remain,
                    ResetAt = b.ResetAt
                }).ToList();
            }
        }

        /// <summary>
        /// Gets or sets the Intel List
        /// </summary>
        public FixedQueue<EVEData.IntelData> IntelDataList { get; set; }

        /// <summary>
        /// Gets or sets the Gamelog List
        /// </summary>
        public FixedQueue<EVEData.GameLogData> GameLogList { get; set; }

        /// <summary>
        /// Gets or sets the current list of Jump Bridges
        /// </summary>
        public List<JumpBridge> JumpBridges { get; set; }

        /// <summary>
        /// Gets the observable collection of Characters we are tracking (thread-safe for UI binding)
        /// </summary>
        [XmlIgnoreAttribute]
        public ObservableCollection<LocalCharacter> LocalCharacters
        {
            get
            {
                return _localCharacters;
            }
        }

        /// <summary>
        /// Delegate for UI thread operations
        /// </summary>
        public static Action<Action> UIThreadInvoker { get; set; }

        /// <summary>
        /// Thread-safe add character method
        /// </summary>
        public void AddCharacter(LocalCharacter character)
        {
            // Use UI thread invoker if available (set by UI layer)
            if(UIThreadInvoker != null)
            {
                UIThreadInvoker(() =>
                {
                    lock(_localCharactersLock)
                    {
                        _localCharacters.Add(character);
                    }
                });
            }
            else
            {
                // Direct access during initialization or from UI thread
                lock(_localCharactersLock)
                {
                    _localCharacters.Add(character);
                }
            }

            // Notify UI - ObservableCollection handles this automatically, but keep for compatibility
            LocalCharacterUpdateEvent?.Invoke();
        }

        /// <summary>
        /// Thread-safe remove character method
        /// </summary>
        public void RemoveCharacter(LocalCharacter character)
        {
            // Use UI thread invoker if available (set by UI layer)
            if(UIThreadInvoker != null)
            {
                UIThreadInvoker(() =>
                {
                    lock(_localCharactersLock)
                    {
                        _localCharacters.Remove(character);
                    }
                });
            }
            else
            {
                // Direct access during initialization or from UI thread
                lock(_localCharactersLock)
                {
                    _localCharacters.Remove(character);
                }
            }

            // Notify UI - ObservableCollection handles this automatically, but keep for compatibility
            LocalCharacterUpdateEvent?.Invoke();
        }

        /// <summary>
        /// Thread-safe iteration helper - gets a safe copy for foreach loops
        /// </summary>
        public List<LocalCharacter> GetLocalCharactersCopy()
        {
            lock(_localCharactersLock)
            {
                return new List<LocalCharacter>(_localCharacters);
            }
        }

        /// <summary>
        /// Thread-safe character lookup by name
        /// </summary>
        public LocalCharacter FindCharacterByName(string characterName)
        {
            lock(_localCharactersLock)
            {
                return _localCharacters.FirstOrDefault(c => c.Name == characterName);
            }
        }

        /// <summary>
        /// Thread-safe character count
        /// </summary>
        public int LocalCharacterCount
        {
            get
            {
                lock(_localCharactersLock)
                {
                    return _localCharacters.Count;
                }
            }
        }

        /// <summary>
        /// Thread-safe character access by index
        /// </summary>
        public LocalCharacter GetCharacterAt(int index)
        {
            lock(_localCharactersLock)
            {
                return index >= 0 && index < _localCharacters.Count ? _localCharacters[index] : null;
            }
        }

        /// <summary>
        /// Move character up in the list
        /// </summary>
        public bool MoveLocalCharacterUp(LocalCharacter character)
        {
            if(UIThreadInvoker != null)
            {
                bool result = false;
                UIThreadInvoker(() =>
                {
                    lock(_localCharactersLock)
                    {
                        int index = _localCharacters.IndexOf(character);
                        if(index > 0)
                        {
                            _localCharacters.Move(index, index - 1);
                            result = true;
                        }
                    }
                });
                return result;
            }
            else
            {
                lock(_localCharactersLock)
                {
                    int index = _localCharacters.IndexOf(character);
                    if(index > 0)
                    {
                        _localCharacters.Move(index, index - 1);
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Move character down in the list
        /// </summary>
        public bool MoveLocalCharacterDown(LocalCharacter character)
        {
            if(UIThreadInvoker != null)
            {
                bool result = false;
                UIThreadInvoker(() =>
                {
                    lock(_localCharactersLock)
                    {
                        int index = _localCharacters.IndexOf(character);
                        if(index >= 0 && index < _localCharacters.Count - 1)
                        {
                            _localCharacters.Move(index, index + 1);
                            result = true;
                        }
                    }
                });
                return result;
            }
            else
            {
                lock(_localCharactersLock)
                {
                    int index = _localCharacters.IndexOf(character);
                    if(index >= 0 && index < _localCharacters.Count - 1)
                    {
                        _localCharacters.Move(index, index + 1);
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the list of Faction warfare systems
        /// </summary>
        [XmlIgnoreAttribute]
        public List<FactionWarfareSystemInfo> FactionWarfareSystems { get; set; }

        /// <summary>
        /// Gets or sets the master list of Regions
        /// </summary>
        public List<MapRegion> Regions { get; set; }

        /// <summary>
        /// Location of the static data distributed with the exectuable
        /// </summary>
        public string DataRootFolder { get; set; }

        /// <summary>
        /// Gets or sets the folder to cache dotland svg's etc to
        /// </summary>
        public string SaveDataRootFolder { get; set; }

        /// <summary>
        /// Gets or sets the folder to cache dotland svg's etc to
        /// </summary>
        public string SaveDataVersionFolder { get; set; }

        public EVEData.Server ServerInfo { get; set; }

        /// <summary>
        /// Gets or sets the ShipTypes ID to Name dictionary
        /// </summary>
        public SerializableDictionary<string, string> ShipTypes { get; set; }

        /// <summary>
        /// Gets or sets the System ID to Name dictionary
        /// </summary>
        public SerializableDictionary<long, string> SystemIDToName { get; set; }

        /// <summary>
        /// Gets or sets the master List of Systems
        /// </summary>
        public List<System> Systems { get; set; }

        /// <summary>
        /// Gets or sets the current list of thera connections
        /// </summary>
        public List<TheraConnection> TheraConnections { get; set; }

        /// <summary>
        /// Gets or sets the current list of Turnur connections
        /// </summary>
        public List<TurnurConnection> TurnurConnections { get; set; }

        public bool UseESIForCharacterPositions { get; set; }

        public List<Storm> MetaliminalStorms { get; set; }

        public List<POI> PointsOfInterest { get; set; }

        /// <summary>
        /// Gets or sets the current list of ZKillData
        /// </summary>
        public ZKillRedisQ ZKillFeed { get; set; }

        /// <summary>
        /// Gets or sets the current list of clear markers for the intel (eg "Clear" "Clr" etc)
        /// </summary>
        public List<string> IntelClearFilters { get; set; }

        /// <summary>
        /// Gets or sets the current list of intel filters used to monitor the local log files
        /// </summary>
        public List<string> IntelFilters { get; set; }

        /// <summary>
        /// Gets or sets the current list of ignore markers for the intel (eg "status")
        /// </summary>
        public List<string> IntelIgnoreFilters { get; set; }

        /// <summary>
        /// Gets or sets the current list of alerting intel market for intel (eg "pilot538 Maken")
        /// </summary>
        public List<string> IntelAlertFilters { get; set; }

        /// <summary>
        /// Gets or sets the Name to System dictionary
        /// </summary>
        private Dictionary<string, System> NameToSystem { get; }

        /// <summary>
        /// Gets or sets the ID to System dictionary
        /// </summary>
        private Dictionary<long, System> IDToSystem { get; }

    }
}
