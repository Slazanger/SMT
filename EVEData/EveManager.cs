//-----------------------------------------------------------------------
// EVE Manager
//-----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using EVEDataUtils;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SMT.EVEData
{
    /// <summary>
    /// The main EVE Manager
    /// </summary>
    public partial class EveManager
    {
        // App-wide UI language and English-to-localized string map (loaded from Translation.csv)
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


        /// <summary>
        /// Migrates old settings from previous versions of the application
        /// </summary>
        private void MigrateOldSettings()
        {
            try
            {
                // if we have a storageroot folder; we have already migrated settings
                if(Directory.Exists(EveAppConfig.StorageRoot))
                {
                    return;
                }

                string oldSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SMT");

                // prior to 1.39 all settings were stored in the My Documents\SMT\ folder
                if(Directory.Exists(oldSettingsFolder))
                {
                    // move the old settings folder to the new location
                    string newSettingsFolder = EveAppConfig.StorageRoot;
                    if(!Directory.Exists(newSettingsFolder))
                    {
                        Directory.CreateDirectory(newSettingsFolder);
                    }
                    // move the old settings to the new location
                    foreach(string file in Directory.GetFiles(oldSettingsFolder))
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(newSettingsFolder, fileName);
                        if(!File.Exists(destFile))
                        {
                            File.Move(file, destFile);
                        }
                    }
                    // delete the old settings folder
                    Directory.Delete(oldSettingsFolder, true);
                }

            }
            catch
            {
                // if we fail to migrate the settings, we just ignore it
                // this is a one time migration so we don't need to worry about it again
            }


        }


        /// <summary>
        /// Initializes a new instance of the <see cref="EveManager" /> class
        /// </summary>
        public EveManager(string version)
        {
            // LocalCharacters is now initialized as a private field
            VersionStr = version;

            MigrateOldSettings();

            string SaveDataRoot = EveAppConfig.StorageRoot;
            if (!Directory.Exists(SaveDataRoot))
            {
                Directory.CreateDirectory(SaveDataRoot);
            }

            DataRootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

            SaveDataRootFolder = SaveDataRoot;

            SaveDataVersionFolder = EveAppConfig.VersionStorage;
            if (!Directory.Exists(SaveDataVersionFolder))
            {
                Directory.CreateDirectory(SaveDataVersionFolder);
            }

            string characterSaveFolder = Path.Combine(SaveDataRootFolder, "Portraits");
            if (!Directory.Exists(characterSaveFolder))
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

        /// <summary>Loads <c>data/Translation.csv</c> into <see cref="Translations"/>.</summary>
        private void LoadTranslations()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "Translation.csv");
                if (File.Exists(path))
                {
                    // Read as UTF-8 (required for non-ASCII translations)
                    string[] lines = File.ReadAllLines(path, global::System.Text.Encoding.UTF8);
                    int count = 0;
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = line.Split(',');
                        if (parts.Length >= 2)
                        {
                            // Strip UTF-8 BOM if present on first column
                            string en = parts[0].Trim().Replace("\uFEFF", "");
                            string zh = parts[1].Trim();
                            if (!Translations.ContainsKey(en))
                            {
                                Translations.Add(en, zh);
                                count++;
                            }
                        }
                    }
                    global::System.Diagnostics.Debug.WriteLine($"Translation.csv: loaded {count} entries.");
                }
                else
                {
                    global::System.Diagnostics.Debug.WriteLine("Translation.csv: file not found: " + path);
                }
            }
            catch (global::System.Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine("Translation.csv: read error: " + ex.Message);
            }
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

        public ESI.NET.EsiClient ESIClient { get; set; }

        public List<string> ESIScopes { get; set; }

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
            if (UIThreadInvoker != null)
            {
                UIThreadInvoker(() =>
                {
                    lock (_localCharactersLock)
                    {
                        _localCharacters.Add(character);
                    }
                });
            }
            else
            {
                // Direct access during initialization or from UI thread
                lock (_localCharactersLock)
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
            if (UIThreadInvoker != null)
            {
                UIThreadInvoker(() =>
                {
                    lock (_localCharactersLock)
                    {
                        _localCharacters.Remove(character);
                    }
                });
            }
            else
            {
                // Direct access during initialization or from UI thread
                lock (_localCharactersLock)
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
            lock (_localCharactersLock)
            {
                return new List<LocalCharacter>(_localCharacters);
            }
        }

        /// <summary>
        /// Thread-safe character lookup by name
        /// </summary>
        public LocalCharacter FindCharacterByName(string characterName)
        {
            lock (_localCharactersLock)
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
                lock (_localCharactersLock)
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
            lock (_localCharactersLock)
            {
                return index >= 0 && index < _localCharacters.Count ? _localCharacters[index] : null;
            }
        }

        /// <summary>
        /// Move character up in the list
        /// </summary>
        public bool MoveLocalCharacterUp(LocalCharacter character)
        {
            if (UIThreadInvoker != null)
            {
                bool result = false;
                UIThreadInvoker(() =>
                {
                    lock (_localCharactersLock)
                    {
                        int index = _localCharacters.IndexOf(character);
                        if (index > 0)
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
                lock (_localCharactersLock)
                {
                    int index = _localCharacters.IndexOf(character);
                    if (index > 0)
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
            if (UIThreadInvoker != null)
            {
                bool result = false;
                UIThreadInvoker(() =>
                {
                    lock (_localCharactersLock)
                    {
                        int index = _localCharacters.IndexOf(character);
                        if (index >= 0 && index < _localCharacters.Count - 1)
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
                lock (_localCharactersLock)
                {
                    int index = _localCharacters.IndexOf(character);
                    if (index >= 0 && index < _localCharacters.Count - 1)
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


        /// <summary>
        /// Does the System Exist ?
        /// </summary>
        /// <param name="name">Name (not ID) of the system</param>
        public bool DoesSystemExist(string name) => GetEveSystem(name) != null;

        /// <summary>
        /// Get the alliance name from the alliance ID
        /// </summary>
        /// <param name="id">Alliance ID</param>
        /// <returns>Alliance Name</returns>
        public string GetAllianceName(int id)
        {
            string name = string.Empty;
            if(AllianceIDToName.ContainsKey(id))
            {
                name = AllianceIDToName[id];
            }

            return name;
        }

        /// <summary>
        /// Gets the alliance ticker eg "TEST" from the alliance ID
        /// </summary>
        /// <param name="id">Alliance ID</param>
        /// <returns>Alliance Ticker</returns>
        public string GetAllianceTicker(int id)
        {
            string ticker = string.Empty;
            if(AllianceIDToTicker.ContainsKey(id))
            {
                ticker = AllianceIDToTicker[id];
            }

            return ticker;
        }

        public string GetCharacterName(int id)
        {
            string name = string.Empty;
            if(CharacterIDToName.ContainsKey(id))
            {
                name = CharacterIDToName[id];
            }

            return name;
        }

        /// <summary>
        /// Get the ESI Logon URL String
        /// </summary>
        public string GetESILogonURL(string challengeCode)
        {
            string URL = ESIClient.SSO.CreateAuthenticationUrl(ESIScopes, VersionStr, challengeCode);
            return URL;
        }

        /// <summary>
        /// Get a System object from the name, note : for regions which have other region systems in it wont return
        /// them.. eg TR07-s is on the esoteria map, but the object corresponding to the feythabolis map will be returned
        /// </summary>
        /// <param name="name">Name (not ID) of the system</param>
        public System GetEveSystem(string name)
        {
            if(NameToSystem.ContainsKey(name))
            {
                return NameToSystem[name];
            }

            return null;
        }

        /// <summary>
        /// Get a System object from the ID
        /// </summary>
        /// <param name="id">ID of the system</param>
        public System GetEveSystemFromID(long id)
        {
            if(IDToSystem.ContainsKey(id))
            {
                return IDToSystem[id];
            }

            return null;
        }

        /// <summary>
        /// Get a System name from the ID
        /// </summary>
        /// <param name="id">ID of the system</param>
        public string GetEveSystemNameFromID(long id)
        {
            System s = GetEveSystemFromID(id);
            if(s != null)
            {
                return s.Name;
            }

            return string.Empty;
        }

        /// <summary>
        /// Calculate the range between the two systems
        /// </summary>
        public decimal GetRangeBetweenSystems(string from, string to)
        {
            System systemFrom = GetEveSystem(from);
            System systemTo = GetEveSystem(to);

            if(systemFrom == null || systemTo == null || from == to)
            {
                return 0.0M;
            }

            decimal x = systemFrom.ActualX - systemTo.ActualX;
            decimal y = systemFrom.ActualY - systemTo.ActualY;
            decimal z = systemFrom.ActualZ - systemTo.ActualZ;

            decimal length = DecimalMath.DecimalEx.Sqrt((x * x) + (y * y) + (z * z));

            return length;
        }

        /// <summary>
        /// Get the MapRegion from the name
        /// </summary>
        /// <param name="name">Name of the Region</param>
        /// <returns>Region Object</returns>
        public MapRegion GetRegion(string name)
        {
            foreach(MapRegion reg in Regions)
            {
                if(reg.Name == name)
                {
                    return reg;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the System name from the System ID
        /// </summary>
        /// <param name="id">System ID</param>
        /// <returns>System Name</returns>
        public string GetSystemNameFromSystemID(long id)
        {
            string name = string.Empty;
            if(SystemIDToName.ContainsKey(id))
            {
                name = SystemIDToName[id];
            }

            return name;
        }

        /// <summary>
        /// Hand the custom smtauth- url we get back from the logon screen
        /// </summary>
        public async void HandleEveAuthSMTUri(Uri uri, string challengeCode)
        {
            // parse the uri
            var query = HttpUtility.ParseQueryString(uri.Query);
            if(query["code"] == null)
            {
                // we're missing a query code
                return;
            }

            string code = query["code"];
            SsoToken sst;

            try
            {
                sst = await ESIClient.SSO.GetToken(GrantType.AuthorizationCode, code, challengeCode);
                if(sst == null || sst.ExpiresIn == 0)
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            AuthorizedCharacterData acd = await ESIClient.SSO.Verify(sst);

            // now find the matching character and update..
            LocalCharacter esiChar = FindCharacterByName(acd.CharacterName);

            if(esiChar == null)
            {
                esiChar = new LocalCharacter(acd.CharacterName, string.Empty, string.Empty);
                AddCharacter(esiChar);
            }

            esiChar.ESIRefreshToken = acd.RefreshToken;
            esiChar.ESILinked = true;
            esiChar.ESIAccessToken = acd.Token;
            esiChar.ESIAccessTokenExpiry = acd.ExpiresOn;
            esiChar.ID = acd.CharacterID;
            esiChar.ESIAuthData = acd;

            // now to find if a matching character
        }

        public void InitNavigation()
        {
            SerializableDictionary<string, List<string>> jumpRangeCache;

            string DataRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

            string JRC = Path.Combine(DataRootPath, "JumpRangeCache.dat");

            if(!File.Exists(JRC))
            {
                throw new NotImplementedException();
            }
            jumpRangeCache = Serialization.DeserializeFromDisk<SerializableDictionary<string, List<string>>>(JRC);
            Navigation.InitNavigation(NameToSystem.Values.ToList(), JumpBridges, jumpRangeCache);

            InitZarzakhConnections();
        }

        /// <summary>
        /// Load the EVE Manager Data from Disk
        /// </summary>
        public void LoadFromDisk()
        {
            SystemIDToName = new SerializableDictionary<long, string>();

            Regions = Serialization.DeserializeFromDisk<List<MapRegion>>(Path.Combine(DataRootFolder, "MapLayout.dat"));
            Systems = Serialization.DeserializeFromDisk<List<System>>(Path.Combine(DataRootFolder, "Systems.dat"));

            ShipTypes = Serialization.DeserializeFromDisk<SerializableDictionary<string, string>>(Path.Combine(DataRootFolder, "ShipTypes.dat"));

            foreach(System s in Systems)
            {
                SystemIDToName[s.ID] = s.Name;
            }

            CharacterIDToName = new SerializableDictionary<int, string>();
            AllianceIDToName = new SerializableDictionary<int, string>();
            AllianceIDToTicker = new SerializableDictionary<int, string>();

            // patch up any links
            foreach(System s in Systems)
            {
                NameToSystem[s.Name] = s;
                IDToSystem[s.ID] = s;
            }

            // now add the beacons
            string cynoBeaconsFile = Path.Combine(SaveDataRootFolder, "CynoBeacons.txt");
            if(File.Exists(cynoBeaconsFile))
            {
                StreamReader file = new StreamReader(cynoBeaconsFile);

                string line;
                while((line = file.ReadLine()) != null)
                {
                    string system = line.Trim();

                    System s = GetEveSystem(system);
                    if(s != null)
                    {
                        s.HasJumpBeacon = true;
                    }
                }
            }

            Init();
        }

        /// <summary>
        /// Load the jump bridge data from disk
        /// </summary>
        public void LoadJumpBridgeData()
        {
            JumpBridges = new List<JumpBridge>();

            string dataFilename = Path.Combine(SaveDataRootFolder, "JumpBridges_" + JumpBridge.SaveVersion + ".dat");
            if(!File.Exists(dataFilename))
            {
                return;
            }

            try
            {
                List<JumpBridge> loadList;
                XmlSerializer xms = new XmlSerializer(typeof(List<JumpBridge>));

                FileStream fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
                XmlReader xmlr = XmlReader.Create(fs);

                loadList = (List<JumpBridge>)xms.Deserialize(xmlr);

                foreach(JumpBridge j in loadList)
                {
                    JumpBridges.Add(j);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Update the Alliance and Ticker data for specified list
        /// </summary>
        public async Task ResolveAllianceIDs(List<int> IDs)
        {
            if(IDs.Count == 0)
            {
                return;
            }

            // strip out any ID's we already know..
            List<int> UnknownIDs = new List<int>();
            foreach(int l in IDs)
            {
                if((!AllianceIDToName.ContainsKey(l) || !AllianceIDToTicker.ContainsKey(l)) && !UnknownIDs.Contains(l))
                {
                    UnknownIDs.Add(l);
                }
            }

            if(UnknownIDs.Count == 0)
            {
                return;
            }

            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.Universe.ResolvedInfo>> esra = await ESIClient.Universe.Names(UnknownIDs);
                if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.ResolvedInfo>>(esra))
                {
                    foreach(ESI.NET.Models.Universe.ResolvedInfo ri in esra.Data)
                    {
                        if(ri.Category == ResolvedInfoCategory.Alliance)
                        {
                            ESI.NET.EsiResponse<ESI.NET.Models.Alliance.Alliance> esraA = await ESIClient.Alliance.Information((int)ri.Id);

                            if(ESIHelpers.ValidateESICall<ESI.NET.Models.Alliance.Alliance>(esraA))
                            {
                                AllianceIDToTicker[ri.Id] = esraA.Data.Ticker;
                                AllianceIDToName[ri.Id] = esraA.Data.Name;
                            }
                            else
                            {
                                AllianceIDToTicker[ri.Id] = "???????????????";
                                AllianceIDToName[ri.Id] = "?????";
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Update the Character ID data for specified list
        /// </summary>
        public async Task ResolveCharacterIDs(List<int> IDs)
        {
            if(IDs.Count == 0)
            {
                return;
            }

            // strip out any ID's we already know..
            List<int> UnknownIDs = new List<int>();
            foreach(int l in IDs)
            {
                if(!CharacterIDToName.ContainsKey(l))
                {
                    UnknownIDs.Add(l);
                }
            }

            if(UnknownIDs.Count == 0)
            {
                return;
            }

            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.Universe.ResolvedInfo>> esra = await ESIClient.Universe.Names(UnknownIDs);
                if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.ResolvedInfo>>(esra))
                {
                    foreach(ESI.NET.Models.Universe.ResolvedInfo ri in esra.Data)
                    {
                        if(ri.Category == ResolvedInfoCategory.Character)
                        {
                            CharacterIDToName[ri.Id] = ri.Name;
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Save the Data to disk
        /// </summary>
        public void SaveData()
        {
            // save off only the ESI authenticated Characters so create a new copy to serialise from..
            List<LocalCharacter> saveList = new List<LocalCharacter>();

            foreach(LocalCharacter c in GetLocalCharactersCopy())
            {
                if(!string.IsNullOrEmpty(c.ESIRefreshToken))
                {
                    saveList.Add(c);
                }
            }

            XmlSerializer xms = new XmlSerializer(typeof(List<LocalCharacter>));
            string dataFilename = Path.Combine(SaveDataRootFolder, "Characters_" + LocalCharacter.SaveVersion + ".dat");

            using(TextWriter tw = new StreamWriter(dataFilename))
            {
                xms.Serialize(tw, saveList);
            }

            string jbFileName = Path.Combine(SaveDataRootFolder, "JumpBridges_" + JumpBridge.SaveVersion + ".dat");
            Serialization.SerializeToDisk<List<JumpBridge>>(JumpBridges, jbFileName);

            List<string> beaconsToSave = new List<string>();
            foreach(System s in Systems)
            {
                if(s.HasJumpBeacon)
                {
                    beaconsToSave.Add(s.Name);
                }
            }

            // save the intel channels / intel filters
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelChannels.txt"), IntelFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelClearFilters.txt"), IntelClearFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelIgnoreFilters.txt"), IntelIgnoreFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelAlertFilters.txt"), IntelAlertFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "CynoBeacons.txt"), beaconsToSave);
        }

        /// <summary>
        /// Setup the intel watcher;  Loads the intel channel filter list and creates the file system watchers
        /// </summary>
        public void SetupIntelWatcher()
        {
            IntelDataList = new FixedQueue<IntelData>();
            IntelDataList.SetSizeLimit(250);

            IntelFilters = new List<string>();

            string intelFileFilter = Path.Combine(SaveDataRootFolder, "IntelChannels.txt");

            if(File.Exists(intelFileFilter))
            {
                StreamReader file = new StreamReader(intelFileFilter);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(!string.IsNullOrEmpty(line))
                    {
                        IntelFilters.Add(line);
                    }
                }
            }
            else
            {
                IntelFilters.Add("Int");
            }

            IntelClearFilters = new List<string>();
            string intelClearFileFilter = Path.Combine(SaveDataRootFolder, "IntelClearFilters.txt");

            if(File.Exists(intelClearFileFilter))
            {
                StreamReader file = new StreamReader(intelClearFileFilter);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(!string.IsNullOrEmpty(line))
                    {
                        IntelClearFilters.Add(line);
                    }
                }
            }
            else
            {
                // default
                IntelClearFilters.Add("Clr");
                IntelClearFilters.Add("Clear");
            }

            IntelIgnoreFilters = new List<string>();
            string intelIgnoreFileFilter = Path.Combine(SaveDataRootFolder, "IntelIgnoreFilters.txt");

            if(File.Exists(intelIgnoreFileFilter))
            {
                StreamReader file = new StreamReader(intelIgnoreFileFilter);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(!string.IsNullOrEmpty(line))
                    {
                        IntelIgnoreFilters.Add(line);
                    }
                }
            }
            else
            {
                // default
                IntelIgnoreFilters.Add("Status");
            }

            IntelAlertFilters = new List<string>();
            string intelAlertFileFilter = Path.Combine(SaveDataRootFolder, "IntelAlertFilters.txt");

            if(File.Exists(intelAlertFileFilter))
            {
                StreamReader file = new StreamReader(intelAlertFileFilter);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(!string.IsNullOrEmpty(line))
                    {
                        IntelAlertFilters.Add(line);
                    }
                }
            }
            else
            {
                // default, alert on nothing
                IntelAlertFilters.Add("");
            }

            intelFileReadPos = new Dictionary<string, int>();

            if(string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
            {
                string[] logFolderLoc = { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "Logs" };
                EVELogFolder = Path.Combine(logFolderLoc);
            }

            string chatlogFolder = Path.Combine(EVELogFolder, "Chatlogs");

            if(Directory.Exists(chatlogFolder))
            {
                intelFileWatcher = new FileSystemWatcher(chatlogFolder)
                {
                    Filter = "*.txt",
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                intelFileWatcher.Changed += IntelFileWatcher_Changed;
            }
        }

        /// <summary>
        /// Setup the game log0 watcher
        /// </summary>
        public void SetupGameLogWatcher()
        {
            gameFileReadPos = new Dictionary<string, int>();
            gamelogFileCharacterMap = new Dictionary<string, string>();

            GameLogList = new FixedQueue<GameLogData>();
            GameLogList.SetSizeLimit(50);

            if(string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
            {
                string[] logFolderLoc = { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "Logs" };
                EVELogFolder = Path.Combine(logFolderLoc);
            }

            string gameLogFolder = Path.Combine(EVELogFolder, "Gamelogs");

            if(Directory.Exists(gameLogFolder))
            {
                gameLogFileWatcher = new FileSystemWatcher(gameLogFolder)
                {
                    Filter = "*.txt",
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                gameLogFileWatcher.Changed += GameLogFileWatcher_Changed;
            }
        }

        public void SetupLogFileTriggers()
        {
            // -----------------------------------------------------------------
            // SUPER HACK WARNING....
            //
            // Start up a thread which just reads the text files in the eve log folder
            // by opening and closing them it updates the sytem meta files which
            // causes the file watcher to operate correctly otherwise this data
            // doesnt get updated until something other than the eve client reads these files

            List<string> logFolders = new List<string>();
            string chatLogFolder = Path.Combine(EVELogFolder, "Chatlogs");
            string gameLogFolder = Path.Combine(EVELogFolder, "Gamelogs");

            logFolders.Add(chatLogFolder);
            logFolders.Add(gameLogFolder);

            new Thread(() =>
            {
                LogFileCacheTrigger(logFolders);
            }).Start();

            // END SUPERHACK
            // -----------------------------------------------------------------
        }

        private void LogFileCacheTrigger(List<string> eveLogFolders)
        {
            Thread.CurrentThread.IsBackground = false;

            foreach(string dir in eveLogFolders)
            {
                if(!Directory.Exists(dir))
                {
                    return;
                }
            }

            // loop forever
            while(WatcherThreadShouldTerminate == false)
            {
                foreach(string folder in eveLogFolders)
                {
                    DirectoryInfo di = new DirectoryInfo(folder);
                    FileInfo[] files = di.GetFiles("*.txt");
                    foreach(FileInfo file in files)
                    {
                        bool readFile = false;
                        foreach(string intelFilterStr in IntelFilters)
                        {
                            if(file.Name.Contains(intelFilterStr, StringComparison.OrdinalIgnoreCase))
                            {
                                readFile = true;
                                break;
                            }
                        }

                        // local files
                        if(file.Name.Contains("Local_"))
                        {
                            readFile = true;
                        }

                        // gamelogs
                        if(folder.Contains("Gamelogs"))
                        {
                            readFile = true;
                        }

                        // only read files from the last day
                        if(file.CreationTime > DateTime.Now.AddDays(-1) && readFile)
                        {
                            FileStream ifs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            ifs.Seek(0, SeekOrigin.End);
                            ifs.Close();
                        }
                    }

                    Thread.Sleep(1500);
                }
            }
        }

        public void ShuddownIntelWatcher()
        {
            if(intelFileWatcher != null)
            {
                intelFileWatcher.Changed -= IntelFileWatcher_Changed;
            }
            WatcherThreadShouldTerminate = true;
        }

        public void ShuddownGameLogWatcher()
        {
            if(gameLogFileWatcher != null)
            {
                gameLogFileWatcher.Changed -= GameLogFileWatcher_Changed;
            }
            WatcherThreadShouldTerminate = true;
        }

        public void ShutDown()
        {
            ShuddownIntelWatcher();
            ShuddownGameLogWatcher();
            BackgroundThreadShouldTerminate = true;

            ZKillFeed.ShutDown();
        }

        /// <summary>
        /// Update The Universe Data from the various ESI end points
        /// </summary>
        public void UpdateESIUniverseData()
        {
            UpdateKillsFromESI();
            UpdateJumpsFromESI();
            UpdateSOVFromESI();
            UpdateIncursionsFromESI();

            UpdateSovStructureUpdate();

            // TEMP Disabled
            //();
        }

        /// <summary>
        /// Update the Alliance and Ticker data for all SOV owners in the specified region
        /// </summary>
        public void UpdateIDsForMapRegion(string name)
        {
            MapRegion r = GetRegion(name);
            if(r == null)
            {
                return;
            }

            List<int> IDToResolve = new List<int>();

            foreach(KeyValuePair<string, MapSystem> kvp in r.MapSystems)
            {
                if(kvp.Value.ActualSystem.SOVAllianceID != 0 && !AllianceIDToName.ContainsKey(kvp.Value.ActualSystem.SOVAllianceID) && !IDToResolve.Contains(kvp.Value.ActualSystem.SOVAllianceID))
                {
                    IDToResolve.Add(kvp.Value.ActualSystem.SOVAllianceID);
                }
            }

            _ = ResolveAllianceIDs(IDToResolve);
        }

        /// <summary>
        /// Update the current Thera Connections from EVE-Scout
        /// </summary>
        public async void UpdateTheraConnections()
        {
            string theraApiURL = "https://api.eve-scout.com/v2/public/signatures?system_name=Thera";
            string strContent = string.Empty;

            try
            {
                HttpClient hc = new HttpClient();
                var response = await hc.GetAsync(theraApiURL);
                response.EnsureSuccessStatusCode();
                strContent = await response.Content.ReadAsStringAsync();

                JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                TheraConnections.Clear();

                /*
                    new format

                    "id": "46",
                    "created_at": "2023-12-02T11:24:49.000Z",
                    "created_by_id": 93027866,
                    "created_by_name": "Das d'Alembert",
                    "updated_at": "2023-12-02T11:27:01.000Z",
                    "updated_by_id": 93027866,
                    "updated_by_name": "Das d'Alembert",
                    "completed_at": "2023-12-02T11:27:01.000Z",
                    "completed_by_id": 93027866,
                    "completed_by_name": "Das d'Alembert",
                    "completed": true,
                    "wh_exits_outward": true,
                    "wh_type": "Q063",
                    "max_ship_size": "medium",
                    "expires_at": "2023-12-03T04:24:49.000Z",
                    "remaining_hours": 14,
                    "signature_type": "wormhole",
                    "out_system_id": 31000005,
                    "out_system_name": "Thera",
                    "out_signature": "HMM-222",
                    "in_system_id": 30001715,
                    "in_system_class": "hs",
                    "in_system_name": "Moutid",
                    "in_region_id": 10000020,
                    "in_region_name": "Tash-Murkon",
                    "in_signature": "LPI-677"
                 */

                while(jsr.Read())
                {
                    if(jsr.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(jsr);
                        string inSignatureId = obj["in_signature"].ToString();
                        string outSignatureId = obj["out_signature"].ToString();
                        long solarSystemId = long.Parse(obj["in_system_id"].ToString());
                        string wormHoleEOL = obj["expires_at"].ToString();
                        string type = obj["signature_type"].ToString();

                        if(type != null && type == "wormhole" && solarSystemId != 0 && wormHoleEOL != null && SystemIDToName.ContainsKey(solarSystemId))
                        {
                            System theraConnectionSystem = GetEveSystemFromID(solarSystemId);

                            TheraConnection tc = new TheraConnection(theraConnectionSystem.Name, theraConnectionSystem.Region, inSignatureId, outSignatureId, wormHoleEOL);
                            TheraConnections.Add(tc);
                        }
                    }
                }
            }
            catch
            {
                return;
            }

            if(TheraUpdateEvent != null)
            {
                TheraUpdateEvent();
            }
        }

        /// <summary>
        /// Update the current Turnur Connections from EVE-Scout
        /// </summary>
        public async void UpdateTurnurConnections()
        {
            string turnurApiURL = "https://api.eve-scout.com/v2/public/signatures?system_name=Turnur";
            string strContent = string.Empty;

            try
            {
                HttpClient hc = new HttpClient();
                var response = await hc.GetAsync(turnurApiURL);
                response.EnsureSuccessStatusCode();
                strContent = await response.Content.ReadAsStringAsync();

                JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                TurnurConnections.Clear();

                /*
                    new format

                    "id": "46",
                    "created_at": "2023-12-02T11:24:49.000Z",
                    "created_by_id": 93027866,
                    "created_by_name": "Das d'Alembert",
                    "updated_at": "2023-12-02T11:27:01.000Z",
                    "updated_by_id": 93027866,
                    "updated_by_name": "Das d'Alembert",
                    "completed_at": "2023-12-02T11:27:01.000Z",
                    "completed_by_id": 93027866,
                    "completed_by_name": "Das d'Alembert",
                    "completed": true,
                    "wh_exits_outward": true,
                    "wh_type": "Q063",
                    "max_ship_size": "medium",
                    "expires_at": "2023-12-03T04:24:49.000Z",
                    "remaining_hours": 14,
                    "signature_type": "wormhole",
                    "out_system_id": 31000005,
                    "out_system_name": "Thera",
                    "out_signature": "HMM-222",
                    "in_system_id": 30001715,
                    "in_system_class": "hs",
                    "in_system_name": "Moutid",
                    "in_region_id": 10000020,
                    "in_region_name": "Tash-Murkon",
                    "in_signature": "LPI-677"
                 */

                while(jsr.Read())
                {
                    if(jsr.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(jsr);
                        string inSignatureId = obj["in_signature"].ToString();
                        string outSignatureId = obj["out_signature"].ToString();
                        long solarSystemId = long.Parse(obj["in_system_id"].ToString());
                        string wormHoleEOL = obj["expires_at"].ToString();
                        string type = obj["signature_type"].ToString();

                        if(type != null && type == "wormhole" && solarSystemId != 0 && wormHoleEOL != null && SystemIDToName.ContainsKey(solarSystemId))
                        {
                            System turnurConnectionSystem = GetEveSystemFromID(solarSystemId);

                            TurnurConnection tc = new TurnurConnection(turnurConnectionSystem.Name, turnurConnectionSystem.Region, inSignatureId, outSignatureId, wormHoleEOL);
                            TurnurConnections.Add(tc);
                        }
                    }
                }
            }
            catch
            {
                return;
            }

            if(TurnurUpdateEvent != null)
            {
                TurnurUpdateEvent();
            }
        }

        public void UpdateMetaliminalStorms()
        {
            MetaliminalStorms.Clear();

            List<Storm> ls = Storm.GetStorms();
            foreach(Storm s in ls)
            {
                System sys = GetEveSystem(s.System);
                if(sys != null)
                {
                    MetaliminalStorms.Add(s);
                }
            }

            // now update the Strong and weak areas around the storm
            foreach(Storm s in MetaliminalStorms)
            {
                // The Strong area is 1 jump out from the centre
                List<string> strongArea = Navigation.GetSystemsXJumpsFrom(new List<string>(), s.System, 1);

                // The weak area is 3 jumps out from the centre
                List<string> weakArea = Navigation.GetSystemsXJumpsFrom(new List<string>(), s.System, 3);

                // strip the strong area out of the weak so we dont have overlapping icons
                s.WeakArea = weakArea.Except(strongArea).ToList();

                // strip the centre out of the strong area
                strongArea.Remove(s.Name);

                s.StrongArea = strongArea;
            }
            if(StormsUpdateEvent != null)
            {
                StormsUpdateEvent();
            }
        }

        public async void UpdateFactionWarfareInfo()
        {
            FactionWarfareSystems.Clear();

            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.FactionWarfare.FactionWarfareSystem>> esr = await ESIClient.FactionWarfare.Systems();

                string debugListofSytems = "";

                if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.FactionWarfare.FactionWarfareSystem>>(esr))
                {
                    foreach(ESI.NET.Models.FactionWarfare.FactionWarfareSystem i in esr.Data)
                    {
                        FactionWarfareSystemInfo fwsi = new FactionWarfareSystemInfo();
                        fwsi.SystemState = FactionWarfareSystemInfo.State.None;

                        fwsi.OccupierID = i.OccupierFactionId;
                        fwsi.OccupierName = FactionWarfareSystemInfo.OwnerIDToName(i.OccupierFactionId);

                        fwsi.OwnerID = i.OwnerFactionId;
                        fwsi.OwnerName = FactionWarfareSystemInfo.OwnerIDToName(i.OwnerFactionId);

                        fwsi.SystemID = i.SolarSystemId;
                        fwsi.SystemName = GetEveSystemNameFromID(i.SolarSystemId);
                        fwsi.LinkSystemID = 0;
                        fwsi.VictoryPoints = i.VictoryPoints;
                        fwsi.VictoryPointsThreshold = i.VictoryPointsThreshold;

                        FactionWarfareSystems.Add(fwsi);

                        debugListofSytems += fwsi.SystemName + "\n";
                    }
                }

                // step 1, identify all the Frontline systems, these will be systems with connections to other systems with a different occupier
                foreach(FactionWarfareSystemInfo fws in FactionWarfareSystems)
                {
                    System s = GetEveSystemFromID(fws.SystemID);
                    foreach(string js in s.Jumps)
                    {
                        foreach(FactionWarfareSystemInfo fwss in FactionWarfareSystems)
                        {
                            if(fwss.SystemName == js && fwss.OccupierID != fws.OccupierID)
                            {
                                fwss.SystemState = FactionWarfareSystemInfo.State.Frontline;
                                fws.SystemState = FactionWarfareSystemInfo.State.Frontline;
                            }
                        }
                    }
                }

                // step 2, itendify all commandline operations by flooding out one from the frontlines
                foreach(FactionWarfareSystemInfo fws in FactionWarfareSystems)
                {
                    if(fws.SystemState == FactionWarfareSystemInfo.State.Frontline)
                    {
                        System s = GetEveSystemFromID(fws.SystemID);

                        foreach(string js in s.Jumps)
                        {
                            foreach(FactionWarfareSystemInfo fwss in FactionWarfareSystems)
                            {
                                if(fwss.SystemName == js && fwss.SystemState == FactionWarfareSystemInfo.State.None && fwss.OccupierID == fws.OccupierID)
                                {
                                    fwss.SystemState = FactionWarfareSystemInfo.State.CommandLineOperation;
                                    fwss.LinkSystemID = fws.SystemID;
                                }
                            }
                        }
                    }
                }

                // step 3, itendify all Rearguard operations by flooding out one from the command lines
                foreach(FactionWarfareSystemInfo fws in FactionWarfareSystems)
                {
                    if(fws.SystemState == FactionWarfareSystemInfo.State.CommandLineOperation)
                    {
                        System s = GetEveSystemFromID(fws.SystemID);

                        foreach(string js in s.Jumps)
                        {
                            foreach(FactionWarfareSystemInfo fwss in FactionWarfareSystems)
                            {
                                if(fwss.SystemName == js && fwss.SystemState == FactionWarfareSystemInfo.State.None && fwss.OccupierID == fws.OccupierID)
                                {
                                    fwss.SystemState = FactionWarfareSystemInfo.State.Rearguard;
                                    fwss.LinkSystemID = fws.SystemID;
                                }
                            }
                        }
                    }
                }

                // for ease remove all "none" systems
                //FactionWarfareSystems.RemoveAll(sys => sys.SystemState == FactionWarfareSystemInfo.State.None);
            }
            catch { }
        }

        public void AddUpdateJumpBridge(string from, string to, long stationID)
        {
            // validate
            if(GetEveSystem(from) == null || GetEveSystem(to) == null)
            {
                return;
            }

            bool found = false;

            foreach(JumpBridge jb in JumpBridges)
            {
                if(jb.From == from)
                {
                    found = true;
                    jb.FromID = stationID;
                }
                if(jb.To == from)
                {
                    found = true;
                    jb.ToID = stationID;
                }
            }

            if(!found)
            {
                JumpBridge njb = new JumpBridge(from, to);
                njb.FromID = stationID;
                JumpBridges.Add(njb);
            }
        }

        /// <summary>
        /// Initialise the eve manager
        /// </summary>
        private void Init()
        {
            IOptions<EsiConfig> config = Options.Create(new EsiConfig()
            {
                EsiUrl = "https://esi.evetech.net",
                DataSource = DataSource.Tranquility,
                ClientId = EveAppConfig.ClientID,
                SecretKey = "Unneeded",
                CallbackUrl = EveAppConfig.CallbackURL,
                UserAgent = "SMT/" + EveAppConfig.SMT_VERSION + EveAppConfig.SMT_USERAGENT_DETAILS,
            });

            ESIClient = new ESI.NET.EsiClient(config);
            ESIScopes = new List<string>
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

            foreach(MapRegion rr in Regions)
            {
                // link to the real systems
                foreach(KeyValuePair<string, MapSystem> kvp in rr.MapSystems)
                {
                    kvp.Value.ActualSystem = GetEveSystem(kvp.Value.Name);
                }
            }

            LoadCharacters();

            InitTheraConnections();
            InitTurnurConnections();

            InitMetaliminalStorms();
            InitFactionWarfareInfo();
            InitPOI();

            ActiveSovCampaigns = new List<SOVCampaign>();

            // Auto-load infrastructure upgrades if file exists
            string upgradesFile = Path.Combine(SaveDataRootFolder, "InfrastructureUpgrades.txt");
            if (File.Exists(upgradesFile))
            {
                LoadInfrastructureUpgrades(upgradesFile);
            }

            InitZKillFeed();

            StartBackgroundThread();
        }

        private void InitPOI()
        {
            PointsOfInterest = new List<POI>();

            try
            {
                string POIcsv = Path.Combine(DataRootFolder, "POI.csv");
                if(File.Exists(POIcsv))
                {
                    StreamReader file = new StreamReader(POIcsv);

                    string line;
                    line = file.ReadLine();
                    while((line = file.ReadLine()) != null)
                    {
                        if(string.IsNullOrEmpty(line))
                        {
                            continue;
                        }
                        string[] bits = line.Split(',');

                        if(bits.Length < 4)
                        {
                            continue;
                        }

                        string system = bits[0];
                        string type = bits[1];
                        string desc = bits[2];
                        string longdesc = bits[3];

                        if(GetEveSystem(system) == null)
                        {
                            continue;
                        }

                        POI p = new POI() { System = system, Type = type, ShortDesc = desc, LongDesc = longdesc };

                        PointsOfInterest.Add(p);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Initialise the Thera Connection Data from EVE-Scout
        /// </summary>
        private void InitTheraConnections()
        {
            TheraConnections = new List<TheraConnection>();
            UpdateTheraConnections();
        }

        /// <summary>
        /// Initialise the Turnur Connection Data from EVE-Scout
        /// </summary>
        private void InitTurnurConnections()
        {
            TurnurConnections = new List<TurnurConnection>();
            UpdateTurnurConnections();
        }

        /// <summary>
        /// Initialise the Zarzakh Connection Data
        /// </summary>
        private void InitZarzakhConnections()
        {
            List<string> zcon = new List<string>();
            foreach(System s in Systems)
            {
                if(s.HasJoveGate)
                {
                    zcon.Add(s.Name);
                }
            }

            Navigation.UpdateZarzakhConnections(zcon);
        }

        private void InitMetaliminalStorms()
        {
            MetaliminalStorms = new List<Storm>();
        }

        private void InitFactionWarfareInfo()
        {
            FactionWarfareSystems = new List<FactionWarfareSystemInfo>();
            UpdateFactionWarfareInfo();
        }

        /// <summary>
        /// Initialise the ZKillBoard Feed
        /// </summary>
        private void InitZKillFeed()
        {
            ZKillFeed = new ZKillRedisQ();
            ZKillFeed.Initialise();
        }

        /// <summary>
        /// Intel File watcher changed handler
        /// </summary>
        private void IntelFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string changedFile = e.FullPath;

            string[] channelParts = e.Name.Split("_");
            string channelName = string.Join("_", channelParts, 0, channelParts.Length - 3);

            bool processFile = false;
            bool localChat = false;

            // check if the changed file path contains the name of a channel we're looking for
            foreach(string intelFilterStr in IntelFilters)
            {
                if(changedFile.Contains(intelFilterStr, StringComparison.OrdinalIgnoreCase))
                {
                    processFile = true;
                    break;
                }
            }

            if(changedFile.Contains("Local_"))
            {
                localChat = true;
                processFile = true;
            }

            if(processFile)
            {
                try
                {
                    Encoding fe = Misc.GetEncoding(changedFile);
                    FileStream ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    StreamReader file = new StreamReader(ifs, fe);

                    int fileReadFrom = 0;

                    // have we seen this file before
                    if(intelFileReadPos.ContainsKey(changedFile))
                    {
                        fileReadFrom = intelFileReadPos[changedFile];
                    }
                    else
                    {
                        if(localChat)
                        {
                            string system = string.Empty;
                            string characterName = string.Empty;

                            // read the iniital block
                            while(!file.EndOfStream)
                            {
                                string l = file.ReadLine();
                                fileReadFrom++;

                                // explicitly skip just "local"
                                if(l.Contains("Channel Name:    Local"))
                                {
                                    // now can read the next line
                                    l = file.ReadLine(); // should be the "Listener : <CharName>"
                                    fileReadFrom++;

                                    characterName = l.Split(':')[1].Trim();

                                    bool addChar = true;
                                    foreach(EVEData.LocalCharacter c in GetLocalCharactersCopy())
                                    {
                                        if(characterName == c.Name)
                                        {
                                            c.Location = system;
                                            c.LocalChatFile = changedFile;

                                            System s = GetEveSystem(system);
                                            if(s != null)
                                            {
                                                c.Region = s.Region;
                                            }
                                            else
                                            {
                                                c.Region = "";
                                            }

                                            addChar = false;
                                        }
                                    }

                                    if(addChar)
                                    {
                                        AddCharacter(new EVEData.LocalCharacter(characterName, changedFile, system));
                                    }

                                    break;
                                }
                            }
                        }

                        while(file.ReadLine() != null)
                        {
                            fileReadFrom++;
                        }

                        fileReadFrom--;
                        file.BaseStream.Seek(0, SeekOrigin.Begin);
                    }

                    for(int i = 0; i < fileReadFrom; i++)
                    {
                        file.ReadLine();
                    }

                    string line = file.ReadLine();

                    while(line != null)
                    {                    // trim any items off the front
                        if(line.Contains('[') && line.Contains(']'))
                        {
                            line = line.Substring(line.IndexOf("["));
                        }

                        if(line == "")
                        {
                            line = file.ReadLine();
                            continue;
                        }

                        fileReadFrom++;

                        if(localChat)
                        {
                            if(line.StartsWith("[") && line.Contains("EVE System > Channel changed to Local"))
                            {
                                string system = line.Split(':').Last().Trim();

                                foreach(EVEData.LocalCharacter c in GetLocalCharactersCopy())
                                {
                                    if(c.LocalChatFile == changedFile)
                                    {
                                        c.Location = system;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // check if it is in the intel list already (ie if you have multiple clients running)
                            bool addToIntel = true;

                            int start = line.IndexOf('>') + 1;
                            string newIntelString = line.Substring(start);

                            if(newIntelString != null)
                            {
                                foreach(EVEData.IntelData idl in IntelDataList)
                                {
                                    if(idl.IntelString == newIntelString && (DateTime.Now - idl.IntelTime).Seconds < 5)
                                    {
                                        addToIntel = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                addToIntel = false;
                            }

                            if(line.Contains("Channel MOTD:"))
                            {
                                addToIntel = false;
                            }

                            foreach(String ignoreMarker in IntelIgnoreFilters)
                            {
                                if(line.IndexOf(ignoreMarker, StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    addToIntel = false;
                                    break;
                                }
                            }

                            if(addToIntel)
                            {
                                EVEData.IntelData id = new EVEData.IntelData(line, channelName);

                                foreach(string s in id.IntelString.Split(' '))
                                {
                                    if(s == "" || s.Length < 3)
                                    {
                                        continue;
                                    }

                                    foreach(String clearMarker in IntelClearFilters)
                                    {
                                        if(clearMarker.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            id.ClearNotification = true;
                                        }
                                    }

                                    foreach(System sys in Systems)
                                    {
                                        if(sys.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0 || s.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            id.Systems.Add(sys.Name);
                                        }
                                    }
                                }

                                IntelDataList.Enqueue(id);

                                if(IntelUpdatedEvent != null)
                                {
                                    IntelUpdatedEvent(IntelDataList);
                                }
                            }
                        }

                        line = file.ReadLine();
                    }

                    ifs.Close();

                    intelFileReadPos[changedFile] = fileReadFrom;
                }
                catch
                {
                }
            }
            else
            {
            }
        }

        /// <summary>
        /// GameLog File watcher changed handler
        /// </summary>
        private void GameLogFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string changedFile = e.FullPath;
            string characterName = string.Empty;

            try
            {
                Encoding fe = EVEDataUtils.Misc.GetEncoding(changedFile);
                FileStream ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                StreamReader file = new StreamReader(ifs, fe);

                int fileReadFrom = 0;

                // have we seen this file before
                if(gameFileReadPos.ContainsKey(changedFile))
                {
                    fileReadFrom = gameFileReadPos[changedFile];
                }
                else
                {
                    // read the iniital block
                    while(!file.EndOfStream)
                    {
                        string l = file.ReadLine();
                        fileReadFrom++;

                        // explicitly skip just "local"
                        if(l.Contains("Gamelog"))
                        {
                            // now can read the next line
                            l = file.ReadLine(); // should be the "Listener : <CharName>"

                            // something wrong with the log file; clear
                            if(!l.Contains("Listener"))
                            {
                                if(gameFileReadPos.ContainsKey(changedFile))
                                {
                                    gameFileReadPos.Remove(changedFile);
                                }

                                return;
                            }

                            fileReadFrom++;

                            gamelogFileCharacterMap[changedFile] = l.Split(':')[1].Trim();

                            // session started
                            l = file.ReadLine();
                            fileReadFrom++;

                            // header end
                            l = file.ReadLine();
                            fileReadFrom++;

                            // as its new; skip the entire file -1
                            break;
                        }
                    }

                    while(!file.EndOfStream)
                    {
                        string l = file.ReadLine();
                        fileReadFrom++;
                    }

                    // back one line
                    fileReadFrom--;

                    file.BaseStream.Seek(0, SeekOrigin.Begin);
                }

                characterName = gamelogFileCharacterMap[changedFile];

                for(int i = 0; i < fileReadFrom; i++)
                {
                    file.ReadLine();
                }

                string line = file.ReadLine();

                while(line != null)
                {                    // trim any items off the front
                    if(line == "" || !line.StartsWith("["))
                    {
                        line = file.ReadLine();
                        fileReadFrom++;
                        continue;
                    }

                    fileReadFrom++;

                    int typeStartPos = line.IndexOf("(") + 1;
                    int typeEndPos = line.IndexOf(")");

                    // file corrupt
                    if(typeStartPos < 1 || typeEndPos < 1)
                    {
                        continue;
                    }

                    string type = line.Substring(typeStartPos, typeEndPos - typeStartPos);

                    line = line.Substring(typeEndPos + 1);

                    // strip the formatting from the log
                    line = Regex.Replace(line, "<.*?>", String.Empty);

                    GameLogData gd = new GameLogData()
                    {
                        Character = characterName,
                        Text = line,
                        Severity = type,
                        Time = DateTime.Now,
                    };

                    GameLogList.Enqueue(gd);
                    if(GameLogAddedEvent != null)
                    {
                        GameLogAddedEvent(GameLogList);
                    }

                    foreach(LocalCharacter lc in GetLocalCharactersCopy())
                    {
                        if(lc.Name == characterName)
                        {
                            if(type == "combat")
                            {
                                if(CombatEvent != null)
                                {
                                    lc.GameLogWarningText = line;
                                    CombatEvent(characterName, line);
                                }
                            }

                            if(
                                line.Contains("cloak deactivates due to a pulse from a Mobile Observatory") ||
                                line.Contains("Your cloak deactivates due to proximity to") ||
                                line.Contains("Your cloak deactivates due to a pulse from a Dazh Liminality Locus")
                                )
                            {
                                if(ShipDecloakedEvent != null)
                                {
                                    ShipDecloakedEvent(characterName, line);
                                    lc.GameLogWarningText = line;
                                }
                            }
                        }
                    }

                    line = file.ReadLine();
                    gameFileReadPos[changedFile] = fileReadFrom;
                }

                ifs.Close();

                gameFileReadPos[changedFile] = fileReadFrom;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Load the character data from disk
        /// </summary>
        private void LoadCharacters()
        {
            string dataFilename = Path.Combine(SaveDataRootFolder, "Characters_" + LocalCharacter.SaveVersion + ".dat");
            if(!File.Exists(dataFilename))
            {
                return;
            }

            try
            {
                List<LocalCharacter> loadList;
                XmlSerializer xms = new XmlSerializer(typeof(List<LocalCharacter>));

                FileStream fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
                XmlReader xmlr = XmlReader.Create(fs);

                loadList = (List<LocalCharacter>)xms.Deserialize(xmlr);

                foreach(LocalCharacter c in loadList)
                {
                    c.ESIAccessToken = string.Empty;
                    c.ESIAccessTokenExpiry = DateTime.MinValue;
                    c.LocalChatFile = string.Empty;
                    c.Location = string.Empty;
                    c.Region = string.Empty;

                    AddCharacter(c);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Start the Low Frequency Update Thread
        /// </summary>
        private void StartBackgroundThread()
        {
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = false;


                // split the intial requests into 3 for a better initialisation

                foreach(LocalCharacter c in GetLocalCharactersCopy())
                {
                    await c.RefreshAccessToken().ConfigureAwait(true);
                }

                foreach(LocalCharacter c in GetLocalCharactersCopy())
                {
                    await c.UpdatePositionFromESI().ConfigureAwait(true);
                }

                foreach(LocalCharacter c in GetLocalCharactersCopy())
                {
                    await c.UpdateInfoFromESI().ConfigureAwait(true);
                }




                // loop forever
                while(BackgroundThreadShouldTerminate == false)
                {
                    // character Update
                    if((NextCharacterUpdate - DateTime.Now).Ticks < 0)
                    {
                        NextCharacterUpdate = DateTime.Now + CharacterUpdateRate;

                        var characters = GetLocalCharactersCopy();
                        for(int i = 0; i < characters.Count; i++)
                        {
                            LocalCharacter c = characters[i];
                            await c.Update();
                        }
                    }

                    // sov update
                    if((NextSOVCampaignUpdate - DateTime.Now).Ticks < 0)
                    {
                        NextSOVCampaignUpdate = DateTime.Now + SOVCampaignUpdateRate;
                        UpdateSovCampaigns();
                    }

                    // low frequency update
                    if((NextLowFreqUpdate - DateTime.Now).Minutes < 0)
                    {
                        NextLowFreqUpdate = DateTime.Now + LowFreqUpdateRate;

                        UpdateESIUniverseData();
                        UpdateServerInfo();
                        UpdateTheraConnections();
                        UpdateTurnurConnections();
                    }

                    if((NextDotlanUpdate - DateTime.Now).Minutes < 0)
                    {
                        UpdateDotlanKillDeltaInfo();
                    }

                    Thread.Sleep(100);
                }
            }).Start();
        }

        private async void UpdateDotlanKillDeltaInfo()
        {
            // set the update for 20 minutes from now initially which will be pushed further once we have the last-modified
            // however if the request fails we still push out the request..
            NextDotlanUpdate = DateTime.Now + TimeSpan.FromMinutes(20);

            try
            {
                string dotlanNPCDeltaAPIurl = "https://evemaps.dotlan.net/ajax/npcdelta";

                HttpClient hc = new HttpClient();
                string versionNum = VersionStr.Split("_")[1];

                string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0";
                hc.DefaultRequestHeaders.Add("User-Agent", userAgent);
                hc.DefaultRequestHeaders.IfModifiedSince = LastDotlanUpdate;

                // set the etag if we have one
                if(LastDotlanETAG != "")
                {
                    hc.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(LastDotlanETAG));
                }

                var response = await hc.GetAsync(dotlanNPCDeltaAPIurl);

                // update the next request to the last modified + 1hr + random offset
                if(response.Content.Headers.LastModified.HasValue)
                {
                    Random rndUpdateOffset = new Random();
                    NextDotlanUpdate = response.Content.Headers.LastModified.Value.DateTime.ToLocalTime() + TimeSpan.FromMinutes(60) + TimeSpan.FromSeconds(rndUpdateOffset.Next(1, 300));
                }

                // update the values for the next request;
                LastDotlanUpdate = DateTime.Now;
                if(response.Headers.ETag != null)
                {
                    LastDotlanETAG = response.Headers.ETag.Tag;
                }

                if(response.StatusCode == HttpStatusCode.NotModified)
                {
                    // we shouldn't hit this; the first request should update the request beyond the last-modified expiring
                    // LastDotlanUpdate = DateTime.Now;
                }
                else
                {
                    // read the data
                    string strContent = string.Empty;
                    strContent = await response.Content.ReadAsStringAsync();

                    // parse the json response into kvp string/strings (system id)/(delta)
                    Dictionary<string, string> killdDeltadata = JsonConvert.DeserializeObject<Dictionary<string, string>>(strContent);

                    foreach(var kvp in killdDeltadata)
                    {
                        Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
                        int systemId = int.Parse(kvp.Key);
                        int killDelta = int.Parse(kvp.Value);

                        System s = GetEveSystemFromID(systemId);
                        if(s != null)
                        {
                            s.NPCKillsDeltaLastHour = killDelta;
                        }
                    }
                }
            }
            catch(Exception e)
            {
            }
        }

        /// <summary>
        /// Start the ESI download for the Jump info
        /// </summary>
        private async void UpdateIncursionsFromESI()
        {
            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.Incursions.Incursion>> esr = await ESIClient.Incursions.All();
                if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.Incursions.Incursion>>(esr))
                {
                    foreach(ESI.NET.Models.Incursions.Incursion i in esr.Data)
                    {
                        foreach(long s in i.InfestedSystems)
                        {
                            EVEData.System sys = GetEveSystemFromID(s);
                            if(sys != null)
                            {
                                sys.ActiveIncursion = true;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Start the ESI download for the Jump info
        /// </summary>
        private async void UpdateJumpsFromESI()
        {
            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.Universe.Jumps>> esr = await ESIClient.Universe.Jumps();
                if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.Jumps>>(esr))
                {
                    foreach(ESI.NET.Models.Universe.Jumps j in esr.Data)
                    {
                        EVEData.System es = GetEveSystemFromID(j.SystemId);
                        if(es != null)
                        {
                            es.JumpsLastHour = j.ShipJumps;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Start the ESI download for the kill info
        /// </summary>
        private async void UpdateKillsFromESI()
        {
            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.Universe.Kills>> esr = await ESIClient.Universe.Kills();
                if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.Kills>>(esr))
                {
                    foreach(ESI.NET.Models.Universe.Kills k in esr.Data)
                    {
                        EVEData.System es = GetEveSystemFromID(k.SystemId);
                        if(es != null)
                        {
                            es.NPCKillsLastHour = k.NpcKills;
                            es.PodKillsLastHour = k.PodKills;
                            es.ShipKillsLastHour = k.ShipKills;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private async void UpdateSovCampaigns()
        {
            try
            {
                bool sendUpdateEvent = false;

                foreach(SOVCampaign sc in ActiveSovCampaigns)
                {
                    sc.Valid = false;
                }

                List<int> allianceIDsToResolve = new List<int>();

                ESI.NET.EsiResponse<List<ESI.NET.Models.Sovereignty.Campaign>> esr = await ESIClient.Sovereignty.Campaigns();
                if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.Sovereignty.Campaign>>(esr))
                {
                    foreach(ESI.NET.Models.Sovereignty.Campaign c in esr.Data)
                    {
                        SOVCampaign ss = null;

                        foreach(SOVCampaign asc in ActiveSovCampaigns)
                        {
                            if(asc.CampaignID == c.CampaignId)
                            {
                                ss = asc;
                            }
                        }

                        if(ss == null)
                        {
                            System sys = GetEveSystemFromID(c.SolarSystemId);
                            if(sys == null)
                            {
                                continue;
                            }

                            ss = new SOVCampaign
                            {
                                CampaignID = c.CampaignId,
                                DefendingAllianceID = c.DefenderId,
                                System = sys.Name,
                                Region = sys.Region,
                                StartTime = c.StartTime,
                                DefendingAllianceName = "",
                            };

                            if(c.EventType == "ihub_defense")
                            {
                                ss.Type = "IHub";
                            }

                            if(c.EventType == "tcu_defense")
                            {
                                ss.Type = "TCU";
                            }

                            ActiveSovCampaigns.Add(ss);
                            sendUpdateEvent = true;
                        }

                        if(ss.AttackersScore != c.AttackersScore || ss.DefendersScore != c.DefenderScore)
                        {
                            sendUpdateEvent = true;
                        }

                        ss.AttackersScore = c.AttackersScore;
                        ss.DefendersScore = c.DefenderScore;
                        ss.Valid = true;

                        if(AllianceIDToName.ContainsKey(ss.DefendingAllianceID))
                        {
                            ss.DefendingAllianceName = AllianceIDToName[ss.DefendingAllianceID];
                        }
                        else
                        {
                            if(!allianceIDsToResolve.Contains(ss.DefendingAllianceID))
                            {
                                allianceIDsToResolve.Add(ss.DefendingAllianceID);
                            }
                        }

                        int NodesToWin = (int)Math.Ceiling(ss.DefendersScore / 0.07);
                        int NodesToDefend = (int)Math.Ceiling(ss.AttackersScore / 0.07);
                        ss.State = $"Nodes Remaining\nAttackers : {NodesToWin}\nDefenders : {NodesToDefend}";

                        ss.TimeToStart = ss.StartTime - DateTime.UtcNow;

                        if(ss.StartTime < DateTime.UtcNow)
                        {
                            ss.IsActive = true;
                        }
                        else
                        {
                            ss.IsActive = false;
                        }
                    }
                }

                if(allianceIDsToResolve.Count > 0)
                {
                    await ResolveAllianceIDs(allianceIDsToResolve);
                }

                foreach(SOVCampaign sc in ActiveSovCampaigns.ToList())
                {
                    if(string.IsNullOrEmpty(sc.DefendingAllianceName) && AllianceIDToName.ContainsKey(sc.DefendingAllianceID))
                    {
                        sc.DefendingAllianceName = AllianceIDToName[sc.DefendingAllianceID];
                    }

                    if(sc.Valid == false)
                    {
                        ActiveSovCampaigns.Remove(sc);
                        sendUpdateEvent = true;
                    }
                }

                if(sendUpdateEvent)
                {
                    if(SovUpdateEvent != null)
                    {
                        SovUpdateEvent();
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Start the ESI download for the kill info
        /// </summary>
        private async void UpdateSOVFromESI()
        {
            string url = @"https://esi.evetech.net/v1/sovereignty/map/?datasource=tranquility";
            string strContent = string.Empty;

            try
            {
                HttpClient hc = new HttpClient();
                var response = await hc.GetAsync(url);
                response.EnsureSuccessStatusCode();
                strContent = await response.Content.ReadAsStringAsync();
                JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                // JSON feed is now in the format : [{ "system_id": 30035042,  and then optionally alliance_id, corporation_id and corporation_id, faction_id },
                while(jsr.Read())
                {
                    if(jsr.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(jsr);
                        long systemID = long.Parse(obj["system_id"].ToString());

                        if(SystemIDToName.ContainsKey(systemID))
                        {
                            System es = GetEveSystem(SystemIDToName[systemID]);
                            if(es != null)
                            {
                                if(obj["alliance_id"] != null)
                                {
                                    es.SOVAllianceID = int.Parse(obj["alliance_id"].ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private async void UpdateSovStructureUpdate()
        {
            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.Sovereignty.Structure>> esr = await ESIClient.Sovereignty.Structures();
                if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.Sovereignty.Structure>>(esr))
                {
                    foreach(ESI.NET.Models.Sovereignty.Structure ss in esr.Data)
                    {
                        EVEData.System es = GetEveSystemFromID(ss.SolarSystemId);
                        if(es != null)
                        {
                            // structures :
                            // Old TCU  : 32226
                            // Old iHub :  32458

                            es.SOVAllianceID = ss.AllianceId;

                            if(ss.TypeId == 32226)
                            {
                                es.TCUVunerabliltyStart = ss.VulnerableStartTime;
                                es.TCUVunerabliltyEnd = ss.VulnerableEndTime;
                                es.TCUOccupancyLevel = (float)ss.VulnerabilityOccupancyLevel;
                            }

                            if(ss.TypeId == 32458)
                            {
                                es.IHubVunerabliltyStart = ss.VulnerableStartTime;
                                es.IHubVunerabliltyEnd = ss.VulnerableEndTime;
                                es.IHubOccupancyLevel = (float)ss.VulnerabilityOccupancyLevel;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Start the download for the Server Info
        /// </summary>
        private async void UpdateServerInfo()
        {
            try
            {
                ESI.NET.EsiResponse<ESI.NET.Models.Status.Status> esr = await ESIClient.Status.Retrieve();

                if(ESIHelpers.ValidateESICall<ESI.NET.Models.Status.Status>(esr))
                {
                    ServerInfo.Name = "Tranquility";
                    ServerInfo.NumPlayers = esr.Data.Players;
                    ServerInfo.ServerVersion = esr.Data.ServerVersion.ToString();
                }
                else
                {
                    ServerInfo.Name = "Tranquility";
                    ServerInfo.NumPlayers = 0;
                    ServerInfo.ServerVersion = "";
                }
            }
            catch { }
        }


        /// <summary>
        /// Load Infrastructure Hub Upgrades from a text file
        /// Format:
        /// SYSTEMNAME
        /// 1    Upgrade Name    Level    Status
        /// or (legacy format):
        /// Sovereignty Hub SYSTEMNAME
        /// 1    Upgrade Name    Level    Status
        /// </summary>
        public void LoadInfrastructureUpgrades(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string currentSystem = null;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string trimmedLine = line.Trim();

                    // Check if this line starts with a digit (upgrade line)
                    bool isUpgradeLine = char.IsDigit(trimmedLine.FirstOrDefault());

                    if (!isUpgradeLine)
                    {
                        // This is a system header line
                        // Support both "Sovereignty Hub SYSTEMNAME" and just "SYSTEMNAME"
                        if (trimmedLine.StartsWith("Sovereignty Hub "))
                        {
                            currentSystem = trimmedLine.Replace("Sovereignty Hub ", "").Trim();
                        }
                        else
                        {
                            currentSystem = trimmedLine;
                        }

                        // Clear existing upgrades for this system
                        System sys = GetEveSystem(currentSystem);
                        if (sys != null)
                        {
                            sys.InfrastructureUpgrades.Clear();
                        }
                    }
                    else if (currentSystem != null)
                    {
                        // Parse upgrade line
                        string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length >= 3)
                        {
                            System sys = GetEveSystem(currentSystem);
                            if (sys != null)
                            {
                                InfrastructureUpgrade upgrade = new InfrastructureUpgrade();

                                // Parse slot number
                                if (int.TryParse(parts[0], out int slotNum))
                                {
                                    upgrade.SlotNumber = slotNum;
                                }

                                // Parse upgrade name and level
                                // The upgrade name could be multiple words, and the level might be at the end
                                // Status is always the last word (Online/Offline)
                                string status = parts[parts.Length - 1];
                                upgrade.IsOnline = status.Equals("Online", StringComparison.OrdinalIgnoreCase);

                                // Check if second-to-last part is a number (level)
                                int levelIndex = -1;
                                int level = 0;
                                if (parts.Length >= 4 && int.TryParse(parts[parts.Length - 2], out level))
                                {
                                    upgrade.Level = level;
                                    levelIndex = parts.Length - 2;
                                }
                                else
                                {
                                    upgrade.Level = 0;
                                    levelIndex = parts.Length - 1;
                                }

                                // Build upgrade name from remaining parts
                                List<string> nameParts = new List<string>();
                                for (int i = 1; i < levelIndex; i++)
                                {
                                    nameParts.Add(parts[i]);
                                }
                                upgrade.UpgradeName = string.Join(" ", nameParts);

                                sys.InfrastructureUpgrades.Add(upgrade);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
            }
        }

        /// <summary>
        /// Save Infrastructure Hub Upgrades to a text file
        /// </summary>
        public void SaveInfrastructureUpgrades(string filePath)
        {
            try
            {
                List<string> lines = new List<string>();

                foreach (System sys in Systems)
                {
                    if (sys.InfrastructureUpgrades.Count > 0)
                    {
                        lines.Add(sys.Name);

                        foreach (InfrastructureUpgrade upgrade in sys.InfrastructureUpgrades.OrderBy(u => u.SlotNumber))
                        {
                            lines.Add(upgrade.ToString());
                        }

                        lines.Add(""); // Empty line between systems
                    }
                }

                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                // Log error if needed
            }
        }

        /// <summary>
        /// Add or update an infrastructure upgrade for a system
        /// </summary>
        public void SetInfrastructureUpgrade(string systemName, int slotNumber, string upgradeName, int level, bool isOnline)
        {
            System sys = GetEveSystem(systemName);
            if (sys != null)
            {
                // Check if upgrade already exists in this slot
                InfrastructureUpgrade existing = sys.InfrastructureUpgrades.FirstOrDefault(u => u.SlotNumber == slotNumber);

                if (existing != null)
                {
                    // Update existing
                    existing.UpgradeName = upgradeName;
                    existing.Level = level;
                    existing.IsOnline = isOnline;
                }
                else
                {
                    // Add new
                    sys.InfrastructureUpgrades.Add(new InfrastructureUpgrade
                    {
                        SlotNumber = slotNumber,
                        UpgradeName = upgradeName,
                        Level = level,
                        IsOnline = isOnline
                    });
                }
            }
        }

        /// <summary>
        /// Remove an infrastructure upgrade from a system
        /// </summary>
        public void RemoveInfrastructureUpgrade(string systemName, int slotNumber)
        {
            System sys = GetEveSystem(systemName);
            if (sys != null)
            {
                InfrastructureUpgrade upgrade = sys.InfrastructureUpgrades.FirstOrDefault(u => u.SlotNumber == slotNumber);
                if (upgrade != null)
                {
                    sys.InfrastructureUpgrades.Remove(upgrade);
                }
            }
        }
    }
}
