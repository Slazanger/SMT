using System.ComponentModel;
using System.Xml.Serialization;
using EVEStandard.Models;
using EVEStandard.Models.API;
using EVEStandard.Models.SSO;

namespace EVEData;
//jumpclones

public class LocalCharacter : Character, INotifyPropertyChanged
{
    public static readonly string SaveVersion = "03";

    [XmlIgnore] public object ActiveRouteLock;

    [XmlIgnore] public SemaphoreSlim UpdateLock;

    [XmlIgnore] public bool warningSystemsNeedsUpdate;

    /// <summary>
    /// Ship Decloak Event Handler
    /// </summary>
    public delegate void RouteUpdatedEventHandler();

    /// <summary>
    /// Ship Decloaked
    /// </summary>
    public event RouteUpdatedEventHandler RouteUpdatedEvent;

    private bool esiRouteNeedsUpdate;

    private bool esiRouteUpdating;

    private bool esiSendRouteClear;

    /// <summary>
    /// The name of the system this character is currently in
    /// </summary>
    private string location;

    private RoutingMode m_NavigationMode;

    private bool m_UseAnsiblexGates;

    private bool m_UseTheraRouting;

    private bool m_UseZarzakhRouting;

    private bool m_UseTurnurRouting;

    private bool m_isOnline;

    private bool m_ObservatoryDecloakWarningEnabled;

    private bool m_GateDecloakWarningEnabled;

    private bool m_DecloakWarningEnabled;

    private bool m_CombatWarningEnabled;

    private bool routeNeedsUpdate = false;

    private int ssoErrorCount = 0;

    private int m_activeRouteLength = 0;

    private bool m_updateTick = true;

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
        var R = new Random();
        var randomOffset = R.Next(90);
        FleetInfo.NextFleetMembershipCheck = DateTime.Now + TimeSpan.FromSeconds(randomOffset);

        Waypoints = new List<string>();
        ActiveRoute = new List<Navigation.RoutePoint>();

        ActiveRouteLock = new object();
        UpdateLock = new SemaphoreSlim(1);

        CorporationID = -1;
        CorporationName = null;
        CorporationTicker = null;
        AllianceID = -1;
        AllianceName = null;
        AllianceTicker = null;

        DangerZoneRange = 5;

        UseAnsiblexGates = true;

        IsOnline = true;
        CombatWarningEnabled = true;
        ObservatoryDecloakWarningEnabled = true;
        DecloakWarningEnabled = true;
        GateDecloakWarningEnabled = true;
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
        IsOnline = true;

        CombatWarningEnabled = false;
        ObservatoryDecloakWarningEnabled = true;
        DecloakWarningEnabled = true;
        GateDecloakWarningEnabled = true;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets or sets the current active route
    /// </summary>
    [XmlIgnore]
    public List<Navigation.RoutePoint> ActiveRoute { get; set; }

    public int ActiveRouteLength
    {
        get => m_activeRouteLength;
        set
        {
            m_activeRouteLength = value;
            OnPropertyChanged("ActiveRouteLength");
        }
    }

    public bool DangerZoneActive { get; set; }
    public bool DeepSearchEnabled { get; set; }

    /// <summary>
    /// Gets or sets the ESI access token
    /// </summary>
    [XmlIgnore]
    public string ESIAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the ESI access token expiry time
    /// </summary>
    public DateTime ESIAccessTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets the ESI auth code
    /// </summary>
    public string ESIAuthCode { get; set; }

    /// <summary>
    /// Stored scopes string for building AuthDTO (space-separated from CharacterDetails.Scopes).
    /// </summary>
    public string ESIScopesStored { get; set; }

    /// <summary>
    /// Gets or sets if this character is linked with ESI
    /// </summary>
    public bool ESILinked { get; set; }

    /// <summary>
    /// Gets or sets the ESI refresh Token
    /// </summary>
    public string ESIRefreshToken { get; set; }

    /// <summary>
    /// Builds AuthDTO for EVEStandard API calls. Returns null if not ESI linked or token missing.
    /// </summary>
    public AuthDTO GetAuthDTO()
    {
        if (!ESILinked || ID == 0 || string.IsNullOrEmpty(ESIAccessToken))
            return null;
        var expiry = ESIAccessTokenExpiry.Kind == DateTimeKind.Utc
            ? ESIAccessTokenExpiry
            : ESIAccessTokenExpiry.ToUniversalTime();
        return new AuthDTO
        {
            CharacterId = ID,
            AccessToken = new AccessTokenDetails
            {
                AccessToken = ESIAccessToken,
                RefreshToken = ESIRefreshToken ?? string.Empty,
                ExpiresUtc = expiry
            },
            Scopes = ESIScopesStored ?? string.Empty
        };
    }

    /// <summary>
    /// Gets or sets the current fleet info for this character
    /// </summary>
    [XmlIgnore]
    public Fleet FleetInfo { get; set; }

    /// <summary>
    /// Fleet Updated Event Handler
    /// </summary>
    public delegate void FleetUpdatedHandler(LocalCharacter fleetOwner);

    /// <summary>
    /// Fleet Updated Events
    /// </summary>
    public event FleetUpdatedHandler FleetUpdatedEvent;

    public bool IsOnline
    {
        get => m_isOnline;
        set
        {
            m_isOnline = value;
            OnPropertyChanged("IsOnline");
        }
    }

    public bool ObservatoryDecloakWarningEnabled
    {
        get => m_ObservatoryDecloakWarningEnabled;
        set
        {
            m_ObservatoryDecloakWarningEnabled = value;
            OnPropertyChanged("ObservatoryDecloakWarningEnabled");
        }
    }

    public bool GateDecloakWarningEnabled
    {
        get => m_GateDecloakWarningEnabled;
        set
        {
            m_GateDecloakWarningEnabled = value;
            OnPropertyChanged("GateDecloakWarningEnabled");
        }
    }

    public bool DecloakWarningEnabled
    {
        get => m_DecloakWarningEnabled;
        set
        {
            m_DecloakWarningEnabled = value;
            OnPropertyChanged("DecloakWarningEnabled");
        }
    }

    public bool CombatWarningEnabled
    {
        get => m_CombatWarningEnabled;
        set
        {
            m_CombatWarningEnabled = value;
            OnPropertyChanged("CombatWarningEnabled");
        }
    }

    private string m_gameLogWarningText;

    [XmlIgnore]
    public string GameLogWarningText
    {
        get => m_gameLogWarningText;
        set
        {
            m_gameLogWarningText = value;
            if (string.IsNullOrEmpty(value))
                WarningState = "";
            else
                WarningState = "Warning";

            OnPropertyChanged("GameLogWarningText");
            OnPropertyChanged("WarningState");
        }
    }

    public string WarningState { get; set; }

    [XmlIgnore] public Dictionary<long, long> LabelMap { get; set; }

    [XmlIgnore] public Dictionary<long, string> LabelNames { get; set; }

    /// <summary>
    /// Gets or sets the The location of the local file bound to this session's "Local" chat channel
    /// </summary>
    public string LocalChatFile { get; set; }

    /// <summary>
    /// Gets or sets the location of the character
    /// </summary>
    public string Location
    {
        get => location;

        set
        {
            if (location == value) return;

            location = value;
            routeNeedsUpdate = true;
            warningSystemsNeedsUpdate = true;

            // clear the warning everytime the location updates
            GameLogWarningText = "";
            OnPropertyChanged("Location");
        }
    }

    public RoutingMode NavigationMode
    {
        get => m_NavigationMode;
        set
        {
            if (m_NavigationMode == value) return;

            m_NavigationMode = value;
            routeNeedsUpdate = true;
            OnPropertyChanged("NavigationMode");
        }
    }

    public string Region { get; set; }

    /// <summary>
    /// Gets or sets the character standings dictionary
    /// </summary>
    [XmlIgnore]
    public Dictionary<long, float> Standings { get; set; }

    public bool UseAnsiblexGates
    {
        get => m_UseAnsiblexGates;
        set
        {
            if (m_UseAnsiblexGates == value) return;

            m_UseAnsiblexGates = value;
            routeNeedsUpdate = true;
            esiRouteNeedsUpdate = true;
            OnPropertyChanged("UseAnsiblexGates");
        }
    }

    public bool UseTheraRouting
    {
        get => m_UseTheraRouting;
        set
        {
            if (m_UseTheraRouting == value) return;

            m_UseTheraRouting = value;
            routeNeedsUpdate = true;
            esiRouteNeedsUpdate = true;
            OnPropertyChanged("UseTheraRouting");
        }
    }

    public bool UseZarzakhRouting
    {
        get => m_UseZarzakhRouting;
        set
        {
            if (m_UseZarzakhRouting == value) return;

            m_UseZarzakhRouting = value;
            routeNeedsUpdate = true;
            esiRouteNeedsUpdate = true;
            OnPropertyChanged("UseZarzakhRouting");
        }
    }

    public bool UseTurnurRouting
    {
        get => m_UseTurnurRouting;
        set
        {
            if (m_UseTurnurRouting == value) return;

            m_UseTurnurRouting = value;
            routeNeedsUpdate = true;
            esiRouteNeedsUpdate = true;
            OnPropertyChanged("UseTurnurRouting");
        }
    }

    public int DangerZoneRange { get; set; }

    [XmlIgnore] public List<string> WarningSystems { get; set; }

    [XmlIgnore] public Uri PortraitLocation { get; set; }

    [XmlIgnore] public string AlertText { get; set; }

    [XmlIgnore] public bool EdenCommStandingsGood { get; set; }

    [XmlIgnore] public bool TrigStandingsGood { get; set; }

    [XmlIgnore] public List<string> Waypoints { get; set; }

    [XmlIgnore] public List<string> JumpWaypoints { get; set; }

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
                ActiveRouteLength = 0;
            }
        }

        Waypoints.Add(EveManager.Instance.SystemIDToName[systemID]);

        routeNeedsUpdate = true;
        esiRouteNeedsUpdate = true;
    }

    public void ClearAllWaypoints()
    {
        lock (ActiveRouteLock)
        {
            ActiveRoute.Clear();
            ActiveRouteLength = 0;
            Waypoints.Clear();
        }

        routeNeedsUpdate = true;
        esiSendRouteClear = true;
    }

    public async Task<List<JumpBridge>> FindJumpGates(string JumpBridgeFilterString = " » ")
    {
        var jbl = new List<JumpBridge>();

        if (!ESILinked)
            return jbl;

        await UpdateLock.WaitAsync();
        {
            var auth = GetAuthDTO();
            if (auth == null)
            {
                UpdateLock.Release();
                return jbl;
            }

            try
            {
                var esr = await EveManager.Instance.EveApiClient.Search.SearchCharacterAsync(auth,
                    new List<string> { "structure" }, JumpBridgeFilterString);
                if (!ESIHelpers.ValidateESICall(esr) || esr.Model == null)
                {
                    UpdateLock.Release();
                    return jbl;
                }

                var structureIds = esr.Model.Structure ?? new List<long>();
                foreach (var stationID in structureIds)
                {
                    var esrs = await EveManager.Instance.EveApiClient.Universe.GetStructureInfoAsync(auth, stationID);
                    if (ESIHelpers.ValidateESICall(esrs) && esrs.Model != null)
                        if (esrs.Model.TypeId == 35841)
                        {
                            var parts = (esrs.Model.Name ?? string.Empty).Split(' ');
                            if (parts.Length >= 3)
                            {
                                var from = parts[0];
                                var to = parts[2];
                                EveManager.Instance.AddUpdateJumpBridge(from, to, stationID);
                            }
                        }

                    Thread.Sleep(100);
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

    public string GetWayPointText()
    {
        var ClipboardText = "Waypoints\n==============\n";

        lock (ActiveRouteLock)
        {
            foreach (var rp in ActiveRoute)
            {
                var WayPointText = string.Empty;
                var wayPointSysID = EveManager.Instance.GetEveSystem(rp.SystemName).ID;
                // explicitly add interim waypoints for ansiblex gates or actual waypoints
                if (rp.GateToTake == Navigation.GateType.Ansiblex)
                {
                    var isSystemLink = true;

                    if (rp.GateToTake == Navigation.GateType.Ansiblex)
                    {
                        var GateDesto = string.Empty;

                        foreach (var jb in EveManager.Instance.JumpBridges)
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
                            WayPointText = "Ansiblex: <url=showinfo:5//" + wayPointSysID + ">" + rp.SystemName + " » " +
                                           GateDesto + " </url>\n";
                        else
                            WayPointText = "Ansiblex: <url=showinfo:35841//" + wayPointSysID + ">" + rp.SystemName +
                                           " » " + GateDesto + "</url>\n";
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

    public void RecalcRoute()
    {
        routeNeedsUpdate = true;
        esiRouteNeedsUpdate = true;
    }

    /// <summary>
    /// To String
    /// </summary>
    public override string ToString()
    {
        var toStr = Name;
        if (ESILinked) toStr += " (ESI)";

        return toStr;
    }

    /// <summary>
    /// Update the Character info
    /// </summary>
    public async Task Update()
    {
        await UpdateLock.WaitAsync();
        {
            var ts = ESIAccessTokenExpiry - DateTime.Now;
            if (ts.Minutes < 1)
            {
                await RefreshAccessToken().ConfigureAwait(false);
                await UpdateInfoFromESI().ConfigureAwait(false);
            }

            // if we're forcing ESI for our location OR we havent had one yet (due to timeout errors with the location endpoint)
            if (EveManager.Instance.UseESIForCharacterPositions || string.IsNullOrEmpty(Location))
                await UpdatePositionFromESI().ConfigureAwait(false);

            // update onliune and fleet status every other tick
            if (m_updateTick)
            {
                await UpdateOnlineStatus().ConfigureAwait(false);
                await UpdateFleetInfo().ConfigureAwait(false);
            }

            m_updateTick = !m_updateTick;

            if (routeNeedsUpdate)
            {
                routeNeedsUpdate = false;
                UpdateActiveRoute();

                if (RouteUpdatedEvent != null) RouteUpdatedEvent();
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
        var handler = PropertyChanged;
        if (handler != null) handler(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Refresh the ESI access token
    /// </summary>
    public async Task RefreshAccessToken()
    {
        if (string.IsNullOrEmpty(ESIRefreshToken) || !ESILinked) return;

        try
        {
            var tokenDetails = await EveManager.Instance.Sso.GetNewPKCEAccessAndRefreshTokenAsync(ESIRefreshToken);
            if (tokenDetails == null || string.IsNullOrEmpty(tokenDetails.AccessToken))
            {
                ssoErrorCount++;
                Thread.Sleep(20000);
                if (ssoErrorCount > 50)
                {
                    ESIRefreshToken = "";
                    ESILinked = false;
                }

                return;
            }

            var characterDetails = await EveManager.Instance.Sso.GetCharacterDetailsAsync(tokenDetails.AccessToken);
            if (characterDetails == null) return;

            ESIAccessToken = tokenDetails.AccessToken;
            ESIAccessTokenExpiry = tokenDetails.ExpiresUtc.ToLocalTime();
            ESIRefreshToken = tokenDetails.RefreshToken ?? string.Empty;
            ESILinked = true;
            ESIScopesStored = characterDetails.Scopes != null
                ? string.Join(" ", characterDetails.Scopes)
                : string.Empty;
        }
        catch (Exception ex)
        {
            // expired token
            if (ex.HResult == -2147024809)
            {
                ESIRefreshToken = "";
                ESILinked = false;
            }
        }
    }

    /// <summary>
    /// Update the active route for the character
    /// </summary>
    private async void UpdateActiveRoute()
    {
        if (esiSendRouteClear)
        {
            esiSendRouteClear = false;
            esiRouteNeedsUpdate = false;

            var s = EveManager.Instance.GetEveSystem(Location);
            if (s != null)
            {
                var auth = GetAuthDTO();
                if (auth != null)
                    try
                    {
                        await EveManager.Instance.EveApiClient.UserInterface.SetAutopilotWaypointAsync(auth, true, true,
                            s.ID);
                    }
                    catch
                    {
                    }
            }

            return;
        }

        if (Waypoints.Count == 0) return;

        {
            // new routing
            var start = string.Empty;
            var end = Location;

            // grab the simple list of thera connections
            var currentActiveTheraConnections = new List<string>();
            foreach (var tc in EveManager.Instance.TheraConnections.ToList())
                currentActiveTheraConnections.Add(tc.System);
            Navigation.UpdateTheraConnections(currentActiveTheraConnections);

            // grab the simple list of turnur connections
            var currentActiveTurnurConnections = new List<string>();
            foreach (var tc in EveManager.Instance.TurnurConnections.ToList())
                currentActiveTurnurConnections.Add(tc.System);
            Navigation.UpdateTurnurConnections(currentActiveTurnurConnections);

            lock (ActiveRouteLock)
            {
                if (Location == Waypoints[0]) Waypoints.RemoveAt(0);
            }

            ActiveRoute.Clear();

            // loop through all the waypoints
            for (var i = 0; i < Waypoints.Count; i++)
            {
                start = end;
                end = Waypoints[i];

                var sysList = Navigation.Navigate(start, end, UseAnsiblexGates, UseTheraRouting, UseZarzakhRouting,
                    UseTurnurRouting, NavigationMode);

                if (sysList != null)
                    lock (ActiveRouteLock)
                    {
                        foreach (var s in sysList) ActiveRoute.Add(s);
                    }
            }

            ActiveRouteLength = ActiveRoute.Count;
        }

        if (esiRouteNeedsUpdate && !esiRouteUpdating)
        {
            esiRouteNeedsUpdate = false;
            esiRouteUpdating = true;

            var WayPointsToAdd = new List<long>();

            lock (ActiveRouteLock)
            {
                foreach (var rp in ActiveRoute)
                    // explicitly add interim waypoints for ansiblex gates or actual waypoints
                    if (
                        rp.GateToTake == Navigation.GateType.Ansiblex ||
                        rp.GateToTake == Navigation.GateType.Thera ||
                        rp.GateToTake == Navigation.GateType.Turnur ||
                        rp.GateToTake == Navigation.GateType.Zarzakh ||
                        Waypoints.Contains(rp.SystemName)
                    )
                    {
                        var wayPointSysID = EveManager.Instance.GetEveSystem(rp.SystemName).ID;

                        if (rp.GateToTake == Navigation.GateType.Ansiblex)
                            foreach (var jb in EveManager.Instance.JumpBridges)
                            {
                                if (jb.From == rp.SystemName)
                                {
                                    if (jb.FromID != 0) wayPointSysID = jb.FromID;
                                    break;
                                }

                                if (jb.To == rp.SystemName)
                                {
                                    if (jb.ToID != 0) wayPointSysID = jb.ToID;
                                    break;
                                }
                            }

                        WayPointsToAdd.Add(wayPointSysID);
                    }
            }

            var firstRoute = true;

            var auth = GetAuthDTO();
            if (auth != null)
                foreach (var SysID in WayPointsToAdd)
                {
                    try
                    {
                        await EveManager.Instance.EveApiClient.UserInterface.SetAutopilotWaypointAsync(auth, firstRoute,
                            false, SysID);
                    }
                    catch
                    {
                    }

                    firstRoute = false;
                    Thread.Sleep(200);
                }

            esiRouteUpdating = false;
        }
    }

    /// <summary>
    /// Update the characters FleetInfo
    /// </summary>
    private async Task UpdateFleetInfo()
    {
        var auth = GetAuthDTO();
        if (auth == null || ID == 0 || !ESILinked) return;

        try
        {
            var sendFleetUpdatedEvent = false;

            if (FleetInfo.NextFleetMembershipCheck < DateTime.Now)
            {
                FleetInfo.NextFleetMembershipCheck = DateTime.Now + TimeSpan.FromSeconds(240);

                var esr = await EveManager.Instance.EveApiClient.Fleets.GetCharacterFleetInfoAsync(auth);
                if (ESIHelpers.ValidateESICall(esr) && esr.Model != null)
                {
                    FleetInfo.FleetID = esr.Model.FleetId;
                    FleetInfo.IsFleetBoss = esr.Model.Role == "fleet_commander";
                }
                else
                {
                    FleetInfo.FleetID = 0;
                    FleetInfo.Members.Clear();
                    sendFleetUpdatedEvent = true;
                }
            }

            if (FleetInfo.FleetID != 0 && FleetInfo.IsFleetBoss)
            {
                var characterIDsToResolve = new List<int>();

                var esrf = await EveManager.Instance.EveApiClient.Fleets.GetFleetMembersAsync(auth, FleetInfo.FleetID);
                if (ESIHelpers.ValidateESICall(esrf) && esrf.Model != null)
                {
                    foreach (var ff in FleetInfo.Members) ff.IsValid = false;

                    foreach (var esifm in esrf.Model)
                    {
                        Fleet.FleetMember fm = null;

                        foreach (var ff in FleetInfo.Members)
                            if (ff.CharacterID == esifm.CharacterId)
                            {
                                fm = ff;
                                fm.IsValid = true;
                            }

                        if (fm == null)
                        {
                            fm = new Fleet.FleetMember();
                            fm.IsValid = true;
                            FleetInfo.Members.Add(fm);
                            sendFleetUpdatedEvent = true;
                        }

                        var es = EveManager.Instance.GetEveSystemFromID(esifm.SolarSystemId);

                        fm.Name = EveManager.Instance.GetCharacterName((int)esifm.CharacterId);

                        fm.CharacterID = (int)esifm.CharacterId;

                        if (es == null)
                        {
                            fm.Location = "";
                            fm.Region = "";
                        }
                        else
                        {
                            fm.Location = es.Name;
                            fm.Region = es.Region;
                        }

                        if (EveManager.Instance.ShipTypes.ContainsKey(esifm.ShipTypeId.ToString()))
                            fm.ShipType = EveManager.Instance.ShipTypes[esifm.ShipTypeId.ToString()];
                        else
                            fm.ShipType = "Unknown : " + esifm.ShipTypeId.ToString();

                        if (string.IsNullOrEmpty(fm.Name)) characterIDsToResolve.Add((int)esifm.CharacterId);
                    }

                    if (characterIDsToResolve.Count > 0)
                        EveManager.Instance.ResolveCharacterIDs(characterIDsToResolve).Wait();

                    foreach (var ff in FleetInfo.Members.ToList())
                        if (!ff.IsValid)
                        {
                            FleetInfo.Members.Remove(ff);
                            sendFleetUpdatedEvent = true;
                        }
                }
                else
                {
                    // something went wrong (probably lost fleet_commander), reset this check
                    FleetInfo.NextFleetMembershipCheck = DateTime.Now + TimeSpan.FromSeconds(60);
                    FleetInfo.FleetID = 0;

                    FleetInfo.Members.Clear();
                    sendFleetUpdatedEvent = true;
                }
            }

            if (sendFleetUpdatedEvent)
                if (FleetUpdatedEvent != null)
                    FleetUpdatedEvent(this);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Update the character info from the ESI data if linked
    /// </summary>
    public async Task UpdateInfoFromESI()
    {
        var auth = GetAuthDTO();
        if (auth == null || ID == 0 || !ESILinked)
        {
            if (ESILinked) ESIAccessTokenExpiry = DateTime.Now;
            return;
        }

        var AllianceToResolve = new List<int>();

        try
        {
            var esr = await EveManager.Instance.EveApiClient.Character.GetCharacterPublicInfoAsync(ID);
            if (ESIHelpers.ValidateESICall(esr) && esr.Model != null)
            {
                CorporationID = (int)esr.Model.CorporationId;
                AllianceID = esr.Model.AllianceId ?? 0;
            }

            if (Standings.Count == 0)
            {
                if (AllianceID != 0)
                {
                    var page = 1;
                    var maxPageCount = 1;
                    do
                    {
                        var esrAlliance =
                            await EveManager.Instance.EveApiClient.Contacts.GetAllianceContactsAsync(auth, AllianceID,
                                page);
                        if (ESIHelpers.ValidateESICall(esrAlliance) && esrAlliance.Model != null)
                        {
                            maxPageCount = esrAlliance.MaxPages > 0 ? esrAlliance.MaxPages : 1;
                            foreach (var con in esrAlliance.Model)
                            {
                                Standings[con.ContactId] = (float)con.Standing;
                                if (con.ContactType == "alliance") AllianceToResolve.Add((int)con.ContactId);
                            }
                        }

                        page++;
                    } while (page <= maxPageCount);
                }

                if (CorporationID != 0)
                {
                    var page = 1;
                    var maxPageCount = 1;
                    do
                    {
                        var esrCorp =
                            await EveManager.Instance.EveApiClient.Contacts.GetCorporationContactsAsync(auth,
                                CorporationID, page);
                        if (ESIHelpers.ValidateESICall(esrCorp) && esrCorp.Model != null)
                        {
                            maxPageCount = esrCorp.MaxPages > 0 ? esrCorp.MaxPages : 1;
                            foreach (var con in esrCorp.Model)
                            {
                                Standings[con.ContactId] = (float)con.Standing;
                                if (con.ContactType == "alliance") AllianceToResolve.Add((int)con.ContactId);
                            }
                        }

                        page++;
                    } while (page <= maxPageCount);
                }
            }

            var portraitRoot = Path.Combine(EveManager.Instance.SaveDataRootFolder, "Portraits");
            var characterPortrait = Path.Combine(portraitRoot, ID.ToString());
            if (!File.Exists(characterPortrait))
            {
                var esri = await EveManager.Instance.EveApiClient.Character.GetCharacterPortraitsAsync(ID);
                if (ESIHelpers.ValidateESICall(esri) && esri.Model != null &&
                    !string.IsNullOrEmpty(esri.Model.Px128x128))
                    try
                    {
                        var hc = new HttpClient();
                        var response = await hc.GetAsync(esri.Model.Px128x128);
                        using (var fs = new FileStream(characterPortrait, FileMode.CreateNew))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }
                    catch
                    {
                    }
            }

            if (File.Exists(characterPortrait)) PortraitLocation = new Uri(characterPortrait);

            if (CorporationID != -1)
            {
                var esrc = await EveManager.Instance.EveApiClient.Corporation.GetCorporationInfoAsync(CorporationID);
                if (ESIHelpers.ValidateESICall(esrc) && esrc.Model != null)
                {
                    CorporationName = esrc.Model.Name;
                    CorporationTicker = esrc.Model.Ticker;
                }
            }

            if (AllianceID > 0)
            {
                var esra = await EveManager.Instance.EveApiClient.Alliance.GetAllianceInfoAsync(AllianceID);
                if (ESIHelpers.ValidateESICall(esra) && esra.Model != null)
                {
                    AllianceName = esra.Model.Name;
                    AllianceTicker = esra.Model.Ticker;
                }
            }
            else
            {
                AllianceName = null;
                AllianceTicker = null;
            }

            EdenCommStandingsGood = false;
            TrigStandingsGood = false;
            var essl = await EveManager.Instance.EveApiClient.Character.GetStandingsAsync(auth);
            if (ESIHelpers.ValidateESICall(essl) && essl.Model != null)
                foreach (var standing in essl.Model)
                {
                    if (standing.FromId == 500027 && standing.StandingValue > 0) EdenCommStandingsGood = true;
                    if (standing.FromId == 500026 && standing.StandingValue > 0) TrigStandingsGood = true;
                }
        }
        catch (Exception)
        {
        }

        EveManager.Instance.ResolveAllianceIDs(AllianceToResolve);
    }

    /// <summary>
    /// Update the characters logged on status from ESI
    /// </summary>
    private async Task UpdateOnlineStatus()
    {
        var auth = GetAuthDTO();
        if (auth == null || ID == 0 || !ESILinked) return;

        try
        {
            var esr = await EveManager.Instance.EveApiClient.Location.GetCharacterOnlineAsync(auth);
            if (ESIHelpers.ValidateESICall(esr) && esr.Model != null) IsOnline = esr.Model.Online;
        }
        catch
        {
        }
    }

    /// <summary>
    /// Update the characters position from ESI (will override the position read from any log files
    /// </summary>
    public async Task UpdatePositionFromESI()
    {
        var auth = GetAuthDTO();
        if (auth == null || ID == 0 || !ESILinked) return;

        try
        {
            var esr = await EveManager.Instance.EveApiClient.Location.GetCharacterLocationAsync(auth);
            if (ESIHelpers.ValidateESICall(esr) && esr.Model != null)
            {
                if (!EveManager.Instance.SystemIDToName.ContainsKey(esr.Model.SolarSystemId))
                {
                    Location = "";
                    Region = "";
                    return;
                }

                Location = EveManager.Instance.SystemIDToName[esr.Model.SolarSystemId];
                var s = EveManager.Instance.GetEveSystem(Location);
                if (s != null)
                    Region = s.Region;
                else
                    Region = "";
            }
        }
        catch
        {
        }
    }

    private void UpdateWarningSystems()
    {
        // only track warning systems if the character is logged in
        if (IsOnline)
        {
            if (!string.IsNullOrEmpty(Location) && DangerZoneRange > 0 && DangerZoneActive)
                WarningSystems = Navigation.GetSystemsXJumpsFrom(new List<string>(), Location, DangerZoneRange);
        }
        else
        {
            if (WarningSystems != null) WarningSystems.Clear();
        }
    }
}