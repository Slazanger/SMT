using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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
        private bool m_ShowNPCKills = false;
        private bool m_ShowPodKills = false;
        private bool m_ShowShipKills = false;
        private bool m_ShowShipJumps = false;
        private bool m_ShowJumpBridges = true;



        public MapConfig MapConf { get; set; }


        public UniverseControl()
        {
            InitializeComponent();
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


        public static readonly RoutedEvent RequestRegionSystemSelectEvent = EventManager.RegisterRoutedEvent("RequestRegionSystem", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UniverseControl));

        public event RoutedEventHandler RequestRegionSystem
        {
            add { AddHandler(RequestRegionSystemSelectEvent, value); }
            remove { RemoveHandler(RequestRegionSystemSelectEvent, value); }
        }



        private List<GateHelper> universeSysLinksCache;
        private double universeWidth;
        private double universeDepth;
        private double universeXMin;
        private double universeXMax;
        private double universeScale;

        private double universeZMin;
        private double universeZMax;

        private EVEData.EveManager EM;


        private VisualHost VHSystems;
        private VisualHost VHLinks;
        private VisualHost VHNames;
        private VisualHost VHRegionNames;
        private VisualHost VHRangeSpheres;
        private VisualHost VHRangeHighlights;
        private VisualHost VHDataSpheres;

        private VisualHost VHCharacters;
        private VisualHost VHZKB;


        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;
        private int uiRefreshTimer_interval = 0;


        public void Init()
        {
            EM = EVEData.EveManager.Instance;



            universeSysLinksCache = new List<GateHelper>();


            universeXMin = 0.0;
            universeXMax = 336522971264518000.0;

            universeZMin = -484452845697854000;
            universeZMax = 472860102256057000.0;

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


            UniverseMainCanvas.Children.Add(VHRangeSpheres);
            UniverseMainCanvas.Children.Add(VHDataSpheres);
            UniverseMainCanvas.Children.Add(VHZKB);
            UniverseMainCanvas.Children.Add(VHRangeHighlights);

            UniverseMainCanvas.Children.Add(VHLinks);
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

                if (sys.ActualX < universeXMin)
                {
                    universeXMin = sys.ActualX;
                }

                if (sys.ActualX > universeXMax)
                {
                    universeXMax = sys.ActualX;
                }

                if (sys.ActualZ < universeZMin)
                {
                    universeZMin = sys.ActualZ;
                }

                if (sys.ActualZ > universeZMax)
                {
                    universeZMax = sys.ActualZ;
                }

            }


            universeWidth = universeXMax - universeXMin;
            universeDepth = universeZMax - universeZMin;


            List<EVEData.System> globalSystemList = new List<EVEData.System>(EM.Systems);
            globalSystemList.Sort((a, b) => string.Compare(a.Name, b.Name));
            GlobalSystemDropDownAC.ItemsSource = globalSystemList;



            ReDrawMap(true);
        }


        private void SetJumpRange_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System sys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;

            VHRangeSpheres.ClearAllChildren();
            VHRangeHighlights.ClearAllChildren();

            MenuItem mi = sender as MenuItem;
            double AU = double.Parse(mi.DataContext as string);

            if (AU == 0.0)
            {
                return;
            }


            double Radius = 9460730472580800.0 * AU * universeScale;
            Brush rangeCol = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeInColourHighlight);
            Brush sysCentreCol = new SolidColorBrush(MapConf.ActiveColourScheme.SelectedSystemColour);
            Brush sysRangeCol = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeInColour);

            rangeCol.Freeze();
            sysCentreCol.Freeze();
            sysRangeCol.Freeze();

            double X = (sys.ActualX - universeXMin) * universeScale; ;
            double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;



            // Create an instance of a DrawingVisual.
            System.Windows.Media.DrawingVisual rangeCircleDV = new System.Windows.Media.DrawingVisual();
            DrawingContext drawingContext = rangeCircleDV.RenderOpen();

            drawingContext.DrawEllipse(rangeCol, new Pen(rangeCol, 1), new Point(X, Z), Radius, Radius);
            drawingContext.DrawRectangle(sysCentreCol, new Pen(sysCentreCol, 1), new Rect(X - 5, Z - 5, 10, 10));

            drawingContext.Close();

            VHRangeSpheres.AddChild(rangeCircleDV);

            foreach (EVEData.System es in EM.Systems)
            {

                double Distance = EM.GetRangeBetweenSystems(sys.Name, es.Name);
                Distance = Distance / 9460730472580800.0;

                double Max = AU;

                if (Distance < Max && Distance > 0.0)
                {
                    double irX = (es.ActualX - universeXMin) * universeScale; ;
                    double irZ = (universeDepth - (es.ActualZ - universeZMin)) * universeScale;

                    System.Windows.Media.DrawingVisual rangeSquareDV = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext from the DrawingVisual.
                    DrawingContext dcR = rangeSquareDV.RenderOpen();

                    dcR.DrawRectangle(sysRangeCol, new Pen(sysRangeCol, 1), new Rect(irX - 5, irZ - 5, 10, 10));
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

            ReDrawMap(FullRedraw, DataRedraw, FastUpdate);
        }

        private void VHSystems_MouseClicked(object sender, RoutedEventArgs e)
        {
            EVEData.System sys = (EVEData.System)e.OriginalSource;


            ContextMenu cm = this.FindResource("SysRightClickContextMenu") as ContextMenu;

            cm.DataContext = sys;
            cm.IsOpen = true;
        }

        private void UniverseControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReDrawMap(false, true, true);
        }



        private Brush SystemColourBrush;
        private Brush ConstellationColourBrush;
        private Brush SystemTextColourBrush;
        private Brush RegionTextColourBrush;
        private Brush RegionTextZoomedOutColourBrush;
        private Brush GateColourBrush;
        private Brush JumpBridgeColourBrush;
        private Brush DataColourBrush;
        private Brush BackgroundColourBrush;



        /// <summary>
        /// Redraw the map
        /// </summary>
        /// <param name="FullRedraw">Clear all the static items or not</param>
        public void ReDrawMap(bool FullRedraw = false, bool DataRedraw = false, bool FastUpdate = false)
        {

            double textXOffset = 3;
            double textYOffset = 2;

            double SystemTextSize = 5;
            double CharacterTextSize = 6;

            double XScale = (UniverseMainCanvas.Width) / universeWidth;
            double ZScale = (UniverseMainCanvas.Height) / universeDepth;
            universeScale = Math.Min(XScale, ZScale);


            // recreate the brushes on a full draw
            if (FullRedraw)
            {
                SystemColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemColour);
                ConstellationColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.ConstellationGateColour);
                SystemTextColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);
                RegionTextColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.RegionMarkerTextColour);
                RegionTextZoomedOutColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.RegionMarkerTextColourFull);
                GateColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.NormalGateColour);
                JumpBridgeColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.FriendlyJumpBridgeColour);
                DataColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.ESIOverlayColour);
                BackgroundColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);


                SystemColourBrush.Freeze();
                ConstellationColourBrush.Freeze();
                SystemTextColourBrush.Freeze();
                RegionTextColourBrush.Freeze();
                GateColourBrush.Freeze();
                JumpBridgeColourBrush.Freeze();
                DataColourBrush.Freeze();
                BackgroundColourBrush.Freeze();
                RegionTextZoomedOutColourBrush.Freeze();


            }




            // update the background colours
            MainZoomControl.Background = BackgroundColourBrush;
            UniverseMainCanvas.Background = BackgroundColourBrush;




            System.Windows.FontStyle fontStyle = FontStyles.Normal;
            FontWeight fontWeight = FontWeights.Medium;
            Typeface tf = new Typeface("Verdana");

            if (FullRedraw)
            {

                VHLinks.ClearAllChildren();
                VHNames.ClearAllChildren();


                ReCreateRegionMarkers(MainZoomControl.Zoom > MapConf.UniverseMaxZoomDisplaySystems);


                foreach (GateHelper gh in universeSysLinksCache)
                {
                    double X1 = (gh.from.ActualX - universeXMin) * universeScale;
                    double Y1 = (universeDepth - (gh.from.ActualZ - universeZMin)) * universeScale;

                    double X2 = (gh.to.ActualX - universeXMin) * universeScale;
                    double Y2 = (universeDepth - (gh.to.ActualZ - universeZMin)) * universeScale;
                    Brush Col = GateColourBrush;

                    if (gh.from.Region != gh.to.Region || gh.from.ConstellationID != gh.to.ConstellationID)
                    {
                        Col = ConstellationColourBrush;
                    }

                    System.Windows.Media.DrawingVisual sysLinkVisual = new System.Windows.Media.DrawingVisual();
                    DrawingContext drawingContext = sysLinkVisual.RenderOpen();
                    drawingContext.DrawLine(new Pen(Col, 0.6), new Point(X1, Y1), new Point(X2, Y2));
                    drawingContext.Close();

                    VHLinks.AddChild(sysLinkVisual, "link");
                }

                if (ShowJumpBridges)
                {
                    foreach (EVEData.JumpBridge jb in EM.JumpBridges)
                    {
                        Line jbLink = new Line();

                        EVEData.System from = EM.GetEveSystem(jb.From);
                        EVEData.System to = EM.GetEveSystem(jb.To);


                        double X1 = (from.ActualX - universeXMin) * universeScale; ;
                        double Y1 = (universeDepth - (from.ActualZ - universeZMin)) * universeScale;

                        double X2 = (to.ActualX - universeXMin) * universeScale;
                        double Y2 = (universeDepth - (to.ActualZ - universeZMin)) * universeScale;


                        System.Windows.Media.DrawingVisual sysLinkVisual = new System.Windows.Media.DrawingVisual();

                        // Retrieve the DrawingContext in order to create new drawing content.
                        DrawingContext drawingContext = sysLinkVisual.RenderOpen();

                        Pen p = new Pen(JumpBridgeColourBrush, 0.6);
                        p.DashStyle = DashStyles.Dot;

                        // Create a rectangle and draw it in the DrawingContext.
                        drawingContext.DrawLine(p, new Point(X1, Y1), new Point(X2, Y2));

                        drawingContext.Close();

                        VHLinks.AddChild(sysLinkVisual, "JB");
                    }
                }




                foreach (EVEData.System sys in EM.Systems)
                {

                    double X = (sys.ActualX - universeXMin) * universeScale;

                    // need to invert Z
                    double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;


                    System.Windows.Media.DrawingVisual systemShapeVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = systemShapeVisual.RenderOpen();

                    // Create a rectangle and draw it in the DrawingContext.
                    Rect rect = new Rect(X - 3, Z - 3, 6, 6);
                    drawingContext.DrawRectangle(SystemColourBrush, null, rect);

                    // Persist the drawing content.
                    drawingContext.Close();
                    VHSystems.AddChild(systemShapeVisual, sys);

                    // add text


                    // Create an instance of a DrawingVisual.
                    System.Windows.Media.DrawingVisual SystemTextVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext from the DrawingVisual.
                    drawingContext = SystemTextVisual.RenderOpen();

#pragma warning disable CS0618 
                    // Draw a formatted text string into the DrawingContext.
                    drawingContext.DrawText(
                        new FormattedText(sys.Name,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            tf,
                            SystemTextSize, SystemTextColourBrush),
                        new Point(X + textXOffset, Z + textYOffset));
#pragma warning restore CS0618 

                    // Close the DrawingContext to persist changes to the DrawingVisual.
                    drawingContext.Close();

                    VHNames.AddChild(SystemTextVisual, sys.Name);

                }
            }


            if (DataRedraw)
            {
                // update the data
                VHDataSpheres.ClearAllChildren();
                foreach (EVEData.System sys in EM.Systems)
                {

                    double X = (sys.ActualX - universeXMin) * universeScale;

                    // need to invert Z
                    double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;

                    double DataScale = 0;


                    if (ShowNPCKills)
                    {
                        DataScale = sys.NPCKillsLastHour * ESIOverlayScale * 0.05f;
                    }

                    if (ShowPodKills)
                    {
                        DataScale = sys.PodKillsLastHour * ESIOverlayScale * 2f;
                    }

                    if (ShowShipKills)
                    {
                        DataScale = sys.ShipKillsLastHour * ESIOverlayScale * 8f;
                    }

                    if (ShowShipJumps)
                    {
                        DataScale = sys.JumpsLastHour * ESIOverlayScale * 0.1f;
                    }

                    if (DataScale > 3)
                    {
                        System.Windows.Media.DrawingVisual dataDV = new System.Windows.Media.DrawingVisual();

                        // Retrieve the DrawingContext in order to create new drawing content.
                        DrawingContext drawingContext = dataDV.RenderOpen();

                        // Create a rectangle and draw it in the DrawingContext.
                        drawingContext.DrawEllipse(DataColourBrush, new Pen(DataColourBrush, 1), new Point(X, Z), DataScale, DataScale);

                        drawingContext.Close();

                        VHDataSpheres.AddChild(dataDV);
                    }
                }

            }

            if (FastUpdate)
            {
                VHCharacters.ClearAllChildren();
                VHZKB.ClearAllChildren();

                float characterNametextXOffset = 3;
                float characterNametextYOffset = -16;
                Brush CharacterNameBrush = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterTextColour);
                Brush CharacterNameSysHighlightBrush = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterHighlightColour);
                Brush ZKBBrush = new SolidColorBrush(MapConf.ActiveColourScheme.ZKillDataOverlay);


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
                    double X = (sys.ActualX - universeXMin) * universeScale;
                    // need to invert Z
                    double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;


                    double charTextOffset = 0;

                    // Create an instance of a DrawingVisual.
                    System.Windows.Media.DrawingVisual nameTextVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext from the DrawingVisual.
                    DrawingContext dc = nameTextVisual.RenderOpen();

                    // draw a circle around the system
                    dc.DrawEllipse(CharacterNameSysHighlightBrush, new Pen(CharacterNameSysHighlightBrush, 1.0), new Point(X, Z), 6, 6);


                    foreach (string name in kvp.Value)
                    {
#pragma warning disable CS0618 
                        // Draw a formatted text string into the DrawingContext.
                        dc.DrawText(
                            new FormattedText(name,
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                tf,
                                CharacterTextSize, CharacterNameBrush),
                            new Point(X + characterNametextXOffset, Z + characterNametextYOffset + charTextOffset));
#pragma warning restore CS0618 

                        charTextOffset -= (CharacterTextSize + 2);
                    }

                    dc.Close();
                    VHCharacters.AddChild(nameTextVisual);
                }


                // now add the zkill data
                Dictionary<string, int> ZKBBaseFeed = new Dictionary<string, int>();
                {
                    foreach (EVEData.ZKillRedisQ.ZKBDataSimple zs in EM.ZKillFeed.KillStream)
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


                    foreach (KeyValuePair<string, int> kvp in ZKBBaseFeed)
                    {
                        double zkbVal = 5 + ((double)kvp.Value * ESIOverlayScale * 2);

                        EVEData.System sys = EM.GetEveSystem(kvp.Key);
                        if (sys == null)
                        {
                            // probably a WH
                            continue;
                        }
                        double X = (sys.ActualX - universeXMin) * universeScale;
                        // need to invert Z
                        double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;


                        // Create an instance of a DrawingVisual.
                        System.Windows.Media.DrawingVisual zkbVisual = new System.Windows.Media.DrawingVisual();

                        // Retrieve the DrawingContext from the DrawingVisual.
                        DrawingContext dc = zkbVisual.RenderOpen();

                        // draw a circle around the system
                        dc.DrawEllipse(ZKBBrush, new Pen(ZKBBrush, 1.0), new Point(X, Z), zkbVal, zkbVal);


                        dc.Close();

                        VHZKB.AddChild(zkbVisual, "ZKBData");


                    }
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

            }
            else
            {
                VHSystems.Visibility = Visibility.Visible;
                ReCreateRegionMarkers(false);
            }


        }

        private bool RegionZoomed = false;
        private void ReCreateRegionMarkers(bool ZoomedOut)
        {

            if(RegionZoomed == ZoomedOut)
            {
                return;
            }
            RegionZoomed = ZoomedOut;

            UniverseMainCanvas.Children.Remove(VHRegionNames);
            VHRegionNames.ClearAllChildren();



            double RegionTextSize = 50;
            Typeface tf = new Typeface("Verdana");

            Brush rtb = RegionTextColourBrush;
            if(ZoomedOut)
            {
                UniverseMainCanvas.Children.Add(VHRegionNames);
                rtb = RegionTextZoomedOutColourBrush;
            }
            else
            {
                UniverseMainCanvas.Children.Insert(0,VHRegionNames);
            }

            foreach (EVEData.MapRegion mr in EM.Regions)
            {
                double X = (mr.RegionX - universeXMin) * universeScale; ;
                double Z = (universeDepth - (mr.RegionZ - universeZMin)) * universeScale;

                System.Windows.Media.DrawingVisual SystemTextVisual = new System.Windows.Media.DrawingVisual();

                DrawingContext drawingContext = SystemTextVisual.RenderOpen();

#pragma warning disable CS0618
                FormattedText ft = new FormattedText(mr.Name, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, tf, RegionTextSize, rtb);
                ft.TextAlignment = TextAlignment.Center;
                drawingContext.DrawText(ft, new Point(X, Z));
#pragma warning restore CS0618

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
                double X1 = (sd.ActualX - universeXMin) * universeScale;
                double Y1 = (universeDepth - (sd.ActualZ - universeZMin)) * universeScale;

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

            ShowSystem(sd.Name);

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
            System.Diagnostics.Process.Start(uRL);
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

            string uRL = string.Format("https://zkillboard.com/system/{0}", eveSys.ID);
            System.Diagnostics.Process.Start(uRL);
        }


        private void SysContexMenuShowInRegion_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System s = ((System.Windows.Controls.MenuItem)e.OriginalSource).DataContext as EVEData.System;

            RoutedEventArgs newEventArgs = new RoutedEventArgs(RequestRegionSystemSelectEvent, s.Name);
            RaiseEvent(newEventArgs);
        }
    }
}

