using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
        public string SMTVersion = "SMT_081";

        private static NLog.Logger OutputLog = NLog.LogManager.GetCurrentClassLogger();

        private List<UIElement> DynamicUniverseElements = new List<UIElement>();

        private bool FilterByRegion = true;

        private LogonWindow logonBrowserWindow;

        private PreferencesWindow preferencesWindow;

        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        public MainWindow()
        {
            OutputLog.Info("Starting App..");
            AppWindow = this;
            DataContext = this;


            InitializeComponent();



            Title = "SMT (CYNO23 NEWS : " + SMTVersion + ")";

            CheckGitHubVersion();

            string dockManagerLayoutName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMTVersion + "\\Layout.dat";
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

            RegionLayoutDoc = FindDocWithContentID(dockManager.Layout, "MapRegionContentID");
            UniverseLayoutDoc = FindDocWithContentID(dockManager.Layout, "FullUniverseViewID");

            // load any custom map settings off disk
            string mapConfigFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMTVersion + "\\MapConfig.dat";
            OutputLog.Info("Loading Map config from {0}", mapConfigFileName);

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

            EVEManager = new EVEData.EveManager(SMTVersion);
            EVEData.EveManager.Instance = EVEManager;

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

            TheraConnectionsList.ItemsSource = EVEManager.TheraConnections;
            JumpBridgeList.ItemsSource = EVEManager.JumpBridges;

            RegionRC.MapConf = MapConf;
            RegionRC.Init();
            RegionRC.SelectRegion(MapConf.DefaultRegion);

            RegionRC.RegionChanged += RegionRC_RegionChanged;
            RegionRC.CharacterSelectionChanged += RegionRC_CharacterSelectionChanged;
            RegionRC.UniverseSystemSelect += RegionRC_UniverseSystemSelect;

            UniverseUC.MapConf = MapConf;
            UniverseUC.Init();
            UniverseUC.RequestRegionSystem += UniverseUC_RequestRegionSystem;

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

            RegionRC.ANOMManager = ANOMManager;

            List<EVEData.System> globalSystemList = new List<EVEData.System>(EVEManager.Systems);
            globalSystemList.Sort((a, b) => string.Compare(a.Name, b.Name));
            RouteSystemDropDownAC.ItemsSource = globalSystemList;



            ColoursPropertyGrid.SelectedObject = MapConf.ActiveColourScheme;
            ColoursPropertyGrid.PropertyValueChanged += ColoursPropertyGrid_PropertyValueChanged; ;
            MapConf.PropertyChanged += MapConf_PropertyChanged;

            Closed += MainWindow_Closed;

            EVEManager.IntelAddedEvent += OnIntelAdded;
            //MapConf.PropertyChanged += RegionRC.MapObjectChanged;

            AddRegionsToUniverse();

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();

            ZKBFeed.ItemsSource = EVEManager.ZKillFeed.KillStream;

            CollectionView zKBFeedview = (CollectionView)CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource);
            zKBFeedview.Refresh();
            zKBFeedview.Filter = ZKBFeedFilter;

            // update
            foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
            {
                lc.WarningSystemRange = MapConf.WarningRange;
                lc.Location = "";
            }

            RawIntelBox.FontSize = MapConf.IntelTextSize;

            ResetIntelSize();
        }

        public EVEData.AnomManager ANOMManager { get; set; }

        /// <summary>
        /// Main Region Manager
        /// </summary>
        public EVEData.EveManager EVEManager { get; set; }

        public MapConfig MapConf { get; }

        private AvalonDock.Layout.LayoutDocument RegionLayoutDoc { get; }

        private AvalonDock.Layout.LayoutDocument UniverseLayoutDoc { get; }

        public Color stringToColour(string str)
        {
            int hash = 0;

            foreach (char c in str.ToCharArray())
            {
                hash = c + ((hash << 5) - hash);
            }

            double R = (((byte)(hash & 0xff) / 255.0) * 80.0) + 127.0;
            double G = (((byte)((hash >> 8) & 0xff) / 255.0) * 80.0) + 127.0;
            double B = (((byte)((hash >> 16) & 0xff) / 255.0) * 80.0) + 127.0;

            return Color.FromArgb(100, (byte)R, (byte)G, (byte)B);
        }

        private void AddDataToUniverse()
        {
            Brush SysOutlineBrush = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);
            Brush TheraBrush = new SolidColorBrush(MapConf.ActiveColourScheme.TheraEntranceRegion);
            Brush CharacterBrush = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterHighlightColour);

            foreach (EVEData.MapRegion mr in EVEManager.Regions)
            {
                bool AddTheraConnection = false;
                foreach (EVEData.TheraConnection tc in EVEManager.TheraConnections)
                {
                    if (string.Compare(tc.Region, mr.Name, true) == 0)
                    {
                        AddTheraConnection = true;
                        break;
                    }
                }

                if (AddTheraConnection)
                {
                    Rectangle TheraShape = new Rectangle() { Width = 8, Height = 8 };

                    TheraShape.Stroke = SysOutlineBrush;
                    TheraShape.StrokeThickness = 1;
                    TheraShape.StrokeLineJoin = PenLineJoin.Round;
                    TheraShape.RadiusX = 2;
                    TheraShape.RadiusY = 2;
                    TheraShape.Fill = TheraBrush;

                    TheraShape.DataContext = mr;
                    TheraShape.MouseEnter += RegionThera_ShapeMouseOverHandler;
                    TheraShape.MouseLeave += RegionThera_ShapeMouseOverHandler;

                    Canvas.SetLeft(TheraShape, mr.UniverseViewX + 28);
                    Canvas.SetTop(TheraShape, mr.UniverseViewY + 3);
                    Canvas.SetZIndex(TheraShape, 22);
                    MainUniverseCanvas.Children.Add(TheraShape);
                    DynamicUniverseElements.Add(TheraShape);
                }

                bool AddCharacter = false;

                foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                {
                    EVEData.System s = EVEManager.GetEveSystem(lc.Location);
                    if (s != null && s.Region == mr.Name)
                    {
                        AddCharacter = true;
                    }
                }

                if (AddCharacter)
                {
                    Rectangle CharacterShape = new Rectangle() { Width = 8, Height = 8 };

                    CharacterShape.Stroke = SysOutlineBrush;
                    CharacterShape.StrokeThickness = 1;
                    CharacterShape.StrokeLineJoin = PenLineJoin.Round;
                    CharacterShape.RadiusX = 2;
                    CharacterShape.RadiusY = 2;
                    CharacterShape.Fill = CharacterBrush;

                    CharacterShape.DataContext = mr;
                    CharacterShape.MouseEnter += RegionCharacter_ShapeMouseOverHandler;
                    CharacterShape.MouseLeave += RegionCharacter_ShapeMouseOverHandler;

                    Canvas.SetLeft(CharacterShape, mr.UniverseViewX + 28);
                    Canvas.SetTop(CharacterShape, mr.UniverseViewY - 11);
                    Canvas.SetZIndex(CharacterShape, 23);
                    MainUniverseCanvas.Children.Add(CharacterShape);
                    DynamicUniverseElements.Add(CharacterShape);
                }
            }
        }

        private void AddRegionsToUniverse()
        {
            Brush SysOutlineBrush = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);
            Brush SysInRegionBrush = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemColour);
            Brush BackgroundColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);

            Brush AmarrBg = new SolidColorBrush(Color.FromArgb(255, 126, 110, 95));
            Brush MinmatarBg = new SolidColorBrush(Color.FromArgb(255, 143, 120, 120));
            Brush GallenteBg = new SolidColorBrush(Color.FromArgb(255, 127, 139, 137));
            Brush CaldariBg = new SolidColorBrush(Color.FromArgb(255, 149, 159, 171));

            MainUniverseCanvas.Background = BackgroundColourBrush;
            MainUniverseGrid.Background = BackgroundColourBrush;

            foreach (EVEData.MapRegion mr in EVEManager.Regions)
            {
                // add circle for system
                Rectangle RegionShape = new Rectangle() { Height = 30, Width = 80 };
                RegionShape.Stroke = SysOutlineBrush;
                RegionShape.StrokeThickness = 1.5;
                RegionShape.StrokeLineJoin = PenLineJoin.Round;
                RegionShape.RadiusX = 5;
                RegionShape.RadiusY = 5;
                RegionShape.Fill = SysInRegionBrush;
                RegionShape.MouseDown += RegionShape_MouseDown;
                RegionShape.DataContext = mr;

                if (mr.Faction == "Amarr")
                {
                    RegionShape.Fill = AmarrBg;
                }
                if (mr.Faction == "Gallente")
                {
                    RegionShape.Fill = GallenteBg;
                }
                if (mr.Faction == "Minmatar")
                {
                    RegionShape.Fill = MinmatarBg;
                }
                if (mr.Faction == "Caldari")
                {
                    RegionShape.Fill = CaldariBg;
                }

                if (mr.HasHighSecSystems)
                {
                    RegionShape.StrokeThickness = 2.0;
                }

                if (RegionRC.ActiveCharacter != null && RegionRC.ActiveCharacter.ESILinked && MapConf.ShowRegionStandings)
                {
                    float averageStanding = 0.0f;
                    float numSystems = 0;

                    foreach (EVEData.MapSystem s in mr.MapSystems.Values)
                    {
                        if (s.OutOfRegion)
                            continue;

                        numSystems++;

                        if (MapConf.SOVBasedITCU)
                        {
                            if (RegionRC.ActiveCharacter.AllianceID != 0 && RegionRC.ActiveCharacter.AllianceID == s.ActualSystem.SOVAllianceTCU)
                            {
                                averageStanding += 10.0f;
                            }

                            if (s.ActualSystem.SOVAllianceTCU != 0 && RegionRC.ActiveCharacter.Standings.Keys.Contains(s.ActualSystem.SOVAllianceTCU))
                            {
                                averageStanding += RegionRC.ActiveCharacter.Standings[s.ActualSystem.SOVAllianceTCU];
                            }
                        }
                        else
                        {
                            if (RegionRC.ActiveCharacter.AllianceID != 0 && RegionRC.ActiveCharacter.AllianceID == s.ActualSystem.SOVAllianceIHUB)
                            {
                                averageStanding += 10.0f;
                            }

                            if (s.ActualSystem.SOVAllianceTCU != 0 && RegionRC.ActiveCharacter.Standings.Keys.Contains(s.ActualSystem.SOVAllianceIHUB))
                            {
                                averageStanding += RegionRC.ActiveCharacter.Standings[s.ActualSystem.SOVAllianceIHUB];
                            }
                        }

                        if (s.ActualSystem.SOVCorp != 0 && RegionRC.ActiveCharacter.Standings.Keys.Contains(s.ActualSystem.SOVCorp))
                        {
                            averageStanding += RegionRC.ActiveCharacter.Standings[s.ActualSystem.SOVCorp];
                        }
                    }

                    averageStanding = averageStanding / numSystems;

                    if (averageStanding > 0.5)
                    {
                        Color BlueIsh = Colors.Gray;
                        BlueIsh.B += (byte)((255 - BlueIsh.B) * (averageStanding / 10.0f));
                        RegionShape.Fill = new SolidColorBrush(BlueIsh);
                    }
                    else if (averageStanding < -0.5)
                    {
                        averageStanding *= -1;
                        Color RedIsh = Colors.Gray;
                        RedIsh.R += (byte)((255 - RedIsh.R) * (averageStanding / 10.0f));
                        RegionShape.Fill = new SolidColorBrush(RedIsh);
                    }
                    else
                    {
                        RegionShape.Fill = new SolidColorBrush(Colors.Gray);
                    }

                    if (mr.HasHighSecSystems)
                    {
                        RegionShape.Fill = new SolidColorBrush(Colors.LightGray);
                    }
                }

                if (MapConf.ShowUniverseRats)
                {
                    double numRatkills = 0.0f;

                    foreach (EVEData.MapSystem s in mr.MapSystems.Values)
                    {
                        if (s.OutOfRegion)
                            continue;

                        numRatkills += s.ActualSystem.NPCKillsLastHour;
                    }
                    byte b = 255;

                    double ratScale = numRatkills / (15000 * MapConf.UniverseDataScale);
                    ratScale = Math.Min(Math.Max(0.0, ratScale), 1.0);
                    b = (byte)(255.0 * (ratScale));

                    Color c = new Color();
                    c.A = b;
                    c.B = b;
                    c.G = b;
                    c.A = 255;

                    RegionShape.Fill = new SolidColorBrush(c);
                }

                if (MapConf.ShowUniversePods)
                {
                    float numPodKills = 0.0f;

                    foreach (EVEData.MapSystem s in mr.MapSystems.Values)
                    {
                        if (s.OutOfRegion)
                            continue;

                        numPodKills += s.ActualSystem.PodKillsLastHour;
                    }
                    byte b = 255;

                    double podScale = numPodKills / (50 * MapConf.UniverseDataScale);
                    podScale = Math.Min(Math.Max(0.0, podScale), 1.0);
                    b = (byte)(255.0 * (podScale));

                    Color c = new Color();
                    c.A = b;
                    c.R = b;
                    c.G = b;
                    c.A = 255;

                    RegionShape.Fill = new SolidColorBrush(c);
                }

                if (MapConf.ShowUniverseKills)
                {
                    float numShipKills = 0.0f;

                    foreach (EVEData.MapSystem s in mr.MapSystems.Values)
                    {
                        if (s.OutOfRegion)
                            continue;

                        numShipKills += s.ActualSystem.ShipKillsLastHour;
                    }
                    byte b = 255;
                    double shipScale = numShipKills / (100 * MapConf.UniverseDataScale);
                    shipScale = Math.Min(Math.Max(0.0, shipScale), 1.0);
                    b = (byte)(255.0 * (shipScale));

                    Color c = new Color();
                    c.A = b;
                    c.R = b;
                    c.B = b;
                    c.A = 255;
                    RegionShape.Fill = new SolidColorBrush(c);
                }

                Canvas.SetLeft(RegionShape, mr.UniverseViewX - 40);
                Canvas.SetTop(RegionShape, mr.UniverseViewY - 15);
                Canvas.SetZIndex(RegionShape, 22);
                MainUniverseCanvas.Children.Add(RegionShape);

                Label RegionText = new Label();
                RegionText.Width = 80;
                RegionText.Height = 27;
                RegionText.Content = mr.Name;
                RegionText.Foreground = SysOutlineBrush;
                RegionText.FontSize = 10;
                RegionText.HorizontalAlignment = HorizontalAlignment.Center;
                RegionText.VerticalAlignment = VerticalAlignment.Center;
                RegionText.IsHitTestVisible = false;

                RegionText.HorizontalContentAlignment = HorizontalAlignment.Center;
                RegionText.VerticalContentAlignment = VerticalAlignment.Center;

                Canvas.SetLeft(RegionText, mr.UniverseViewX - 40);
                Canvas.SetTop(RegionText, mr.UniverseViewY - 15);
                Canvas.SetZIndex(RegionText, 23);
                MainUniverseCanvas.Children.Add(RegionText);

                if (mr.Faction != "")
                {
                    Label FactionText = new Label();
                    FactionText.Width = 80;
                    FactionText.Height = 30;
                    FactionText.Content = mr.Faction;
                    FactionText.Foreground = SysOutlineBrush;
                    FactionText.FontSize = 5;
                    FactionText.HorizontalAlignment = HorizontalAlignment.Center;
                    FactionText.VerticalAlignment = VerticalAlignment.Center;
                    FactionText.IsHitTestVisible = false;

                    FactionText.HorizontalContentAlignment = HorizontalAlignment.Center;
                    FactionText.VerticalContentAlignment = VerticalAlignment.Bottom;

                    Canvas.SetLeft(FactionText, mr.UniverseViewX - 40);
                    Canvas.SetTop(FactionText, mr.UniverseViewY - 20);
                    Canvas.SetZIndex(FactionText, 23);
                    MainUniverseCanvas.Children.Add(FactionText);
                }

                // now add all the region links
                foreach (string s in mr.RegionLinks)
                {
                    EVEData.MapRegion or = EVEManager.GetRegion(s);
                    Line regionLink = new Line();

                    regionLink.X1 = mr.UniverseViewX;
                    regionLink.Y1 = mr.UniverseViewY;

                    regionLink.X2 = or.UniverseViewX;
                    regionLink.Y2 = or.UniverseViewY;

                    regionLink.Stroke = SysOutlineBrush;
                    regionLink.StrokeThickness = 1;
                    regionLink.Visibility = Visibility.Visible;

                    Canvas.SetZIndex(regionLink, 21);
                    MainUniverseCanvas.Children.Add(regionLink);
                }
            }
        }

        private void btn_AddCharacter_Click(object sender, RoutedEventArgs e)
        {
            string eSILogonURL = EVEManager.GetESILogonURL();

            if (logonBrowserWindow != null)
            {
                logonBrowserWindow.Close();
            }
            System.Diagnostics.Process.Start(eSILogonURL);

            logonBrowserWindow = new LogonWindow();
            logonBrowserWindow.Owner = this;
            logonBrowserWindow.ShowDialog();
        }

        private void btn_UpdateThera_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.UpdateTheraConnections();
        }

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

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            About popup = new About();
            popup.ShowDialog();
        }

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
                        RegionRC.SelectSystem(lc.Location, true);
                    }
                }
            }
        }

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
                            if (releaseInfo.TagName != SMTVersion)
                            {
                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                {
                                    NewVersionWindow nw = new NewVersionWindow();
                                    nw.ReleaseInfo = releaseInfo.Body;
                                    nw.CurrentVersion = SMTVersion;
                                    nw.NewVersion = releaseInfo.TagName;
                                    nw.ReleaseURL = releaseInfo.HtmlUrl.ToString();
                                    nw.Show();
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

        private void ClearIntelBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                EVEManager.IntelDataList.Clear();
            }), DispatcherPriority.ApplicationIdle);
        }

        private void ClearJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.JumpBridges.Clear();
            EVEData.Navigation.ClearJumpBridges();
        }

        private void ClearWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionRC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null)
            {
                lock (c.ActiveRouteLock)
                {
                    c.ActiveRoute.Clear();
                    c.Waypoints.Clear();
                }
            }
        }

        private void ColoursPropertyGrid_PropertyValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            RegionRC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, true);
        }

        private void CopyRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionRC.ActiveCharacter as EVEData.LocalCharacter;
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

        private async void ImportJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            ImportJumpGatesBtn.IsEnabled = false;
            ClearJumpGatesBtn.IsEnabled = false;

            foreach (EVEData.LocalCharacter c in EVEManager.LocalCharacters)
            {
                if (c.ESILinked)
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

            EVEData.Navigation.UpdateJumpBridges(EVEManager.JumpBridges.ToList());

            ImportJumpGatesBtn.IsEnabled = true;
            ClearJumpGatesBtn.IsEnabled = true;
        }

        private void ImportPasteJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Text))
            {
                return;
            }
            String JBText = Clipboard.GetText(TextDataFormat.Text);

            using (StringReader reader = new StringReader(JBText))
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
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // save off the dockmanager layout

            string dockManagerLayoutName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMTVersion + "\\Layout.dat";
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
                // Save the Map Colours
                string mapConfigFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SMT\\" + SMTVersion + "\\MapConfig.dat";

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
            { }

            // save the character data
            EVEManager.SaveData();
            EVEManager.ShutDown();
        }

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

            RegionRC.ReDrawMap(true);

            if (e.PropertyName == "ShowRegionStandings")
            {
                RedrawUniverse(true);
            }

            if (e.PropertyName == "ShowUniverseRats")
            {
                RedrawUniverse(true);
            }

            if (e.PropertyName == "ShowUniversePods")
            {
                RedrawUniverse(true);
            }

            if (e.PropertyName == "ShowUniverseKills")
            {
                RedrawUniverse(true);
            }

            if (e.PropertyName == "UniverseDataScale")
            {
                RedrawUniverse(true);
            }

            if (e.PropertyName == "IntelTextSize")
            {
                ResetIntelSize();
            }
        }

        private void MenuItem_ViewIntelClick(object sender, RoutedEventArgs e)
        {
        }

        private void OnIntelAdded(List<string> intelsystems)
        {
            bool PlaySound = false;

            if (MapConf.PlayIntelSound)
            {
                if (MapConf.PlaySoundOnlyInDangerZone)
                {
                    if (MapConf.PlayIntelSoundOnUnknown && intelsystems.Count == 0)
                    {
                        PlaySound = true;
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
                                        PlaySound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    PlaySound = true;
                }
            }

            if (PlaySound)
            {
                Uri uri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                var player = new MediaPlayer();
                player.Open(uri);
                player.Play();
            }
        }

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
                        if (RegionRC.Region.Name != sys.Region)
                        {
                            RegionRC.SelectRegion(sys.Region);
                        }

                        RegionRC.SelectSystem(s, true);
                        return;
                    }
                }
            }
        }

        private void RedrawUniverse(bool Redraw)
        {
            if (Redraw)
            {
                MainUniverseCanvas.Children.Clear();
                AddRegionsToUniverse();
            }
            else
            {
                foreach (UIElement uie in DynamicUniverseElements)
                {
                    MainUniverseCanvas.Children.Remove(uie);
                }
                DynamicUniverseElements.Clear();
            }

            AddDataToUniverse();
        }



        private void RegionCharacter_ShapeMouseOverHandler(object sender, MouseEventArgs e)
        {
            Shape obj = sender as Shape;

            EVEData.MapRegion selectedRegion = obj.DataContext as EVEData.MapRegion;

            if (obj.IsMouseOver)
            {
                RegionCharacterInfo.PlacementTarget = obj;
                RegionCharacterInfo.VerticalOffset = 5;
                RegionCharacterInfo.HorizontalOffset = 15;

                RegionCharacterInfoSP.Children.Clear();

                foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                {
                    EVEData.System s = EVEManager.GetEveSystem(lc.Location);
                    if (s != null && s.Region == selectedRegion.Name)
                    {
                        Label l = new Label();
                        l.Content = lc.Name + " (" + lc.Location + ")";
                        RegionCharacterInfoSP.Children.Add(l);
                    }
                }

                RegionCharacterInfo.IsOpen = true;
            }
            else
            {
                RegionCharacterInfo.IsOpen = false;
            }
        }

        private void RegionRC_CharacterSelectionChanged(object sender, PropertyChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource).Refresh();
        }

        private void RegionRC_RegionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (RegionLayoutDoc != null)
            {
                RegionLayoutDoc.Title = RegionRC.Region.Name;
            }

            CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource).Refresh();
        }

        private void RegionRC_UniverseSystemSelect(object sender, RoutedEventArgs e)
        {
            string sysName = e.OriginalSource as string;
            UniverseUC.ShowSystem(sysName);

            if (UniverseLayoutDoc != null)
            {
                UniverseLayoutDoc.IsSelected = true;
            }
        }

        private void RegionShape_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Shape obj = sender as Shape;
            EVEData.MapRegion mr = obj.DataContext as EVEData.MapRegion;
            if (mr == null)
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                RegionRC.SelectRegion(mr.Name);
                if (RegionLayoutDoc != null)
                {
                    RegionLayoutDoc.IsSelected = true;
                }
            }
        }

        private void RegionThera_ShapeMouseOverHandler(object sender, MouseEventArgs e)
        {
            Shape obj = sender as Shape;

            EVEData.MapRegion selectedRegion = obj.DataContext as EVEData.MapRegion;

            if (obj.IsMouseOver)
            {
                RegionTheraInfo.PlacementTarget = obj;
                RegionTheraInfo.VerticalOffset = 5;
                RegionTheraInfo.HorizontalOffset = 15;

                RegionTheraInfoSP.Children.Clear();

                Label header = new Label();
                header.Content = "Thera Connections";
                header.FontWeight = FontWeights.Bold;
                header.Margin = new Thickness(1);
                header.Padding = new Thickness(1);
                RegionTheraInfoSP.Children.Add(header);

                foreach (EVEData.TheraConnection tc in EVEManager.TheraConnections)
                {
                    if (string.Compare(tc.Region, selectedRegion.Name, true) == 0)
                    {
                        Label l = new Label();
                        l.Content = $"    {tc.System}";
                        l.Margin = new Thickness(1);
                        l.Padding = new Thickness(1);

                        RegionTheraInfoSP.Children.Add(l);
                    }
                }

                RegionTheraInfo.IsOpen = true;
            }
            else
            {
                RegionTheraInfo.IsOpen = false;
            }
        }

        private void ResetColourData_Click(object sender, RoutedEventArgs e)
        {
            MapConf.SetDefaultColours();
            ColoursPropertyGrid.SelectedObject = MapConf.ActiveColourScheme;
            RegionRC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, true);
        }

        private void ResetIntelSize()
        {
            var RawIntelTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            RawIntelTextBlockFactory.SetValue(TextBlock.TextProperty, new Binding("."));
            RawIntelTextBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            RawIntelTextBlockFactory.SetValue(TextBlock.FontSizeProperty, MapConf.IntelTextSize);
            RawIntelTextBlockFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
            var RawIntelTextTemplate = new DataTemplate();
            RawIntelTextTemplate.VisualTree = RawIntelTextBlockFactory;

            RawIntelBox.ItemTemplate = RawIntelTextTemplate;
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
                        RegionRC.SelectSystem(tc.System, true);
                    }
                }
            }
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            RedrawUniverse(false);
        }

        private void UniverseUC_RequestRegionSystem(object sender, RoutedEventArgs e)
        {
            string sysName = e.OriginalSource as string;
            RegionRC.FollowCharacter = false;
            RegionRC.SelectSystem(sysName, true);

            if (RegionLayoutDoc != null)
            {
                RegionLayoutDoc.IsSelected = true;
            }
        }

        private void UpdateLocalFromClipboardBtn_Click(object sender, RoutedEventArgs e)
        {
            string pasteData = Clipboard.GetText();

            List<string> CharactersToResolve = new List<string>();
            if (pasteData != null || pasteData != string.Empty)
            {
                string[] localItems = pasteData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                foreach (String item in localItems)
                {
                    if (item.All(x => char.IsLetterOrDigit(x) || char.IsWhiteSpace(x) || x == '-' || x == '\''))
                    {
                        CharactersToResolve.Add(item);
                    }
                }
            }

            if (CharactersToResolve.Count > 0)
            {
                EVEManager.BulkUpdateCharacterCache(CharactersToResolve);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ZKBContexMenu_ShowSystem_Click(object sender, RoutedEventArgs e)
        {
            if (ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if (zkbs != null)
            {
                RegionRC.SelectSystem(zkbs.SystemName, true);

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
            if (FilterByRegion == false)
            {
                return true;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zs = item as EVEData.ZKillRedisQ.ZKBDataSimple;
            if (zs == null)
            {
                return false;
            }

            if (RegionRC.Region.IsSystemOnMap(zs.SystemName))
            {
                return true;
            }

            return false;
        }

        private void ZKBFeedFilterViewChk_Checked(object sender, RoutedEventArgs e)
        {
            FilterByRegion = (bool)ZKBFeedFilterViewChk.IsChecked;

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
            RegionRC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, false);
        }

        private void ForceESIUpdate_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.UpdateESIUniverseData();
        }

        private void RouteSystemDropDownAC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            if(RegionRC.ActiveCharacter == null)
            {
                return;
            }

            if(RouteSystemDropDownAC.SelectedItem == null)
            {
                return;
            }
            EVEData.System s = RouteSystemDropDownAC.SelectedItem as EVEData.System;
            

            if(s != null)
            {
                RegionRC.ActiveCharacter.AddDestination(s.ID, false);
            }
        }

        private void FullScreenToggle_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(miFullScreenToggle.IsChecked)
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



        /*

        private void JPSuggestRoute_Click(object sender, RoutedEventArgs e)
        {
            if (JPShipType.SelectedItem == null)
            {
                return;
            }

            EVEData.EveManager.JumpShip js = (EVEData.EveManager.JumpShip)JPShipType.SelectedItem;
            double JumpDistance = 0.0;

            switch (js)
            {
                case EVEData.EveManager.JumpShip.Super: { JumpDistance = 6.0; } break;
                case EVEData.EveManager.JumpShip.Titan: { JumpDistance = 6.0; } break;
                case EVEData.EveManager.JumpShip.Dread: { JumpDistance = 7.0; } break;
                case EVEData.EveManager.JumpShip.Carrier: { JumpDistance = 7.0; } break;
                case EVEData.EveManager.JumpShip.FAX: { JumpDistance = 7.0; } break;
                case EVEData.EveManager.JumpShip.Blops: { JumpDistance = 8.0; } break;
                case EVEData.EveManager.JumpShip.Rorqual: { JumpDistance = 10.0; } break;
                case EVEData.EveManager.JumpShip.JF: { JumpDistance = 10.0; } break;
            }
            {
                List<EVEData.Navigation.RoutePoint> rpl = EVEData.Navigation.NavigateCapitals("B-II34", "Obe", JumpDistance);
                JPSuggestedRoute.ItemsSource = rpl;

                UniverseUC.ActiveRoute = rpl;
            }
        }

        */
    }

    public class ZKBBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EVEData.ZKillRedisQ.ZKBDataSimple zs = value as EVEData.ZKillRedisQ.ZKBDataSimple;
            Color rowCol = (Color)ColorConverter.ConvertFromString("#FF333333");
            if (zs != null)
            {
                float Standing = 0.0f;

                EVEData.LocalCharacter c = MainWindow.AppWindow.RegionRC.ActiveCharacter;
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