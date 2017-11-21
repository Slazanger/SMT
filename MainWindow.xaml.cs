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


        private static NLog.Logger OutputLog = NLog.LogManager.GetCurrentClassLogger();


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

        private List<System.Windows.UIElement> DynamicMapElements;

        private MapConfig MapConf;

        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        public MainWindow()
        {
            OutputLog.Info("Starting App..");

            InitializeComponent();

            DynamicMapElements = new List<UIElement>();



            // load any custom map settings off disk
            string mapConfigFileName = AppDomain.CurrentDomain.BaseDirectory + @"\MapConfig.dat";
            OutputLog.Info("Loading Map config from {0}", mapConfigFileName);


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


            SelectedSystem = string.Empty;

            EVEManager = new EVEData.EveManager();
            EVEData.EveManager.SetInstance(EVEManager);

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


            RegionDropDown.ItemsSource = EVEManager.Regions;

            EVEManager.SetupIntelWatcher();
            RawIntelBox.ItemsSource = EVEManager.IntelDataList;

            
            // load jump bridge data
            EVEManager.LoadJumpBridgeData();
            EVEManager.StartUpdateKillsFromESI();
            EVEManager.StartUpdateJumpsFromESI();
            EVEManager.StartUpdateSOVFromESI();

            foreach (EVEData.MapRegion rd in EVEManager.Regions)
            {
                if (rd.Name == MapConf.DefaultRegion)
                {
                    RegionDropDown.SelectedItem = rd;
                    List<EVEData.MapSystem> newList = rd.MapSystems.Values.ToList().OrderBy(o => o.Name).ToList();
                    SystemDropDownAC.ItemsSource = newList;
                    MapDocument.Title = rd.Name;
                }
            }


            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();

            CharacterDropDown.ItemsSource = EVEManager.LocalCharacters;

            MapControlsPropertyGrid.SelectedObject = MapConf;

            ColourListDropdown.ItemsSource = MapConf.MapColours;

            CharacterList.ItemsSource = EVEManager.LocalCharacters;


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

            EVEManager.IntelAddedEvent += OnIntelAdded;


            ToolBoxCanvas.DataContext = MapConf;

            MapConf.PropertyChanged += MapObjectChanged;
            

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
            EVEManager.SaveData();
        }


        private void ColoursPropertyGrid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReDrawMap();
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            ReDrawMap(false);
        }

        private void MapObjectChanged(object sender, PropertyChangedEventArgs e)
        {
            ReDrawMap();
        }


        private void ReDrawMap(bool fullRedraw = true)
        {
            if (CharacterDropDown.SelectedItem != null && FollowCharacter == true)
            {
                HandleCharacterSelectionChange();
            }

            MainCanvasGrid.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);
            MainCanvas.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);
            MainZoomControl.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);


            if (fullRedraw)
            {
                MainCanvas.Children.Clear();
                AddSystemsToMap();
            }
            else
            {
                foreach(UIElement uie in DynamicMapElements)
                {
                    MainCanvas.Children.Remove(uie);
                }
                DynamicMapElements.Clear();
            }

            AddDataToMap();
            AddHighlightToSystem(SelectedSystem);
            AddSystemIntelOverlay();
            AddCharactersToMap();

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
                    bool redraw = false;
                    if(MapConf.ShowJumpDistance)
                    {
                        redraw = true;
                    }
                    SelectSystem(selectedSys.Name);

                    ReDrawMap(redraw);

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
                DynamicMapElements.Add(highlightSystemCircle);

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

                        double radiusScale = (DateTime.Now - id.IntelTime).TotalSeconds / (double)MapConf.MaxIntelSeconds;

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

                        DynamicMapElements.Add(intelShape);
                    }
                }
            }
        }


        private struct GateHelper
        {
            public EVEData.MapSystem from { get; set; }
            public EVEData.MapSystem to { get; set; }
        }



        private void AddSystemsToMap()
        {
            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

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

                        // animate the jump bridges
                        DoubleAnimation da = new DoubleAnimation();
                        da.From = 0;
                        da.To = 200;
                        da.By = 2;
                        da.Duration = new Duration(TimeSpan.FromSeconds(40));
                        da.RepeatBehavior = RepeatBehavior.Forever;

                        path.StrokeDashArray = dashes;
                        path.BeginAnimation(Shape.StrokeDashOffsetProperty, da);



                        Canvas.SetZIndex(path, 19);

                        MainCanvas.Children.Add(path);
                    }
                }
            }

            List<GateHelper> sysLinks = new List<GateHelper>();

            Brush SysOutline = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);
            Brush SysInRegion = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemColour);
            Brush SysOutRegion = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemColour);
            Brush SysInRegionText = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);
            Brush SysOutRegionText = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);

            Brush NormalGate = new SolidColorBrush(MapConf.ActiveColourScheme.NormalGateColour);
            Brush ConstellationGate = new SolidColorBrush(MapConf.ActiveColourScheme.ConstellationGateColour);

            Brush JumpInRange = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeInColour);
            Brush JumpOutRange = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeOutColour);



            foreach (EVEData.MapSystem sys in rd.MapSystems.Values.ToList())
            {
                double circleSize = 15;
                double circleOffset = circleSize / 2;
                double textXOffset = 5;
                double textYOffset = -8;
                double textYOffset2 = 6;


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


                systemShape.Stroke = SysOutline;


                if (sys.OutOfRegion)
                {
                    systemShape.Fill = SysOutRegion;
                }
                else
                {
                    systemShape.Fill = SysInRegion;
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
                    sysText.Foreground = SysOutRegionText;
                }
                else
                {
                    sysText.Foreground = SysInRegionText;
                }

                Canvas.SetLeft(sysText, sys.LayoutX + textXOffset);
                Canvas.SetTop(sysText, sys.LayoutY + textYOffset);
                Canvas.SetZIndex(sysText, 20);

                MainCanvas.Children.Add(sysText);


                // now add any jumps (todo : this will duplicate, eg, D-P will link to E1 and E1 will link to D-P



                foreach (string jumpTo in sys.ActualSystem.Jumps)
                {
                    if(rd.IsSystemOnMap(jumpTo))
                    {

                        EVEData.MapSystem to = rd.MapSystems[jumpTo];

                        bool NeedsAdd = true;
                        foreach(GateHelper gh in sysLinks)
                        {
                           if( ((gh.from == sys) || (gh.to == sys)) &&  ( (gh.from == to) || (gh.to == to) ))
                            {
                                NeedsAdd = false;
                                break;
                            } 
                        }

                        if(NeedsAdd)
                        {
                            GateHelper g = new GateHelper();
                            g.from = sys;
                            g.to = to;
                            sysLinks.Add(g);
                        }
                    }
                }



                double regionMarkerOffset = textYOffset2;

                if ( ( MapConf.ShowSystemSovName | MapConf.ShowSystemSovTicker) && sys.ActualSystem.SOVAlliance != null && EVEManager.AllianceIDToName.Keys.Contains(sys.ActualSystem.SOVAlliance))
                {
                    Label sysRegionText = new Label();

                    string content = "";
                    string allianceName = EVEManager.GetAllianceName(sys.ActualSystem.SOVAlliance);
                    string allianceTicker = EVEManager.GetAllianceTicker(sys.ActualSystem.SOVAlliance);

                    if (MapConf.ShowSystemSovName)
                    {
                        content = allianceName;
                    }

                    if (MapConf.ShowSystemSovTicker)
                    {
                        content = allianceTicker;

                    }

                    if (MapConf.ShowSystemSovTicker && MapConf.ShowSystemSovName && allianceName != string.Empty && allianceTicker != String.Empty )
                    {
                        content = allianceName + " (" + allianceTicker + ")";
                    }



                    sysRegionText.Content = content;
                    sysRegionText.FontSize = 7;
                    sysText.FontSize = MapConf.ActiveColourScheme.SystemTextSize;


                    Canvas.SetLeft(sysRegionText, sys.LayoutX + textXOffset);
                    Canvas.SetTop(sysRegionText, sys.LayoutY + textYOffset2);
                    Canvas.SetZIndex(sysRegionText, 20);

                    MainCanvas.Children.Add(sysRegionText);

                    regionMarkerOffset += 8;
                }

                

                if( MapConf.ShowJumpDistance && SelectedSystem != null && sys.Name != SelectedSystem)
                {

                    double Distance = EVEManager.GetRange(SelectedSystem, sys.Name);
                    Distance = Distance / 9460730472580800.0;

                    double Max = 0.1f;

                    switch (MapConf.JumpShipType)
                    {
                        case MapConfig.JumpShip.Super: { Max = 6.0; } break;
                        case MapConfig.JumpShip.Titan: { Max = 6.0; } break;

                        case MapConfig.JumpShip.Dread: { Max = 7.0; } break;
                        case MapConfig.JumpShip.Carrier: { Max = 7.0; } break;
                        case MapConfig.JumpShip.FAX: { Max = 7.0; } break;
                        case MapConfig.JumpShip.Blops: { Max = 8.0; } break;
                        case MapConfig.JumpShip.JF: { Max = 10.0; } break;
                    }

                    if (Distance < Max && Distance > 0.0)
                    {
                        systemShape.Fill = JumpInRange;

                        string JD = Distance.ToString("0.00") + " LY";

                        Label DistanceText = new Label();

                        DistanceText.Content = JD;
                        DistanceText.FontSize = 9;
                        DistanceText.Foreground = SysOutRegionText;
                        regionMarkerOffset += 8;

                        Canvas.SetLeft(DistanceText, sys.LayoutX + textXOffset);
                        Canvas.SetTop(DistanceText, sys.LayoutY + textYOffset2);


                        Canvas.SetZIndex(DistanceText, 20);
                        MainCanvas.Children.Add(DistanceText);

                    }
                    else
                    {
                        systemShape.Fill = JumpOutRange;
                    }
                }

                


                if (sys.OutOfRegion)
                {
                    Label sysRegionText = new Label();
                    sysRegionText.Content = "(" + sys.Region + ")";
                    sysRegionText.FontSize = 7;
                    sysRegionText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);

                    Canvas.SetLeft(sysRegionText, sys.LayoutX + textXOffset);
                    Canvas.SetTop(sysRegionText, sys.LayoutY + regionMarkerOffset);
                    Canvas.SetZIndex(sysRegionText, 20);

                    MainCanvas.Children.Add(sysRegionText);

                }




            }

            foreach(GateHelper gh in sysLinks)
            {
                Line sysLink = new Line();


                sysLink.X1 = gh.from.LayoutX;
                sysLink.Y1 = gh.from.LayoutY;

                sysLink.X2 = gh.to.LayoutX;
                sysLink.Y2 = gh.to.LayoutY;

                if (gh.from.ActualSystem.Region != gh.to.ActualSystem.Region || gh.from.ActualSystem.ConstellationID != gh.to.ActualSystem.ConstellationID)
                {
                    sysLink.Stroke = ConstellationGate;
                }
                else
                {
                    sysLink.Stroke = NormalGate;
                }

                sysLink.StrokeThickness = 1;
                sysLink.Visibility = Visibility.Visible;

                Canvas.SetZIndex(sysLink, 19);
                MainCanvas.Children.Add(sysLink);
            }

        }


        private void AddDataToMap()
        {
            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            Dictionary<string, List<Polygon>> mergedOverlay = new Dictionary<string, List<Polygon>>();

            foreach (EVEData.MapSystem sys in rd.MapSystems.Values.ToList())
            {
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
                    DynamicMapElements.Add(infoCircle);
                }



                if (MapConf.ColourBySov && sys.ActualSystem.SOVAlliance != null)
                {
                    Polygon poly = new Polygon();

                    foreach (Point p in sys.CellPoints)
                    {
                        poly.Points.Add(p);
                    }

                    Color c = stringToColour(sys.ActualSystem.SOVAlliance);
                    //c.A = 75;
                    poly.Fill = new SolidColorBrush(c);
                    poly.SnapsToDevicePixels = true;
                    poly.Stroke = poly.Fill;
                    poly.StrokeThickness = 0.5;
                    poly.StrokeDashCap = PenLineCap.Round;
                    poly.StrokeLineJoin = PenLineJoin.Round;


                    //MainCanvas.Children.Add(poly);

                    // save the dynamic map elements
                    //DynamicMapElements.Add(poly);

                    if (!mergedOverlay.Keys.Contains(sys.ActualSystem.SOVAlliance))
                    {
                        mergedOverlay[sys.ActualSystem.SOVAlliance] = new List<Polygon>();
                    }

                    mergedOverlay[sys.ActualSystem.SOVAlliance].Add(poly);

                }

            }


            if(MapConf.ColourBySov)
            {
                List<Shape> mergedGeom = new List<Shape>();
                foreach (List<Polygon> pl in mergedOverlay.Values)
                {
                    CombinedGeometry c = new CombinedGeometry();
                    // quick and dirty recursive geometry combine

                    if (pl.Count >= 2)
                    {
                        Rect r = new Rect(MainCanvas.RenderSize);

                        foreach (Polygon pg in pl)
                        {
                            pg.Measure(MainCanvas.RenderSize);
                            pg.Arrange(r);


                            if (c.Geometry1 == null)
                            {
                                c.Geometry1 = pg.RenderedGeometry;

                                continue;
                            }

                            c.Geometry2 = pg.RenderedGeometry;
                            c.GeometryCombineMode = GeometryCombineMode.Union;

                            CombinedGeometry temp = c;
                            c = new CombinedGeometry();
                            c.Geometry1 = temp;
                        }

                        System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
                        p.Data = c;
                        p.Stroke = System.Windows.Media.Brushes.Black;
                        p.StrokeThickness = 1;
                        p.Fill = pl[0].Fill;
                        p.SnapsToDevicePixels = true;
                        mergedGeom.Add(p);
                      
                    }

                    else
                    {
                        pl[0].Stroke = System.Windows.Media.Brushes.Black;
                        pl[0].StrokeThickness = 1;

                        mergedGeom.Add(pl[0]);
                    }
                }


                foreach(Shape poly in mergedGeom)
                {
                    MainCanvas.Children.Add(poly);

                    // save the dynamic map elements
                    DynamicMapElements.Add(poly);
                }

            }
        }


        private void AddCharactersToMap()
        {
            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            foreach (EVEData.Character c in EVEManager.LocalCharacters)
            {
                if (rd.IsSystemOnMap(c.Location))
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
                    DynamicMapElements.Add(highlightSystemCircle);

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

                    Canvas.SetLeft(charText, ms.LayoutX + textXOffset);
                    Canvas.SetTop(charText, ms.LayoutY + textYOffset);
                    Canvas.SetZIndex(charText, 20);
                    MainCanvas.Children.Add(charText);
                    DynamicMapElements.Add(charText);


                }
            }
        }



        private void OnIntelAdded()
        {
            if(MapConf.PlayIntelSound)
            {
                Uri uri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                var player = new MediaPlayer();
                player.Open(uri);
                player.Play();
            }
        }



        private void OnRegionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // clear the character selection
            // CharacterDropDown.SelectedItem = null;
            FollowCharacter = false;

            EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;

            EVEManager.UpdateIDsForMapRegion(rd.Name);

            ReDrawMap();
            MapConf.DefaultRegion = rd.Name;

            MainContent.DataContext = RegionDropDown.SelectedItem;
            MainAnomGrid.DataContext = null;
            AnomSigList.ItemsSource = null;

            SystemDropDownAC.SelectedItem = null;

            List<EVEData.MapSystem> newList = rd.MapSystems.Values.ToList().OrderBy(o => o.Name).ToList();
            SystemDropDownAC.ItemsSource = newList;
            MapDocument.Title = rd.Name;

            // reset any custom zoom as we're changing region
            MainZoomControl.Zoom = 1.0f;
            MainZoomControl.Mode = WpfHelpers.WpfControls.Zoombox.ZoomControlModes.Fill;
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

                    MapDocument.Title = regionName;

                    EVEManager.UpdateIDsForMapRegion(rd.Name);
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
                ReDrawMap(false);
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

        public Color stringToColour(string str)
        {
            int hash = 0;


            foreach (char c in str.ToCharArray())
            {
                hash = c + ((hash << 5) - hash);
            }

            double R = (((byte) (hash & 0xff) / 255.0) * 80.0 ) + 127.0 ;
            double G = (((byte) ((hash >> 8) & 0xff) / 255.0 ) * 80.0 ) + 127.0;
            double B = (((byte) ((hash >> 16) & 0xff) / 255.0)* 80.0) + 127.0;

            

            return Color.FromRgb((byte)R, (byte)G, (byte)B);

        }

        private void RawIntelBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(RawIntelBox.SelectedItem == null)
            {
                return;
            }
           
            EVEData.IntelData intel = RawIntelBox.SelectedItem as EVEData.IntelData;

            foreach(string s in intel.IntelString.Split(' '))
            {
                if(s=="")
                {
                    continue;
                }
                if (EVEManager.Systems.Any(x => (x.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)))
                {
                    EVEData.System sys = EVEManager.GetEveSystem(s);
                    if (sys == null)
                    {
                        return;
                    }

                    EVEData.MapRegion rd = RegionDropDown.SelectedItem as EVEData.MapRegion;
                    if(rd.Name != sys.Region)
                    {
                        SelectRegion(sys.Region);
                    }


                    SelectSystem(s);
                    return;
                }
            }

        }
    }





}