using AvalonDock.Layout;
using Microsoft.Toolkit.Uwp.Notifications;
using NAudio.Wave;
using NHotkey;
using NHotkey.Wpf;
using SMT.EVEData;
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
            /// <summary>
            /// Main Window
            /// </summary>
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

        private void RegionUC_SystemHoverEvent(string system)
        {
            RawIntelBox.SelectedItem = null;

            // iterate over all of the RawIntelBox ui items
            for(int i = 0; i < RawIntelBox.Items.Count; i++)
            {
                IntelData id = RawIntelBox.Items[i] as IntelData;
                if(id != null)
                {
                    if(system != string.Empty && id.Systems.Contains(system))
                    {
                        // highlight the item
                        ListViewItem lvi = RawIntelBox.ItemContainerGenerator.ContainerFromItem(id) as ListViewItem;
                        if(lvi != null)
                        {
                            lvi.Background = Brushes.DarkRed;
                        }
                    }
                    else
                    {
                        // remove the highlight
                        ListViewItem lvi = RawIntelBox.ItemContainerGenerator.ContainerFromItem(id) as ListViewItem;
                        if(lvi != null)
                        {
                            lvi.Background = Brushes.Transparent;
                        }
                    }
                }
            }
        }

        private void OnGamelogUpdated(List<EVEData.GameLogData> gll)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                List<GameLogData> removeList = new List<GameLogData>();
                List<GameLogData> addList = new List<GameLogData>();

                // remove old

                if(GameLogCache.Count > 50)
                {
                    foreach(GameLogData gl in GameLogCache)
                    {
                        if(!gll.Contains(gl))
                        {
                            removeList.Add(gl);
                        }
                    }

                    foreach(GameLogData gl in removeList)
                    {
                        GameLogCache.Remove(gl);
                    }
                }

                // add new
                foreach(GameLogData gl in gll)
                {
                    if(!GameLogCache.Contains(gl))
                    {
                        GameLogCache.Insert(0, gl);
                    }
                }
            }), DispatcherPriority.Normal);
        }

        private void Storms_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(MetaliminalStormList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void TheraConnections_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(TheraConnectionsList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void TurnurConnections_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(TurnurConnectionsList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowPlacement.SetPlacement(new WindowInteropHelper(this).Handle, Properties.Settings.Default.MainWindow_placement);
        }

        private void SaveDefaultLayout()
        {
            // first delete the existing
            string defaultLayoutFile = AppDomain.CurrentDomain.BaseDirectory + @"\DefaultWindowLayout.dat";

            if(File.Exists(defaultLayoutFile))
            {
                File.Delete(defaultLayoutFile);
            }

            try
            {
                if(OperatingSystem.IsWindows())
                {
                    AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                    using(var sw = new StreamWriter(defaultLayoutFile))
                    {
                        ls.Serialize(sw);
                    }
                }
            }
            catch
            {
            }
        }

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

        private void ActiveSovCampaigns_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(SovCampaignList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void LocalCharacters_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(CharactersList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void Exit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private AvalonDock.Layout.LayoutDocument FindDocWithContentID(AvalonDock.Layout.ILayoutElement root, string contentID)
        {
            AvalonDock.Layout.LayoutDocument content = null;

            if(root is AvalonDock.Layout.ILayoutContainer)
            {
                AvalonDock.Layout.ILayoutContainer ic = root as AvalonDock.Layout.ILayoutContainer;
                foreach(AvalonDock.Layout.ILayoutElement ie in ic.Children)
                {
                    AvalonDock.Layout.LayoutDocument f = FindDocWithContentID(ie, contentID);
                    if(f != null)
                    {
                        content = f;
                        break;
                    }
                }
            }
            else
            {
                if(root is AvalonDock.Layout.LayoutDocument)
                {
                    AvalonDock.Layout.LayoutDocument i = root as AvalonDock.Layout.LayoutDocument;
                    if(i.ContentId == contentID)
                    {
                        content = i;
                    }
                }
            }
            return content;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if(MapConf.CloseToTray && MapConf.MinimizeToTray && AppWindow.ShowInTaskbar != false)
            {
                e.Cancel = true;
                AppWindow.Hide();
                AppWindow.ShowInTaskbar = false;
                nIcon.Visible = true;
                return;
            }

            // Store the main window position and size

            Properties.Settings.Default.MainWindow_placement = WindowPlacement.GetPlacement(new WindowInteropHelper(AppWindow).Handle);
            Properties.Settings.Default.Save();

            EVEManager.ZKillFeed.KillsAddedEvent -= OnZKillsAdded;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // save off the dockmanager layout
            string dockManagerLayoutName = Path.Combine(EveAppConfig.StorageRoot, "Layout_" + WindowLayoutVersion + ".dat");

            try
            {
                AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                using(var sw = new StreamWriter(dockManagerLayoutName))
                {
                    ls.Serialize(sw);
                }
            }
            catch
            {
            }

            try
            {
                // Save off any explicit items
                MapConf.UseESIForCharacterPositions = EVEManager.UseESIForCharacterPositions;

                // Save the Map Colours
                string mapConfigFileName = Path.Combine(EveAppConfig.StorageRoot, "MapConfig_" + MapConfig.SaveVersion + ".dat");

                // save off the toolbar setup
                MapConf.ToolBox_ShowJumpBridges = RegionUC.ShowJumpBridges;
                MapConf.ToolBox_ShowNPCKills = RegionUC.ShowNPCKills;
                MapConf.ToolBox_ShowPodKills = RegionUC.ShowPodKills;
                MapConf.ToolBox_ShowShipJumps = RegionUC.ShowShipJumps;
                MapConf.ToolBox_ShowShipKills = RegionUC.ShowShipKills;
                MapConf.ToolBox_ShowSovOwner = RegionUC.ShowSovOwner;
                MapConf.ToolBox_ShowStandings = RegionUC.ShowStandings;
                MapConf.ToolBox_ShowSystemADM = RegionUC.ShowSystemADM;
                MapConf.ToolBox_ShowSystemSecurity = RegionUC.ShowSystemSecurity;
                MapConf.ToolBox_ShowSystemTimers = RegionUC.ShowSystemTimers;
                MapConf.ToolBox_ESIOverlayScale = RegionUC.ESIOverlayScale;

                // now serialise the class to disk
                XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
                using(TextWriter tw = new StreamWriter(mapConfigFileName))
                {
                    xms.Serialize(tw, MapConf);
                }

                // Save any custom map Layout
                string customLayoutFile = Path.Combine(EveAppConfig.VersionStorage, "CustomUniverseLayout.txt");

                using(TextWriter tw = new StreamWriter(customLayoutFile))
                {
                    foreach(EVEData.System s in EVEManager.Systems)
                    {
                        if(s.CustomUniverseLayout)
                        {
                            tw.WriteLine($"{s.Region},{s.Name},{s.UniverseX},{s.UniverseY}");
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                // save the Anom Data
                // now serialise the class to disk
                XmlSerializer anomxms = new XmlSerializer(typeof(EVEData.AnomManager));
                string anomDataFilename = EVEManager.SaveDataVersionFolder + @"\Anoms.dat";

                using(TextWriter tw = new StreamWriter(anomDataFilename))
                {
                    anomxms.Serialize(tw, ANOMManager);
                }
            }
            catch
            {
            }

            // save the character data
            EVEManager.SaveData();
            EVEManager.ShutDown();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if(this.WindowState == WindowState.Minimized)
            {
                if(MapConf.MinimizeToTray)
                {
                    AppWindow.Hide();
                    AppWindow.ShowInTaskbar = false;
                    nIcon.Visible = true;
                }
            }
        }

        private void NIcon_DClick(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.Visibility = Visibility.Visible;
            this.Show();
            this.WindowState = WindowState.Normal;
            nIcon.Visible = false;
            this.Activate();
        }

        private void NIcon_Exit(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void SovCampaignList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if(grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;

                    EVEData.SOVCampaign sc = dgr.Item as EVEData.SOVCampaign;

                    if(sc != null)
                    {
                        RegionUC.SelectSystem(sc.System, true);
                    }

                    if(RegionLayoutDoc != null)
                    {
                        RegionLayoutDoc.IsSelected = true;
                    }
                }
            }
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            uiRefreshCounter++;
            if(uiRefreshCounter == 5)
            {
                uiRefreshCounter = 0;

                if(FleetMembersList.ItemsSource != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
                    }), DispatcherPriority.Normal);
                }
            }
            if(MapConf.SyncActiveCharacterBasedOnActiveEVEClient)
            {
                UpdateCharacterSelectionBasedOnActiveWindow();
            }

            if(manualZKillFilterRefreshRequired)
            {
                try
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource).Refresh();
                    }), DispatcherPriority.Normal);
                }
                catch
                {
                    // ignore the error
                }

                manualZKillFilterRefreshRequired = false;
            }

            // refresh the anomalies datagrid every 60 seconds to update the "since" column
            anomRefreshCounter++;
            if(anomRefreshCounter == 60)
            {
                anomRefreshCounter = 0;
                AnomSigList.Items.Refresh();
            }
        }
    }

    /// <summary>
    /// TimeSpanConverter Sec statuc colour converter
    /// </summary>
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan ts = (TimeSpan)value;

            string Output = "";

            if(ts.Ticks < 0)
            {
                Output += "-";
            }

            if(ts.Days != 0)
            {
                Output += Math.Abs(ts.Days) + "d ";
            }

            if(ts.Hours != 0)
            {
                Output += Math.Abs(ts.Hours) + "h ";
            }

            Output += Math.Abs(ts.Minutes) + "m ";

            return Output;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// ZKillboard Sec statuc colour converter
    /// </summary>
    public class ZKBBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EVEData.ZKillRedisQ.ZKBDataSimple zs = value as EVEData.ZKillRedisQ.ZKBDataSimple;
            Color rowCol = (Color)ColorConverter.ConvertFromString("#FF333333");
            if(zs != null)
            {
                float Standing = 0.0f;

                EVEData.LocalCharacter c = MainWindow.AppWindow.RegionUC.ActiveCharacter;
                if(c != null && c.ESILinked)
                {
                    if(c.AllianceID != 0 && c.AllianceID == zs.VictimAllianceID)
                    {
                        Standing = 10.0f;
                    }

                    if(c.Standings.Keys.Contains(zs.VictimAllianceID))
                    {
                        Standing = c.Standings[zs.VictimAllianceID];
                    }

                    if(Standing == -10.0)
                    {
                        rowCol = Colors.Red;
                    }

                    if(Standing == -5.0)
                    {
                        rowCol = Colors.Orange;
                    }

                    if(Standing == 5.0)
                    {
                        rowCol = Colors.LightBlue;
                    }

                    if(Standing == 10.0)
                    {
                        rowCol = Colors.Blue;
                    }
                }

                // Do the conversion from bool to visibility
            }

            return new SolidColorBrush(rowCol);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// ZKillboard Sec statuc colour converter
    /// </summary>
    public class ZKBForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EVEData.ZKillRedisQ.ZKBDataSimple zs = value as EVEData.ZKillRedisQ.ZKBDataSimple;
            Color rowCol = Colors.White;
            if(zs != null)
            {
                float Standing = 0.0f;

                EVEData.LocalCharacter c = MainWindow.AppWindow.RegionUC.ActiveCharacter;
                if(c != null && c.ESILinked)
                {
                    if(c.AllianceID != 0 && c.AllianceID == zs.VictimAllianceID)
                    {
                        Standing = 10.0f;
                    }

                    if(c.Standings.Keys.Contains(zs.VictimAllianceID))
                    {
                        Standing = c.Standings[zs.VictimAllianceID];
                    }

                    if(Standing == -10.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if(Standing == -5.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if(Standing == 5.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if(Standing == 10.0)
                    {
                        rowCol = Colors.White;
                    }
                }

                // Do the conversion from bool to visibility
            }

            return new SolidColorBrush(rowCol);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// Convert time elapsed to simple human format
    /// </summary>
    public class TimeSinceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not DateTime timeFound)
                return "Unknown";

            var now = DateTime.Now;
            var elapsed = now - timeFound;

            if(elapsed.TotalDays >= 1.0 && elapsed.TotalDays < 2.0)
                return $"{(int)elapsed.TotalDays} day";
            else if(elapsed.TotalDays >= 2.0)
                return $"{(int)elapsed.TotalDays} days";

            if(elapsed.TotalHours >= 1.0 && elapsed.TotalHours < 2.0)
                return $"{(int)elapsed.TotalHours} hour";
            else if(elapsed.TotalHours >= 2.0)
                return $"{(int)elapsed.TotalHours} hours";

            if(elapsed.TotalMinutes < 1.0)
                return "Now";
            else if(elapsed.TotalMinutes < 2.0)
                return $"{(int)elapsed.TotalMinutes} minute";
            else
                return $"{(int)elapsed.TotalMinutes} minutes";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///  Window Flashing helper
    /// </summary>
    public static class FlashWindow
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            /// <summary>
            /// The size of the structure in bytes.
            /// </summary>
            public uint cbSize;

            /// <summary>
            /// A Handle to the Window to be Flashed. The window can be either opened or minimized.
            /// </summary>
            public IntPtr hwnd;

            /// <summary>
            /// The Flash Status.
            /// </summary>
            public uint dwFlags;

            /// <summary>
            /// The number of times to Flash the window.
            /// </summary>
            public uint uCount;

            /// <summary>
            /// The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
            /// </summary>
            public uint dwTimeout;
        }

        /// <summary>
        /// Flash both the window caption and taskbar button.
        /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
        /// </summary>
        public const uint FLASHW_ALL = 3;

        private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
        {
            FLASHWINFO fi = new FLASHWINFO();
            fi.cbSize = Convert.ToUInt32(System.Runtime.InteropServices.Marshal.SizeOf(fi));
            fi.hwnd = handle;
            fi.dwFlags = flags;
            fi.uCount = count;
            fi.dwTimeout = timeout;
            return fi;
        }

        /// <summary>
        /// Flash the specified Window for the specified number of times
        /// </summary>
        /// <param name="window">The Window to Flash.</param>
        /// <param name="count">The number of times to Flash.</param>
        /// <returns></returns>
        public static bool Flash(System.Windows.Window window, uint count)
        {
            if(Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(new System.Windows.Interop.WindowInteropHelper(window).Handle, FLASHW_ALL, count, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }

        /// <summary>
        /// A boolean value indicating whether the application is running on Windows 2000 or later.
        /// </summary>
        private static bool Win2000OrLater
        {
            get { return System.Environment.OSVersion.Version.Major >= 5; }
        }
    }
}
