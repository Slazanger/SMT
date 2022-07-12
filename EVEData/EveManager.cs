﻿//-----------------------------------------------------------------------
// EVE Manager
//-----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utils;


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
            LocalCharacters = new ObservableCollection<LocalCharacter>();
            VersionStr = version;

            string SaveDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT";
            if (!Directory.Exists(SaveDataRoot))
            {
                Directory.CreateDirectory(SaveDataRoot);
            }

            SaveDataRootFolder = SaveDataRoot;

            SaveDataVersionFolder = SaveDataRoot + "\\" + VersionStr;
            if (!Directory.Exists(SaveDataVersionFolder))
            {
                Directory.CreateDirectory(SaveDataVersionFolder);
            }

            string characterSaveFolder = SaveDataRootFolder + "\\Portraits";
            if (!Directory.Exists(characterSaveFolder))
            {
                Directory.CreateDirectory(characterSaveFolder);
            }

            CharacterIDToName = new SerializableDictionary<long, string>();
            AllianceIDToName = new SerializableDictionary<long, string>();
            AllianceIDToTicker = new SerializableDictionary<long, string>();
            NameToSystem = new Dictionary<string, System>();

            ServerInfo = new EVEData.Server();
        }

        /// <summary>
        /// Intel Added Event Handler
        /// </summary>
        public delegate void IntelAddedEventHandler(List<string> systems);

        /// <summary>
        /// Intel Added Event
        /// </summary>
        public event IntelAddedEventHandler IntelAddedEvent;




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

        public ObservableCollection<SOVCampaign> ActiveSovCampaigns { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Name dictionary
        /// </summary>
        public SerializableDictionary<long, string> AllianceIDToName { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Alliance Ticker dictionary
        /// </summary>
        public SerializableDictionary<long, string> AllianceIDToTicker { get; set; }

        /// <summary>
        /// Gets or sets the character cache
        /// </summary>
        [XmlIgnoreAttribute]
        public SerializableDictionary<string, Character> CharacterCache { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Name dictionary
        /// </summary>
        public SerializableDictionary<long, string> CharacterIDToName { get; set; }

        public List<Coalition> Coalitions { get; set; }

        public ESI.NET.EsiClient ESIClient { get; set; }
        public List<string> ESIScopes { get; set; }

        /// <summary>
        /// Gets or sets the Intel List
        /// </summary>
        public BindingList<EVEData.IntelData> IntelDataList { get; set; }

        /// <summary>
        /// Gets or sets the Gamelog List
        /// </summary>
        public BindingList<EVEData.GameLogData> GameLogList { get; set; }

        /// <summary>
        /// Gets or sets the current list of Jump Bridges
        /// </summary>
        public ObservableCollection<JumpBridge> JumpBridges { get; set; }

        /// <summary>
        /// Gets or sets the list of Characters we are tracking
        /// </summary>
        [XmlIgnoreAttribute]
        public ObservableCollection<LocalCharacter> LocalCharacters { get; set; }

        /// <summary>
        /// Gets or sets the master list of Regions
        /// </summary>
        public List<MapRegion> Regions { get; set; }

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
        public ObservableCollection<TheraConnection> TheraConnections { get; set; }

        public bool UseESIForCharacterPositions { get; set; }

        public ObservableCollection<Storm> MetaliminalStorms { get; set; }

        public ObservableCollection<POI> PointsOfInterest { get; set; }

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
        /// Scrape the maps from dotlan and initialise the region data from dotlan
        /// </summary>
        public void CreateFromScratch()
        {
            // allow parsing to work for all locales (comma/dot in csv float)
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Regions = new List<MapRegion>();

            // manually add the regions we care about
            Regions.Add(new MapRegion("Aridia", "10000054", "Amarr", 140, 405));
            Regions.Add(new MapRegion("Black Rise", "10000069", "Caldari", 450, 250));
            Regions.Add(new MapRegion("The Bleak Lands", "10000038", "Amarr", 500, 460));
            Regions.Add(new MapRegion("Branch", "10000055", string.Empty, 520, 50));
            Regions.Add(new MapRegion("Cache", "10000007", string.Empty, 965, 400));
            Regions.Add(new MapRegion("Catch", "10000014", string.Empty, 555, 640));
            Regions.Add(new MapRegion("The Citadel", "10000033", "Caldari", 505, 310));
            Regions.Add(new MapRegion("Cloud Ring", "10000051", string.Empty, 250, 120));
            Regions.Add(new MapRegion("Cobalt Edge", "10000053", string.Empty, 950, 65));
            Regions.Add(new MapRegion("Curse", "10000012", "Angel Cartel", 675, 560));
            Regions.Add(new MapRegion("Deklein", "10000035", string.Empty, 410, 75));
            Regions.Add(new MapRegion("Delve", "10000060", "Blood Raider", 115, 605));
            Regions.Add(new MapRegion("Derelik", "10000001", "Ammatar", 650, 485));
            Regions.Add(new MapRegion("Detorid", "10000005", string.Empty, 880, 700));
            Regions.Add(new MapRegion("Devoid", "10000036", "Amarr", 495, 530));
            Regions.Add(new MapRegion("Domain", "10000043", "Amarr", 405, 480));
            Regions.Add(new MapRegion("Esoteria", "10000039", string.Empty, 440, 725));
            Regions.Add(new MapRegion("Essence", "10000064", "Gallente", 370, 290));
            Regions.Add(new MapRegion("Etherium Reach", "10000027", string.Empty, 785, 310));
            Regions.Add(new MapRegion("Everyshore", "10000037", "Gallente", 330, 365));
            Regions.Add(new MapRegion("Fade", "10000046", string.Empty, 360, 130));
            Regions.Add(new MapRegion("Feythabolis", "10000056", string.Empty, 535, 755));
            Regions.Add(new MapRegion("The Forge", "10000002", "Caldari", 600, 310));
            Regions.Add(new MapRegion("Fountain", "10000058", string.Empty, 60, 250));
            Regions.Add(new MapRegion("Geminate", "10000029", "The Society", 665, 245));
            Regions.Add(new MapRegion("Genesis", "10000067", "Amarr", 240, 430));
            Regions.Add(new MapRegion("Great Wildlands", "10000011", "Thukker Tribe", 815, 460));
            Regions.Add(new MapRegion("Heimatar", "10000030", "Minmatar", 610, 430));
            Regions.Add(new MapRegion("Immensea", "10000025", string.Empty, 675, 615));
            Regions.Add(new MapRegion("Impass", "10000031", string.Empty, 600, 695));
            Regions.Add(new MapRegion("Insmother", "10000009", string.Empty, 945, 580));
            Regions.Add(new MapRegion("Kador", "10000052", "Amarr", 330, 440));
            Regions.Add(new MapRegion("The Kalevala Expanse", "10000034", string.Empty, 745, 185));
            Regions.Add(new MapRegion("Khanid", "10000049", "Khanid", 235, 570));
            Regions.Add(new MapRegion("Kor-Azor", "10000065", "Amarr", 250, 505));
            Regions.Add(new MapRegion("Lonetrek", "10000016", "Caldari", 550, 230));
            Regions.Add(new MapRegion("Malpais", "10000013", string.Empty, 885, 260));
            Regions.Add(new MapRegion("Metropolis", "10000042", "Minmatar", 665, 365));
            Regions.Add(new MapRegion("Molden Heath", "10000028", "Minmatar", 730, 430));
            Regions.Add(new MapRegion("Oasa", "10000040", string.Empty, 945, 160));
            Regions.Add(new MapRegion("Omist", "10000062", string.Empty, 720, 740));
            Regions.Add(new MapRegion("Outer Passage", "10000021", string.Empty, 965, 230));
            Regions.Add(new MapRegion("Outer Ring", "10000057", "ORE", 120, 140));
            Regions.Add(new MapRegion("Paragon Soul", "10000059", string.Empty, 320, 740));
            Regions.Add(new MapRegion("Period Basis", "10000063", string.Empty, 220, 700));
            Regions.Add(new MapRegion("Perrigen Falls", "10000066", string.Empty, 800, 130));
            Regions.Add(new MapRegion("Placid", "10000048", "Gallente", 300, 220));
            Regions.Add(new MapRegion("Providence", "10000047", string.Empty, 505, 585));
            Regions.Add(new MapRegion("Pure Blind", "10000023", string.Empty, 435, 190));
            Regions.Add(new MapRegion("Querious", "10000050", string.Empty, 340, 640));
            Regions.Add(new MapRegion("Scalding Pass", "10000008", string.Empty, 800, 540));
            Regions.Add(new MapRegion("Sinq Laison", "10000032", "Gallente", 475, 385));
            Regions.Add(new MapRegion("Solitude", "10000044", "Gallente", 155, 335));
            Regions.Add(new MapRegion("The Spire", "10000018", string.Empty, 860, 350));
            Regions.Add(new MapRegion("Stain", "10000022", "Sansha", 450, 675));
            Regions.Add(new MapRegion("Syndicate", "10000041", "Syndicate", 180, 250));
            Regions.Add(new MapRegion("Tash-Murkon", "10000020", "Amarr", 365, 545));
            Regions.Add(new MapRegion("Tenal", "10000045", string.Empty, 700, 70));
            Regions.Add(new MapRegion("Tenerifis", "10000061", string.Empty, 715, 675));
            Regions.Add(new MapRegion("Tribute", "10000010", string.Empty, 535, 145));
            Regions.Add(new MapRegion("Vale of the Silent", "10000003", string.Empty, 615, 190));
            Regions.Add(new MapRegion("Venal", "10000015", "Guristas", 570, 105));
            Regions.Add(new MapRegion("Verge Vendor", "10000068", "Gallente", 245, 330));
            Regions.Add(new MapRegion("Wicked Creek", "10000006", string.Empty, 790, 615));

            SystemIDToName = new SerializableDictionary<long, string>();

            Systems = new List<System>();

            // update the region cache
            foreach (MapRegion rd in Regions)
            {
                string localSVG = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\..\EVEData\data\SourceMaps\dotlan\" + rd.DotLanRef + ".svg";

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
                string systemsXpath = @"//*[@id='sysuse']";
                XmlNodeList xn = xmldoc.SelectNodes(systemsXpath);

                XmlNode sysUseNode = xn[0];
                foreach (XmlNode system in sysUseNode.ChildNodes)
                {
                    // extact the base from info from the g
                    long systemID = long.Parse(system.Attributes["id"].Value.Substring(3));
                    float x = float.Parse(system.Attributes["x"].Value) + (float.Parse(system.Attributes["width"].Value) / 2.0f);
                    float y = float.Parse(system.Attributes["y"].Value) + (float.Parse(system.Attributes["height"].Value) / 2.0f);

                    float RoundVal = 5.0f;
                    x = (float)Math.Round(x / RoundVal, 0) * RoundVal;
                    y = (float)Math.Round(y / RoundVal, 0) * RoundVal;

                    string systemnodepath = @"//*[@id='def" + systemID + "']";
                    XmlNodeList snl = xmldoc.SelectNodes(systemnodepath);
                    XmlNode sysdefNode = snl[0];

                    XmlNode aNode = sysdefNode.ChildNodes[0];

                    string name;
                    bool hasStation = false;
                    bool hasIceBelt = false;
                    XmlNodeList iceNodes = aNode.SelectNodes(@".//*[@class='i']");
                    if (iceNodes[0] != null)
                    {
                        hasIceBelt = true;
                    }

                    // SS Nodes for system nodes
                    XmlNodeList ssNodes = aNode.SelectNodes(@".//*[@class='ss']");
                    if (ssNodes[0] != null)
                    {
                        name = ssNodes[0].InnerText;

                        // create and add the system
                        System s = new System(name, systemID, rd.Name, hasStation, hasIceBelt);
                        Systems.Add(s);

                        NameToSystem[name] = s;

                        // create and add the map version
                        rd.MapSystems[name] = new MapSystem
                        {
                            Name = name,
                            LayoutX = x,
                            LayoutY = y,
                            Region = rd.Name,
                            OutOfRegion = false,
                        };

                        SystemIDToName[systemID] = name;
                    }
                    else
                    {
                        // er / es nodes are region and constellation links
                        XmlNodeList esNodes = aNode.SelectNodes(@".//*[@class='es']");
                        XmlNodeList erNodes = aNode.SelectNodes(@".//*[@class='er']");

                        if (esNodes[0] != null && erNodes[0] != null)
                        {
                            name = esNodes[0].InnerText;
                            string regionLinkName = erNodes[0].InnerText;

                            SystemIDToName[systemID] = name;

                            rd.MapSystems[name] = new MapSystem
                            {
                                Name = name,
                                LayoutX = x,
                                LayoutY = y,
                                Region = regionLinkName,
                                OutOfRegion = true,
                            };
                        }
                    }
                }
                // extract Ice Systems
            }

            // now open up the eve static data export and extract some info from it
            string eveStaticDataSolarSystemFile = AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\..\..\EVEData\data\mapSolarSystems.csv";
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

                    double x = Convert.ToDouble(bits[4]);
                    double y = Convert.ToDouble(bits[5]);
                    double z = Convert.ToDouble(bits[6]);
                    double security = Convert.ToDouble(bits[21]);
                    double radius = Convert.ToDouble(bits[23]);

                    System s = GetEveSystem(systemName);
                    if (s != null)
                    {
                        s.ActualX = x;
                        s.ActualY = y;
                        s.ActualZ = z;
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
            string eveStaticDataRegionFile = AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\..\..\EVEData\data\mapRegions.csv";
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
                        r.RegionX = x;
                        r.RegionY = z;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            string eveStaticDataJumpsFile = AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\..\..\EVEData\data\mapSolarSystemJumps.csv";
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
                        from.Jumps.Add(to.Name);
                        to.Jumps.Add(from.Name);
                    }
                }
            }

            // now open up the eve static data export and extract some info from it
            string eveStaticDataStationsFile = AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\..\..\EVEData\data\staStations.csv";
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
            string eveStaticDataConstellationFile = AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\..\..\EVEData\data\mapConstellations.csv";
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

            foreach (System s in Systems)
            {
                NameToSystem[s.Name] = s;

                // default to no invasion
                s.TrigInvasionStatus = System.EdenComTrigStatus.None;
            }

            // 13 Oct 2020 :  Temp rebuild of maps for Trig Invasions

            // Krai Perun
            // 20000787    30000157    Otela
            // 20000787    30000192    Otanuomi
            // 20000787    30001372    Kino
            // 20000787    30001445    Nalvula
            // 20000787    30002079    Krirald
            // 20000787    30002737    Konola
            // 20000787    30005005    Ignebaener
            // 20000787    30010141    Sakenta
            // 20000787    30031392    Komo

            // Krai Svarog
            // 20000788    30000021    Kuharah
            // 20000788    30001413    Nani
            // 20000788    30002225    Harva
            // 20000788    30002411    Skarkon
            // 20000788    30002770    Tunudan
            // 20000788    30003495    Raravoss
            // 20000788    30003504    Niarja
            // 20000788    30040141    Urhinichi
            // 20000788    30045328    Ahtila

            // Krai Veles
            // 20000789    30000206    Wirashoda
            // 20000789    30001381    Arvasaras
            // 20000789    30002652    Ala
            // 20000789    30002702    Archee
            // 20000789    30002797    Kaunokka
            // 20000789    30003046    Angymonne
            // 20000789    30005029    Vale
            // 20000789    30020141    Senda
            // 20000789    30045329    Ichoriya

            EVEData.MapRegion blackRise = GetRegion("Black Rise");
            blackRise.MapSystems.Remove("Ahtila");  // Pochven
            blackRise.MapSystems.Remove("Ichoriya"); // Pochven

            EVEData.MapRegion theBleakLands = GetRegion("The Bleak Lands");
            theBleakLands.MapSystems.Remove("Raravoss"); // out of region link

            EVEData.MapRegion theCitadel = GetRegion("The Citadel");
            theCitadel.MapSystems.Remove("Konola"); // Pochven
            theCitadel.MapSystems.Remove("Komo"); // Pochven
            theCitadel.MapSystems.Remove("Kaunokka"); // Pochven
            theCitadel.MapSystems.Remove("Tunudan"); // Pochven
            theCitadel.MapSystems.Remove("Urhinichi"); // Pochven
            theCitadel.MapSystems.Remove("Kino"); // out of region link
            theCitadel.MapSystems.Remove("Niarja"); // out of region link

            EVEData.MapRegion derelik = GetRegion("Derelik");
            derelik.MapSystems.Remove("Kuharah"); // Pochven

            EVEData.MapRegion domain = GetRegion("Domain");
            domain.MapSystems.Remove("Niarja"); // Pochven
            domain.MapSystems.Remove("Raravoss"); // Pochven
            domain.MapSystems.Remove("Harva"); // Pochven
            domain.MapSystems.Remove("Kaaputenen"); // No longer connected

            EVEData.MapRegion essence = GetRegion("Essence");
            essence.MapSystems.Remove("Vale"); // Pochven
            essence.MapSystems.Remove("Ignebaener"); // Pochven

            EVEData.MapRegion etheriumReach = GetRegion("Etherium Reach");
            etheriumReach.MapSystems.Remove("Skarkon"); // out of region link

            EVEData.MapRegion everyshore = GetRegion("Everyshore");
            everyshore.MapSystems.Remove("Angymonne");  // Pochven

            EVEData.MapRegion theForge = GetRegion("The Forge");
            theForge.MapSystems.Remove("Otanuomi");  // Pochven
            theForge.MapSystems.Remove("Wirashoda");  // Pochven
            theForge.MapSystems.Remove("Sakenta");  // Pochven
            theForge.MapSystems.Remove("Otela");  // Pochven
            theForge.MapSystems.Remove("Senda");  // Pochven

            EVEData.MapRegion lonetrek = GetRegion("Lonetrek");
            lonetrek.MapSystems.Remove("Nalvula");  // Pochven
            lonetrek.MapSystems.Remove("Kino");  // Pochven
            lonetrek.MapSystems.Remove("Nani");  // Pochven
            lonetrek.MapSystems.Remove("Arvasaras");  // Pochven

            EVEData.MapRegion metropolis = GetRegion("Metropolis");
            metropolis.MapSystems.Remove("Krirald");  // Pochven

            EVEData.MapRegion moldenHeath = GetRegion("Molden Heath");
            moldenHeath.MapSystems.Remove("Skarkon");  // Pochven
            moldenHeath.MapSystems.Remove("L4X-1V");  // no longer connected

            EVEData.MapRegion sinqLaison = GetRegion("Sinq Laison");
            sinqLaison.MapSystems.Remove("Archee");  // Pochven
            sinqLaison.MapSystems.Remove("Ala");  // Pochven

            EVEData.MapRegion tribute = GetRegion("Tribute");
            tribute.MapSystems.Remove("Nalvula");  // out of region

            EVEData.MapRegion vergeVendor = GetRegion("Verge Vendor");
            vergeVendor.MapSystems.Remove("Ignebaener");  // out of region
            vergeVendor.MapSystems.Remove("Lisbaetanne");  // doesnt make sense to keep

            // now create Pochven from scratch
            MapRegion pochven = new MapRegion("Pochven", "10000008", "Triglavian", 50, 50);
            Regions.Add(pochven);

            // Krai Perun
            pochven.MapSystems.Add("Otela", new MapSystem() { Name = "Otela", LayoutX = 915, LayoutY = 360, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Otanuomi", new MapSystem() { Name = "Otanuomi", LayoutX = 600, LayoutY = 550, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Kino", new MapSystem() { Name = "Kino", LayoutX = 790, LayoutY = 390, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Nalvula", new MapSystem() { Name = "Nalvula", LayoutX = 870, LayoutY = 445, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Krirald", new MapSystem() { Name = "Krirald", LayoutX = 690, LayoutY = 520, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Konola", new MapSystem() { Name = "Konola", LayoutX = 780, LayoutY = 480, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Ignebaener", new MapSystem() { Name = "Ignebaener", LayoutX = 835, LayoutY = 320, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Sakenta", new MapSystem() { Name = "Sakenta", LayoutX = 680, LayoutY = 245, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Komo", new MapSystem() { Name = "Komo", LayoutX = 760, LayoutY = 285, Region = "Pochven", OutOfRegion = false });

            // Krai Svarog
            pochven.MapSystems.Add("Kuharah", new MapSystem() { Name = "Kuharah", LayoutX = 115, LayoutY = 500, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Nani", new MapSystem() { Name = "Nani", LayoutX = 350, LayoutY = 665, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Harva", new MapSystem() { Name = "Harva", LayoutX = 95, LayoutY = 670, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Skarkon", new MapSystem() { Name = "Skarkon", LayoutX = 255, LayoutY = 705, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Tunudan", new MapSystem() { Name = "Tunudan", LayoutX = 105, LayoutY = 585, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Raravoss", new MapSystem() { Name = "Raravoss", LayoutX = 160, LayoutY = 745, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Niarja", new MapSystem() { Name = "Niarja", LayoutX = 185, LayoutY = 645, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Urhinichi", new MapSystem() { Name = "Urhinichi", LayoutX = 440, LayoutY = 625, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Ahtila", new MapSystem() { Name = "Ahtila", LayoutX = 120, LayoutY = 435, Region = "Pochven", OutOfRegion = false });

            // Krai Veles
            pochven.MapSystems.Add("Wirashoda", new MapSystem() { Name = "Wirashoda", LayoutX = 145, LayoutY = 215, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Arvasaras", new MapSystem() { Name = "Arvasaras", LayoutX = 525, LayoutY = 170, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Ala", new MapSystem() { Name = "Ala", LayoutX = 155, LayoutY = 145, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Archee", new MapSystem() { Name = "Archee", LayoutX = 235, LayoutY = 130, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Kaunokka", new MapSystem() { Name = "Kaunokka", LayoutX = 445, LayoutY = 135, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Angymonne", new MapSystem() { Name = "Angymonne", LayoutX = 280, LayoutY = 50, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Vale", new MapSystem() { Name = "Vale", LayoutX = 170, LayoutY = 70, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Senda", new MapSystem() { Name = "Senda", LayoutX = 135, LayoutY = 295, Region = "Pochven", OutOfRegion = false });
            pochven.MapSystems.Add("Ichoriya", new MapSystem() { Name = "Ichoriya", LayoutX = 365, LayoutY = 100, Region = "Pochven", OutOfRegion = false });

            // now add the additional systems added with trailblazers release

            // Caldari Hykkota to Ahbazon
            EVEData.MapRegion genesis = GetRegion("Genesis");

            theForge.MapSystems.Add("Ahbazon", new MapSystem() { Name = "Ahbazon", LayoutX = 25, LayoutY = 225, Region = "Genesis", OutOfRegion = true });
            genesis.MapSystems.Add("Hykkota", new MapSystem() { Name = "Hykkota", LayoutX = 755, LayoutY = 195, Region = "The Forge", OutOfRegion = true });

            NameToSystem["Ahbazon"].Jumps.Add("Hykkota");
            NameToSystem["Hykkota"].Jumps.Add("Ahbazon");

            // Amarr Saminer to F7-ICZ
            EVEData.MapRegion tashMurkon = GetRegion("Tash-Murkon");
            EVEData.MapRegion stain = GetRegion("Stain");

            tashMurkon.MapSystems.Add("F7-ICZ", new MapSystem() { Name = "F7-ICZ", LayoutX = 30, LayoutY = 95, Region = "Stain", OutOfRegion = true });
            stain.MapSystems.Add("Saminer", new MapSystem() { Name = "Saminer", LayoutX = 150, LayoutY = 40, Region = "Tash-Murkon", OutOfRegion = true });

            NameToSystem["Saminer"].Jumps.Add("F7-ICZ");
            NameToSystem["F7-ICZ"].Jumps.Add("Saminer");

            // Minmatar - Irgrus to Pakhshi – IRGRUS TO PAKHSHI
            metropolis.MapSystems["Irgrus"].LayoutX -= 20; // move up slightly
            metropolis.MapSystems["Irgrus"].LayoutY -= 40; // move up slightly

            genesis.MapSystems.Add("Irgrus", new MapSystem() { Name = "Irgrus", LayoutX = 525, LayoutY = 105, Region = "Metropolis", OutOfRegion = true });
            metropolis.MapSystems.Add("Pakhshi", new MapSystem() { Name = "Pakhshi", LayoutX = 425, LayoutY = 750, Region = "Genesis", OutOfRegion = true });

            NameToSystem["Irgrus"].Jumps.Add("Pakhshi");
            NameToSystem["Pakhshi"].Jumps.Add("Irgrus");

            //GALLENTE – Kenninck TO Eggheron

            EVEData.MapRegion placid = GetRegion("Placid");
            EVEData.MapRegion solitude = GetRegion("Solitude");

            solitude.MapSystems.Add("Kenninck", new MapSystem() { Name = "Kenninck", LayoutX = 285, LayoutY = 30, Region = "Placid", OutOfRegion = true });
            placid.MapSystems.Add("Eggheron", new MapSystem() { Name = "Eggheron", LayoutX = 40, LayoutY = 605, Region = "Solitude", OutOfRegion = true });

            NameToSystem["Kenninck"].Jumps.Add("Eggheron");
            NameToSystem["Eggheron"].Jumps.Add("Kenninck");

            // now read the trig/edencom systems

            string trigSystemsFile = AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\..\..\EVEData\data\trigInvasionSystems.csv";
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
                // collect the system points to generate them from
                List<Vector2f> points = new List<Vector2f>();

                foreach (MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    points.Add(new Vector2f(ms.LayoutX, ms.LayoutY));
                }

                // create the voronoi
                csDelaunay.Voronoi v = new csDelaunay.Voronoi(points, new Rectf(0, 0, 1050, 800));

                // extract the points from the graph for each cell
                foreach (MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    List<Vector2f> cellList = v.Region(new Vector2f(ms.LayoutX, ms.LayoutY));
                    ms.CellPoints = new List<Point>();

                    foreach (Vector2f vc in cellList)
                    {
                        float RoundVal = 2.5f;
                        ms.CellPoints.Add(new Point(Math.Round(vc.x / RoundVal, 1, MidpointRounding.AwayFromZero) * RoundVal, Math.Round(vc.y / RoundVal, 1, MidpointRounding.AwayFromZero) * RoundVal));
                        //ms.CellPoints.Add(new Point(vc.x, vc.y));
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
            string eveStaticDataItemTypesFile = AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\..\..\EVEData\data\invTypes.csv";
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
            string eveStaticDataJoveObservatories = AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\..\..\EVEData\data\JoveSystems.csv";
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
            double universeXMin = 0.0;
            double universeXMax = 336522971264518000.0;

            double universeZMin = -484452845697854000;
            double universeZMax = 472860102256057000.0;

            foreach (EVEData.System sys in Systems)
            {
                if (sys.ActualX < universeXMin)
                {
                    universeXMin = sys.ActualX;
                }

                if (sys.ActualX > universeXMax)
                {
                    universeXMax = sys.ActualX;
                }

                if (sys.ActualZ < universeZMin)
                {
                    universeZMin = sys.ActualZ;
                }

                if (sys.ActualZ > universeZMax)
                {
                    universeZMax = sys.ActualZ;
                }
            }
            double universeWidth = universeXMax - universeXMin;
            double universeDepth = universeZMax - universeZMin;
            double XScale = (RenderSize) / universeWidth;
            double ZScale = (RenderSize) / universeDepth;
            double universeScale = Math.Min(XScale, ZScale);

            foreach (EVEData.System sys in Systems)
            {
                double X = (sys.ActualX - universeXMin) * universeScale;

                // need to invert Z
                double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;

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
                shapeCalc.Alpha = 1 / (20 * 9460730472580800.0 * 5.22295244275827E-15);
                shapeCalc.CloseShape = true;

                nAlpha.Shape ns = shapeCalc.CalculateShape(regionShapePL.ToArray());

                mr.RegionOutline = new List<Point>();

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
                            mr.RegionOutline.Add(new Point(ns.Vertices[CurrentPoint].X, ns.Vertices[CurrentPoint].Y));
                            CurrentPoint = i.Item2;
                            processed.Add(i);
                            break;
                        }

                        if (i.Item2 == CurrentPoint)
                        {
                            mr.RegionOutline.Add(new Point(ns.Vertices[CurrentPoint].X, ns.Vertices[CurrentPoint].Y));
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

            // now serialise the classes to disk

            string saveDataFolder = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\..\EVEData\data\";

            Serialization.SerializeToDisk<SerializableDictionary<string, string>>(ShipTypes, saveDataFolder + @"\ShipTypes.dat");
            Serialization.SerializeToDisk<List<MapRegion>>(Regions, saveDataFolder + @"\MapLayout.dat");
            Serialization.SerializeToDisk<List<System>>(Systems, saveDataFolder + @"\Systems.dat");


            // debug






            // now close 

            Application.Current.Shutdown();
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
        public string GetAllianceName(long id)
        {
            string name = string.Empty;
            if (AllianceIDToName.Keys.Contains(id))
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
        public string GetAllianceTicker(long id)
        {
            string ticker = string.Empty;
            if (AllianceIDToTicker.Keys.Contains(id))
            {
                ticker = AllianceIDToTicker[id];
            }

            return ticker;
        }

        public string GetCharacterName(long id)
        {
            string name = string.Empty;
            if (CharacterIDToName.Keys.Contains(id))
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
            foreach (System s in Systems)
            {
                if (s.ID == id)
                {
                    return s;
                }
            }

            return null;
        }

        /// <summary>
        /// Get a System name from the ID
        /// </summary>
        /// <param name="id">ID of the system</param>
        public string GetEveSystemNameFromID(long id)
        {
            foreach (System s in Systems)
            {
                if (s.ID == id)
                {
                    return s.Name;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Calculate the range between the two systems
        /// </summary>
        public double GetRangeBetweenSystems(string from, string to)
        {
            System systemFrom = GetEveSystem(from);
            System systemTo = GetEveSystem(to);

            if (systemFrom == null || systemTo == null)
            {
                return 0.0;
            }

            double x = systemFrom.ActualX - systemTo.ActualX;
            double y = systemFrom.ActualY - systemTo.ActualY;
            double z = systemFrom.ActualZ - systemTo.ActualZ;

            double length = Math.Sqrt((x * x) + (y * y) + (z * z));

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
            if (SystemIDToName.Keys.Contains(id))
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

                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    LocalCharacters.Add(esiChar);
                }), DispatcherPriority.Normal, null);
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
            Navigation.InitNavigation(NameToSystem.Values.ToList(), JumpBridges.ToList());
        }

        /// <summary>
        /// Load the EVE Manager Data from Disk
        /// </summary>
        public void LoadFromDisk()
        {
            SystemIDToName = new SerializableDictionary<long, string>();




            Regions = Serialization.DeserializeFromDisk<List<MapRegion>>(AppDomain.CurrentDomain.BaseDirectory + @"\data\MapLayout.dat");





            Systems = Serialization.DeserializeFromDisk<List<System>>(AppDomain.CurrentDomain.BaseDirectory + @"\data\Systems.dat");
            ShipTypes = Serialization.DeserializeFromDisk<SerializableDictionary<string, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\data\ShipTypes.dat");

            foreach (System s in Systems)
            {
                SystemIDToName[s.ID] = s.Name;
            }

            if (File.Exists(SaveDataVersionFolder + @"\CharacterNames.dat"))
            {
                CharacterIDToName = Serialization.DeserializeFromDisk<SerializableDictionary<long, string>>(SaveDataVersionFolder + @"\CharacterNames.dat");
            }
            if (CharacterIDToName == null)
            {
                CharacterIDToName = new SerializableDictionary<long, string>();
            }

            if (File.Exists(SaveDataVersionFolder + @"\AllianceNames.dat"))
            {
                AllianceIDToName = Serialization.DeserializeFromDisk<SerializableDictionary<long, string>>(SaveDataVersionFolder + @"\AllianceNames.dat");
            }

            if (AllianceIDToName == null)
            {
                AllianceIDToName = new SerializableDictionary<long, string>();
            }

            if (File.Exists(SaveDataVersionFolder + @"\AllianceTickers.dat"))
            {
                AllianceIDToTicker = Serialization.DeserializeFromDisk<SerializableDictionary<long, string>>(SaveDataVersionFolder + @"\AllianceTickers.dat");
            }

            if (AllianceIDToTicker == null)
            {
                AllianceIDToTicker = new SerializableDictionary<long, string>();
            }

            // patch up any links
            foreach (System s in Systems)
            {
                NameToSystem[s.Name] = s;
            }

            // now add the beacons
            string cynoBeaconsFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\CynoBeacons.txt";
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
            JumpBridges = new ObservableCollection<JumpBridge>();

            string dataFilename = SaveDataRootFolder + @"\JumpBridges_" + JumpBridge.SaveVersion + ".dat";
            if (!File.Exists(dataFilename))
            {
                return;
            }

            try
            {
                ObservableCollection<JumpBridge> loadList;
                XmlSerializer xms = new XmlSerializer(typeof(ObservableCollection<JumpBridge>));

                FileStream fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
                XmlReader xmlr = XmlReader.Create(fs);

                loadList = (ObservableCollection<JumpBridge>)xms.Deserialize(xmlr);

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
        public async Task ResolveAllianceIDs(List<long> IDs)
        {
            if (IDs.Count == 0)
            {
                return;
            }

            // strip out any ID's we already know..
            List<long> UnknownIDs = new List<long>();
            foreach (long l in IDs)
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

        /// <summary>
        /// Update the Character ID data for specified list
        /// </summary>
        public async Task ResolveCharacterIDs(List<long> IDs)
        {
            if (IDs.Count == 0)
            {
                return;
            }

            // strip out any ID's we already know..
            List<long> UnknownIDs = new List<long>();
            foreach (long l in IDs)
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

        /// <summary>
        /// Save the Data to disk
        /// </summary>
        public void SaveData()
        {
            // save off only the ESI authenticated Characters so create a new copy to serialise from..
            ObservableCollection<LocalCharacter> saveList = new ObservableCollection<LocalCharacter>();

            foreach (LocalCharacter c in LocalCharacters)
            {
                if (!string.IsNullOrEmpty(c.ESIRefreshToken))
                {
                    saveList.Add(c);
                }
            }

            XmlSerializer xms = new XmlSerializer(typeof(ObservableCollection<LocalCharacter>));
            string dataFilename = SaveDataRootFolder + @"\Characters_" + LocalCharacter.SaveVersion + ".dat";

            using (TextWriter tw = new StreamWriter(dataFilename))
            {
                xms.Serialize(tw, saveList);
            }

            // now serialise the caches to disk
            Serialization.SerializeToDisk<SerializableDictionary<long, string>>(CharacterIDToName, SaveDataVersionFolder + @"\CharacterNames.dat");
            Serialization.SerializeToDisk<SerializableDictionary<long, string>>(AllianceIDToName, SaveDataVersionFolder + @"\AllianceNames.dat");
            Serialization.SerializeToDisk<SerializableDictionary<long, string>>(AllianceIDToTicker, SaveDataVersionFolder + @"\AllianceTickers.dat");

            string jbFileName = SaveDataRootFolder + @"\JumpBridges_" + JumpBridge.SaveVersion + ".dat";
            Serialization.SerializeToDisk<ObservableCollection<JumpBridge>>(JumpBridges, jbFileName);

            List<string> beaconsToSave = new List<string>();
            foreach (System s in Systems)
            {
                if (s.HasJumpBeacon)
                {
                    beaconsToSave.Add(s.Name);
                }
            }

            // save the intel channels / intel filters
            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SMT\IntelChannels.txt", IntelFilters);
            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SMT\IntelClearFilters.txt", IntelClearFilters);
            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SMT\IntelIgnoreFilters.txt", IntelIgnoreFilters);
            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SMT\CynoBeacons.txt", beaconsToSave);
        }

        /// <summary>
        /// Setup the intel watcher;  Loads the intel channel filter list and creates the file system watchers
        /// </summary>
        public void SetupIntelWatcher()
        {
            IntelFilters = new List<string>();
            IntelDataList = new BindingList<IntelData>();
            string intelFileFilter = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SMT\IntelChannels.txt";

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
            string intelClearFileFilter = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SMT\IntelClearFilters.txt";

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
            string intelIgnoreFileFilter = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SMT\IntelIgnoreFilters.txt";

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
                EVELogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs";
            }

            string chatlogFolder = EVELogFolder + @"\Chatlogs\\";

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

            GameLogList = new BindingList<GameLogData>();

            if (string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
            {
                EVELogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\";
            }

            string gameLogFolder = EVELogFolder + @"\Gamelogs\\";

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
            string chatLogFolder = EVELogFolder + @"\Chatlogs\";
            string gameLogFolder = EVELogFolder + @"\Gamelogs\";

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
                            if (file.Name.IndexOf(intelFilterStr, StringComparison.OrdinalIgnoreCase) >= 0)
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
                        if (folder.Contains("GameLogs"))
                        {
                            readFile = true;
                        }

                        // only read files from the last day
                        if (file.CreationTime > DateTime.Now.AddDays(-1) && readFile)
                        {
                            FileStream ifs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            ifs.Seek(0, SeekOrigin.End);
                            Thread.Sleep(100);
                            ifs.Close();
                            Thread.Sleep(100);
                        }
                    }

                    Thread.Sleep(1000);
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

            List<long> IDToResolve = new List<long>();

            foreach (KeyValuePair<string, MapSystem> kvp in r.MapSystems)
            {
                if (kvp.Value.ActualSystem.SOVAllianceTCU != 0 && !AllianceIDToName.Keys.Contains(kvp.Value.ActualSystem.SOVAllianceTCU) && !IDToResolve.Contains(kvp.Value.ActualSystem.SOVAllianceTCU))
                {
                    IDToResolve.Add(kvp.Value.ActualSystem.SOVAllianceTCU);
                }

                if (kvp.Value.ActualSystem.SOVAllianceIHUB != 0 && !AllianceIDToName.Keys.Contains(kvp.Value.ActualSystem.SOVAllianceIHUB) && !IDToResolve.Contains(kvp.Value.ActualSystem.SOVAllianceIHUB))
                {
                    IDToResolve.Add(kvp.Value.ActualSystem.SOVAllianceIHUB);
                }
            }

            ResolveAllianceIDs(IDToResolve);
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


                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    TheraConnections.Clear();
                }), DispatcherPriority.Normal, null);


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

                        if (type != null && type == "wormhole" && solarSystemId != 0 && wormHoleEOL != null && SystemIDToName.Keys.Contains(solarSystemId))
                        {
                            System theraConnectionSystem = GetEveSystemFromID(solarSystemId);

                            TheraConnection tc = new TheraConnection(theraConnectionSystem.Name, theraConnectionSystem.Region, inSignatureId, outSignatureId, wormHoleEOL);

                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                TheraConnections.Add(tc);
                            }), DispatcherPriority.Normal, null);
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }

        public void UpdateMetaliminalStorms()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
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
            }), DispatcherPriority.Normal, null);

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
                SecretKey = EveAppConfig.SecretKey,
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
            InitPOI();

            ActiveSovCampaigns = new ObservableCollection<SOVCampaign>();

            InitZKillFeed();
            UpdateCoalitionInfo();

            StartBackgroundThread();
        }

        private void InitPOI()
        {
            PointsOfInterest = new ObservableCollection<POI>();

            try
            {
                string POIcsv = AppDomain.CurrentDomain.BaseDirectory + @"\data\POI.csv";
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
            TheraConnections = new ObservableCollection<TheraConnection>();
            UpdateTheraConnections();
        }

        private void InitMetaliminalStorms()
        {
            MetaliminalStorms = new ObservableCollection<Storm>();
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
            string channelName = e.Name.Substring(0, e.Name.IndexOf("_"));

            bool processFile = false;
            bool localChat = false;

            // check if the changed file path contains the name of a channel we're looking for
            foreach (string intelFilterStr in IntelFilters)
            {
                if (changedFile.IndexOf(intelFilterStr, StringComparison.OrdinalIgnoreCase) >= 0)
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
                    if (intelFileReadPos.Keys.Contains<string>(changedFile))
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
                                        Application.Current.Dispatcher.Invoke((Action)(() =>
                                        {
                                            LocalCharacters.Add(new EVEData.LocalCharacter(characterName, changedFile, system));
                                        }), DispatcherPriority.Normal, null);
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
                        if (line.Contains("[") && line.Contains("]"))
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

                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                {
                                    foreach (EVEData.LocalCharacter c in LocalCharacters)
                                    {
                                        if (c.LocalChatFile == changedFile)
                                        {
                                            c.Location = system;
                                        }
                                    }
                                }), DispatcherPriority.Normal, null);
                            }
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
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

                                if (addToIntel)
                                {
                                    EVEData.IntelData id = new EVEData.IntelData(line, channelName);

                                    foreach (string s in id.IntelString.Split(' '))
                                    {
                                        if (s == "" || s.Length < 3)
                                        {
                                            continue;
                                        }

                                        // check if we should ignore this message (maybe we could put this before every other checks ?)
                                        foreach (String ignoreMarker in IntelIgnoreFilters)
                                        {
                                            if (ignoreMarker.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0)
                                            {
                                                // do not even add to the intel list
                                                return;
                                            }
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

                                    IntelDataList.Insert(0, id);

                                    if (IntelAddedEvent != null && !id.ClearNotification)
                                    {
                                        IntelAddedEvent(id.Systems);
                                    }
                                }
                            }), DispatcherPriority.Normal, null);
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
                Encoding fe = Misc.GetEncoding(changedFile);
                FileStream ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                StreamReader file = new StreamReader(ifs, fe);

                int fileReadFrom = 0;

                // have we seen this file before
                if (gameFileReadPos.Keys.Contains<string>(changedFile))
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
                            fileReadFrom++;

                            gamelogFileCharacterMap[changedFile] = l.Split(':')[1].Trim();

                            // session started
                            l = file.ReadLine();
                            fileReadFrom++;

                            // header end
                            l = file.ReadLine();
                            fileReadFrom++;

                            break;
                        }
                    }

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

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        GameLogData gd = new GameLogData()
                        {
                            Character = characterName,
                            Text = line,
                            Severity = type,
                            Time = DateTime.Now,
                        };

                        GameLogList.Insert(0, gd);
                    }), DispatcherPriority.Normal, null);

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

                            if (line.Contains("cloak deactivates due to a pulse from a Mobile Observatory"))
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
            string dataFilename = SaveDataRootFolder + @"\Characters_" + LocalCharacter.SaveVersion + ".dat";
            if (!File.Exists(dataFilename))
            {
                return;
            }

            try
            {
                ObservableCollection<LocalCharacter> loadList;
                XmlSerializer xms = new XmlSerializer(typeof(ObservableCollection<LocalCharacter>));

                FileStream fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
                XmlReader xmlr = XmlReader.Create(fs);

                loadList = (ObservableCollection<LocalCharacter>)xms.Deserialize(xmlr);

                foreach (LocalCharacter c in loadList)
                {
                    c.ESIAccessToken = string.Empty;
                    c.ESIAccessTokenExpiry = DateTime.MinValue;
                    c.LocalChatFile = string.Empty;
                    c.Location = string.Empty;
                    c.Region = string.Empty;

                    LocalCharacters.Add(c);
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

        private async void UpdateCoalitionInfo()
        {
            Coalitions = new List<Coalition>();

            string url = @"http://rischwa.net/api/coalitions/current";
            string strContent = string.Empty;

            try
            {
                HttpClient hc = new HttpClient();
                var response = await hc.GetAsync(url);
                response.EnsureSuccessStatusCode();
                strContent = await response.Content.ReadAsStringAsync();

                var coalitions = CoalitionData.CoalitionInfo.FromJson(strContent);

                if (coalitions != null)
                {
                    foreach (CoalitionData.Coalition cd in coalitions.Coalitions)
                    {
                        Coalition c = new Coalition();
                        c.Name = cd.Name;
                        c.ID = cd.Id;
                        c.MemberAlliances = new List<long>();
                        c.CoalitionColor = (Color)ColorConverter.ConvertFromString(cd.Color);
                        //c.CoalitionBrush = new SolidColorBrush(c.CoalitionColor);

                        foreach (CoalitionData.Alliance a in cd.Alliances)
                        {
                            c.MemberAlliances.Add(a.Id);
                        }

                        Coalitions.Add(c);
                    }
                }
            }
            catch
            {
            }
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
                foreach (SOVCampaign sc in ActiveSovCampaigns)
                {
                    sc.Valid = false;
                }

                List<long> allianceIDsToResolve = new List<long>();

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

                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                ActiveSovCampaigns.Add(ss);
                            }), DispatcherPriority.Normal, null);
                        }

                        ss.AttackersScore = c.AttackersScore;
                        ss.DefendersScore = c.DefenderScore;
                        ss.Valid = true;

                        if (AllianceIDToName.Keys.Contains(ss.DefendingAllianceID))
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
                    if (string.IsNullOrEmpty(sc.DefendingAllianceName) && AllianceIDToName.Keys.Contains(sc.DefendingAllianceID))
                    {
                        sc.DefendingAllianceName = AllianceIDToName[sc.DefendingAllianceID];
                    }

                    if (sc.Valid == false)
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            ActiveSovCampaigns.Remove(sc);
                        }), DispatcherPriority.Normal, null);
                    }
                }

                // super hack : I want to update the both the times and filter colour that
                // this gets used for but the binding neither seem to propigate the change
                // but this forces a listchanged which ultimately triggers a refresh
                // ugly and to be fixed after some investigation
                {
                    SOVCampaign hackSC = new SOVCampaign();
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        ActiveSovCampaigns.Add(hackSC);
                        ActiveSovCampaigns.Remove(hackSC);
                    }), DispatcherPriority.Normal, null);
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

                        if (SystemIDToName.Keys.Contains(systemID))
                        {
                            System es = GetEveSystem(SystemIDToName[systemID]);
                            if (es != null)
                            {
                                if (obj["alliance_id"] != null)
                                {
                                    es.SOVAllianceTCU = long.Parse(obj["alliance_id"].ToString());
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
    }
}