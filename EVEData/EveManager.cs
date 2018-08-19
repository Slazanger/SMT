//-----------------------------------------------------------------------
// EVE Manager
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        /// Pending Access token 
        /// </summary>
        private string pendingAccessToken;

        /// <summary>
        /// Pending Token Type
        /// </summary>
        private string pendingTokenType;

        /// <summary>
        /// Pending Token Expiry
        /// </summary>
        private string pendingExpiresIn;

        /// <summary>
        /// Pending Refresh Token
        /// </summary>
        private string pendingRefreshToken;

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

            AllianceIDToName = new SerializableDictionary<string, string>();
            AllianceIDToTicker = new SerializableDictionary<string, string>();
            NameToSystem = new Dictionary<string, System>();
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
        public SerializableDictionary<string, string> SystemIDToName { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Name dictionary
        /// </summary>
        public SerializableDictionary<string, string> AllianceIDToName { get; set; }

        /// <summary>
        /// Gets or sets the Alliance ID to Alliance Ticker dictionary
        /// </summary>
        public SerializableDictionary<string, string> AllianceIDToTicker { get; set; }

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

        /// <summary>
        /// Gets or sets the Name to System dictionary
        /// </summary>
        private Dictionary<string, System> NameToSystem { get; }

        /// <summary>
        /// Gets or sets the current list of intel filters used to monitor the local log files
        /// </summary>
        private List<string> IntelFilters { get; set; }


        public EveTrace.EveTraceFleetInfo FleetIntel;



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
        public string GetSystemNameFromSystemID(string id)
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
        public string GetAllianceName(string id)
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
        public string GetAllianceTicker(string id)
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
            Utils.SerializToDisk<SerializableDictionary<string, string>>(AllianceIDToName, AppDomain.CurrentDomain.BaseDirectory + @"\AllianceNames.dat");
            Utils.SerializToDisk<SerializableDictionary<string, string>>(AllianceIDToTicker, AppDomain.CurrentDomain.BaseDirectory + @"\AllianceTickers.dat");
        }

        public void ShutDown()
        {
            ZKillFeed.ShutDown();
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
                    if (jbbits.Length < 5)
                    {
                        continue;
                    }

                    // in the form :
                    // FromSystem FromPlanetMoon <-> ToSystem ToPlanetMoon
                    string from = jbbits[0];
                    string fromData = jbbits[1];

                    string to = jbbits[3];
                    string toData = jbbits[4];

                    if (DoesSystemExist(from) && DoesSystemExist(to))
                    {
                        JumpBridges.Add(new JumpBridge(from, fromData, to, toData, friendly));
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

            intelFileReadPos = new Dictionary<string, int>();

            string eveLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Chatlogs\";
            if (Directory.Exists(eveLogFolder))
            {
                outputLog.Info("adding file watcher to : {0}", eveLogFolder);

                intelFileWatcher = new FileSystemWatcher(eveLogFolder);
                intelFileWatcher.Filter = "*.txt";
                intelFileWatcher.EnableRaisingEvents = true;
                intelFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
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
                Thread.CurrentThread.IsBackground = true;

                // loop forever
                while (true)
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

        /// <summary>
        /// Update The Universe Data from the various ESI end points
        /// </summary>
        public void UpdateESIUniverseData()
        {
            StartUpdateKillsFromESI();
            StartUpdateJumpsFromESI();
            StartUpdateSOVFromESI();
            StartUpdateIncursionsFromESI();
            StartEveTraceFleetUpdate();
            StartUpdateStructureHunterUpdate();
        }

        /// <summary>
        /// Scrape the maps from dotlan and initialise the region data from dotlan
        /// </summary>
        public void CreateFromScratch()
        {
            Regions = new List<MapRegion>();

            // manually add the regions we care about
            Regions.Add(new MapRegion("Aridia", "Amarr", 140, 405));
            Regions.Add(new MapRegion("Black Rise", "Caldari", 450, 250));
            Regions.Add(new MapRegion("The Bleak Lands", "Amarr", 500, 460));
            Regions.Add(new MapRegion("Branch", string.Empty, 520, 50));
            Regions.Add(new MapRegion("Cache", string.Empty, 965, 400));
            Regions.Add(new MapRegion("Catch", string.Empty, 555, 640));
            Regions.Add(new MapRegion("The Citadel", "Caldari", 505, 310));
            Regions.Add(new MapRegion("Cloud Ring", string.Empty, 250, 120));
            Regions.Add(new MapRegion("Cobalt Edge", string.Empty, 950, 65));
            Regions.Add(new MapRegion("Curse", "Angel Cartel", 675, 560));
            Regions.Add(new MapRegion("Deklein", string.Empty, 410, 75));
            Regions.Add(new MapRegion("Delve", "Blood Raider", 115, 605));
            Regions.Add(new MapRegion("Derelik", "Ammatar", 650, 485));
            Regions.Add(new MapRegion("Detorid", string.Empty, 880, 700));
            Regions.Add(new MapRegion("Devoid", "Amarr", 495, 530));
            Regions.Add(new MapRegion("Domain", "Amarr", 405, 480));
            Regions.Add(new MapRegion("Esoteria", string.Empty, 440, 725));
            Regions.Add(new MapRegion("Essence", "Gallente", 370, 290));
            Regions.Add(new MapRegion("Etherium Reach", string.Empty, 785, 310));
            Regions.Add(new MapRegion("Everyshore", "Gallente", 330, 365));
            Regions.Add(new MapRegion("Fade", string.Empty, 360, 130));
            Regions.Add(new MapRegion("Feythabolis", string.Empty, 535, 755));
            Regions.Add(new MapRegion("The Forge", "Caldari", 600, 300));
            Regions.Add(new MapRegion("Fountain", string.Empty, 60, 250));
            Regions.Add(new MapRegion("Geminate", "The Society", 665, 245));
            Regions.Add(new MapRegion("Genesis", string.Empty, 240, 430));
            Regions.Add(new MapRegion("Great Wildlands", "Thukker Tribe", 815, 460));
            Regions.Add(new MapRegion("Heimatar", "Minmatar", 610, 430));
            Regions.Add(new MapRegion("Immensea", string.Empty, 675, 615));
            Regions.Add(new MapRegion("Impass", string.Empty, 600, 695));
            Regions.Add(new MapRegion("Insmother", string.Empty, 945, 580));
            Regions.Add(new MapRegion("Kador", "Amarr", 330, 440));
            Regions.Add(new MapRegion("The Kalevala Expanse", string.Empty, 745, 185));
            Regions.Add(new MapRegion("Khanid", "Khanid", 235, 570));
            Regions.Add(new MapRegion("Kor-Azor", "Amarr", 250, 505));
            Regions.Add(new MapRegion("Lonetrek", "Caldari", 550, 230));
            Regions.Add(new MapRegion("Malpais", string.Empty, 885, 260));
            Regions.Add(new MapRegion("Metropolis", "Minmatar", 665, 365));
            Regions.Add(new MapRegion("Molden Heath", "Minmatar", 730, 430));
            Regions.Add(new MapRegion("Oasa", string.Empty, 945, 160));
            Regions.Add(new MapRegion("Omist", string.Empty, 720, 740));
            Regions.Add(new MapRegion("Outer Passage", string.Empty, 965, 230));
            Regions.Add(new MapRegion("Outer Ring", "ORE", 120, 140));
            Regions.Add(new MapRegion("Paragon Soul", string.Empty, 320, 740));
            Regions.Add(new MapRegion("Period Basis", string.Empty, 220, 700));
            Regions.Add(new MapRegion("Perrigen Falls", string.Empty, 800, 130));
            Regions.Add(new MapRegion("Placid", "Gallente", 300, 220));
            Regions.Add(new MapRegion("Providence", string.Empty, 505, 585));
            Regions.Add(new MapRegion("Pure Blind", string.Empty, 435, 190));
            Regions.Add(new MapRegion("Querious", string.Empty, 340, 640));
            Regions.Add(new MapRegion("Scalding Pass", string.Empty, 800, 540));
            Regions.Add(new MapRegion("Sinq Laison", "Gallente", 420, 375));
            Regions.Add(new MapRegion("Solitude", "Gallente", 155, 335));
            Regions.Add(new MapRegion("The Spire", string.Empty, 860, 350));
            Regions.Add(new MapRegion("Stain", "Sansha", 450, 675));
            Regions.Add(new MapRegion("Syndicate", "Syndicate", 180, 250));
            Regions.Add(new MapRegion("Tash-Murkon", "Amarr", 365, 545));
            Regions.Add(new MapRegion("Tenal", string.Empty, 700, 70));
            Regions.Add(new MapRegion("Tenerifis", string.Empty, 715, 675));
            Regions.Add(new MapRegion("Tribute", string.Empty, 535, 145));
            Regions.Add(new MapRegion("Vale of the Silent", string.Empty, 615, 190));
            Regions.Add(new MapRegion("Venal", "Guristas", 570, 105));
            Regions.Add(new MapRegion("Verge Vendor", "Gallente", 245, 330));
            Regions.Add(new MapRegion("Wicked Creek", string.Empty, 790, 615));

            SystemIDToName = new SerializableDictionary<string, string>();

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
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.XmlResolver = null;
                FileStream fs = new FileStream(localSVG, FileMode.Open, FileAccess.Read);
                xmldoc.Load(fs);

                // get the svg/g/g sys use child nodes
                string systemsXpath = @"//*[@id='sysuse']";
                XmlNodeList xn = xmldoc.SelectNodes(systemsXpath);

                XmlNode sysUseNode = xn[0];
                foreach (XmlNode system in sysUseNode.ChildNodes)
                {
                    // extact the base from info from the g
                    string systemID = system.Attributes["id"].Value.Substring(3);
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

                    // SS Nodes for system nodes
                    XmlNodeList ssNodes = aNode.SelectNodes(@".//*[@class='ss']");
                    if (ssNodes[0] != null)
                    {
                        name = ssNodes[0].InnerText;

                        // create and add the system
                        System s = new System(name, systemID, rd.Name, hasStation);
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

                    string fromID = bits[2];
                    string toID = bits[3];

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

                    string stationSystem = bits[8];

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
            SystemIDToName = new SerializableDictionary<string, string>();

            Regions = Utils.DeserializeFromDisk<List<MapRegion>>(AppDomain.CurrentDomain.BaseDirectory + @"\MapLayout.dat");
            Systems = Utils.DeserializeFromDisk<List<System>>(AppDomain.CurrentDomain.BaseDirectory + @"\Systems.dat");
            ShipTypes = Utils.DeserializeFromDisk<SerializableDictionary<string, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\ShipTypes.dat");

            foreach (System s in Systems)
            {
                SystemIDToName[s.ID] = s.Name;
            }

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceNames.dat"))
            {
                AllianceIDToName = Utils.DeserializeFromDisk<SerializableDictionary<string, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceNames.dat");
            }
            else
            {
                AllianceIDToName = new SerializableDictionary<string, string>();
            }

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceTickers.dat"))
            {
                AllianceIDToTicker = Utils.DeserializeFromDisk<SerializableDictionary<string, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceTickers.dat");
            }
            else
            {
                AllianceIDToTicker = new SerializableDictionary<string, string>();
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
        public System GetEveSystemFromID(string id)
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
        public string GetEveSystemNameFromID(string id)
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
            UriBuilder esiLogonBuilder = new UriBuilder("https://login.eveonline.com/oauth/authorize/");

            var esiQuery = HttpUtility.ParseQueryString(esiLogonBuilder.Query);
            esiQuery["response_type"] = "code";
            esiQuery["client_id"] = EveAppConfig.ClientID;
            esiQuery["redirect_uri"] = EveAppConfig.CallbackURL;
            esiQuery["scope"] = "publicData esi-location.read_location.v1 esi-search.search_structures.v1 esi-clones.read_clones.v1 esi-universe.read_structures.v1 esi-fleets.read_fleet.v1 esi-ui.write_waypoint.v1 esi-characters.read_standings.v1 esi-location.read_online.v1 esi-characters.read_fatigue.v1 esi-alliances.read_contacts.v1";
            esiQuery["state"] = Process.GetCurrentProcess().Id.ToString();

            esiLogonBuilder.Query = esiQuery.ToString();

            // old way... Process.Start();
            return esiLogonBuilder.ToString();
        }

        /// <summary>
        /// Hand the custom smtauth- url we get back from the logon screen
        /// </summary>
        public bool HandleEveAuthSMTUri(Uri uri)
        {
            // parse the uri
            var query = HttpUtility.ParseQueryString(uri.Query);

            if (query["state"] == null || int.Parse(query["state"]) != Process.GetCurrentProcess().Id)
            {
                // this query isnt for us..
                return false;
            }

            if (query["code"] == null)
            {
                // we're missing a query code
                return false;
            }

            // now we have the initial uri call back we can verify the auth code
            string url = @"https://login.eveonline.com/oauth/token";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Post;
            request.Timeout = 20000;
            request.Proxy = null;

            string code = query["code"];
            string authHeader = EveAppConfig.ClientID + ":" + EveAppConfig.SecretKey;
            string authHeader_64 = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader));

            request.Headers[HttpRequestHeader.Authorization] = authHeader_64;

            var httpData = HttpUtility.ParseQueryString(string.Empty);
            httpData["grant_type"] = "authorization_code";
            httpData["code"] = code;

            string httpDataStr = httpData.ToString();
            byte[] data = UTF8Encoding.UTF8.GetBytes(httpDataStr);
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);

            request.BeginGetResponse(new AsyncCallback(ESIValidateAuthCodeCallback), request);

            return true;
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

            string allianceList = "[";
            foreach (MapSystem s in r.MapSystems.Values.ToList())
            {
                if (s.ActualSystem.SOVAlliance != null && !AllianceIDToName.Keys.Contains(s.ActualSystem.SOVAlliance) && !allianceList.Contains(s.ActualSystem.SOVAlliance))
                {
                    allianceList += "\"";
                    allianceList += s.ActualSystem.SOVAlliance;
                    allianceList += "\",";
                }
            }
            allianceList += "\"0\"]";

            if (allianceList.Length > 8)
            {
                string url = @"https://esi.evetech.net/v2/universe/names/?datasource=tranquility";

               byte[] data = UTF8Encoding.UTF8.GetBytes(allianceList);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Post;
                request.Timeout = 20000;
                request.Proxy = null;


                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                var stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);

                request.BeginGetResponse(new AsyncCallback(ESIUpdateAllianceIDCallback), request);
            }
            else
            {
                // we've cached every known system on the map already
            }
        }

        /// <summary>
        /// Update the Alliance and Ticker data for specified list
        /// </summary>
        public void ResolveAllianceIDs(List<string> IDs)
        {
            string allianceList = "["; ;
            foreach (string s in IDs)
            {
                if (!AllianceIDToName.Keys.Contains(s) && !allianceList.Contains(s))
                {
                    allianceList += "\"";
                    allianceList += s ;
                    allianceList += "\",";
                }
            }

            allianceList += "\"0\"]";

            if (allianceList.Length > 8)
            {
                string url = @"https://esi.evetech.net/v2/universe/names/?datasource=tranquility";

                byte[] data = UTF8Encoding.UTF8.GetBytes(allianceList);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Post;
                request.Timeout = 20000;
                request.Proxy = null;
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                var stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);

                request.BeginGetResponse(new AsyncCallback(ESIUpdateAllianceIDCallback), request);


            }
            else
            {
                // we've cached every known system on the map already
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

            request.BeginGetResponse(new AsyncCallback(UpdateTheraConnectionsCallback), request);

            TheraConnections.Clear();
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

            // start the character update thread
            StartUpdateCharacterThread();

            InitTheraConnections();

            InitZKillFeed();


            StartUpdateESIUniverseDataThread();
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
                    c.Update();

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
                                        }), DispatcherPriority.ApplicationIdle);


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
                                }), DispatcherPriority.ApplicationIdle);
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
                                        foreach(System sys in Systems)
                                        {
                                            if(sys.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0)
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
                            }), DispatcherPriority.ApplicationIdle);
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

        /// <summary>
        /// ESI Validate Auth Code Callback
        /// </summary>
        private void ESIValidateAuthCodeCallback(IAsyncResult asyncResult)
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
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                pendingAccessToken = obj["access_token"].ToString();
                                pendingTokenType = obj["token_type"].ToString();
                                pendingExpiresIn = obj["expires_in"].ToString();
                                pendingRefreshToken = obj["refresh_token"].ToString();

                                // now requests the character information
                                string url = @"https://login.eveonline.com/oauth/verify";
                                HttpWebRequest verifyRequest = (HttpWebRequest)WebRequest.Create(url);
                                verifyRequest.Method = WebRequestMethods.Http.Get;
                                verifyRequest.Timeout = 20000;
                                verifyRequest.Proxy = null;
                                string authHeader = "Bearer " + pendingAccessToken;

                                verifyRequest.Headers[HttpRequestHeader.Authorization] = authHeader;

                                verifyRequest.BeginGetResponse(new AsyncCallback(ESIVerifyAccessCodeCallback), verifyRequest);
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
        /// ESI Verify Access Code Callback
        /// </summary>
        private void ESIVerifyAccessCodeCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream verifyStream = response.GetResponseStream();
                    using (StreamReader srr = new StreamReader(verifyStream))
                    {
                        string verifyResult = srr.ReadToEnd();

                        JsonTextReader jsr = new JsonTextReader(new StringReader(verifyResult));
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                string characterID = obj["CharacterID"].ToString();
                                string characterName = obj["CharacterName"].ToString();
                                string tokenType = obj["TokenType"].ToString();
                                string characterOwnerHash = obj["CharacterOwnerHash"].ToString();
                                string expiresOn = obj["ExpiresOn"].ToString();

                                // now find the matching character and update..
                                LocalCharacter esiChar = null;
                                foreach (LocalCharacter c in LocalCharacters)
                                {
                                    if (c.Name == characterName)
                                    {
                                        esiChar = c;
                                    }
                                }

                                if (esiChar == null)
                                {
                                    esiChar = new LocalCharacter(characterName, string.Empty, string.Empty);

                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                    {
                                        LocalCharacters.Add(esiChar);
                                    }), DispatcherPriority.ApplicationIdle);
                                }

                                esiChar.ESIRefreshToken = pendingRefreshToken;
                                esiChar.ESILinked = true;
                                esiChar.ESIAccessToken = pendingAccessToken;
                                esiChar.ESIAccessTokenExpiry = DateTime.Parse(expiresOn);
                                esiChar.ID = characterID;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
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
                                EVEData.System es = GetEveSystemFromID(s.SystemId.ToString());
                                
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

        

        /// <summary>
        /// Start the ESI download for the kill info
        /// </summary>
        private void StartUpdateKillsFromESI()
        {
            string url = @"https://esi.evetech.net/v2/universe/system_kills/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIKillsReadCallback), request);
        }

        /// <summary>
        /// ESI Result Response
        /// </summary>
        private void ESIKillsReadCallback(IAsyncResult asyncResult)
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

                        // JSON feed is now in the format : [{ "system_id": 30035042, "ship_kills": 103, "npc_kills": 103, "pod_kills": 0},
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                string systemID = obj["system_id"].ToString();
                                string shipKills = obj["ship_kills"].ToString();
                                string npcKills = obj["npc_kills"].ToString();
                                string podKills = obj["pod_kills"].ToString();

                                if (SystemIDToName[systemID] != null)
                                {
                                    System es = GetEveSystem(SystemIDToName[systemID]);
                                    if (es != null)
                                    {
                                        es.ShipKillsLastHour = int.Parse(shipKills);
                                        es.PodKillsLastHour = int.Parse(podKills);
                                        es.NPCKillsLastHour = int.Parse(npcKills);
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
        /// Start the ESI download for the Jump info
        /// </summary>
        private void StartUpdateJumpsFromESI()
        {
            string url = @"https://esi.evetech.net/v1/universe/system_jumps/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIJumpsReadCallback), request);
        }

        /// <summary>
        /// ESI Result Response
        /// </summary>
        private void ESIJumpsReadCallback(IAsyncResult asyncResult)
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

                        // JSON feed is now in the format : [{ "system_id": 30035042, "ship_jumps": 103},
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                string systemID = obj["system_id"].ToString();
                                string ship_jumps = obj["ship_jumps"].ToString();

                                if (SystemIDToName[systemID] != null)
                                {
                                    System es = GetEveSystem(SystemIDToName[systemID]);
                                    if (es != null)
                                    {
                                        es.JumpsLastHour = int.Parse(ship_jumps);
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
        /// Start the ESI download for the Jump info
        /// </summary>
        private void StartUpdateIncursionsFromESI()
        {
            string url = @"https://esi.evetech.net/latest/incursions/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIIncursionsReadCallback), request);
        }

        /// <summary>
        /// ESI Result Response
        /// </summary>
        private void ESIIncursionsReadCallback(IAsyncResult asyncResult)
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
                        IncursionData.IncursionInfo[] incursions  = IncursionData.IncursionInfo.FromJson(strContent);

                        foreach(IncursionData.IncursionInfo id in incursions)
                        {
                            foreach(long systemID in id.InfestedSolarSystems)
                            {
                                EVEData.System s = GetEveSystemFromID(systemID.ToString());
                                if(s!=null)
                                {
                                    s.ActiveIncursion = true; 
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
        /// Start the Character Update Thread
        /// </summary>
        private void StartUpdateCharacterThread()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // loop forever
                while (true)
                {
                    for (int i = 0; i < LocalCharacters.Count; i++)
                    {
                        LocalCharacter c = LocalCharacters.ElementAt(i);
                        c.Update();
                    }

                    Thread.Sleep(2000);
                }
            }).Start();
        }


        /// <summary>
        /// Start the Character Update Thread
        /// </summary>
        private void StartUpdateESIUniverseDataThread()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // loop forever
                while (true)
                {
                    UpdateESIUniverseData();

                    // every 30 mins
                    Thread.Sleep(1800000);
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
        }

        /// <summary>
        /// ESI Result Response
        /// </summary>
        private void ESIUpdateSovCallback(IAsyncResult asyncResult)
        {
            string alliancesToResolve = string.Empty;

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
                                string systemID = obj["system_id"].ToString();

                                if (SystemIDToName.Keys.Contains(systemID))
                                {
                                    System es = GetEveSystem(SystemIDToName[systemID]);
                                    if (es != null)
                                    {
                                        if (obj["alliance_id"] != null)
                                        {
                                            es.SOVAlliance = obj["alliance_id"].ToString();

                                            alliancesToResolve += es.SOVAlliance + ",";
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
        /// ESI Result Response
        /// </summary>
        private void ESIUpdateAllianceIDCallback(IAsyncResult asyncResult)
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

                                if(obj["category"].ToString() != "alliance")
                                {
                                    continue;
                                }

                                string allianceName = obj["name"].ToString();
                                string allianceId = obj["id"].ToString();
                                AllianceIDToName[allianceId] = allianceName;

                                string allianceUrl = @"https://esi.evetech.net/v3/alliances/" + allianceId + "/?datasource=tranquility";

                                HttpWebRequest allianceRequest = (HttpWebRequest)WebRequest.Create(allianceUrl);
                                allianceRequest.Method = WebRequestMethods.Http.Get;
                                allianceRequest.Timeout = 20000;
                                allianceRequest.Proxy = null;
                                
                                WebResponse allianceRequestWebResponse = allianceRequest.GetResponse();

                                Stream allianceRequestResponeStream = allianceRequestWebResponse.GetResponseStream();
                                using (StreamReader allianceRequestResponseStreamReader = new StreamReader(allianceRequestResponeStream))
                                {
                                    // Need to return this response
                                    string allianceRequestString = allianceRequestResponseStreamReader.ReadToEnd();

                                    JsonTextReader jobj = new JsonTextReader(new StringReader(allianceRequestString));
                                    while (jobj.Read())
                                    {
                                        if (jobj.TokenType == JsonToken.StartObject)
                                        {
                                            JObject aobj = JObject.Load(jobj);
                                            allianceName = aobj["name"].ToString();
                                            string allianceTicker = aobj["ticker"].ToString();
                                            AllianceIDToTicker[allianceId] = allianceTicker;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
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
                                string solarSystemId = obj["wormholeDestinationSolarSystemId"].ToString();
                                string wormHoleEOL = obj["wormholeEol"].ToString();
                                string type = obj["type"].ToString();

                                if (type != null && type == "wormhole" && solarSystemId != null && wormHoleEOL != null && SystemIDToName.Keys.Contains(solarSystemId))
                                {
                                    System theraConnectionSystem = GetEveSystemFromID(solarSystemId);

                                    TheraConnection tc = new TheraConnection(theraConnectionSystem.Name, theraConnectionSystem.Region, inSignatureId, outSignatureId, wormHoleEOL);

                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                    {
                                        TheraConnections.Add(tc);
                                    }), DispatcherPriority.ApplicationIdle);
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
