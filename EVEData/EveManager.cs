//-----------------------------------------------------------------------
// EVE Manager
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMT.EVEData
{
    /// <summary>
    /// The main EVE Manager
    /// </summary>
    public class EveManager
    {
        /// <summary>
        /// Debug output log
        /// </summary>
        private static NLog.Logger outputLog = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// singleton instance of this class
        /// </summary>
        private static EveManager instance;

        /// <summary>
        /// File system watcher
        /// </summary>
        private FileSystemWatcher intelFileWatcher;

        /// <summary>
        /// Read position map for the intel files
        /// </summary>
        private Dictionary<string, int> intelFileReadPos;


        /// <summary>
        /// Initializes a new instance of the <see cref="EveManager" /> class
        /// </summary>
        public EveManager()
        {
            LocalCharacters = new ObservableCollection<LocalCharacter>();

            // ensure we have the cache folder setup
            DataCacheFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SMTCache";
            if (!Directory.Exists(DataCacheFolder))
            {
                Directory.CreateDirectory(DataCacheFolder);
            }

            string webCacheFoilder = DataCacheFolder + "\\WebCache";
            if (!Directory.Exists(webCacheFoilder))
            {
                Directory.CreateDirectory(webCacheFoilder);
            }

            string portraitCacheFoilder = DataCacheFolder + "\\Portraits";
            if (!Directory.Exists(portraitCacheFoilder))
            {
                Directory.CreateDirectory(portraitCacheFoilder);
            }

            string logosFoilder = DataCacheFolder + "\\Logos";
            if (!Directory.Exists(logosFoilder))
            {
                Directory.CreateDirectory(logosFoilder);
            }

            AllianceIDToName = new SerializableDictionary<long, string>();
            AllianceIDToTicker = new SerializableDictionary<long, string>();
            NameToSystem = new Dictionary<string, System>();


            ServerInfo = new EVEData.Server();
        }

        /// <summary>
        /// Intel Added Event Handler
        /// </summary>
        public delegate void IntelAddedEventHandler();

        /// <summary>
        /// Intel Added Event
        /// </summary>
        public event IntelAddedEventHandler IntelAddedEvent;

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

        /// <summary>
        /// Gets or sets the ShipTypes ID to Name dictionary
        /// </summary>
        public SerializableDictionary<string, string> ShipTypes { get; set; }


        /// <summary>
        /// Gets or sets the master list of Regions
        /// </summary>
        public List<MapRegion> Regions { get; set; }

        /// <summary>
        /// Gets or sets the master List of Systems
        /// </summary>
        public List<System> Systems { get; set; }

        /// <summary>
        /// Gets or sets the System ID to Name dictionary
        /// </summary>
        public SerializableDictionary<long, string> SystemIDToName { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Name dictionary
        /// </summary>
        public SerializableDictionary<long, string> AllianceIDToName { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Alliance Ticker dictionary
        /// </summary>
        public SerializableDictionary<long, string> AllianceIDToTicker { get; set; }

        /// <summary>
        /// Gets or sets the Intel List
        /// </summary>
        public BindingList<EVEData.IntelData> IntelDataList { get; set; }



        /// <summary>
        /// Gets or sets the list of Characters we are tracking
        /// </summary>
        [XmlIgnoreAttribute]
        public ObservableCollection<LocalCharacter> LocalCharacters { get; set; }

        /// <summary>
        /// Gets or sets the character cache
        /// </summary>
        [XmlIgnoreAttribute]
        public SerializableDictionary<string, Character> CharacterCache { get; set; }

        /// <summary>
        /// Gets or sets the folder to cache dotland svg's etc to
        /// </summary>
        public string DataCacheFolder { get; set; }

        /// <summary>
        /// Gets or sets the current list of thera connections
        /// </summary>
        public ObservableCollection<TheraConnection> TheraConnections { get; set; }

        /// <summary>
        /// Gets or sets the current list of ZKillData
        /// </summary>
        public ZKillRedisQ ZKillFeed { get; set; }

        /// <summary>
        /// Gets or sets the current list of Jump Bridges
        /// </summary>
        public List<JumpBridge> JumpBridges { get; set; }

        
        public List<Coalition> Coalitions { get; set; }

        /// <summary>
        /// Gets or sets the Name to System dictionary
        /// </summary>
        private Dictionary<string, System> NameToSystem { get; }

        /// <summary>
        /// Gets or sets the current list of intel filters used to monitor the local log files
        /// </summary>
        private List<string> IntelFilters { get; set; }

        /// <summary>
        /// Gets or sets the current list of clear markers for the intel (eg "Clear" "Clr" etc)
        /// </summary>
        private List<string> IntelClearFilters { get; set; }


        private bool WatcherThreadShouldTerminate = false;
        private bool BackgroundThreadShouldTerminate = false;


        public EveTrace.EveTraceFleetInfo FleetIntel;

        public EVEData.Server ServerInfo { get; set; }

        public List<string> ESIScopes { get; set; }

        public ESI.NET.EsiClient ESIClient { get; set; }

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

        /// <summary>
        /// Save the Data to disk
        /// </summary>
        public void SaveData()
        {
            // save off only the ESI authenticated Characters so create a new copy to serialise from..
            ObservableCollection<LocalCharacter> saveList = new ObservableCollection<LocalCharacter>();

            foreach (LocalCharacter c in LocalCharacters)
            {
                if (c.ESIRefreshToken != string.Empty)
                {
                    saveList.Add(c);
                }
            }

            XmlSerializer xms = new XmlSerializer(typeof(ObservableCollection<LocalCharacter>));
            string dataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Characters.dat";

            using (TextWriter tw = new StreamWriter(dataFilename))
            {
                xms.Serialize(tw, saveList);
            }

            // now serialise the caches to disk
            Utils.SerializToDisk<SerializableDictionary<long, string>>(AllianceIDToName, AppDomain.CurrentDomain.BaseDirectory + @"\AllianceNames.dat");
            Utils.SerializToDisk<SerializableDictionary<long, string>>(AllianceIDToTicker, AppDomain.CurrentDomain.BaseDirectory + @"\AllianceTickers.dat");
        }

        public void ShutDown()
        {
            ShuddownIntelWatcher();
            BackgroundThreadShouldTerminate = true;

            ZKillFeed.ShutDown();
        }


        public void InitNavigation()
        {
            Navigation.InitNavigation(NameToSystem.Values.ToList(), JumpBridges);
        }


        /// <summary>
        /// Load the jump bridge data from disk
        /// </summary>
        public void LoadJumpBridgeData()
        {
            JumpBridges = new List<JumpBridge>();
            string jumpBridgeData = AppDomain.CurrentDomain.BaseDirectory + @"\JumpBridges.txt";

            bool friendly = true;

            if (File.Exists(jumpBridgeData))
            {
                StreamReader file = new StreamReader(jumpBridgeData);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("---HOSTILE---"))
                    {
                        friendly = false;
                    }

                    // Skip comments
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }

                    string[] jbbits = line.Split(' ');

                    // malformed line
                    if (jbbits.Length < 3)
                    {
                        continue;
                    }

                    // in the form :
                    // FromSystem --> ToSystem
                    string from = jbbits[0];
                    string to = jbbits[2];

                    if (DoesSystemExist(from) && DoesSystemExist(to))
                    {
                        JumpBridges.Add(new JumpBridge(from, to, friendly));
                    }
                }
            }
        }

        /// <summary>
        /// Setup the intel watcher;  Loads the intel channel filter list and creates the file system watchers
        /// </summary>
        public void SetupIntelWatcher()
        {
            IntelFilters = new List<string>();
            IntelDataList = new BindingList<IntelData>();
            string intelFileFilter = AppDomain.CurrentDomain.BaseDirectory + @"\IntelChannels.txt";

            outputLog.Info("Loading Intel File Filer from  {0}", intelFileFilter);
            if (File.Exists(intelFileFilter))
            {
                StreamReader file = new StreamReader(intelFileFilter);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line.Trim();
                    if (line != string.Empty)
                    {
                        outputLog.Info("adding intel filer : {0}", line);
                        IntelFilters.Add(line);
                    }
                }
            }


            IntelClearFilters = new List<string>();
            string intelClearFileFilter = AppDomain.CurrentDomain.BaseDirectory + @"\IntelClearFilters.txt";

            outputLog.Info("intelClearFileFilter Intel Filter File from  {0}", intelClearFileFilter);
            if (File.Exists(intelClearFileFilter))
            {
                StreamReader file = new StreamReader(intelClearFileFilter);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line.Trim();
                    if (line != string.Empty)
                    {
                        outputLog.Info("adding clear intel filer : {0}", line);
                        IntelClearFilters.Add(line);
                    }
                }
            }



            intelFileReadPos = new Dictionary<string, int>();

            string eveLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Chatlogs\";
            if (Directory.Exists(eveLogFolder))
            {
                outputLog.Info("adding file watcher to : {0}", eveLogFolder);

                intelFileWatcher = new FileSystemWatcher(eveLogFolder)
                {
                    Filter = "*.txt",
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                intelFileWatcher.Changed += IntelFileWatcher_Changed;
            }

            // -----------------------------------------------------------------
            // SUPER HACK WARNING....
            //
            // Start up a thread which just reads the text files in the eve log folder
            // by opening and closing them it updates the sytem meta files which
            // causes the file watcher to operate correctly otherwise this data
            // doesnt get updated until something other than the eve client reads these files



            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = false;

                // loop forever
                while (WatcherThreadShouldTerminate == false)
                {
                    DirectoryInfo di = new DirectoryInfo(eveLogFolder);
                    FileInfo[] files = di.GetFiles("*.txt");
                    foreach (FileInfo file in files)
                    {
                        bool readFile = false;
                        foreach (string intelFilterStr in IntelFilters)
                        {
                            if (file.Name.Contains(intelFilterStr))
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

                        // only read files from the last day
                        if (file.CreationTime > DateTime.Now.AddDays(-1) && readFile)
                        {
                            FileStream ifs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            ifs.Seek(0, SeekOrigin.End);
                            Thread.Sleep(100);
                            ifs.Close();
                            Thread.Sleep(200);
                        }
                    }

                    Thread.Sleep(2000);
                }
            }).Start();

            // END SUPERHACK
            // -----------------------------------------------------------------
        }

        public void ShuddownIntelWatcher()
        {
            string eveLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Chatlogs\";
            if (intelFileWatcher != null)
            {
                intelFileWatcher.Changed -= IntelFileWatcher_Changed;
            }
            WatcherThreadShouldTerminate = true;
        }

        /// <summary>
        /// Update The Universe Data from the various ESI end points
        /// </summary>
        public void UpdateESIUniverseData()
        {
            StartUpdateKillsFromESI();
            StartUpdateJumpsFromESI();
            StartUpdateSOVFromESI();
            StartUpdateIncursionsFromESI();
            // temp disabled
            //StartEveTraceFleetUpdate();
//            StartUpdateStructureHunterUpdate();
            StartUpdateSovStructureUpdate();
        }

        /// <summary>
        /// Scrape the maps from dotlan and initialise the region data from dotlan
        /// </summary>
        public void CreateFromScratch()
        {
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
            Regions.Add(new MapRegion("The Forge", "10000002", "Caldari", 600, 300));
            Regions.Add(new MapRegion("Fountain", "10000058", string.Empty, 60, 250));
            Regions.Add(new MapRegion("Geminate", "10000029", "The Society", 665, 245));
            Regions.Add(new MapRegion("Genesis", "10000067", string.Empty, 240, 430));
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
            Regions.Add(new MapRegion("Sinq Laison", "10000032", "Gallente", 420, 375));
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

            // create folder cache
            WebClient webClient = new WebClient();

            // update the region cache
            foreach (MapRegion rd in Regions)
            {
                string localSVG = DataCacheFolder + @"\" + rd.DotLanRef + ".svg";
                string remoteSVG = @"http://evemaps.dotlan.net/svg/" + rd.DotLanRef + ".svg";

                bool needsDownload = true;

                if (File.Exists(localSVG))
                {
                    needsDownload = false;
                }

                if (needsDownload)
                {
                    webClient.DownloadFile(remoteSVG, localSVG);

                    // throttle so we dont hammer the server
                    Thread.Sleep(100);
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
                    x = (float)Math.Round(x/ RoundVal, 0) * RoundVal;
                    y = (float)Math.Round(y/ RoundVal, 0) * RoundVal;

                    string systemnodepath = @"//*[@id='def" + systemID + "']";
                    XmlNodeList snl = xmldoc.SelectNodes(systemnodepath);
                    XmlNode sysdefNode = snl[0];

                    XmlNode aNode = sysdefNode.ChildNodes[0];

                    string name;
                    bool hasStation = false;
                    bool hasIceBelt = false;
                    XmlNodeList iceNodes = aNode.SelectNodes(@".//*[@class='i']");
                    if(iceNodes[0] != null)
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
            string eveStaticDataSolarSystemFile = AppDomain.CurrentDomain.BaseDirectory + @"\mapSolarSystems.csv";
            if (File.Exists(eveStaticDataSolarSystemFile))
            {
                StreamReader file = new StreamReader(eveStaticDataSolarSystemFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string constID = bits[1];
                    string systemID = bits[2];
                    string systemName = bits[3]; // SystemIDToName[SystemID];

                    double x = Convert.ToDouble(bits[4]);
                    double y = Convert.ToDouble(bits[5]);
                    double z = Convert.ToDouble(bits[6]);
                    double security = Convert.ToDouble(bits[21]);

                    System s = GetEveSystem(systemName);
                    if (s != null)
                    {
                        s.ActualX = x;
                        s.ActualY = y;
                        s.ActualZ = z;
                        s.TrueSec = security;
                        s.ConstellationID = constID;
                    }
                }
            }
            else
            {
                // Error
            }

            string eveStaticDataJumpsFile = AppDomain.CurrentDomain.BaseDirectory + @"\mapSolarSystemJumps.csv";
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
            string eveStaticDataStationsFile = AppDomain.CurrentDomain.BaseDirectory + @"\staStations.csv";
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
                    if(SS != null)
                    {
                        SS.HasNPCStation = true;
                    }
                }
            }
            else
            {
                // error
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

            foreach (System s in Systems)
            {
                NameToSystem[s.Name] = s;
            }

            foreach (MapRegion rr in Regions)
            {
                // link to the real systems
                foreach (MapSystem ms in rr.MapSystems.Values.ToList())
                {
                    ms.ActualSystem = GetEveSystem(ms.Name);
                }
            }

            // collect the system points to generate them from 
            List<Vector2f> regionpoints = new List<Vector2f>();

            // now Generate the region links
            foreach (MapRegion mr in Regions)
            {
                mr.RegionLinks = new List<string>();

                regionpoints.Add(new Vector2f(mr.RegionX, mr.RegionY));

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
            string eveStaticDataItemTypesFile = AppDomain.CurrentDomain.BaseDirectory + @"\invTypes.csv";
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
                    if(line == string.Empty)
                    {
                        continue;
                    }
                    string[] bits = line.Split(',');

                    if(bits.Length < 3)
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

            // now serialise the classes to disk
            Utils.SerializToDisk<SerializableDictionary<string, string>>(ShipTypes, AppDomain.CurrentDomain.BaseDirectory + @"\ShipTypes.dat");
            Utils.SerializToDisk<List<MapRegion>>(Regions, AppDomain.CurrentDomain.BaseDirectory + @"\MapLayout.dat");
            Utils.SerializToDisk<List<System>>(Systems, AppDomain.CurrentDomain.BaseDirectory + @"\Systems.dat");

            Init();
        }

        /// <summary>
        /// Load the EVE Manager Data from Disk
        /// </summary>
        public void LoadFromDisk()
        {
            SystemIDToName = new SerializableDictionary<long, string>();

            Regions = Utils.DeserializeFromDisk<List<MapRegion>>(AppDomain.CurrentDomain.BaseDirectory + @"\MapLayout.dat");
            Systems = Utils.DeserializeFromDisk<List<System>>(AppDomain.CurrentDomain.BaseDirectory + @"\Systems.dat");
            ShipTypes = Utils.DeserializeFromDisk<SerializableDictionary<string, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\ShipTypes.dat");

            foreach (System s in Systems)
            {
                SystemIDToName[s.ID] = s.Name;
            }

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceNames.dat"))
            {
                AllianceIDToName = Utils.DeserializeFromDisk<SerializableDictionary<long, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceNames.dat");
            }

            if(AllianceIDToName == null)
            {
                AllianceIDToName = new SerializableDictionary<long, string>();
            }

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceTickers.dat"))
            {
                AllianceIDToTicker = Utils.DeserializeFromDisk<SerializableDictionary<long, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceTickers.dat");
            }

            if(AllianceIDToTicker == null)
            {
                AllianceIDToTicker = new SerializableDictionary<long, string>();
            }

            Init();
        }

        /// <summary>
        /// Does the System Exist ?
        /// </summary>
        /// <param name="name">Name (not ID) of the system</param>
        public bool DoesSystemExist(string name) => GetEveSystem(name) != null;

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
        /// Get the ESI Logon URL String
        /// </summary>
        public string GetESILogonURL()
        {
            return ESIClient.SSO.CreateAuthenticationUrl(ESIScopes);
        }

        /// <summary>
        /// Hand the custom smtauth- url we get back from the logon screen
        /// </summary>
        public async void HandleEveAuthSMTUri(Uri uri)
        {
            // parse the uri
            var query = HttpUtility.ParseQueryString(uri.Query);
            if (query["code"] == null)
            {
                // we're missing a query code
                return;
            }

            string code = query["code"];

            SsoToken sst = await ESIClient.SSO.GetToken(GrantType.AuthorizationCode, code);
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
                }), DispatcherPriority.ContextIdle, null);
            }

            esiChar.ESIRefreshToken = acd.RefreshToken;
            esiChar.ESILinked = true;
            esiChar.ESIAccessToken = acd.Token;
            esiChar.ESIAccessTokenExpiry = acd.ExpiresOn;
            esiChar.ID = acd.CharacterID;
            esiChar.ESIAuthData = acd;


            // now to find if a matching character

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

            foreach (MapSystem s in r.MapSystems.Values.ToList())
            {
                
                if (s.ActualSystem.SOVAlliance != 0 && !AllianceIDToName.Keys.Contains(s.ActualSystem.SOVAlliance) && !IDToResolve.Contains(s.ActualSystem.SOVAlliance))
                {
                    IDToResolve.Add(s.ActualSystem.SOVAlliance);
                }
            }


            ResolveAllianceIDs(IDToResolve);
        }

        /// <summary>
        /// Update the Alliance and Ticker data for specified list
        /// </summary>
        public async Task ResolveAllianceIDs(List<long> IDs)
        {
            if(IDs.Count == 0 )
            {
                return;
            }

            ESI.NET.EsiResponse<List<ESI.NET.Models.Universe.ResolvedInfo>> esra = await ESIClient.Universe.Names(IDs);
            if(ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.ResolvedInfo>>(esra))
            {
                foreach(ESI.NET.Models.Universe.ResolvedInfo ri in esra.Data)
                {
                    if(ri.Category == ResolvedInfoCategory.Alliance)
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
        /// Update the current Thera Connections from EVE-Scout
        /// </summary>
        public void UpdateTheraConnections()
        {
            string theraApiURL = "https://www.eve-scout.com/api/wormholes";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(theraApiURL);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                TheraConnections.Clear();
            }), DispatcherPriority.ContextIdle, null);

            request.BeginGetResponse(new AsyncCallback(UpdateTheraConnectionsCallback), request);


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
                UserAgent = "SMT-map-app"
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



            // patch up any links
            foreach (System s in Systems)
            {
                NameToSystem[s.Name] = s;
            }

            foreach (MapRegion rr in Regions)
            {
                // link to the real systems
                foreach (MapSystem ms in rr.MapSystems.Values.ToList())
                {
                    ms.ActualSystem = GetEveSystem(ms.Name);
                }
            }

            LoadCharacters();

            InitTheraConnections();
            InitZKillFeed();
            StartUpdateCoalitionInfo();

            StartBackgroundThread();
        }

        /// <summary>
        /// Load the character data from disk
        /// </summary>
        private void LoadCharacters()
        {
            string dataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Characters.dat";
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
        /// Intel File watcher changed handler
        /// </summary>
        private void IntelFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string changedFile = e.FullPath;

            outputLog.Info("File Watcher triggered : {0}", changedFile);

            bool processFile = false;
            bool localChat = false;

            foreach (string intelFilterStr in IntelFilters)
            {
                if (changedFile.Contains(intelFilterStr))
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
                    Encoding fe = Utils.GetEncoding(changedFile);
                    FileStream ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    outputLog.Info("Log File Encoding : {0}", fe.ToString());

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
                            outputLog.Info("Processing file : {0}", changedFile);
                            string system = string.Empty;
                            string characterName = string.Empty;

                            // read the iniital block
                            while (!file.EndOfStream)
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
                                    foreach (EVEData.LocalCharacter c in LocalCharacters)
                                    {
                                        if (characterName == c.Name)
                                        {
                                            c.Location = system;
                                            c.LocalChatFile = changedFile;

                                            System s = GetEveSystem(system);
                                            if(s!=null)
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
                                        }), DispatcherPriority.ContextIdle, null);


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

                        if(line == "")
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
                                            outputLog.Info("Character {0} moved from {1} to {2}", c.Name, c.Location, system);

                                            c.Location = system;
                                        }
                                    }
                                }), DispatcherPriority.ContextIdle, null);
                            }
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                // check if it is in the intel list already (ie if you have multiple clients running)
                                bool addToIntel = true;
                                foreach (EVEData.IntelData idl in IntelDataList)
                                {
                                    if (idl.RawIntelString == line)
                                    {
                                        addToIntel = false;
                                        break;
                                    }
                                }

                                if (addToIntel)
                                {
                                    EVEData.IntelData id = new EVEData.IntelData(line);

                                    foreach (string s in id.IntelString.Split(' '))
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

                                    IntelDataList.Insert(0, id);

                                    if (IntelAddedEvent != null)
                                    {
                                        IntelAddedEvent();
                                    }
                                }
                                else
                                {
                                    outputLog.Info("Already have Line : {0} ", line);
                                }
                            }), DispatcherPriority.ContextIdle, null);
                        }

                        line = file.ReadLine();
                    }

                    ifs.Close();

                    intelFileReadPos[changedFile] = fileReadFrom;
                    outputLog.Info("File Read pos : {0} {1}", changedFile, fileReadFrom);
                }
                catch (Exception ex)
                {
                    outputLog.Info("Intel Process Failed : {0}", ex.ToString());
                }
            }
            else
            {
                outputLog.Info("Skipping File : {0}", changedFile);
            }
        }

        private void StartEveTraceFleetUpdate()
        {
            string url = @"https://api.evetrace.com/api/v1/fleet/";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(EveTraceFleetUpdateCallback), request);
        }

        private void EveTraceFleetUpdateCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();

                        EveTrace.EveTraceFleetInfo fleetInfo = EveTrace.EveTraceFleetInfo.FromJson(strContent);

                        if(fleetInfo != null)
                        {
                            FleetIntel = fleetInfo;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }


        private void StartUpdateCoalitionInfo()
        {

            Coalitions = new List<Coalition>();


            string url = @"http://rischwa.net/api/coalitions/current";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(UpdateCoalitionInfoCallback), request);
        }

        private void UpdateCoalitionInfoCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();

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
                }
            }
            catch (Exception)
            {
            }
        }


        private void StartUpdateStructureHunterUpdate()
        {
            string url = @"https://stop.hammerti.me.uk/api/structure/all";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(StructureHunterUpdateCallback), request);
        }

        private void StructureHunterUpdateCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();

                        var structures = StructureHunter.Structures.FromJson(strContent);

                        if (structures != null)
                        {
                            foreach(StructureHunter.Structures s in structures.Values.ToList())
                            {
                                EVEData.System es = GetEveSystemFromID(s.SystemId);
                                
                                if( es != null )
                                {
                                    es.SHStructures.Add(s);
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private async void StartUpdateSovStructureUpdate()
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
                            }

                            if (ss.TypeId == 32458)
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
            ESI.NET.EsiResponse<ESI.NET.Models.Status.Status> esr = await ESIClient.Status.Retrieve();

            if(ESIHelpers.ValidateESICall<ESI.NET.Models.Status.Status>(esr))
            {
                ServerInfo.Name = "Tranquility";
                ServerInfo.NumPlayers = esr.Data.Players;
                ServerInfo.Version = esr.Data.ServerVersion.ToString();
            }
            else
            {
                ServerInfo.Name = "Tranquility";
                ServerInfo.NumPlayers = 0;
                ServerInfo.Version = "??????";
            }
        }


        /// <summary>
        /// Start the ESI download for the kill info
        /// </summary>
        private async void StartUpdateKillsFromESI()
        {
            ESI.NET.EsiResponse<List<ESI.NET.Models.Universe.Kills>> esr = await ESIClient.Universe.Kills();
            if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Universe.Kills>>(esr))
            {
                foreach(ESI.NET.Models.Universe.Kills k in esr.Data)
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



        /// <summary>
        /// Start the ESI download for the Jump info
        /// </summary>
        private async void StartUpdateJumpsFromESI()
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


        /// <summary>
        /// Start the ESI download for the Jump info
        /// </summary>
        private async void StartUpdateIncursionsFromESI()
        {
            ESI.NET.EsiResponse<List<ESI.NET.Models.Incursions.Incursion>> esr = await ESIClient.Incursions.All();
            if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Incursions.Incursion>>(esr))
            {
                foreach(ESI.NET.Models.Incursions.Incursion i in esr.Data)
                {
                    foreach(long s in i.InfestedSystems )
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



        /// <summary>
        /// Start the Low Frequency Update Thread
        /// </summary>
        private void StartBackgroundThread()
        {



            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = false;

                TimeSpan CharacterUpdateRate = TimeSpan.FromSeconds(1);
                TimeSpan LowFreqUpdateRate = TimeSpan.FromMinutes(10);

                DateTime NextCharacterUpdate = DateTime.Now;
                DateTime NextLowFreqUpdate = DateTime.Now;


            // loop forever
                while (BackgroundThreadShouldTerminate == false)
                {
                    // character Update 
                    if((NextCharacterUpdate - DateTime.Now).Milliseconds < 100)
                    {
                        NextCharacterUpdate = DateTime.Now + CharacterUpdateRate;

                        for (int i = 0; i < LocalCharacters.Count; i++)
                        {
                            LocalCharacter c = LocalCharacters.ElementAt(i);
                            c.Update();
                        }
                    }


                    // low frequency update
                    if((NextLowFreqUpdate - DateTime.Now).Minutes < 0 )
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


        /// <summary>
        /// Start the ESI download for the kill info
        /// </summary>
        private void StartUpdateSOVFromESI()
        {
            string url = @"https://esi.evetech.net/v1/sovereignty/map/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIUpdateSovCallback), request);

            /*
            This need expanding; awaiting fix
            ESI.NET.EsiResponse<List<ESI.NET.Models.Sovereignty.SystemSovereignty>> esr = await ESIClient.Sovereignty.Systems();
            if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Sovereignty.SystemSovereignty>>(esr))
            {
                foreach (ESI.NET.Models.Sovereignty.SystemSovereignty ss in esr.Data)
                {
                    EVEData.System sys = GetEveSystemFromID(ss.SystemId);
                    if(sys != null)
                    {
                        sys.SOVAlliance = ss.
                    }
                }


            }
            */
        }

            /// <summary>
            /// ESI Result Response
            /// </summary>
            private void ESIUpdateSovCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();
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
                                            es.SOVAlliance = long.Parse(obj["alliance_id"].ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
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

        /// <summary>
        ///  Update Thera Connections Callback
        /// </summary>
        private void UpdateTheraConnectionsCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();

                        JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

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
                                    }), DispatcherPriority.ContextIdle, null);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public CharacterIDs.Character[] BulkUpdateCharacterCache(List<string> charList)
        {

            CharacterIDs.CharacterIdData cd = new CharacterIDs.CharacterIdData();

            string esiCharString = "[";
            foreach (string s in charList)
            {
                esiCharString += "\"";
                esiCharString += s;
                esiCharString += "\",";
            }
            esiCharString += "\"0\"]";

            string url = @"https://esi.evetech.net/v1/universe/ids/?";

            var httpData = HttpUtility.ParseQueryString(string.Empty);

            httpData["datasource"] = "tranquility";


            string httpDataStr = httpData.ToString();
            byte[] data = UTF8Encoding.UTF8.GetBytes(esiCharString);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + httpDataStr);
            request.Method = WebRequestMethods.Http.Post;
            request.Timeout = 20000;
            request.Proxy = null;
            request.ContentType = "application/json";
            request.ContentLength = data.Length;

            var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);

            HttpWebResponse esiResult = (HttpWebResponse)request.GetResponse();
 


            if (esiResult.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            Stream responseStream = esiResult.GetResponseStream();
            using (StreamReader sr = new StreamReader(responseStream))
            {
                // Need to return this response
                string strContent = sr.ReadToEnd();

                try
                {
                    cd = CharacterIDs.CharacterIdData.FromJson(strContent);
                    if (cd.Characters != null)
                    {
                    }
                }
                catch { }
            }

            return cd.Characters;
        }


        /// <summary>
        /// Initialise the ZKillBoard Feed
        /// </summary>
        private void InitZKillFeed()
        {
            ZKillFeed = new ZKillRedisQ();
            ZKillFeed.Initialise();
        }      
    }
}
