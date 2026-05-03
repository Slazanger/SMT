using AvalonDock.Layout;
using Microsoft.Toolkit.Uwp.Notifications;
using NAudio.Wave;
using NHotkey;
using NHotkey.Wpf;
using SMT.EVEData;
using SMT.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using static SMT.EVEData.ZKillRedisQ;

namespace SMT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow AppWindow;
        private LogonWindow logonBrowserWindow;
        private List<Overlay> overlayWindows = new();
        private bool overlayWindowsAreClickTrough = false;

        public bool OverlayWindowsAreClickTrough
        {
            get => overlayWindowsAreClickTrough;
        }

        private PreferencesWindow preferencesWindow;

        private int uiRefreshCounter = 0;
        private int anomRefreshCounter = 0;
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        private bool manualZKillFilterRefreshRequired = true;

        private List<InfoItem> InfoLayer;

        private Dictionary<string, long> CharacterNameIDCache;
        private Dictionary<long, string> CharacterIDNameCache;

        public JumpRoute CapitalRoute { get; set; }

        public EventHandler OnSelectedCharChangedEventHandler;

        private System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();

        private readonly string WindowLayoutVersion = "03";

        private IWavePlayer waveOutEvent;
        private AudioFileReader audioFileReader;
        private ObservableCollection<EVEData.IntelData> IntelCache;
        private ObservableCollection<EVEData.GameLogData> GameLogCache;

        private bool zkbFilterByRegion = true;

        private bool sovCampaignFilterByRegion = false;

        /// <summary>
        /// Anom Manager
        /// </summary>
        public EVEData.AnomManager ANOMManager { get; set; }

        /// <summary>
        /// Main Region Manager
        /// </summary>
        public EVEData.EveManager EVEManager { get; set; }

        /// <summary>
        /// Main Map Config
        /// </summary>
        public MapConfig MapConf { get; }

        private AvalonDock.Layout.LayoutDocument RegionLayoutDoc { get; set; }

        private AvalonDock.Layout.LayoutDocument UniverseLayoutDoc { get; set; }
        // Property now automatically fires an event when the active character changes.
        private EVEData.LocalCharacter activeCharacter;

        public EVEData.LocalCharacter ActiveCharacter
        { get => activeCharacter; set { activeCharacter = value; OnSelectedCharChangedEventHandler?.Invoke(this, EventArgs.Empty); } }
        public void UpdateTabTitles()
        {
            try
            {
                // Refresh region map document title
                if (RegionUC != null && RegionUC.Region != null)
                {
                    RegionLayoutDoc.Title = RegionUC.Region.LocalizedName;
                }
                else
                {
                    RegionLayoutDoc.Title = Application.Current.TryFindResource("Main_Panel_Region") as string;
                }

                // Refresh universe map document title
                UniverseLayoutDoc.Title = Application.Current.TryFindResource("Main_Panel_Universe") as string;

                // Refresh regions overview document title
                var regionsDoc = dockManager.Layout.Descendents().OfType<AvalonDock.Layout.LayoutDocument>().FirstOrDefault(d => d.ContentId == "UniverseContentID");
                if (regionsDoc != null)
                {
                    regionsDoc.Title = Application.Current.TryFindResource("Main_Panel_Regions") as string;
                }
                var anchorables = dockManager.Layout.Descendents().OfType<AvalonDock.Layout.LayoutAnchorable>();
                foreach (var panel in anchorables)
                {
                    switch (panel.ContentId)
                    {
                        case "AnomsContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_Anoms") as string; break;
                        case "CharactersContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_Characters") as string; break;
                        case "RouteContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_Route") as string; break;
                        case "JumpRouteContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_JumpPlanner") as string; break;
                        case "TheraContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_WH") as string; break;
                        case "StormContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_Storms") as string; break;
                        case "SOVCampaignsID": panel.Title = Application.Current.TryFindResource("Main_Panel_SOV") as string; break;
                        case "ZKBContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_ZKB") as string; break;
                        case "IntelContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_Intel") as string; break;
                        case "GameLogContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_Gamelog") as string; break;
                        case "FleetContentID": panel.Title = Application.Current.TryFindResource("Main_Panel_Fleet") as string; break;
                    }
                }
            }
            catch { }
        }
        public MainWindow()
        {
            AppWindow = this;
            DataContext = this;

            this.FontFamily = new FontFamily(new Uri("pack://application:,,,/External/AtkinsonHyperlegible/"), "./#Atkinson Hyperlegible");

            Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");


            waveOutEvent = new WaveOutEvent { DeviceNumber = -1 };

            audioFileReader = new AudioFileReader(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");

            try
            {
                waveOutEvent.Init(audioFileReader);
            }
            catch
            {
                // wave output fails on some devices; try falling back to dsound
                waveOutEvent = new DirectSoundOut();
                waveOutEvent.Init(audioFileReader);
            }


            CharacterNameIDCache = new Dictionary<string, long>();
            CharacterIDNameCache = new Dictionary<long, string>();

            InitializeComponent();

            Title = $"SMT : {EveAppConfig.SMT_TITLE} ({EveAppConfig.SMT_VERSION})";

            // Load the Dock Manager Layout file
            string dockManagerLayoutName = Path.Combine(EveAppConfig.StorageRoot, "Layout_" + WindowLayoutVersion + ".dat");
            if(File.Exists(dockManagerLayoutName) && OperatingSystem.IsWindows())
            {
                try
                {
                    AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new(dockManager);
                    using(var sr = new StreamReader(dockManagerLayoutName))
                    {
                        ls.Deserialize(sr);
                    }
                }
                catch
                {
                }
            }

            // Due to bugs in the Dock manager patch up the content id's for the 2 main views
            RegionLayoutDoc = FindDocWithContentID(dockManager.Layout, "MapRegionContentID");
            UniverseLayoutDoc = FindDocWithContentID(dockManager.Layout, "FullUniverseViewID");

            // load any custom map settings off disk
            string mapConfigFileName = Path.Combine(EveAppConfig.StorageRoot, "MapConfig_" + MapConfig.SaveVersion + ".dat");

            if(File.Exists(mapConfigFileName))
            {
                try
                {
                    XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
                    FileStream fs = new FileStream(mapConfigFileName, FileMode.Open);
                    XmlReader xmlr = XmlReader.Create(fs);

                    MapConf = (MapConfig)xms.Deserialize(xmlr);
                    fs.Close();
                }
                catch
                {
                    MapConf = new MapConfig();
                    MapConf.SetDefaultColours();
                }
            }
            else
            {
                MapConf = new MapConfig();
                MapConf.SetDefaultColours();
            }

            if(MapConf.AlwaysOnTop)
            {
                this.Topmost = true;
            }

            // Create the main EVE manager

            CapitalRoute = new JumpRoute();

            EVEManager = new EVEData.EveManager(EveAppConfig.SMT_VERSION);
            EVEData.EveManager.Instance = EVEManager;
            
            // Set up UI thread marshaling for ObservableCollection operations
            EVEData.EveManager.UIThreadInvoker = (action) =>
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(action);
                }
            };
            
            EVEManager.EVELogFolder = MapConf.CustomEveLogFolderLocation;

            EVEManager.UseESIForCharacterPositions = MapConf.UseESIForCharacterPositions;

            // if we want to re-build the data as we've changed the format, recreate it all from scratch
            bool initFromScratch = false;

            if(initFromScratch)
            {
                SaveDefaultLayout();
                return;
            }
            else
            {
                EVEManager.LoadFromDisk();
            }

            EVEManager.SetupIntelWatcher();
            EVEManager.SetupGameLogWatcher();
            EVEManager.SetupLogFileTriggers();

            IntelCache = new ObservableCollection<IntelData>();
            RawIntelBox.ItemsSource = IntelCache;

            GameLogCache = new ObservableCollection<GameLogData>();
            RawGameDataBox.ItemsSource = GameLogCache;

            // add test intel with debug info
            IntelData id = new IntelData("[00:00] blah.... > blah", "System");
            id.IntelString = "Intel Watcher monitoring : " + EVEManager.EVELogFolder + @"\Chatlogs\";

            IntelData idtwo = new IntelData("[00:00] blah.... > blah", "System");
            idtwo.IntelString = "Intel Filters : " + String.Join(",", EVEManager.IntelFilters);

            IntelData idthree = new IntelData("[00:00] blah... > blah", "System");
            idthree.IntelString = "Intel Alert Filters : " + String.Join(", ", EVEManager.IntelAlertFilters);

            IntelData idfour = new IntelData("[00:00] blah... > blah", "System");
            idfour.IntelString = "Intel Alert Filters Count: " + EVEManager.IntelAlertFilters.Count();

            IntelCache.Add(id);
            IntelCache.Add(idtwo);
            IntelCache.Add(idthree);
            IntelCache.Add(idfour);

            MapConf.CurrentEveLogFolderLocation = EVEManager.EVELogFolder;

            EVEManager.ZKillFeed.KillExpireTimeMinutes = MapConf.ZkillExpireTimeMinutes;

            // load jump bridge data
            EVEManager.LoadJumpBridgeData();
            EVEManager.InitNavigation();

            EVEManager.UpdateMetaliminalStorms();

            EVEManager.LocalCharacterUpdateEvent += LocalCharacters_CollectionChanged;

            CharactersList.ItemsSource = EVEManager.LocalCharacters;
            CurrentActiveCharacterCombo.ItemsSource = EVEManager.LocalCharacters;

            FleetMembersList.DataContext = this;

            TheraConnectionsList.ItemsSource = EVEManager.TheraConnections;
            EVEManager.TheraUpdateEvent += TheraConnections_CollectionChanged;

            TurnurConnectionsList.ItemsSource = EVEManager.TurnurConnections;
            EVEManager.TurnurUpdateEvent += TurnurConnections_CollectionChanged;

            MetaliminalStormList.ItemsSource = EVEManager.MetaliminalStorms;
            EVEManager.StormsUpdateEvent += Storms_CollectionChanged;

            SovCampaignList.ItemsSource = EVEManager.ActiveSovCampaigns;
            CollectionView sovCampaignView = (CollectionView)CollectionViewSource.GetDefaultView(SovCampaignList.ItemsSource);
            sovCampaignView.Filter = item => SovCampaignFilter(item);
            EVEManager.SovUpdateEvent += ActiveSovCampaigns_CollectionChanged;

            LoadInfoObjects();

            // load any custom universe view layout
            // Save any custom map Layout

            string customLayoutFile = Path.Combine(EveAppConfig.VersionStorage, "CustomUniverseLayout.txt");

            if(File.Exists(customLayoutFile))
            {
                try
                {
                    using(TextReader tr = new StreamReader(customLayoutFile))
                    {
                        string line = tr.ReadLine();

                        while(line != null)
                        {
                            string[] bits = line.Split(',');
                            string region = bits[0];
                            string system = bits[1];
                            double x = double.Parse(bits[2]);
                            double y = double.Parse(bits[3]);

                            EVEData.System sys = EVEManager.GetEveSystem(system);
                            if(sys != null)
                            {
                                sys.UniverseX = x;
                                sys.UniverseY = y;
                                sys.CustomUniverseLayout = true;
                            }

                            line = tr.ReadLine();
                        }
                    }
                }
                catch { }
            }

            // load the anom data
            string anomDataFilename = EVEManager.SaveDataVersionFolder + @"\Anoms.dat";
            if(File.Exists(anomDataFilename))
            {
                try
                {
                    XmlSerializer xms = new XmlSerializer(typeof(EVEData.AnomManager));

                    FileStream fs = new FileStream(anomDataFilename, FileMode.Open);
                    XmlReader xmlr = XmlReader.Create(fs);

                    ANOMManager = (EVEData.AnomManager)xms.Deserialize(xmlr);
                    fs.Close();
                }
                catch
                {
                    ANOMManager = new EVEData.AnomManager();
                }
            }
            else
            {
                ANOMManager = new EVEData.AnomManager();
            }

            RegionUC.MapConf = MapConf;
            RegionUC.ANOMManager = ANOMManager;
            RegionUC.Init();
            RegionUC.SelectRegion(MapConf.DefaultRegion);

            RegionUC.RegionChanged += RegionUC_RegionChanged;
            RegionUC.UniverseSystemSelect += RegionUC_UniverseSystemSelect;

            RegionUC.SystemHoverEvent += RegionUC_SystemHoverEvent; ;

            UniverseUC.MapConf = MapConf;
            UniverseUC.CapitalRoute = CapitalRoute;
            UniverseUC.Init();
            UniverseUC.RequestRegionSystem += UniverseUC_RequestRegionSystem;

            RegionsViewUC.MapConf = MapConf;
            RegionsViewUC.Init();
            RegionsViewUC.RequestRegion += RegionsViewUC_RequestRegion;

            AppStatusBar.DataContext = EVEManager.ServerInfo;

            List<EVEData.System> globalSystemList = new List<EVEData.System>(EVEManager.Systems);
            globalSystemList.Sort((a, b) => string.Compare(a.Name, b.Name));
            RouteSystemDropDownAC.ItemsSource = globalSystemList;
            JumpRouteSystemDropDownAC.ItemsSource = globalSystemList;
            JumpRouteAvoidSystemDropDownAC.ItemsSource = globalSystemList;

            MapConf.PropertyChanged += MapConf_PropertyChanged;

            Closing += MainWindow_Closing;
            Closed += MainWindow_Closed;
            StateChanged += MainWindow_StateChanged;

            EVEManager.IntelUpdatedEvent += OnIntelUpdated;
            EVEManager.GameLogAddedEvent += OnGamelogUpdated;
            EVEManager.ShipDecloakedEvent += OnShipDecloaked;
            EVEManager.CombatEvent += OnCombatEvent;

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 1);
            uiRefreshTimer.Start();

            ZKBFeed.ItemsSource = EVEManager.ZKillFeed.KillStream;

            CollectionView zKBFeedview = (CollectionView)CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource);
            manualZKillFilterRefreshRequired = true;

            // Define your existing filter logic
            Predicate<object> initialFilter = item => ZKBFeedFilter(item);

            // Apply the both filters
            zKBFeedview.Filter = item =>
            {
                // copy the list before checking
                var listToFilter = EVEManager.ZKillFeed.KillStream.ToList();
                var filteredItems = listToFilter.Where(initialFilter.Invoke).Take(30).ToList();
                return filteredItems.Contains((ZKBDataSimple)item);
            };

            EVEManager.ZKillFeed.KillsAddedEvent += OnZKillsAdded;

            foreach(EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
            {
                lc.Location = "";
            }

            // Listen to notification activation and select the character if clicked on
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                if(args.Contains("character"))
                {
                    string charName = args["character"];

                    foreach(EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                    {
                        if(lc.Name == charName)
                        {
                            // Need to dispatch to UI thread if performing UI operations
                            Application.Current.Dispatcher.Invoke(delegate
                            {
                                ActiveCharacter = lc;
                                CurrentActiveCharacterCombo.SelectedItem = lc;

                                FleetMembersList.ItemsSource = lc.FleetInfo.Members;
                                lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
                                lc.FleetUpdatedEvent += OnFleetMemebersUpdate;

                                lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;
                                lc.RouteUpdatedEvent += OnCharacterRouteUpdate;

                                CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();

                                RegionUC.FollowCharacter = true;
                                RegionUC.SelectSystem(lc.Location, true);

                                UniverseUC.FollowCharacter = true;
                                UniverseUC.UpdateActiveCharacter(lc);
                            });

                            break;
                        }
                    }
                }
            };

            using(System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess())
            {
                nIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(currentProcess.MainModule.FileName);
            }
            nIcon.Visible = false;
            nIcon.Text = "SMT";
            nIcon.DoubleClick += NIcon_DClick;
            nIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            nIcon.ContextMenuStrip.Items.Add("Show", null, NIcon_DClick);
            nIcon.ContextMenuStrip.Items.Add("Exit", null, NIcon_Exit);

            CheckGitHubVersion();

            RegionUC.SelectRegion(MapConf.DefaultRegion);
        }
    }
}
