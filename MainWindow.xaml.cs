using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        public const string SMT_VERSION = "SMT_109";
        public static MainWindow AppWindow;
        private LogonWindow logonBrowserWindow;

        private MediaPlayer mediaPlayer;
        private PreferencesWindow preferencesWindow;

        private int uiRefreshCounter = 0;
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        private List<InfoItem> InfoLayer;

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

            InitializeComponent();

            Title = "SMT (Powered by Plastic Support : " + SMT_VERSION + ")";

            CheckGitHubVersion();

            // Load the Dock Manager Layout file
            string dockManagerLayoutName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMT_VERSION + "\\Layout.dat";
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
            string mapConfigFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMT_VERSION + "\\MapConfig.dat";

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

            EVEManager = new EVEData.EveManager(SMT_VERSION);
            EVEData.EveManager.Instance = EVEManager;
            EVEManager.EVELogFolder = MapConf.CustomEveLogFolderLocation;

            EVEManager.UseESIForCharacterPositions = MapConf.UseESIForCharacterPositions;

            // if we want to re-build the data as we've changed the format, recreate it all from scratch
            bool initFromScratch = true;
            if (initFromScratch)
            {
                EVEManager.CreateFromScratch();
                SaveDefaultLayout();
            }
            else
            {
                EVEManager.LoadFromDisk();
            }

            EVEManager.SetupIntelWatcher();
            EVEManager.SetupGameLogWatcher();
            EVEManager.SetupLogFileTriggers();

            RawIntelBox.ItemsSource = EVEManager.IntelDataList;
            RawGameDataBox.ItemsSource = EVEManager.GameLogList;

            // add test intel with debug info
            IntelData id = new IntelData("[00:00] blah.... > blah", "System");
            id.IntelString = "Intel Watcher monitoring : " + EVEManager.EVELogFolder + @"\Chatlogs\";

            IntelData idtwo = new IntelData("[00:00] blah.... > blah", "System");
            idtwo.IntelString = "Intel Filters : " + String.Join(",", EVEManager.IntelFilters);

            EVEManager.IntelDataList.Add(id);
            EVEManager.IntelDataList.Add(idtwo);

            MapConf.CurrentEveLogFolderLocation = EVEManager.EVELogFolder;

            EVEManager.ZKillFeed.KillExpireTimeMinutes = MapConf.ZkillExpireTimeMinutes;

            // load jump bridge data
            EVEManager.LoadJumpBridgeData();
            EVEManager.UpdateESIUniverseData();
            EVEManager.InitNavigation();

            EVEManager.UpdateMetaliminalStorms();

            EVEManager.LocalCharacters.CollectionChanged += LocalCharacters_CollectionChanged;

            CharactersList.ItemsSource = EVEManager.LocalCharacters;
            CurrentActiveCharacterCombo.ItemsSource = EVEManager.LocalCharacters;

            FleetMembersList.DataContext = this;

            TheraConnectionsList.ItemsSource = EVEManager.TheraConnections;
            JumpBridgeList.ItemsSource = EVEManager.JumpBridges;
            MetaliminalStormList.ItemsSource = EVEManager.MetaliminalStorms;

            SovCampaignList.ItemsSource = EVEManager.ActiveSovCampaigns;
            EVEManager.ActiveSovCampaigns.CollectionChanged += ActiveSovCampaigns_CollectionChanged;

            LoadInfoObjects();
            UpdateJumpBridgeSummary();

            // load any custom universe view layout
            // Save any custom map Layout
            string customLayoutFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMT_VERSION + "\\CustomUniverseLayout.txt";
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

            RegionUC.MapConf = MapConf;
            RegionUC.Init();
            RegionUC.SelectRegion(MapConf.DefaultRegion);

            RegionUC.RegionChanged += RegionUC_RegionChanged;
            RegionUC.UniverseSystemSelect += RegionUC_UniverseSystemSelect;

            UniverseUC.MapConf = MapConf;
            UniverseUC.Init();
            UniverseUC.RequestRegionSystem += UniverseUC_RequestRegionSystem;

            RegionsViewUC.MapConf = MapConf;
            RegionsViewUC.Init();
            RegionsViewUC.RequestRegion += RegionsViewUC_RequestRegion;

            AppStatusBar.DataContext = EVEManager.ServerInfo;

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

            RegionUC.ANOMManager = ANOMManager;

            List<EVEData.System> globalSystemList = new List<EVEData.System>(EVEManager.Systems);
            globalSystemList.Sort((a, b) => string.Compare(a.Name, b.Name));
            RouteSystemDropDownAC.ItemsSource = globalSystemList;

            MapConf.PropertyChanged += MapConf_PropertyChanged;

            Closed += MainWindow_Closed;

            EVEManager.IntelAddedEvent += OnIntelAdded;

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 1);
            uiRefreshTimer.Start();

            ZKBFeed.ItemsSource = EVEManager.ZKillFeed.KillStream;

            CollectionView zKBFeedview = (CollectionView)CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource);
            zKBFeedview.Refresh();
            zKBFeedview.Filter = ZKBFeedFilter;

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

        private void ActiveSovCampaigns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(SovCampaignList.ItemsSource).Refresh();
        }

        private void LocalCharacters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(CharactersList.ItemsSource).Refresh();
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

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // save off the dockmanager layout

            string dockManagerLayoutName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMT_VERSION + "\\Layout.dat";
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
                string mapConfigFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMT_VERSION + "\\MapConfig.dat";

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
                string customLayoutFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMT_VERSION + "\\CustomUniverseLayout.txt";
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
                    CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
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
                if (releaseInfo.TagName != SMT_VERSION)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        NewVersionWindow nw = new NewVersionWindow();
                        nw.ReleaseInfo = releaseInfo.Body;
                        nw.CurrentVersion = SMT_VERSION;
                        nw.NewVersion = releaseInfo.TagName;
                        nw.ReleaseURL = releaseInfo.HtmlUrl.ToString();
                        nw.Owner = this;
                        nw.ShowDialog();
                    }), DispatcherPriority.ApplicationIdle);
                }
            }
        }

        #endregion NewVersion

        #region Characters

        public EVEData.LocalCharacter ActiveCharacter { get; set; }

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

        private void CharactersListMenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (CharactersList.SelectedIndex == -1)
            {
                return;
            }

            EVEData.LocalCharacter lc = CharactersList.SelectedItem as EVEData.LocalCharacter;

            ActiveCharacter = null;
            FleetMembersList.ItemsSource = null;

            CurrentActiveCharacterCombo.SelectedIndex = -1;
            RegionsViewUC.ActiveCharacter = null;
            RegionUC.ActiveCharacter = null;
            RegionUC.UpdateActiveCharacter();
            UniverseUC.ActiveCharacter = null;
            OnCharacterSelectionChanged();

            EVEManager.LocalCharacters.Remove(lc);
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

                RegionsViewUC.ActiveCharacter = lc;
                RegionUC.UpdateActiveCharacter(lc);
                UniverseUC.UpdateActiveCharacter(lc);
            }

            OnCharacterSelectionChanged();
        }

        private void UpdateCharacterSelectionBasedOnActiveWindow()
        {
            string ActiveWindowText = Utils.GetCaptionOfActiveWindow();

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
                        RegionUC.UpdateActiveCharacter(lc);
                        UniverseUC.UpdateActiveCharacter(lc);

                        break;
                    }
                }
            }
        }

        #endregion Characters

        #region intel

        private void ClearIntelBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                EVEManager.IntelDataList.Clear();
            }), DispatcherPriority.ApplicationIdle);
        }

        private void ClearGameLogBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                EVEManager.GameLogList.Clear();
            }), DispatcherPriority.ApplicationIdle);
        }

        private void OnIntelAdded(List<string> intelsystems)
        {
            bool playSound = false;

            if (MapConf.PlayIntelSound)
            {
                if (MapConf.PlaySoundOnlyInDangerZone)
                {
                    if (MapConf.PlayIntelSoundOnUnknown && intelsystems.Count == 0)
                    {
                        playSound = true;
                    }

                    foreach (string s in intelsystems)
                    {
                        foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                        {
                            if (lc.WarningSystems != null && lc.DangerZoneActive)
                            {
                                foreach (string ls in lc.WarningSystems)
                                {
                                    if (ls == s)
                                    {
                                        playSound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    playSound = true;
                }
            }

            if (playSound)
            {
                mediaPlayer.Stop();
                mediaPlayer.Volume = MapConf.IntelSoundVolume;
                mediaPlayer.Position = new TimeSpan(0, 0, 0);
                mediaPlayer.Play();
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
                        Process.Start(url);
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

        private void ClearWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null)
            {
                c.ClearAllWaypoints();
            }
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
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges.ToList());
            RegionUC.ReDrawMap(true);

            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null && c.Waypoints.Count > 0)
            {
                c.RecalcRoute();
            }
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
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges.ToList());
            RegionUC.ReDrawMap(true);

            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null && c.Waypoints.Count > 0)
            {
                c.RecalcRoute();
            }

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
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges.ToList());
            UpdateJumpBridgeSummary();
            RegionUC.ReDrawMap(true);

            ImportJumpGatesBtn.IsEnabled = true;
            ClearJumpGatesBtn.IsEnabled = true;
            JumpBridgeList.IsEnabled = true;
            ImportPasteJumpGatesBtn.IsEnabled = true;
            ExportJumpGatesBtn.IsEnabled = true;
        }

        private void ImportPasteJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Text))
            {
                return;
            }
            String jbText = Clipboard.GetText(TextDataFormat.Text);

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
                    string from = groups[5].Value;
                    string to = groups[6].Value;
                    EVEManager.AddUpdateJumpBridge(from, to, IDFrom);
                }
            }

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges.ToList());
            UpdateJumpBridgeSummary();
            RegionUC.ReDrawMap(true);
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
                System.Diagnostics.Process.Start(KillURL);
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
                System.Diagnostics.Process.Start(KillURL);
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
                System.Diagnostics.Process.Start(KillURL);
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
            string infoObjectsFile = AppDomain.CurrentDomain.BaseDirectory + @"\InfoObjects.txt";
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

                        ii.X1 = (int)fromMS.LayoutX;
                        ii.Y1 = (int)fromMS.LayoutY;
                        ii.X2 = (int)toMS.LayoutX;
                        ii.Y2 = (int)toMS.LayoutY;
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
                        ii.X1 = (int)fromMS.LayoutX;
                        ii.Y1 = (int)fromMS.LayoutY;
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
                tb.AddInlineImage(lc.Portrait.UriSource);
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
}