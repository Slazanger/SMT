using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Windows.Data;

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
        public EVEData.EveManager EVEManager;


        public EVEData.AnomManager ANOMManager;

        /// <summary>
        /// List of all systems
        /// </summary>
        public List<string> AllSystems;

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


        public string SelectedSystem { get; set; }


        MapConfig MapConf;

        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        public MainWindow()
        {
            InitializeComponent();


            // load any custom map settings off disk
            string MapConfigFileName = AppDomain.CurrentDomain.BaseDirectory + @"\MapConfig.dat";
            if(File.Exists(MapConfigFileName))
            {
                try
                {
                    XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
                    FileStream fs = new FileStream(MapConfigFileName, FileMode.Open);
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

            SelectedSystem = "";


            // if we want to re-build the data as we've changed the format, recreate it all from DOTLAN
            bool initFromDotlan = false;
            if(initFromDotlan)
            {
                EVEManager = new EVEData.EveManager();
                EVEManager.InitFromDotLAN();
            }
            else
            {
                XmlSerializer xms = new XmlSerializer(typeof(EVEData.EveManager));
                string dataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\RegionInfo.dat";

                FileStream fs = new FileStream(dataFilename, FileMode.Open);
                XmlReader xmlr = XmlReader.Create(fs);

                EVEManager = (EVEData.EveManager)xms.Deserialize(xmlr);
            }

            RegionDropDown.ItemsSource = EVEManager.Regions;


            EVEManager.SetupIntelWatcher();
            RawIntelBox.ItemsSource = EVEManager.IntelDataList;

            // load jump bridge data
            EVEManager.LoadJumpBridgeData();
            EVEManager.StartUpdateKillsFromESI();
            EVEManager.StartUpdateJumpsFromESI();

            AllSystems = new List<string>();
            foreach (EVEData.RegionData rd in EVEManager.Regions)
            {
                if (rd.Name == MapConf.DefaultRegion )
                {
                    RegionDropDown.SelectedItem = rd;
                    List<EVEData.System> newList = rd.Systems.Values.ToList().OrderBy(o => o.Name).ToList(); ;
                    SystemDropDownAC.ItemsSource = newList;
                }

                AllSystems.AddRange(rd.Systems.Keys.ToList());

            }

            AllSystems.Sort();

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();

            CharacterDropDown.ItemsSource = EVEManager.LocalCharacters;

            MapControlsPropertyGrid.SelectedObject = MapConf;
            MapControlsPropertyGrid.PropertyValueChanged += MapControlsPropertyGrid_PropertyValueChanged;

            ColourListDropdown.ItemsSource = MapConf.MapColours;

            MapColours selectedColours  = MapConf.MapColours[0];
            // find the matching active colour scheme
            foreach( MapColours mc in MapConf.MapColours)
            {
                if(MapConf.DefaultColourSchemeName == mc.Name)
                {
                    selectedColours = mc;
                }
            }


            // load the dockmanager layout
            string DockManagerLayoutName = AppDomain.CurrentDomain.BaseDirectory + @"\Layout.dat";
            if (File.Exists(DockManagerLayoutName))
            {
                try
                {
                    Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                    using (var sr = new StreamReader(DockManagerLayoutName))
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
            if(File.Exists(anomDataFilename))
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
            string DockManagerLayoutName = AppDomain.CurrentDomain.BaseDirectory + @"\Layout.dat";
            try
            {
                Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                using (var sw = new StreamWriter(DockManagerLayoutName))
                {
                    ls.Serialize(sw);
                }
            }
            catch
            {
            }


            // Save the Map Colours
            string MapConfigFileName = AppDomain.CurrentDomain.BaseDirectory + @"\MapConfig.dat";

            // now serialise the class to disk
            XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
            using (TextWriter tw = new StreamWriter(MapConfigFileName))
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
        }

        ~MainWindow()
        {

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



        void ReDrawMap()
        {
            if(CharacterDropDown.SelectedItem != null)
            {
                HandleCharacterSelectionChange();
            }


            MainCanvasGrid.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);
            MainCanvas.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);

            MainCanvas.Children.Clear();
            AddSystemsToMap();
            AddHighlightToSystem(SelectedSystem);
        }


        void ShapeMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            Shape obj = sender as Shape;
            EVEData.RegionData currentRegion = RegionDropDown.SelectedItem as EVEData.RegionData;

            EVEData.System selectedSys = obj.DataContext as EVEData.System;

            if(e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    EVEData.Character c = CharacterDropDown.SelectedItem as EVEData.Character;
                    if(c != null)
                    {
                        if(selectedSys.Name != c.Location )
                        {
                            CharacterDropDown.SelectedItem = null;
                        }
                    }

                    ReDrawMap();
                    SelectSystem(selectedSys.Name);
                }

                if (e.ClickCount == 2 && selectedSys.Region != currentRegion.Name)
                {
                    foreach (EVEData.RegionData rd in EVEManager.Regions)
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
                cm.IsOpen = true;
            }
        }

        void ShapeMouseOverHandler(object sender, MouseEventArgs e)
        {
            Shape obj = sender as Shape;
            EVEData.RegionData currentRegion = RegionDropDown.SelectedItem as EVEData.RegionData;

            EVEData.System selectedSys = obj.DataContext as EVEData.System;
            if(obj.IsMouseOver && MapConf.ShowSystemPopup)
            {
                SystemInfoPopup.PlacementTarget = obj;
                SystemInfoPopup.VerticalOffset = 5;
                SystemInfoPopup.HorizontalOffset = 15;
                SystemInfoPopup.DataContext = selectedSys;

                SystemInfoPopup.IsOpen = true;
            }
            else
            {
                SystemInfoPopup.IsOpen = false;

            }
        }



        private void SelectSystem(string name)
        {
            EVEData.RegionData rd = RegionDropDown.SelectedItem as EVEData.RegionData;

            
            foreach (EVEData.System es in rd.Systems.Values.ToList() )
            {
                if(es.Name == name)
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
            EVEData.RegionData rd = RegionDropDown.SelectedItem as EVEData.RegionData;
            if(!rd.Systems.Keys.Contains(name))
            {
                return;
            }


            EVEData.System selectedSys = rd.Systems[name];
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

                Canvas.SetLeft(highlightSystemCircle, selectedSys.DotlanX - circleOffset);
                Canvas.SetTop(highlightSystemCircle, selectedSys.DotLanY - circleOffset);
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
            if(!MapConf.ShowIntel)
            {
                return;
            }

            EVEData.RegionData rd = RegionDropDown.SelectedItem as EVEData.RegionData;

            foreach (EVEData.IntelData id in EVEManager.IntelDataList)
            {
                foreach(string sysStr in id.Systems)
                {
                    if (rd.IsSystemOnMap(sysStr))
                    {
                        EVEData.System sys = rd.Systems[sysStr];

                        double radiusScale = (DateTime.Now - id.IntelTime).TotalSeconds / 120.0;

                        if(radiusScale < 0.0 || radiusScale >=1.0 )
                        {
                            continue;
                        }

                        // add circle to the map
                        double radius = 100 * (1.0 - radiusScale);
                        double circleOffset = radius / 2;

                        Shape intelShape = new Ellipse() { Height = radius, Width = radius };

                        intelShape.Fill = new SolidColorBrush(MapConf.ActiveColourScheme.IntelOverlayColour);
                        Canvas.SetLeft(intelShape, sys.DotlanX - circleOffset);
                        Canvas.SetTop(intelShape, sys.DotLanY - circleOffset);
                        Canvas.SetZIndex(intelShape, 15);
                        MainCanvas.Children.Add(intelShape);
                    }
                }
            }
        }

        private void AddCharactersToMap()
        {
            EVEData.RegionData rd = RegionDropDown.SelectedItem as EVEData.RegionData;

            foreach(EVEData.Character c in EVEManager.LocalCharacters)
            {
                EVEData.System s = EVEManager.GetEveSystem(c.Location);
                if (s != null && EVEManager.GetRegion(s.Region) == rd )
                {
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

                    Canvas.SetLeft(highlightSystemCircle, s.DotlanX - circleOffset);
                    Canvas.SetTop(highlightSystemCircle, s.DotLanY - circleOffset);
                    Canvas.SetZIndex(highlightSystemCircle, 19);

                    MainCanvas.Children.Add(highlightSystemCircle);

                    //               Storyboard s = new Storyboard();
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
                    Canvas.SetLeft(charText, s.DotlanX + textXOffset);
                    Canvas.SetTop(charText, s.DotLanY + textYOffset);
                    Canvas.SetZIndex(charText, 20);
                    MainCanvas.Children.Add(charText);
                }
            }
        }


        private void AddSystemsToMap()
        {
            EVEData.RegionData rd = RegionDropDown.SelectedItem as EVEData.RegionData;

            AddSystemIntelOverlay();


            foreach (EVEData.Link jump in rd.Jumps)
            {
                Line sysLink = new Line();

                EVEData.System from = rd.Systems[jump.From];
                EVEData.System to = rd.Systems[jump.To];
                sysLink.X1 = from.DotlanX;
                sysLink.Y1 = from.DotLanY;

                sysLink.X2 = to.DotlanX;
                sysLink.Y2 = to.DotLanY;

                if(jump.ConstelationLink)
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

            if(MapConf.ShowJumpBridges || MapConf.ShowHostileJumpBridges)
            {
                foreach (EVEData.JumpBridge jb in EVEManager.JumpBridges)
                {
                    if (rd.IsSystemOnMap(jb.From) && rd.IsSystemOnMap(jb.To))
                    {
                        if((jb.Friendly && !MapConf.ShowJumpBridges) || (!jb.Friendly && !MapConf.ShowHostileJumpBridges))
                        {
                            continue;
                        }


                        // jbLink.Data
                        EVEData.System from = rd.Systems[jb.From];
                        EVEData.System to = rd.Systems[jb.To];




                        Point StartPoint = new Point(from.DotlanX, from.DotLanY);
                        Point EndPoint = new Point(to.DotlanX, to.DotLanY);

                        Vector Dir = Point.Subtract(StartPoint, EndPoint);


                        double jbDistance = Point.Subtract(StartPoint, EndPoint).Length;

                        Size arcSize = new Size(jbDistance + 50, jbDistance + 50);


                        ArcSegment arcseg = new ArcSegment(EndPoint, arcSize, 100, false, SweepDirection.Clockwise, true);

                        PathSegmentCollection pscollection = new PathSegmentCollection();
                        pscollection.Add(arcseg);


                        PathFigure pf = new PathFigure();
                        pf.Segments = pscollection;
                        pf.StartPoint = StartPoint;



                        PathFigureCollection pfcollection = new PathFigureCollection();
                        pfcollection.Add(pf);

                        PathGeometry pathGeometry = new PathGeometry();
                        pathGeometry.Figures = pfcollection;

                        System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
                        path.Data = pathGeometry;

                        if(jb.Friendly)
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

            foreach (EVEData.System sys in rd.Systems.Values.ToList())
            {
                double circleSize = 15;
                double circleOffset = circleSize / 2;
                double textXOffset = 5;
                double textYOffset = -8;
                double textYOffset2 = 4;

                bool OutofRegion = sys.Region != rd.Name;

                // add circle for system

                Shape systemShape;

                if (sys.HasNPCStation)
                {
                    systemShape = new Rectangle() { Height = circleSize, Width = circleSize };
                }
                else
                {
                    systemShape = new Ellipse() { Height = circleSize, Width = circleSize };

                }

                systemShape.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);


                if (OutofRegion)
                {
                    
                    systemShape.Fill = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemColour);
                }
                else
                {
                    systemShape.Fill = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemColour);
                }

                systemShape.DataContext = sys;
                systemShape.MouseDown += ShapeMouseDownHandler;
                systemShape.MouseEnter += ShapeMouseOverHandler;
                systemShape.MouseLeave += ShapeMouseOverHandler;



                Canvas.SetLeft(systemShape, sys.DotlanX - circleOffset);
                Canvas.SetTop(systemShape, sys.DotLanY - circleOffset);
                Canvas.SetZIndex(systemShape, 20);
                MainCanvas.Children.Add(systemShape);

                // add text

                Label sysText = new Label();
                sysText.Content = sys.Name;
                if (MapConf.ActiveColourScheme.SystemTextSize > 0)
                {
                    sysText.FontSize = MapConf.ActiveColourScheme.SystemTextSize;
                }

                if (OutofRegion)
                {
                    sysText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);
                }
                else
                {
                    sysText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);
                }

                Canvas.SetLeft(sysText, sys.DotlanX + textXOffset);
                Canvas.SetTop(sysText, sys.DotLanY + textYOffset);
                Canvas.SetZIndex(sysText, 20);
                
                MainCanvas.Children.Add(sysText);

                if (!OutofRegion)
                {
                    bool renderInfoText = false;

                    if(renderInfoText)
                    {
                        double dataTextOffset = 12;
                        double dataTextOffsetIncrement = 8;
                        if (sys.ShipKillsLastHour > 0)
                        {
                            Label shipText = new Label();
                            shipText.FontSize = 6;
                            shipText.Content = "Ship Kills :" + sys.ShipKillsLastHour;
                            Canvas.SetLeft(shipText, sys.DotlanX + textXOffset);
                            Canvas.SetTop(shipText, sys.DotLanY + textYOffset + dataTextOffset);
                            dataTextOffset += dataTextOffsetIncrement;
                            MainCanvas.Children.Add(shipText);
                        }


                        if (sys.PodKillsLastHour > 0)
                        {
                            Label podText = new Label();
                            podText.FontSize = 6;
                            podText.Content = "POD Kills :" + sys.PodKillsLastHour;
                            Canvas.SetLeft(podText, sys.DotlanX + textXOffset);
                            Canvas.SetTop(podText, sys.DotLanY + textYOffset + dataTextOffset);
                            dataTextOffset += dataTextOffsetIncrement;
                            MainCanvas.Children.Add(podText);
                        }


                        if (sys.NPCKillsLastHour > 0)
                        {
                            Label npcText = new Label();
                            npcText.FontSize = 6;
                            npcText.Content = "NPC Kills :" + sys.NPCKillsLastHour;
                            Canvas.SetLeft(npcText, sys.DotlanX + textXOffset);
                            Canvas.SetTop(npcText, sys.DotLanY + textYOffset + dataTextOffset);
                            dataTextOffset += dataTextOffsetIncrement;
                            MainCanvas.Children.Add(npcText);
                        }
                    }


                    int InfoValue = -1;
                    SolidColorBrush InfoColour = new SolidColorBrush(MapConf.ActiveColourScheme.ESIOverlayColour);
                    double InfoSize = 0.0;
                    if(MapConf.ShowNPCKills)
                    {
                        InfoValue = sys.NPCKillsLastHour;
                        InfoSize = 0.15f * InfoValue * MapConf.ESIOverlayScale;
                    }

                    if (MapConf.ShowPodKills)
                    {
                        InfoValue = sys.PodKillsLastHour;
                        InfoSize = 20.0f * InfoValue * MapConf.ESIOverlayScale;

                    }

                    if (MapConf.ShowShipKills)
                    {
                        InfoValue = sys.ShipKillsLastHour;
                        InfoSize = 20.0f * InfoValue  * MapConf.ESIOverlayScale;
                    }

                    if (MapConf.ShowShipJumps)
                    {
                        InfoValue = sys.JumpsLastHour;
                        InfoSize = InfoValue * MapConf.ESIOverlayScale;
                    }


                    if (InfoValue != -1)
                    {

                        Shape infoCircle = new Ellipse() { Height = InfoSize, Width = InfoSize };
                        infoCircle.Fill = InfoColour;

                        Canvas.SetZIndex(infoCircle, 10);
                        Canvas.SetLeft(infoCircle, sys.DotlanX - (InfoSize / 2));
                        Canvas.SetTop(infoCircle, sys.DotLanY - (InfoSize / 2));
                        MainCanvas.Children.Add(infoCircle);
                    }
                }
                else
                {
                    Label sysRegionText = new Label();
                    sysRegionText.Content = "(" + sys.Region + ")";
                    sysRegionText.FontSize = 7;
                    sysRegionText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);

                    Canvas.SetLeft(sysRegionText, sys.DotlanX + textXOffset);
                    Canvas.SetTop(sysRegionText, sys.DotLanY + textYOffset2);
                    Canvas.SetZIndex(sysRegionText, 20);

                    MainCanvas.Children.Add(sysRegionText);

                }
            }

            AddCharactersToMap();

        }


        private void OnRegionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // clear the character selection
            CharacterDropDown.SelectedItem = null;

            EVEData.RegionData rd = RegionDropDown.SelectedItem as EVEData.RegionData;

            ReDrawMap();
            MapConf.DefaultRegion = rd.Name;


            MainContent.DataContext = RegionDropDown.SelectedItem;
            MainAnomGrid.DataContext = null;
            AnomSigList.ItemsSource = null;

            SystemDropDownAC.SelectedItem = null;

            List<EVEData.System> newList = rd.Systems.Values.ToList().OrderBy(o => o.Name).ToList(); ;
            SystemDropDownAC.ItemsSource = newList;
        }

        private void SelectRegion(string RegionName)
        {
            foreach (EVEData.RegionData rd in EVEManager.Regions)
            {
                if (rd.Name == RegionName)
                {
                    RegionDropDown.SelectedItem = rd;
                    List<EVEData.System> newList = rd.Systems.Values.ToList().OrderBy(o => o.Name).ToList(); ;
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
            // clear the character selection
            CharacterDropDown.SelectedItem = null;


            EVEData.System sd = SystemDropDownAC.SelectedItem as EVEData.System;


            if (sd != null)
            {
                SelectSystem(sd.Name);
                ReDrawMap();
            }

        }

        private void SysContexMenuItemDotlan_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            EVEData.RegionData rd = EVEManager.GetRegion(eveSys.Region);

            string URL = String.Format("http://evemaps.dotlan.net/map/{0}/{1}", rd.DotLanRef, eveSys.Name);
            System.Diagnostics.Process.Start(URL);
        }

        private void SysContexMenuItemZKB_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            EVEData.RegionData rd = EVEManager.GetRegion(eveSys.Region);

            string URL = String.Format("https://zkillboard.com/system/{0}", eveSys.ID);
            System.Diagnostics.Process.Start(URL);

        }

        private void HandleCharacterSelectionChange()
        {
            EVEData.Character c = CharacterDropDown.SelectedItem as EVEData.Character;
            EVEData.RegionData rd = RegionDropDown.SelectedItem as EVEData.RegionData;

            if (c != null)
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

            if(pasteData != null || pasteData != "")
            {
                EVEData.AnomData ad = MainAnomGrid.DataContext as EVEData.AnomData;

                if(ad != null)
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
            EVEManager.InitiateESILogon();
        }
    }
}
