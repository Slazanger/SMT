//-----------------------------------------------------------------------
// EVE Manager (partial)
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
    // Data generation (CreateFromScratch)
    public partial class EveManager
    {
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

            Regions.Add(new MapRegion("Yasna Zakh", "", string.Empty, 50, 260));

            SystemIDToName = new SerializableDictionary<long, string>();

            Systems = new List<System>();

            // update the region cache
            foreach(MapRegion rd in Regions)
            {
                string localSVG = sourceFolder + @"\data\SourceMaps\raw\" + rd.DotLanRef + "_layout.svg";

                if(!File.Exists(localSVG))
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

                foreach(XmlNode xn in xnl)
                {
                    long systemID = long.Parse(xn.Attributes["ID"].Value);
                    float x = float.Parse(xn.Attributes["x"].Value);
                    float y = float.Parse(xn.Attributes["y"].Value);

                    /*
                    float RoundVal = 2.0f;
                    x = (float)Math.Round(x / RoundVal, 0) * RoundVal;
                    y = (float)Math.Round(y / RoundVal, 0) * RoundVal;
                    */
                    x = (float)Math.Round(x, 0);
                    y = (float)Math.Round(y, 0);

                    string name;
                    string region;

                    if(xn.Attributes["Name"] == null)
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
                    if(region == rd.Name)
                    {
                        System s = new System(name, systemID, rd.Name, hasStation, hasIceBelt);
                        if(GetEveSystem(name) != null)
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
            if(File.Exists(eveStaticDataSolarSystemFile))
            {
                StreamReader file = new StreamReader(eveStaticDataSolarSystemFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
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
                    if(s != null)
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
                        if(regionID == "10000070")
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
            if(File.Exists(eveStaticDataRegionFile))
            {
                StreamReader file = new StreamReader(eveStaticDataRegionFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string regionName = bits[1]; // SystemIDToName[SystemID];

                    double x = Convert.ToDouble(bits[2]);
                    double y = Convert.ToDouble(bits[3]);
                    double z = Convert.ToDouble(bits[4]);

                    MapRegion r = GetRegion(regionName);
                    if(r != null)
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
            if(File.Exists(eveStaticDataJumpsFile))
            {
                StreamReader file = new StreamReader(eveStaticDataJumpsFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    long fromID = long.Parse(bits[2]);
                    long toID = long.Parse(bits[3]);

                    System from = GetEveSystemFromID(fromID);
                    System to = GetEveSystemFromID(toID);

                    if(from != null && to != null)
                    {
                        if(from.Name == "Zarzakh" || to.Name == "Zarzakh")
                        {
                            // zarzakh is now in the jumps file; however we dont want to add the gates for it
                            continue;
                        }

                        if(!from.Jumps.Contains(to.Name))
                        {
                            from.Jumps.Add(to.Name);
                        }
                        if(!to.Jumps.Contains(from.Name))
                        {
                            to.Jumps.Add(from.Name);
                        }
                    }
                }
            }

            string eveStaticDataJumpsExtraFile = sourceFolder + @"\data\mapSolarSystemJumpsExtra.csv";
            if(File.Exists(eveStaticDataJumpsExtraFile))
            {
                StreamReader file = new StreamReader(eveStaticDataJumpsExtraFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string fromName = bits[0];
                    string toName = bits[1];

                    System from = GetEveSystem(fromName);
                    System to = GetEveSystem(toName);

                    if(from != null && to != null)
                    {
                        if(!from.Jumps.Contains(to.Name))
                        {
                            from.Jumps.Add(to.Name);
                        }
                        if(!to.Jumps.Contains(from.Name))
                        {
                            to.Jumps.Add(from.Name);
                        }
                    }
                }
            }

            // now open up the eve static data export and extract some info from it
            string eveStaticDataStationsFile = sourceFolder + @"\data\staStations.csv";
            if(File.Exists(eveStaticDataStationsFile))
            {
                StreamReader file = new StreamReader(eveStaticDataStationsFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
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
                throw new Exception("Data Creation Error");
            }

            // now open up the eve static data export and extract some info from it
            string eveStaticDataConstellationFile = sourceFolder + @"\data\mapConstellations.csv";
            if(File.Exists(eveStaticDataConstellationFile))
            {
                StreamReader file = new StreamReader(eveStaticDataConstellationFile);

                Dictionary<string, string> constMap = new Dictionary<string, string>();

                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string constID = bits[1];
                    string constName = bits[2];

                    constMap[constID] = constName;
                }

                // TEMP : Manually add
                constMap["20010000"] = "Duzna Kah";

                foreach(System s in Systems)
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
            if(File.Exists(iceSystemsFile))
            {
                StreamReader file = new StreamReader(iceSystemsFile);
                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    System s = GetEveSystem(line);
                    if(s != null)
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
            if(File.Exists(fwSystemsFile))
            {
                StreamReader file = new StreamReader(fwSystemsFile);
                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    System s = GetEveSystem(line);
                    if(s != null)
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
            if(File.Exists(blueSunSystemsFile))
            {
                StreamReader file = new StreamReader(blueSunSystemsFile);
                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    System s = GetEveSystem(line);
                    if(s != null)
                    {
                        s.HasBlueA0Star = true;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            foreach(System s in Systems)
            {
                NameToSystem[s.Name] = s;
                IDToSystem[s.ID] = s;

                // default to no invasion
                s.TrigInvasionStatus = System.EdenComTrigStatus.None;
            }

            string trigSystemsFile = sourceFolder + @"\data\trigInvasionSystems.csv";
            if(File.Exists(trigSystemsFile))
            {
                StreamReader file = new StreamReader(trigSystemsFile);

                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    string[] bits = line.Split(',');

                    string systemid = bits[0];
                    string status = bits[1];

                    System.EdenComTrigStatus invasionStatus = System.EdenComTrigStatus.None;
                    switch(status)
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

                    if(invasionStatus != System.EdenComTrigStatus.None)
                    {
                        System s = GetEveSystemFromID(long.Parse(systemid));
                        if(s != null)
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
            foreach(MapRegion mr in Regions)
            {
                // enforce a minimum spread
                bool mrDone = false;
                int mrIteration = 0;
                float mrMinSpread = 49.0f;

                while(!mrDone)
                {
                    mrIteration++;
                    bool movedThisTime = false;

                    foreach(MapSystem sysA in mr.MapSystems.Values)
                    {
                        foreach(MapSystem sysB in mr.MapSystems.Values)
                        {
                            if(sysA == sysB)
                            {
                                continue;
                            }

                            float dx = sysA.Layout.X - sysB.Layout.X;
                            float dy = sysA.Layout.Y - sysB.Layout.Y;
                            float l = (float)Math.Sqrt(dx * dx + dy * dy);

                            float s = mrMinSpread - l;

                            if(s > 0)
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

                    if(movedThisTime == false)
                    {
                        mrDone = true;
                    }

                    if(mrIteration > 20)
                    {
                        mrDone = true;
                    }
                }

                // collect the system points to generate them from
                List<Vector2f> points = new List<Vector2f>();

                foreach(MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    points.Add(new Vector2f(ms.Layout.X, ms.Layout.Y));
                }

                // generate filler points to help the voronoi to get better partitioning of open areas
                int division = 5;
                int minDistance = 100;
                int minDistanceOOR = 70;
                int margin = 180;

                List<Vector2f> fillerPoints = new List<Vector2f>();

                for(int ix = -margin; ix < 1050 + margin; ix += division)
                {
                    for(int iy = -margin; iy < 800 + margin; iy += division)
                    {
                        bool add = true;

                        foreach(MapSystem ms in mr.MapSystems.Values.ToList())
                        {
                            double dx = ms.Layout.X - ix;
                            double dy = ms.Layout.Y - iy;
                            double l = Math.Sqrt(dx * dx + dy * dy);

                            if(ms.OutOfRegion)
                            {
                                if(l < (minDistanceOOR))
                                {
                                    add = false;
                                    break;
                                }
                            }
                            else
                            {
                                if(l < (minDistance))
                                {
                                    add = false;
                                    break;
                                }
                            }
                        }

                        if(add)
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
                foreach(MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    csDelaunay.Site s = v.SitesIndexedByLocation[points[i]];
                    i++;

                    List<Vector2f> cellList = s.Region(clipRect);
                    ms.Layout = new Vector2(s.x, s.y);

                    ms.CellPoints = new List<Vector2>();

                    foreach(Vector2f vc in cellList)
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

            foreach(MapRegion rr in Regions)
            {
                // link to the real systems
                foreach(MapSystem ms in rr.MapSystems.Values.ToList())
                {
                    ms.ActualSystem = GetEveSystem(ms.Name);

                    if(!ms.OutOfRegion)
                    {
                        if(ms.ActualSystem.TrueSec >= 0.45)
                        {
                            rr.HasHighSecSystems = true;
                        }

                        if(ms.ActualSystem.TrueSec > 0.0 && ms.ActualSystem.TrueSec < 0.45)
                        {
                            rr.HasLowSecSystems = true;
                        }

                        if(ms.ActualSystem.TrueSec <= 0.0)
                        {
                            rr.HasNullSecSystems = true;
                        }
                    }

                    if(rr.MetaRegion)
                    {
                        ms.OutOfRegion = !ms.ActualSystem.FactionWarSystem;
                    }
                }
            }

            // default to text below
            foreach(MapRegion rr in Regions)
            {
                foreach(MapSystem msA in rr.MapSystems.Values.ToList())
                {
                    msA.TextPos = MapSystem.TextPosition.Bottom;
                }
            }

            /*DISABLED FOR NOW

            // calculate the optimal text offset
            foreach(MapRegion rr in Regions)
            {
                foreach(MapSystem msA in rr.MapSystems.Values.ToList())
                {
                    bool TopClear = true;
                    bool BottomClear = true;
                    bool LeftClear = true;
                    bool RightClear = true;
                    float MaxDistance = 60.0f;

                    foreach(string sj in msA.ActualSystem.Jumps)
                    {
                        if(rr.IsSystemOnMap(sj))
                        {
                            // if its within range
                            Vector2 v1 = msA.Layout;
                            Vector2 v2 = rr.MapSystems[sj].Layout;

                            // calculate the azimuth between them
                            float deltaX = v2.X - v1.X;
                            float deltaY = v2.Y - v1.Y;

                            // Calculate the angle in radians
                            double angleInRadians = Math.Atan2(deltaY, deltaX);

                            // Convert the angle to degrees
                            double angleInDegrees = angleInRadians * (180.0 / Math.PI);

                            // Ensure the angle is between 0 and 360 degrees
                            if(angleInDegrees < 0)
                            {
                                angleInDegrees += 360;
                            }

                            // 0 to the right
                            // 90 below
                            // 180 to the left
                            // 270 above

                            if(angleInDegrees > 205 && angleInDegrees < 335)
                            {
                                TopClear = false;
                            }

                            if(angleInDegrees > 25 && angleInDegrees < 155)
                            {
                                BottomClear = false;
                            }

                            if(angleInDegrees > 295 || angleInDegrees < 65)
                            {
                                RightClear = false;
                            }

                            if(angleInDegrees > 115 && angleInDegrees < 245)
                            {
                                LeftClear = false;
                            }
                        }
                    }

                    foreach(MapSystem msB in rr.MapSystems.Values.ToList())
                    {
                        if(msA.Name == msB.Name)
                        {
                            continue;
                        }

                        // if its within range
                        Vector2 v1 = msA.Layout;
                        Vector2 v2 = msB.Layout;

                        Vector2 vDifference = v1 - v2;

                        if(vDifference.Length() < MaxDistance)
                        {
                            // calculate the azimuth between them
                            float deltaX = v2.X - v1.X;
                            float deltaY = v2.Y - v1.Y;

                            // Calculate the angle in radians
                            double angleInRadians = Math.Atan2(deltaY, deltaX);

                            // Convert the angle to degrees
                            double angleInDegrees = angleInRadians * (180.0 / Math.PI);

                            // Ensure the angle is between 0 and 360 degrees
                            if(angleInDegrees < 0)
                            {
                                angleInDegrees += 360;
                            }

                            // 0 to the right
                            // 90 below
                            // 180 to the left
                            // 270 above

                            if(angleInDegrees > 205 && angleInDegrees < 335)
                            {
                                TopClear = false;
                            }

                            if(angleInDegrees > 25 && angleInDegrees < 155)
                            {
                                BottomClear = false;
                            }

                            if(angleInDegrees > 295 || angleInDegrees < 65)
                            {
                                RightClear = false;
                            }

                            if(angleInDegrees > 115 && angleInDegrees < 245)
                            {
                                LeftClear = false;
                            }
                        }
                    }

                    // default
                    msA.TextPos = MapSystem.TextPosition.Bottom;

                    if(LeftClear)
                    {
                        msA.TextPos = MapSystem.TextPosition.Left;
                    }

                    if(RightClear)
                    {
                        msA.TextPos = MapSystem.TextPosition.Right;
                    }

                    if(TopClear)
                    {
                        msA.TextPos = MapSystem.TextPosition.Top;
                    }

                    if(BottomClear)
                    {
                        msA.TextPos = MapSystem.TextPosition.Bottom;
                    }
                }
            }

            */

            // collect the system points to generate them from
            List<Vector2f> regionpoints = new List<Vector2f>();

            // now Generate the region links
            foreach(MapRegion mr in Regions)
            {
                mr.RegionLinks = new List<string>();

                regionpoints.Add(new Vector2f(mr.UniverseViewX, mr.UniverseViewY));

                foreach(MapSystem ms in mr.MapSystems.Values.ToList())
                {
                    // only check systems in the region
                    if(ms.ActualSystem.Region == mr.Name)
                    {
                        foreach(string s in ms.ActualSystem.Jumps)
                        {
                            System sys = GetEveSystem(s);

                            // we have link to another region
                            if(sys.Region != mr.Name)
                            {
                                if(!mr.RegionLinks.Contains(sys.Region))
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
            if(File.Exists(eveStaticDataItemTypesFile))
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
                while((line = file.ReadLine()) != null)
                {
                    if(string.IsNullOrEmpty(line))
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

                    if(ValidShipGroupIDs.Contains(groupID))
                    {
                        ShipTypes.Add(typeID, ItemName);
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // until we build a new jsonline parser for the new SDE format manually add the new ships from Catalyst which are
            // the Pioneer,
            // Outrider,
            // Venture Consortium Issue
            // Pioneer Consortium Issue
            // Odysseus

            ShipTypes.Add("89240", "Pioneer");
            ShipTypes.Add("89649", "Outrider");
            ShipTypes.Add("89648", "Venture Consortium Issue");
            ShipTypes.Add("89647", "Pioneer Consortium Issue");
            ShipTypes.Add("89607", "Odysseus");

            // now add the jove systems
            string eveStaticDataJoveObservatories = sourceFolder + @"\data\JoveSystems.csv";
            if(File.Exists(eveStaticDataJoveObservatories))
            {
                StreamReader file = new StreamReader(eveStaticDataJoveObservatories);

                // read the headers..
                string line;
                line = file.ReadLine();
                while((line = file.ReadLine()) != null)
                {
                    if(string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    string[] bits = line.Split(';');

                    if(bits.Length != 4)
                    {
                        continue;
                    }

                    string system = bits[0];

                    System s = GetEveSystem(system);
                    if(s != null)
                    {
                        s.HasJoveObservatory = true;
                    }
                }
            }
            else
            {
                throw new Exception("Data Creation Error");
            }

            // Now add the joveGate Systems
            string eveStaticDataJoveGates = sourceFolder + @"\data\JoveGates.csv";
            if(File.Exists(eveStaticDataJoveGates))
            {
                StreamReader file = new StreamReader(eveStaticDataJoveGates);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    System s = GetEveSystem(line);
                    if(s != null)
                    {
                        s.HasJoveGate = true;
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

            foreach(EVEData.System sys in Systems)
            {
                if((double)sys.ActualX < universeXMin)
                {
                    universeXMin = (double)sys.ActualX;
                }

                if((double)sys.ActualX > universeXMax)
                {
                    universeXMax = (double)sys.ActualX;
                }

                if((double)sys.ActualZ < universeZMin)
                {
                    universeZMin = (double)sys.ActualZ;
                }

                if((double)sys.ActualZ > universeZMax)
                {
                    universeZMax = (double)sys.ActualZ;
                }
            }
            double universeWidth = universeXMax - universeXMin;
            double universeDepth = universeZMax - universeZMin;
            double XScale = RenderSize / universeWidth;
            double ZScale = RenderSize / universeDepth;
            double universeScale = Math.Min(XScale, ZScale);

            foreach(EVEData.System sys in Systems)
            {
                double X = ((double)sys.ActualX - universeXMin) * universeScale;

                // need to invert Z
                double Z = (universeDepth - ((double)sys.ActualZ - universeZMin)) * universeScale;

                sys.UniverseX = X;
                sys.UniverseY = Z;
            }

            // now create the region outlines and recalc the centre
            foreach(MapRegion mr in Regions)
            {
                mr.RegionX = (mr.RegionX - universeXMin) * universeScale;
                mr.RegionY = (universeDepth - (mr.RegionY - universeZMin)) * universeScale;

                List<nAlpha.Point> regionShapePL = new List<nAlpha.Point>();
                foreach(System s in Systems)
                {
                    if(s.Region == mr.Name)
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
                while(count < edgeCount)
                {
                    foreach(Tuple<int, int> i in ns.Edges)
                    {
                        if(processed.Contains(i))
                            continue;

                        if(i.Item1 == CurrentPoint)
                        {
                            mr.RegionOutline.Add(new Vector2((int)ns.Vertices[CurrentPoint].X, (int)ns.Vertices[CurrentPoint].Y));
                            CurrentPoint = i.Item2;
                            processed.Add(i);
                            break;
                        }

                        if(i.Item2 == CurrentPoint)
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

            while(!done)
            {
                iteration++;
                bool movedThisTime = false;

                foreach(EVEData.System sysA in Systems)
                {
                    foreach(EVEData.System sysB in Systems)
                    {
                        if(sysA == sysB)
                        {
                            continue;
                        }

                        double dx = sysA.UniverseX - sysB.UniverseX;
                        double dy = sysA.UniverseY - sysB.UniverseY;
                        double l = Math.Sqrt(dx * dx + dy * dy);

                        double s = minSpread - l;

                        if(s > 0)
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

                if(movedThisTime == false)
                {
                    done = true;
                }

                if(iteration > 20)
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
    }
}
