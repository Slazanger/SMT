using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;


namespace SMT.EVEData
{

    //jumpclones


    public class LocalCharacter : Character, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The name of the system this character is currently in 
        /// </summary>
        private string location;

        [XmlIgnoreAttribute]
        public object ActiveRouteLock;



        /// <summary>
        /// Does the route need updating
        /// </summary>
        private bool routeNeedsUpdate = false;


        private bool esiRouteNeedsUpdate = false;

        /// <summary>
        /// Gets or sets the The location of the local file bound to this session's "Local" chat channel
        /// </summary>
        public string LocalChatFile { get; set; }

        /// <summary>
        /// Gets or sets the location of the character
        /// </summary>
        public string Location
        {
            get
            {
                return location;
            }

            set
            {
                if (location == value)
                {
                    return;
                }

                location = value;
                routeNeedsUpdate = true;
            }
        }

        public string Region { get; set; }


        /// <summary>
        /// Gets or sets if this character is linked with ESI
        /// </summary>
        public bool ESILinked { get; set; }

        /// <summary>
        /// Gets or sets the ESI auth code
        /// </summary>
        public string ESIAuthCode { get; set; }

        /// <summary>
        /// Gets or sets the ESI access token
        /// </summary>
        [XmlIgnoreAttribute]
        public string ESIAccessToken { get; set; }

        /// <summary>
        /// Gets or sets the ESI access token expiry time
        /// </summary>
        public DateTime ESIAccessTokenExpiry { get; set; }

        /// <summary>
        /// Gets or sets the ESI refresh Token
        /// </summary>
        public string ESIRefreshToken { get; set; }

        [XmlIgnoreAttribute]
        public ESI.NET.Models.SSO.AuthorizedCharacterData ESIAuthData { get; set; }

        /// <summary>
        /// Gets or sets the character standings dictionary
        /// </summary>
        [XmlIgnoreAttribute]
        public Dictionary<long, float> Standings { get; set; }

        [XmlIgnoreAttribute]
        public Dictionary<long, long> LabelMap { get; set; }

        [XmlIgnoreAttribute]
        public Dictionary<long, string> LabelNames { get; set; }


        public SerializableDictionary<String, ObservableCollection<Structure>> KnownStructures { get; set; }


        /// <summary>
        /// Gets or sets the current fleet info for this character
        /// </summary>
        [XmlIgnoreAttribute]
        public Fleet FleetInfo { get; set; }



        private bool m_UseAnsiblexGates;
        public bool UseAnsiblexGates
        {
            get
            {
                return m_UseAnsiblexGates;
            }
            set
            {
                if (m_UseAnsiblexGates == value)
                {
                    return;
                }

                m_UseAnsiblexGates = value;
                routeNeedsUpdate = true;
                esiRouteNeedsUpdate = true;
                OnPropertyChanged("UseAnsiblexGates");
            }
        }


        private RoutingMode m_NavigationMode;

        public RoutingMode NavigationMode
        {
            get
            {
                return m_NavigationMode;
            }
            set
            {
                if (m_NavigationMode == value)
                {
                    return;
                }


                m_NavigationMode = value;
                routeNeedsUpdate = true;
            }
        }

        /// <summary>
        /// Gets or sets the current list of Waypoints
        /// </summary>
        [XmlIgnoreAttribute]
        public ObservableCollection<string> Waypoints { get; set; }

        /// <summary>
        /// Gets or sets the current active route
        /// </summary>
        [XmlIgnoreAttribute]
        public ObservableCollection<Navigation.RoutePoint> ActiveRoute { get; set; }

        /// <summary>
        /// Gets or sets the character structure dictionary
        /// </summary>
        [XmlIgnoreAttribute]
        public Dictionary<string, List<StructureIDs.StructureIdData>> DockableStructures { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="Character" /> class
        /// </summary>
        public LocalCharacter()
        {
            Standings = new Dictionary<long, float>();

            LabelMap = new Dictionary<long, long>();
            LabelNames = new Dictionary<long, string>();

            CorporationID = -1;
            AllianceID = -1;
            FleetInfo = new Fleet();
            Waypoints = new ObservableCollection<string>();
            ActiveRoute = new ObservableCollection<Navigation.RoutePoint>();
            DockableStructures = new Dictionary<string, List<StructureIDs.StructureIdData>>();

            UseAnsiblexGates = true;

            ActiveRouteLock = new object();

            KnownStructures = new SerializableDictionary<string, ObservableCollection<Structure>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Character" /> class.
        /// </summary>
        /// <param name="name">Name of Character</param>
        /// <param name="lcf">Local Chat File Location</param>
        /// <param name="location">Current Location of Character</param>
        public LocalCharacter(string name, string lcf, string location)
        {
            Name = name;
            LocalChatFile = lcf;
            Location = location;

            UseAnsiblexGates = true;

            ESILinked = false;
            ESIAuthCode = string.Empty;
            ESIAccessToken = string.Empty;
            ESIRefreshToken = string.Empty;

            Standings = new Dictionary<long, float>();

            LabelMap = new Dictionary<long, long>();
            LabelNames = new Dictionary<long, string>();

            FleetInfo = new Fleet();

            Waypoints = new ObservableCollection<string>();
            ActiveRoute = new ObservableCollection<Navigation.RoutePoint>();

            ActiveRouteLock = new object();
        }


        /// <summary>
        /// To String
        /// </summary>
        public override string ToString()
        {
            string toStr = Name;
            if (ESILinked)
            {
                toStr += " (ESI)";
            }

            return toStr;
        }


        /// <summary>
        /// Update the Character info
        /// </summary>
        public void Update()
        {
            TimeSpan ts = ESIAccessTokenExpiry - DateTime.Now;
            if (ts.Minutes < 1)
            {
                RefreshAccessToken().Wait();
                UpdateInfoFromESI().Wait();
            }

            UpdatePositionFromESI().Wait();
            //UpdateFleetInfo();

            if (routeNeedsUpdate)
            {
                routeNeedsUpdate = false;
                UpdateActiveRoute();
            }
        }

        /// <summary>
        /// Add Destination to the route
        /// </summary>
        /// <param name="systemID">System to set destination to</param>
        /// <param name="clear">Clear all waypoints before setting?</param>
        public void AddDestination(long systemID, bool clear)
        {
            lock (ActiveRouteLock)
            {
                if (clear)
                {
                    Waypoints.Clear();
                    ActiveRoute.Clear();
                }

            }

            Waypoints.Add(EveManager.Instance.SystemIDToName[systemID]);

            routeNeedsUpdate = true;
            esiRouteNeedsUpdate = true;
        }

        /// <summary>
        /// Update the active route for the character
        /// </summary>
        private async void UpdateActiveRoute()
        {
            if (Waypoints.Count == 0)
            {
                return;
            }


            lock (ActiveRouteLock)
            {
                // new routing

                string start = string.Empty;
                string end = Location;

                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    if (Location == Waypoints[0])
                    {
                        Waypoints.RemoveAt(0);
                    }

                    ActiveRoute.Clear();
                }), DispatcherPriority.ApplicationIdle);

                // loop through all the waypoints and query ESI for the route
                for (int i = 0; i < Waypoints.Count; i++)
                {
                    start = end;
                    end = Waypoints[i];

                    List<Navigation.RoutePoint> sysList = Navigation.Navigate(start, end, UseAnsiblexGates, NavigationMode);

                    if (sysList != null)
                    {

                        foreach (Navigation.RoutePoint s in sysList)
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                ActiveRoute.Add(s);
                            }), DispatcherPriority.ContextIdle, null);
                        }
                    }
                }

            }

            if (esiRouteNeedsUpdate)
            {

                esiRouteNeedsUpdate = false;

                List<long> WayPointsToAdd = new List<long>();


                lock (ActiveRouteLock)
                {
                    foreach (Navigation.RoutePoint rp in ActiveRoute)
                    {
                        // explicitly add interim waypoints for ansiblex gates or actual waypoints
                        if (rp.GateToTake == Navigation.GateType.Ansibex || Waypoints.Contains(rp.SystemName))
                        {
                            long wayPointSysID = EveManager.Instance.GetEveSystem(rp.SystemName).ID;

                            if (rp.GateToTake == Navigation.GateType.Ansibex)
                            {
                                foreach (JumpBridge jb in EveManager.Instance.JumpBridges)
                                {
                                    if (jb.From == rp.SystemName)
                                    {
                                        if (jb.FromID != 0)
                                        {
                                            wayPointSysID = jb.FromID;
                                        }
                                        break;
                                    }

                                    if (jb.To == rp.SystemName)
                                    {
                                        if (jb.ToID != 0)
                                        {
                                            wayPointSysID = jb.ToID;
                                        }
                                        break;
                                    }

                                }
                            }
                            WayPointsToAdd.Add(wayPointSysID);
                        }
                    }
                }

                bool firstRoute = true;


                foreach (long SysID in WayPointsToAdd)
                {
                    ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
                    esiClient.SetCharacterData(ESIAuthData);

                    ESI.NET.EsiResponse<string> esr = await esiClient.UserInterface.Waypoint(SysID, false, firstRoute);
                    if (EVEData.ESIHelpers.ValidateESICall<string>(esr))
                    {
                        //                        routeNeedsUpdate = true;
                    }
                    firstRoute = false;

                    //Thread.Sleep(50);
                }
            }
        }

        /// <summary>
        /// Refresh the ESI access token
        /// </summary>
        private async Task RefreshAccessToken()
        {
            if (String.IsNullOrEmpty(ESIRefreshToken) || !ESILinked)
            {
                return;
            }

            try
            {
                SsoToken sst;
                AuthorizedCharacterData acd;
                sst = await EveManager.Instance.ESIClient.SSO.GetToken(GrantType.RefreshToken, ESIRefreshToken);
                if (sst == null || sst.RefreshToken == null)
                {
                    return;
                }

                acd = await EveManager.Instance.ESIClient.SSO.Verify(sst);

                if (String.IsNullOrEmpty(acd.Token))
                {
                    return;
                }

                ESIAccessToken = acd.Token;
                ESIAccessTokenExpiry = acd.ExpiresOn.ToLocalTime();
                ESIRefreshToken = acd.RefreshToken;
                ESILinked = true;
                ESIAuthData = acd;

            }

            catch { }
        }

        /// <summary>
        /// Update the characters position from ESI (will override the position read from any log files
        /// </summary>
        private async Task UpdatePositionFromESI()
        {
            if (ID == 0 || !ESILinked || ESIAuthData == null)
            {
                return;
            }

            try
            {
                ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
                esiClient.SetCharacterData(ESIAuthData);
                ESI.NET.EsiResponse<ESI.NET.Models.Location.Location> esr = await esiClient.Location.Location();

                if (ESIHelpers.ValidateESICall<ESI.NET.Models.Location.Location>(esr))
                {
                    if (!EveManager.Instance.SystemIDToName.Keys.Contains(esr.Data.SolarSystemId))
                    {
                        return;
                    }
                    Location = EveManager.Instance.SystemIDToName[esr.Data.SolarSystemId];
                    System s = EVEData.EveManager.Instance.GetEveSystem(Location);
                    if (s != null)
                    {
                        Region = s.Region;
                    }
                    else
                    {
                        Region = "";
                    }
                }
            }
            catch { }
        }


        /// <summary>
        /// Update the characters FleetInfo
        /// </summary>
        private async void UpdateFleetInfo()
        {
            if (ID == 0 || !ESILinked || ESIAuthData == null)
            {
                return;
            }

            try
            {
                ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
                esiClient.SetCharacterData(ESIAuthData);

                ESI.NET.EsiResponse<ESI.NET.Models.Fleets.FleetInfo> esr = await esiClient.Fleets.FleetInfo();

                if (ESIHelpers.ValidateESICall<ESI.NET.Models.Fleets.FleetInfo>(esr))
                {
                    FleetInfo.FleetID = esr.Data.FleetId;

                    // in fleet, extract info
                    ESI.NET.EsiResponse<List<ESI.NET.Models.Fleets.Member>> esrf = await esiClient.Fleets.Members(esr.Data.FleetId);
                    if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Fleets.Member>>(esrf))
                    {
                        foreach (ESI.NET.Models.Fleets.Member fm in esrf.Data)
                        {
                            //fm.CharacterId
                        }



                    }
                }
                else
                {
                    FleetInfo.FleetID = 0;
                    FleetInfo.Members.Clear();
                }
            }
            catch { }
        }


        /// <summary>
        /// Update the character info from the ESI data if linked
        /// </summary>
        private async Task UpdateInfoFromESI()
        {
            if (ID == 0 || !ESILinked || ESIAuthData == null)
            {
                return;
            }

            List<long> AllianceToResolve = new List<long>();

            try
            {
                ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
                esiClient.SetCharacterData(ESIAuthData);


                //if (CorporationID == -1 || AllianceID == -1)
                {
                    ESI.NET.EsiResponse<ESI.NET.Models.Character.Information> esr = await esiClient.Character.Information((int)ID);

                    if (EVEData.ESIHelpers.ValidateESICall<ESI.NET.Models.Character.Information>(esr))
                    {
                        CorporationID = esr.Data.CorporationId;
                        AllianceID = esr.Data.AllianceId;
                    }
                }


                // if we have an alliance, and no current standings set
                if (AllianceID != 0 && Standings.Count == 0)
                {
                    int page = 0;
                    int maxPageCount = 1;
                    do
                    {
                        page++;


                        // SJS here.. list modifeied exception

                        esiClient.SetCharacterData(ESIAuthData);
                        ESI.NET.EsiResponse<List<ESI.NET.Models.Contacts.Contact>> esr = await esiClient.Contacts.ListForAlliance(page);

                        if (EVEData.ESIHelpers.ValidateESICall<List<ESI.NET.Models.Contacts.Contact>>(esr))
                        {
                            if (esr.Pages.HasValue)
                            {
                                maxPageCount = (int)esr.Pages;
                            }

                            foreach (ESI.NET.Models.Contacts.Contact con in esr.Data)
                            {
                                Standings[con.ContactId] = (float)con.Standing;
                                LabelMap[con.ContactId] = con.LabelId;

                                if (con.ContactType == "alliance")
                                {
                                    AllianceToResolve.Add(con.ContactId);
                                }
                            }
                        }
                    }
                    while (page < maxPageCount);
                }
            }
            catch (Exception)
            {
            }

            await EveManager.Instance.ResolveAllianceIDs(AllianceToResolve);
        }


        public async Task<List<JumpBridge>> FindJumpGates(string JumpBridgeFilterString = " » ")
        {


            List<JumpBridge> jbl = new List<JumpBridge>();

            if (!ESILinked)
                return jbl;

            ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
            esiClient.SetCharacterData(ESIAuthData);


            Dictionary<long, ESI.NET.Models.Universe.Structure> SystemJumpGateList = new Dictionary<long, ESI.NET.Models.Universe.Structure>();

            esiClient.SetCharacterData(ESIAuthData);
            ESI.NET.EsiResponse<ESI.NET.Models.SearchResults> esr = await esiClient.Search.Query(SearchType.Character, JumpBridgeFilterString, SearchCategory.Structure);
            if (EVEData.ESIHelpers.ValidateESICall<ESI.NET.Models.SearchResults>(esr))
            {
                if (esr.Data.Structures == null)
                {
                    return jbl;
                }


                foreach (long stationID in esr.Data.Structures)
                {
                    ESI.NET.EsiResponse<ESI.NET.Models.Universe.Structure> esrs = await esiClient.Universe.Structure(stationID);

                    if (EVEData.ESIHelpers.ValidateESICall<ESI.NET.Models.Universe.Structure>(esrs))
                    {
                        SystemJumpGateList[stationID] = esrs.Data;

                        // found a jump gate
                        if (esrs.Data.TypeId == 35841)
                        {
                            string[] parts = esrs.Data.Name.Split(' ');
                            string from = parts[0];
                            string to = parts[2];

                            EveManager.Instance.AddUpdateJumpBridge(from, to, stationID);
                        }
                    }

                    Thread.Sleep(10);
                }
            }

            return jbl;
        }


        public async void UpdateStructureInfoForRegion2(string Region)
        {
            if (!ESILinked)
                return;


            MapRegion mr = EveManager.Instance.GetRegion(Region);
            // somethings gone wrong
            if (mr == null)
            {
                return;
            }


            ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
            esiClient.SetCharacterData(ESIAuthData);


            Dictionary<long, ESI.NET.Models.Universe.Structure> SystemStructureList = new Dictionary<long, ESI.NET.Models.Universe.Structure>();


            // iterate over each structure and search for structres containing the text for each system
            foreach (MapSystem ms in mr.MapSystems.Values.ToList())
            {

                if (ms.OutOfRegion)
                {
                    continue;
                }


                UriBuilder urlBuilder = new UriBuilder(@"https://esi.evetech.net/latest/characters/" + ID + "/search/");


                esiClient.SetCharacterData(ESIAuthData);
                ESI.NET.EsiResponse<ESI.NET.Models.SearchResults> esr = await esiClient.Search.Query(SearchType.Character, ms.Name, SearchCategory.Structure);
                if (EVEData.ESIHelpers.ValidateESICall<ESI.NET.Models.SearchResults>(esr))
                {
                    if (esr.Data.Structures == null)
                    {
                        return;
                    }


                    foreach (long stationID in esr.Data.Structures)
                    {
                        ESI.NET.EsiResponse<ESI.NET.Models.Universe.Structure> esrs = await esiClient.Universe.Structure(stationID);

                        if (EVEData.ESIHelpers.ValidateESICall<ESI.NET.Models.Universe.Structure>(esrs))
                        {
                            SystemStructureList[stationID] = esrs.Data;
                        }

                        Thread.Sleep(20);
                    }
                }

                Thread.Sleep(100);

                //ssssss

            }



            string CSVPath = AppDomain.CurrentDomain.BaseDirectory + "\\Strucutres_" + mr.Name + "_" + ID + ".csv";

            using (var w = new StreamWriter(CSVPath, false))
            {
                string Header = "SolarSystem ID,StructureID,Type ID,Name,Owner";
                w.WriteLine(Header);


                foreach (long ID in SystemStructureList.Keys)
                {
                    ESI.NET.Models.Universe.Structure s = SystemStructureList[ID];
                    string Line = $"{s.SolarSystemId},{ID},{s.TypeId},{s.Name}";
                    w.WriteLine(Line);
                    w.Flush();
                }
            }
        }


        public async void GetStructureInfoForSystem(string system)
        {
            if (!ESILinked)
                return;


            MapRegion mr = EveManager.Instance.GetRegion(Region);
            // somethings gone wrong
            if (mr == null)
            {
                return;
            }

            ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
            esiClient.SetCharacterData(ESIAuthData);


            Dictionary<long, ESI.NET.Models.Universe.Structure> SystemStructureList = new Dictionary<long, ESI.NET.Models.Universe.Structure>();


            esiClient.SetCharacterData(ESIAuthData);
            ESI.NET.EsiResponse<ESI.NET.Models.SearchResults> esr = await esiClient.Search.Query(SearchType.Character, system, SearchCategory.Structure);
            if (EVEData.ESIHelpers.ValidateESICall<ESI.NET.Models.SearchResults>(esr))
            {
                if (esr.Data.Structures == null)
                {
                    return;
                }


                foreach (long stationID in esr.Data.Structures)
                {
                    ESI.NET.EsiResponse<ESI.NET.Models.Universe.Structure> esrs = await esiClient.Universe.Structure(stationID);

                    if (EVEData.ESIHelpers.ValidateESICall<ESI.NET.Models.Universe.Structure>(esrs))
                    {
                        SystemStructureList[stationID] = esrs.Data;
                    }

                    Thread.Sleep(20);
                }
            }

            Thread.Sleep(100);



        }


        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

    }
}
