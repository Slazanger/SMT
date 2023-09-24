﻿//-----------------------------------------------------------------------
// EVE Manager
//-----------------------------------------------------------------------

using System.Data;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
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
    public class EveManager
    {
        /// <summary>
        /// singleton instance of this class
        /// </summary>
        private static EveManager instance;

        private bool BackgroundThreadShouldTerminate;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="EveManager" /> class
        /// </summary>
        public EveManager(string version)
        {
            LocalCharacters = new List<LocalCharacter>();
            VersionStr = version;

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
            Coalitions = new List<Coalition>();
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

        public List<Coalition> Coalitions { get; set; }

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
        /// Gets or sets the list of Characters we are tracking
        /// </summary>
        [XmlIgnoreAttribute]
        public List<LocalCharacter> LocalCharacters { get; set; }

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
        /// Gets or sets the Name to System dictionary
        /// </summary>
        private Dictionary<string, System> NameToSystem { get; }

        /// <summary>
        /// Gets or sets the ID to System dictionary
        /// </summary>
        private Dictionary<long, System> IDToSystem { get; }

        /// <summary>
        /// Scrape the maps from dotlan and initialise the region data from dotlan
        /// </summary>
        public void CreateFromScratch(string sourceFolder, string outputFolder)
        {
            // allow parsing to work for all locales (comma/dot in csv float)
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Regions = new List<MapRegion>();

            // manually add the regions we care about
            Regions.Add(new MapRegion("Aridia", "10000054", "Amarr", 280, 810));
            Regions.Add(new MapRegion("Black Rise", "10000069", "Caldari", 900, 500));
            Regions.Add(new MapRegion("The Bleak Lands", "10000038", "Amarr", 1000, 920));
            Regions.Add(new MapRegion("Branch", "10000055", string.Empty, 1040, 100));
            Regions.Add(new MapRegion("Cache", "10000007", string.Empty, 1930, 800));
            Regions.Add(new MapRegion("Catch", "10000014", string.Empty, 1110, 1280));
            Regions.Add(new MapRegion("The Citadel", "10000033", "Caldari", 1010, 620));
            Regions.Add(new MapRegion("Cloud Ring", "10000051", string.Empty, 500, 240));
            Regions.Add(new MapRegion("Cobalt Edge", "10000053", string.Empty, 1900, 130));
            Regions.Add(new MapRegion("Curse", "10000012", "Angel Cartel", 1350, 1120));
            Regions.Add(new MapRegion("Deklein", "10000035", string.Empty, 820, 150));
            Regions.Add(new MapRegion("Delve", "10000060", "Blood Raider", 230, 1210));
            Regions.Add(new MapRegion("Derelik", "10000001", "Ammatar", 1300, 970));
            Regions.Add(new MapRegion("Detorid", "10000005", string.Empty, 1760, 1400));
            Regions.Add(new MapRegion("Devoid", "10000036", "Amarr", 990, 1060));
            Regions.Add(new MapRegion("Domain", "10000043", "Amarr", 810, 960));
            Regions.Add(new MapRegion("Esoteria", "10000039", string.Empty, 880, 1450));
            Regions.Add(new MapRegion("Essence", "10000064", "Gallente", 740, 580));
            Regions.Add(new MapRegion("Etherium Reach", "10000027", string.Empty, 1570, 620));
            Regions.Add(new MapRegion("Everyshore", "10000037", "Gallente", 660, 730));
            Regions.Add(new MapRegion("Fade", "10000046", string.Empty, 720, 260));
            Regions.Add(new MapRegion("Feythabolis", "10000056", string.Empty, 1070, 1510));
            Regions.Add(new MapRegion("The Forge", "10000002", "Caldari", 1200, 620));
            Regions.Add(new MapRegion("Fountain", "10000058", string.Empty, 120, 500));
            Regions.Add(new MapRegion("Geminate", "10000029", "The Society", 1330, 490));
            Regions.Add(new MapRegion("Genesis", "10000067", "Amarr", 480, 860));
            Regions.Add(new MapRegion("Great Wildlands", "10000011", "Thukker Tribe", 1630, 920));
            Regions.Add(new MapRegion("Heimatar", "10000030", "Minmatar", 1220, 860));
            Regions.Add(new MapRegion("Immensea", "10000025", string.Empty, 1350, 1230));
            Regions.Add(new MapRegion("Impass", "10000031", string.Empty, 1200, 1390));
            Regions.Add(new MapRegion("Insmother", "10000009", string.Empty, 1880, 1160));
            Regions.Add(new MapRegion("Kador", "10000052", "Amarr", 660, 880));
            Regions.Add(new MapRegion("The Kalevala Expanse", "10000034", string.Empty, 1490, 370));
            Regions.Add(new MapRegion("Khanid", "10000049", "Khanid", 470, 1140));
            Regions.Add(new MapRegion("Kor-Azor", "10000065", "Amarr", 500, 1010));
            Regions.Add(new MapRegion("Lonetrek", "10000016", "Caldari", 1100, 460));
            Regions.Add(new MapRegion("Malpais", "10000013", string.Empty, 1770, 520));
            Regions.Add(new MapRegion("Metropolis", "10000042", "Minmatar", 1330, 730));
            Regions.Add(new MapRegion("Molden Heath", "10000028", "Minmatar", 1460, 860));
            Regions.Add(new MapRegion("Oasa", "10000040", string.Empty, 1890, 320));
            Regions.Add(new MapRegion("Omist", "10000062", string.Empty, 1440, 1480));
            Regions.Add(new MapRegion("Outer Passage", "10000021", string.Empty, 1930, 460));
            Regions.Add(new MapRegion("Outer Ring", "10000057", "ORE", 240, 280));
            Regions.Add(new MapRegion("Paragon Soul", "10000059", string.Empty, 640, 1480));
            Regions.Add(new MapRegion("Period Basis", "10000063", string.Empty, 440, 1400));
            Regions.Add(new MapRegion("Perrigen Falls", "10000066", string.Empty, 1600, 260));
            Regions.Add(new MapRegion("Placid", "10000048", "Gallente", 600, 440));
            Regions.Add(new MapRegion("Providence", "10000047", string.Empty, 1010, 1130));
            Regions.Add(new MapRegion("Pure Blind", "10000023", string.Empty, 870, 380));
            Regions.Add(new MapRegion("Querious", "10000050", string.Empty, 680, 1280));
            Regions.Add(new MapRegion("Scalding Pass", "10000008", string.Empty, 1600, 1080));
            Regions.Add(new MapRegion("Sinq Laison", "10000032", "Gallente", 950, 770));
            Regions.Add(new MapRegion("Solitude", "10000044", "Gallente", 310, 670));
            Regions.Add(new MapRegion("The Spire", "10000018", string.Empty, 1720, 700));
            Regions.Add(new MapRegion("Stain", "10000022", "Sansha", 900, 1350));
            Regions.Add(new MapRegion("Syndicate", "10000041", "Syndicate", 360, 500));
            Regions.Add(new MapRegion("Tash-Murkon", "10000020", "Amarr", 730, 1090));

            Regions.Add(new MapRegion("Tenal", "10000045", string.Empty, 1400, 140));
            Regions.Add(new MapRegion("Tenerifis", "10000061", string.Empty, 1430, 1350));
            Regions.Add(new MapRegion("Tribute", "10000010", string.Empty, 1070, 290));

            Regions.Add(new MapRegion("Vale of the Silent", "10000003", string.Empty, 1230, 380));
            Regions.Add(new MapRegion("Venal", "10000015", "Guristas", 1140, 210));
            Regions.Add(new MapRegion("Verge Vendor", "10000068", "Gallente", 490, 660));
            Regions.Add(new MapRegion("Wicked Creek", "10000006", string.Empty, 1580, 1230));
            Regions.Add(new MapRegion("Pochven", "10000008", "Triglavian", 50, 50));

            Regions.Add(new MapRegion("Warzone - Amarr vs Minmatar", "", "Faction War", 50, 120, true));
            Regions.Add(new MapRegion("Warzone - Caldari vs Gallente", "", "Faction War", 50, 190, true));

            SystemIDToName = new SerializableDictionary<long, string>();

            Systems = new List<System>();

            // update the region cache
            foreach (MapRegion rd in Regions)
            {


                string localSVG = sourceFolder + @"\data\SourceMaps\raw\" + rd.DotLanRef + "_layout.svg";

                if (!File.Exists(localSVG))
                {
                    // error
                    throw new NullReferenceException();
                }

                // parse the svg as xml
                XmlDocument xmldoc = new XmlDocument
                {
                    XmlResolver = null
                };
                FileStream fs = new FileStream(localSVG, FileMode.Open, FileAccess.Read);
                xmldoc.Load(fs);

                // get the svg/g/g sys use child nodes
                string systemsXpath = @"//*[@Type='system']";
                XmlNodeList xnl = xmldoc.SelectNodes(systemsXpath);

                foreach (XmlNode xn in xnl)
                {
                    long systemID = long.Parse(xn.Attributes["ID"].Value);
                    float x = float.Parse(xn.Attributes["x"].Value);
                    float y = float.Parse(xn.Attributes["y"].Value);

                    float RoundVal = 10.0f;
                    x = (float)Math.Round(x / RoundVal, 0) * RoundVal;
                    y = (float)Math.Round(y / RoundVal, 0) * RoundVal;

                    string name;
                    string region;

                    if (xn.Attributes["Name"] == null)
                    {
                        name = GetEveSystemFromID(systemID).Name;
                        region = GetEveSystemFromID(systemID).Region;
                    }
                    else
                    {
                        name = xn.Attributes["Name"].Value;
                        region = xn.Attributes["Region"].Value;
                    }

                    bool hasStation = false;
                    bool hasIceBelt = false;

                    // create and add the system
                    if (region == rd.Name)
                    {
                        System s = new System(name, systemID, rd.Name, hasStation, hasIceBelt);
                        if (GetEveSystem(name) != null)
                        {
                            int test = 0;
                            test++;
                        }
                        Systems.Add(s);
                        NameToSystem[name] = s;
                        IDToSystem[systemID] = s;
                    }

                    // create and add the map version
                    rd.MapSystems[name] = new MapSystem
                    {
                        Name = name,
                        Layout = new Vector2(x, y),
                        Region = region,
                        OutOfRegion = rd.Name != region,
                    };
                }
            }

            // now open up the eve static data export and extract some info from it
            string eveStaticDataSolarSystemFile = sourceFolder + @"\data\mapSolarSystems.csv";
            if (File.Exists(eveStaticDataSolarSystemFile))
            {
                StreamReader file = new StreamReader(eveStaticDataSolarSystemFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string regionID = bits[0];
                    string constID = bits[1];
                    string systemID = bits[2];
                    string systemName = bits[3]; // SystemIDToName[SystemID];

                    //CCP have their own version of what a Light Year is.. so instead of 9460730472580800.0 its this
                    // beware when converting units
                    decimal LYScale = 9460000000000000.0m;

                    decimal x = Convert.ToDecimal(bits[4]);
                    decimal y = Convert.ToDecimal(bits[5]);
                    decimal z = Convert.ToDecimal(bits[6]);
                    double security = Convert.ToDouble(bits[21]);
                    double radius = Convert.ToDouble(bits[23]);

                    System s = GetEveSystem(systemName);
                    if (s != null)
                    {
                        // note : scale the coordinates to Light Year scale as at M double doesnt have enough precision however decimal doesnt
                        // have the range for the calculations
                        s.ActualX = x / LYScale;
                        s.ActualY = y / LYScale;
                        s.ActualZ = z / LYScale;
                        s.TrueSec = security;
                        s.ConstellationID = constID;
                        s.RadiusAU = radius / 149597870700;

                        // manually patch pochven
                        if (regionID == "10000070")
                        {
                            s.Region = "Pochven";
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // now open up the eve static data export for the regions and extract some info from it
            string eveStaticDataRegionFile = sourceFolder + @"\data\mapRegions.csv";
            if (File.Exists(eveStaticDataRegionFile))
            {
                StreamReader file = new StreamReader(eveStaticDataRegionFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string regionName = bits[1]; // SystemIDToName[SystemID];

                    double x = Convert.ToDouble(bits[2]);
                    double y = Convert.ToDouble(bits[3]);
                    double z = Convert.ToDouble(bits[4]);

                    MapRegion r = GetRegion(regionName);
                    if (r != null)
                    {
                        r.RegionX = x / 9460730472580800.0;
                        r.RegionY = z / 9460730472580800.0;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            string eveStaticDataJumpsFile = sourceFolder + @"\data\mapSolarSystemJumps.csv";
            if (File.Exists(eveStaticDataJumpsFile))
            {
                StreamReader file = new StreamReader(eveStaticDataJumpsFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    long fromID = long.Parse(bits[2]);
                    long toID = long.Parse(bits[3]);

                    System from = GetEveSystemFromID(fromID);
                    System to = GetEveSystemFromID(toID);

                    if (from != null && to != null)
                    {
                        if (!from.Jumps.Contains(to.Name))
                        {
                            from.Jumps.Add(to.Name);
                        }
                        if (!to.Jumps.Contains(from.Name))
                        {
                            to.Jumps.Add(from.Name);
                        }
                    }
                }
            }

            string eveStaticDataJumpsExtraFile = sourceFolder + @"\data\mapSolarSystemJumpsExtra.csv";
            if (File.Exists(eveStaticDataJumpsExtraFile))
            {
                StreamReader file = new StreamReader(eveStaticDataJumpsExtraFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string fromName = bits[0];
                    string toName = bits[1];

                    System from = GetEveSystem(fromName);
                    System to = GetEveSystem(toName);

                    if (from != null && to != null)
                    {
                        if (!from.Jumps.Contains(to.Name))
                        {
                            from.Jumps.Add(to.Name);
                        }
                        if (!to.Jumps.Contains(from.Name))
                        {
                            to.Jumps.Add(from.Name);
                        }
                    }
                }
            }

            // now open up the eve static data export and extract some info from it
            string eveStaticDataStationsFile = sourceFolder + @"\data\staStations.csv";
            if (File.Exists(eveStaticDataStationsFile))
            {
                StreamReader file = new StreamReader(eveStaticDataStationsFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    long stationSystem = long.Parse(bits[8]);

                    System SS = GetEveSystemFromID(stationSystem);
                    if (SS != null)
                    {
                        SS.HasNPCStation = true;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // now open up the eve static data export and extract some info from it
            string eveStaticDataConstellationFile = sourceFolder + @"\data\mapConstellations.csv";
            if (File.Exists(eveStaticDataConstellationFile))
            {
                StreamReader file = new StreamReader(eveStaticDataConstellationFile);

                Dictionary<string, string> constMap = new Dictionary<string, string>();

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string constID = bits[1];
                    string constName = bits[2];

                    constMap[constID] = constName;
                }

                foreach (System s in Systems)
                {
                    s.ConstellationName = constMap[s.ConstellationID];
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // now open up the ice systems
            string iceSystemsFile = sourceFolder + @"\data\iceSystems.csv";
            if (File.Exists(iceSystemsFile))
            {
                StreamReader file = new StreamReader(iceSystemsFile);
                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    System s = GetEveSystem(line);
                    if (s != null)
                    {
                        s.HasIceBelt = true;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // now open up the ice systems
            string fwSystemsFile = sourceFolder + @"\data\factionWarfareSystems.csv";
            if (File.Exists(fwSystemsFile))
            {
                StreamReader file = new StreamReader(fwSystemsFile);
                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    System s = GetEveSystem(line);
                    if (s != null)
                    {
                        s.FactionWarSystem = true;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // now open up the blue a0 sun systems
            string blueSunSystemsFile = sourceFolder + @"\data\a0BlueStarSystems.csv";
            if (File.Exists(blueSunSystemsFile))
            {
                StreamReader file = new StreamReader(blueSunSystemsFile);
                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    System s = GetEveSystem(line);
                    if (s != null)
                    {
                        s.HasBlueA0Star = true;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            foreach (System s in Systems)
            {
                NameToSystem[s.Name] = s;
                IDToSystem[s.ID] = s;

                // default to no invasion
                s.TrigInvasionStatus = System.EdenComTrigStatus.None;
            }

            string trigSystemsFile = sourceFolder + @"\data\trigInvasionSystems.csv";
            if (File.Exists(trigSystemsFile))
            {
                StreamReader file = new StreamReader(trigSystemsFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string systemid = bits[0];
                    string status = bits[1];

                    System.EdenComTrigStatus invasionStatus = System.EdenComTrigStatus.None;
                    switch (status)
                    {
                        case "edencom_minor_victory":
                            invasionStatus = System.EdenComTrigStatus.EdencomMinorVictory;
                            break;

                        case "fortress":
                            invasionStatus = System.EdenComTrigStatus.Fortress;
                            break;

                        case "triglavian_minor_victory":
                            invasionStatus = System.EdenComTrigStatus.TriglavianMinorVictory;
                            break;
                    }

                    if (invasionStatus != System.EdenComTrigStatus.None)
                    {
                        System s = GetEveSystemFromID(long.Parse(systemid));
                        if (s != null)
                        {
                            s.TrigInvasionStatus = invasionStatus;
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // now create the voronoi regions
            foreach (MapRegion mr in Regions)
            {
                // enforce a minimum spread
                bool mrDone = false;
                int mrIteration = 0;
                float mrMinSpread = 49.0f;

                while (!mrDone)
                {
                    mrIteration++;
                    bool movedThisTime = false;

                    foreach (MapSystem sysA in mr.MapSystems.Values)
                    {
                        foreach (MapSystem sysB in mr.MapSystems.Values)
                        {
                            if (sysA == sysB)
                            {
                                continue;
                            }

                            float dx = sysA.Layout.X - sysB.Layout.X;
                            float dy = sysA.Layout.Y - sysB.Layout.Y;
                            float l = (float)Math.Sqrt(dx * dx + dy * dy);

                            float s = mrMinSpread - l;

                            if (s > 0)
                            {
                                movedThisTime = true;

                                // move apart
                                dx = dx / l;
                                dy = dy / l;

                                sysB.Layout = new Vector2(sysB.Layout.X - (dx * s / 2), sysB.Layout.Y - (dy * s / 2));
                                sysA.Layout = new Vector2(sysA.Layout.X + (dx * s / 2), sysA.Layout.Y + (dy * s / 2));
                            }
                        }
                    }

                    if (movedThisTime == false)
                    {
                        mrDone = true;
                    }

                    if (mrIteration > 20)
                    {
                        mrDone = true;
                    }
                }

                // collect the system points to generate them from
                List<Vector2f> points = new List<Vector2f>();

                foreach (MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    points.Add(new Vector2f(ms.Layout.X, ms.Layout.Y));
                }

                // generate filler points to help the voronoi to get better partitioning of open areas
                int division = 5;
                int minDistance = 100;
                int minDistanceOOR = 70;
                int margin = 180;

                List<Vector2f> fillerPoints = new List<Vector2f>();

                for (int ix = -margin; ix < 1050 + margin; ix += division)
                {
                    for (int iy = -margin; iy < 800 + margin; iy += division)
                    {
                        bool add = true;

                        foreach (MapSystem ms in mr.MapSystems.Values.ToList())
                        {
                            double dx = ms.Layout.X - ix;
                            double dy = ms.Layout.Y - iy;
                            double l = Math.Sqrt(dx * dx + dy * dy);

                            if (ms.OutOfRegion)
                            {
                                if (l < (minDistanceOOR))
                                {
                                    add = false;
                                    break;
                                }
                            }
                            else
                            {
                                if (l < (minDistance))
                                {
                                    add = false;
                                    break;
                                }
                            }
                        }

                        if (add)
                        {
                            fillerPoints.Add(new Vector2f(ix, iy));
                        }
                    }
                }
                points.AddRange(fillerPoints);

                Rectf clipRect = new Rectf(-margin, -margin, 1050 + 2 * margin, 800 + 2 * margin);

                // create the voronoi
                csDelaunay.Voronoi v = new csDelaunay.Voronoi(points, clipRect, 0);

                int i = 0;
                // extract the points from the graph for each cell
                foreach (MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    csDelaunay.Site s = v.SitesIndexedByLocation[points[i]];
                    i++;

                    List<Vector2f> cellList = s.Region(clipRect);
                    ms.Layout = new Vector2(s.x, s.y);

                    ms.CellPoints = new List<Vector2>();

                    foreach (Vector2f vc in cellList)
                    {
                        float RoundVal = 2.5f;

                        double finalX = vc.x;
                        double finalY = vc.y;

                        int X = (int)(Math.Round(finalX / RoundVal, 1, MidpointRounding.AwayFromZero) * RoundVal);
                        int Y = (int)(Math.Round(finalY / RoundVal, 1, MidpointRounding.AwayFromZero) * RoundVal);

                        ms.CellPoints.Add(new Vector2(X, Y));
                    }
                }
            }

            foreach (MapRegion rr in Regions)
            {
                // link to the real systems
                foreach (MapSystem ms in rr.MapSystems.Values.ToList())
                {
                    ms.ActualSystem = GetEveSystem(ms.Name);

                    if (!ms.OutOfRegion)
                    {
                        if (ms.ActualSystem.TrueSec >= 0.45)
                        {
                            rr.HasHighSecSystems = true;
                        }

                        if (ms.ActualSystem.TrueSec > 0.0 && ms.ActualSystem.TrueSec < 0.45)
                        {
                            rr.HasLowSecSystems = true;
                        }

                        if (ms.ActualSystem.TrueSec <= 0.0)
                        {
                            rr.HasNullSecSystems = true;
                        }
                    }

                    if (rr.MetaRegion)
                    {
                        ms.OutOfRegion = !ms.ActualSystem.FactionWarSystem;
                    }
                }
            }

            // collect the system points to generate them from
            List<Vector2f> regionpoints = new List<Vector2f>();

            // now Generate the region links
            foreach (MapRegion mr in Regions)
            {
                mr.RegionLinks = new List<string>();

                regionpoints.Add(new Vector2f(mr.UniverseViewX, mr.UniverseViewY));

                foreach (MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    // only check systems in the region
                    if (ms.ActualSystem.Region == mr.Name)
                    {
                        foreach (string s in ms.ActualSystem.Jumps)
                        {
                            System sys = GetEveSystem(s);

                            // we have link to another region
                            if (sys.Region != mr.Name)
                            {
                                if (!mr.RegionLinks.Contains(sys.Region))
                                {
                                    mr.RegionLinks.Add(sys.Region);
                                }
                            }
                        }
                    }
                }
            }

            // now get the ships
            string eveStaticDataItemTypesFile = sourceFolder + @"\data\invTypes.csv";
            if (File.Exists(eveStaticDataItemTypesFile))
            {
                ShipTypes = new SerializableDictionary<string, string>();

                List<string> ValidShipGroupIDs = new List<string>();

                ValidShipGroupIDs.Add("25"); //  Frigate
                ValidShipGroupIDs.Add("26"); //  Cruiser
                ValidShipGroupIDs.Add("27"); //  Battleship
                ValidShipGroupIDs.Add("28"); //  Industrial
                ValidShipGroupIDs.Add("29"); //  Capsule
                ValidShipGroupIDs.Add("30"); //  Titan
                ValidShipGroupIDs.Add("31"); //  Shuttle
                ValidShipGroupIDs.Add("237"); //  Corvette
                ValidShipGroupIDs.Add("324"); //  Assault Frigate
                ValidShipGroupIDs.Add("358"); //  Heavy Assault Cruiser
                ValidShipGroupIDs.Add("380"); //  Deep Space Transport
                ValidShipGroupIDs.Add("381"); //  Elite Battleship
                ValidShipGroupIDs.Add("419"); //  Combat Battlecruiser
                ValidShipGroupIDs.Add("420"); //  Destroyer
                ValidShipGroupIDs.Add("463"); //  Mining Barge
                ValidShipGroupIDs.Add("485"); //  Dreadnought
                ValidShipGroupIDs.Add("513"); //  Freighter
                ValidShipGroupIDs.Add("540"); //  Command Ship
                ValidShipGroupIDs.Add("541"); //  Interdictor
                ValidShipGroupIDs.Add("543"); //  Exhumer
                ValidShipGroupIDs.Add("547"); //  Carrier
                ValidShipGroupIDs.Add("659"); //  Supercarrier
                ValidShipGroupIDs.Add("830"); //  Covert Ops
                ValidShipGroupIDs.Add("831"); //  Interceptor
                ValidShipGroupIDs.Add("832"); //  Logistics
                ValidShipGroupIDs.Add("833"); //  Force Recon Ship
                ValidShipGroupIDs.Add("834"); //  Stealth Bomber
                ValidShipGroupIDs.Add("883"); //  Capital Industrial Ship
                ValidShipGroupIDs.Add("893"); //  Electronic Attack Ship
                ValidShipGroupIDs.Add("894"); //  Heavy Interdiction Cruiser
                ValidShipGroupIDs.Add("898"); //  Black Ops
                ValidShipGroupIDs.Add("900"); //  Marauder
                ValidShipGroupIDs.Add("902"); //  Jump Freighter
                ValidShipGroupIDs.Add("906"); //  Combat Recon Ship
                ValidShipGroupIDs.Add("941"); //  Industrial Command Ship
                ValidShipGroupIDs.Add("963"); //  Strategic Cruiser
                ValidShipGroupIDs.Add("1022"); //  Prototype Exploration Ship
                ValidShipGroupIDs.Add("1201"); //  Attack Battlecruiser
                ValidShipGroupIDs.Add("1202"); //  Blockade Runner
                ValidShipGroupIDs.Add("1283"); //  Expedition Frigate
                ValidShipGroupIDs.Add("1305"); //  Tactical Destroyer
                ValidShipGroupIDs.Add("1527"); //  Logistics Frigate
                ValidShipGroupIDs.Add("1534"); //  Command Destroyer
                ValidShipGroupIDs.Add("1538"); //  Force Auxiliary
                ValidShipGroupIDs.Add("1972"); //  Flag Cruiser
                // fighters
                ValidShipGroupIDs.Add("1537"); //  Support Fighter None    0   0   0   0   1
                ValidShipGroupIDs.Add("1652"); //  Light Fighter   None    0   0   0   0   1
                ValidShipGroupIDs.Add("1653"); //  Heavy Fighter   None    0   0   0   0   1

                // deployables
                ValidShipGroupIDs.Add("361");  //  Mobile Warp Disruptor
                ValidShipGroupIDs.Add("1149"); //  Mobile Jump Disruptor
                ValidShipGroupIDs.Add("1246"); //  Mobile Depot
                ValidShipGroupIDs.Add("1247"); //  Mobile Siphon Unit
                ValidShipGroupIDs.Add("1249"); //  Mobile Cyno Inhibitor
                ValidShipGroupIDs.Add("1250"); //  Mobile Tractor Unit
                ValidShipGroupIDs.Add("1273"); //  Encounter Surveillance System
                ValidShipGroupIDs.Add("1274"); //  Mobile Decoy Unit
                ValidShipGroupIDs.Add("1275"); //  Mobile Scan Inhibitor
                ValidShipGroupIDs.Add("1276"); //  Mobile Micro Jump Unit
                ValidShipGroupIDs.Add("1297"); //  Mobile Vault

                // structures
                ValidShipGroupIDs.Add("1312"); //  Observatory Structures
                ValidShipGroupIDs.Add("1404"); //  Engineering Complex
                ValidShipGroupIDs.Add("1405"); //  Laboratory
                ValidShipGroupIDs.Add("1406"); //  Refinery
                ValidShipGroupIDs.Add("1407"); //  Observatory Array
                ValidShipGroupIDs.Add("1408"); //  Stargate
                ValidShipGroupIDs.Add("1409"); //  Administration Hub
                ValidShipGroupIDs.Add("1410"); //  Advertisement Center

                // citadels
                ValidShipGroupIDs.Add("1657"); //  Citadel
                ValidShipGroupIDs.Add("1876"); //  Engineering Complex
                ValidShipGroupIDs.Add("1924"); //  Forward Operating Base

                StreamReader file = new StreamReader(eveStaticDataItemTypesFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    string[] bits = line.Split(',');

                    if (bits.Length < 3)
                    {
                        continue;
                    }

                    string typeID = bits[0];
                    string groupID = bits[1];
                    string ItemName = bits[2];

                    if (ValidShipGroupIDs.Contains(groupID))
                    {
                        ShipTypes.Add(typeID, ItemName);
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // now add the jove systems
            string eveStaticDataJoveObservatories = sourceFolder + @"\data\JoveSystems.csv";
            if (File.Exists(eveStaticDataJoveObservatories))
            {
                StreamReader file = new StreamReader(eveStaticDataJoveObservatories);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    string[] bits = line.Split(';');

                    if (bits.Length != 4)
                    {
                        continue;
                    }

                    string system = bits[0];

                    System s = GetEveSystem(system);
                    if (s != null)
                    {
                        s.HasJoveObservatory = true;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // now generate the 2d universe view coordinates

            double RenderSize = 5000;
            double universeXMin = 484452845697854000;
            double universeXMax = -484452845697854000;

            double universeZMin = 484452845697854000;
            double universeZMax = -472860102256057000.0;

            foreach (EVEData.System sys in Systems)
            {
                if ((double)sys.ActualX < universeXMin)
                {
                    universeXMin = (double)sys.ActualX;
                }

                if ((double)sys.ActualX > universeXMax)
                {
                    universeXMax = (double)sys.ActualX;
                }

                if ((double)sys.ActualZ < universeZMin)
                {
                    universeZMin = (double)sys.ActualZ;
                }

                if ((double)sys.ActualZ > universeZMax)
                {
                    universeZMax = (double)sys.ActualZ;
                }
            }
            double universeWidth = universeXMax - universeXMin;
            double universeDepth = universeZMax - universeZMin;
            double XScale = RenderSize / universeWidth;
            double ZScale = RenderSize / universeDepth;
            double universeScale = Math.Min(XScale, ZScale);

            foreach (EVEData.System sys in Systems)
            {
                double X = ((double)sys.ActualX - universeXMin) * universeScale;

                // need to invert Z
                double Z = (universeDepth - ((double)sys.ActualZ - universeZMin)) * universeScale;

                sys.UniverseX = X;
                sys.UniverseY = Z;
            }

            // now create the region outlines and recalc the centre
            foreach (MapRegion mr in Regions)
            {
                mr.RegionX = (mr.RegionX - universeXMin) * universeScale;
                mr.RegionY = (universeDepth - (mr.RegionY - universeZMin)) * universeScale;

                List<nAlpha.Point> regionShapePL = new List<nAlpha.Point>();
                foreach (System s in Systems)
                {
                    if (s.Region == mr.Name)
                    {
                        nAlpha.Point p = new nAlpha.Point(s.UniverseX, s.UniverseY);
                        regionShapePL.Add(p);
                    }
                }

                nAlpha.AlphaShapeCalculator shapeCalc = new nAlpha.AlphaShapeCalculator();
                shapeCalc.Alpha = 1 / (20 * 5.22295244275827E-15);
                shapeCalc.CloseShape = true;

                nAlpha.Shape ns = shapeCalc.CalculateShape(regionShapePL.ToArray());

                mr.RegionOutline = new List<Vector2>();

                List<Tuple<int, int>> processed = new List<Tuple<int, int>>();

                int CurrentPoint = 0;
                int count = 0;
                int edgeCount = ns.Edges.Length;
                while (count < edgeCount)
                {
                    foreach (Tuple<int, int> i in ns.Edges)
                    {
                        if (processed.Contains(i))
                            continue;

                        if (i.Item1 == CurrentPoint)
                        {
                            mr.RegionOutline.Add(new Vector2((int)ns.Vertices[CurrentPoint].X, (int)ns.Vertices[CurrentPoint].Y));
                            CurrentPoint = i.Item2;
                            processed.Add(i);
                            break;
                        }

                        if (i.Item2 == CurrentPoint)
                        {
                            mr.RegionOutline.Add(new Vector2((int)ns.Vertices[CurrentPoint].X, (int)ns.Vertices[CurrentPoint].Y));
                            CurrentPoint = i.Item1;
                            processed.Add(i);
                            break;
                        }
                    }

                    count++;
                }
            }

            bool done = false;
            int iteration = 0;
            double minSpread = 19.0;

            while (!done)
            {
                iteration++;
                bool movedThisTime = false;

                foreach (EVEData.System sysA in Systems)
                {
                    foreach (EVEData.System sysB in Systems)
                    {
                        if (sysA == sysB)
                        {
                            continue;
                        }

                        double dx = sysA.UniverseX - sysB.UniverseX;
                        double dy = sysA.UniverseY - sysB.UniverseY;
                        double l = Math.Sqrt(dx * dx + dy * dy);

                        double s = minSpread - l;

                        if (s > 0)
                        {
                            movedThisTime = true;

                            // move apart
                            dx = dx / l;
                            dy = dy / l;

                            sysB.UniverseX -= dx * s / 2;
                            sysB.UniverseY -= dy * s / 2;

                            sysA.UniverseX += dx * s / 2;
                            sysA.UniverseY += dy * s / 2;
                        }
                    }
                }

                if (movedThisTime == false)
                {
                    done = true;
                }

                if (iteration > 20)
                {
                    done = true;
                }
            }

            // cache the navigation data
            SerializableDictionary<string, List<string>> jumpRangeCache = Navigation.CreateStaticNavigationCache(Systems);

            // now serialise the classes to disk

            string saveDataFolder = outputFolder + @"\data\";

            Serialization.SerializeToDisk<SerializableDictionary<string, List<string>>>(jumpRangeCache, saveDataFolder + @"\JumpRangeCache.dat");
            Serialization.SerializeToDisk<SerializableDictionary<string, string>>(ShipTypes, saveDataFolder + @"\ShipTypes.dat");
            Serialization.SerializeToDisk<List<MapRegion>>(Regions, saveDataFolder + @"\MapLayout.dat");
            Serialization.SerializeToDisk<List<System>>(Systems, saveDataFolder + @"\Systems.dat");
        }

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
            if (AllianceIDToName.ContainsKey(id))
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
            if (AllianceIDToTicker.ContainsKey(id))
            {
                ticker = AllianceIDToTicker[id];
            }

            return ticker;
        }

        public string GetCharacterName(int id)
        {
            string name = string.Empty;
            if (CharacterIDToName.ContainsKey(id))
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
            if (NameToSystem.ContainsKey(name))
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
            if (IDToSystem.ContainsKey(id))
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
            if (s != null)
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

            if (systemFrom == null || systemTo == null || from == to)
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
            foreach (MapRegion reg in Regions)
            {
                if (reg.Name == name)
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
            if (SystemIDToName.ContainsKey(id))
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
            if (query["code"] == null)
            {
                // we're missing a query code
                return;
            }

            string code = query["code"];
            SsoToken sst;

            try
            {
                sst = await ESIClient.SSO.GetToken(GrantType.AuthorizationCode, code, challengeCode);
                if (sst == null || sst.ExpiresIn == 0)
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
            LocalCharacter esiChar = null;
            foreach (LocalCharacter c in LocalCharacters)
            {
                if (c.Name == acd.CharacterName)
                {
                    esiChar = c;
                }
            }

            if (esiChar == null)
            {
                esiChar = new LocalCharacter(acd.CharacterName, string.Empty, string.Empty);
                LocalCharacters.Add(esiChar);

                if (LocalCharacterUpdateEvent != null)
                {
                    LocalCharacterUpdateEvent();
                }
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

            if (!File.Exists(JRC))
            {
                throw new NotImplementedException();
            }
            jumpRangeCache = Serialization.DeserializeFromDisk<SerializableDictionary<string, List<string>>>(JRC);
            Navigation.InitNavigation(NameToSystem.Values.ToList(), JumpBridges, jumpRangeCache);
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

            foreach (System s in Systems)
            {
                SystemIDToName[s.ID] = s.Name;
            }

            CharacterIDToName = new SerializableDictionary<int, string>();
            AllianceIDToName = new SerializableDictionary<int, string>();
            AllianceIDToTicker = new SerializableDictionary<int, string>();

            // patch up any links
            foreach (System s in Systems)
            {
                NameToSystem[s.Name] = s;
                IDToSystem[s.ID] = s;
            }

            // now add the beacons
            string cynoBeaconsFile = Path.Combine(SaveDataRootFolder,  "CynoBeacons.txt");
            if (File.Exists(cynoBeaconsFile))
            {
                StreamReader file = new StreamReader(cynoBeaconsFile);

                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string system = line.Trim();

                    System s = GetEveSystem(system);
                    if (s != null)
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

            string dataFilename = Path.Combine(SaveDataRootFolder,  "JumpBridges_" + JumpBridge.SaveVersion + ".dat");
            if (!File.Exists(dataFilename))
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

                foreach (JumpBridge j in loadList)
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
            if (IDs.Count == 0)
            {
                return;
            }

            // strip out any ID's we already know..
            List<int> UnknownIDs = new List<int>();
            foreach (int l in IDs)
            {
                if (!AllianceIDToName.ContainsKey(l) || !AllianceIDToTicker.ContainsKey(l))
                {
                    UnknownIDs.Add(l);
                }
            }

            if (UnknownIDs.Count == 0)
            {
                return;
            }

            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.Universe.ResolvedInfo>> esra = await ESIClient.Universe.Names(UnknownIDs);
                if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.ResolvedInfo>>(esra))
                {
                    foreach (ESI.NET.Models.Universe.ResolvedInfo ri in esra.Data)
                    {
                        if (ri.Category == ResolvedInfoCategory.Alliance)
                        {
                            ESI.NET.EsiResponse<ESI.NET.Models.Alliance.Alliance> esraA = await ESIClient.Alliance.Information((int)ri.Id);

                            if (ESIHelpers.ValidateESICall<ESI.NET.Models.Alliance.Alliance>(esraA))
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
            if (IDs.Count == 0)
            {
                return;
            }

            // strip out any ID's we already know..
            List<int> UnknownIDs = new List<int>();
            foreach (int l in IDs)
            {
                if (!CharacterIDToName.ContainsKey(l))
                {
                    UnknownIDs.Add(l);
                }
            }

            if (UnknownIDs.Count == 0)
            {
                return;
            }

            try
            {
                ESI.NET.EsiResponse<List<ESI.NET.Models.Universe.ResolvedInfo>> esra = await ESIClient.Universe.Names(UnknownIDs);
                if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.ResolvedInfo>>(esra))
                {
                    foreach (ESI.NET.Models.Universe.ResolvedInfo ri in esra.Data)
                    {
                        if (ri.Category == ResolvedInfoCategory.Character)
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

            foreach (LocalCharacter c in LocalCharacters)
            {
                if (!string.IsNullOrEmpty(c.ESIRefreshToken))
                {
                    saveList.Add(c);
                }
            }

            XmlSerializer xms = new XmlSerializer(typeof(List<LocalCharacter>));
            string dataFilename = Path.Combine(SaveDataRootFolder, "Characters_" + LocalCharacter.SaveVersion + ".dat");

            using (TextWriter tw = new StreamWriter(dataFilename))
            {
                xms.Serialize(tw, saveList);
            }

            string jbFileName = Path.Combine(SaveDataRootFolder, "JumpBridges_" + JumpBridge.SaveVersion + ".dat");
            Serialization.SerializeToDisk<List<JumpBridge>>(JumpBridges, jbFileName);

            List<string> beaconsToSave = new List<string>();
            foreach (System s in Systems)
            {
                if (s.HasJumpBeacon)
                {
                    beaconsToSave.Add(s.Name);
                }
            }

            // save the intel channels / intel filters
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelChannels.txt"), IntelFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelClearFilters.txt"), IntelClearFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelIgnoreFilters.txt"), IntelIgnoreFilters);
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

            if (File.Exists(intelFileFilter))
            {
                StreamReader file = new StreamReader(intelFileFilter);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrEmpty(line))
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

            if (File.Exists(intelClearFileFilter))
            {
                StreamReader file = new StreamReader(intelClearFileFilter);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrEmpty(line))
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

            if (File.Exists(intelIgnoreFileFilter))
            {
                StreamReader file = new StreamReader(intelIgnoreFileFilter);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrEmpty(line))
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

            intelFileReadPos = new Dictionary<string, int>();

            if (string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
            {
                string[] logFolderLoc = { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "Logs" }; 
                EVELogFolder =  Path.Combine(logFolderLoc);
            }

            string chatlogFolder = Path.Combine(EVELogFolder, "Chatlogs");

            if (Directory.Exists(chatlogFolder))
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

            if (string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
            {
                string[] logFolderLoc = { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "Logs" };
                EVELogFolder = Path.Combine(logFolderLoc);
            }

            string gameLogFolder = Path.Combine(EVELogFolder, "Gamelogs") ;

            if (Directory.Exists(gameLogFolder))
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

            foreach (string dir in eveLogFolders)
            {
                if (!Directory.Exists(dir))
                {
                    return;
                }
            }

            // loop forever
            while (WatcherThreadShouldTerminate == false)
            {
                foreach (string folder in eveLogFolders)
                {
                    DirectoryInfo di = new DirectoryInfo(folder);
                    FileInfo[] files = di.GetFiles("*.txt");
                    foreach (FileInfo file in files)
                    {
                        bool readFile = false;
                        foreach (string intelFilterStr in IntelFilters)
                        {
                            if (file.Name.Contains(intelFilterStr, StringComparison.OrdinalIgnoreCase))
                            {
                                readFile = true;
                                break;
                            }
                        }

                        // local files
                        if (file.Name.Contains("Local_"))
                        {
                            readFile = true;
                        }

                        // gamelogs
                        if (folder.Contains("Gamelogs"))
                        {
                            readFile = true;
                        }

                        // only read files from the last day
                        if (file.CreationTime > DateTime.Now.AddDays(-1) && readFile)
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
            if (intelFileWatcher != null)
            {
                intelFileWatcher.Changed -= IntelFileWatcher_Changed;
            }
            WatcherThreadShouldTerminate = true;
        }

        public void ShuddownGameLogWatcher()
        {
            if (gameLogFileWatcher != null)
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
            UpdateDotlanKillDeltaInfo();
        }

        /// <summary>
        /// Update the Alliance and Ticker data for all SOV owners in the specified region
        /// </summary>
        public void UpdateIDsForMapRegion(string name)
        {
            MapRegion r = GetRegion(name);
            if (r == null)
            {
                return;
            }

            List<int> IDToResolve = new List<int>();

            foreach (KeyValuePair<string, MapSystem> kvp in r.MapSystems)
            {
                if (kvp.Value.ActualSystem.SOVAllianceTCU != 0 && !AllianceIDToName.ContainsKey(kvp.Value.ActualSystem.SOVAllianceTCU) && !IDToResolve.Contains(kvp.Value.ActualSystem.SOVAllianceTCU))
                {
                    IDToResolve.Add(kvp.Value.ActualSystem.SOVAllianceTCU);
                }

                if (kvp.Value.ActualSystem.SOVAllianceIHUB != 0 && !AllianceIDToName.ContainsKey(kvp.Value.ActualSystem.SOVAllianceIHUB) && !IDToResolve.Contains(kvp.Value.ActualSystem.SOVAllianceIHUB))
                {
                    IDToResolve.Add(kvp.Value.ActualSystem.SOVAllianceIHUB);
                }
            }

            _ = ResolveAllianceIDs(IDToResolve);
        }

        /// <summary>
        /// Update the current Thera Connections from EVE-Scout
        /// </summary>
        public async void UpdateTheraConnections()
        {
            string theraApiURL = "https://www.eve-scout.com/api/wormholes";
            string strContent = string.Empty;

            try
            {
                HttpClient hc = new HttpClient();
                var response = await hc.GetAsync(theraApiURL);
                response.EnsureSuccessStatusCode();
                strContent = await response.Content.ReadAsStringAsync();

                JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                TheraConnections.Clear();

                // JSON feed is now in the format : {"id":38199,"signatureId":"QRQ","type":"wormhole","status":"scanned","wormholeMass":"stable","wormholeEol":"critical","wormholeEstimatedEol":"2018-02-25T20:41:21.000Z","wormholeDestinationSignatureId":"VHT","createdAt":"2018-02-25T04:41:21.000Z","updatedAt":"2018-02-25T16:41:46.000Z","deletedAt":null,"statusUpdatedAt":"2018-02-25T04:41:44.000Z","createdBy":"Erik Holden","createdById":"95598233","deletedBy":null,"deletedById":null,"wormholeSourceWormholeTypeId":91,"wormholeDestinationWormholeTypeId":140,"solarSystemId":31000005,"wormholeDestinationSolarSystemId":30001175,"sourceWormholeType":
                while (jsr.Read())
                {
                    if (jsr.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(jsr);
                        string inSignatureId = obj["wormholeDestinationSignatureId"].ToString();
                        string outSignatureId = obj["signatureId"].ToString();
                        long solarSystemId = long.Parse(obj["wormholeDestinationSolarSystemId"].ToString());
                        string wormHoleEOL = obj["wormholeEol"].ToString();
                        string type = obj["type"].ToString();

                        if (type != null && type == "wormhole" && solarSystemId != 0 && wormHoleEOL != null && SystemIDToName.ContainsKey(solarSystemId))
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

            if (TheraUpdateEvent != null)
            {
                TheraUpdateEvent();
            }
        }

        public void UpdateMetaliminalStorms()
        {
            MetaliminalStorms.Clear();

            List<Storm> ls = Storm.GetStorms();
            foreach (Storm s in ls)
            {
                System sys = GetEveSystem(s.System);
                if (sys != null)
                {
                    MetaliminalStorms.Add(s);
                }
            }

            // now update the Strong and weak areas around the storm
            foreach (Storm s in MetaliminalStorms)
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
            if (StormsUpdateEvent != null)
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

                if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.FactionWarfare.FactionWarfareSystem>>(esr))
                {
                    foreach (ESI.NET.Models.FactionWarfare.FactionWarfareSystem i in esr.Data)
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
                foreach (FactionWarfareSystemInfo fws in FactionWarfareSystems)
                {
                    System s = GetEveSystemFromID(fws.SystemID);
                    foreach (string js in s.Jumps)
                    {
                        foreach (FactionWarfareSystemInfo fwss in FactionWarfareSystems)
                        {
                            if (fwss.SystemName == js && fwss.OccupierID != fws.OccupierID)
                            {
                                fwss.SystemState = FactionWarfareSystemInfo.State.Frontline;
                                fws.SystemState = FactionWarfareSystemInfo.State.Frontline;
                            }
                        }
                    }
                }

                // step 2, itendify all commandline operations by flooding out one from the frontlines
                foreach (FactionWarfareSystemInfo fws in FactionWarfareSystems)
                {
                    if (fws.SystemState == FactionWarfareSystemInfo.State.Frontline)
                    {
                        System s = GetEveSystemFromID(fws.SystemID);

                        foreach (string js in s.Jumps)
                        {
                            foreach (FactionWarfareSystemInfo fwss in FactionWarfareSystems)
                            {
                                if (fwss.SystemName == js && fwss.SystemState == FactionWarfareSystemInfo.State.None && fwss.OccupierID == fws.OccupierID)
                                {
                                    fwss.SystemState = FactionWarfareSystemInfo.State.CommandLineOperation;
                                    fwss.LinkSystemID = fws.SystemID;
                                }
                            }
                        }
                    }
                }

                // step 3, itendify all Rearguard operations by flooding out one from the command lines
                foreach (FactionWarfareSystemInfo fws in FactionWarfareSystems)
                {
                    if (fws.SystemState == FactionWarfareSystemInfo.State.CommandLineOperation)
                    {
                        System s = GetEveSystemFromID(fws.SystemID);

                        foreach (string js in s.Jumps)
                        {
                            foreach (FactionWarfareSystemInfo fwss in FactionWarfareSystems)
                            {
                                if (fwss.SystemName == js && fwss.SystemState == FactionWarfareSystemInfo.State.None && fwss.OccupierID == fws.OccupierID)
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
            if (GetEveSystem(from) == null || GetEveSystem(to) == null)
            {
                return;
            }

            bool found = false;

            foreach (JumpBridge jb in JumpBridges)
            {
                if (jb.From == from)
                {
                    found = true;
                    jb.FromID = stationID;
                }
                if (jb.To == from)
                {
                    found = true;
                    jb.ToID = stationID;
                }
            }

            if (!found)
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
                EsiUrl = "https://esi.evetech.net/",
                DataSource = DataSource.Tranquility,
                ClientId = EveAppConfig.ClientID,
                SecretKey = "Unneeded",
                CallbackUrl = EveAppConfig.CallbackURL,
                UserAgent = "SMT-map-app",
            });

            ESIClient = new ESI.NET.EsiClient(config);
            ESIScopes = new List<string>
            {
                "publicData",
                "esi-location.read_location.v1",
                "esi-search.search_structures.v1",
                "esi-clones.read_clones.v1",
                "esi-universe.read_structures.v1",
                "esi-fleets.read_fleet.v1",
                "esi-ui.write_waypoint.v1",
                "esi-characters.read_standings.v1",
                "esi-location.read_online.v1",
                "esi-characters.read_fatigue.v1",
                "esi-corporations.read_contacts.v1",
                "esi-alliances.read_contacts.v1"
            };

            foreach (MapRegion rr in Regions)
            {
                // link to the real systems
                foreach (KeyValuePair<string, MapSystem> kvp in rr.MapSystems)
                {
                    kvp.Value.ActualSystem = GetEveSystem(kvp.Value.Name);
                }
            }

            LoadCharacters();

            InitTheraConnections();
            InitMetaliminalStorms();
            InitFactionWarfareInfo();
            InitPOI();

            ActiveSovCampaigns = new List<SOVCampaign>();

            InitZKillFeed();

            // Removed as the api site is down
            //UpdateCoalitionInfo();

            StartBackgroundThread();
        }

        private void InitPOI()
        {
            PointsOfInterest = new List<POI>();

            try
            {
                string POIcsv = Path.Combine(DataRootFolder, "POI.csv");
                if (File.Exists(POIcsv))
                {
                    StreamReader file = new StreamReader(POIcsv);

                    string line;
                    line = file.ReadLine();
                    while ((line = file.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }
                        string[] bits = line.Split(',');

                        if (bits.Length < 4)
                        {
                            continue;
                        }

                        string system = bits[0];
                        string type = bits[1];
                        string desc = bits[2];
                        string longdesc = bits[3];

                        if (GetEveSystem(system) == null)
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
            ZKillFeed.VerString = VersionStr;
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
            foreach (string intelFilterStr in IntelFilters)
            {
                if (changedFile.Contains(intelFilterStr, StringComparison.OrdinalIgnoreCase))
                {
                    processFile = true;
                    break;
                }
            }

            if (changedFile.Contains("Local_"))
            {
                localChat = true;
                processFile = true;
            }

            if (processFile)
            {
                try
                {
                    Encoding fe = Misc.GetEncoding(changedFile);
                    FileStream ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    StreamReader file = new StreamReader(ifs, fe);

                    int fileReadFrom = 0;

                    // have we seen this file before
                    if (intelFileReadPos.ContainsKey(changedFile))
                    {
                        fileReadFrom = intelFileReadPos[changedFile];
                    }
                    else
                    {
                        if (localChat)
                        {
                            string system = string.Empty;
                            string characterName = string.Empty;

                            // read the iniital block
                            while (!file.EndOfStream)
                            {
                                string l = file.ReadLine();
                                fileReadFrom++;

                                // explicitly skip just "local"
                                if (l.Contains("Channel Name:    Local"))
                                {
                                    // now can read the next line
                                    l = file.ReadLine(); // should be the "Listener : <CharName>"
                                    fileReadFrom++;

                                    characterName = l.Split(':')[1].Trim();

                                    bool addChar = true;
                                    foreach (EVEData.LocalCharacter c in LocalCharacters)
                                    {
                                        if (characterName == c.Name)
                                        {
                                            c.Location = system;
                                            c.LocalChatFile = changedFile;

                                            System s = GetEveSystem(system);
                                            if (s != null)
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

                                    if (addChar)
                                    {
                                        LocalCharacters.Add(new EVEData.LocalCharacter(characterName, changedFile, system));
                                        if (LocalCharacterUpdateEvent != null)
                                        {
                                            LocalCharacterUpdateEvent();
                                        }
                                    }

                                    break;
                                }
                            }
                        }

                        while (file.ReadLine() != null)
                        {
                            fileReadFrom++;
                        }

                        fileReadFrom--;
                        file.BaseStream.Seek(0, SeekOrigin.Begin);
                    }

                    for (int i = 0; i < fileReadFrom; i++)
                    {
                        file.ReadLine();
                    }

                    string line = file.ReadLine();

                    while (line != null)
                    {                    // trim any items off the front
                        if (line.Contains('[') && line.Contains(']'))
                        {
                            line = line.Substring(line.IndexOf("["));
                        }

                        if (line == "")
                        {
                            line = file.ReadLine();
                            continue;
                        }

                        fileReadFrom++;

                        if (localChat)
                        {
                            if (line.StartsWith("[") && line.Contains("EVE System > Channel changed to Local"))
                            {
                                string system = line.Split(':').Last().Trim();

                                foreach (EVEData.LocalCharacter c in LocalCharacters)
                                {
                                    if (c.LocalChatFile == changedFile)
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

                            if (newIntelString != null)
                            {
                                foreach (EVEData.IntelData idl in IntelDataList)
                                {
                                    if (idl.IntelString == newIntelString && (DateTime.Now - idl.IntelTime).Seconds < 5)
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

                            if (line.Contains("Channel MOTD:"))
                            {
                                addToIntel = false;
                            }

                            foreach (String ignoreMarker in IntelIgnoreFilters)
                            {
                                if (line.IndexOf(ignoreMarker, StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    addToIntel = false;
                                    break;
                                }
                            }


                            if (addToIntel)
                            {
                                EVEData.IntelData id = new EVEData.IntelData(line, channelName);


                                foreach (string s in id.IntelString.Split(' '))
                                {
                                    if (s == "" || s.Length < 3)
                                    {
                                        continue;
                                    }

                                    foreach (String clearMarker in IntelClearFilters)
                                    {
                                        if (clearMarker.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            id.ClearNotification = true;
                                        }
                                    }

                                    foreach (System sys in Systems)
                                    {
                                        if (sys.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0 || s.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            id.Systems.Add(sys.Name);
                                        }
                                    }
                                }

                                IntelDataList.Enqueue(id);

                                if (IntelUpdatedEvent != null)
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
                if (gameFileReadPos.ContainsKey(changedFile))
                {
                    fileReadFrom = gameFileReadPos[changedFile];
                }
                else
                {
                    // read the iniital block
                    while (!file.EndOfStream)
                    {
                        string l = file.ReadLine();
                        fileReadFrom++;

                        // explicitly skip just "local"
                        if (l.Contains("Gamelog"))
                        {
                            // now can read the next line
                            l = file.ReadLine(); // should be the "Listener : <CharName>"

                            // something wrong with the log file; clear
                            if (!l.Contains("Listener"))
                            {
                                if (gameFileReadPos.ContainsKey(changedFile))
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

                    while (!file.EndOfStream)
                    {
                        string l = file.ReadLine();
                        fileReadFrom++;
                    }

                    // back one line
                    fileReadFrom--;

                    file.BaseStream.Seek(0, SeekOrigin.Begin);
                }

                characterName = gamelogFileCharacterMap[changedFile];

                for (int i = 0; i < fileReadFrom; i++)
                {
                    file.ReadLine();
                }

                string line = file.ReadLine();

                while (line != null)
                {                    // trim any items off the front
                    if (line == "" || !line.StartsWith("["))
                    {
                        line = file.ReadLine();
                        fileReadFrom++;
                        continue;
                    }

                    fileReadFrom++;

                    int typeStartPos = line.IndexOf("(") + 1;
                    int typeEndPos = line.IndexOf(")");

                    // file corrupt
                    if (typeStartPos < 1 || typeEndPos < 1)
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
                    if (GameLogAddedEvent != null)
                    {
                        GameLogAddedEvent(GameLogList);
                    }

                    foreach (LocalCharacter lc in LocalCharacters)
                    {
                        if (lc.Name == characterName)
                        {
                            if (type == "combat")
                            {
                                if (CombatEvent != null)
                                {
                                    lc.GameLogWarningText = line;
                                    CombatEvent(characterName, line);
                                }
                            }

                            if (line.Contains("cloak deactivates due to a pulse from a Mobile Observatory") || line.Contains("Your cloak deactivates due to proximity to"))
                            {
                                if (ShipDecloakedEvent != null)
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

            string dataFilename = Path.Combine(SaveDataRootFolder,  "Characters_" + LocalCharacter.SaveVersion + ".dat");
            if (!File.Exists(dataFilename))
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

                foreach (LocalCharacter c in loadList)
                {
                    c.ESIAccessToken = string.Empty;
                    c.ESIAccessTokenExpiry = DateTime.MinValue;
                    c.LocalChatFile = string.Empty;
                    c.Location = string.Empty;
                    c.Region = string.Empty;

                    LocalCharacters.Add(c);

                    if (LocalCharacterUpdateEvent != null)
                    {
                        LocalCharacterUpdateEvent();
                    }
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

                TimeSpan CharacterUpdateRate = TimeSpan.FromSeconds(1);
                TimeSpan LowFreqUpdateRate = TimeSpan.FromMinutes(20);
                TimeSpan SOVCampaignUpdateRate = TimeSpan.FromSeconds(30);

                DateTime NextCharacterUpdate = DateTime.Now;
                DateTime NextLowFreqUpdate = DateTime.Now;
                DateTime NextSOVCampaignUpdate = DateTime.Now;

                // loop forever
                while (BackgroundThreadShouldTerminate == false)
                {
                    // character Update
                    if ((NextCharacterUpdate - DateTime.Now).Ticks < 0)
                    {
                        NextCharacterUpdate = DateTime.Now + CharacterUpdateRate;

                        for (int i = 0; i < LocalCharacters.Count; i++)
                        {
                            LocalCharacter c = LocalCharacters.ElementAt(i);
                            await c.Update();
                        }
                    }

                    // sov update
                    if ((NextSOVCampaignUpdate - DateTime.Now).Ticks < 0)
                    {
                        NextSOVCampaignUpdate = DateTime.Now + SOVCampaignUpdateRate;
                        UpdateSovCampaigns();
                    }

                    // low frequency update
                    if ((NextLowFreqUpdate - DateTime.Now).Minutes < 0)
                    {
                        NextLowFreqUpdate = DateTime.Now + LowFreqUpdateRate;

                        UpdateESIUniverseData();
                        UpdateServerInfo();
                        UpdateTheraConnections();
                    }

                    Thread.Sleep(100);
                }
            }).Start();
        }

        private async void UpdateDotlanKillDeltaInfo()
        {
            foreach (MapRegion mr in Regions)
            {
                // clear the data set
                foreach (MapSystem ms in mr.MapSystems.Values)
                {
                    if (!ms.OutOfRegion)
                    {
                        ms.ActualSystem.NPCKillsDeltaLastHour = 0;
                    }
                }

                string url = @"http://evemaps.dotlan.net/js/" + mr.DotLanRef + ".js";
                string strContent = string.Empty;

                try
                {
                    HttpClient hc = new HttpClient();
                    hc.DefaultRequestHeaders.Add("User-Agent", EveAppConfig.SMT_VERSION);
                    var response = await hc.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    strContent = await response.Content.ReadAsStringAsync();

                    // this string is a javascript variable; so if we strip off the comment and variable we can parse it as raw json
                    int substrpos = strContent.IndexOf("{");
                    string json = strContent.Substring(substrpos - 1);

                    var systemData = Dotlan.SystemData.FromJson(json);

                    foreach (KeyValuePair<string, Dotlan.SystemData> kvp in systemData)
                    {
                        System s = GetEveSystemFromID(long.Parse(kvp.Key));
                        if (s != null && kvp.Value.Nd.HasValue)
                        {
                            s.NPCKillsDeltaLastHour = (int)kvp.Value.Nd.Value;
                        }
                    }
                }
                catch
                {
                }
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
                if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Incursions.Incursion>>(esr))
                {
                    foreach (ESI.NET.Models.Incursions.Incursion i in esr.Data)
                    {
                        foreach (long s in i.InfestedSystems)
                        {
                            EVEData.System sys = GetEveSystemFromID(s);
                            if (sys != null)
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
                if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.Jumps>>(esr))
                {
                    foreach (ESI.NET.Models.Universe.Jumps j in esr.Data)
                    {
                        EVEData.System es = GetEveSystemFromID(j.SystemId);
                        if (es != null)
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
                if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.Kills>>(esr))
                {
                    foreach (ESI.NET.Models.Universe.Kills k in esr.Data)
                    {
                        EVEData.System es = GetEveSystemFromID(k.SystemId);
                        if (es != null)
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

                foreach (SOVCampaign sc in ActiveSovCampaigns)
                {
                    sc.Valid = false;
                }

                List<int> allianceIDsToResolve = new List<int>();

                ESI.NET.EsiResponse<List<ESI.NET.Models.Sovereignty.Campaign>> esr = await ESIClient.Sovereignty.Campaigns();
                if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Sovereignty.Campaign>>(esr))
                {
                    foreach (ESI.NET.Models.Sovereignty.Campaign c in esr.Data)
                    {
                        SOVCampaign ss = null;

                        foreach (SOVCampaign asc in ActiveSovCampaigns)
                        {
                            if (asc.CampaignID == c.CampaignId)
                            {
                                ss = asc;
                            }
                        }

                        if (ss == null)
                        {
                            System sys = GetEveSystemFromID(c.SolarSystemId);
                            if (sys == null)
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

                            if (c.EventType == "ihub_defense")
                            {
                                ss.Type = "IHub";
                            }

                            if (c.EventType == "tcu_defense")
                            {
                                ss.Type = "TCU";
                            }

                            ActiveSovCampaigns.Add(ss);
                            sendUpdateEvent = true;
                        }

                        if (ss.AttackersScore != c.AttackersScore || ss.DefendersScore != c.DefenderScore)
                        {
                            sendUpdateEvent = true;
                        }

                        ss.AttackersScore = c.AttackersScore;
                        ss.DefendersScore = c.DefenderScore;
                        ss.Valid = true;

                        if (AllianceIDToName.ContainsKey(ss.DefendingAllianceID))
                        {
                            ss.DefendingAllianceName = AllianceIDToName[ss.DefendingAllianceID];
                        }
                        else
                        {
                            if (!allianceIDsToResolve.Contains(ss.DefendingAllianceID))
                            {
                                allianceIDsToResolve.Add(ss.DefendingAllianceID);
                            }
                        }

                        int NodesToWin = (int)Math.Ceiling(ss.DefendersScore / 0.07);
                        int NodesToDefend = (int)Math.Ceiling(ss.AttackersScore / 0.07);
                        ss.State = $"Nodes Remaining\nAttackers : {NodesToWin}\nDefenders : {NodesToDefend}";

                        ss.TimeToStart = ss.StartTime - DateTime.UtcNow;

                        if (ss.StartTime < DateTime.UtcNow)
                        {
                            ss.IsActive = true;
                        }
                        else
                        {
                            ss.IsActive = false;
                        }
                    }
                }

                if (allianceIDsToResolve.Count > 0)
                {
                    await ResolveAllianceIDs(allianceIDsToResolve);
                }

                foreach (SOVCampaign sc in ActiveSovCampaigns.ToList())
                {
                    if (string.IsNullOrEmpty(sc.DefendingAllianceName) && AllianceIDToName.ContainsKey(sc.DefendingAllianceID))
                    {
                        sc.DefendingAllianceName = AllianceIDToName[sc.DefendingAllianceID];
                    }

                    if (sc.Valid == false)
                    {
                        ActiveSovCampaigns.Remove(sc);
                        sendUpdateEvent = true;
                    }
                }

                if (sendUpdateEvent)
                {
                    if (SovUpdateEvent != null)
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
                while (jsr.Read())
                {
                    if (jsr.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(jsr);
                        long systemID = long.Parse(obj["system_id"].ToString());

                        if (SystemIDToName.ContainsKey(systemID))
                        {
                            System es = GetEveSystem(SystemIDToName[systemID]);
                            if (es != null)
                            {
                                if (obj["alliance_id"] != null)
                                {
                                    es.SOVAllianceTCU = int.Parse(obj["alliance_id"].ToString());
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
                if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Sovereignty.Structure>>(esr))
                {
                    foreach (ESI.NET.Models.Sovereignty.Structure ss in esr.Data)
                    {
                        EVEData.System es = GetEveSystemFromID(ss.SolarSystemId);
                        if (es != null)
                        {
                            if (ss.TypeId == 32226)
                            {
                                es.TCUVunerabliltyStart = ss.VulnerableStartTime;
                                es.TCUVunerabliltyEnd = ss.VulnerableEndTime;
                                es.TCUOccupancyLevel = (float)ss.VulnerabilityOccupancyLevel;
                                es.SOVAllianceTCU = ss.AllianceId;
                            }

                            if (ss.TypeId == 32458)
                            {
                                es.IHubVunerabliltyStart = ss.VulnerableStartTime;
                                es.IHubVunerabliltyEnd = ss.VulnerableEndTime;
                                es.IHubOccupancyLevel = (float)ss.VulnerabilityOccupancyLevel;
                                es.SOVAllianceIHUB = ss.AllianceId;
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

                if (ESIHelpers.ValidateESICall<ESI.NET.Models.Status.Status>(esr))
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

        public void RemoveCharacter(LocalCharacter lc)
        {
            LocalCharacters.Remove(lc);

            if (LocalCharacterUpdateEvent != null)
            {
                LocalCharacterUpdateEvent();
            }
        }
    }
}