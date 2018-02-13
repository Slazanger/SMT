using SMT.EVEData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SMT
{
    /// <summary>
    /// Interaction logic for RegionControl.xaml
    /// </summary>
    public partial class RegionControl : UserControl
    {
        public EVEData.MapRegion Region { get; set; }
        public MapConfig MapConf { get; set; }
        public EveManager EM { get; set; }

        // Constant Colours
        private Brush StandingVBadBrush  = new SolidColorBrush(Color.FromRgb(148, 5, 5));
        private Brush StandingBadBrush   = new SolidColorBrush(Color.FromRgb(196, 72, 6));
        private Brush StandingNeutBrush  = new SolidColorBrush(Color.FromRgb(140, 140, 140));
        private Brush StandingGoodBrush  = new SolidColorBrush(Color.FromRgb(43, 101, 196));
        private Brush StandingVGoodBrush = new SolidColorBrush(Color.FromRgb(5, 34, 120));


        // Store the Dynamic Map elements so they can seperately be cleared
        private List<System.Windows.UIElement> DynamicMapElements;

        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        /// <summary>
        /// Redraw the map
        /// </summary>
        /// <param name="FullRedraw">Clear all the static items or not</param>
        public void ReDrawMap(bool FullRedraw = false)
        {
            if (FullRedraw)
            {
                // reset the background
                MainCanvasGrid.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);
                MainCanvas.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);
                MainZoomControl.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);

                MainCanvas.Children.Clear();

                // re-add the static content
                AddSystemsToMap();
            }
            else
            {
                // remove anything temporary
                foreach (UIElement uie in DynamicMapElements)
                {
                    MainCanvas.Children.Remove(uie);
                }
                DynamicMapElements.Clear();
            }

            AddCharactersToMap();
        }


        private struct GateHelper
        {
            public EVEData.MapSystem from { get; set; }
            public EVEData.MapSystem to { get; set; }
        }


        private const double SYSTEM_SHAPE_SIZE = 18;
        private const double SYSTEM_SHAPE_OFFSET = SYSTEM_SHAPE_SIZE / 2;
        private const double SYSTEM_TEXT_TEXT_SIZE= 6;
        private const double SYSTEM_TEXT_X_OFFSET = 10;
        private const double SYSTEM_TEXT_Y_OFFSET = 2;
        private const double SYSTEM_REGION_TEXT_X_OFFSET = 5;
        private const double SYSTEM_REGION_TEXT_Y_OFFSET = SYSTEM_TEXT_Y_OFFSET + SYSTEM_TEXT_TEXT_SIZE + 2;


        private const int SYSTEM_Z_INDEX = 20;
        private const int SYSTEM_LINK_INDEX = 19;

        /// <summary>
        /// Initialise the control
        /// </summary>
        public void Init()
        {
            EM = EVEData.EveManager.GetInstance();

            DynamicMapElements = new List<UIElement>();

            RegionSelectCB.ItemsSource = EM.Regions;
            SelectRegion(MapConf.DefaultRegion);

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick; ;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            ReDrawMap(false);
        }

        /// <summary>
        /// Select A Region
        /// </summary>
        /// <param name="regionName">Region to Select</param>
        public void SelectRegion(string regionName)
        {
            // check we havent selected the same system
            if(Region !=null && Region.Name == regionName)
            {
                return;
            }

            // check its a valid system
            EVEData.MapRegion mr = EM.GetRegion(regionName);
            if(mr == null)
            {
                return;
            }

            // update the selected region
            Region = mr;
            SystemDropDownAC.ItemsSource = Region.MapSystems.Keys.ToList();

            ReDrawMap(true);
        }

        /// <summary>
        /// Add the base systems, and jumps to the map
        /// </summary>
        private void AddSystemsToMap()
        {
            // brushes
            Brush SysOutlineBrush = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);
            Brush SysInRegionBrush = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemColour);
            Brush SysOutRegionBrush = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemColour);
            Brush SysInRegionTextBrush = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);
            Brush SysOutRegionTextBrush = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);

            Brush FriendlyJumpBridgeBrush = new SolidColorBrush(MapConf.ActiveColourScheme.FriendlyJumpBridgeColour);
            Brush HostileJumpBridgeBrush = new SolidColorBrush(MapConf.ActiveColourScheme.HostileJumpBridgeColour);


            Color bgtc = MapConf.ActiveColourScheme.MapBackgroundColour;
            bgtc.A = 192;
            Brush SysTextBackgroundBrush = new SolidColorBrush(bgtc);

            Brush NormalGateBrush = new SolidColorBrush(MapConf.ActiveColourScheme.NormalGateColour);
            Brush ConstellationGateBrush = new SolidColorBrush(MapConf.ActiveColourScheme.ConstellationGateColour);

            // cache all system links 
            List<GateHelper> systemLinks = new List<GateHelper>();

            foreach (EVEData.MapSystem system in Region.MapSystems.Values.ToList())
            {
                // add circle for system
                Shape systemShape;

                if (system.ActualSystem.HasNPCStation)
                {
                    systemShape = new Rectangle() { Height = SYSTEM_SHAPE_SIZE, Width = SYSTEM_SHAPE_SIZE };
                }
                else
                {
                    systemShape = new Ellipse() { Height = SYSTEM_SHAPE_SIZE, Width = SYSTEM_SHAPE_SIZE };
                }

                systemShape.Stroke = SysOutlineBrush;
                systemShape.StrokeThickness = 1.5;
                systemShape.StrokeLineJoin = PenLineJoin.Round;

                if (system.OutOfRegion)
                {
                    systemShape.Fill = SysOutRegionBrush;
                }
                else
                {
                    systemShape.Fill = SysInRegionBrush;
                }

                // override with sec status colours
                if (MapConf.ShowSystemSecurity)
                {
                    systemShape.Fill = new SolidColorBrush(MapColours.GetSecStatusColour(system.ActualSystem.Security));
                }

                systemShape.DataContext = system;
                systemShape.MouseDown += ShapeMouseDownHandler;
                systemShape.MouseEnter += ShapeMouseOverHandler;
                systemShape.MouseLeave += ShapeMouseOverHandler;

                Canvas.SetLeft(systemShape, system.LayoutX - SYSTEM_SHAPE_OFFSET);
                Canvas.SetTop(systemShape, system.LayoutY - SYSTEM_SHAPE_OFFSET);
                Canvas.SetZIndex(systemShape, SYSTEM_Z_INDEX);
                MainCanvas.Children.Add(systemShape);

                Label sysText = new Label();
                sysText.Content = system.Name;
                if (MapConf.ActiveColourScheme.SystemTextSize > 0)
                {
                    sysText.FontSize = MapConf.ActiveColourScheme.SystemTextSize;
                }

                if (system.OutOfRegion)
                {
                    sysText.Foreground = SysOutRegionTextBrush;
                }
                else
                {
                    sysText.Foreground = SysInRegionTextBrush;
                }
                Thickness border = new Thickness(0.0);

                sysText.Padding = border;
                sysText.Margin = border;
                sysText.Background = SysTextBackgroundBrush;

                Canvas.SetLeft(sysText, system.LayoutX + SYSTEM_TEXT_X_OFFSET);
                Canvas.SetTop(sysText, system.LayoutY + SYSTEM_TEXT_Y_OFFSET);
                Canvas.SetZIndex(sysText, SYSTEM_Z_INDEX);

                MainCanvas.Children.Add(sysText);

                // generate the list of links
                foreach (string jumpTo in system.ActualSystem.Jumps)
                {
                    if (Region.IsSystemOnMap(jumpTo))
                    {
                        EVEData.MapSystem to = Region.MapSystems[jumpTo];

                        bool NeedsAdd = true;
                        foreach (GateHelper gh in systemLinks)
                        {
                            if (((gh.from == system) || (gh.to == system)) && ((gh.from == to) || (gh.to == to)))
                            {
                                NeedsAdd = false;
                                break;
                            }
                        }

                        if (NeedsAdd)
                        {
                            GateHelper g = new GateHelper();
                            g.from = system;
                            g.to = to;
                            systemLinks.Add(g);
                        }
                    }
                }

                double regionMarkerOffset = SYSTEM_REGION_TEXT_Y_OFFSET ;

                if ((MapConf.ShowSystemSovName | MapConf.ShowSystemSovTicker) && system.ActualSystem.SOVAlliance != null && EM.AllianceIDToName.Keys.Contains(system.ActualSystem.SOVAlliance))
                {
                    Label sysRegionText = new Label();

                    string content = "";
                    string allianceName = EM.GetAllianceName(system.ActualSystem.SOVAlliance);
                    string allianceTicker = EM.GetAllianceTicker(system.ActualSystem.SOVAlliance);

                    if (MapConf.ShowSystemSovName)
                    {
                        content = allianceName;
                    }

                    if (MapConf.ShowSystemSovTicker)
                    {
                        content = allianceTicker;
                    }

                    if (MapConf.ShowSystemSovTicker && MapConf.ShowSystemSovName && allianceName != string.Empty && allianceTicker != String.Empty)
                    {
                        content = allianceName + " (" + allianceTicker + ")";
                    }

                    sysRegionText.Content = content;
                    sysRegionText.FontSize = SYSTEM_TEXT_TEXT_SIZE;
                    sysText.FontSize = MapConf.ActiveColourScheme.SystemTextSize;

                    Canvas.SetLeft(sysRegionText, system.LayoutX + SYSTEM_REGION_TEXT_X_OFFSET);
                    Canvas.SetTop (sysRegionText, system.LayoutY + SYSTEM_REGION_TEXT_Y_OFFSET);
                    Canvas.SetZIndex(sysRegionText, SYSTEM_Z_INDEX);

                    MainCanvas.Children.Add(sysRegionText);

                    regionMarkerOffset += SYSTEM_TEXT_TEXT_SIZE;
                }

                if (system.OutOfRegion)
                {
                    Label sysRegionText = new Label();
                    sysRegionText.Content = "(" + system.Region + ")";
                    sysRegionText.FontSize = SYSTEM_TEXT_TEXT_SIZE;
                    sysRegionText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);

                    Canvas.SetLeft(sysRegionText, system.LayoutX + SYSTEM_REGION_TEXT_X_OFFSET);
                    Canvas.SetTop(sysRegionText, system.LayoutY + regionMarkerOffset);
                    Canvas.SetZIndex(sysRegionText, SYSTEM_Z_INDEX);

                    MainCanvas.Children.Add(sysRegionText);
                }
            }

            // now add the links
            foreach (GateHelper gh in systemLinks)
            {
                Line sysLink = new Line();

                sysLink.X1 = gh.from.LayoutX;
                sysLink.Y1 = gh.from.LayoutY;

                sysLink.X2 = gh.to.LayoutX;
                sysLink.Y2 = gh.to.LayoutY;

                if (gh.from.ActualSystem.Region != gh.to.ActualSystem.Region || gh.from.ActualSystem.ConstellationID != gh.to.ActualSystem.ConstellationID)
                {
                    sysLink.Stroke = ConstellationGateBrush;
                }
                else
                {
                    sysLink.Stroke = NormalGateBrush;
                }

                sysLink.StrokeThickness = 1.2;
                sysLink.Visibility = Visibility.Visible;

                Canvas.SetZIndex(sysLink, SYSTEM_LINK_INDEX);
                MainCanvas.Children.Add(sysLink);
            }

            if (MapConf.ShowJumpBridges || MapConf.ShowHostileJumpBridges && EM.JumpBridges != null)
            {
                foreach (EVEData.JumpBridge jb in EM.JumpBridges)
                {
                    if (Region.IsSystemOnMap(jb.From) || Region.IsSystemOnMap(jb.To))
                    {
                        if ((jb.Friendly && !MapConf.ShowJumpBridges) || (!jb.Friendly && !MapConf.ShowHostileJumpBridges))
                        {
                            continue;
                        }

                        EVEData.MapSystem from;



                        // swap as we'll be discarding from
                        if (!Region.IsSystemOnMap(jb.From))
                        {
                            from = Region.MapSystems[jb.To];
                        }
                        else
                        {
                            from = Region.MapSystems[jb.From];
                        }

                        Point startPoint = new Point(from.LayoutX, from.LayoutY);
                        Point endPoint;

                        if (!Region.IsSystemOnMap(jb.To))
                        {
                            endPoint = new Point(from.LayoutX - 20, from.LayoutY - 40);
                        }
                        else
                        {
                            EVEData.MapSystem to = Region.MapSystems[jb.To];
                            endPoint = new Point(to.LayoutX, to.LayoutY);
                        }

                        

                        Vector dir = Point.Subtract(startPoint, endPoint);

                        double jbDistance = Point.Subtract(startPoint, endPoint).Length;

                        Size arcSize = new Size(jbDistance + 60, jbDistance + 60);

                        ArcSegment arcseg = new ArcSegment(endPoint, arcSize, 140, false, SweepDirection.Clockwise, true);

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
                        da.Duration = new Duration(TimeSpan.FromSeconds(90));
                        da.RepeatBehavior = RepeatBehavior.Forever;

                        path.StrokeDashArray = dashes;
                        path.BeginAnimation(Shape.StrokeDashOffsetProperty, da);

                        Canvas.SetZIndex(path, 19);

                        MainCanvas.Children.Add(path);
                    }
                }
            }


        }


        /// <summary>
        /// Add Characters to the region
        /// </summary>
        void AddCharactersToMap()
        {
            // Cache all characters in the same system so we can render them on seperate lines
            Dictionary<string, List<EVEData.Character>> charLocationMap = new Dictionary<string, List<EVEData.Character>>();

            foreach (EVEData.Character c in EM.LocalCharacters)
            {
                // ignore characters out of this Map..
                if (!Region.IsSystemOnMap(c.Location))
                {
                    continue;
                }


                if (!charLocationMap.Keys.Contains(c.Location))
                {
                    charLocationMap[c.Location] = new List<EVEData.Character>();
                }
                charLocationMap[c.Location].Add(c);
            }
            
            foreach (List<EVEData.Character> lc in charLocationMap.Values)
            {
                double textYOffset = -24;
                double textXOffset = 6;

                EVEData.MapSystem ms = Region.MapSystems[lc[0].Location];

                // add circle for system

                double circleSize = 26;
                double circleOffset = circleSize / 2;

                Shape highlightSystemCircle = new Ellipse() { Height = circleSize, Width = circleSize };

                highlightSystemCircle.Stroke = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterHighlightColour);
                highlightSystemCircle.StrokeThickness = 3;

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
                Canvas.SetZIndex(highlightSystemCircle, 25);

                MainCanvas.Children.Add(highlightSystemCircle);
                DynamicMapElements.Add(highlightSystemCircle);

                // Storyboard s = new Storyboard();
                DoubleAnimation da = new DoubleAnimation();
                da.From = 360;
                da.To = 0;
                da.Duration = new Duration(TimeSpan.FromSeconds(12));
                da.RepeatBehavior = RepeatBehavior.Forever;

                RotateTransform eTransform = (RotateTransform)highlightSystemCircle.RenderTransform;
                eTransform.BeginAnimation(RotateTransform.AngleProperty, da);
                
                foreach (EVEData.Character c in lc)
                {
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

                    textYOffset -= (MapConf.ActiveColourScheme.CharacterTextSize + 4);
                }
            }
        }

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

        private void AddDataToMap()
        {
            EVEData.Character c = CharacterDropDown.SelectedItem as EVEData.Character;

            foreach (EVEData.MapSystem sys in Region.MapSystems.Values.ToList())
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



                if ((MapConf.ColourBySov || MapConf.ShowSystemSovStanding) && sys.ActualSystem.SOVAlliance != null)
                {
                    Polygon poly = new Polygon();

                    foreach (Point p in sys.CellPoints)
                    {
                        poly.Points.Add(p);
                    }

                    bool addToMap = true;
                    Brush br = new SolidColorBrush(stringToColour(sys.ActualSystem.SOVAlliance));

                    if (MapConf.ShowSystemSovStanding)
                    {
                        if (c != null && c.ESILinked)
                        {
                            float Standing = 0.0f;

                            if (c.AllianceID != null && c.AllianceID == sys.ActualSystem.SOVAlliance)
                            {
                                Standing = 10.0f;
                            }

                            if (sys.ActualSystem.SOVCorp != null && c.Standings.Keys.Contains(sys.ActualSystem.SOVCorp))
                            {
                                Standing = c.Standings[sys.ActualSystem.SOVCorp];
                            }

                            if (sys.ActualSystem.SOVAlliance != null && c.Standings.Keys.Contains(sys.ActualSystem.SOVAlliance))
                            {
                                Standing = c.Standings[sys.ActualSystem.SOVAlliance];
                            }

                            if (Standing == 0.0f)
                            {
                                addToMap = false;
                            }



                            br = StandingNeutBrush;

                            if (Standing == -10.0)
                            {
                                br = StandingVBadBrush;
                            }

                            if (Standing == -5.0)
                            {
                                br = StandingBadBrush;
                            }

                            if (Standing == 5.0)
                            {
                                br = StandingGoodBrush;
                            }

                            if (Standing == 10.0)
                            {
                                br = StandingVGoodBrush;
                            }
                        }
                        else
                        {
                            // enabled but not linked
                            addToMap = false;
                        }
                    }


                    poly.Fill = br;
                    poly.SnapsToDevicePixels = true;
                    poly.Stroke = poly.Fill;
                    poly.StrokeThickness = 0.5;
                    poly.StrokeDashCap = PenLineCap.Round;
                    poly.StrokeLineJoin = PenLineJoin.Round;

                    if (addToMap)
                    {
                        MainCanvas.Children.Add(poly);

                        // save the dynamic map elements
                        DynamicMapElements.Add(poly);
                    }
                }
            }
        }


        public RegionControl()
        {
            InitializeComponent();
        }

        private void RegionSelectCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EVEData.MapRegion rd = RegionSelectCB.SelectedItem as EVEData.MapRegion;
            if(rd == null)
            {
                return;
            }

            SelectRegion(rd.Name);
        }


        private void SysContexMenuItemAddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            EVEData.Character c = CharacterDropDown.SelectedItem as EVEData.Character;
            if (c != null)
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

        private void SysContexMenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            try
            {
                if (eveSys != null)
                {
                    Clipboard.SetText(eveSys.Name);
                }
            }
            catch { }
        }

        private void SysContexMenuItemDotlan_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("http://evemaps.dotlan.net/map/{0}/{1}", rd.DotLanRef, eveSys.Name);
            System.Diagnostics.Process.Start(uRL);
        }

        private void SysContexMenuItemZKB_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("https://zkillboard.com/system/{0}", eveSys.ActualSystem.ID);
            System.Diagnostics.Process.Start(uRL);
        }


        private void ShapeMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            Shape obj = sender as Shape;

            EVEData.MapSystem selectedSys = obj.DataContext as EVEData.MapSystem;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    bool redraw = false;
                    if (MapConf.ShowJumpDistance)
                    {
                        redraw = true;
                    }
                    // TODO: SelectSystem(selectedSys.Name);

                    ReDrawMap(redraw);

                }

                if (e.ClickCount == 2 && selectedSys.Region != Region.Name)
                {
                    foreach (EVEData.MapRegion rd in EM.Regions)
                    {
                        if (rd.Name == selectedSys.Region)
                        {
                            RegionSelectCB.SelectedItem = rd;

                            ReDrawMap();
                            // TODO:  SelectSystem(selectedSys.Name);
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
                if (c != null && c.ESILinked)
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

            EVEData.MapSystem selectedSys = obj.DataContext as EVEData.MapSystem;

            if (obj.IsMouseOver && MapConf.ShowSystemPopup)
            {
                SystemInfoPopup.PlacementTarget = obj;
                SystemInfoPopup.VerticalOffset = 5;
                SystemInfoPopup.HorizontalOffset = 15;
                SystemInfoPopup.DataContext = selectedSys.ActualSystem;

                // check JB Info
                SystemInfoPopup_JBInfo.Content = "";
                foreach (EVEData.JumpBridge jb in EM.JumpBridges)
                {
                    if(selectedSys.Name == jb.From)
                    {
                        SystemInfoPopup_JBInfo.Content = "JB (" + jb.FromInfo + ") to " + jb.To;
                    }

                    if (selectedSys.Name == jb.To)
                    {
                        SystemInfoPopup_JBInfo.Content = "JB (" + jb.ToInfo + ") to " + jb.From;
                    }

                }



                SystemInfoPopup.IsOpen = true;
            }
            else
            {
                SystemInfoPopup.IsOpen = false;
            }
        }



    }
}
