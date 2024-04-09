﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SMT.EVEData;

namespace SMT
{
    /// <summary>
    /// Interaction logic for UniverseControl.xaml
    /// </summary>
    public partial class UniverseControl : UserControl, INotifyPropertyChanged
    {
        private class VisualHost : FrameworkElement
        {
            // Create a collection of child visual objects.
            private VisualCollection Children;

            private Dictionary<Visual, Object> DataContextData;

            public void AddChild(Visual vis, object dataContext = null)
            {
                Children.Add(vis);
                DataContextData.Add(vis, dataContext);
            }

            public void RemoveChild(Visual vis, object dataContext = null)
            {
                Children.Remove(vis);
                DataContextData.Remove(vis);
            }

            public void ClearAllChildren()
            {
                Children.Clear();
                DataContextData.Clear();
            }

            public bool HitTestEnabled
            {
                get;
                set;
            }

            public VisualHost()
            {
                Children = new VisualCollection(this);
                DataContextData = new Dictionary<Visual, object>();

                HitTestEnabled = false;

                UseLayoutRounding = true;

                MouseRightButtonUp += VisualHost_MouseButtonUp;
            }

            private void VisualHost_MouseButtonUp(object sender, MouseButtonEventArgs e)
            {
                // Retreive the coordinates of the mouse button event.
                Point pt = e.GetPosition((UIElement)sender);

                if (HitTestEnabled)
                {
                    // Initiate the hit test by setting up a hit test result callback method.
                    VisualTreeHelper.HitTest(this, null, HitTestCheck, new PointHitTestParameters(pt));
                }
            }

            // Provide a required override for the VisualChildrenCount property.
            protected override int VisualChildrenCount => Children.Count;

            // Provide a required override for the GetVisualChild method.
            protected override Visual GetVisualChild(int index)
            {
                if (index < 0 || index >= Children.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return Children[index];
            }

            public HitTestResultBehavior HitTestCheck(HitTestResult result)
            {
                System.Windows.Media.DrawingVisual dv = null;
                if (result.VisualHit.GetType() == typeof(System.Windows.Media.DrawingVisual))
                {
                    dv = (System.Windows.Media.DrawingVisual)result.VisualHit;
                }

                if (dv != null && DataContextData.ContainsKey(dv))
                {
                    RoutedEventArgs newEventArgs = new RoutedEventArgs(MouseClickedEvent, DataContextData[dv]);
                    RaiseEvent(newEventArgs);
                }

                // Stop the hit test enumeration of objects in the visual tree.
                return HitTestResultBehavior.Stop;
            }

            public static readonly RoutedEvent MouseClickedEvent = EventManager.RegisterRoutedEvent("MouseClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VisualHost));

            public event RoutedEventHandler MouseClicked
            {
                add { AddHandler(MouseClickedEvent, value); }
                remove { RemoveHandler(MouseClickedEvent, value); }
            }
        }

        private double m_ESIOverlayScale = 1.0f;
        private double universeScale = 49.412945332043492; // note this is calculated based off 5000x5000 at the data compile time
        private bool m_ShowNPCKills;
        private bool m_ShowPodKills;
        private bool m_ShowShipKills;
        private bool m_ShowShipJumps;
        private bool m_ShowJumpBridges = true;

        public MapConfig MapConf { get; set; }

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

        public UniverseControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private struct GateHelper
        {
            public EVEData.System from { get; set; }
            public EVEData.System to { get; set; }
        }

        public bool ShowJumpBridges
        {
            get
            {
                return m_ShowJumpBridges;
            }
            set
            {
                m_ShowJumpBridges = value;
                OnPropertyChanged("ShowJumpBridges");
            }
        }

        public double ESIOverlayScale
        {
            get
            {
                return m_ESIOverlayScale;
            }
            set
            {
                m_ESIOverlayScale = value;
                OnPropertyChanged("ESIOverlayScale");
            }
        }

        public bool ShowNPCKills
        {
            get
            {
                return m_ShowNPCKills;
            }

            set
            {
                m_ShowNPCKills = value;

                if (m_ShowNPCKills)
                {
                    ShowPodKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowNPCKills");
            }
        }

        public bool ShowPodKills
        {
            get
            {
                return m_ShowPodKills;
            }

            set
            {
                m_ShowPodKills = value;
                if (m_ShowPodKills)
                {
                    ShowNPCKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowPodKills");
            }
        }

        public bool ShowShipKills
        {
            get
            {
                return m_ShowShipKills;
            }

            set
            {
                m_ShowShipKills = value;
                if (m_ShowShipKills)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowShipKills");
            }
        }

        public bool ShowShipJumps
        {
            get
            {
                return m_ShowShipJumps;
            }

            set
            {
                m_ShowShipJumps = value;
                if (m_ShowShipJumps)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipKills = false;
                }

                OnPropertyChanged("ShowShipJumps");
            }
        }

        public EVEData.LocalCharacter ActiveCharacter { get; set; }

        public void UpdateActiveCharacter(EVEData.LocalCharacter lc)
        {
            ActiveCharacter = lc;

            if (FollowCharacterChk.IsChecked.HasValue && (bool)FollowCharacterChk.IsChecked)
            {
                CentreMapOnActiveCharacter();
            }
        }

        public EVEData.JumpRoute CapitalRoute { get; set; }

        public static readonly RoutedEvent RequestRegionSystemSelectEvent = EventManager.RegisterRoutedEvent("RequestRegionSystem", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UniverseControl));

        public event RoutedEventHandler RequestRegionSystem
        {
            add { AddHandler(RequestRegionSystemSelectEvent, value); }
            remove { RemoveHandler(RequestRegionSystemSelectEvent, value); }
        }

        private List<GateHelper> universeSysLinksCache;

        private List<KeyValuePair<string, decimal>> activeJumpSpheres;

        private EVEData.EveManager EM;

        private VisualHost VHSystems;
        private VisualHost VHLinks;
        private VisualHost VHNames;
        private VisualHost VHRegionNames;
        private VisualHost VHRangeSpheres;
        private VisualHost VHRangeHighlights;
        private VisualHost VHDataSpheres;
        private VisualHost VHRoute;
        private VisualHost VHRegionShapes;

        private VisualHost VHCharacters;
        private VisualHost VHZKB;

        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        private int uiRefreshTimer_interval = 0;

        public void Init()
        {
            EM = EVEData.EveManager.Instance;

            universeSysLinksCache = new List<GateHelper>();
            activeJumpSpheres = new List<KeyValuePair<string, decimal>>();

            VHSystems = new VisualHost();
            VHSystems.HitTestEnabled = true;
            VHSystems.MouseClicked += VHSystems_MouseClicked;

            VHLinks = new VisualHost();
            VHNames = new VisualHost();
            VHRegionNames = new VisualHost();
            VHRangeSpheres = new VisualHost();
            VHDataSpheres = new VisualHost();
            VHRangeHighlights = new VisualHost();
            VHCharacters = new VisualHost();
            VHZKB = new VisualHost();
            VHRoute = new VisualHost();
            VHRegionShapes = new VisualHost();

            UniverseMainCanvas.Children.Add(VHRegionShapes);

            UniverseMainCanvas.Children.Add(VHRangeSpheres);
            UniverseMainCanvas.Children.Add(VHDataSpheres);
            UniverseMainCanvas.Children.Add(VHZKB);
            UniverseMainCanvas.Children.Add(VHRangeHighlights);

            UniverseMainCanvas.Children.Add(VHLinks);
            UniverseMainCanvas.Children.Add(VHRoute);
            UniverseMainCanvas.Children.Add(VHNames);
            UniverseMainCanvas.Children.Add(VHCharacters);
            UniverseMainCanvas.Children.Add(VHSystems);

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();

            PropertyChanged += UniverseControl_PropertyChanged;

            DataContext = this;

            foreach (EVEData.System sys in EM.Systems)
            {
                foreach (string jumpTo in sys.Jumps)
                {
                    EVEData.System to = EM.GetEveSystem(jumpTo);

                    bool NeedsAdd = true;
                    foreach (GateHelper gh in universeSysLinksCache)
                    {
                        if (((gh.from == sys) || (gh.to == sys)) && ((gh.from == to) || (gh.to == to)))
                        {
                            NeedsAdd = false;
                            break;
                        }
                    }

                    if (NeedsAdd)
                    {
                        GateHelper g = new GateHelper();
                        g.from = sys;
                        g.to = to;
                        universeSysLinksCache.Add(g);
                    }
                }
            }
            List<EVEData.System> globalSystemList = new List<EVEData.System>(EM.Systems);
            globalSystemList.Sort((a, b) => string.Compare(a.Name, b.Name));
            GlobalSystemDropDownAC.ItemsSource = globalSystemList;

            ReDrawMap(true);
        }

        public List<Point> ConvexHull(List<Point> points)
        {
            if (points.Count < 3)
            {
                throw new ArgumentException("At least 3 points reqired", "points");
            }

            List<Point> hull = new List<Point>();

            // get leftmost point
            Point vPointOnHull = points.Where(p => p.X == points.Min(min => min.X)).First();

            Point vEndpoint;
            do
            {
                hull.Add(vPointOnHull);
                vEndpoint = points[0];

                for (int i = 1; i < points.Count; i++)
                {
                    if ((vPointOnHull == vEndpoint)
                        || (Orientation(vPointOnHull, vEndpoint, points[i]) == -1))
                    {
                        vEndpoint = points[i];
                    }
                }

                vPointOnHull = vEndpoint;
            }
            while (vEndpoint != hull[0]);

            return hull;
        }

        // Left test implementation given by Petr
        private static int Orientation(Point p1, Point p2, Point p)
        {
            // Determinant
            int Orin = (int)((p2.X - p1.X) * (p.Y - p1.Y) - (p.X - p1.X) * (p2.Y - p1.Y));

            if (Orin > 0)
                return -1; //          (* Orientation is to the left-hand side  *)
            if (Orin < 0)
                return 1; // (* Orientation is to the right-hand side *)

            return 0; //  (* Orientation is neutral aka collinear  *)
        }

        private void SetJumpRange_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System sys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;

            VHRangeSpheres.ClearAllChildren();
            VHRangeHighlights.ClearAllChildren();

            MenuItem mi = sender as MenuItem;
            double LY = double.Parse(mi.DataContext as string);

            if (LY == -1.0)
            {
                activeJumpSpheres.Clear();
                return;
            }

            foreach (KeyValuePair<string, decimal> kvp in activeJumpSpheres)
            {
                if (kvp.Key == sys.Name)
                {
                    activeJumpSpheres.Remove(kvp);
                    break;
                }
            }

            if (LY > 0)
            {
                activeJumpSpheres.Add(new KeyValuePair<string, decimal>(sys.Name, (decimal)LY));
            }

            Brush rangeCol = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeInColourHighlight);
            Brush rangeOverlapCol = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeOverlapHighlight);
            Brush sysCentreCol = new SolidColorBrush(MapConf.ActiveColourScheme.SelectedSystemColour);
            Brush sysRangeCol = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeInColour);

            rangeCol.Freeze();
            rangeOverlapCol.Freeze();
            sysCentreCol.Freeze();
            sysRangeCol.Freeze();

            System.Windows.Media.DrawingVisual rangeCircleDV = new System.Windows.Media.DrawingVisual();
            DrawingContext drawingContext = rangeCircleDV.RenderOpen();

            foreach (KeyValuePair<string, decimal> kvp in activeJumpSpheres)
            {
                EVEData.System ssys = EM.GetEveSystem(kvp.Key);

                double Radius = (double)(kvp.Value * (decimal)universeScale);

                double X = ssys.UniverseX;
                double Z = ssys.UniverseY;

                // Create an instance of a DrawingVisual.

                drawingContext.DrawEllipse(rangeCol, new Pen(rangeCol, 1), new Point(X, Z), Radius, Radius);
                drawingContext.DrawRectangle(sysCentreCol, new Pen(sysCentreCol, 1), new Rect(X - 5, Z - 5, 10, 10));
            }
            VHRangeSpheres.AddChild(rangeCircleDV);
            drawingContext.Close();

            foreach (EVEData.System es in EM.Systems)
            {
                bool inRange = false;
                bool overlap = false;

                foreach (KeyValuePair<string, decimal> kvp in activeJumpSpheres)
                {
                    decimal Distance = EM.GetRangeBetweenSystems(kvp.Key, es.Name);

                    if (Distance < kvp.Value && Distance > 0.0m && es.TrueSec <= 0.45 && es.Region != "Pochven")
                    {
                        if (inRange == true)
                        {
                            overlap = true;
                        }
                        inRange = true;
                    }
                }

                if (inRange)
                {
                    double irX = es.UniverseX;
                    double irZ = es.UniverseY;
                    System.Windows.Media.DrawingVisual rangeSquareDV = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext from the DrawingVisual.
                    DrawingContext dcR = rangeSquareDV.RenderOpen();
                    if (overlap)
                    {
                        dcR.DrawRectangle(sysRangeCol, new Pen(rangeOverlapCol, 1), new Rect(irX - 3, irZ - 3, 6, 6));
                    }
                    else
                    {
                        dcR.DrawRectangle(sysRangeCol, new Pen(sysRangeCol, 1), new Rect(irX - 3, irZ - 3, 6, 6));
                    }

                    dcR.Close();

                    VHRangeHighlights.AddChild(rangeSquareDV);
                }
            }
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            uiRefreshTimer_interval++;

            bool FullRedraw = false;
            bool FastUpdate = true;
            bool DataRedraw = false;

            if (uiRefreshTimer_interval == 4)
            {
                uiRefreshTimer_interval = 0;
                DataRedraw = false;
            }

            if (FollowCharacterChk.IsChecked.HasValue && (bool)FollowCharacterChk.IsChecked)
            {
                CentreMapOnActiveCharacter();
            }
            ReDrawMap(FullRedraw, DataRedraw, FastUpdate);
        }

        private void VHSystems_MouseClicked(object sender, RoutedEventArgs e)
        {
            EVEData.System sys = (EVEData.System)e.OriginalSource;

            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                currentDebugSystem = sys;
            }
            else
            {
                currentDebugSystem = null;
            }

            ContextMenu cm = this.FindResource("SysRightClickContextMenu") as ContextMenu;

            cm.DataContext = sys;
            cm.IsOpen = true;

            MenuItem setDesto = cm.Items[2] as MenuItem;
            MenuItem addWaypoint = cm.Items[4] as MenuItem;
            MenuItem clearRoute = cm.Items[6] as MenuItem;

            if (ActiveCharacter != null && ActiveCharacter.ESILinked)
            {
                setDesto.IsEnabled = true;
                addWaypoint.IsEnabled = true;
                clearRoute.IsEnabled = true;
            }

            // update SOV
            MenuItem SovHeader = cm.Items[9] as MenuItem;
            SovHeader.Items.Clear();
            SovHeader.IsEnabled = false;

            if (sys.SOVAllianceIHUB != 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "IHUB: " + EM.GetAllianceTicker(sys.SOVAllianceIHUB);
                mi.DataContext = sys.SOVAllianceIHUB;
                mi.Click += VHSystems_SOV_Clicked;
                SovHeader.IsEnabled = true;
                SovHeader.Items.Add(mi);
            }

            if (sys.SOVAllianceTCU != 0)
            {
                MenuItem mi = new MenuItem();
                mi.DataContext = sys.SOVAllianceTCU;
                mi.Header = "TCU : " + EM.GetAllianceTicker(sys.SOVAllianceTCU);
                mi.Click += VHSystems_SOV_Clicked;
                SovHeader.IsEnabled = true;
                SovHeader.Items.Add(mi);
            }

            // update stats
            MenuItem StatsHeader = cm.Items[8] as MenuItem;
            StatsHeader.Items.Clear();
            StatsHeader.IsEnabled = false;

            if (sys.HasNPCStation)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "NPC Station(s)";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.HasIceBelt)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Ice Belts";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.HasJoveObservatory)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Jove Observatory";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.JumpsLastHour > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Jumps : " + sys.JumpsLastHour;
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.ShipKillsLastHour > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Ship Kills : " + sys.ShipKillsLastHour;
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.PodKillsLastHour > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Pod Kills : " + sys.PodKillsLastHour;
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.NPCKillsLastHour > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "NPC Kills : " + sys.NPCKillsLastHour + " (Delta: " + sys.NPCKillsDeltaLastHour + ")";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.RadiusAU > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Radius : " + sys.RadiusAU.ToString("#.##") + " (AU)";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }
        }

        private void VHSystems_SOV_Clicked(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            long ID = (long)mi.DataContext;

            if (ID != 0)
            {
                string uRL = string.Format("https://evewho.com/alliance/{0}", ID);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uRL) { UseShellExecute = true });
            }
        }

        private void UniverseControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReDrawMap(false, true, true);
        }

        private Brush SystemColourBrush;
        private Brush SystemColourHiSecBrush;
        private Brush SystemColourLowSecBrush;
        private Brush SystemColourNullSecBrush;
        private Brush SystemColourOutlineBrush;

        private Brush ConstellationColourBrush;
        private Brush SystemTextColourBrush;
        private Brush RegionTextColourBrush;
        private Brush RegionTextZoomedOutColourBrush;
        private Brush GateColourBrush;
        private Brush RegionGateColourBrush;
        private Brush PochvenGateColourBrush;
        private Brush JumpBridgeColourBrush;
        private Brush DataColourBrush;
        private Brush BackgroundColourBrush;
        private Brush RegionShapeColourBrush;

        /// <summary>
        /// Redraw the map
        /// </summary>
        /// <param name="FullRedraw">Clear all the static items or not</param>
        public void ReDrawMap(bool FullRedraw = false, bool DataRedraw = false, bool FastUpdate = false)
        {
            double textXOffset = 0;
            double textYOffset = 3;

            double SystemTextSize = 5;
            double CharacterTextSize = 6;

            // recreate the brushes on a full draw
            if (FullRedraw)
            {
                SystemColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.UniverseSystemColour);
                SystemColourHiSecBrush = new SolidColorBrush(MapColours.GetSecStatusColour(1.0, false));
                SystemColourLowSecBrush = new SolidColorBrush(MapColours.GetSecStatusColour(0.4, false));
                SystemColourNullSecBrush = new SolidColorBrush(MapColours.GetSecStatusColour(-0.5, true));
                SystemColourOutlineBrush = new SolidColorBrush(Colors.Black);

                ConstellationColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.UniverseConstellationGateColour);
                SystemTextColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.UniverseSystemTextColour);
                RegionTextColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.RegionMarkerTextColour);
                RegionTextZoomedOutColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.RegionMarkerTextColourFull);
                GateColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.UniverseGateColour);
                JumpBridgeColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.FriendlyJumpBridgeColour);
                DataColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.ESIOverlayColour);
                BackgroundColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.UniverseMapBackgroundColour);
                RegionGateColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.UniverseRegionGateColour);
                PochvenGateColourBrush = new SolidColorBrush(Colors.DimGray);

                Color RegionShapeFillCol = MapConf.ActiveColourScheme.UniverseMapBackgroundColour;
                RegionShapeFillCol.R = (Byte)(RegionShapeFillCol.R * 0.8);
                RegionShapeFillCol.G = (Byte)(RegionShapeFillCol.G * 0.8);
                RegionShapeFillCol.B = (Byte)(RegionShapeFillCol.B * 0.8);

                RegionShapeColourBrush = new SolidColorBrush(RegionShapeFillCol);

                SystemColourBrush.Freeze();
                SystemColourHiSecBrush.Freeze();
                SystemColourLowSecBrush.Freeze();
                SystemColourNullSecBrush.Freeze();
                SystemColourOutlineBrush.Freeze();

                ConstellationColourBrush.Freeze();

                SystemTextColourBrush.Freeze();
                RegionTextColourBrush.Freeze();
                GateColourBrush.Freeze();
                JumpBridgeColourBrush.Freeze();
                DataColourBrush.Freeze();
                BackgroundColourBrush.Freeze();
                RegionTextZoomedOutColourBrush.Freeze();
                RegionShapeColourBrush.Freeze();
            }

            SolidColorBrush PositiveDeltaColor = new SolidColorBrush(Colors.Green);
            SolidColorBrush NegativeDeltaColor = new SolidColorBrush(Colors.Red);

            SolidColorBrush jumpRouteColour = new SolidColorBrush(Colors.Orange);
            SolidColorBrush activeRouteColour = new SolidColorBrush(Colors.Yellow);
            SolidColorBrush activeRouteAniblexColour = new SolidColorBrush(Colors.DarkMagenta);

            // update the background colours
            MainZoomControl.Background = BackgroundColourBrush;
            UniverseMainCanvas.Background = BackgroundColourBrush;

            System.Windows.FontStyle fontStyle = FontStyles.Normal;
            FontWeight fontWeight = FontWeights.Medium;
            Typeface tf = new Typeface("Verdana");

            if (FullRedraw)
            {
                VHSystems.ClearAllChildren();
                VHLinks.ClearAllChildren();
                VHNames.ClearAllChildren();
                VHRegionShapes.ClearAllChildren();
                VHRoute.ClearAllChildren();

                ReCreateRegionMarkers(MainZoomControl.Zoom > MapConf.UniverseMaxZoomDisplaySystems);

                Pen GatePen = new Pen(GateColourBrush, 0.6);
                Pen ConstGatePen = new Pen(ConstellationColourBrush, 0.6);
                Pen RegionGatePen = new Pen(RegionGateColourBrush, 0.8);
                Pen SysOutlinePen = new Pen(SystemColourOutlineBrush, 0.3);
                Pen PochvenGatePen = new Pen(PochvenGateColourBrush, 0.3);

                System.Windows.Media.DrawingVisual gatesDrawingVisual = new System.Windows.Media.DrawingVisual();
                DrawingContext gatesDrawingContext = gatesDrawingVisual.RenderOpen();

                foreach (GateHelper gh in universeSysLinksCache)
                {
                    double X1 = gh.from.UniverseX;
                    double Y1 = gh.from.UniverseY;

                    double X2 = gh.to.UniverseX;
                    double Y2 = gh.to.UniverseY;
                    Pen p = GatePen;

                    if (gh.from.ConstellationID != gh.to.ConstellationID)
                    {
                        p = ConstGatePen;
                    }

                    if (gh.from.Region != gh.to.Region)
                    {
                        p = RegionGatePen;
                    }

                    if (gh.from.Region == "Pochven")
                    {
                        p = PochvenGatePen;
                    }

                    gatesDrawingContext.DrawLine(p, new Point(X1, Y1), new Point(X2, Y2));
                }

                gatesDrawingContext.Close();
                //gatesDrawingVisual.CacheMode = new BitmapCache(3);
                VHLinks.AddChild(gatesDrawingVisual, "link");

                if (ShowJumpBridges)
                {
                    Pen p = new Pen(JumpBridgeColourBrush, 0.6);
                    p.DashStyle = DashStyles.Dot;

                    System.Windows.Media.DrawingVisual jbDrawingVisual = new System.Windows.Media.DrawingVisual();
                    DrawingContext drawingContext;
                    drawingContext = jbDrawingVisual.RenderOpen();

                    foreach (EVEData.JumpBridge jb in EM.JumpBridges)
                    {
                        EVEData.System from = EM.GetEveSystem(jb.From);
                        EVEData.System to = EM.GetEveSystem(jb.To);

                        double X1 = from.UniverseX;
                        double Y1 = from.UniverseY;

                        double X2 = to.UniverseX;
                        double Y2 = to.UniverseY;

                        // Create a rectangle and draw it in the DrawingContext.
                        drawingContext.DrawLine(p, new Point(X1, Y1), new Point(X2, Y2));
                    }

                    drawingContext.Close();
                    VHLinks.AddChild(jbDrawingVisual, "JB");
                }

                // Create an instance of a DrawingVisual.

                CultureInfo ci = CultureInfo.GetCultureInfo("en-us");

                foreach (EVEData.System sys in EM.Systems)
                {
                    System.Windows.Media.DrawingVisual SystemTextVisual = new System.Windows.Media.DrawingVisual();
                    DrawingContext systemTextDrawingContext = SystemTextVisual.RenderOpen();

                    double X = sys.UniverseX;
                    double Z = sys.UniverseY;

                    System.Windows.Media.DrawingVisual systemShapeVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = systemShapeVisual.RenderOpen();

                    // Create a rectangle and draw it in the DrawingContext.
                    Rect rect = new Rect(X - 2, Z - 2, 4, 4);

                    Brush sysbrush = SystemColourNullSecBrush;
                    if (sys.TrueSec >= 0.45)
                    {
                        sysbrush = SystemColourHiSecBrush;
                    }
                    else if (sys.TrueSec > 0.0)
                    {
                        sysbrush = SystemColourLowSecBrush;
                    }

                    if (sys.HasNPCStation)
                    {
                        drawingContext.DrawRectangle(sysbrush, SysOutlinePen, rect);
                    }
                    else
                    {
                        drawingContext.DrawEllipse(sysbrush, SysOutlinePen, new Point(X, Z), 2, 2);
                    }

                    // Persist the drawing content.
                    drawingContext.Close();
                    VHSystems.AddChild(systemShapeVisual, sys);

                    FormattedText ft = new FormattedText(sys.Name,
                             ci,
                             FlowDirection.LeftToRight,
                             tf,
                             SystemTextSize, SystemTextColourBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    ft.TextAlignment = TextAlignment.Center;
                    // Draw a formatted text string into the DrawingContext.
                    systemTextDrawingContext.DrawText(

                        ft,
                        new Point(X + textXOffset, Z + textYOffset));

                    // Close the DrawingContext to persist changes to the DrawingVisual.

                    systemTextDrawingContext.Close();
                    VHNames.AddChild(SystemTextVisual, null);
                }

                // region shapes
                {
                    Pen RegionShapePen = new Pen(RegionShapeColourBrush, 1.0);
                    System.Windows.Media.DrawingVisual dataDV = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = dataDV.RenderOpen();

                    foreach (EVEData.System sys in EM.Systems)
                    {
                        double X = sys.UniverseX;

                        // need to invert Z
                        double Z = sys.UniverseY;

                        double blobSize = 55;

                        drawingContext.DrawEllipse(RegionShapeColourBrush, RegionShapePen, new Point(X, Z), blobSize, blobSize);
                    }
                    drawingContext.Close();

                    dataDV.CacheMode = new BitmapCache(0.1);
                    VHRegionShapes.AddChild(dataDV);
                }
            }

            if (DataRedraw)
            {
                Brush dataBrush = DataColourBrush;

                // update the data
                VHDataSpheres.ClearAllChildren();

                Pen dataPen = new Pen(dataBrush, 1);

                foreach (EVEData.System sys in EM.Systems)
                {
                    System.Windows.Media.DrawingVisual dataDV = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = dataDV.RenderOpen();

                    double X = sys.UniverseX;

                    // need to invert Z
                    double Z = sys.UniverseY;

                    double DataScale = 0;

                    if (ShowNPCKills)
                    {
                        DataScale = sys.NPCKillsLastHour * ESIOverlayScale * 0.05f;

                        if (MapConf.ShowRattingDataAsDelta)
                        {
                            if (!MapConf.ShowNegativeRattingDelta)
                            {
                                DataScale = Math.Max(0, sys.NPCKillsDeltaLastHour) * ESIOverlayScale * 0.05f; ;
                            }
                            else
                            {
                                DataScale = Math.Abs(sys.NPCKillsDeltaLastHour) * ESIOverlayScale * 0.05f;

                                if (sys.NPCKillsDeltaLastHour > 0)
                                {
                                    dataBrush = PositiveDeltaColor;
                                }
                                else
                                {
                                    dataBrush = NegativeDeltaColor;
                                }
                            }
                        }
                    }

                    if (ShowPodKills)
                    {
                        DataScale = sys.PodKillsLastHour * ESIOverlayScale * 2f;
                    }

                    if (ShowShipKills)
                    {
                        DataScale = sys.ShipKillsLastHour * ESIOverlayScale * 1f;
                    }

                    if (ShowShipJumps)
                    {
                        DataScale = sys.JumpsLastHour * ESIOverlayScale * 0.1f;
                    }

                    if (DataScale > 3)
                    {
                        // Create a rectangle and draw it in the DrawingContext.
                        drawingContext.DrawEllipse(dataBrush, dataPen, new Point(X, Z), DataScale, DataScale);
                    }

                    drawingContext.Close();
                    VHDataSpheres.AddChild(dataDV);
                }
            }

            if (FastUpdate)
            {
                VHCharacters.ClearAllChildren();
                VHZKB.ClearAllChildren();
                VHRoute.ClearAllChildren();

                float characterNametextXOffset = 3;
                float characterNametextYOffset = -16;
                Brush CharacterNameBrush = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterTextColour);
                Brush CharacterOfflineNameBrush = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterOfflineTextColour);
                Brush CharacterNameSysHighlightBrush = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterHighlightColour);
                Brush ZKBBrush = new SolidColorBrush(MapConf.ActiveColourScheme.ZKillDataOverlay);
                Brush RouteBrush = new SolidColorBrush(Colors.Orange);
                Brush RouteAltBrush = new SolidColorBrush(Colors.DarkRed);

                if (MapConf.ShowCharacterNamesOnMap)
                {
                    Pen p = new Pen(CharacterNameSysHighlightBrush, 1.0);

                    Dictionary<string, List<string>> MapCharacters = new Dictionary<string, List<string>>();

                    // add the characters
                    foreach (EVEData.LocalCharacter lc in EM.LocalCharacters)
                    {
                        if (!string.IsNullOrEmpty(lc.Location))
                        {
                            if (!MapCharacters.ContainsKey(lc.Location))
                            {
                                MapCharacters.Add(lc.Location, new List<string>());
                            }
                            MapCharacters[lc.Location].Add(lc.Name);
                        }
                    }

                    foreach (KeyValuePair<string, List<string>> kvp in MapCharacters)
                    {
                        EVEData.System sys = EM.GetEveSystem(kvp.Key);
                        if (sys == null)
                        {
                            continue;
                        }
                        double X = sys.UniverseX;                        // need to invert Z
                        double Z = sys.UniverseY;

                        double charTextOffset = 0;

                        // Create an instance of a DrawingVisual.
                        System.Windows.Media.DrawingVisual nameTextVisual = new System.Windows.Media.DrawingVisual();

                        // Retrieve the DrawingContext from the DrawingVisual.
                        DrawingContext dc = nameTextVisual.RenderOpen();

                        // draw a circle around the system
                        dc.DrawEllipse(CharacterNameSysHighlightBrush, p, new Point(X, Z), 6, 6);

                        foreach (string name in kvp.Value)
                        {
                            // Draw a formatted text string into the DrawingContext.
                            dc.DrawText(
                                new FormattedText(name,
                                    CultureInfo.GetCultureInfo("en-us"),
                                    FlowDirection.LeftToRight,
                                    tf,
                                    CharacterTextSize, CharacterNameBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip),
                                new Point(X + characterNametextXOffset, Z + characterNametextYOffset + charTextOffset));

                            charTextOffset -= (CharacterTextSize + 2);
                        }

                        dc.Close();
                        VHCharacters.AddChild(nameTextVisual);
                    }
                }

                // now add the zkill data
                Dictionary<string, int> ZKBBaseFeed = new Dictionary<string, int>();
                {
                    foreach (EVEData.ZKillRedisQ.ZKBDataSimple zs in EM.ZKillFeed.KillStream.ToList())
                    {
                        if (ZKBBaseFeed.ContainsKey(zs.SystemName))
                        {
                            ZKBBaseFeed[zs.SystemName]++;
                        }
                        else
                        {
                            ZKBBaseFeed[zs.SystemName] = 1;
                        }
                    }

                    Pen zkbPen = new Pen(ZKBBrush, 1.0);

                    foreach (KeyValuePair<string, int> kvp in ZKBBaseFeed)
                    {
                        // Create an instance of a DrawingVisual.
                        System.Windows.Media.DrawingVisual zkbVisual = new System.Windows.Media.DrawingVisual();

                        // Retrieve the DrawingContext from the DrawingVisual.
                        DrawingContext dc = zkbVisual.RenderOpen();

                        double zkbVal = 5 + ((double)kvp.Value * ESIOverlayScale * 2);

                        EVEData.System sys = EM.GetEveSystem(kvp.Key);
                        if (sys == null)
                        {
                            // probably a WH
                            continue;
                        }
                        double X = sys.UniverseX;
                        // need to invert Z
                        double Z = sys.UniverseY;

                        // draw a circle around the system
                        dc.DrawEllipse(ZKBBrush, zkbPen, new Point(X, Z), zkbVal, zkbVal);
                        dc.Close();
                        VHZKB.AddChild(zkbVisual, "ZKBData");
                    }
                }

                if (CapitalRoute != null && CapitalRoute.CurrentRoute.Count > 1)
                {
                    Pen dashedRoutePen = new Pen(jumpRouteColour, 2);
                    dashedRoutePen.DashStyle = DashStyles.Dot;
                    Pen outlinePen = new Pen(activeRouteColour, 2);
                    Pen outlineAltPen = new Pen(RouteAltBrush, 2);

                    System.Windows.Media.DrawingVisual routeVisual = new System.Windows.Media.DrawingVisual();

                    //Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = routeVisual.RenderOpen();

                    // add the lines
                    for (int i = 1; i < CapitalRoute.CurrentRoute.Count; i++)
                    {
                        Pen linePen = dashedRoutePen;

                        EVEData.System sysA = EM.GetEveSystem(CapitalRoute.CurrentRoute[i - 1].SystemName);
                        EVEData.System sysB = EM.GetEveSystem(CapitalRoute.CurrentRoute[i].SystemName);

                        if (sysA != null && sysB != null)
                        {
                            double X1 = sysA.UniverseX;
                            double Y1 = sysA.UniverseY;

                            double X2 = sysB.UniverseX;
                            double Y2 = sysB.UniverseY;

                            if (i == 1)
                            {
                                drawingContext.DrawEllipse(RouteBrush, linePen, new Point(X1, Y1), 6, 6);
                            }
                            drawingContext.DrawEllipse(RouteBrush, linePen, new Point(X2, Y2), 6, 6);

                            //Create a rectangle and draw it in the DrawingContext.
                            drawingContext.DrawLine(linePen, new Point(X1, Y1), new Point(X2, Y2));
                        }
                    }

                    // add the alternates
                    List<string> alts = new List<string>();
                    foreach (List<string> sss in CapitalRoute.AlternateMids.Values)
                    {
                        foreach (string s in sss)
                        {
                            if (!alts.Contains(s))
                            {
                                alts.Add(s);
                            }
                        }
                    }
                    foreach (string s in alts)
                    {
                        EVEData.System sys = EM.GetEveSystem(s);
                        if (s != null)
                        {
                            double X = sys.UniverseX;
                            double Y = sys.UniverseY;

                            drawingContext.DrawEllipse(RouteAltBrush, outlineAltPen, new Point(X, Y), 3, 3);
                        }
                    }

                    drawingContext.Close();

                    VHRoute.AddChild(routeVisual, "ActiveRoute");
                }
            }
        }

        private void MainZoomControl_ZoomChanged(object sender, RoutedEventArgs e)
        {
            if (MainZoomControl.Zoom < MapConf.UniverseMaxZoomDisplaySystemsText)
            {
                VHNames.Visibility = Visibility.Hidden;
                VHRangeHighlights.Visibility = Visibility.Hidden;
            }
            else
            {
                VHNames.Visibility = Visibility.Visible;
                VHRangeHighlights.Visibility = Visibility.Visible;
            }

            if (MainZoomControl.Zoom < MapConf.UniverseMaxZoomDisplaySystems)
            {
                VHSystems.Visibility = Visibility.Hidden;
                ReCreateRegionMarkers(true);
                VHRegionShapes.Visibility = Visibility.Visible;
                VHLinks.Visibility = Visibility.Hidden;
            }
            else
            {
                VHSystems.Visibility = Visibility.Visible;
                VHRegionShapes.Visibility = Visibility.Hidden;
                VHLinks.Visibility = Visibility.Visible;
                ReCreateRegionMarkers(false);
            }
        }

        private bool RegionZoomed = false;

        private void ReCreateRegionMarkers(bool ZoomedOut)
        {
            if (RegionZoomed == ZoomedOut)
            {
                return;
            }
            RegionZoomed = ZoomedOut;

            UniverseMainCanvas.Children.Remove(VHRegionNames);
            VHRegionNames.ClearAllChildren();

            double RegionTextSize = 50;
            Typeface tf = new Typeface("Verdana");

            Brush rtb = RegionTextColourBrush;
            if (ZoomedOut)
            {
                UniverseMainCanvas.Children.Add(VHRegionNames);
                rtb = RegionTextZoomedOutColourBrush;
            }
            else
            {
                UniverseMainCanvas.Children.Insert(0, VHRegionNames);
            }

            foreach (EVEData.MapRegion mr in EM.Regions)
            {
                if (mr.MetaRegion || mr.Name == "Pochven")
                {
                    continue;
                }

                double X = mr.RegionX;
                double Z = mr.RegionY;

                System.Windows.Media.DrawingVisual SystemTextVisual = new System.Windows.Media.DrawingVisual();
                DrawingContext drawingContext = SystemTextVisual.RenderOpen();

                FormattedText ft = new FormattedText(mr.Name, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, tf, RegionTextSize, rtb, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                ft.TextAlignment = TextAlignment.Center;
                drawingContext.DrawText(ft, new Point(X, Z));

                drawingContext.Close();

                VHRegionNames.AddChild(SystemTextVisual);
            }
        }

        public void ShowSystem(string SystemName)
        {
            EVEData.System sd = EM.GetEveSystem(SystemName);

            if (sd != null)
            {
                // actual
                double X1 = sd.UniverseX;
                double Y1 = sd.UniverseY;

                MainZoomControl.Show(X1, Y1, 3.0);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void GlobalSystemDropDownAC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EVEData.System sd = GlobalSystemDropDownAC.SelectedItem as EVEData.System;

            if (sd != null)
            {
                FollowCharacterChk.IsChecked = false;
                ShowSystem(sd.Name);
            }
        }

        /// <summary>
        /// Dotlan Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemDotlan_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("http://evemaps.dotlan.net/map/{0}/{1}", rd.DotLanRef, eveSys.Name);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uRL) { UseShellExecute = true });
        }

        /// <summary>
        /// ZKillboard Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemZKB_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("https://zkillboard.com/system/{0}/", eveSys.ID);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uRL) { UseShellExecute = true });
        }

        private void SysContexMenuShowInRegion_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System s = ((System.Windows.Controls.MenuItem)e.OriginalSource).DataContext as EVEData.System;

            RoutedEventArgs newEventArgs = new RoutedEventArgs(RequestRegionSystemSelectEvent, s.Name);
            RaiseEvent(newEventArgs);
        }

        private void FollowCharacterChk_Checked(object sender, RoutedEventArgs e)
        {
            CentreMapOnActiveCharacter();
        }

        private void CentreMapOnActiveCharacter()
        {
            if (ActiveCharacter == null || string.IsNullOrEmpty(ActiveCharacter.Location))
            {
                return;
            }

            EVEData.System s = EM.GetEveSystem(ActiveCharacter.Location);

            if (s != null)
            {
                // actual
                double X1 = s.UniverseX;
                double Y1 = s.UniverseY;

                MainZoomControl.Show(X1, Y1, MainZoomControl.Zoom);
            }
        }

        private void MainZoomControl_ContentDragFinished(object sender, RoutedEventArgs e)
        {
            if (FollowCharacterChk.IsChecked.HasValue && (bool)FollowCharacterChk.IsChecked)
            {
                FollowCharacterChk.IsChecked = false;
            }
        }

        private void RecentreBtn_Click(object sender, RoutedEventArgs e)
        {
            CentreMapOnActiveCharacter();
        }

        private void SysContexMenuItemSetDestination_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            if (ActiveCharacter != null)
            {
                ActiveCharacter.AddDestination(eveSys.ID, true);
            }
        }

        private void SysContexMenuItemSetDestinationAll_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;

            foreach (LocalCharacter lc in EM.LocalCharacters)
            {
                if (lc.IsOnline && lc.ESILinked)
                {
                    lc.AddDestination(eveSys.ID, true);
                }
            }

        }


        private void SysContexMenuItemAddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            if (ActiveCharacter != null)
            {
                ActiveCharacter.AddDestination(eveSys.ID, false);
            }
        }

        private void SysContexMenuItemAddWaypointAll_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            foreach (LocalCharacter lc in EM.LocalCharacters)
            {
                if (lc.IsOnline && lc.ESILinked)
                {
                    lc.AddDestination(eveSys.ID, false);
                }
            }
        }





        private void SysContexMenuItemClearRoute_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveCharacter != null)
            {
                ActiveCharacter.ClearAllWaypoints();
            }
        }

        private EVEData.System currentDebugSystem;

        private void UniverseMainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MapConf.Debug_EnableMapEdit == false)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                Point p = Mouse.GetPosition(UniverseMainCanvas);

                if (currentDebugSystem != null)
                {
                    currentDebugSystem.UniverseX = p.X;
                    currentDebugSystem.UniverseY = p.Y;
                    currentDebugSystem.CustomUniverseLayout = true;
                    currentDebugSystem = null;
                    ReDrawMap(true, true, false);
                }
            }
        }
    }
}