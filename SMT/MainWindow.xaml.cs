using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using SMT.EVEData;

namespace SMT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow AppWindow;
        private LogonWindow logonBrowserWindow;
        private Overlay overlayWindow;

        private MediaPlayer mediaPlayer;
        private PreferencesWindow preferencesWindow;

        private int uiRefreshCounter = 0;
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        private List<InfoItem> InfoLayer;

        private Dictionary<string, long> CharacterNameIDCache;
        private Dictionary<long, string> CharacterIDNameCache;

        public JumpRoute CapitalRoute { get; set; }

        public EventHandler OnSelectedCharChangedEventHandler;

        System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();

        /// <summary>
        /// Main Window
        /// </summary>
        public MainWindow()
        {
            AppWindow = this;
            DataContext = this;

            mediaPlayer = new MediaPlayer();
            Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
            mediaPlayer.Open(woopUri);

            CharacterNameIDCache = new Dictionary<string, long>();
            CharacterIDNameCache = new Dictionary<long, string>();

            InitializeComponent();

            Title = $"SMT : {EveAppConfig.SMT_TITLE} ({EveAppConfig.SMT_VERSION})";

            // Load the Dock Manager Layout file
            string dockManagerLayoutName = EveAppConfig.VersionStorage + "Layout.dat";
            if (File.Exists(dockManagerLayoutName) && OperatingSystem.IsWindows())
            {
                try
                {
                    AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new(dockManager);
                    using (var sr = new StreamReader(dockManagerLayoutName))
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
            string mapConfigFileName = EveAppConfig.VersionStorage + "MapConfig.dat";

            if (File.Exists(mapConfigFileName))
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

            // Create the main EVE manager

            CapitalRoute = new JumpRoute();

            EVEManager = new EVEData.EveManager(EveAppConfig.SMT_VERSION);
            EVEData.EveManager.Instance = EVEManager;
            EVEManager.EVELogFolder = MapConf.CustomEveLogFolderLocation;

            EVEManager.UseESIForCharacterPositions = MapConf.UseESIForCharacterPositions;

            // if we want to re-build the data as we've changed the format, recreate it all from scratch
            bool initFromScratch = false;

            if (initFromScratch)
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
            RawGameDataBox.ItemsSource = EVEManager.GameLogList;

            // add test intel with debug info
            IntelData id = new IntelData("[00:00] blah.... > blah", "System");
            id.IntelString = "Intel Watcher monitoring : " + EVEManager.EVELogFolder + @"\Chatlogs\";

            IntelData idtwo = new IntelData("[00:00] blah.... > blah", "System");
            idtwo.IntelString = "Intel Filters : " + String.Join(",", EVEManager.IntelFilters);

            IntelCache.Add(id);
            IntelCache.Add(idtwo);

            MapConf.CurrentEveLogFolderLocation = EVEManager.EVELogFolder;

            EVEManager.ZKillFeed.KillExpireTimeMinutes = MapConf.ZkillExpireTimeMinutes;

            // load jump bridge data
            EVEManager.LoadJumpBridgeData();
            EVEManager.UpdateESIUniverseData();
            EVEManager.InitNavigation();

            EVEManager.UpdateMetaliminalStorms();

            EVEManager.LocalCharacterUpdateEvent += LocalCharacters_CollectionChanged;

            CharactersList.ItemsSource = EVEManager.LocalCharacters;
            CurrentActiveCharacterCombo.ItemsSource = EVEManager.LocalCharacters;

            FleetMembersList.DataContext = this;

            TheraConnectionsList.ItemsSource = EVEManager.TheraConnections;
            EVEManager.TheraUpdateEvent += TheraConnections_CollectionChanged;

            JumpBridgeList.ItemsSource = EVEManager.JumpBridges;
            MetaliminalStormList.ItemsSource = EVEManager.MetaliminalStorms;
            EVEManager.StormsUpdateEvent += Storms_CollectionChanged;

            SovCampaignList.ItemsSource = EVEManager.ActiveSovCampaigns;
            EVEManager.SovUpdateEvent += ActiveSovCampaigns_CollectionChanged;

            LoadInfoObjects();
            UpdateJumpBridgeSummary();

            // load any custom universe view layout
            // Save any custom map Layout

            string customLayoutFile = EveAppConfig.VersionStorage + "CustomUniverseLayout.txt";

            if (File.Exists(customLayoutFile))
            {
                try
                {
                    using (TextReader tr = new StreamReader(customLayoutFile))
                    {
                        string line = tr.ReadLine();

                        while (line != null)
                        {
                            string[] bits = line.Split(',');
                            string region = bits[0];
                            string system = bits[1];
                            double x = double.Parse(bits[2]);
                            double y = double.Parse(bits[3]);

                            EVEData.System sys = EVEManager.GetEveSystem(system);
                            if (sys != null)
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
            if (File.Exists(anomDataFilename))
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
            EVEManager.GameLogAddedEvent += OnGamelogAdded;
            EVEManager.ShipDecloakedEvent += OnShipDecloaked;
            EVEManager.CombatEvent += OnCombatEvent;

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 1);
            uiRefreshTimer.Start();

            ZKBFeed.ItemsSource = EVEManager.ZKillFeed.KillStream;

            CollectionView zKBFeedview = (CollectionView)CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource);
            zKBFeedview.Refresh();
            zKBFeedview.Filter = ZKBFeedFilter;

            EVEManager.ZKillFeed.KillsAddedEvent += OnZKillsAdded;

            foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
            {
                lc.Location = "";
            }

            // Listen to notification activation and select the character if clicked on
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                if (args.Contains("character"))
                {
                    string charName = args["character"];

                    foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                    {
                        if (lc.Name == charName)
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

            using (System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess())
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

        private void OnGamelogAdded()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(RawGameDataBox.ItemsSource).Refresh();
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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowPlacement.SetPlacement(new WindowInteropHelper(this).Handle, Properties.Settings.Default.MainWindow_placement);
        }

        private void SaveDefaultLayout()
        {
            // first delete the existing
            string defaultLayoutFile = AppDomain.CurrentDomain.BaseDirectory + @"\DefaultWindowLayout.dat";

            if (File.Exists(defaultLayoutFile))
            {
                File.Delete(defaultLayoutFile);
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                    using (var sw = new StreamWriter(defaultLayoutFile))
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

            if (root is AvalonDock.Layout.ILayoutContainer)
            {
                AvalonDock.Layout.ILayoutContainer ic = root as AvalonDock.Layout.ILayoutContainer;
                foreach (AvalonDock.Layout.ILayoutElement ie in ic.Children)
                {
                    AvalonDock.Layout.LayoutDocument f = FindDocWithContentID(ie, contentID);
                    if (f != null)
                    {
                        content = f;
                        break;
                    }
                }
            }
            else
            {
                if (root is AvalonDock.Layout.LayoutDocument)
                {
                    AvalonDock.Layout.LayoutDocument i = root as AvalonDock.Layout.LayoutDocument;
                    if (i.ContentId == contentID)
                    {
                        content = i;
                    }
                }
            }
            return content;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (MapConf.CloseToTray && MapConf.MinimizeToTray && AppWindow.ShowInTaskbar != false)
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

            string dockManagerLayoutName = EveAppConfig.VersionStorage + "Layout.dat";

            try
            {
                AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                using (var sw = new StreamWriter(dockManagerLayoutName))
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
                string mapConfigFileName = EveAppConfig.VersionStorage + "MapConfig.dat";

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

                // now serialise the class to disk
                XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
                using (TextWriter tw = new StreamWriter(mapConfigFileName))
                {
                    xms.Serialize(tw, MapConf);
                }

                // Save any custom map Layout
                string customLayoutFile = EveAppConfig.VersionStorage + "CustomUniverseLayout.txt";

                using (TextWriter tw = new StreamWriter(customLayoutFile))
                {
                    foreach (EVEData.System s in EVEManager.Systems)
                    {
                        if (s.CustomUniverseLayout)
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

                using (TextWriter tw = new StreamWriter(anomDataFilename))
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
            if (this.WindowState == WindowState.Minimized)
            {
                if (MapConf.MinimizeToTray)
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
            if (sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;

                    EVEData.SOVCampaign sc = dgr.Item as EVEData.SOVCampaign;

                    if (sc != null)
                    {
                        RegionUC.SelectSystem(sc.System, true);
                    }

                    if (RegionLayoutDoc != null)
                    {
                        RegionLayoutDoc.IsSelected = true;
                    }
                }
            }
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            uiRefreshCounter++;
            if (uiRefreshCounter == 5)
            {
                uiRefreshCounter = 0;

                if (FleetMembersList.ItemsSource != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();

                    }), DispatcherPriority.Normal);
                }
            }
            if (MapConf.SyncActiveCharacterBasedOnActiveEVEClient)
            {
                UpdateCharacterSelectionBasedOnActiveWindow();
            }
        }

        #region RegionsView Control

        private void RegionsViewUC_RequestRegion(object sender, RoutedEventArgs e)
        {
            string regionName = e.OriginalSource as string;
            RegionUC.FollowCharacter = false;
            RegionUC.SelectRegion(regionName);

            if (RegionLayoutDoc != null)
            {
                RegionLayoutDoc.IsSelected = true;
            }
        }

        #endregion RegionsView Control

        #region Region Control

        private void MapConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AlwaysOnTop")
            {
                if (MapConf.AlwaysOnTop)
                {
                    this.Topmost = true;
                }
                else
                {
                    this.Topmost = false;
                }
            }

            if (e.PropertyName == "ShowZKillData")
            {
                EVEManager.ZKillFeed.PauseUpdate = !MapConf.ShowZKillData;
            }

            RegionUC.ReDrawMap(true);

            if (e.PropertyName == "ShowRegionStandings")
            {
                RegionsViewUC.Redraw(true);
            }

            if (e.PropertyName == "ShowUniverseRats")
            {
                RegionsViewUC.Redraw(true);
            }

            if (e.PropertyName == "ShowUniversePods")
            {
                RegionsViewUC.Redraw(true);
            }

            if (e.PropertyName == "ShowUniverseKills")
            {
                RegionsViewUC.Redraw(true);
            }

            if (e.PropertyName == "UniverseDataScale")
            {
                RegionsViewUC.Redraw(true);
            }
        }

        public void OnCharacterSelectionChanged()
        {
            CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource).Refresh();
        }

        private void RegionUC_RegionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (RegionLayoutDoc != null)
            {
                RegionLayoutDoc.Title = RegionUC.Region.Name;
            }

            CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource).Refresh();
        }

        private void RegionUC_UniverseSystemSelect(object sender, RoutedEventArgs e)
        {
            string sysName = e.OriginalSource as string;
            UniverseUC.ShowSystem(sysName);

            if (UniverseLayoutDoc != null)
            {
                UniverseLayoutDoc.IsSelected = true;
            }
        }

        #endregion Region Control

        #region Universe Control

        private void UniverseUC_RequestRegionSystem(object sender, RoutedEventArgs e)
        {
            string sysName = e.OriginalSource as string;
            RegionUC.FollowCharacter = false;
            RegionUC.SelectSystem(sysName, true);

            if (RegionLayoutDoc != null)
            {
                RegionLayoutDoc.IsSelected = true;
            }
        }

        #endregion Universe Control

        #region Preferences & Options

        private void ForceESIUpdate_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.UpdateESIUniverseData();
        }

        private void ClearOldEVELogs_Click(object sender, RoutedEventArgs e)
        {
            string EVEGameLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Gamelogs";
            {
                DirectoryInfo di = new DirectoryInfo(EVEGameLogFolder);
                FileInfo[] files = di.GetFiles("*.txt");
                foreach (FileInfo file in files)
                {
                    // keep only recent files
                    if (file.CreationTime < DateTime.Now.AddDays(-1))
                    {
                        try
                        {
                            File.Delete(file.FullName);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            string EVEChatLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Chatlogs";
            {
                DirectoryInfo di = new DirectoryInfo(EVEChatLogFolder);
                FileInfo[] files = di.GetFiles("*.txt");
                foreach (FileInfo file in files)
                {
                    // keep only recent files
                    if (file.CreationTime < DateTime.Now.AddDays(-1))
                    {
                        try
                        {
                            File.Delete(file.FullName);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private void FullScreenToggle_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (miFullScreenToggle.IsChecked)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
            }
        }

        private void Preferences_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (preferencesWindow != null)
            {
                preferencesWindow.Close();
            }

            preferencesWindow = new PreferencesWindow();
            preferencesWindow.Owner = this;
            preferencesWindow.DataContext = MapConf;
            preferencesWindow.MapConf = MapConf;
            preferencesWindow.EM = EVEManager;
            preferencesWindow.Init();
            preferencesWindow.ShowDialog();
            preferencesWindow.Closed += PreferencesWindow_Closed;
        }

        private void PreferencesWindow_Closed(object sender, EventArgs e)
        {
            RegionUC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, false);
        }

        #endregion Preferences & Options

        #region NewVersion

        private async void CheckGitHubVersion()
        {
            string url = @"https://api.github.com/repos/slazanger/smt/releases/latest";
            string strContent = string.Empty;

            try
            {
                HttpClient hc = new HttpClient();
                hc.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SMT", EveAppConfig.SMT_VERSION));
                var response = await hc.GetAsync(url);
                response.EnsureSuccessStatusCode();
                strContent = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return;
            }

            GitHubRelease.Release releaseInfo = GitHubRelease.Release.FromJson(strContent);

            if (releaseInfo != null)
            {
                if (releaseInfo.TagName != EveAppConfig.SMT_VERSION)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        NewVersionWindow nw = new NewVersionWindow();
                        nw.ReleaseInfo = releaseInfo.Body;
                        nw.CurrentVersion = EveAppConfig.SMT_VERSION;
                        nw.NewVersion = releaseInfo.TagName;
                        nw.ReleaseURL = releaseInfo.HtmlUrl.ToString();
                        nw.Owner = this;
                        nw.ShowDialog();
                    }), DispatcherPriority.Normal);
                }
            }
        }

        #endregion NewVersion

        #region Characters

        // Property now automatically fires an event when the active character changes.
        private EVEData.LocalCharacter activeCharacter;

        public EVEData.LocalCharacter ActiveCharacter
        { get => activeCharacter; set { activeCharacter = value; OnSelectedCharChangedEventHandler?.Invoke(this, EventArgs.Empty); } }

        /// <summary>
        ///  Add Character Button Clicked
        /// </summary>
        private void btn_AddCharacter_Click(object sender, RoutedEventArgs e)
        {
            AddCharacter();
        }

        public void AddCharacter()
        {
            if (logonBrowserWindow != null)
            {
                logonBrowserWindow.Close();
            }

            logonBrowserWindow = new LogonWindow();
            logonBrowserWindow.Owner = this;
            logonBrowserWindow.ShowDialog();
        }

        private void CharactersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    EVEData.LocalCharacter lc = dgr.Item as EVEData.LocalCharacter;

                    if (lc != null)
                    {
                        ActiveCharacter = lc;
                        CurrentActiveCharacterCombo.SelectedItem = lc;

                        lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
                        lc.FleetUpdatedEvent += OnFleetMemebersUpdate;

                        lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;
                        lc.RouteUpdatedEvent += OnCharacterRouteUpdate;

                        FleetMembersList.ItemsSource = lc.FleetInfo.Members;
                        CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();

                        RegionUC.FollowCharacter = true;
                        RegionUC.SelectSystem(lc.Location, true);

                        UniverseUC.FollowCharacter = true;
                        UniverseUC.UpdateActiveCharacter(lc);
                    }
                }
            }
        }

        private void OnCharacterRouteUpdate()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(dgActiveCharacterRoute.ItemsSource).Refresh();
                CollectionViewSource.GetDefaultView(lbActiveCharacterWaypoints.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void CharactersListMenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (CharactersList.SelectedIndex == -1)
            {
                return;
            }

            EVEData.LocalCharacter lc = CharactersList.SelectedItem as EVEData.LocalCharacter;

            lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
            lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;




            ActiveCharacter = null;
            FleetMembersList.ItemsSource = null;

            CurrentActiveCharacterCombo.SelectedIndex = -1;
            RegionsViewUC.ActiveCharacter = null;
            RegionUC.ActiveCharacter = null;
            RegionUC.UpdateActiveCharacter();
            UniverseUC.ActiveCharacter = null;
            OnCharacterSelectionChanged();

            EVEManager.RemoveCharacter(lc);
        }

        private void CurrentActiveCharacterCombo_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentActiveCharacterCombo.SelectedIndex == -1)
            {
                RegionsViewUC.ActiveCharacter = null;
                RegionUC.ActiveCharacter = null;
                FleetMembersList.ItemsSource = null;
                RegionUC.UpdateActiveCharacter();
                UniverseUC.UpdateActiveCharacter(null);
            }
            else
            {
                EVEData.LocalCharacter lc = CurrentActiveCharacterCombo.SelectedItem as EVEData.LocalCharacter;
                ActiveCharacter = lc;

                FleetMembersList.ItemsSource = lc.FleetInfo.Members;
                CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
                lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
                lc.FleetUpdatedEvent += OnFleetMemebersUpdate;

                lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;
                lc.RouteUpdatedEvent += OnCharacterRouteUpdate;


                RegionsViewUC.ActiveCharacter = lc;
                RegionUC.UpdateActiveCharacter(lc);
                UniverseUC.UpdateActiveCharacter(lc);
            }

            OnCharacterSelectionChanged();
        }

        private void UpdateCharacterSelectionBasedOnActiveWindow()
        {
            string ActiveWindowText = Utils.Misc.GetCaptionOfActiveWindow();

            if (ActiveWindowText.Contains("EVE - "))
            {
                string characterName = ActiveWindowText.Substring(6);
                foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                {
                    if (lc.Name == characterName)
                    {
                        ActiveCharacter = lc;
                        CurrentActiveCharacterCombo.SelectedItem = lc;
                        FleetMembersList.ItemsSource = lc.FleetInfo.Members;
                        CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
                        lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
                        lc.FleetUpdatedEvent += OnFleetMemebersUpdate;

                        lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;
                        lc.RouteUpdatedEvent += OnCharacterRouteUpdate;

                        RegionUC.UpdateActiveCharacter(lc);
                        UniverseUC.UpdateActiveCharacter(lc);

                        break;
                    }
                }
            }
        }


        public void OnFleetMemebersUpdate(LocalCharacter c)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();

            }), DispatcherPriority.Normal);


        }

        #endregion Characters

        #region intel

        ObservableCollection<EVEData.IntelData> IntelCache;

        private void ClearIntelBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.IntelDataList.ClearAll();
            IntelCache.Clear();

        }

        private void ClearGameLogBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                EVEManager.GameLogList.ClearAll();
                CollectionViewSource.GetDefaultView(RawGameDataBox.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void OnIntelUpdated(List<IntelData> idl)
        {
            bool playSound = false;
            bool flashWindow = false;


            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                List<IntelData> removeList = new List<IntelData>();
                List<IntelData> addList = new List<IntelData>();

                // remove old

                if(IntelCache.Count > 50)
                {
                    foreach (IntelData id in IntelCache)
                    {
                        if (!idl.Contains(id))
                        {
                            removeList.Add(id);
                        }
                    }

                    foreach (IntelData id in removeList)
                    {
                        IntelCache.Remove(id);
                    }
                }

  

                // add new
                foreach (IntelData id in idl)
                {
                    if(!IntelCache.Contains(id))
                    {
                        IntelCache.Insert(0, id);
                    }
                }
            }), DispatcherPriority.Normal);


            IntelData id = IntelCache[0];



            if (id.ClearNotification)
            {
                // do nothing for now
                return;
            }

            if (MapConf.PlayIntelSound || MapConf.FlashWindow)
            {
                if (MapConf.PlaySoundOnlyInDangerZone || MapConf.FlashWindowOnlyInDangerZone)
                {
                    if (MapConf.PlayIntelSoundOnUnknown && id.Systems.Count == 0)
                    {
                        playSound = true;
                        flashWindow = true;
                    }

                    foreach (string s in id.Systems)
                    {
                        foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                        {
                            if (lc.WarningSystems != null && lc.DangerZoneActive)
                            {
                                foreach (string ls in lc.WarningSystems)
                                {
                                    if (ls == s)
                                    {
                                        playSound = playSound || MapConf.PlaySoundOnlyInDangerZone;
                                        flashWindow = flashWindow || MapConf.FlashWindowOnlyInDangerZone;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    playSound = MapConf.PlayIntelSound;
                    flashWindow = MapConf.FlashWindow;
                }
            }

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (playSound || (!MapConf.PlaySoundOnlyInDangerZone && MapConf.PlayIntelSound))
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Volume = MapConf.IntelSoundVolume;
                    mediaPlayer.Position = new TimeSpan(0, 0, 0);
                    mediaPlayer.Play();
                }
                if (flashWindow || (!MapConf.FlashWindowOnlyInDangerZone && MapConf.FlashWindow))
                {
                    FlashWindow.Flash(AppWindow, 5);
                }

            }), DispatcherPriority.Normal);

        }

        private void OnShipDecloaked(string character, string text)
        {
            foreach (LocalCharacter lc in EVEManager.LocalCharacters)
            {
                if (lc.Name == character)
                {
                    if (lc.ObservatoryDecloakWarningEnabled)
                    {
                        if (OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                                ToastContentBuilder tb = new ToastContentBuilder();
                                tb.AddText("SMT Alert");
                                tb.AddText("Character : " + character + "(" + lc.Location + ")");

                                // add the character portrait if we have one                                
                                if(lc.PortraitLocation != null )
                                {
                                    tb.AddInlineImage(lc.PortraitLocation);
                                }

                                tb.AddText(text);
                                tb.AddArgument("character", character);
                                tb.SetToastScenario(ToastScenario.Alarm);
                                tb.SetToastDuration(ToastDuration.Long);
                                Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                                tb.AddAudio(woopUri);
                                tb.Show();
                            }), DispatcherPriority.Normal, null);
                        }
                    }

                    break;
                }
            }
        }

        private void OnCombatEvent(string character, string text)
        {
            foreach (LocalCharacter lc in EVEManager.LocalCharacters)
            {
                if (lc.Name == character)
                {
                    if (lc.CombatWarningEnabled)
                    {
                        if (OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                                ToastContentBuilder tb = new ToastContentBuilder();
                                tb.AddText("SMT Alert");
                                tb.AddText("Character : " + character + "(" + lc.Location + ")");

                                // add the character portrait if we have one                                
                                if (lc.PortraitLocation != null)
                                {
                                    tb.AddInlineImage(lc.PortraitLocation);
                                }

                                tb.AddText(text);
                                tb.AddArgument("character", character);
                                tb.SetToastScenario(ToastScenario.Alarm);
                                tb.SetToastDuration(ToastDuration.Long);
                                Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                                tb.AddAudio(woopUri);
                                tb.Show();
                            }), DispatcherPriority.Normal, null);
                        }
                    }

                    break;
                }
            }
        }


        private void OnZKillsAdded()
        {
            if(Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource).Refresh();
                }), DispatcherPriority.Normal, null);

            }
        }



        private void RawIntelBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RawIntelBox.SelectedItem == null)
            {
                return;
            }

            EVEData.IntelData chat = RawIntelBox.SelectedItem as EVEData.IntelData;

            bool selectedSystem = false;

            foreach (string s in chat.IntelString.Split(' '))
            {
                if (s == "")
                {
                    continue;
                }
                var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                foreach (Match m in linkParser.Matches(s))
                {
                    string url = m.Value;
                    if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    {
                        url = "http://" + url;
                    }
                    if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
                // only select the first system
                if (!selectedSystem)
                {
                    foreach (EVEData.System sys in EVEManager.Systems)
                    {
                        if (s.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            if (RegionUC.Region.Name != sys.Region)
                            {
                                RegionUC.SelectRegion(sys.Region);
                            }

                            RegionUC.SelectSystem(s, true);
                            selectedSystem = true;
                        }
                    }
                }
            }
        }

        #endregion intel

        #region Thera

        /// <summary>
        /// Update Thera Button Clicked
        /// </summary>
        private void btn_UpdateThera_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.UpdateTheraConnections();
        }

        private void TheraConnectionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    EVEData.TheraConnection tc = dgr.Item as EVEData.TheraConnection;

                    if (tc != null)
                    {
                        RegionUC.SelectSystem(tc.System, true);
                    }
                }
            }
        }

        #endregion Thera

        #region Route

        private void refreshJumpRouteUI()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (capitalRouteWaypointsLB.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(capitalRouteWaypointsLB.ItemsSource).Refresh();
                }

                if (capitalRouteAvoidLB.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(capitalRouteAvoidLB.ItemsSource).Refresh();
                }

                if (dgCapitalRouteCurrentRoute.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(dgCapitalRouteCurrentRoute.ItemsSource).Refresh();
                }


                // lbAlternateMids could be null, need to check..

                if (lbAlternateMids.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(lbAlternateMids.ItemsSource).Refresh();
                }
            }), DispatcherPriority.Normal, null);
        }

        private void AddWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RegionUC.ActiveCharacter == null)
            {
                return;
            }

            if (RouteSystemDropDownAC.SelectedItem == null)
            {
                return;
            }
            EVEData.System s = RouteSystemDropDownAC.SelectedItem as EVEData.System;

            if (s != null)
            {
                RegionUC.ActiveCharacter.AddDestination(s.ID, false);
            }
        }

        private void AddJumpWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (JumpRouteSystemDropDownAC.SelectedItem == null)
            {
                return;
            }
            EVEData.System s = JumpRouteSystemDropDownAC.SelectedItem as EVEData.System;

            if (s != null)
            {
                CapitalRoute.WayPoints.Add(s.Name);
                CapitalRoute.Recalculate();

                if (CapitalRoute.CurrentRoute.Count == 0)
                {
                    lblCapitalRouteSummary.Content = "No Valid Route Found";
                }
                else
                {
                    lblCapitalRouteSummary.Content = $"{CapitalRoute.CurrentRoute.Count - 2} Mids";
                }

                refreshJumpRouteUI();
            }
        }

        private void AddJumpAvoidSystemsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (JumpRouteAvoidSystemDropDownAC.SelectedItem == null)
            {
                return;
            }
            EVEData.System s = JumpRouteAvoidSystemDropDownAC.SelectedItem as EVEData.System;

            if (s != null)
            {
                CapitalRoute.AvoidSystems.Add(s.Name);
                CapitalRoute.Recalculate();

                if (CapitalRoute.CurrentRoute.Count == 0)
                {
                    lblCapitalRouteSummary.Content = "No Valid Route Found";
                }
                else
                {
                    lblCapitalRouteSummary.Content = $"{CapitalRoute.CurrentRoute.Count - 2} Mids";
                }

                refreshJumpRouteUI();
            }
        }

        private void ClearWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null)
            {
                c.ClearAllWaypoints();
            }
        }

        private void ClearJumpWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            CapitalRoute.WayPoints.Clear();
            CapitalRoute.CurrentRoute.Clear();
            lbAlternateMids.ItemsSource = null;
            lblAlternateMids.Content = "";

            refreshJumpRouteUI();
        }

        private void ClearJumpAvoidSystemsBtn_Click(object sender, RoutedEventArgs e)
        {
            CapitalRoute.AvoidSystems.Clear();
            refreshJumpRouteUI();
        }

        private void CopyRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null)
            {
                string WPT = c.GetWayPointText();

                try
                {
                    Clipboard.SetText(WPT);
                }
                catch { }
            }
        }

        private void ReCalculateRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null && c.Waypoints.Count > 0)
            {
                c.RecalcRoute();
            }
        }

        #endregion Route

        #region JumpBridges

        private void ClearJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.JumpBridges.Clear();
            EVEData.Navigation.ClearJumpBridges();

            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();
        }

        private void DeleteJumpGateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (JumpBridgeList.SelectedIndex == -1)
            {
                return;
            }

            EVEData.JumpBridge jb = JumpBridgeList.SelectedItem as EVEData.JumpBridge;

            EVEManager.JumpBridges.Remove(jb);

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges);
            RegionUC.ReDrawMap(true);

            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null && c.Waypoints.Count > 0)
            {
                c.RecalcRoute();
            }

            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();
            UpdateJumpBridgeSummary();
        }

        private void EnableDisableJumpGateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (JumpBridgeList.SelectedIndex == -1)
            {
                return;
            }

            EVEData.JumpBridge jb = JumpBridgeList.SelectedItem as EVEData.JumpBridge;

            jb.Disabled = !jb.Disabled;

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges);
            RegionUC.ReDrawMap(true);

            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null && c.Waypoints.Count > 0)
            {
                c.RecalcRoute();
            }

            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();
            UpdateJumpBridgeSummary();
        }

        private void ExportJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            string ExportText = "";

            foreach (EVEData.MapRegion mr in EVEManager.Regions)
            {
                ExportText += "# " + mr.Name + "\n";

                foreach (EVEData.JumpBridge jb in EVEManager.JumpBridges)
                {
                    EVEData.System es = EVEManager.GetEveSystem(jb.From);
                    if (es.Region == mr.Name)
                    {
                        ExportText += $"{jb.FromID} {jb.From} --> {jb.To}\n";
                    }

                    es = EVEManager.GetEveSystem(jb.To);
                    if (es.Region == mr.Name)
                    {
                        ExportText += $"{jb.ToID} {jb.To} --> {jb.From}\n";
                    }
                }

                ExportText += "\n";
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, ExportText);
                }
                catch { }
            }
        }

        private async void FindJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            ImportJumpGatesBtn.IsEnabled = false;
            ClearJumpGatesBtn.IsEnabled = false;
            JumpBridgeList.IsEnabled = false;
            ImportPasteJumpGatesBtn.IsEnabled = false;
            ExportJumpGatesBtn.IsEnabled = false;

            foreach (EVEData.LocalCharacter c in EVEManager.LocalCharacters)
            {
                if (c.ESILinked)
                {
                    // This should never be set due to https://developers.eveonline.com/blog/article/the-esi-api-is-a-shared-resource-do-not-abuse-it
                    if (c.DeepSearchEnabled && GateSearchFilter.Text == " » ")
                    {
                        string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                        string basesearch = " » ";

                        foreach (char cc in chars)
                        {
                            string search = basesearch + cc;
                            List<EVEData.JumpBridge> jbl = await c.FindJumpGates(search);

                            foreach (EVEData.JumpBridge jb in jbl)
                            {
                                bool found = false;

                                foreach (EVEData.JumpBridge jbr in EVEManager.JumpBridges)
                                {
                                    if ((jb.From == jbr.From && jb.To == jbr.To) || (jb.From == jbr.To && jb.To == jbr.From))
                                    {
                                        found = true;
                                    }
                                }

                                if (!found)
                                {
                                    EVEManager.JumpBridges.Add(jb);
                                }
                            }

                            Thread.Sleep(100);
                        }

                        foreach (char cc in chars)
                        {
                            string search = cc + basesearch;
                            List<EVEData.JumpBridge> jbl = await c.FindJumpGates(search);

                            foreach (EVEData.JumpBridge jb in jbl)
                            {
                                bool found = false;

                                foreach (EVEData.JumpBridge jbr in EVEManager.JumpBridges)
                                {
                                    if ((jb.From == jbr.From && jb.To == jbr.To) || (jb.From == jbr.To && jb.To == jbr.From))
                                    {
                                        found = true;
                                    }
                                }

                                if (!found)
                                {
                                    EVEManager.JumpBridges.Add(jb);
                                }
                            }

                            Thread.Sleep(100);
                        }
                    }
                    else
                    {
                        List<EVEData.JumpBridge> jbl = await c.FindJumpGates(GateSearchFilter.Text);

                        foreach (EVEData.JumpBridge jb in jbl)
                        {
                            bool found = false;

                            foreach (EVEData.JumpBridge jbr in EVEManager.JumpBridges)
                            {
                                if ((jb.From == jbr.From && jb.To == jbr.To) || (jb.From == jbr.To && jb.To == jbr.From))
                                {
                                    found = true;
                                }
                            }

                            if (!found)
                            {
                                EVEManager.JumpBridges.Add(jb);
                            }
                        }
                    }
                }
            }

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges);
            UpdateJumpBridgeSummary();
            RegionUC.ReDrawMap(true);

            ImportJumpGatesBtn.IsEnabled = true;
            ClearJumpGatesBtn.IsEnabled = true;
            JumpBridgeList.IsEnabled = true;
            ImportPasteJumpGatesBtn.IsEnabled = true;
            ExportJumpGatesBtn.IsEnabled = true;
            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();


        }

        private void ImportPasteJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Text))
            {
                return;
            }
            String jbText = Clipboard.GetText(TextDataFormat.UnicodeText);

            Regex rx = new Regex(
                @"<url=showinfo:35841//([0-9]+)>(.*?) » (.*?) - .*?</url>|^[\t ]*([0-9]+) (.*) --> (.*)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
            MatchCollection matches = rx.Matches(jbText);

            foreach (Match match in matches)
            {
                // from eve chat
                // 1 = id
                // 2 = from
                // 3 = to

                // from export
                // 4 = id
                // 5 = from
                // 6 = to
                GroupCollection groups = match.Groups;
                long IDFrom = 0;
                if (groups[1].Value != "" && groups[2].Value != "" && groups[3].Value != "")
                {
                    long.TryParse(groups[1].Value, out IDFrom);
                    string from = groups[2].Value;
                    string to = groups[3].Value;
                    EVEManager.AddUpdateJumpBridge(from, to, IDFrom);
                }
                else if (groups[4].Value != "" && groups[5].Value != "" && groups[6].Value != "")
                {
                    long.TryParse(groups[4].Value, out IDFrom);
                    string from = groups[5].Value.Trim();
                    string to = groups[6].Value.Trim();
                    EVEManager.AddUpdateJumpBridge(from, to, IDFrom);
                }
            }

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges);
            UpdateJumpBridgeSummary();
            RegionUC.ReDrawMap(true);
            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();

        }

        private void UpdateJumpBridgeSummary()
        {
            int JBCount = 0;
            int MissingInfo = 0;
            int Disabled = 0;

            foreach (EVEData.JumpBridge jb in EVEManager.JumpBridges)
            {
                JBCount++;

                if (jb.FromID == 0 || jb.ToID == 0)
                {
                    MissingInfo++;
                }
                if (jb.Disabled)
                {
                    Disabled++;
                }
            }

            string Label = $"{JBCount} gates, {MissingInfo} Incomplete, {Disabled} Disabled ";

            AnsiblexSummaryLbl.Content = Label;
        }

        #endregion JumpBridges

        #region ZKillBoard

        private bool zkbFilterByRegion = true;

        private void ZKBContexMenu_ShowSystem_Click(object sender, RoutedEventArgs e)
        {
            if (ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if (zkbs != null)
            {
                RegionUC.SelectSystem(zkbs.SystemName, true);

                if (RegionLayoutDoc != null)
                {
                    RegionLayoutDoc.IsSelected = true;
                }
            }
        }

        private void ZKBContexMenu_ShowZKB_Click(object sender, RoutedEventArgs e)
        {
            if (ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if (zkbs != null)
            {
                string KillURL = "https://zkillboard.com/kill/" + zkbs.KillID + "/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(KillURL) { UseShellExecute = true });
            }
        }

        private void ZKBFeed_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if (zkbs != null)
            {
                string KillURL = "https://zkillboard.com/kill/" + zkbs.KillID + "/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(KillURL) { UseShellExecute = true });
            }
        }

        private void ZKBFeed_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            if (ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if (zkbs != null)
            {
                string KillURL = "https://zkillboard.com/kill/" + zkbs.KillID + "/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(KillURL) { UseShellExecute = true });
            }
        }

        private bool ZKBFeedFilter(object item)
        {
            if (zkbFilterByRegion == false)
            {
                return true;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zs = item as EVEData.ZKillRedisQ.ZKBDataSimple;
            if (zs == null)
            {
                return false;
            }

            if (RegionUC.Region.IsSystemOnMap(zs.SystemName))
            {
                return true;
            }

            return false;
        }

        private void ZKBFeedFilterViewChk_Checked(object sender, RoutedEventArgs e)
        {
            zkbFilterByRegion = (bool)ZKBFeedFilterViewChk.IsChecked;

            if (ZKBFeed != null)
            {
                try
                {
                    CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource).Refresh();
                }
                catch
                {
                }
            }
        }

        #endregion ZKillBoard

        #region Anoms

        /// <summary>
        /// Clear system Anoms button clicked
        /// </summary>
        private void btnClearAnomList_Click(object sender, RoutedEventArgs e)
        {
            EVEData.AnomData ad = ANOMManager.ActiveSystem;
            if (ad != null)
            {
                ad.Anoms.Clear();
                AnomSigList.Items.Refresh();
                AnomSigList.UpdateLayout();
                CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
            }
        }

        /// <summary>
        /// Update Anoms clicked
        /// </summary>
        private void btnUpdateAnomList_Click(object sender, RoutedEventArgs e)
        {
            string pasteData = Clipboard.GetText();
            if (!string.IsNullOrEmpty(pasteData))
            {
                EVEData.AnomData ad = ANOMManager.ActiveSystem;

                if (ad != null)
                {
                    ad.UpdateFromPaste(pasteData);
                    AnomSigList.Items.Refresh();
                    AnomSigList.UpdateLayout();
                    CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
                }
            }
        }

        #endregion Anoms

        private void Characters_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CharactersWindow charactersWindow = new CharactersWindow();
            charactersWindow.characterLV.ItemsSource = EVEManager.LocalCharacters;

            charactersWindow.Owner = this;

            charactersWindow.ShowDialog();
        }

        private void LoadInfoObjects()
        {
            InfoLayer = new List<InfoItem>();

            // now add the beacons
            string infoObjectsFile = EveAppConfig.StorageRoot + @"\InfoObjects.txt";
            if (File.Exists(infoObjectsFile))
            {
                StreamReader file = new StreamReader(infoObjectsFile);

                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }

                    string[] parts = line.Split(',');

                    if (parts.Length == 0)
                    {
                        continue;
                    }

                    string region = parts[0];

                    EVEData.MapRegion mr = EVEManager.GetRegion(region);
                    if (mr == null)
                    {
                        continue;
                    }

                    if (parts[1] == "SYSLINK" || parts[1] == "SYSLINKARC")
                    {
                        if (parts.Length != 7)
                        {
                            continue;
                        }
                        // REGION SYSLINK FROM TO SOLID/DASHED size #FFFFFF
                        string from = parts[2];
                        string to = parts[3];
                        string lineStyle = parts[4];
                        string size = parts[5];
                        string colour = parts[6];

                        if (!mr.MapSystems.ContainsKey(from))
                        {
                            continue;
                        }

                        if (!mr.MapSystems.ContainsKey(to))
                        {
                            continue;
                        }

                        EVEData.MapSystem fromMS = mr.MapSystems[from];
                        EVEData.MapSystem toMS = mr.MapSystems[to];

                        InfoItem.LineType lt = InfoItem.LineType.Solid;
                        if (lineStyle == "DASHED")
                        {
                            lt = InfoItem.LineType.Dashed;
                        }

                        if (lineStyle == "LIGHTDASHED")
                        {
                            lt = InfoItem.LineType.LightDashed;
                        }

                        Color c = (Color)ColorConverter.ConvertFromString(colour);

                        int lineThickness = int.Parse(size);

                        InfoItem ii = new InfoItem();
                        ii.DrawType = InfoItem.ShapeType.Line;

                        if (parts[1] == "SYSLINKARC")
                        {
                            ii.DrawType = InfoItem.ShapeType.ArcLine;
                        }

                        ii.X1 = (int)fromMS.Layout.X;
                        ii.Y1 = (int)fromMS.Layout.Y;
                        ii.X2 = (int)toMS.Layout.X;
                        ii.Y2 = (int)toMS.Layout.Y;
                        ii.Size = lineThickness;
                        ii.Region = region;
                        ii.Fill = c;
                        ii.LineStyle = lt;
                        InfoLayer.Add(ii);
                    }

                    if (parts[1] == "SYSMARKER")
                    {
                        if (parts.Length != 5)
                        {
                            continue;
                        }
                        // REGION SYSMARKER FROM SIZE #FFFFFF
                        string from = parts[2];
                        string size = parts[3];
                        string colour = parts[4];

                        if (!mr.MapSystems.ContainsKey(from))
                        {
                            continue;
                        }

                        EVEData.MapSystem fromMS = mr.MapSystems[from];

                        Color c = (Color)ColorConverter.ConvertFromString(colour);

                        int radius = int.Parse(size);

                        InfoItem ii = new InfoItem();
                        ii.DrawType = InfoItem.ShapeType.Circle;
                        ii.X1 = (int)fromMS.Layout.X;
                        ii.Y1 = (int)fromMS.Layout.Y;
                        ii.Size = radius;
                        ii.Region = region;
                        ii.Fill = c;
                        InfoLayer.Add(ii);
                    }
                }
            }

            RegionUC.InfoLayer = InfoLayer;
        }

        private void TestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string characterName = ActiveCharacter.Name;

            LocalCharacter lc = ActiveCharacter;

            string line = "Your cloak deactivates due to a pulse from a Mobile Observatory deployed by Slazanger.";

            lc.GameLogWarningText = line;

            if (OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
            {
                ToastContentBuilder tb = new ToastContentBuilder();
                tb.AddText("SMT Alert");
                tb.AddText("Character : " + characterName + "(" + lc.Location + ")");

                // add the character portrait if we have one                                
                if (lc.PortraitLocation != null)
                {
                    tb.AddInlineImage(lc.PortraitLocation);
                }

                tb.AddText(line);
                tb.AddArgument("character", characterName);
                tb.SetToastScenario(ToastScenario.Alarm);
                tb.SetToastDuration(ToastDuration.Long);
                Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                tb.AddAudio(woopUri);
                tb.Show();
            }
        }

        private void miResetLayout_Click(object sender, RoutedEventArgs e)
        {
            string defaultLayoutFile = AppDomain.CurrentDomain.BaseDirectory + @"\DefaultWindowLayout.dat";
            if (File.Exists(defaultLayoutFile))
            {
                try
                {
                    AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                    using (var sr = new StreamReader(defaultLayoutFile))
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

            dockManager.UpdateLayout();
        }

        private void JumpPlannerShipType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CapitalRoute != null)
            {
                ComboBox cb = sender as ComboBox;
                ComboBoxItem cbi = cb.SelectedItem as ComboBoxItem;
                CapitalRoute.MaxLY = double.Parse(cbi.DataContext as string);
                CapitalRoute.Recalculate();

                if (CapitalRoute.CurrentRoute.Count == 0)
                {
                    lblCapitalRouteSummary.Content = "No Valid Route Found";
                }
                else
                {
                    lblCapitalRouteSummary.Content = $"{CapitalRoute.CurrentRoute.Count - 2} Mids";
                }

                refreshJumpRouteUI();
            }
        }

        private void JumpPlannerJDC_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CapitalRoute != null)
            {
                ComboBox cb = sender as ComboBox;
                ComboBoxItem cbi = cb.SelectedItem as ComboBoxItem;
                CapitalRoute.JDC = int.Parse(cbi.DataContext as string);
                CapitalRoute.Recalculate();

                if (CapitalRoute.CurrentRoute.Count == 0)
                {
                    lblCapitalRouteSummary.Content = "No Valid Route Found";
                }
                else
                {
                    lblCapitalRouteSummary.Content = $"{CapitalRoute.CurrentRoute.Count - 2} Mids";
                }

                refreshJumpRouteUI();
            }
        }

        private void CurrentCapitalRouteItem_Selected(object sender, RoutedEventArgs e)
        {
            if (dgCapitalRouteCurrentRoute.SelectedItem != null)
            {
                Navigation.RoutePoint rp = dgCapitalRouteCurrentRoute.SelectedItem as Navigation.RoutePoint;
                string sel = rp.SystemName;

                UniverseUC.ShowSystem(sel);

                lblAlternateMids.Content = sel;
                if (CapitalRoute.AlternateMids.ContainsKey(sel))
                {
                    lbAlternateMids.ItemsSource = CapitalRoute.AlternateMids[sel];
                }
                else
                {
                    lbAlternateMids.ItemsSource = null;
                }


            }
        }

        private void CapitalWaypointsContextMenuMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (capitalRouteWaypointsLB.SelectedItem != null && capitalRouteWaypointsLB.SelectedIndex != 0)
            {
                string sys = CapitalRoute.WayPoints[capitalRouteWaypointsLB.SelectedIndex];
                CapitalRoute.WayPoints.RemoveAt(capitalRouteWaypointsLB.SelectedIndex);
                CapitalRoute.WayPoints.Insert(capitalRouteWaypointsLB.SelectedIndex - 1, sys);

                CapitalRoute.Recalculate();
                refreshJumpRouteUI();
            }
        }

        private void CapitalWaypointsContextMenuMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (capitalRouteWaypointsLB.SelectedItem != null && capitalRouteWaypointsLB.SelectedIndex != CapitalRoute.WayPoints.Count - 1)
            {
                string sys = CapitalRoute.WayPoints[capitalRouteWaypointsLB.SelectedIndex];
                CapitalRoute.WayPoints.RemoveAt(capitalRouteWaypointsLB.SelectedIndex);
                CapitalRoute.WayPoints.Insert(capitalRouteWaypointsLB.SelectedIndex + 1, sys);


                CapitalRoute.Recalculate();
                refreshJumpRouteUI();
            }
        }

        private void CapitalWaypointsContextMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (capitalRouteWaypointsLB.SelectedItem != null)
            {
                CapitalRoute.WayPoints.RemoveAt(capitalRouteWaypointsLB.SelectedIndex);
                CapitalRoute.Recalculate();
                refreshJumpRouteUI();
            }
        }

        private void CapitalRouteContextMenuUseAlt_Click(object sender, RoutedEventArgs e)
        {
            if (lbAlternateMids.SelectedItem != null)
            {
                string selectedAlt = lbAlternateMids.SelectedItem as string;

                // need to find where to insert the new waypoint
                int waypointIndex = -1;
                foreach (Navigation.RoutePoint rp in CapitalRoute.CurrentRoute)
                {
                    if (rp.SystemName == CapitalRoute.WayPoints[waypointIndex + 1])
                    {
                        waypointIndex++;
                    }
                    if (CapitalRoute.AlternateMids.ContainsKey(rp.SystemName))
                    {
                        foreach (string alt in CapitalRoute.AlternateMids[rp.SystemName])
                        {
                            if (alt == selectedAlt)
                            {
                                CapitalRoute.WayPoints.Insert(waypointIndex + 1, selectedAlt);
                                break;
                            }
                        }
                    }
                }

                CapitalRoute.Recalculate();
                refreshJumpRouteUI();
            }
        }

        private void btnUnseenFits_Click(object sender, RoutedEventArgs e)
        {
            string KillURL = "https://zkillboard.com/character/93280351/losses/";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(KillURL) { UseShellExecute = true });
        }

        private void OverlayWindow_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (overlayWindow != null)
            {
                return;
            }

            overlayWindow = new Overlay(this);
            overlayWindow.Closing += OnOverlayWindowClosing;
            overlayWindow.Show();
        }

        public void OnOverlayWindowClosing(object sender, CancelEventArgs e)
        {
            overlayWindow = null;
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

            if (ts.Ticks < 0)
            {
                Output += "-";
            }

            if (ts.Days != 0)
            {
                Output += Math.Abs(ts.Days) + "d ";
            }

            if (ts.Hours != 0)
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
            if (zs != null)
            {
                float Standing = 0.0f;

                EVEData.LocalCharacter c = MainWindow.AppWindow.RegionUC.ActiveCharacter;
                if (c != null && c.ESILinked)
                {
                    if (c.AllianceID != 0 && c.AllianceID == zs.VictimAllianceID)
                    {
                        Standing = 10.0f;
                    }

                    if (c.Standings.Keys.Contains(zs.VictimAllianceID))
                    {
                        Standing = c.Standings[zs.VictimAllianceID];
                    }

                    if (Standing == -10.0)
                    {
                        rowCol = Colors.Red;
                    }

                    if (Standing == -5.0)
                    {
                        rowCol = Colors.Orange;
                    }

                    if (Standing == 5.0)
                    {
                        rowCol = Colors.LightBlue;
                    }

                    if (Standing == 10.0)
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
            if (zs != null)
            {
                float Standing = 0.0f;

                EVEData.LocalCharacter c = MainWindow.AppWindow.RegionUC.ActiveCharacter;
                if (c != null && c.ESILinked)
                {
                    if (c.AllianceID != 0 && c.AllianceID == zs.VictimAllianceID)
                    {
                        Standing = 10.0f;
                    }

                    if (c.Standings.Keys.Contains(zs.VictimAllianceID))
                    {
                        Standing = c.Standings[zs.VictimAllianceID];
                    }

                    if (Standing == -10.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if (Standing == -5.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if (Standing == 5.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if (Standing == 10.0)
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
            if (Win2000OrLater)
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