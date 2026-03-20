using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SMT.EVEData;
using SMT.ResourceUsage;

namespace SMT
{
    /// <summary>
    /// Interaction logic for RegionControl.xaml
    /// </summary>
    public partial class RegionControl : UserControl, INotifyPropertyChanged
    {
        public static readonly RoutedEvent UniverseSystemSelectEvent = EventManager.RegisterRoutedEvent("UniverseSystemSelect", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UniverseControl));
        private const int SYSTEM_LINK_INDEX = 19;
        private const double SYSTEM_REGION_TEXT_WIDTH = 100;
        private const double SYSTEM_REGION_TEXT_X_OFFSET = -SYSTEM_REGION_TEXT_WIDTH / 2;
        private const double SYSTEM_REGION_TEXT_Y_OFFSET = SYSTEM_TEXT_Y_OFFSET + SYSTEM_TEXT_TEXT_SIZE + 3;
        private const double SYSTEM_SHAPE_OFFSET = SYSTEM_SHAPE_SIZE / 2;
        private const double SYSTEM_SHAPE_SIZE = 18;
        private const double SYSTEM_TEXT_TEXT_SIZE = 6;
        private const double SYSTEM_SHAPE_OOR_SIZE = 14;
        private const double SYSTEM_SHAPE_OOR_OFFSET = SYSTEM_SHAPE_OOR_SIZE / 2;

        private const int SYSTEM_TEXT_WIDTH = 100;
        private const int SYSTEM_TEXT_HEIGHT = 50;
        private const double SYSTEM_TEXT_X_OFFSET = SYSTEM_TEXT_WIDTH / 2;
        private const double SYSTEM_TEXT_Y_OFFSET = SYSTEM_TEXT_HEIGHT / 2;

        // depth order of data
        private const int ZINDEX_CHARACTERS = 140;

        private const int ZINDEX_POI = 113;
        private const int ZINDEX_SOV_FIGHT_LOGO = 105;
        private const int ZINDEX_CYNOBEACON = 105;
        private const int ZINDEX_TEXT = 101;
        private const int ZINDEX_SYSTEM = 100;
        private const int ZINDEX_SYSTEM_OUTLINE = 99;
        private const int ZINDEX_SOV_FIGHT_SHAPE = 97;
        private const int ZINDEX_THERA = 97;
        private const int ZINDEX_TURNER = 97;
        private const int ZINDEX_STORM = 95;
        private const int ZINDEX_TRIG = 97;
        private const int ZINDEX_RANGEMARKER = 96;
        private const int ZINDEX_SYSICON = 100;
        private const int ZINDEX_ADM = 99;
        private const int ZINDEX_POLY = 98;
        private const int ZINDEX_JOVE = 105;

        private const int THERA_Z_INDEX = 22;

        private readonly Brush SelectedAllianceBrush = new SolidColorBrush(Color.FromArgb(180, 200, 200, 200));
        private Dictionary<string, EVEData.EveManager.JumpShip> activeJumpSpheres;
        private string currentCharacterJumpSystem;
        private string currentJumpCharacter;

        // Store the Dynamic Map elements so they can seperately be cleared
        private List<System.Windows.UIElement> DynamicMapElements;

        private List<System.Windows.UIElement> DynamicMapElementsSysLinkHighlight;
        private List<System.Windows.UIElement> DynamicMapElementsCharacters;
        private List<System.Windows.UIElement> DynamicMapElementsJBHighlight;
        private List<System.Windows.UIElement> DynamicMapElementsRangeMarkers;
        private List<System.Windows.UIElement> DynamicMapElementsRouteHighlight;
        private System.Windows.Media.Imaging.BitmapImage edencomLogoImage;
        private System.Windows.Media.Imaging.BitmapImage fightImage;
        private System.Windows.Media.Imaging.BitmapImage joveLogoImage;
        private System.Windows.Media.Imaging.BitmapImage stormImageBase;
        private System.Windows.Media.Imaging.BitmapImage stormImageEM;
        private System.Windows.Media.Imaging.BitmapImage stormImageExp;
        private System.Windows.Media.Imaging.BitmapImage stormImageKin;
        private System.Windows.Media.Imaging.BitmapImage stormImageTherm;

        private EVEData.EveManager.JumpShip jumpShipType;
        private LocalCharacter m_ActiveCharacter;

        // Map Controls
        private double m_ESIOverlayScale = 1.0f;

        private bool m_ShowJumpBridges = true;
        private bool m_ShowNPCKills;
        private bool m_ShowPodKills;
        private bool m_ShowShipJumps;
        private bool m_ShowShipKills;
        private bool m_ShowSovOwner;
        private bool m_ShowStandings;
        private bool m_ShowSystemADM;
        private bool m_ShowSystemSecurity;
        private bool m_ShowSystemTimers;
        private Dictionary<string, List<KeyValuePair<int, string>>> NameTrackingLocationMap = new Dictionary<string, List<KeyValuePair<int, string>>>();
        private long SelectedAlliance;
        private bool showJumpDistance;
        private Brush StandingBadBrush = new SolidColorBrush(Color.FromArgb(110, 196, 72, 6));
        private Brush StandingGoodBrush = new SolidColorBrush(Color.FromArgb(110, 43, 101, 196));
        private Brush StandingNeutBrush = new SolidColorBrush(Color.FromArgb(110, 140, 140, 140));

        // Constant Colours
        private Brush StandingVBadBrush = new SolidColorBrush(Color.FromArgb(110, 148, 5, 5));
        private Brush StandingVGoodBrush = new SolidColorBrush(Color.FromArgb(110, 5, 34, 120));

        /// <summary>Standing tier colours for map tickers: same semantic tiers as the kill feed, higher luminance for dark map backgrounds.</summary>
        private static readonly SolidColorBrush TickerStandingTerribleBrush;
        private static readonly SolidColorBrush TickerStandingBadBrush;
        private static readonly SolidColorBrush TickerStandingGoodBrush;
        private static readonly SolidColorBrush TickerStandingExcellentBrush;

        static RegionControl()
        {
            TickerStandingTerribleBrush = new SolidColorBrush(Color.FromRgb(255, 95, 95));
            TickerStandingBadBrush = new SolidColorBrush(Color.FromRgb(255, 184, 77));
            TickerStandingGoodBrush = new SolidColorBrush(Color.FromRgb(110, 220, 255));
            TickerStandingExcellentBrush = new SolidColorBrush(Color.FromRgb(140, 180, 255));
            TickerStandingTerribleBrush.Freeze();
            TickerStandingBadBrush.Freeze();
            TickerStandingGoodBrush.Freeze();
            TickerStandingExcellentBrush.Freeze();
        }

        private List<Point> SystemIcon_Astrahaus = new List<Point>
        {
            new Point(6,12),
            new Point(6,7),
            new Point(9,7),
            new Point(9,4),
            //new Point(10,4),
            new Point(9,7),
            new Point(12,7),
            new Point(12,12),
        };

        private List<Point> SystemIcon_Fortizar = new List<Point>
        {
            new Point(4,12),
            new Point(4,7),
            new Point(6,7),
            new Point(6,5),
            new Point(12,5),
            new Point(12,7),
            new Point(14,7),
            new Point(14,12),
        };

        private List<Point> SystemIcon_Keepstar = new List<Point>
        {
            new Point(1,17),
            new Point(1,0),
            new Point(7,0),
            new Point(7,7),
            new Point(12,7),
            new Point(12,0),
            new Point(18,0),
            new Point(18,17),
        };

        private System.Windows.Media.Imaging.BitmapImage trigLogoImage;

        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        // events

        /// <summary>
        /// Intel Updated Event Handler
        /// </summary>
        public delegate void SystemHover(string system);

        /// <summary>
        /// Intel Updated Event
        /// </summary>
        public event SystemHover SystemHoverEvent;

        /// <summary>
        /// Constructor
        /// </summary>
        public RegionControl()
        {
            InitializeComponent();
            DataContext = this;

            activeJumpSpheres = new Dictionary<string, EVEData.EveManager.JumpShip>();

            joveLogoImage = ResourceLoader.LoadBitmapFromResource("Images/Jove_logo.png");
            trigLogoImage = ResourceLoader.LoadBitmapFromResource("Images/TrigTile.png");
            edencomLogoImage = ResourceLoader.LoadBitmapFromResource("Images/edencom.png");
            fightImage = ResourceLoader.LoadBitmapFromResource("Images/fight.png");
            stormImageBase = ResourceLoader.LoadBitmapFromResource("Images/cloud_unknown.png");
            stormImageEM = ResourceLoader.LoadBitmapFromResource("Images/cloud_em.png");
            stormImageExp = ResourceLoader.LoadBitmapFromResource("Images/cloud_explosive.png");
            stormImageKin = ResourceLoader.LoadBitmapFromResource("Images/cloud_kinetic.png");
            stormImageTherm = ResourceLoader.LoadBitmapFromResource("Images/cloud_thermal.png");

            helpIcon.MouseLeftButtonDown += HelpIcon_MouseLeftButtonDown;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangedEventHandler RegionChanged;

        public event RoutedEventHandler UniverseSystemSelect
        {
            add { AddHandler(UniverseSystemSelectEvent, value); }
            remove { RemoveHandler(UniverseSystemSelectEvent, value); }
        }

        public LocalCharacter ActiveCharacter
        {
            get
            {
                return m_ActiveCharacter;
            }
            set
            {
                m_ActiveCharacter = value;
                OnPropertyChanged("ActiveCharacter");
            }
        }

        public AnomManager ANOMManager { get; set; }

        public EveManager EM { get; set; }

        public double ESIOverlayScale
        {
            get
            {
                return m_ESIOverlayScale;
            }
            set
            {
                m_ESIOverlayScale = value;
                OnPropertyChanged("ESIOverlayScale");
            }
        }

        public bool FollowCharacter
        {
            get
            {
                return FollowCharacterChk.IsChecked.Value;
            }
            set
            {
                FollowCharacterChk.IsChecked = value;
            }
        }

        public MapConfig MapConf { get; set; }

        public EVEData.MapRegion Region { get; set; }

        public string SelectedSystem { get; set; }

        public bool ShowJumpBridges
        {
            get
            {
                return m_ShowJumpBridges;
            }
            set
            {
                m_ShowJumpBridges = value;
                OnPropertyChanged("ShowJumpBridges");
            }
        }

        public bool ShowNPCKills
        {
            get
            {
                return m_ShowNPCKills;
            }

            set
            {
                m_ShowNPCKills = value;

                if(m_ShowNPCKills)
                {
                    ShowPodKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowNPCKills");
            }
        }

        public bool ShowPodKills
        {
            get
            {
                return m_ShowPodKills;
            }

            set
            {
                m_ShowPodKills = value;
                if(m_ShowPodKills)
                {
                    ShowNPCKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowPodKills");
            }
        }

        public bool ShowShipJumps
        {
            get
            {
                return m_ShowShipJumps;
            }

            set
            {
                m_ShowShipJumps = value;
                if(m_ShowShipJumps)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipKills = false;
                }

                OnPropertyChanged("ShowShipJumps");
            }
        }

        public bool ShowShipKills
        {
            get
            {
                return m_ShowShipKills;
            }

            set
            {
                m_ShowShipKills = value;
                if(m_ShowShipKills)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowShipKills");
            }
        }

        public bool ShowSovOwner
        {
            get
            {
                return m_ShowSovOwner;
            }
            set
            {
                m_ShowSovOwner = value;
                OnPropertyChanged("ShowSovOwner");
            }
        }

        public bool ShowStandings
        {
            get
            {
                return m_ShowStandings;
            }
            set
            {
                m_ShowStandings = value;

                OnPropertyChanged("ShowStandings");
            }
        }

        public bool ShowSystemADM
        {
            get
            {
                return m_ShowSystemADM;
            }
            set
            {
                m_ShowSystemADM = value;
                if(m_ShowSystemADM)
                {
                    ShowSystemSecurity = false;
                }
                OnPropertyChanged("ShowSystemADM");
            }
        }

        public bool ShowSystemSecurity
        {
            get
            {
                return m_ShowSystemSecurity;
            }
            set
            {
                m_ShowSystemSecurity = value;
                if(m_ShowSystemSecurity)
                {
                    ShowSystemADM = false;
                }
                OnPropertyChanged("ShowSystemSecurity");
            }
        }

        public bool ShowSystemTimers
        {
            get
            {
                return m_ShowSystemTimers;
            }
            set
            {
                m_ShowSystemTimers = value;
                OnPropertyChanged("ShowSystemTimers");
            }
        }

        public List<InfoItem> InfoLayer { get; set; }

    }
}
