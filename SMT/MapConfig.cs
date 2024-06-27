using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SMT
{
    public class MapConfig : INotifyPropertyChanged
    {
        public static readonly string SaveVersion = "01";

        [Browsable(false)]
        public MapColours ActiveColourScheme;

        [Category("Navigation")]
        private bool m_AlwaysOnTop;

        private bool m_MinimizeToTray;
        private bool m_CloseToTray;

        private bool m_FlashWindow;
        private bool m_FlashWindowOnlyInDangerZone;

        private bool m_PlayIntelSound;
        private bool m_PlaySoundOnlyInDangerZone;

        private string m_DefaultRegion;

        private bool m_LimitESIDataToRegion;
        private bool m_DisableJumpBridgesPathAnimation;
        private bool m_DisableRoutePathAnimation;
        private int m_FleetMaxMembersPerSystem = 5;
        private bool m_FleetShowOnMap = true;
        private bool m_FleetShowShipType;
        private double m_IntelTextSize = 10;

        private bool m_JumpRangeInAsOutline;

        private int m_MaxIntelSeconds;

        private bool m_showCompactCharactersOnMap = false;
        private bool m_showOfflineCharactersOnMap = true;
        private bool m_ShowCharacterNamesOnMap = true;
        private bool m_ShowCoalition;

        private bool m_ShowDangerZone;

        private bool m_ShowIhubVunerabilities;

        private bool m_ShowJoveObservatories;
        private bool m_ShowNegativeRattingDelta;

        private bool m_ShowRattingDataAsDelta = true;

        private bool m_ShowRegionStandings;
        private bool m_ShowSimpleSecurityView;

        private bool m_ShowToolBox = true;

        private bool m_ShowTrigInvasions = true;
        private bool m_ShowTrueSec;

        private bool m_ShowUniverseKills;

        private bool m_ShowUniversePods;

        private bool m_ShowUniverseRats;

        private bool m_ShowZKillData;

        private bool m_SyncActiveCharacterBasedOnActiveEVEClient;
        private double m_UniverseDataScale = 1.0f;

        private float m_UniverseMaxZoomDisplaySystems;

        private float m_UniverseMaxZoomDisplaySystemsText;

        private int m_UpcomingSovMinutes;

        private int m_ZkillExpireTimeMinutes;

        private bool m_drawRoute;

        private bool m_followOnZoom;

        private string m_CustomEveLogFolderLocation;

        private bool m_ClampMaxESIOverlayValue;

        private int m_MaxESIOverlayValue;

        // Overlay settings
        private float m_overlayBackgroundOpacity = 0.2f;

        private float m_overlayOpacity = 0.5f;
        private int m_overlayRange = 5;
        private float m_intelFreshTime = 30;
        private float m_intelStaleTime = 120;
        private float m_intelHistoricTime = 600;
        private bool m_overlayGathererMode = false;
        private bool m_overlayHunterModeShowFullRegion = true;
        private bool m_overlayShowCharName = true;
        private bool m_overlayShowCharLocation = true;
        private bool m_overlayShowNPCKills = true;
        private bool m_overlayShowNPCKillDelta = true;
        private bool m_overlayShowRoute = true;
        private bool m_overlayShowJumpBridges = true;
        private bool m_overlayShowSystemNames = false;
        private bool m_overlayShowAllCharacterNames = false;
        private bool m_overlayIndividualCharacterWindows = false;

        private bool m_EnableHTTPProxy;
        private string m_HTTPProxyServer;
        private string m_HTTPProxyPort;


        public MapConfig()
        {
            SetDefaults();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [Category("General")]
        [DisplayName("Always on top")]
        public bool AlwaysOnTop
        {
            get
            {
                return m_AlwaysOnTop;
            }
            set
            {
                m_AlwaysOnTop = value;
                OnPropertyChanged("AlwaysOnTop");
            }
        }

        [Category("General")]
        [DisplayName("Minimize to tray")]
        public bool MinimizeToTray
        {
            get
            {
                return m_MinimizeToTray;
            }
            set
            {
                if (!value)
                {
                    CloseToTray = false;
                }
                m_MinimizeToTray = value;
                OnPropertyChanged("MinimizeToTray");
            }
        }

        [Category("General")]
        [DisplayName("Close to tray")]
        public bool CloseToTray
        {
            get
            {
                return m_CloseToTray;
            }
            set
            {
                m_CloseToTray = value;
                OnPropertyChanged("CloseToTray");
            }
        }

        [Browsable(false)]
        public string DefaultColourSchemeName { get; set; }

        [Browsable(false)]
        public string DefaultRegion
        {
            get
            {
                return m_DefaultRegion;
            }
            set
            {
                m_DefaultRegion = value;
                OnPropertyChanged("DefaultRegion");
            }
        }

        [Browsable(false)]
        public bool ToolBox_ShowJumpBridges { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowNPCKills { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowPodKills { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowShipJumps { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowShipKills { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowSovOwner { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowStandings { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowSystemADM { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowSystemSecurity { get; set; }

        [Browsable(false)]
        public bool ToolBox_ShowSystemTimers { get; set; }

        [Browsable(false)]
        public double ToolBox_ESIOverlayScale { get; set; }



        [Browsable(false)]
        public bool Debug_EnableMapEdit { get; set; }


        [Browsable(false)]
        public bool EnableHTTPProxy
        {
            get => m_EnableHTTPProxy;
            set
            {
                m_EnableHTTPProxy = value;
                OnPropertyChanged("EnableHTTPProxy");
            }
        }

        public string HTTPProxyServer { get; set; }
        public string HTTPProxyPort { get; set; }


        public bool DisableJumpBridgesPathAnimation
        {
            get => m_DisableJumpBridgesPathAnimation;
            set
            {
                m_DisableJumpBridgesPathAnimation = value;
                OnPropertyChanged("DisableJumpBridgesPathAnimation");
            }
        }

        public bool DisableRoutePathAnimation
        {
            get => m_DisableRoutePathAnimation;
            set
            {
                m_DisableRoutePathAnimation = value;
                OnPropertyChanged("DisableRoutePathAnimation");
            }
        }

        [XmlIgnoreAttribute]
        public string CurrentEveLogFolderLocation
        {
            get; set;
        }

        public string CustomEveLogFolderLocation
        {
            get => m_CustomEveLogFolderLocation;
            set
            {
                m_CustomEveLogFolderLocation = value;
                OnPropertyChanged("CustomEveLogFolderLocation");
            }
        }

        public bool DrawRoute
        {
            get
            {
                return m_drawRoute;
            }
            set
            {
                m_drawRoute = value;
                OnPropertyChanged("DrawRoute");
            }
        }

        public bool FollowOnZoom
        {
            get
            {
                return m_followOnZoom;
            }
            set
            {
                m_followOnZoom = value;
                OnPropertyChanged("FollowOnZoom");
            }
        }

        public bool LimitESIDataToRegion
        {
            get
            {
                return m_LimitESIDataToRegion;
            }
            set
            {
                m_LimitESIDataToRegion = value;
                OnPropertyChanged("LimitESIDataToRegion");
            }
        }

        public bool ClampMaxESIOverlayValue
        {
            get
            {
                return m_ClampMaxESIOverlayValue;
            }
            set
            {
                m_ClampMaxESIOverlayValue = value;
                OnPropertyChanged("ClampMaxESIOverlayValue");
            }
        }

        public int MaxESIOverlayValue
        {
            get
            {
                return m_MaxESIOverlayValue;
            }
            set
            {
                if (value >= 30)
                {
                    m_MaxESIOverlayValue = value;
                }
                else
                {
                    m_MaxESIOverlayValue = 30;
                }
                OnPropertyChanged("MaxESIOverlayValue");
            }
        }

        [Category("Fleet")]
        [DisplayName("Max Fleet Per System")]
        public int FleetMaxMembersPerSystem
        {
            get
            {
                return m_FleetMaxMembersPerSystem;
            }
            set
            {
                // clamp to 1 miniumum
                if (value > 0)
                {
                    m_FleetMaxMembersPerSystem = value;
                }
                else
                {
                    m_FleetMaxMembersPerSystem = 1;
                }

                OnPropertyChanged("FleetMaxMembersPerSystem");
            }
        }

        [Category("Fleet")]
        [DisplayName("Show On Map")]
        public bool FleetShowOnMap
        {
            get
            {
                return m_FleetShowOnMap;
            }
            set
            {
                m_FleetShowOnMap = value;
                OnPropertyChanged("FleetShowOnMap");
            }
        }

        [Category("Fleet")]
        [DisplayName("Show Ship Type")]
        public bool FleetShowShipType
        {
            get
            {
                return m_FleetShowShipType;
            }
            set
            {
                m_FleetShowShipType = value;
                OnPropertyChanged("FleetShowShipType");
            }
        }

        [Category("Intel")]
        [DisplayName("Text Size")]
        public double IntelTextSize
        {
            get
            {
                return m_IntelTextSize;
            }
            set
            {
                if (value > 20)
                {
                    m_IntelTextSize = 20;
                }
                else
                {
                    m_IntelTextSize = value;
                }

                if (value < 8)
                {
                    m_IntelTextSize = 8;
                }
                else
                {
                    m_IntelTextSize = value;
                }

                OnPropertyChanged("IntelTextSize");
            }
        }

        [Category("Navigation")]
        [DisplayName("Jump Range as Outline")]
        public bool JumpRangeInAsOutline
        {
            get
            {
                return m_JumpRangeInAsOutline;
            }
            set
            {
                m_JumpRangeInAsOutline = value;
                OnPropertyChanged("JumpRangeInAsOutline");
            }
        }

        [Category("Navigation")]
        [DisplayName("Ship Type")]
        public EVEData.EveManager.JumpShip JumpShipType { get; set; }

        [Category("Intel")]
        [DisplayName("Max Intel Time (s)")]
        public int MaxIntelSeconds
        {
            get
            {
                return m_MaxIntelSeconds;
            }
            set
            {
                // clamp to 30s miniumum
                if (value > 30)
                {
                    m_MaxIntelSeconds = value;
                }
                else
                {
                    m_MaxIntelSeconds = 30;
                }
            }
        }

        [Category("Intel")]
        [DisplayName("Warning Sound")]
        public bool PlayIntelSound
        {
            get
            {
                return m_PlayIntelSound;
            }
            set
            {
                if (!value)
                {
                    PlaySoundOnlyInDangerZone = false;
                }
                m_PlayIntelSound = value;
                OnPropertyChanged("PlayIntelSound");
            }
        }

        [Category("Intel")]
        [DisplayName("Warning on Alert Text")]
        public bool PlayIntelSoundOnAlert { get; set; }

        [Category("Intel")]
        [DisplayName("Warning On Unknown")]
        public bool PlayIntelSoundOnUnknown { get; set; }

        [Category("Intel")]
        [DisplayName("Flash Window on New Intel")]
        public bool FlashWindow
        {
            get
            {
                return m_FlashWindow;
            }
            set
            {
                if (!value)
                {
                    FlashWindowOnlyInDangerZone = false;
                }
                m_FlashWindow = value;
                OnPropertyChanged("FlashWindow");
            }
        }

        [Category("Intel")]
        [DisplayName("Warning Sound Volume")]
        public float IntelSoundVolume { get; set; }

        [Category("Intel")]
        [DisplayName("Limit Sound to Dangerzone")]
        public bool PlaySoundOnlyInDangerZone
        {
            get
            {
                return m_PlaySoundOnlyInDangerZone;
            }
            set
            {
                m_PlaySoundOnlyInDangerZone = value;
                OnPropertyChanged("PlaySoundOnlyInDangerZone");
            }
        }

        [Category("Intel")]
        [DisplayName("Limit Window Flash to Dangerzone")]
        public bool FlashWindowOnlyInDangerZone
        {
            get
            {
                return m_FlashWindowOnlyInDangerZone;
            }
            set
            {
                m_FlashWindowOnlyInDangerZone = value;
                OnPropertyChanged("FlashWindowOnlyInDangerZone");
            }
        }

        [Category("Incursions")]
        [DisplayName("Show Active Incursions")]
        public bool ShowActiveIncursions { get; set; }

        public bool ShowCharacterNamesOnMap
        {
            get
            {
                return m_ShowCharacterNamesOnMap;
            }
            set
            {
                m_ShowCharacterNamesOnMap = value;
                OnPropertyChanged("ShowCharacterNamesOnMap");
            }
        }

        public bool ShowCompactCharactersOnMap
        {
            get
            {
                return m_showCompactCharactersOnMap;
            }
            set
            {
                m_showCompactCharactersOnMap = value;
                OnPropertyChanged("ShowCompactCharactersOnMap");
            }
        }

        public bool ShowOfflineCharactersOnMap
        {
            get
            {
                return m_showOfflineCharactersOnMap;
            }
            set
            {
                m_showOfflineCharactersOnMap = value;
                OnPropertyChanged("ShowOfflineCharactersOnMap");
            }
        }

        [Category("SOV")]
        [DisplayName("Show Coalition")]
        public bool ShowCoalition
        {
            get
            {
                return m_ShowCoalition;
            }

            set
            {
                m_ShowCoalition = value;
                OnPropertyChanged("ShowCoalition");
            }
        }

        [Category("Navigation")]
        [DisplayName("Show Cyno Beacons")]
        public bool ShowCynoBeacons { get; set; }

        [Category("Intel")]
        [DisplayName("Show DangerZone")]
        public bool ShowDangerZone
        {
            get
            {
                return m_ShowDangerZone;
            }
            set
            {
                m_ShowDangerZone = value;
                OnPropertyChanged("ShowDangerZone");
            }
        }

        [Category("SOV")]
        [DisplayName("Show IHUB Timers")]
        public bool ShowIhubVunerabilities
        {
            get
            {
                return m_ShowIhubVunerabilities;
            }

            set
            {
                m_ShowIhubVunerabilities = value;

                OnPropertyChanged("ShowIhubVunerabilities");
            }
        }

        [Category("Jove")]
        [DisplayName("Show Observatories")]
        public bool ShowJoveObservatories
        {
            get
            {
                return m_ShowJoveObservatories;
            }
            set
            {
                m_ShowJoveObservatories = value;
                OnPropertyChanged("ShowJoveObservatories");
            }
        }

        [Category("Misc")]
        [DisplayName("Show Negative Ratting Delta")]
        public bool ShowNegativeRattingDelta
        {
            get
            {
                return m_ShowNegativeRattingDelta;
            }
            set
            {
                m_ShowNegativeRattingDelta = value;
                OnPropertyChanged("ShowNegativeRattingDelta");
            }
        }

        [Category("Misc")]
        [DisplayName("Show Ratting Data as Delta")]
        public bool ShowRattingDataAsDelta
        {
            get
            {
                return m_ShowRattingDataAsDelta;
            }
            set
            {
                m_ShowRattingDataAsDelta = value;
                OnPropertyChanged("ShowRattingDataAsDelta");
            }
        }

        [Category("Regions")]
        [DisplayName("Show RegionStandings")]
        public bool ShowRegionStandings
        {
            get
            {
                return m_ShowRegionStandings;
            }

            set
            {
                m_ShowRegionStandings = value;

                if (m_ShowRegionStandings)
                {
                    ShowUniverseRats = false;
                    ShowUniversePods = false;
                    ShowUniverseKills = false;
                }

                OnPropertyChanged("ShowRegionStandings");
            }
        }

        [Category("Misc")]
        [DisplayName("Simple Security View")]
        public bool ShowSimpleSecurityView
        {
            get
            {
                return m_ShowSimpleSecurityView;
            }
            set
            {
                m_ShowSimpleSecurityView = value;
                OnPropertyChanged("ShowSimpleSecurityView");
            }
        }

        [Category("General")]
        [DisplayName("System Popup")]
        public bool ShowSystemPopup { get; set; }


        [Category("General")]
        [DisplayName("Show Toolbox")]
        public bool ShowToolBox
        {
            get
            {
                return m_ShowToolBox;
            }
            set
            {
                m_ShowToolBox = value;
                OnPropertyChanged("ShowToolBox");
            }
        }

        public bool ShowTrigInvasions
        {
            get
            {
                return m_ShowTrigInvasions;
            }
            set
            {
                m_ShowTrigInvasions = value;
                OnPropertyChanged("ShowTrigInvasions");
            }
        }

        [Category("General")]
        [DisplayName("Show TrueSec")]
        public bool ShowTrueSec
        {
            get
            {
                return m_ShowTrueSec;
            }
            set
            {
                m_ShowTrueSec = value;
                OnPropertyChanged("ShowTrueSec");
            }
        }

        [XmlIgnoreAttribute]
        [Category("Regions")]
        [DisplayName("Show Ship kill Stats")]
        public bool ShowUniverseKills
        {
            get
            {
                return m_ShowUniverseKills;
            }

            set
            {
                m_ShowUniverseKills = value;

                if (m_ShowUniverseKills)
                {
                    ShowRegionStandings = false;
                    ShowUniverseRats = false;
                    ShowUniversePods = false;
                }

                OnPropertyChanged("ShowUniverseKills");
            }
        }

        [XmlIgnoreAttribute]
        [Category("Regions")]
        [DisplayName("Show Pod kill Stats")]
        public bool ShowUniversePods
        {
            get
            {
                return m_ShowUniversePods;
            }

            set
            {
                m_ShowUniversePods = value;
                if (ShowUniversePods)
                {
                    ShowRegionStandings = false;
                    ShowUniverseRats = false;
                    ShowUniverseKills = false;
                }

                OnPropertyChanged("ShowUniversePods");
            }
        }

        [XmlIgnoreAttribute]
        [Category("Regions")]
        [DisplayName("Show Ratting Stats")]
        public bool ShowUniverseRats
        {
            get
            {
                return m_ShowUniverseRats;
            }

            set
            {
                m_ShowUniverseRats = value;
                if (m_ShowUniverseRats)
                {
                    ShowRegionStandings = false;
                    ShowUniversePods = false;
                    ShowUniverseKills = false;
                }

                OnPropertyChanged("ShowUniverseRats");
            }
        }

        [Category("General")]
        [DisplayName("Show ZKillData")]
        public bool ShowZKillData
        {
            get
            {
                return m_ShowZKillData;
            }
            set
            {
                m_ShowZKillData = value;
                OnPropertyChanged("ShowZKillData");
            }
        }

 
 

        public bool SyncActiveCharacterBasedOnActiveEVEClient
        {
            get
            {
                return m_SyncActiveCharacterBasedOnActiveEVEClient;
            }
            set
            {
                m_SyncActiveCharacterBasedOnActiveEVEClient = value;
                OnPropertyChanged("SyncActiveCharacterBasedOnActiveEVEClient");
            }
        }

        [XmlIgnoreAttribute]
        [Category("Regions")]
        [DisplayName("Universe Data Scale")]
        public double UniverseDataScale
        {
            get
            {
                return m_UniverseDataScale;
            }

            set
            {
                m_UniverseDataScale = value;

                if (m_UniverseDataScale < 0.01)
                {
                    m_UniverseDataScale = 0.01;
                }

                OnPropertyChanged("UniverseDataScale");
            }
        }

        [Category("Universe View")]
        [DisplayName("Systems Max Zoom")]
        public float UniverseMaxZoomDisplaySystems
        {
            get
            {
                return m_UniverseMaxZoomDisplaySystems;
            }

            set
            {
                m_UniverseMaxZoomDisplaySystems = Math.Min(Math.Max(value, 0.5f), 10.0f);
                OnPropertyChanged("UniverseMaxZoomDisplaySystems");
            }
        }

        [Category("Universe View")]
        [DisplayName("Systems Text Max Zoom")]
        public float UniverseMaxZoomDisplaySystemsText
        {
            get
            {
                return m_UniverseMaxZoomDisplaySystemsText;
            }

            set
            {
                m_UniverseMaxZoomDisplaySystemsText = Math.Min(Math.Max(value, 0.5f), 10.0f);
                OnPropertyChanged("UniverseMaxZoomDisplaySystemsText");
            }
        }

        [Category("SOV")]
        [DisplayName("Upcoming Period (Mins)")]
        public int UpcomingSovMinutes
        {
            get
            {
                return m_UpcomingSovMinutes;
            }

            set
            {
                m_UpcomingSovMinutes = value;
                if (m_UpcomingSovMinutes < 5)
                {
                    m_UpcomingSovMinutes = 5;
                }

                OnPropertyChanged("UpcomingSovMinutes");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Window Content Opacity")]
        public float OverlayOpacity
        {
            get
            {
                return m_overlayOpacity;
            }
            set
            {
                m_overlayOpacity = value > 0f ? value : 1f;

                OnPropertyChanged("OverlayOpacity");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Window Background Opacity")]
        public float OverlayBackgroundOpacity
        {
            get
            {
                return m_overlayBackgroundOpacity;
            }
            set
            {
                m_overlayBackgroundOpacity = value > 0f ? value : 1f;

                OnPropertyChanged("OverlayBackgroundOpacity");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay System Jump Range")]
        public int OverlayRange
        {
            get
            {
                return m_overlayRange;
            }
            set
            {
                m_overlayRange = value > 0 ? value : 1;

                OnPropertyChanged("OverlayRange");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay In Gatherer Mode")]
        public bool OverlayGathererMode
        {
            get
            {
                return m_overlayGathererMode;
            }
            set
            {
                m_overlayGathererMode = value;

                OnPropertyChanged("OverlayGathererMode");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay In Hunter Mode Shows Full Region")]
        public bool OverlayHunterModeShowFullRegion
        {
            get
            {
                return m_overlayHunterModeShowFullRegion;
            }
            set
            {
                m_overlayHunterModeShowFullRegion = value;

                OnPropertyChanged("OverlayHunterModeShowFullRegion");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Show Char Name")]
        public bool OverlayShowCharName
        {
            get
            {
                return m_overlayShowCharName;
            }
            set
            {
                m_overlayShowCharName = value;

                OnPropertyChanged("OverlayShowCharName");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Show Char Location")]
        public bool OverlayShowCharLocation
        {
            get
            {
                return m_overlayShowCharLocation;
            }
            set
            {
                m_overlayShowCharLocation = value;

                OnPropertyChanged("OverlayShowCharLocation");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Show NPC Kills")]
        public bool OverlayShowNPCKills
        {
            get
            {
                return m_overlayShowNPCKills;
            }
            set
            {
                m_overlayShowNPCKills = value;

                OnPropertyChanged("OverlayShowNPCKills");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Show NPC Kill Delta")]
        public bool OverlayShowNPCKillDelta
        {
            get
            {
                return m_overlayShowNPCKillDelta;
            }
            set
            {
                m_overlayShowNPCKillDelta = value;

                OnPropertyChanged("OverlayShowNPCKillDelta");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Show Route")]
        public bool OverlayShowRoute
        {
            get
            {
                return m_overlayShowRoute;
            }
            set
            {
                m_overlayShowRoute = value;

                OnPropertyChanged("OverlayShowRoute");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Show JumpBridges")]
        public bool OverlayShowJumpBridges
        {
            get
            {
                return m_overlayShowJumpBridges;
            }
            set
            {
                m_overlayShowJumpBridges = value;

                OnPropertyChanged("OverlayShowJumpBridges");
            }
        }
        
        [Category("Overlay")]
        [DisplayName("Overlay Individual Character Windows")]
        public bool OverlayIndividualCharacterWindows
        {
            get
            {
                return m_overlayIndividualCharacterWindows;
            }
            set
            {
                m_overlayIndividualCharacterWindows = value;

                OnPropertyChanged("OverlayIndividualCharacterWindows");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Show System Names")]
        public bool OverlayShowSystemNames
        {
            get
            {
                return m_overlayShowSystemNames;
            }
            set
            {
                m_overlayShowSystemNames = value;

                OnPropertyChanged("OverlayShowSystemNames");
            }
        }
        
        [Category("Overlay")]
        [DisplayName("Overlay Show All Character Names")]
        public bool OverlayShowAllCharacterNames
        {
            get
            {
                return m_overlayShowAllCharacterNames;
            }
            set
            {
                m_overlayShowAllCharacterNames = value;

                OnPropertyChanged("OverlayShowAllCharacterNames");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Intel Fresh Time")]
        public float IntelFreshTime
        {
            get
            {
                return m_intelFreshTime;
            }
            set
            {
                m_intelFreshTime = value > 0 ? value : 1;

                OnPropertyChanged("IntelFreshTime");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Intel Stale Time")]
        public float IntelStaleTime
        {
            get
            {
                return m_intelStaleTime;
            }
            set
            {
                m_intelStaleTime = value > 0 ? value : 1;

                OnPropertyChanged("IntelStaleTime");
            }
        }

        [Category("Overlay")]
        [DisplayName("Overlay Intel Fresh Time")]
        public float IntelHistoricTime
        {
            get
            {
                return m_intelHistoricTime;
            }
            set
            {
                m_intelHistoricTime = value > 0 ? value : 1;

                OnPropertyChanged("IntelHistoricTime");
            }
        }

        public int ZkillExpireTimeMinutes
        {
            get
            {
                return m_ZkillExpireTimeMinutes;
            }

            set
            {
                m_ZkillExpireTimeMinutes = value;
                if (m_ZkillExpireTimeMinutes < 5)
                {
                    m_UpcomingSovMinutes = 5;
                }

                OnPropertyChanged("ZkillExpireTimeMinutes");
            }
        }

        public bool UseESIForCharacterPositions { get; set; }

        public void SetDefaultColours()
        {
            MapColours defaultColours = new MapColours
            {
                Name = "Default",
                UserEditable = false,
                FriendlyJumpBridgeColour = Colors.Goldenrod,
                DisabledJumpBridgeColour = Color.FromRgb(205, 55, 50),
                SystemOutlineColour = Color.FromRgb(0, 0, 0),
                InRegionSystemColour = Colors.SlateGray,
                InRegionSystemTextColour = Colors.BlanchedAlmond,
                OutRegionSystemColour = (Color)ColorConverter.ConvertFromString("#FF272B2F"),
                OutRegionSystemTextColour = (Color)ColorConverter.ConvertFromString("#FF7E8184"),

                UniverseSystemColour = Colors.SlateGray,
                UniverseConstellationGateColour = Colors.SlateGray,
                UniverseSystemTextColour = Colors.BlanchedAlmond,
                UniverseGateColour = Colors.DarkSlateBlue,
                UniverseRegionGateColour = Color.FromRgb(128, 64, 64),
                UniverseMapBackgroundColour = Color.FromRgb(43, 43, 48),

                PopupText = Color.FromRgb(0, 0, 0),
                PopupBackground = (Color)ColorConverter.ConvertFromString("#FF959595"),

                MapBackgroundColour = Color.FromRgb(43, 43, 48),
                RegionMarkerTextColour = Color.FromRgb(49, 49, 53),
                RegionMarkerTextColourFull = Color.FromRgb(0, 0, 0),
                ESIOverlayColour = (Color)ColorConverter.ConvertFromString("#FF74B071"),

                IntelOverlayColour = Color.FromRgb(178, 34, 34),
                IntelClearOverlayColour = Colors.Orange,

                NormalGateColour = Colors.DarkSlateBlue,
                ConstellationGateColour = Colors.SlateGray,
                RegionGateColour = Color.FromRgb(128, 64, 64),
                SelectedSystemColour = Color.FromRgb(255, 255, 255),
                CharacterHighlightColour = Color.FromRgb(170, 130, 180),
                CharacterOfflineTextColour = Colors.DarkGray,
                CharacterTextColour = Color.FromRgb(240, 190, 10),
                CharacterTextSize = 11,
                SystemTextSize = 12,
                SystemSubTextSize = 7,

                FleetMemberTextColour = Colors.White,

                JumpRangeInColour = Color.FromRgb(255, 165, 0),
                JumpRangeInColourHighlight = Color.FromArgb(20, 82, 135, 125),
                JumpRangeOverlapHighlight = Colors.DarkBlue,

                ActiveIncursionColour = Color.FromRgb(110, 82, 77),

                SOVStructureVulnerableColour = Color.FromRgb(64, 64, 64),
                SOVStructureVulnerableSoonColour = Color.FromRgb(178, 178, 178),

                ConstellationHighlightColour = Color.FromRgb(147, 131, 131),

                TheraEntranceRegion = Colors.YellowGreen,
                TheraEntranceSystem = Colors.YellowGreen,

                ZKillDataOverlay = Colors.Purple
            };

            ActiveColourScheme = defaultColours;
        }

        public void SetDefaults()
        {
            DefaultRegion = "Molden Heath";
            ShowSystemPopup = true;
            MaxIntelSeconds = 120;
            UpcomingSovMinutes = 30;
            ZkillExpireTimeMinutes = 30;
            AlwaysOnTop = false;
            MinimizeToTray = false;
            CloseToTray = false;
            FlashWindow = false;
            ShowToolBox = true;
            ShowZKillData = true;
            ShowTrueSec = true;
            JumpRangeInAsOutline = true;
            ShowActiveIncursions = true;
            UseESIForCharacterPositions = true;
            ShowCharacterNamesOnMap = true;
            ShowOfflineCharactersOnMap = true;
            ShowIhubVunerabilities = true;

            ShowJoveObservatories = true;
            ShowCynoBeacons = true;
            LimitESIDataToRegion = false;
            ClampMaxESIOverlayValue = true;
            MaxESIOverlayValue = 120;
            UniverseMaxZoomDisplaySystems = 1.3f;
            UniverseMaxZoomDisplaySystemsText = 2.0f;

            IntelSoundVolume = 0.5f;

            OverlayOpacity = 0.5f;
            OverlayRange = 5;
            OverlayGathererMode = false;
            OverlayShowCharName = true;
            OverlayShowCharLocation = true;
            OverlayShowNPCKills = true;
            OverlayShowNPCKillDelta = true;
            OverlayShowRoute = true;
            OverlayShowJumpBridges = true;
            OverlayShowSystemNames = false;
            OverlayShowAllCharacterNames = false;

            IntelFreshTime = 30;
            IntelStaleTime = 120;
            IntelHistoricTime = 600;
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