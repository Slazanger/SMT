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

        public const string SMT_VERSION = "SMT_082";

        private List<UIElement> dynamicRegionsViewElements = new List<UIElement>();

        private bool filterByRegion = true;

        private LogonWindow logonBrowserWindow;

        private PreferencesWindow preferencesWindow;

        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        /// <summary>
        /// Main Window
        /// </summary>
        public MainWindow()
        {
            AppWindow = this;
            DataContext = this;

            InitializeComponent();

            Title = "SMT (CYNO23 NEWS : " + SMT_VERSION + ")";

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

            AddRegionsToUniverse();

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

        /// <summary>
        /// Add Data to the Universe (Thera, Characters etc)
        /// </summary>
        private void AddDataToUniverse()
        {
            Brush sysOutlineBrush = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);
            Brush theraBrush = new SolidColorBrush(MapConf.ActiveColourScheme.TheraEntranceRegion);
            Brush characterBrush = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterHighlightColour);

            foreach (EVEData.MapRegion mr in EVEManager.Regions)
            {
                bool addTheraConnection = false;
                foreach (EVEData.TheraConnection tc in EVEManager.TheraConnections)
                {
                    if (string.Compare(tc.Region, mr.Name, true) == 0)
                    {
                        addTheraConnection = true;
                        break;
                    }
                }

                if (addTheraConnection)
                {
                    Rectangle theraShape = new Rectangle() { Width = 8, Height = 8 };

                    theraShape.Stroke = sysOutlineBrush;
                    theraShape.StrokeThickness = 1;
                    theraShape.StrokeLineJoin = PenLineJoin.Round;
                    theraShape.RadiusX = 2;
                    theraShape.RadiusY = 2;
                    theraShape.Fill = theraBrush;

                    theraShape.DataContext = mr;
                    theraShape.MouseEnter += RegionThera_ShapeMouseOverHandler;
                    theraShape.MouseLeave += RegionThera_ShapeMouseOverHandler;

                    Canvas.SetLeft(theraShape, mr.UniverseViewX + 28);
                    Canvas.SetTop(theraShape, mr.UniverseViewY + 3);
                    Canvas.SetZIndex(theraShape, 22);
                    MainUniverseCanvas.Children.Add(theraShape);
                    dynamicRegionsViewElements.Add(theraShape);
                }

                bool addCharacter = false;

                foreach (EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                {
                    EVEData.System s = EVEManager.GetEveSystem(lc.Location);
                    if (s != null && s.Region == mr.Name)
                    {
                        addCharacter = true;
                    }
                }

                if (addCharacter)
                {
                    Rectangle characterShape = new Rectangle() { Width = 8, Height = 8 };

                    characterShape.Stroke = sysOutlineBrush;
                    characterShape.StrokeThickness = 1;
                    characterShape.StrokeLineJoin = PenLineJoin.Round;
                    characterShape.RadiusX = 2;
                    characterShape.RadiusY = 2;
                    characterShape.Fill = characterBrush;

                    characterShape.DataContext = mr;
                    characterShape.MouseEnter += RegionCharacter_ShapeMouseOverHandler;
                    characterShape.MouseLeave += RegionCharacter_ShapeMouseOverHandler;

                    Canvas.SetLeft(characterShape, mr.UniverseViewX + 28);
                    Canvas.SetTop(characterShape, mr.UniverseViewY - 11);
                    Canvas.SetZIndex(characterShape, 23);
                    MainUniverseCanvas.Children.Add(characterShape);
                    dynamicRegionsViewElements.Add(characterShape);
                }
            }
        }

        /// <summary>
        /// Add the regions to the universe view
        /// </summary>
        private void AddRegionsToUniverse()
        {
            Brush sysOutlineBrush = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);
            Brush sysInRegionBrush = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemColour);
            Brush backgroundColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);

            Brush amarrBg = new SolidColorBrush(Color.FromArgb(255, 126, 110, 95));
            Brush minmatarBg = new SolidColorBrush(Color.FromArgb(255, 143, 120, 120));
            Brush gallenteBg = new SolidColorBrush(Color.FromArgb(255, 127, 139, 137));
            Brush caldariBg = new SolidColorBrush(Color.FromArgb(255, 149, 159, 171));

            MainUniverseCanvas.Background = backgroundColourBrush;
            MainUniverseGrid.Background = backgroundColourBrush;

            foreach (EVEData.MapRegion mr in EVEManager.Regions)
            {
                // add circle for system
                Rectangle regionShape = new Rectangle() { Height = 30, Width = 80 };
                regionShape.Stroke = sysOutlineBrush;
                regionShape.StrokeThickness = 1.5;
                regionShape.StrokeLineJoin = PenLineJoin.Round;
                regionShape.RadiusX = 5;
                regionShape.RadiusY = 5;
                regionShape.Fill = sysInRegionBrush;
                regionShape.MouseDown += RegionShape_MouseDown;
                regionShape.DataContext = mr;

                if (mr.Faction == "Amarr")
                {
                    regionShape.Fill = amarrBg;
                }
                if (mr.Faction == "Gallente")
                {
                    regionShape.Fill = gallenteBg;
                }
                if (mr.Faction == "Minmatar")
                {
                    regionShape.Fill = minmatarBg;
                }
                if (mr.Faction == "Caldari")
                {
                    regionShape.Fill = caldariBg;
                }

                if (mr.HasHighSecSystems)
                {
                    regionShape.StrokeThickness = 2.0;
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
                        Color blueIsh = Colors.Gray;
                        blueIsh.B += (byte)((255 - blueIsh.B) * (averageStanding / 10.0f));
                        regionShape.Fill = new SolidColorBrush(blueIsh);
                    }
                    else if (averageStanding < -0.5)
                    {
                        averageStanding *= -1;
                        Color redIsh = Colors.Gray;
                        redIsh.R += (byte)((255 - redIsh.R) * (averageStanding / 10.0f));
                        regionShape.Fill = new SolidColorBrush(redIsh);
                    }
                    else
                    {
                        regionShape.Fill = new SolidColorBrush(Colors.Gray);
                    }

                    if (mr.HasHighSecSystems)
                    {
                        regionShape.Fill = new SolidColorBrush(Colors.LightGray);
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

                    regionShape.Fill = new SolidColorBrush(c);
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

                    regionShape.Fill = new SolidColorBrush(c);
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
                    regionShape.Fill = new SolidColorBrush(c);
                }

                Canvas.SetLeft(regionShape, mr.UniverseViewX - 40);
                Canvas.SetTop(regionShape, mr.UniverseViewY - 15);
                Canvas.SetZIndex(regionShape, 22);
                MainUniverseCanvas.Children.Add(regionShape);

                Label regionText = new Label();
                regionText.Width = 80;
                regionText.Height = 27;
                regionText.Content = mr.Name;
                regionText.Foreground = sysOutlineBrush;
                regionText.FontSize = 10;
                regionText.HorizontalAlignment = HorizontalAlignment.Center;
                regionText.VerticalAlignment = VerticalAlignment.Center;
                regionText.IsHitTestVisible = false;

                regionText.HorizontalContentAlignment = HorizontalAlignment.Center;
                regionText.VerticalContentAlignment = VerticalAlignment.Center;

                Canvas.SetLeft(regionText, mr.UniverseViewX - 40);
                Canvas.SetTop(regionText, mr.UniverseViewY - 15);
                Canvas.SetZIndex(regionText, 23);
                MainUniverseCanvas.Children.Add(regionText);

                if (!string.IsNullOrEmpty(mr.Faction))
                {
                    Label factionText = new Label();
                    factionText.Width = 80;
                    factionText.Height = 30;
                    factionText.Content = mr.Faction;
                    factionText.Foreground = sysOutlineBrush;
                    factionText.FontSize = 6;
                    factionText.HorizontalAlignment = HorizontalAlignment.Center;
                    factionText.VerticalAlignment = VerticalAlignment.Center;
                    factionText.IsHitTestVisible = false;

                    factionText.HorizontalContentAlignment = HorizontalAlignment.Center;
                    factionText.VerticalContentAlignment = VerticalAlignment.Bottom;

                    Canvas.SetLeft(factionText, mr.UniverseViewX - 40);
                    Canvas.SetTop(factionText, mr.UniverseViewY - 15);
                    Canvas.SetZIndex(factionText, 23);
                    MainUniverseCanvas.Children.Add(factionText);
                }

                // now add all the region links : TODO :  this will end up adding 2 lines, region a -> b and b -> a
                foreach (string s in mr.RegionLinks)
                {
                    EVEData.MapRegion or = EVEManager.GetRegion(s);
                    Line regionLink = new Line();

                    regionLink.X1 = mr.UniverseViewX;
                    regionLink.Y1 = mr.UniverseViewY;

                    regionLink.X2 = or.UniverseViewX;
                    regionLink.Y2 = or.UniverseViewY;

                    regionLink.Stroke = sysOutlineBrush;
                    regionLink.StrokeThickness = 1;
                    regionLink.Visibility = Visibility.Visible;

                    Canvas.SetZIndex(regionLink, 21);
                    MainUniverseCanvas.Children.Add(regionLink);
                }
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


        private void RedrawUniverse(bool redraw)
        {
            if (redraw)
            {
                MainUniverseCanvas.Children.Clear();
                AddRegionsToUniverse();
            }
            else
            {
                foreach (UIElement uie in dynamicRegionsViewElements)
                {
                    MainUniverseCanvas.Children.Remove(uie);
                }
                dynamicRegionsViewElements.Clear();
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
        private int uiRefreshCounter = 0;

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            uiRefreshCounter++;
            if (uiRefreshCounter == 5)
            {
                RedrawUniverse(false);
                uiRefreshCounter = 0;
            }
            if (MapConf.SyncActiveCharacterBasedOnActiveEVEClient)
            {
                UpdateCharacterSelectionBasedOnActiveWindow();
            }
        }



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

        #endregion

        #region Universe Control 

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

        #endregion

        #region Preferences & Options

        private void ColoursPropertyGrid_PropertyValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            RegionRC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, true);
        }

        private void ResetColourData_Click(object sender, RoutedEventArgs e)
        {
            MapConf.SetDefaultColours();
            ColoursPropertyGrid.SelectedObject = MapConf.ActiveColourScheme;
            RegionRC.ReDrawMap(true);
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
            RegionRC.ReDrawMap(true);
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
                        RegionRC.CharacterDropDown.SelectedItem = lc;
                        RegionRC.ActiveCharacter = lc;
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
            string esiLogonURL = EVEManager.GetESILogonURL();

            if (logonBrowserWindow != null)
            {
                logonBrowserWindow.Close();
            }
            System.Diagnostics.Process.Start(esiLogonURL);

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
                        RegionRC.SelectSystem(lc.Location, true);
                    }
                }
            }
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

            if (playSound)
            {
                Uri uri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                var player = new MediaPlayer();
                player.Open(uri);
                player.Play();
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
                        RegionRC.SelectSystem(tc.System, true);
                    }
                }
            }
        }

        #endregion Thera

        #region Route

        private void AddWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RegionRC.ActiveCharacter == null)
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
                RegionRC.ActiveCharacter.AddDestination(s.ID, false);
            }
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

        private void ReCalculateRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionRC.ActiveCharacter as EVEData.LocalCharacter;
            if (c != null && c.Waypoints.Count > 0)
            {
                c.RecalcRoute();
            }
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
            RegionRC.ReDrawMap(true);

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
            RegionRC.ReDrawMap(true);

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
            RegionRC.ReDrawMap(true);

            EVEData.LocalCharacter c = RegionRC.ActiveCharacter as EVEData.LocalCharacter;
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
            RegionRC.ReDrawMap(true);

            EVEData.LocalCharacter c = RegionRC.ActiveCharacter as EVEData.LocalCharacter;
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
            if (filterByRegion == false)
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
            filterByRegion = (bool)ZKBFeedFilterViewChk.IsChecked;

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

