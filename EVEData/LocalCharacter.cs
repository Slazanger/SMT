using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        [XmlIgnoreAttribute]
        public object ActiveRouteLock;

        [XmlIgnoreAttribute]
        public SemaphoreSlim UpdateLock;

        public static readonly string SaveVersion = "02";

        [XmlIgnoreAttribute]
        public bool warningSystemsNeedsUpdate = false;

        private bool esiRouteNeedsUpdate = false;

        /// <summary>
        /// The name of the system this character is currently in
        /// </summary>
        private string location;

        private RoutingMode m_NavigationMode;

        private bool m_UseAnsiblexGates;

        /// <summary>
        /// Does the route need updating
        /// </summary>
        private bool routeNeedsUpdate = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Character" /> class
        /// </summary>
        public LocalCharacter()
        {
            UseAnsiblexGates = true;

            ESILinked = false;
            ESIAuthCode = string.Empty;
            ESIAccessToken = string.Empty;
            ESIRefreshToken = string.Empty;

            Standings = new Dictionary<long, float>();

            LabelMap = new Dictionary<long, long>();
            LabelNames = new Dictionary<long, string>();


            FleetInfo = new Fleet();

            FleetInfo.IsFleetBoss = false;
            FleetInfo.FleetID = 0;

            // Random Offset to stop all the errors hitting at once
            Random R = new Random();
            int randomOffset = R.Next(90);
            FleetInfo.NextFleetMembershipCheck = DateTime.Now + TimeSpan.FromSeconds(randomOffset);


            Waypoints = new ObservableCollection<string>();
            ActiveRoute = new ObservableCollection<Navigation.RoutePoint>();

            ActiveRouteLock = new object();
            UpdateLock = new SemaphoreSlim(1);

            CorporationID = -1;
            AllianceID = -1;
            DockableStructures = new Dictionary<string, List<StructureIDs.StructureIdData>>();

            UseAnsiblexGates = true;


            KnownStructures = new SerializableDictionary<string, ObservableCollection<Structure>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Character" /> class.
        /// </summary>
        /// <param name="name">Name of Character</param>
        /// <param name="lcf">Local Chat File Location</param>
        /// <param name="location">Current Location of Character</param>
        public LocalCharacter(string name, string lcf, string location)
            : this()
        {
            Name = name;
            LocalChatFile = lcf;
            Location = location;
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
        /// Gets or sets the ESI access token
        /// </summary>
        [XmlIgnoreAttribute]
        public string ESIAccessToken { get; set; }

        /// <summary>
        /// Gets or sets the ESI access token expiry time
        /// </summary>
        public DateTime ESIAccessTokenExpiry { get; set; }

        /// <summary>
        /// Gets or sets the ESI auth code
        /// </summary>
        public string ESIAuthCode { get; set; }

        [XmlIgnoreAttribute]
        public ESI.NET.Models.SSO.AuthorizedCharacterData ESIAuthData { get; set; }

        /// <summary>
        /// Gets or sets if this character is linked with ESI
        /// </summary>
        public bool ESILinked { get; set; }

        /// <summary>
        /// Gets or sets the ESI refresh Token
        /// </summary>
        public string ESIRefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the current fleet info for this character
        /// </summary>
        [XmlIgnoreAttribute]
        public Fleet FleetInfo { get; set; }

        public SerializableDictionary<String, ObservableCollection<Structure>> KnownStructures { get; set; }

        [XmlIgnoreAttribute]
        public Dictionary<long, long> LabelMap { get; set; }

        [XmlIgnoreAttribute]
        public Dictionary<long, string> LabelNames { get; set; }

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
                warningSystemsNeedsUpdate = true;
            }
        }

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

        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the character standings dictionary
        /// </summary>
        [XmlIgnoreAttribute]
        public Dictionary<long, float> Standings { get; set; }

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

        public bool DeepSearchEnabled { get; set; }

        public int WarningSystemRange { get; set; }

        public List<string> WarningSystems { get; set; }

        /// <summary>
        /// Gets or sets the current list of Waypoints
        /// </summary>
        [XmlIgnoreAttribute]
        public ObservableCollection<string> Waypoints { get; set; }

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

        public async Task<List<JumpBridge>> FindJumpGates(string JumpBridgeFilterString = " » ")
        {
            List<JumpBridge> jbl = new List<JumpBridge>();

            if (!ESILinked)
                return jbl;

            await UpdateLock.WaitAsync();
            {
                ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
                esiClient.SetCharacterData(ESIAuthData);

                Dictionary<long, ESI.NET.Models.Universe.Structure> SystemJumpGateList = new Dictionary<long, ESI.NET.Models.Universe.Structure>();

                esiClient.SetCharacterData(ESIAuthData);

                try
                {
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

                            Thread.Sleep(100);
                        }
                    }
                }
                catch
                {
                    // ESI-Search failed
                }

            }
            UpdateLock.Release();


            return jbl;
        }

        public void RecalcRoute()
        {
            routeNeedsUpdate = true;
            esiRouteNeedsUpdate = true;
        }

        public string GetWayPointText()
        {
            string ClipboardText = "Waypoints\n==============\n";

            lock (ActiveRouteLock)
            {
                foreach (Navigation.RoutePoint rp in ActiveRoute)
                {
                    string WayPointText = string.Empty;
                    long wayPointSysID = EveManager.Instance.GetEveSystem(rp.SystemName).ID;
                    // explicitly add interim waypoints for ansiblex gates or actual waypoints
                    if (rp.GateToTake == Navigation.GateType.Ansibex)
                    {
                        bool isSystemLink = true;

                        if (rp.GateToTake == Navigation.GateType.Ansibex)
                        {
                            string GateDesto = string.Empty;

                            foreach (JumpBridge jb in EveManager.Instance.JumpBridges)
                            {
                                if (jb.From == rp.SystemName)
                                {
                                    if (jb.FromID != 0)
                                    {
                                        wayPointSysID = jb.FromID;
                                        isSystemLink = false;
                                    }

                                    GateDesto = jb.To;
                                    break;
                                }

                                if (jb.To == rp.SystemName)
                                {
                                    if (jb.ToID != 0)
                                    {
                                        wayPointSysID = jb.ToID;
                                        isSystemLink = false;
                                    }

                                    GateDesto = jb.From;
                                    break;
                                }
                            }

                            if (isSystemLink)
                            {
                                WayPointText = "Ansiblex: <url=showinfo:5//" + wayPointSysID + ">" + rp.SystemName + " » " + GateDesto + " </url>\n";
                            }
                            else
                            {
                                WayPointText = "Ansiblex: <url=showinfo:35841//" + wayPointSysID + ">" + rp.SystemName + " » " + GateDesto + "</url>\n";
                            }
                        }
                    }

                    if (Waypoints.Contains(rp.SystemName))
                    {
                        // regular waypoint
                        wayPointSysID = EveManager.Instance.GetEveSystem(rp.SystemName).ID;

                        WayPointText = "<url=showinfo:5//" + wayPointSysID + ">" + rp.SystemName + "</url>\n";
                    }

                    ClipboardText += WayPointText;
                }
            }

            return ClipboardText;
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
        public async Task Update()
        {
            await UpdateLock.WaitAsync();
            {
                TimeSpan ts = ESIAccessTokenExpiry - DateTime.Now;
                if (ts.Minutes < 1)
                {
                    RefreshAccessToken().Wait();
                    UpdateInfoFromESI().Wait();
                }

                if(EveManager.Instance.UseESIForCharacterPositions)
                {
                    UpdatePositionFromESI().Wait();
                }


                UpdateFleetInfo().Wait();

                if (routeNeedsUpdate)
                {
                    routeNeedsUpdate = false;
                    UpdateActiveRoute();
                }

                if (warningSystemsNeedsUpdate)
                {
                    warningSystemsNeedsUpdate = false;
                    UpdateWarningSystems();
                }
            }
            UpdateLock.Release();
        }



        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
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
                sst = await EveManager.Instance.ESIClient.SSO.GetTokenV2(GrantType.RefreshToken, ESIRefreshToken, string.Empty, null);
                if (sst == null || sst.RefreshToken == null)
                {
                    // we have a valid refresh token BUT it failed to auth; we need to force 
                    // a reauth
                    ESIRefreshToken = "";
                    ESILinked = false;

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
        /// Update the active route for the character
        /// </summary>
        private async void UpdateActiveRoute()
        {
            if (Waypoints.Count == 0)
            {
                return;
            }


            {
                // new routing

                string start = string.Empty;
                string end = Location;

                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    lock (ActiveRouteLock)
                    {
                        if (Location == Waypoints[0])
                        {
                            Waypoints.RemoveAt(0);
                        }
                    }

                    ActiveRoute.Clear();
                }), DispatcherPriority.Normal);

                // loop through all the waypoints 
                for (int i = 0; i < Waypoints.Count; i++)
                {
                    start = end;
                    end = Waypoints[i];

                    List<Navigation.RoutePoint> sysList = Navigation.Navigate(start, end, UseAnsiblexGates, false, NavigationMode);

                    if (sysList != null)
                    {

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            lock (ActiveRouteLock)
                            {
                                foreach (Navigation.RoutePoint s in sysList)
                                {
                                    ActiveRoute.Add(s);
                                }
                            }
                        }), DispatcherPriority.Normal, null);

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

                    Thread.Sleep(50);
                }
            }
        }

        /// <summary>
        /// Update the characters FleetInfo
        /// </summary>
        private async Task UpdateFleetInfo()
        {
            if (ID == 0 || !ESILinked || ESIAuthData == null)
            {
                return;
            }

            try
            {
                ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
                esiClient.SetCharacterData(ESIAuthData);


                if (FleetInfo.NextFleetMembershipCheck < DateTime.Now)
                {
                    // route is cached for 60s, however checking this can hit the rate limit
                    FleetInfo.NextFleetMembershipCheck = DateTime.Now + TimeSpan.FromSeconds(240);

                    ESI.NET.EsiResponse<ESI.NET.Models.Fleets.FleetInfo> esr = await esiClient.Fleets.FleetInfo();

                    if (ESIHelpers.ValidateESICall<ESI.NET.Models.Fleets.FleetInfo>(esr))
                    {
                        FleetInfo.FleetID = esr.Data.FleetId;
                        FleetInfo.IsFleetBoss = esr.Data.Role == "fleet_commander" ? true : false;
                    }
                    else
                    {
                        FleetInfo.FleetID = 0;

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        { 
                            FleetInfo.Members.Clear();
                        }), DispatcherPriority.Normal);

                    }
                }

                if(FleetInfo.FleetID != 0 && FleetInfo.IsFleetBoss)
                {
                    List<long> characterIDsToResolve = new List<long>();

                    ESI.NET.EsiResponse<List<ESI.NET.Models.Fleets.Member>> esrf = await esiClient.Fleets.Members(FleetInfo.FleetID);
                    if (ESIHelpers.ValidateESICall<List<ESI.NET.Models.Fleets.Member>>(esrf))
                    {
                        foreach(Fleet.FleetMember ff in FleetInfo.Members)
                        {
                            ff.IsValid = false;
                        }

                        foreach (ESI.NET.Models.Fleets.Member esifm in esrf.Data)
                        {

                            Fleet.FleetMember fm = null;

                            foreach (Fleet.FleetMember ff in FleetInfo.Members)
                            {
                                if(ff.CharacterID == esifm.CharacterId)
                                {
                                    fm = ff;
                                    fm.IsValid = true;
                                }
                            }

                            if(fm == null)
                            {
                                fm = new Fleet.FleetMember();
                                fm.IsValid = true;

                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                {
                                    FleetInfo.Members.Add(fm);
                                }), DispatcherPriority.Normal);

                            }

                            fm.Name = EveManager.Instance.GetCharacterName(esifm.CharacterId);
                            fm.Location = EveManager.Instance.GetEveSystemNameFromID(esifm.SolarSystemId);
                            fm.CharacterID = esifm.CharacterId;
                            if (EveManager.Instance.ShipTypes.ContainsKey(esifm.ShipTypeId.ToString()))
                            {
                                fm.ShipType = EveManager.Instance.ShipTypes[esifm.ShipTypeId.ToString()];
                            }
                            else
                            {
                                fm.ShipType = "Unknown : " + esifm.ShipTypeId.ToString();
                            }

                            if (String.IsNullOrEmpty(fm.Name))
                            {
                                characterIDsToResolve.Add(esifm.CharacterId);
                            }
                        }

                        if (characterIDsToResolve.Count > 0)
                        {
                            EveManager.Instance.ResolveCharacterIDs(characterIDsToResolve).Wait();
                        }

                        foreach (Fleet.FleetMember ff in FleetInfo.Members.ToList())
                        {
                            if (!ff.IsValid)
                            {
                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                {
                                    FleetInfo.Members.Remove(ff);
                                }), DispatcherPriority.Normal);
                            }
                        }
                    }
                    else
                    {
                        // something went wrong (probably lost fleet_commander), reset this check
                        FleetInfo.NextFleetMembershipCheck = DateTime.Now + TimeSpan.FromSeconds(60);
                        FleetInfo.FleetID = 0;

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            FleetInfo.Members.Clear();
                        }), DispatcherPriority.Normal);
                    }

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
                if(ESILinked)
                {
                    ESIAccessTokenExpiry = DateTime.Now;
                }
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

                            if (esr.Data == null)
                            {
                                // in an alliance with no contacts
                                continue;
                            }


                            foreach (ESI.NET.Models.Contacts.Contact con in esr.Data)
                            {
                                Standings[con.ContactId] = (float)con.Standing;
                                // Removed LabelMap[con.ContactId] = con.LabelIds;

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
                        Location = "";
                        Region = "";
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

        private void UpdateWarningSystems()
        {
            if (!string.IsNullOrEmpty(Location) && WarningSystemRange > 0)
            {
                WarningSystems = Navigation.GetSystemsXJumpsFrom(new List<string>(), Location, WarningSystemRange);
            }
        }
    }
}