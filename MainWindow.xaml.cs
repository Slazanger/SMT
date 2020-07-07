using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace SMT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow AppWindow;

        public const string SMT_VERSION = "SMT_089";


        private LogonWindow logonBrowserWindow;

        private PreferencesWindow preferencesWindow;

        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        private MediaPlayer mediaPlayer;
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

            Title = "SMT (Pathos, Crashier Test Dummy : " + SMT_VERSION + ")";

            CheckGitHubVersion();

            // Load the Dock Manager Layout file
            string dockManagerLayoutName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMT_VERSION + "\\Layout.dat";
            if (File.Exists(dockManagerLayoutName))
            {
                try
                {
                    AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
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

            EVEManager.UseESIForCharacterPositions = MapConf.UseESIForCharacterPositions;

            // if we want to re-build the data as we've changed the format, recreate it all from scratch
            bool initFromScratch = false;
            if (initFromScratch)
            {
                EVEManager.CreateFromScratch();
            }
            else
            {
                EVEManager.LoadFromDisk();
            }

            EVEManager.SetupIntelWatcher();
            RawIntelBox.ItemsSource = EVEManager.IntelDataList;

            // load jump bridge data
            EVEManager.LoadJumpBridgeData();
            EVEManager.UpdateESIUniverseData();
            EVEManager.InitNavigation();

            CharactersList.ItemsSource = EVEManager.LocalCharacters;
            CurrentActiveCharacterCombo.ItemsSource = EVEManager.LocalCharacters;

            FleetMembersList.DataContext = this;

            TheraConnectionsList.ItemsSource = EVEManager.TheraConnections;
            JumpBridgeList.ItemsSource = EVEManager.JumpBridges;

            SovCampaignList.ItemsSource = EVEManager.ActiveSovCampaigns;
            EVEManager.ActiveSovCampaigns.CollectionChanged += ActiveSovCampaigns_CollectionChanged;
           
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

            ColoursPropertyGrid.SelectedObject = MapConf.ActiveColourScheme;
            ColoursPropertyGrid.PropertyValueChanged += ColoursPropertyGrid_PropertyValueChanged; ;
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
                lc.WarningSystemRange = MapConf.WarningRange;
                lc.Location = "";
            }

        }

        private void ActiveSovCampaigns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(SovCampaignList.ItemsSource).Refresh();
        }


        private void Exit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
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

        private AvalonDock.Layout.LayoutDocument RegionLayoutDoc { get; }
        private AvalonDock.Layout.LayoutDocument UniverseLayoutDoc { get; }

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

                // now serialise the class to disk
                XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
                using (TextWriter tw = new StreamWriter(mapConfigFileName))
                {
                    xms.Serialize(tw, MapConf);
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

        private int uiRefreshCounter = 0;

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            uiRefreshCounter++;
            if (uiRefreshCounter == 5)
            {
                uiRefreshCounter = 0;
                if(FleetMembersList.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
                }

            }
            if (MapConf.SyncActiveCharacterBasedOnActiveEVEClient)
            {
                UpdateCharacterSelectionBasedOnActiveWindow();
            }
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


        #endregion

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

            if (e.PropertyName == "WarningRange")
            {
                foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                {
                    lc.WarningSystemRange = MapConf.WarningRange;
                    lc.warningSystemsNeedsUpdate = true;
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

        private void OnCharacterSelectionChanged()
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

        #endregion

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

        #endregion

        #region Preferences & Options

        private void ColoursPropertyGrid_PropertyValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            RegionUC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, true);
        }

        private void ResetColourData_Click(object sender, RoutedEventArgs e)
        {
            MapConf.SetDefaultColours();
            ColoursPropertyGrid.SelectedObject = MapConf.ActiveColourScheme;
            RegionUC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, true);
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
            preferencesWindow.ShowDialog();
            preferencesWindow.Closed += PreferencesWindow_Closed;
        }

        private void PreferencesWindow_Closed(object sender, EventArgs e)
        {
            RegionUC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, false);
        }

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

        #endregion Preferences & Options

        #region NewVersion

        private void CheckGitHubVersion()
        {
            string url = @"https://api.github.com/repos/slazanger/smt/releases/latest";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;
            request.UserAgent = "SMT/0.xx";

            request.BeginGetResponse(new AsyncCallback(CheckGitHubVersionCallback), request);
        }

        private void CheckGitHubVersionCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();

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
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Characters

        public EVEData.LocalCharacter ActiveCharacter { get; set; }


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

        /// <summary>
        ///  Add Character Button Clicked
        /// </summary>
        private void btn_AddCharacter_Click(object sender, RoutedEventArgs e)
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



        #endregion

        #region intel

        private void RawIntelBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RawIntelBox.SelectedItem == null)
            {
                return;
            }

            EVEData.IntelData intel = RawIntelBox.SelectedItem as EVEData.IntelData;

            foreach (string s in intel.IntelString.Split(' '))
            {
                if (s == "")
                {
                    continue;
                }

                foreach (EVEData.System sys in EVEManager.Systems)
                {
                    if (s.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (RegionUC.Region.Name != sys.Region)
                        {
                            RegionUC.SelectRegion(sys.Region);
                        }

                        RegionUC.SelectSystem(s, true);
                        return;
                    }
                }
            }
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
                            if (lc.WarningSystems != null)
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

            if (playSound )
            {
                mediaPlayer.Stop();
                mediaPlayer.Position = new TimeSpan(0, 0, 0);
                mediaPlayer.Play(); 
            }
        }

        private void ClearIntelBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                EVEManager.IntelDataList.Clear();
            }), DispatcherPriority.ApplicationIdle);
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
                lock (c.ActiveRouteLock)
                {

                    c.ActiveRoute.Clear();
                    c.Waypoints.Clear();
                }
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


        #endregion Route

        #region JumpBridges

        private async void ImportJumpGatesBtn_Click(object sender, RoutedEventArgs e)
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
                    if (c.DeepSearchEnabled)
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

                            Thread.Sleep(2000);
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

            using (StringReader reader = new StringReader(jbText))
            {
                string line = string.Empty;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        // ignore comments
                        if (line.StartsWith("#"))
                        {
                            continue;
                        }

                        string[] bits = line.Split(' ');
                        if (bits.Length > 3)
                        {
                            long IDFrom = 0;
                            long.TryParse(bits[0], out IDFrom);

                            string from = bits[1];
                            string to = bits[3];

                            EVEManager.AddUpdateJumpBridge(from, to, IDFrom);
                        }
                    }
                } while (line != null);
            }

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges.ToList());
            RegionUC.ReDrawMap(true);

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

        private void ClearJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.JumpBridges.Clear();
            EVEData.Navigation.ClearJumpBridges();
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


        #endregion ZKillboard

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

            if (pasteData != null || pasteData != string.Empty)
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

    }



    /// <summary>
    /// TimeSpanConverter Sec statuc colour converter
    /// </summary>
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan ts = (TimeSpan) value;


            string Output = "";

            if(ts.Ticks < 0)
            {
                Output += "-";
            }

            if(ts.Days != 0)
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



}

