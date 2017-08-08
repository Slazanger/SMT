using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace SMT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Main Region Manager
        /// </summary>
        public EVEData.EveManager EVEManager { get; set; }

        public EVEData.AnomManager ANOMManager { get; set; }

        /// <summary>
        /// Show the NPC kills in the last hour on the map
        /// </summary>
        public bool ShowNPCKills { get; set; }

        /// <summary>
        /// Show the Pod Kills in the last hour on the map
        /// </summary>
        public bool ShowPodKills { get; set; }

        /// <summary>
        /// Show the Ship Kills in the last hour on the map
        /// </summary>
        public bool ShowShipKills { get; set; }

        /// <summary>
        /// Show the number of jumps on the mapo
        /// </summary>
        public bool ShowJumps { get; set; }

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

        public string SelectedSystem { get; set; }

        private MapConfig MapConf;

        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        public MainWindow()
        {
            InitializeComponent();

            // load any custom map settings off disk
            string mapConfigFileName = AppDomain.CurrentDomain.BaseDirectory + @"\MapConfig.dat";
            if (File.Exists(mapConfigFileName))
            {
                try
                {
                    XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
                    FileStream fs = new FileStream(mapConfigFileName, FileMode.Open);
                    XmlReader xmlr = XmlReader.Create(fs);

                    MapConf = (MapConfig)xms.Deserialize(xmlr);
                }
                catch
                {
                    MapConf = new MapConfig();
                    MapConf.MapColours = new List<MapColours>();
                    MapConf.SetDefaultColours();
                }
            }
            else
            {
                MapConf = new MapConfig();
                MapConf.MapColours = new List<MapColours>();
                MapConf.SetDefaultColours();
            }

            ShowNPCKills = false;
            ShowPodKills = true;
            ShowShipKills = false;
            ShowJumps = false;

            SelectedSystem = string.Empty;

            EVEManager = new EVEData.EveManager();
            EVEData.EveManager.SetInstance(EVEManager);

            // if we want to re-build the data as we've changed the format, recreate it all from DOTLAN
            bool initFromScratch = true;
            if (initFromScratch)
            {
                EVEManager.CreateFromScratch();
            }
            else
            {
                EVEManager.LoadFromDisk();
            }


            RegionDropDown.ItemsSource = EVEManager.Regions;

            EVEManager.SetupIntelWatcher();
            RawIntelBox.ItemsSource = EVEManager.IntelDataList;

            // load jump bridge data
            EVEManager.LoadJumpBridgeData();
            EVEManager.StartUpdateKillsFromESI();
            EVEManager.StartUpdateJumpsFromESI();

            foreach (EVEData.MapRegion rd in EVEManager.Regions)
            {
                if (rd.Name == MapConf.DefaultRegion)
                {
                    RegionDropDown.SelectedItem = rd;
                    List<EVEData.MapSystem> newList = rd.MapSystems.Values.ToList().OrderBy(o => o.Name).ToList();
                    SystemDropDownAC.ItemsSource = newList;
                }
            }


            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();

            CharacterDropDown.ItemsSource = EVEManager.LocalCharacters;

            MapControlsPropertyGrid.SelectedObject = MapConf;
            MapControlsPropertyGrid.PropertyValueChanged += MapControlsPropertyGrid_PropertyValueChanged;

            ColourListDropdown.ItemsSource = MapConf.MapColours;

            MapColours selectedColours = MapConf.MapColours[0];

            // find the matching active colour scheme
            foreach (MapColours mc in MapConf.MapColours)
            {
                if (MapConf.DefaultColourSchemeName == mc.Name)
                {
                    selectedColours = mc;
                }
            }

            // load the dockmanager layout
            string dockManagerLayoutName = AppDomain.CurrentDomain.BaseDirectory + @"\Layout.dat";
            if (File.Exists(dockManagerLayoutName))
            {
                try
                {
                    Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                    using (var sr = new StreamReader(dockManagerLayoutName))
                    {
                        ls.Deserialize(sr);
                    }
                }
                catch
                {
                }
            }

            // load the anom data
            string anomDataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Anoms.dat";
            if (File.Exists(anomDataFilename))
            {
                try
                {
                    XmlSerializer xms = new XmlSerializer(typeof(EVEData.AnomManager));

                    FileStream fs = new FileStream(anomDataFilename, FileMode.Open);
                    XmlReader xmlr = XmlReader.Create(fs);

                    ANOMManager = (EVEData.AnomManager)xms.Deserialize(xmlr);
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

            // ColourListDropdown.SelectedItem = selectedColours;
            ColoursPropertyGrid.SelectedObject = selectedColours;
            MapConf.ActiveColourScheme = selectedColours;
            ColoursPropertyGrid.PropertyChanged += ColoursPropertyGrid_PropertyChanged;
            ReDrawMap();

            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // save off the dockmanager layout
            string dockManagerLayoutName = AppDomain.CurrentDomain.BaseDirectory + @"\Layout.dat";
            try
            {
                Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                using (var sw = new StreamWriter(dockManagerLayoutName))
                {
                    ls.Serialize(sw);
                }
            }
            catch
            {
            }

            // Save the Map Colours
            string mapConfigFileName = AppDomain.CurrentDomain.BaseDirectory + @"\MapConfig.dat";

            // now serialise the class to disk
            XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
            using (TextWriter tw = new StreamWriter(mapConfigFileName))
            {
                xms.Serialize(tw, MapConf);
            }

            // save the Anom Data
            // now serialise the class to disk
            XmlSerializer anomxms = new XmlSerializer(typeof(EVEData.AnomManager));
            string anomDataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Anoms.dat";

            using (TextWriter tw = new StreamWriter(anomDataFilename))
            {
                anomxms.Serialize(tw, ANOMManager);
            }

            // save the character data
            EVEManager.SaveCharacters();
        }

        private void MapControlsPropertyGrid_PropertyValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            ReDrawMap();
        }

        private void ColoursPropertyGrid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReDrawMap();
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            ReDrawMap();
        }

        private void ReDrawMap()
        {
            if (CharacterDropDown.SelectedItem != null && FollowCharacter == true)
            {
                HandleCharacterSelectionChange();
            }

            MainCanvasGrid.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);
            MainCanvas.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);

            MainCanvas.Children.Clear();
            AddSystemsToMap();
            AddHighlightToSystem(SelectedSystem);
        }

        private void ShapeMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            Shape obj = sender as Shape;
            EVEData.MapRegion currentRegion = RegionDropDown.SelectedItem as EVEData.MapRegion;

            EVEData.MapSystem selectedSys = obj.DataContext as EVEData.MapSystem;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    ReDrawMap();
                    SelectSystem(selectedSys.Name);
                }

                if (e.ClickCount == 2 && selectedSys.Region != currentRegion.Name)
                {
                    foreach (EVEData.MapRegion rd in EVEManager.Regions)
                    {
                        if (rd.Name == selectedSys.Region)
                        {
                            RegionDropDown.SelectedItem = rd;

                            ReDrawMap();
                            SelectSystem(selectedSys.Name);
                            break;
                        }
                    }
                }
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                ContextMenu cm = this.FindResource("SysRightClickContextMenu") as ContextMenu;
                cm.PlacementTarget = obj;
                cm.DataContext = selectedSys;

                MenuItem setDesto = cm.Items[2] as MenuItem;
                MenuItem addWaypoint = cm.Items[3] as MenuItem;


                setDesto.IsEnabled = false;
                addWaypoint.IsEnabled = false;

                EVEData.Character c = CharacterDropDown.SelectedItem as EVEData.Character;
                if(c != null && c.ESILinked)
                {
                    setDesto.IsEnabled = true;
                    addWaypoint.IsEnabled = true;
                }

            

                cm.IsOpen = true;
            }
        }

        private void ShapeMouseOverHandler(object sender, MouseEventArgs e)
        {
            Shape obj = sender as Shape;
            EVEData.MapRegion currentRegion = RegionDropDown.SelectedItem as EVEData.MapRegion;

            EVEData.MapSystem selectedSys = obj.DataContext as EVEData.MapSystem;

            if (obj.IsMouseOver && MapConf.ShowSystemPopup)
            {
                SystemInfoPopup.PlacementTarget = obj;
                SystemInfoPopup.VerticalOffset = 5;
                SystemInfoPopup.HorizontalOffset = 15;
                SystemInfoPopup.DataContext = selectedSys.ActualSystem;

                SystemInfoPopup.IsOpen = true;
            }
            else
            {
                SystemInfoPopup.IsOpen = false;
            }
        }

        private void SelectSystem(string name)
        {
            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            foreach (EVEData.MapSystem es in rd.MapSystems.Values.ToList())
            {
                if (es.Name == name)
                {
                    SystemDropDownAC.SelectedItem = es;
                    SelectedSystem = es.Name;
                    AddHighlightToSystem(name);

                    break;
                }
            }

            // now setup the anom data
            EVEData.AnomData system = ANOMManager.GetSystemAnomData(name);
            MainAnomGrid.DataContext = system;
            AnomSigList.ItemsSource = system.Anoms.Values;
        }

        private void AddHighlightToSystem(string name)
        {
            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;
            if (!rd.MapSystems.Keys.Contains(name))
            {
                return;
            }

            EVEData.MapSystem selectedSys = rd.MapSystems[name];
            if (selectedSys != null)
            {
                double circleSize = 30;
                double circleOffset = circleSize / 2;

                // add circle for system
                Shape highlightSystemCircle = new Ellipse() { Height = circleSize, Width = circleSize };
                highlightSystemCircle.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.SelectedSystemColour);

                highlightSystemCircle.StrokeThickness = 3;

                RotateTransform rt = new RotateTransform();
                rt.CenterX = circleSize / 2;
                rt.CenterY = circleSize / 2;
                highlightSystemCircle.RenderTransform = rt;

                DoubleCollection dashes = new DoubleCollection();
                dashes.Add(1.0);
                dashes.Add(1.0);

                highlightSystemCircle.StrokeDashArray = dashes;

                Canvas.SetLeft(highlightSystemCircle, selectedSys.LayoutX - circleOffset);
                Canvas.SetTop(highlightSystemCircle, selectedSys.LayoutY - circleOffset);
                Canvas.SetZIndex(highlightSystemCircle, 19);

                MainCanvas.Children.Add(highlightSystemCircle);

                DoubleAnimation da = new DoubleAnimation();
                da.From = 0;
                da.To = 360;
                da.Duration = new Duration(TimeSpan.FromSeconds(12));

                RotateTransform eTransform = (RotateTransform)highlightSystemCircle.RenderTransform;
                eTransform.BeginAnimation(RotateTransform.AngleProperty, da);
            }
        }

        private void AddSystemIntelOverlay()
        {
            if (!MapConf.ShowIntel)
            {
                return;
            }

            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            foreach (EVEData.IntelData id in EVEManager.IntelDataList)
            {
                foreach (string sysStr in id.Systems)
                {
                    if (rd.IsSystemOnMap(sysStr))
                    {
                        EVEData.MapSystem sys = rd.MapSystems[sysStr];

                        double radiusScale = (DateTime.Now - id.IntelTime).TotalSeconds / 120.0;

                        if (radiusScale < 0.0 || radiusScale >= 1.0)
                        {
                            continue;
                        }

                        // add circle to the map
                        double radius = 100 * (1.0 - radiusScale);
                        double circleOffset = radius / 2;

                        Shape intelShape = new Ellipse() { Height = radius, Width = radius };

                        intelShape.Fill = new SolidColorBrush(MapConf.ActiveColourScheme.IntelOverlayColour);
                        Canvas.SetLeft(intelShape, sys.LayoutX - circleOffset);
                        Canvas.SetTop(intelShape, sys.LayoutY - circleOffset);
                        Canvas.SetZIndex(intelShape, 15);
                        MainCanvas.Children.Add(intelShape);
                    }
                }
            }
        }

        private void AddCharactersToMap()
        {
            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            foreach (EVEData.Character c in EVEManager.LocalCharacters)
            {
                if(rd.IsSystemOnMap(c.Location))
                {
                    EVEData.MapSystem ms = rd.MapSystems[c.Location];

                    // add the character to
                    double circleSize = 26;
                    double circleOffset = circleSize / 2;

                    // add circle for system
                    Shape highlightSystemCircle = new Ellipse() { Height = circleSize, Width = circleSize };

                    highlightSystemCircle.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterHighlightColour);
                    highlightSystemCircle.StrokeThickness = 2;

                    RotateTransform rt = new RotateTransform();
                    rt.CenterX = circleSize / 2;
                    rt.CenterY = circleSize / 2;
                    highlightSystemCircle.RenderTransform = rt;

                    DoubleCollection dashes = new DoubleCollection();
                    dashes.Add(1.0);
                    dashes.Add(1.0);

                    highlightSystemCircle.StrokeDashArray = dashes;

                    Canvas.SetLeft(highlightSystemCircle, ms.LayoutX - circleOffset);
                    Canvas.SetTop(highlightSystemCircle, ms.LayoutY - circleOffset);
                    Canvas.SetZIndex(highlightSystemCircle, 19);

                    MainCanvas.Children.Add(highlightSystemCircle);

                    // Storyboard s = new Storyboard();
                    DoubleAnimation da = new DoubleAnimation();
                    da.From = 360;
                    da.To = 0;
                    da.Duration = new Duration(TimeSpan.FromSeconds(12));

                    RotateTransform eTransform = (RotateTransform)highlightSystemCircle.RenderTransform;
                    eTransform.BeginAnimation(RotateTransform.AngleProperty, da);

                    double textYOffset = -24;
                    double textXOffset = 6;

                    // also add the name of the character above the system
                    Label charText = new Label();
                    charText.Content = c.Name;
                    charText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterTextColour);

                    if (MapConf.ActiveColourScheme.CharacterTextSize > 0)
                    {
                        charText.FontSize = MapConf.ActiveColourScheme.CharacterTextSize;
                    }

                    Canvas.SetLeft(charText, ms.LayoutX+ textXOffset);
                    Canvas.SetTop(charText, ms.LayoutY + textYOffset);
                    Canvas.SetZIndex(charText, 20);
                    MainCanvas.Children.Add(charText);
                }
            }
        }

        private void AddSystemsToMap()
        {
            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            AddSystemIntelOverlay();

            foreach (EVEData.Link jump in rd.Jumps)
            {
                Line sysLink = new Line();

                EVEData.MapSystem from = rd.MapSystems[jump.From];
                EVEData.MapSystem to = rd.MapSystems[jump.To];
                sysLink.X1 = from.LayoutX;
                sysLink.Y1 = from.LayoutY;

                sysLink.X2 = to.LayoutX;
                sysLink.Y2 = to.LayoutY;

                if (jump.ConstelationLink)
                {
                    sysLink.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.ConstellationGateColour);
                }
                else
                {
                    sysLink.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.NormalGateColour);
                }

                sysLink.StrokeThickness = 1;
                sysLink.Visibility = Visibility.Visible;

                Canvas.SetZIndex(sysLink, 19);
                MainCanvas.Children.Add(sysLink);
            }

            if (MapConf.ShowJumpBridges || MapConf.ShowHostileJumpBridges)
            {
                foreach (EVEData.JumpBridge jb in EVEManager.JumpBridges)
                {
                    if (rd.IsSystemOnMap(jb.From) && rd.IsSystemOnMap(jb.To))
                    {
                        if ((jb.Friendly && !MapConf.ShowJumpBridges) || (!jb.Friendly && !MapConf.ShowHostileJumpBridges))
                        {
                            continue;
                        }

                        // jbLink.Data
                        EVEData.MapSystem from = rd.MapSystems[jb.From];
                        EVEData.MapSystem to = rd.MapSystems[jb.To];

                        Point startPoint = new Point(from.LayoutX, from.LayoutY);
                        Point endPoint = new Point(to.LayoutX, to.LayoutY);

                        Vector dir = Point.Subtract(startPoint, endPoint);

                        double jbDistance = Point.Subtract(startPoint, endPoint).Length;

                        Size arcSize = new Size(jbDistance + 50, jbDistance + 50);

                        ArcSegment arcseg = new ArcSegment(endPoint, arcSize, 100, false, SweepDirection.Clockwise, true);

                        PathSegmentCollection pscollection = new PathSegmentCollection();
                        pscollection.Add(arcseg);

                        PathFigure pf = new PathFigure();
                        pf.Segments = pscollection;
                        pf.StartPoint = startPoint;

                        PathFigureCollection pfcollection = new PathFigureCollection();
                        pfcollection.Add(pf);

                        PathGeometry pathGeometry = new PathGeometry();
                        pathGeometry.Figures = pfcollection;

                        System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
                        path.Data = pathGeometry;

                        if (jb.Friendly)
                        {
                            path.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.FriendlyJumpBridgeColour);
                        }
                        else
                        {
                            path.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.HostileJumpBridgeColour);
                        }

                        path.StrokeThickness = 2;

                        DoubleCollection dashes = new DoubleCollection();
                        dashes.Add(1.0);
                        dashes.Add(1.0);

                        path.StrokeDashArray = dashes;

                        Canvas.SetZIndex(path, 19);

                        MainCanvas.Children.Add(path);
                    }
                }
            }

            foreach (EVEData.MapSystem sys in rd.MapSystems.Values.ToList())
            {
                double circleSize = 15;
                double circleOffset = circleSize / 2;
                double textXOffset = 5;
                double textYOffset = -8;
                double textYOffset2 = 4;


                // add circle for system
                Shape systemShape;

                if (sys.ActualSystem.HasNPCStation)
                {
                    systemShape = new Rectangle() { Height = circleSize, Width = circleSize };
                }
                else
                {
                    systemShape = new Ellipse() { Height = circleSize, Width = circleSize };
                }

                systemShape.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);

                if (sys.OutOfRegion)
                {
                    systemShape.Fill = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemColour);
                }
                else
                {
                    systemShape.Fill = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemColour);
                }

                // override with sec status colours
                if (MapConf.ShowSystemSecurity)
                {
                    systemShape.Fill = new SolidColorBrush(MapColours.GetSecStatusColour(sys.ActualSystem.Security));
                }

                systemShape.DataContext = sys;
                systemShape.MouseDown += ShapeMouseDownHandler;
                systemShape.MouseEnter += ShapeMouseOverHandler;
                systemShape.MouseLeave += ShapeMouseOverHandler;

                Canvas.SetLeft(systemShape, sys.LayoutX - circleOffset);
                Canvas.SetTop(systemShape, sys.LayoutY - circleOffset);
                Canvas.SetZIndex(systemShape, 20);
                MainCanvas.Children.Add(systemShape);

                // add text
                Label sysText = new Label();
                sysText.Content = sys.Name;
                if (MapConf.ActiveColourScheme.SystemTextSize > 0)
                {
                    sysText.FontSize = MapConf.ActiveColourScheme.SystemTextSize;
                }

                if (sys.OutOfRegion)
                {
                    sysText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);
                }
                else
                {
                    sysText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);
                }

                Canvas.SetLeft(sysText, sys.LayoutX + textXOffset);
                Canvas.SetTop(sysText, sys.LayoutY + textYOffset);
                Canvas.SetZIndex(sysText, 20);

                MainCanvas.Children.Add(sysText);

                int nPCKillsLastHour = sys.ActualSystem.NPCKillsLastHour;
                int podKillsLastHour = sys.ActualSystem.PodKillsLastHour;
                int shipKillsLastHour = sys.ActualSystem.ShipKillsLastHour;
                int jumpsLastHour = sys.ActualSystem.JumpsLastHour;

                int infoValue = -1;
                SolidColorBrush infoColour = new SolidColorBrush(MapConf.ActiveColourScheme.ESIOverlayColour);
                double infoSize = 0.0;
                if (MapConf.ShowNPCKills)
                {
                    infoValue = nPCKillsLastHour;
                    infoSize = 0.15f * infoValue * MapConf.ESIOverlayScale;
                }

                if (MapConf.ShowPodKills)
                {
                    infoValue = podKillsLastHour;
                    infoSize = 20.0f * infoValue * MapConf.ESIOverlayScale;
                }

                if (MapConf.ShowShipKills)
                {
                    infoValue = shipKillsLastHour;
                    infoSize = 20.0f * infoValue * MapConf.ESIOverlayScale;
                }

                if (MapConf.ShowShipJumps)
                {
                    infoValue = sys.ActualSystem.JumpsLastHour;
                    infoSize = infoValue * MapConf.ESIOverlayScale;
                }

                if (infoValue != -1)
                {
                    Shape infoCircle = new Ellipse() { Height = infoSize, Width = infoSize };
                    infoCircle.Fill = infoColour;

                    Canvas.SetZIndex(infoCircle, 10);
                    Canvas.SetLeft(infoCircle, sys.LayoutX - (infoSize / 2));
                    Canvas.SetTop(infoCircle, sys.LayoutY - (infoSize / 2));
                    MainCanvas.Children.Add(infoCircle);
                }

                if (sys.OutOfRegion)
                {
                    Label sysRegionText = new Label();
                    sysRegionText.Content = "(" + sys.Region + ")";
                    sysRegionText.FontSize = 7;
                    sysRegionText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);

                    Canvas.SetLeft(sysRegionText, sys.LayoutX + textXOffset);
                    Canvas.SetTop(sysRegionText, sys.LayoutY + textYOffset2);
                    Canvas.SetZIndex(sysRegionText, 20);

                    MainCanvas.Children.Add(sysRegionText);
                }
            }

            AddCharactersToMap();
        }

        private void OnRegionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // clear the character selection
            // CharacterDropDown.SelectedItem = null;
            FollowCharacter = false;

            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            ReDrawMap();
            MapConf.DefaultRegion = rd.Name;

            MainContent.DataContext = RegionDropDown.SelectedItem;
            MainAnomGrid.DataContext = null;
            AnomSigList.ItemsSource = null;

            SystemDropDownAC.SelectedItem = null;

            List<EVEData.MapSystem> newList = rd.MapSystems.Values.ToList().OrderBy(o => o.Name).ToList();
            SystemDropDownAC.ItemsSource = newList;
        }

        private void SelectRegion(string regionName)
        {
            foreach (EVEData.MapRegion rd in EVEManager.Regions)
            {
                if (rd.Name == regionName)
                {
                    RegionDropDown.SelectedItem = rd;
                    List<EVEData.MapSystem> newList = rd.MapSystems.Values.ToList().OrderBy(o => o.Name).ToList();
                    SystemDropDownAC.ItemsSource = newList;
                }
            }
        }

        private void refreshData_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.StartUpdateKillsFromESI();
            EVEManager.StartUpdateJumpsFromESI();
        }

        private void SystemDropDownAC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EVEData.MapSystem sd = SystemDropDownAC.SelectedItem as EVEData.MapSystem;

            if (sd != null)
            {
                SelectSystem(sd.Name);
                ReDrawMap();
            }
        }

        private void SysContexMenuItemAddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            EVEData.Character c = CharacterDropDown.SelectedItem as EVEData.Character;
            if(c != null)
            {
                c.AddDestination(eveSys.ActualSystem.ID, false);
            }

        }

        private void SysContexMenuItemSetDestination_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            EVEData.Character c = CharacterDropDown.SelectedItem as EVEData.Character;
            if (c != null)
            {
                c.AddDestination(eveSys.ActualSystem.ID, true);
            }

        }



        private void SysContexMenuItemDotlan_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            EVEData.MapRegion rd = EVEManager.GetRegion(eveSys.Region);

            string uRL = string.Format("http://evemaps.dotlan.net/map/{0}/{1}", rd.DotLanRef, eveSys.Name);
            System.Diagnostics.Process.Start(uRL);
        }

        private void SysContexMenuItemZKB_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            EVEData.MapRegion rd = EVEManager.GetRegion(eveSys.Region);

            string uRL = string.Format("https://zkillboard.com/system/{0}", eveSys.ActualSystem.ID);
            System.Diagnostics.Process.Start(uRL);
        }

        private void HandleCharacterSelectionChange()
        {
            EVEData.Character c = CharacterDropDown.SelectedItem as EVEData.Character;
            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            if (c != null && FollowCharacter)
            {
                EVEData.System s = EVEManager.GetEveSystem(c.Location);
                if (s != null)
                {
                    if (s.Region != rd.Name)
                    {
                        // change region
                        SelectRegion(s.Region);

                    }

                    SelectSystem(c.Location);

                    CharacterDropDown.SelectedItem = c;

                    // force the follow as this will be reset by the region change
                    FollowCharacter = true;

                }
            }
        }

        private void CharacterDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleCharacterSelectionChange();
        }

        private void CharacterDropDown_DropDownClosed(object sender, EventArgs e)
        {
            HandleCharacterSelectionChange();
        }

        private void ResetColourData_Click(object sender, RoutedEventArgs e)
        {
            MapConf.MapColours = new List<MapColours>();
            MapConf.SetDefaultColours();
            ColourListDropdown.ItemsSource = MapConf.MapColours;
            ColourListDropdown.SelectedItem = MapConf.MapColours[0];

            ReDrawMap();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            About popup = new About();
            popup.ShowDialog();
        }

        private void ColourListDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MapColours newSelection = ColourListDropdown.SelectedItem as MapColours;
            if (newSelection == null)
            {
                return;
            }

            MapConf.ActiveColourScheme = newSelection;
            ColoursPropertyGrid.SelectedObject = newSelection;
            ColoursPropertyGrid.Update();

            MapConf.DefaultColourSchemeName = newSelection.Name;
        }

        private void btnUpdateAnomList_Click(object sender, RoutedEventArgs e)
        {
            string pasteData = Clipboard.GetText();

            if (pasteData != null || pasteData != string.Empty)
            {
                EVEData.AnomData ad = MainAnomGrid.DataContext as EVEData.AnomData;

                if (ad != null)
                {
                    ad.UpdateFromPaste(pasteData);
                    AnomSigList.Items.Refresh();
                    AnomSigList.UpdateLayout();
                    CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
                }
            }
        }

        private void btnClearAnomList_Click(object sender, RoutedEventArgs e)
        {
            EVEData.AnomData ad = MainAnomGrid.DataContext as EVEData.AnomData;
            if (ad != null)
            {
                ad.Anoms.Clear();
                AnomSigList.Items.Refresh();
                AnomSigList.UpdateLayout();
                CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
            }
        }

        private void btn_AddCharacter_Click(object sender, RoutedEventArgs e)
        {
            string eSILogonURL = EVEManager.GetESILogonURL();

            LogonWindow logonBrowserWindow = new LogonWindow();
            logonBrowserWindow.logonBrowser.Navigate(eSILogonURL);

            logonBrowserWindow.URLName.Text = eSILogonURL;
            logonBrowserWindow.ShowDialog();
        }

        private void FollowCharacterChk_Checked(object sender, RoutedEventArgs e)
        {
            HandleCharacterSelectionChange();
        }
    }
}