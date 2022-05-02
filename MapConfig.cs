using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SMT
{

    public class MapConfig : INotifyPropertyChanged
    {
        [Browsable(false)]
        public MapColours ActiveColourScheme;

        [Category("Navigation")]
        public ObservableCollection<StaticJumpOverlay> StaticJumpPoints;

        private bool m_AlwaysOnTop;

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

        private bool m_showOfflineCharactersOnMap = true;
        private bool m_ShowCharacterNamesOnMap = true;
        private bool m_ShowCoalition;

        private bool m_ShowDangerZone;

        private bool m_ShowIhubVunerabilities;

        private bool m_ShowJoveObservatories;
        private bool m_ShowNegativeRattingDelta;

        private bool m_ShowRattingDataAsDelta;

        private bool m_ShowRegionStandings;
        private bool m_ShowSimpleSecurityView;
        private bool m_ShowTCUVunerabilities;

        private bool m_ShowToolBox = true;

        private bool m_ShowTrigInvasions = true;
        private bool m_ShowTrueSec;

        private bool m_ShowUniverseKills;

        private bool m_ShowUniversePods;

        private bool m_ShowUniverseRats;

        private bool m_ShowZKillData;

        private bool m_SOVBasedonTCU;

        private bool m_SOVShowConflicts;
        private bool m_SyncActiveCharacterBasedOnActiveEVEClient;
        private double m_UniverseDataScale = 1.0f;

        private float m_UniverseMaxZoomDisplaySystems;

        private float m_UniverseMaxZoomDisplaySystemsText;

        private int m_UpcomingSovMinutes;

        private int m_ZkillExpireTimeMinutes;

        private bool m_drawRoute;

        private string m_CustomEveLogFolderLocation;

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
        public bool Debug_EnableMapEdit { get; set; }

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
        public bool PlayIntelSound { get; set; }

        [Category("Intel")]
        [DisplayName("Warning On Unknown")]
        public bool PlayIntelSoundOnUnknown { get; set; }

        [Category("Intel")]
        [DisplayName("Warning Sound Volume")]
        public float IntelSoundVolume { get; set; }

        [Category("Intel")]
        [DisplayName("Limit Sound to Dangerzone")]
        public bool PlaySoundOnlyInDangerZone { get; set; }

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
                m_ShowTCUVunerabilities = !m_ShowIhubVunerabilities;

                OnPropertyChanged("ShowIhubVunerabilities");
                OnPropertyChanged("ShowTCUVunerabilities");
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

        [Category("SOV")]
        [DisplayName("Show TCU Timers")]
        public bool ShowTCUVunerabilities
        {
            get
            {
                return m_ShowTCUVunerabilities;
            }

            set
            {
                m_ShowTCUVunerabilities = value;
                m_ShowIhubVunerabilities = !m_ShowTCUVunerabilities;

                OnPropertyChanged("ShowIhubVunerabilities");
                OnPropertyChanged("ShowTCUVunerabilities");
            }
        }

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

        [Category("SOV")]
        [DisplayName("Show Sov Based on TCU")]
        public bool SOVBasedITCU
        {
            get
            {
                return m_SOVBasedonTCU;
            }
            set
            {
                m_SOVBasedonTCU = value;
                OnPropertyChanged("SOVBasedITCU");
            }
        }

        [Category("SOV")]
        [DisplayName("Show Sov Conflicts")]
        public bool SOVShowConflicts
        {
            get
            {
                return m_SOVShowConflicts;
            }
            set
            {
                m_SOVShowConflicts = value;
                OnPropertyChanged("SOVShowConflicts");
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
                OutRegionSystemColour = Color.FromRgb(128, 64, 64),
                OutRegionSystemTextColour = (Color)ColorConverter.ConvertFromString("#FF726340"),

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
                ESIOverlayColour = Color.FromRgb(188, 143, 143),
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
                JumpRangeInColourHighlight = Color.FromArgb(20, 82, 135, 155),
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
            ShowToolBox = true;
            ShowZKillData = true;
            ShowTrueSec = true;
            JumpRangeInAsOutline = true;
            ShowActiveIncursions = true;
            StaticJumpPoints = new ObservableCollection<StaticJumpOverlay>();
            SOVShowConflicts = true;
            SOVBasedITCU = true;
            UseESIForCharacterPositions = true;
            ShowCharacterNamesOnMap = true;
            ShowOfflineCharactersOnMap = true;
            ShowIhubVunerabilities = true;

            ShowJoveObservatories = true;
            ShowCynoBeacons = true;
            LimitESIDataToRegion = false;

            UniverseMaxZoomDisplaySystems = 1.3f;
            UniverseMaxZoomDisplaySystemsText = 2.0f;

            IntelSoundVolume = 0.5f;
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