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
    // Init, background thread, ESI/Dotlan updates, infrastructure
    public partial class EveManager
    {
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
                var esr = await EveApiClient.FactionWarfare.FactionWarSystemOwnershipAsync();

                string debugListofSytems = "";

                if(ESIHelpers.ValidateESICall(esr))
                {
                    foreach(FactionWarSystem i in esr.Model)
                    {
                        FactionWarfareSystemInfo fwsi = new FactionWarfareSystemInfo();
                        fwsi.SystemState = FactionWarfareSystemInfo.State.None;

                        fwsi.OccupierID = (int)i.OccupierFactionId;
                        fwsi.OccupierName = FactionWarfareSystemInfo.OwnerIDToName((int)i.OccupierFactionId);

                        fwsi.OwnerID = (int)i.OwnerFactionId;
                        fwsi.OwnerName = FactionWarfareSystemInfo.OwnerIDToName((int)i.OwnerFactionId);

                        fwsi.SystemID = (int)i.SolarSystemId;
                        fwsi.SystemName = GetEveSystemNameFromID(i.SolarSystemId);
                        fwsi.LinkSystemID = 0;
                        fwsi.VictoryPoints = (int)i.VictoryPoints;
                        fwsi.VictoryPointsThreshold = (int)i.VictoryPointsThreshold;

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


        /// <summary>
        /// Initialise the eve manager
        /// </summary>
        private void Init()
        {
            string userAgent = "SMT/" + EveAppConfig.SMT_VERSION + EveAppConfig.SMT_USERAGENT_DETAILS;
            EveApiClient = new EVEStandardAPI(userAgent, DataSource.Tranquility, CompatibilityDate.v2025_12_16, TimeSpan.FromSeconds(30));
            Sso = new SSOv2(DataSource.Tranquility, EveAppConfig.CallbackURL, EveAppConfig.ClientID, null);

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
            if(File.Exists(upgradesFile))
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
            ZKillFeed = new ZKillR2Z2();
            ZKillFeed.Initialise();
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
            catch(Exception)
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
                var esr = await EveApiClient.Incursion.ListIncursionsAsync();
                if(ESIHelpers.ValidateESICall(esr))
                {
                    foreach(Incursion i in esr.Model)
                    {
                        foreach(long s in i.InfestedSolarSystems ?? new List<long>())
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
                var esr = await EveApiClient.Universe.GetSystemJumpsAsync();
                if(ESIHelpers.ValidateESICall(esr))
                {
                    foreach(SystemJumps j in esr.Model)
                    {
                        EVEData.System es = GetEveSystemFromID(j.SystemId);
                        if(es != null)
                        {
                            es.JumpsLastHour = (int)j.ShipJumps;
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
                var esr = await EveApiClient.Universe.GetSystemKillsAsync();
                if(ESIHelpers.ValidateESICall(esr))
                {
                    foreach(SystemKills k in esr.Model)
                    {
                        EVEData.System es = GetEveSystemFromID(k.SystemId);
                        if(es != null)
                        {
                            es.NPCKillsLastHour = (int)k.NpcKills;
                            es.PodKillsLastHour = (int)k.PodKills;
                            es.ShipKillsLastHour = (int)k.ShipKills;
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

                var esr = await EveApiClient.Sovereignty.ListSovereigntyCampaignsAsync();
                if(ESIHelpers.ValidateESICall(esr))
                {
                    foreach(SovereigntyCampaign c in esr.Model)
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
                                CampaignID = (int)c.CampaignId,
                                DefendingAllianceID = (int)(c.DefenderId ?? 0),
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

                        if(ss.AttackersScore != (c.AttackersScore ?? 0) || ss.DefendersScore != (c.DefenderScore ?? 0))
                        {
                            sendUpdateEvent = true;
                        }

                        ss.AttackersScore = c.AttackersScore ?? 0;
                        ss.DefendersScore = c.DefenderScore ?? 0;
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
                var esr = await EveApiClient.Sovereignty.ListSovereigntyStructuresAsync();
                if(ESIHelpers.ValidateESICall(esr))
                {
                    foreach(SovereigntyStructure ss in esr.Model)
                    {
                        EVEData.System es = GetEveSystemFromID(ss.SolarSystemId);
                        if(es != null)
                        {
                            // structures : Old TCU  : 32226, Old iHub : 32458
                            es.SOVAllianceID = (int)ss.AllianceId;

                            if(ss.StructureTypeId == 32226)
                            {
                                es.TCUVunerabliltyStart = ss.VulnerableStartTime ?? default;
                                es.TCUVunerabliltyEnd = ss.VulnerableEndTime ?? default;
                                es.TCUOccupancyLevel = (float)(ss.VulnerabilityOccupancyLevel ?? 0);
                            }

                            if(ss.StructureTypeId == 32458)
                            {
                                es.IHubVunerabliltyStart = ss.VulnerableStartTime ?? default;
                                es.IHubVunerabliltyEnd = ss.VulnerableEndTime ?? default;
                                es.IHubOccupancyLevel = (float)(ss.VulnerabilityOccupancyLevel ?? 0);
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
                var esr = await EveApiClient.Status.GetStatusAsync();

                if(ESIHelpers.ValidateESICall(esr))
                {
                    ServerInfo.Name = "Tranquility";
                    ServerInfo.NumPlayers = (int)esr.Model.Players;
                    ServerInfo.ServerVersion = esr.Model.ServerVersion ?? string.Empty;
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
            if(!File.Exists(filePath))
            {
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string currentSystem = null;

                foreach(string line in lines)
                {
                    if(string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string trimmedLine = line.Trim();

                    // Check if this line starts with a digit (upgrade line)
                    bool isUpgradeLine = char.IsDigit(trimmedLine.FirstOrDefault());

                    if(!isUpgradeLine)
                    {
                        // This is a system header line
                        // Support both "Sovereignty Hub SYSTEMNAME" and just "SYSTEMNAME"
                        if(trimmedLine.StartsWith("Sovereignty Hub "))
                        {
                            currentSystem = trimmedLine.Replace("Sovereignty Hub ", "").Trim();
                        }
                        else
                        {
                            currentSystem = trimmedLine;
                        }

                        // Clear existing upgrades for this system
                        System sys = GetEveSystem(currentSystem);
                        if(sys != null)
                        {
                            sys.InfrastructureUpgrades.Clear();
                        }
                    }
                    else if(currentSystem != null)
                    {
                        // Parse upgrade line
                        string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if(parts.Length >= 3)
                        {
                            System sys = GetEveSystem(currentSystem);
                            if(sys != null)
                            {
                                InfrastructureUpgrade upgrade = new InfrastructureUpgrade();

                                // Parse slot number
                                if(int.TryParse(parts[0], out int slotNum))
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
                                if(parts.Length >= 4 && int.TryParse(parts[parts.Length - 2], out level))
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
                                for(int i = 1; i < levelIndex; i++)
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
            catch(Exception)
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

                foreach(System sys in Systems)
                {
                    if(sys.InfrastructureUpgrades.Count > 0)
                    {
                        lines.Add(sys.Name);

                        foreach(InfrastructureUpgrade upgrade in sys.InfrastructureUpgrades.OrderBy(u => u.SlotNumber))
                        {
                            lines.Add(upgrade.ToString());
                        }

                        lines.Add(""); // Empty line between systems
                    }
                }

                File.WriteAllLines(filePath, lines);
            }
            catch(Exception)
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
            if(sys != null)
            {
                // Check if upgrade already exists in this slot
                InfrastructureUpgrade existing = sys.InfrastructureUpgrades.FirstOrDefault(u => u.SlotNumber == slotNumber);

                if(existing != null)
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
            if(sys != null)
            {
                InfrastructureUpgrade upgrade = sys.InfrastructureUpgrades.FirstOrDefault(u => u.SlotNumber == slotNumber);
                if(upgrade != null)
                {
                    sys.InfrastructureUpgrades.Remove(upgrade);
                }
            }
        }
    }
}
