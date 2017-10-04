using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Xml;
using System.Xml.Serialization;

namespace SMT.EVEData
{
    public class EveManager
    {
        public const string CLIENT_ID = "ace68fde71fc4749bb27f33e8aad0b70";
        public const string SECRET_KEY = "kT7fsRg8WiRb9lujedQVyKEPgaJr40hevUdTdKaF";


        private static NLog.Logger OutputLog = NLog.LogManager.GetCurrentClassLogger();

        private static EveManager s_Instance;

        public static void SetInstance(EveManager instance)
        {
            s_Instance = instance;
        }

        public static EveManager GetInstance()
        {
             return s_Instance;
        }

        /// <summary>
        /// Master List of Regions
        /// </summary>
        public List<MapRegion> Regions { get; set; }

        public SerializableDictionary<string, System> Systems { get; set; }

        /// <summary>
        /// Lookup from Internal ID to Name
        /// </summary>
        public SerializableDictionary<string, string> SystemIDToName { get; set; }


        public SerializableDictionary<string, string> AllianceIDToName { get; set; }

        public SerializableDictionary<string, string> AllianceIDToTicker { get; set; }


        public string GetAllianceName( string ID )
        {
            string Name = String.Empty;
            if (AllianceIDToName.Keys.Contains(ID)) 
            {
                Name = AllianceIDToName[ID];
            }
            return Name;
        }

        public string GetAllianceTicker(string ID)
        {
            string Ticker = String.Empty;
            if (AllianceIDToTicker.Keys.Contains(ID))
            {
                Ticker = AllianceIDToTicker[ID];
            }
            return Ticker;
        }



        /// <summary>
        /// List of Jumb bridoges
        /// </summary>

        /// <summary>
        /// Folder to cache dotland svg's etc to
        /// </summary>
        public string DataCacheFolder { get; set; }

        [XmlIgnoreAttribute]
        public ObservableCollection<Character> LocalCharacters { get; set; }

        public void LoadCharacters()
        {
            string dataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Characters.dat";
            if (!File.Exists(dataFilename))
            {
                return;
            }

            try
            {
                ObservableCollection<Character> loadList;
                XmlSerializer xms = new XmlSerializer(typeof(ObservableCollection<Character>));

                FileStream fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
                XmlReader xmlr = XmlReader.Create(fs);

                loadList = (ObservableCollection<Character>)xms.Deserialize(xmlr);

                foreach (Character c in loadList)
                {
                    c.ESIAccessToken = string.Empty;
                    c.ESIAccessTokenExpiry = DateTime.MinValue;
                    c.LocalChatFile = string.Empty;
                    c.Location = string.Empty;
                    c.Update();

                    LocalCharacters.Add(c);
                }
            }
            catch { }
        }

        public void SaveData()
        {
            // save off only the ESI authenticated Characters so create a new copy to serialise from..
            ObservableCollection<Character> saveList = new ObservableCollection<Character>();

            foreach (Character c in LocalCharacters)
            {
                if (c.ESIRefreshToken != string.Empty)
                {
                    saveList.Add(c);
                }
            }

            XmlSerializer xms = new XmlSerializer(typeof(ObservableCollection<Character>));
            string dataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Characters.dat";

            using (TextWriter tw = new StreamWriter(dataFilename))
            {
                xms.Serialize(tw, saveList);
            }




            // now serialise the caches to disk
            SerializToDisk<SerializableDictionary<string, string>>(AllianceIDToName, AppDomain.CurrentDomain.BaseDirectory + @"\AllianceNames.dat");
            SerializToDisk<SerializableDictionary<string, string>>(AllianceIDToTicker, AppDomain.CurrentDomain.BaseDirectory + @"\AllianceTickers.dat");


        }

        public List<JumpBridge> JumpBridges { get; set; }

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

        private FileSystemWatcher IntelFileWatcher;

        private Dictionary<string, int> IntelFileReadPos;

        public BindingList<EVEData.IntelData> IntelDataList { get; set; }

        public List<string> IntelFilters { get; set; }



        public delegate void IntelAddedEventHandler();

        public event IntelAddedEventHandler IntelAddedEvent;


        public void SetupIntelWatcher()
        {
            IntelFilters = new List<string>();
            IntelDataList = new BindingList<IntelData>();
            string intelFileFilter = AppDomain.CurrentDomain.BaseDirectory + @"\IntelChannels.txt";

            OutputLog.Info("Loading Intel File Filer from  {0}", intelFileFilter);
            if (File.Exists(intelFileFilter))
            {
                StreamReader file = new StreamReader(intelFileFilter);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line.Trim();
                    if (line != string.Empty)
                    {

                        OutputLog.Info("adding intel filer : {0}", line);
                        IntelFilters.Add(line);
                    }
                }
            }

            IntelFileReadPos = new Dictionary<string, int>();

            string eveLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Chatlogs\";
            if (Directory.Exists(eveLogFolder))
            {
                OutputLog.Info("adding file watcher to : {0}", eveLogFolder);

                IntelFileWatcher = new FileSystemWatcher(eveLogFolder);
                IntelFileWatcher.Filter = "*.txt";
                IntelFileWatcher.EnableRaisingEvents = true;
                IntelFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                IntelFileWatcher.Changed += IntelFileWatcher_Changed;
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
                    Thread.Sleep(1000);
                }
            }).Start();
            /// END SUPERHACK
            /// -----------------------------------------------------------------
        }

        public static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.Default;
        }

        private void IntelFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string changedFile = e.FullPath;

            OutputLog.Info("File Watcher triggered : {0}", changedFile);

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
                    Encoding fe = GetEncoding(changedFile);


                    FileStream ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);


                    OutputLog.Info("Log File Encoding : {0}", fe.ToString());


                    StreamReader file = new StreamReader(ifs, fe);

                    int fileReadFrom = 0;

                    // have we seen this file before
                    if (IntelFileReadPos.Keys.Contains<string>(changedFile))
                    {
                        fileReadFrom = IntelFileReadPos[changedFile];
                    }
                    else
                    {
                        if (localChat)
                        {
                            OutputLog.Info("Processing file : {0}", changedFile);
                            string system = string.Empty;
                            string characterName = string.Empty;

                            // read the iniital block
                            while (!file.EndOfStream)
                            {
                                string l = file.ReadLine();
                                fileReadFrom++;

                                if (l.Contains("Channel ID"))
                                {
                                    string temp = l.Split(',')[1].Split(')')[0].Trim();
                                    if (SystemIDToName.Keys.Contains(temp))
                                    {
                                        system = SystemIDToName[temp];
                                    }

                                    // now can read the next line
                                    l = file.ReadLine(); // should be the "Channel Name : Local"
                                    l = file.ReadLine();

                                    characterName = l.Split(':')[1].Trim();

                                    bool addChar = true;
                                    foreach (EVEData.Character c in LocalCharacters)
                                    {
                                        if (characterName == c.Name)
                                        {
                                            c.Location = system;
                                            c.LocalChatFile = changedFile;
                                            addChar = false;
                                        }
                                    }

                                    if (addChar)
                                    {
                                        Application.Current.Dispatcher.Invoke((Action)(() =>
                                        {
                                            LocalCharacters.Add(new EVEData.Character(characterName, changedFile, system));
                                        }));
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
                    // trim any items off the front
                    if (line.Contains("[") && line.Contains("]"))
                    {
                        line = line.Substring(line.IndexOf("["));
                    }


                    while (line != null)
                    {
                        fileReadFrom++;

                        if (localChat)
                        {

                            if (line.StartsWith("[") && line.Contains("EVE System > Channel changed to Local"))
                            {
                                string system = line.Split(':').Last().Trim();
                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                {
                                    foreach (EVEData.Character c in LocalCharacters)
                                    {
                                        if (c.LocalChatFile == changedFile)
                                        {
                                            OutputLog.Info("Character {0} moved from {1} to {2}", c.Name, c.Location, system);

                                            c.Location = system;

                                        }
                                    }
                                }));
                            }
                        }
                        else
                        {
                            //if (line.StartsWith("["))
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
                                            if (DoesSystemExist(s))
                                            {
                                                OutputLog.Info("Adding Intel Line : {0} ", line);
                                                id.Systems.Add(s);
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
                                        OutputLog.Info("Already have Line : {0} ", line);
                                    }
                                }));
                            }
                            //else
                            //{
                            //   OutputLog.Info("Rejecting Line : {0} ", line);
                            //}
                        }

                        line = file.ReadLine();
                    }

                    ifs.Close();

                    IntelFileReadPos[changedFile] = fileReadFrom;
                    OutputLog.Info("File Read pos : {0} {1}", changedFile, fileReadFrom);

                }
                catch ( Exception ex)
                {
                    OutputLog.Info("Intel Process Failed : {0}", ex.ToString());
                }

            }
            else
            {
                OutputLog.Info("Skipping File : {0}", changedFile);

            }
        }

        /// <summary>
        /// Scrape the maps from dotlan and initialise the region data from dotland
        /// </summary>
        public void CreateFromScratch()
        {

            Regions = new List<MapRegion>();

            // manually add the regions we care about
            Regions.Add(new MapRegion("Aridia"));
            Regions.Add(new MapRegion("Black Rise"));
            Regions.Add(new MapRegion("The Bleak Lands"));
            Regions.Add(new MapRegion("Branch"));
            Regions.Add(new MapRegion("Cache"));
            Regions.Add(new MapRegion("Catch"));
            Regions.Add(new MapRegion("The Citadel"));
            Regions.Add(new MapRegion("Cloud Ring"));
            Regions.Add(new MapRegion("Cobalt Edge"));
            Regions.Add(new MapRegion("Curse"));
            Regions.Add(new MapRegion("Deklein"));
            Regions.Add(new MapRegion("Delve"));
            Regions.Add(new MapRegion("Derelik"));
            Regions.Add(new MapRegion("Detorid"));
            Regions.Add(new MapRegion("Devoid"));
            Regions.Add(new MapRegion("Domain"));
            Regions.Add(new MapRegion("Esoteria"));
            Regions.Add(new MapRegion("Essence"));
            Regions.Add(new MapRegion("Etherium Reach"));
            Regions.Add(new MapRegion("Everyshore"));
            Regions.Add(new MapRegion("Fade"));
            Regions.Add(new MapRegion("Feythabolis"));
            Regions.Add(new MapRegion("The Forge"));
            Regions.Add(new MapRegion("Fountain"));
            Regions.Add(new MapRegion("Geminate"));
            Regions.Add(new MapRegion("Genesis"));
            Regions.Add(new MapRegion("Great Wildlands"));
            Regions.Add(new MapRegion("Heimatar"));
            Regions.Add(new MapRegion("Immensea"));
            Regions.Add(new MapRegion("Impass"));
            Regions.Add(new MapRegion("Insmother"));
            Regions.Add(new MapRegion("Kador"));
            Regions.Add(new MapRegion("The Kalevala Expanse"));
            Regions.Add(new MapRegion("Khanid"));
            Regions.Add(new MapRegion("Kor-Azor"));
            Regions.Add(new MapRegion("Lonetrek"));
            Regions.Add(new MapRegion("Malpais"));
            Regions.Add(new MapRegion("Metropolis"));
            Regions.Add(new MapRegion("Molden Heath"));
            Regions.Add(new MapRegion("Oasa"));
            Regions.Add(new MapRegion("Omist"));
            Regions.Add(new MapRegion("Outer Passage"));
            Regions.Add(new MapRegion("Outer Ring"));
            Regions.Add(new MapRegion("Paragon Soul"));
            Regions.Add(new MapRegion("Period Basis"));
            Regions.Add(new MapRegion("Perrigen Falls"));
            Regions.Add(new MapRegion("Placid"));
            Regions.Add(new MapRegion("Providence"));
            Regions.Add(new MapRegion("Pure Blind"));
            Regions.Add(new MapRegion("Querious"));
            Regions.Add(new MapRegion("Scalding Pass"));
            Regions.Add(new MapRegion("Sinq Laison"));
            Regions.Add(new MapRegion("Solitude"));
            Regions.Add(new MapRegion("The Spire"));
            Regions.Add(new MapRegion("Stain"));
            Regions.Add(new MapRegion("Syndicate"));
            Regions.Add(new MapRegion("Tash-Murkon"));
            Regions.Add(new MapRegion("Tenal"));
            Regions.Add(new MapRegion("Tenerifis"));
            Regions.Add(new MapRegion("Tribute"));
            Regions.Add(new MapRegion("Vale of the Silent"));
            Regions.Add(new MapRegion("Venal"));
            Regions.Add(new MapRegion("Verge Vendor"));
            Regions.Add(new MapRegion("Wicked Creek"));

            SystemIDToName = new SerializableDictionary<string, string>();

            Systems = new SerializableDictionary<string, System>();

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

                    string systemnodepath = @"//*[@id='def" + systemID + "']";
                    XmlNodeList snl = xmldoc.SelectNodes(systemnodepath);
                    XmlNode sysdefNode = snl[0];

                    XmlNode aNode = sysdefNode.ChildNodes[0];

                    string name;
                    bool hasStation = false;

                    // scan for <polygon> nodes as these are the npc station nodes
                    foreach (XmlNode sxn in sysdefNode.ChildNodes)
                    {
                        if (sxn.Name == "polygon")
                        {
                            hasStation = true;
                            break;
                        }
                    }

                    // SS Nodes for system nodes
                    XmlNodeList ssNodes = aNode.SelectNodes(@".//*[@class='ss']");
                    if (ssNodes[0] != null)
                    {
                        name = ssNodes[0].InnerText;

                        // create and add the system
                        Systems[name] = new System(name, systemID, rd.Name, hasStation);

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

                // now catch the links - j
                string jXpath = @"//*[@class='j']";
                xn = xmldoc.SelectNodes(jXpath);

                foreach (XmlNode jump in xn)
                {
                    // will be in the format j-XXXXXXXX-YYYYYYYY
                    string id = jump.Attributes["id"].Value.Substring(2);

                    string systemOneID = id.Substring(0, 8);
                    string systemTwoID = id.Substring(9);

                    rd.Jumps.Add(new Link(SystemIDToName[systemOneID], SystemIDToName[systemTwoID], false));
                }

                // now catch the constellation links - jc
                string jcXpath = @"//*[@class='jc']";
                xn = xmldoc.SelectNodes(jcXpath);

                foreach (XmlNode jump in xn)
                {
                    // will be in the format j-XXXXXXXX-YYYYYYYY
                    string id = jump.Attributes["id"].Value.Substring(2);

                    string systemOneID = id.Substring(0, 8);
                    string systemTwoID = id.Substring(9);

                    rd.Jumps.Add(new Link(SystemIDToName[systemOneID], SystemIDToName[systemTwoID], true));
                }

                // now catch the region links - jr
                string jrXpath = @"//*[@class='jr']";
                xn = xmldoc.SelectNodes(jrXpath);

                foreach (XmlNode jump in xn)
                {
                    // will be in the format j-XXXXXXXX-YYYYYYYY
                    string id = jump.Attributes["id"].Value.Substring(2);

                    string systemOneID = id.Substring(0, 8);
                    string systemTwoID = id.Substring(9);

                    rd.Jumps.Add(new Link(SystemIDToName[systemOneID], SystemIDToName[systemTwoID], true));
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
                        s.Security = security;
                    }
                }

            }
            else
            {

            }


            // now create the voronoi regions
            foreach ( MapRegion mr in Regions)
            {

                // collect the system points to generate them from 
                List<Vector2f> points = new List<Vector2f>();


                foreach ( MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    points.Add(new Vector2f(ms.LayoutX, ms.LayoutY));
                }

                // create the voronoi
                csDelaunay.Voronoi v = new csDelaunay.Voronoi(points, new Rectf(0,0, 1050, 800));


                // extract the points from the graph for each cell

                foreach (MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    List<Vector2f> cellList = v.Region(new Vector2f(ms.LayoutX, ms.LayoutY));
                    ms.CellPoints = new List<Point>();
                                        
                    foreach(Vector2f vc in cellList)
                    {
                        ms.CellPoints.Add(new Point(vc.x, vc.y));
                    }

                }
            }



            // now serialise the classes to disk
            SerializToDisk<List<MapRegion>>(Regions, AppDomain.CurrentDomain.BaseDirectory + @"\MapLayout.dat");
            SerializToDisk<SerializableDictionary<string, System>>(Systems, AppDomain.CurrentDomain.BaseDirectory + @"\Systems.dat");




            // now create all of the alliance/corp DB's
            
            /*

            string url = @"https://esi.tech.ccp.is/latest/alliances/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            WebResponse  wr = request.GetResponse();

            Stream responseStream = wr.GetResponseStream();
            using (StreamReader sr = new StreamReader(responseStream))
            {
                string strContent = sr.ReadToEnd();

                JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                // JSON feed is now in the format : [{ "system_id": 30035042,  and then optionally alliance_id, corporation_id and corporation_id, faction_id },
                while (jsr.Read())
                {
                    if(jsr.TokenType == JsonToken.Integer)
                    {
                        string allianceID = jsr.Value.ToString();
                        string allianceUrl = @"https://esi.tech.ccp.is/latest/alliances/" + allianceID +  "/?datasource=tranquility";

                        HttpWebRequest allianceRequest = (HttpWebRequest)WebRequest.Create(allianceUrl);
                        allianceRequest.Method = WebRequestMethods.Http.Get;
                        allianceRequest.Timeout = 20000;
                        allianceRequest.Proxy = null;

                        WebResponse alWR = allianceRequest.GetResponse();

                        Stream alStream = alWR.GetResponseStream();
                        using (StreamReader alSR = new StreamReader(alStream))
                        {
                            // Need to return this response
                            string alStrContent = alSR.ReadToEnd();

                            JsonTextReader jobj = new JsonTextReader(new StringReader(alStrContent));
                            while (jobj.Read())
                            {
                                if (jobj.TokenType == JsonToken.StartObject)
                                {
                                    JObject obj = JObject.Load(jobj);
                                    string AllianceName = obj["alliance_name"].ToString();
                                    string AllianceTicker = obj["ticker"].ToString();

                                    AllianceIDToName[allianceID] = AllianceName;
                                    AllianceIDToTicker[allianceID] = AllianceTicker;

                                }
                            }
                        }


                    }

                    Thread.Sleep(50);

                }
            }
            */
            
            Init();
        }


        public void LoadFromDisk()
        {
            SystemIDToName = new SerializableDictionary<string, string>();

            Regions = DeserializeFromDisk<List<MapRegion>>(AppDomain.CurrentDomain.BaseDirectory + @"\MapLayout.dat");
            Systems = DeserializeFromDisk<SerializableDictionary<string, System>>(AppDomain.CurrentDomain.BaseDirectory + @"\Systems.dat");

            foreach(System s in Systems.Values.ToList())
            {
                SystemIDToName[s.ID] = s.Name;
            }

            AllianceIDToName = DeserializeFromDisk<SerializableDictionary<string, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceNames.dat");
            AllianceIDToTicker = DeserializeFromDisk< SerializableDictionary<string, string>>(AppDomain.CurrentDomain.BaseDirectory + @"\AllianceTickers.dat");



            Init();
        }

        private void SerializToDisk<T>( T obj, string Filename)
        {
            XmlSerializer xms = new XmlSerializer(typeof(T));

            using (TextWriter tw = new StreamWriter(Filename))
            {
                xms.Serialize(tw, obj);
            }
        }

        private T DeserializeFromDisk<T>(string Filename)
        {
            XmlSerializer xms = new XmlSerializer(typeof(T));

            FileStream fs = new FileStream(Filename, FileMode.Open);
            XmlReader xmlr = XmlReader.Create(fs);

            return (T)xms.Deserialize(xmlr);
        }



        /// <summary>
        /// Initialise
        /// </summary>
        private void Init()
        {
            // patch up any links

            foreach (MapRegion rr in Regions)
            {
                // link to the real systems
                foreach (MapSystem ms in rr.MapSystems.Values.ToList())
                {
                    ms.ActualSystem = Systems[ms.Name];
                }
            }

            LoadCharacters();

            // start the character update thread
            StartUpdateCharacterThread();



        }


        /// <summary>
        /// Does the System Exist ?
        /// </summary>
        /// <param name="name">Name (not ID) of the system</param>
        public bool DoesSystemExist(string name)
        {
            return GetEveSystem(name) != null;
        }

        /// <summary>
        /// Get a System object from the name, note : for regions which have other region systems in it wont return
        /// them.. eg TR07-s is on the esoteria map, but the object corresponding to the feythabolis map will be returned
        /// </summary>
        /// <param name="name">Name (not ID) of the system</param>
        public System GetEveSystem(string name)
        {
            if(Systems.Keys.Contains(name))
            {
                return Systems[name];
            }
            return null;
        }

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

        public string GetESILogonURL()
        {
            UriBuilder esiLogonBuilder = new UriBuilder("https://login.eveonline.com/oauth/authorize/");

            var esiQuery = HttpUtility.ParseQueryString(esiLogonBuilder.Query);
            esiQuery["response_type"] = "code";
            esiQuery["client_id"] = CLIENT_ID;
            esiQuery["redirect_uri"] = @"eveauth-smt://callback";
            esiQuery["scope"] = "            publicData esi-location.read_location.v1 esi-ui.write_waypoint.v1 esi-characters.read_standings.v1 esi-location.read_online.v1";


            esiQuery["state"] = Process.GetCurrentProcess().Id.ToString();

            esiLogonBuilder.Query = esiQuery.ToString();

            // old way... Process.Start();
            return esiLogonBuilder.ToString();
        }

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
            string authHeader = CLIENT_ID + ":" + SECRET_KEY;
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

        // since we dont know what character these tokens belong too until we check just store them..
        private string PendingAccessToken;

        private string PendingTokenType;
        private string PendingExpiresIn;
        private string PendingRefreshToken;

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
                                PendingAccessToken = obj["access_token"].ToString();
                                PendingTokenType = obj["token_type"].ToString();
                                PendingExpiresIn = obj["expires_in"].ToString();
                                PendingRefreshToken = obj["refresh_token"].ToString();

                                // now requests the character information
                                string url = @"https://login.eveonline.com/oauth/verify";
                                HttpWebRequest verifyRequest = (HttpWebRequest)WebRequest.Create(url);
                                verifyRequest.Method = WebRequestMethods.Http.Get;
                                verifyRequest.Timeout = 20000;
                                verifyRequest.Proxy = null;
                                string authHeader = "Bearer " + PendingAccessToken;

                                verifyRequest.Headers[HttpRequestHeader.Authorization] = authHeader;

                                verifyRequest.BeginGetResponse(new AsyncCallback(ESIVerifyAccessCodeCallback), verifyRequest);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /// ....
            }
        }

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
                                Character esiChar = null;
                                foreach (Character c in LocalCharacters)
                                {
                                    if (c.Name == characterName)
                                    {
                                        esiChar = c;
                                    }
                                }

                                if (esiChar == null)
                                {
                                    esiChar = new Character(characterName, string.Empty, string.Empty);

                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                    {
                                        LocalCharacters.Add(esiChar);
                                    }));
                                }

                                esiChar.ESIRefreshToken = PendingRefreshToken;
                                esiChar.ESILinked = true;
                                esiChar.ESIAccessToken = PendingAccessToken;
                                esiChar.ESIAccessTokenExpiry = DateTime.Parse(expiresOn);
                                esiChar.ID = characterID;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /// ....
            }
        }

        /// <summary>
        /// Start the ESI download for the kill info
        /// </summary>
        public void StartUpdateKillsFromESI()
        {
            string url = @"https://esi.tech.ccp.is/latest/universe/system_kills/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIKillsReadCallback), request);
        }

        /// <summary>
        /// ESI Result Response
        /// </summary>
        /// <param name="asyncResult"></param>
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
                                string nPCKills = obj["npc_kills"].ToString();
                                string podKills = obj["pod_kills"].ToString();

                                if (SystemIDToName[systemID] != null)
                                {
                                    System es = GetEveSystem(SystemIDToName[systemID]);
                                    if (es != null)
                                    {
                                        es.ShipKillsLastHour = int.Parse(shipKills);
                                        es.PodKillsLastHour = int.Parse(podKills);
                                        es.NPCKillsLastHour = int.Parse(nPCKills);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /// ....
            }
        }

        /// <summary>
        /// Start the ESI download for the Jump info
        /// </summary>
        public void StartUpdateJumpsFromESI()
        {
            string url = @"https://esi.tech.ccp.is/latest/universe/system_jumps/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIJumpsReadCallback), request);
        }

        /// <summary>
        /// ESI Result Response
        /// </summary>
        /// <param name="asyncResult"></param>
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
                /// ....
            }
        }

        public void StartUpdateCharacterThread()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // loop forever
                while (true)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        foreach (Character c in LocalCharacters)
                        {
                            c.Update();
                        }
                    }));
                    Thread.Sleep(5000);
                }
            }).Start();
        }


        /// <summary>
        /// Start the ESI download for the kill info
        /// </summary>
        public void StartUpdateSOVFromESI()
        {
            string url = @"https://esi.tech.ccp.is/latest/sovereignty/map/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIUpdateSovCallback), request);
        }

        /// <summary>
        /// ESI Result Response
        /// </summary>
        /// <param name="asyncResult"></param>
        private void ESIUpdateSovCallback(IAsyncResult asyncResult)
        {
            string AlliancesToResolve = "";

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
                                        if(obj["alliance_id"] != null)
                                        {
                                            es.SOVAlliance = obj["alliance_id"].ToString() ;

                                            AlliancesToResolve += es.SOVAlliance + ",";
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
                /// ....
            }


            // now that we have the list of alliances we need to resolve those into Names


        }


        public void UpdateIDsForMapRegion(string name)
        {
            MapRegion r = GetRegion(name);
            if (r == null)
            {
                return;
            }

            string AllianceList = string.Empty;
            foreach(MapSystem s in r.MapSystems.Values.ToList())
            {
                if(s.ActualSystem.SOVAlliance != null && !AllianceIDToName.Keys.Contains(s.ActualSystem.SOVAlliance) && !AllianceList.Contains(s.ActualSystem.SOVAlliance) )
                {
                    AllianceList += s.ActualSystem.SOVAlliance + ",";
                }
            }

            if (AllianceList != string.Empty)
            {
                AllianceList += "0";
                string url = @"https://esi.tech.ccp.is/latest/alliances/names/?datasource=tranquility";

                url += "&alliance_ids=" + Uri.EscapeUriString(AllianceList);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Get;
                request.Timeout = 20000;
                request.Proxy = null;
                request.BeginGetResponse(new AsyncCallback(ESIUpdateAllianceIDCallback), request);
            }
            else
            {
                // we've cached every known system on the map already
            }

        }

        /// <summary>
        /// ESI Result Response
        /// </summary>
        /// <param name="asyncResult"></param>
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
                                string Name = obj["alliance_name"].ToString();
                                string ID = obj["alliance_id"].ToString();
                                AllianceIDToName[ID] = Name;
                               
                                string allianceUrl = @"https://esi.tech.ccp.is/latest/alliances/" + ID + "/?datasource=tranquility";

                                HttpWebRequest allianceRequest = (HttpWebRequest)WebRequest.Create(allianceUrl);
                                allianceRequest.Method = WebRequestMethods.Http.Get;
                                allianceRequest.Timeout = 20000;
                                allianceRequest.Proxy = null;

                                WebResponse alWR = allianceRequest.GetResponse();

                                Stream alStream = alWR.GetResponseStream();
                                using (StreamReader alSR = new StreamReader(alStream))
                                {
                                    // Need to return this response
                                    string alStrContent = alSR.ReadToEnd();

                                    JsonTextReader jobj = new JsonTextReader(new StringReader(alStrContent));
                                    while (jobj.Read())
                                    {
                                        if (jobj.TokenType == JsonToken.StartObject)
                                        {
                                            JObject aobj = JObject.Load(jobj);
                                            string AllianceName = aobj["alliance_name"].ToString();
                                            string AllianceTicker = aobj["ticker"].ToString();

                                            AllianceIDToTicker[ID] = AllianceTicker;

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
                /// ....
            }
        }


        public double GetRange(string From, string To)
        {
            System A = GetEveSystem(From);
            System B = GetEveSystem(To);

            if(A == null || B == null )
            {
                return 0.0;
            }

            double X = A.ActualX - B.ActualX;
            double Y = A.ActualY - B.ActualY;
            double Z = A.ActualZ - B.ActualZ;

            double Length = Math.Sqrt(X * X + Y * Y + Z * Z);

            return Length; 

        }





        public EveManager()
        {
            LocalCharacters = new ObservableCollection<Character>();

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


        }




    }


}
