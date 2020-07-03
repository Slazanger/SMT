using SMT.EVEData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WpfHelpers.ResourceUsage;

namespace SMT
{
    /// <summary>
    /// Interaction logic for RegionControl.xaml
    /// </summary>
    public partial class RegionControl : UserControl, INotifyPropertyChanged
    {
        public static readonly RoutedEvent UniverseSystemSelectEvent = EventManager.RegisterRoutedEvent("UniverseSystemSelect", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UniverseControl));
        private const int SYSTEM_LINK_INDEX = 19;
        private const double SYSTEM_REGION_TEXT_X_OFFSET = 6;
        private const double SYSTEM_REGION_TEXT_Y_OFFSET = SYSTEM_TEXT_Y_OFFSET + SYSTEM_TEXT_TEXT_SIZE + 2;
        private const double SYSTEM_SHAPE_OFFSET = SYSTEM_SHAPE_SIZE / 2;
        private const double SYSTEM_SHAPE_SIZE = 20;
        private const double SYSTEM_TEXT_TEXT_SIZE = 7;
        private const double SYSTEM_TEXT_X_OFFSET = 10;
        private const double SYSTEM_TEXT_Y_OFFSET = 2;
        private const int SYSTEM_Z_INDEX = 22;

        private Dictionary<string, List<KeyValuePair<bool, string>>> NameTrackingLocationMap = new Dictionary<string, List<KeyValuePair<bool, string>>>();

        // Store the Dynamic Map elements so they can seperately be cleared
        private List<System.Windows.UIElement> DynamicMapElements;

        private LocalCharacter m_ActiveCharacter;

        // Map Controls
        private double m_ESIOverlayScale = 1.0f;

        private bool m_ShowJumpBridges = true;
        private bool m_ShowNPCKills = false;
        private bool m_ShowPodKills = false;
        private bool m_ShowShipJumps = false;
        private bool m_ShowShipKills = false;
        private bool m_ShowSovOwner = false;
        private bool m_ShowStandings = false;
        private bool m_ShowSystemSecurity = false;
        private bool m_ShowSystemADM = false;
        private bool m_ShowSystemTimers = false;

        private long SelectedAlliance = 0;
        private readonly Brush SelectedAllianceBrush = new SolidColorBrush(Color.FromArgb(180, 200, 200, 200));
        private Brush StandingBadBrush = new SolidColorBrush(Color.FromArgb(110, 196, 72, 6));
        private Brush StandingGoodBrush = new SolidColorBrush(Color.FromArgb(110, 43, 101, 196));
        private Brush StandingNeutBrush = new SolidColorBrush(Color.FromArgb(110, 140, 140, 140));

        // Constant Colours
        private Brush StandingVBadBrush = new SolidColorBrush(Color.FromArgb(110, 148, 5, 5));

        private Brush StandingVGoodBrush = new SolidColorBrush(Color.FromArgb(110, 5, 34, 120));

        private List<Point> SystemIcon_Astrahaus = new List<Point>
        {
            new Point(6,12),
            new Point(6,7),
            new Point(9,7),
            new Point(9,4),
            //new Point(10,4),
            new Point(9,7),
            new Point(12,7),
            new Point(12,12),
        };

        private List<Point> SystemIcon_Fortizar = new List<Point>
        {
            new Point(4,12),
            new Point(4,7),
            new Point(6,7),
            new Point(6,5),
            new Point(12,5),
            new Point(12,7),
            new Point(14,7),
            new Point(14,12),
        };

        private List<Point> SystemIcon_Keepstar = new List<Point>
        {
            new Point(1,17),
            new Point(1,0),
            new Point(7,0),
            new Point(7,7),
            new Point(12,7),
            new Point(12,0),
            new Point(18,0),
            new Point(18,17),
        };

        private List<Point> SystemIcon_NPCStation = new List<Point>
        {
            new Point(2,16),
            new Point(2,2),
            new Point(16,2),
            new Point(16,16),
        };

        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        private string currentJumpCharacter;

        private EVEData.EveManager.JumpShip jumpShipType;

        private string currentCharacterJumpSystem;

        private bool showJumpDistance;

        private Dictionary<string, EVEData.EveManager.JumpShip> activeJumpSpheres;


        /// <summary>
        /// Constructor
        /// </summary>
        public RegionControl()
        {
            InitializeComponent();
            DataContext = this;

            activeJumpSpheres = new Dictionary<string, EVEData.EveManager.JumpShip>();
        }



        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangedEventHandler RegionChanged;

        public event RoutedEventHandler UniverseSystemSelect
        {
            add { AddHandler(UniverseSystemSelectEvent, value); }
            remove { RemoveHandler(UniverseSystemSelectEvent, value); }
        }

        public LocalCharacter ActiveCharacter
        {
            get
            {
                return m_ActiveCharacter;
            }
            set
            {
                m_ActiveCharacter = value;
                OnPropertyChanged("ActiveCharacter");
            }
        }

        public AnomManager ANOMManager { get; set; }
        public EveManager EM { get; set; }

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

        public MapConfig MapConf { get; set; }
        public EVEData.MapRegion Region { get; set; }
        public string SelectedSystem { get; set; }

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

        public bool ShowSovOwner
        {
            get
            {
                return m_ShowSovOwner;
            }
            set
            {
                m_ShowSovOwner = value;
                OnPropertyChanged("ShowSovOwner");
            }
        }

        public bool ShowStandings
        {
            get
            {
                return m_ShowStandings;
            }
            set
            {
                m_ShowStandings = value;

                OnPropertyChanged("ShowStandings");
            }
        }

        public bool ShowSystemSecurity
        {
            get
            {
                return m_ShowSystemSecurity;
            }
            set
            {
                m_ShowSystemSecurity = value;
                if (m_ShowSystemSecurity)
                {
                    ShowSystemADM = false;
                }
                OnPropertyChanged("ShowSystemSecurity");
            }
        }

        public bool ShowSystemADM
        {
            get
            {
                return m_ShowSystemADM;
            }
            set
            {
                m_ShowSystemADM = value;
                if (m_ShowSystemADM)
                {
                    ShowSystemSecurity = false;
                }
                OnPropertyChanged("ShowSystemADM");
            }
        }

        public bool ShowSystemTimers
        {
            get
            {
                return m_ShowSystemTimers;
            }
            set
            {
                m_ShowSystemTimers = value;
                OnPropertyChanged("ShowSystemTimers");
            }
        }

        public void AddTheraSystemsToMap()
        {
            Brush TheraBrush = new SolidColorBrush(MapConf.ActiveColourScheme.TheraEntranceSystem);

            foreach (TheraConnection tc in EM.TheraConnections)
            {
                if (Region.IsSystemOnMap(tc.System))
                {
                    MapSystem ms = Region.MapSystems[tc.System];

                    Shape TheraShape;
                    if (ms.ActualSystem.HasNPCStation)
                    {
                        TheraShape = new Rectangle() { Height = SYSTEM_SHAPE_SIZE + 6, Width = SYSTEM_SHAPE_SIZE + 6 };
                    }
                    else
                    {
                        TheraShape = new Ellipse() { Height = SYSTEM_SHAPE_SIZE + 6, Width = SYSTEM_SHAPE_SIZE + 6 };
                    }

                    TheraShape.Stroke = TheraBrush;
                    TheraShape.StrokeThickness = 1.5;
                    TheraShape.StrokeLineJoin = PenLineJoin.Round;
                    TheraShape.Fill = TheraBrush;

                    Canvas.SetLeft(TheraShape, ms.LayoutX - (SYSTEM_SHAPE_OFFSET + 3));
                    Canvas.SetTop(TheraShape, ms.LayoutY - (SYSTEM_SHAPE_OFFSET + 3));
                    Canvas.SetZIndex(TheraShape, SYSTEM_Z_INDEX - 3);
                    MainCanvas.Children.Add(TheraShape);
                }
            }
        }


        public void AddSovConflictsToMap()
        {
            if(!ShowSystemTimers)
            {
                return;
            }

            Brush ActiveSovFightBrush = new SolidColorBrush(Colors.DarkRed);

            foreach (SOVCampaign sc in EM.ActiveSovCampaigns)
            {
                if (Region.IsSystemOnMap(sc.System))
                {
                    MapSystem ms = Region.MapSystems[sc.System];



                    Image SovFightLogo = new Image
                    {
                        Width = 10,
                        Height = 10,
                        Name = "JoveLogo",
                        Source = ResourceLoader.LoadBitmapFromResource("Images/Fight.png"),
                        Stretch = Stretch.Uniform,
                        IsHitTestVisible = false,
                    };

                    

                    SovFightLogo.IsHitTestVisible = false;

                    Canvas.SetLeft(SovFightLogo, ms.LayoutX - SYSTEM_SHAPE_OFFSET + 5);
                    Canvas.SetTop(SovFightLogo, ms.LayoutY - SYSTEM_SHAPE_OFFSET + 5);
                    Canvas.SetZIndex(SovFightLogo, SYSTEM_Z_INDEX + 5);
                    MainCanvas.Children.Add(SovFightLogo);


                    if(sc.IsActive || sc.Type == "IHub")
                    {
                        Shape activeSovFightShape = new Ellipse() { Height = SYSTEM_SHAPE_SIZE + 18, Width = SYSTEM_SHAPE_SIZE + 18 };
                        

                        activeSovFightShape.Stroke = ActiveSovFightBrush;
                        activeSovFightShape.StrokeThickness = 9;
                        activeSovFightShape.StrokeLineJoin = PenLineJoin.Round;
                        activeSovFightShape.Fill = ActiveSovFightBrush;

                        Canvas.SetLeft(activeSovFightShape, ms.LayoutX - (SYSTEM_SHAPE_OFFSET + 9));
                        Canvas.SetTop(activeSovFightShape, ms.LayoutY - (SYSTEM_SHAPE_OFFSET + 9));
                        Canvas.SetZIndex(activeSovFightShape, SYSTEM_Z_INDEX - 3);
                        MainCanvas.Children.Add(activeSovFightShape);
                    }     
                }
            }
        }


        /// <summary>
        /// Initialise the control
        /// </summary>
        public void Init()
        {
            EM = EVEData.EveManager.Instance;
            SelectedSystem = string.Empty;

            DynamicMapElements = new List<UIElement>();

            ActiveCharacter = null;

            RegionSelectCB.ItemsSource = EM.Regions;
            SelectRegion(MapConf.DefaultRegion);

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick; ;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 2);
            uiRefreshTimer.Start();

            DataContext = this;

            List<EVEData.System> globalSystemList = new List<EVEData.System>(EM.Systems);
            globalSystemList.Sort((a, b) => string.Compare(a.Name, b.Name));
            GlobalSystemDropDownAC.ItemsSource = globalSystemList;


            List<EVEData.MapSystem> newList = Region.MapSystems.Values.ToList().OrderBy(o => o.Name).ToList();
            SystemDropDownAC.ItemsSource = newList;

            PropertyChanged += MapObjectChanged;
        }

        /// <summary>
        /// Redraw the map
        /// </summary>
        /// <param name="FullRedraw">Clear all the static items or not</param>
        public void ReDrawMap(bool FullRedraw = false)
        {
            if (ActiveCharacter != null && FollowCharacter == true)
            {
                UpdateActiveCharacter();
            }

            if (FullRedraw)
            {
                // reset the background
                //MainCanvasGrid.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);

                Color c1 = MapConf.ActiveColourScheme.MapBackgroundColour;
                Color c2 = MapConf.ActiveColourScheme.MapBackgroundColour;
                c1.R = (byte)(0.9 * c1.R);
                c1.G = (byte)(0.9 * c1.G);
                c1.B = (byte)(0.9 * c1.B);

                LinearGradientBrush lgb = new LinearGradientBrush();
                lgb.StartPoint = new Point(0, 0);
                lgb.EndPoint = new Point(0, 1);

                lgb.GradientStops.Add(new GradientStop(c1, 0.0));
                lgb.GradientStops.Add(new GradientStop(c2, 0.05));
                lgb.GradientStops.Add(new GradientStop(c2, 0.95));
                lgb.GradientStops.Add(new GradientStop(c1, 1.0));

                MainCanvasGrid.Background = lgb;

                //                MainCanvas.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);
                MainCanvasGrid.Background = lgb;
                //MainZoomControl.Background = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);
                MainZoomControl.Background = lgb;

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
            AddDataToMap();
            AddSystemIntelOverlay();
            AddHighlightToSystem(SelectedSystem);
            AddRouteToMap();
            AddTheraSystemsToMap();
            AddSovConflictsToMap();
        }

        /// <summary>
        /// Select A Region
        /// </summary>
        /// <param name="regionName">Region to Select</param>
        public void SelectRegion(string regionName)
        {
            // check we havent selected the same system
            if (Region != null && Region.Name == regionName)
            {
                return;
            }

            FollowCharacter = false;


            // close the context menu if its open
            ContextMenu cm = this.FindResource("SysRightClickContextMenu") as ContextMenu;
            cm.IsOpen = false;

            SelectedAlliance = 0;

            EM.UpdateIDsForMapRegion(regionName);

            // check its a valid system
            EVEData.MapRegion mr = EM.GetRegion(regionName);
            if (mr == null)
            {
                return;
            }

            // update the selected region
            Region = mr;
            RegionNameLabel.Content = mr.Name;
            MapConf.DefaultRegion = mr.Name;

            List<EVEData.MapSystem> newList = Region.MapSystems.Values.ToList().OrderBy(o => o.Name).ToList();
            SystemDropDownAC.ItemsSource = newList;

            // SJS Disabled until ticket resolved with CCP
            //            if (ActiveCharacter != null)
            //            {
            //                ActiveCharacter.UpdateStructureInfoForRegion2(regionName);
            //            }

            ReDrawMap(true);

            // select the item in the dropdown
            RegionSelectCB.SelectedItem = Region;

            OnRegionChanged(regionName);
        }

        public void SelectSystem(string name, bool changeRegion = false)
        {
            if (SelectedSystem == name)
            {
                return;
            }

            EVEData.System sys = EM.GetEveSystem(name);

            if (sys == null)
            {
                return;
            }

            if (changeRegion && !Region.IsSystemOnMap(name))
            {
                SelectRegion(sys.Region);
            }

            foreach (KeyValuePair<string, MapSystem> kvp in Region.MapSystems)
            {
                if (kvp.Value.Name == name)
                {
                    SystemDropDownAC.SelectedItem = kvp.Value;
                    SelectedSystem = kvp.Value.Name;
                    AddHighlightToSystem(name);

                    break;
                }
            }

            // now setup the anom data

            EVEData.AnomData system = ANOMManager.GetSystemAnomData(name);
            ANOMManager.ActiveSystem = system;
            ///AnomSigList.ItemsSource = system.Anoms.Values;
        }


        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        protected void OnRegionChanged(string name)
        {
            PropertyChangedEventHandler handler = RegionChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// Add Characters to the region
        /// </summary>
        private void AddCharactersToMap()
        {
            // Cache all characters in the same system so we can render them on seperate lines
            if(!MapConf.ShowCharacterNamesOnMap)
            {
                return;
            }



            NameTrackingLocationMap.Clear();

            foreach (EVEData.LocalCharacter c in EM.LocalCharacters)
            {
                // ignore characters out of this Map..
                if (!Region.IsSystemOnMap(c.Location))
                {
                    continue;
                }

                if (!NameTrackingLocationMap.ContainsKey(c.Location))
                {
                    NameTrackingLocationMap[c.Location] = new List<KeyValuePair<bool, string>>();
                }
                NameTrackingLocationMap[c.Location].Add(new KeyValuePair<bool, string>(true, c.Name));
            }

            if (ActiveCharacter != null && MapConf.FleetShowOnMap)
            {
                foreach (Fleet.FleetMember fm in ActiveCharacter.FleetInfo.Members)
                {

                    if (!Region.IsSystemOnMap(fm.Location))
                    {
                        continue;
                    }

                    // check its not one of our characters
                    bool addFleetMember = true;
                    foreach (EVEData.LocalCharacter c in EM.LocalCharacters)
                    {
                        if (c.Name == fm.Name)
                        {
                            addFleetMember = false;
                            break;
                        }
                    }

                    if (addFleetMember)
                    {
                        // ignore characters out of this Map..
                        if (!Region.IsSystemOnMap(fm.Location))
                        {
                            continue;
                        }

                        if (!NameTrackingLocationMap.ContainsKey(fm.Location))
                        {
                            NameTrackingLocationMap[fm.Location] = new List<KeyValuePair<bool, string>>();
                        }

                        string displayName = fm.Name;
                        if(MapConf.FleetShowShipType)
                        {
                            displayName += " (" + fm.ShipType + ")";
                        }
                        NameTrackingLocationMap[fm.Location].Add(new KeyValuePair<bool, string>(false, displayName));
                    }
                }
            }


            foreach (string lkvpk in NameTrackingLocationMap.Keys)
            {
                List<KeyValuePair<bool, string>> lkvp = NameTrackingLocationMap[lkvpk];
                EVEData.MapSystem ms = Region.MapSystems[lkvpk];


                bool addIndividualFleetMembers = true;
                int fleetMemberCount = 0;
                foreach(KeyValuePair<bool, string> kvp in lkvp)
                {
                    if(kvp.Key == false)
                    {
                        fleetMemberCount++;
                    }
                }

                if(fleetMemberCount > MapConf.FleetMaxMembersPerSystem)
                {
                    addIndividualFleetMembers = false;
                }

                double textYOffset = -24;
                double textXOffset = 6;


                SolidColorBrush fleetMemberText = new SolidColorBrush(MapConf.ActiveColourScheme.FleetMemberTextColour);
                SolidColorBrush localCharacterText = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterTextColour);

                foreach (KeyValuePair<bool, string> kvp in lkvp)
                {
                    if(kvp.Key || kvp.Key == false && addIndividualFleetMembers)
                    {
                        Label charText = new Label();
                        charText.Content = kvp.Value;
                        charText.Foreground = kvp.Key ? localCharacterText : fleetMemberText;
                        charText.IsHitTestVisible = false;

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

                if (!addIndividualFleetMembers)
                {
                    Label charText = new Label();
                    charText.Content = "Fleet (" + fleetMemberCount + ")";
                    charText.Foreground = fleetMemberText;
                    charText.IsHitTestVisible = false;

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

                Timeline.SetDesiredFrameRate(da, 20);

                RotateTransform eTransform = (RotateTransform)highlightSystemCircle.RenderTransform;
                eTransform.BeginAnimation(RotateTransform.AngleProperty, da);

            }

            List<string> WarningZoneHighlights = new List<string>();

            foreach (EVEData.LocalCharacter c in EM.LocalCharacters)
            {
                if (MapConf.ShowDangerZone && c.WarningSystems != null)
                {
                    foreach (string s in c.WarningSystems)
                    {
                        if (!WarningZoneHighlights.Contains(s))
                        {
                            WarningZoneHighlights.Add(s);
                        }
                    }
                }
            }

            double warningCircleSize = 40;
            double warningCircleSizeOffset = warningCircleSize / 2;

            foreach (string s in WarningZoneHighlights)
            {
                if (Region.IsSystemOnMap(s))
                {
                    EVEData.MapSystem mss = Region.MapSystems[s];
                    Shape WarninghighlightSystemCircle = new Ellipse() { Height = warningCircleSize, Width = warningCircleSize };
                    WarninghighlightSystemCircle.Stroke = new SolidColorBrush(Colors.IndianRed);
                    WarninghighlightSystemCircle.StrokeThickness = 3;

                    Canvas.SetLeft(WarninghighlightSystemCircle, mss.LayoutX - warningCircleSizeOffset);
                    Canvas.SetTop(WarninghighlightSystemCircle, mss.LayoutY - warningCircleSizeOffset);
                    Canvas.SetZIndex(WarninghighlightSystemCircle, 24);
                    MainCanvas.Children.Add(WarninghighlightSystemCircle);
                    DynamicMapElements.Add(WarninghighlightSystemCircle);
                }
            }

        }

        private void AddDataToMap()
        {
            Color DataColor = MapConf.ActiveColourScheme.ESIOverlayColour;
            Color DataLargeColor = MapConf.ActiveColourScheme.ESIOverlayColour;

            DataLargeColor.R = (byte)(DataLargeColor.R * 0.75);
            DataLargeColor.G = (byte)(DataLargeColor.G * 0.75);
            DataLargeColor.B = (byte)(DataLargeColor.B * 0.75);

            Color DataLargeColorDelta = MapConf.ActiveColourScheme.ESIOverlayColour;
            DataLargeColorDelta.R = (byte)(DataLargeColorDelta.R * 0.4);
            DataLargeColorDelta.G = (byte)(DataLargeColorDelta.G * 0.4);
            DataLargeColorDelta.B = (byte)(DataLargeColorDelta.B * 0.4);

            SolidColorBrush dataColor = new SolidColorBrush(DataColor);
            SolidColorBrush infoColour = dataColor;

            SolidColorBrush PositiveDeltaColor = new SolidColorBrush(Colors.Green);
            SolidColorBrush NegativeDeltaColor = new SolidColorBrush(Colors.Red);

            Brush JumpInRange = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeInColour);
            Brush JumpInRangeMulti = new SolidColorBrush(Colors.Black);


            SolidColorBrush infoColourDelta = new SolidColorBrush(DataLargeColorDelta);

            SolidColorBrush zkbColour = new SolidColorBrush(MapConf.ActiveColourScheme.ZKillDataOverlay);

            SolidColorBrush infoLargeColour = new SolidColorBrush(DataLargeColor);
            SolidColorBrush infoVulnerable = new SolidColorBrush(MapConf.ActiveColourScheme.SOVStructureVunerableColour);
            SolidColorBrush infoVulnerableSoon = new SolidColorBrush(MapConf.ActiveColourScheme.SOVStructureVunerableSoonColour);


            BridgeInfoStackPanel.Children.Clear();
            if (!string.IsNullOrEmpty(currentJumpCharacter))
            {
                EVEData.System js = EM.GetEveSystem(currentCharacterJumpSystem);
                string text = "";
                if (MapConf.ShowCharacterNamesOnMap)
                {
                    text = $"{jumpShipType} range from {currentJumpCharacter} : {currentCharacterJumpSystem} ({js.Region})";
                }
                else
                {
                    text = $"{jumpShipType} range from {currentCharacterJumpSystem} ({js.Region})";
                }
                    
                    

                Label l = new Label();
                l.Content = text;
                l.FontSize = 14;
                l.FontWeight = FontWeights.Bold;
                l.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);

                BridgeInfoStackPanel.Children.Add(l);

            }
            foreach (string key in activeJumpSpheres.Keys)
            {

                EVEData.System js = EM.GetEveSystem(key);
                string text = $"{activeJumpSpheres[key]} range from {key} ({js.Region})";

                Label l = new Label();
                l.Content = text;
                l.FontSize = 14;
                l.FontWeight = FontWeights.Bold;
                l.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);

                BridgeInfoStackPanel.Children.Add(l);


            }



            foreach (EVEData.MapSystem sys in Region.MapSystems.Values.ToList())
            {
                infoColour = dataColor;
                long SystemAlliance = 0;

                if (MapConf.SOVBasedITCU)
                {
                    SystemAlliance = sys.ActualSystem.SOVAllianceTCU;
                }
                else
                {
                    SystemAlliance = sys.ActualSystem.SOVAllianceIHUB;
                }

                //
                Coalition SystemCoalition = null;
                if (SystemAlliance != 0)
                {
                    foreach (Coalition c in EM.Coalitions)
                    {
                        foreach (long l in c.MemberAlliances)
                        {
                            if (l == SystemAlliance)
                            {
                                SystemCoalition = c;
                                break;
                            }
                        }
                    }
                }

                int nPCKillsLastHour = sys.ActualSystem.NPCKillsLastHour;
                int podKillsLastHour = sys.ActualSystem.PodKillsLastHour;
                int shipKillsLastHour = sys.ActualSystem.ShipKillsLastHour;
                int jumpsLastHour = sys.ActualSystem.JumpsLastHour;

                int infoValue = -1;
                double infoSize = 0.0;

                if (ShowNPCKills)
                {
                    infoValue = nPCKillsLastHour;
                    infoSize = 0.15f * infoValue * ESIOverlayScale;

                    if (MapConf.ShowRattingDataAsDelta)
                    {
                        /*                        if(!MapConf.ShowNegativeRattingDelta)
                                                {
                                                    infoValue = Math.Max(0, sys.ActualSystem.NPCKillsDeltaLastHour);
                                                    infoSize = 0.15f * infoValue * ESIOverlayScale;
                                                }
                        */
                        if (MapConf.ShowNegativeRattingDelta)
                        {
                            infoValue = Math.Abs(sys.ActualSystem.NPCKillsDeltaLastHour);
                            infoSize = 0.15f * infoValue * ESIOverlayScale;

                            if (sys.ActualSystem.NPCKillsDeltaLastHour > 0)
                            {
                                infoColour = PositiveDeltaColor;
                            }
                            else
                            {
                                infoColour = NegativeDeltaColor;
                            }
                        }
                    }
                }

                if (ShowPodKills)
                {
                    infoValue = podKillsLastHour;
                    infoSize = 20.0f * infoValue * ESIOverlayScale;
                }

                if (ShowShipKills)
                {
                    infoValue = shipKillsLastHour;
                    infoSize = 20.0f * infoValue * ESIOverlayScale;
                }

                if (ShowShipJumps)
                {
                    infoValue = sys.ActualSystem.JumpsLastHour;
                    infoSize = infoValue * ESIOverlayScale;
                }

                if (ShowSystemTimers && MapConf.ShowIhubVunerabilities)
                {
                    DateTime now = DateTime.Now;

                    if (now > sys.ActualSystem.IHubVunerabliltyStart && now < sys.ActualSystem.IHubVunerabliltyEnd)
                    {
                        infoValue = (int)sys.ActualSystem.IHubOccupancyLevel;
                        infoSize = 30;
                        infoColour = infoVulnerable;
                    }
                    else if (now.AddMinutes(30) > sys.ActualSystem.IHubVunerabliltyStart)
                    {
                        infoValue = (int)sys.ActualSystem.IHubOccupancyLevel;
                        infoSize = 27;
                        infoColour = infoVulnerableSoon;
                    }
                    else
                    {
                        infoValue = -1;
                    }
                }

                if (ShowSystemTimers && MapConf.ShowTCUVunerabilities)
                {
                    DateTime now = DateTime.Now;

                    if (now > sys.ActualSystem.TCUVunerabliltyStart && now < sys.ActualSystem.TCUVunerabliltyEnd)
                    {
                        infoValue = (int)sys.ActualSystem.TCUOccupancyLevel;
                        infoSize = 30;
                        infoColour = infoVulnerable;
                    }
                    else if (now.AddMinutes(MapConf.UpcomingSovMinutes) > sys.ActualSystem.TCUVunerabliltyStart)
                    {
                        infoValue = (int)sys.ActualSystem.TCUOccupancyLevel;
                        infoSize = 27;
                        infoColour = infoVulnerableSoon;
                    }
                    else
                    {
                        infoValue = -1;
                    }
                }

                if (infoValue > 0)
                {
                    // clamp to a minimum
                    if (infoSize < 24)
                        infoSize = 24;

                    Shape infoCircle = new Ellipse() { Height = infoSize, Width = infoSize };
                    infoCircle.Fill = infoColour;

                    Canvas.SetZIndex(infoCircle, 10);
                    Canvas.SetLeft(infoCircle, sys.LayoutX - (infoSize / 2));
                    Canvas.SetTop(infoCircle, sys.LayoutY - (infoSize / 2));
                    MainCanvas.Children.Add(infoCircle);
                    DynamicMapElements.Add(infoCircle);
                }

                if (ShowNPCKills && MapConf.ShowRattingDataAsDelta && !MapConf.ShowNegativeRattingDelta)
                {
                    infoValue = Math.Max(0, sys.ActualSystem.NPCKillsDeltaLastHour);
                    infoSize = 0.15f * infoValue * ESIOverlayScale;

                    Shape infoCircle = new Ellipse() { Height = infoSize, Width = infoSize };
                    infoCircle.Fill = infoColourDelta;

                    Canvas.SetZIndex(infoCircle, 11);
                    Canvas.SetLeft(infoCircle, sys.LayoutX - (infoSize / 2));
                    Canvas.SetTop(infoCircle, sys.LayoutY - (infoSize / 2));
                    MainCanvas.Children.Add(infoCircle);
                    DynamicMapElements.Add(infoCircle);
                }

                if (infoSize > 60)
                {
                    Shape infoCircle = new Ellipse() { Height = 30, Width = 30 };
                    infoCircle.Fill = infoLargeColour;

                    Canvas.SetZIndex(infoCircle, 11);
                    Canvas.SetLeft(infoCircle, sys.LayoutX - (15));
                    Canvas.SetTop(infoCircle, sys.LayoutY - (15));
                    MainCanvas.Children.Add(infoCircle);
                    DynamicMapElements.Add(infoCircle);
                }

                if ((sys.ActualSystem.SOVAllianceTCU != 0 || sys.ActualSystem.SOVAllianceIHUB != 0) && ShowStandings)
                {
                    bool addToMap = true;
                    Brush br = null;

                    if (ActiveCharacter != null && ActiveCharacter.ESILinked)
                    {
                        float Standing = 0.0f;
                        float StandingTCU = 0.0f;
                        float StandingIHUB = 0.0f;

                        if (ActiveCharacter.AllianceID != 0 && ActiveCharacter.AllianceID == sys.ActualSystem.SOVAllianceTCU)
                        {
                            StandingTCU = 10.0f;
                        }
                        if (ActiveCharacter.AllianceID != 0 && ActiveCharacter.AllianceID == sys.ActualSystem.SOVAllianceIHUB)
                        {
                            StandingIHUB = 10.0f;
                        }

                        if (sys.ActualSystem.SOVCorp != 0 && ActiveCharacter.Standings.Keys.Contains(sys.ActualSystem.SOVCorp))
                        {
                            StandingTCU = ActiveCharacter.Standings[sys.ActualSystem.SOVCorp];
                            StandingIHUB = ActiveCharacter.Standings[sys.ActualSystem.SOVCorp];
                        }

                        if (sys.ActualSystem.SOVAllianceTCU != 0 && ActiveCharacter.Standings.Keys.Contains(sys.ActualSystem.SOVAllianceTCU))
                        {
                            StandingTCU = ActiveCharacter.Standings[sys.ActualSystem.SOVAllianceTCU];
                        }
                        if (sys.ActualSystem.SOVAllianceIHUB != 0 && ActiveCharacter.Standings.Keys.Contains(sys.ActualSystem.SOVAllianceIHUB))
                        {
                            StandingIHUB = ActiveCharacter.Standings[sys.ActualSystem.SOVAllianceIHUB];
                        }

                        if (MapConf.SOVBasedITCU)
                        {
                            Standing = StandingTCU;
                        }
                        else
                        {
                            Standing = StandingIHUB;
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

                        if (MapConf.SOVShowConflicts && sys.ActualSystem.SOVAllianceTCU != sys.ActualSystem.SOVAllianceIHUB)
                        {
                            addToMap = true;

                            Brush b1 = Brushes.Transparent;
                            Brush b2 = Brushes.Transparent;

                            switch (StandingTCU)
                            {
                                case -10.0f:
                                    b1 = StandingVBadBrush;
                                    break;

                                case -5.0f:
                                    b1 = StandingBadBrush;
                                    break;

                                case 5.0f:
                                    b1 = StandingGoodBrush;
                                    break;

                                case 10.0f:
                                    b1 = StandingVGoodBrush;
                                    break;
                            }

                            switch (StandingIHUB)
                            {
                                case -10.0f:
                                    b2 = StandingVBadBrush;
                                    break;

                                case -5.0f:
                                    b2 = StandingBadBrush;
                                    break;

                                case 5.0f:
                                    b2 = StandingGoodBrush;
                                    break;

                                case 10.0f:
                                    b2 = StandingVGoodBrush;
                                    break;
                            }

                            if (StandingIHUB < 0 && StandingTCU > 0 || StandingIHUB > 0 && StandingTCU < 0)
                            {
                                LinearGradientBrush lgb = new LinearGradientBrush();
                                lgb.StartPoint = new Point(0, 0);
                                lgb.EndPoint = new Point(1, 1);
                                lgb.GradientStops.Add(new GradientStop(Colors.Yellow, 0.0));
                                lgb.GradientStops.Add(new GradientStop(Colors.Red, 1));

                                br = lgb;
                            }
                            else
                            {
                                // Create a DrawingBrush
                                DrawingBrush myBrush = new DrawingBrush();
                                // Create a Geometry with white background
                                GeometryDrawing backgroundSquare = new GeometryDrawing(b1, null, new RectangleGeometry(new Rect(0, 0, 8, 8)));
                                // Create a GeometryGroup that will be added to Geometry
                                GeometryGroup gGroup = new GeometryGroup();
                                gGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 4, 4)));
                                gGroup.Children.Add(new RectangleGeometry(new Rect(4, 4, 4, 4)));
                                // Create a GeomertyDrawing
                                GeometryDrawing checkers = new GeometryDrawing(b2, null, gGroup);
                                DrawingGroup checkersDrawingGroup = new DrawingGroup();
                                checkersDrawingGroup.Children.Add(backgroundSquare);
                                checkersDrawingGroup.Children.Add(checkers);
                                myBrush.Drawing = checkersDrawingGroup;
                                // Set Viewport and TimeMode
                                myBrush.Viewport = new Rect(0, 0, 8, 8);
                                myBrush.Viewbox = new Rect(0, 0, 8, 8);
                                myBrush.TileMode = TileMode.Tile;
                                myBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                myBrush.ViewportUnits = BrushMappingMode.Absolute;

                                br = myBrush;
                            }
                        }
                    }
                    else
                    {
                        // enabled but not linked
                        addToMap = false;
                    }

                    if (addToMap)
                    {
                        Polygon poly = new Polygon();
                        poly.Fill = br;
                        //poly.SnapsToDevicePixels = true;
                        poly.Stroke = poly.Fill;
                        poly.StrokeThickness = 0.4;
                        poly.StrokeDashCap = PenLineCap.Round;
                        poly.StrokeLineJoin = PenLineJoin.Round;
                        poly.Stretch = Stretch.None;

                        foreach (Point p in sys.CellPoints)
                        {
                            poly.Points.Add(p);
                        }

                        MainCanvas.Children.Add(poly);

                        // save the dynamic map elements
                        DynamicMapElements.Add(poly);
                    }
                }

                if (SystemAlliance != 0 && MapConf.ShowCoalition && SystemCoalition != null && ShowSovOwner)
                {
                    Polygon poly = new Polygon();
                    poly.Fill = new SolidColorBrush(SystemCoalition.CoalitionColor);
                    poly.SnapsToDevicePixels = true;
                    poly.Stroke = poly.Fill;
                    poly.StrokeThickness = 0.4;
                    poly.StrokeDashCap = PenLineCap.Round;
                    poly.StrokeLineJoin = PenLineJoin.Round;

                    foreach (Point p in sys.CellPoints)
                    {
                        poly.Points.Add(p);
                    }

                    MainCanvas.Children.Add(poly);

                    // save the dynamic map elements
                    DynamicMapElements.Add(poly);
                }

                if (activeJumpSpheres.Count > 0 || currentJumpCharacter != null)
                {
                    bool AddHighlight = false;
                    bool DoubleHighlight = false;

                    // check character 
                    if (!string.IsNullOrEmpty(currentJumpCharacter))
                    {
                        double Distance = EM.GetRangeBetweenSystems(currentCharacterJumpSystem, sys.Name);
                        Distance = Distance / 9460730472580800.0;

                        double Max = 0.1f;

                        switch (jumpShipType)
                        {
                            case EVEData.EveManager.JumpShip.Super: { Max = 6.0; } break;
                            case EVEData.EveManager.JumpShip.Titan: { Max = 6.0; } break;
                            case EVEData.EveManager.JumpShip.Dread: { Max = 7.0; } break;
                            case EVEData.EveManager.JumpShip.Carrier: { Max = 7.0; } break;
                            case EVEData.EveManager.JumpShip.FAX: { Max = 7.0; } break;
                            case EVEData.EveManager.JumpShip.Blops: { Max = 8.0; } break;
                            case EVEData.EveManager.JumpShip.Rorqual: { Max = 10.0; } break;
                            case EVEData.EveManager.JumpShip.JF: { Max = 10.0; } break;
                        }

                        if (Distance < Max && Distance > 0.0 && sys.ActualSystem.TrueSec <= 0.45 && currentCharacterJumpSystem != sys.Name)
                        {
                            AddHighlight = true;
                        }
                    }

                    foreach (string key in activeJumpSpheres.Keys)
                    {
                        if(!string.IsNullOrEmpty(currentJumpCharacter) && key == currentCharacterJumpSystem)
                        {
                            continue;
                        }

                        double Distance = EM.GetRangeBetweenSystems(key, sys.Name);
                        Distance = Distance / 9460730472580800.0;

                        double Max = 0.1f;

                        switch (activeJumpSpheres[key])
                        {
                            case EVEData.EveManager.JumpShip.Super: { Max = 6.0; } break;
                            case EVEData.EveManager.JumpShip.Titan: { Max = 6.0; } break;
                            case EVEData.EveManager.JumpShip.Dread: { Max = 7.0; } break;
                            case EVEData.EveManager.JumpShip.Carrier: { Max = 7.0; } break;
                            case EVEData.EveManager.JumpShip.FAX: { Max = 7.0; } break;
                            case EVEData.EveManager.JumpShip.Blops: { Max = 8.0; } break;
                            case EVEData.EveManager.JumpShip.Rorqual: { Max = 10.0; } break;
                            case EVEData.EveManager.JumpShip.JF: { Max = 10.0; } break;
                        }

                        if (Distance < Max && Distance > 0.0 && sys.ActualSystem.TrueSec <= 0.45 && key != sys.Name)
                        {
                            if (AddHighlight)
                            {
                                DoubleHighlight = true;
                            }
                            AddHighlight = true;
                        }
                    }

                    if (AddHighlight)
                    {
                        Brush HighlightBrush = JumpInRange;
                        if (DoubleHighlight)
                        {
                            HighlightBrush = JumpInRangeMulti;
                        }


                        if (MapConf.JumpRangeInAsOutline)
                        {
                            Shape InRangeMarker;


                            if (sys.ActualSystem.HasNPCStation)
                            {
                                InRangeMarker = new Rectangle() { Height = SYSTEM_SHAPE_SIZE + 6, Width = SYSTEM_SHAPE_SIZE + 6 };
                            }
                            else
                            {
                                InRangeMarker = new Ellipse() { Height = SYSTEM_SHAPE_SIZE + 6, Width = SYSTEM_SHAPE_SIZE + 6 };
                            }

                            InRangeMarker.Stroke = HighlightBrush;
                            InRangeMarker.StrokeThickness = 6;
                            InRangeMarker.StrokeLineJoin = PenLineJoin.Round;
                            InRangeMarker.Fill = HighlightBrush;

                            Canvas.SetLeft(InRangeMarker, sys.LayoutX - (SYSTEM_SHAPE_SIZE + 6) / 2);
                            Canvas.SetTop(InRangeMarker, sys.LayoutY - (SYSTEM_SHAPE_SIZE + 6) / 2);
                            Canvas.SetZIndex(InRangeMarker, 19);

                            MainCanvas.Children.Add(InRangeMarker);
                            DynamicMapElements.Add(InRangeMarker);
                        }
                        else
                        {
                            Polygon poly = new Polygon();

                            foreach (Point p in sys.CellPoints)
                            {
                                poly.Points.Add(p);
                            }

                            poly.Fill = HighlightBrush;
                            poly.SnapsToDevicePixels = true;
                            poly.Stroke = poly.Fill;
                            poly.StrokeThickness = 3;
                            poly.StrokeDashCap = PenLineCap.Round;
                            poly.StrokeLineJoin = PenLineJoin.Round;
                            MainCanvas.Children.Add(poly);
                            DynamicMapElements.Add(poly);
                        }
                    }
                }

            }

            Dictionary<string, int> ZKBBaseFeed = new Dictionary<string, int>();
            {
                foreach (EVEData.ZKillRedisQ.ZKBDataSimple zs in EM.ZKillFeed.KillStream)
                {
                    if (ZKBBaseFeed.Keys.Contains(zs.SystemName))
                    {
                        ZKBBaseFeed[zs.SystemName]++;
                    }
                    else
                    {
                        ZKBBaseFeed[zs.SystemName] = 1;
                    }
                }

                foreach (KeyValuePair<string, EVEData.MapSystem> kvp in Region.MapSystems)
                {
                    EVEData.MapSystem sys = kvp.Value;

                    if (ZKBBaseFeed.Keys.Contains(sys.ActualSystem.Name))
                    {
                        double ZKBValue = 24 + ((double)ZKBBaseFeed[sys.ActualSystem.Name] * ESIOverlayScale * 2);

                        Shape infoCircle = new Ellipse() { Height = ZKBValue, Width = ZKBValue };
                        infoCircle.Fill = zkbColour;

                        Canvas.SetZIndex(infoCircle, 11);
                        Canvas.SetLeft(infoCircle, sys.LayoutX - (ZKBValue / 2));
                        Canvas.SetTop(infoCircle, sys.LayoutY - (ZKBValue / 2));
                        MainCanvas.Children.Add(infoCircle);
                        DynamicMapElements.Add(infoCircle);
                    }
                }
            }
        }

        private void AddHighlightToSystem(string name)
        {
            if (!Region.MapSystems.Keys.Contains(name))
            {
                return;
            }

            EVEData.MapSystem selectedSys = Region.MapSystems[name];
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
                Timeline.SetDesiredFrameRate(da, 20);

                try
                {
                    RotateTransform eTransform = (RotateTransform)highlightSystemCircle.RenderTransform;
                    eTransform.BeginAnimation(RotateTransform.AngleProperty, da);
                }
                catch
                {
                }
            }
        }

        private void AddRouteToMap()
        {
            if (ActiveCharacter == null)
                return;

            Brush RouteBrush = new SolidColorBrush(Colors.Yellow);
            Brush RouteAnsiblexBrush = new SolidColorBrush(Colors.DarkMagenta);


            // no active route
            if (ActiveCharacter.ActiveRoute.Count == 0)
            {
                return;
            }

            string Start = "";
            string End = ActiveCharacter.Location;

            try
            {
                for (int i = 1; i < ActiveCharacter.ActiveRoute.Count; i++)
                {
                    Start = End;
                    End = ActiveCharacter.ActiveRoute[i].SystemName;

                    if (!(Region.IsSystemOnMap(Start) && Region.IsSystemOnMap(End)))
                    {
                        continue;
                    }

                    EVEData.MapSystem from = Region.MapSystems[Start];
                    EVEData.MapSystem to = Region.MapSystems[End];

                    Line routeLine = new Line();

                    routeLine.X1 = from.LayoutX;
                    routeLine.Y1 = from.LayoutY;

                    routeLine.X2 = to.LayoutX;
                    routeLine.Y2 = to.LayoutY;

                    routeLine.StrokeThickness = 5;
                    routeLine.Visibility = Visibility.Visible;
                    if (ActiveCharacter.ActiveRoute[i - 1].GateToTake == Navigation.GateType.Ansibex)
                    {
                        routeLine.Stroke = RouteAnsiblexBrush;
                    }
                    else
                    {
                        routeLine.Stroke = RouteBrush;
                    }

                    DoubleCollection dashes = new DoubleCollection();
                    dashes.Add(1.0);
                    dashes.Add(1.0);

                    routeLine.StrokeDashArray = dashes;

                    // animate the jump bridges
                    DoubleAnimation da = new DoubleAnimation();
                    da.From = 200;
                    da.To = 0;
                    da.By = 2;
                    da.Duration = new Duration(TimeSpan.FromSeconds(40));
                    da.RepeatBehavior = RepeatBehavior.Forever;
                    Timeline.SetDesiredFrameRate(da, 20);

                    routeLine.StrokeDashArray = dashes;

                    if (!MapConf.DisableRoutePathAnimation)
                    {
                        routeLine.BeginAnimation(Shape.StrokeDashOffsetProperty, da);
                    }

                    Canvas.SetZIndex(routeLine, 18);
                    MainCanvas.Children.Add(routeLine);

                    DynamicMapElements.Add(routeLine);
                }
            }
            catch
            {
            }
        }

        private void AddSystemIntelOverlay()
        {
            Brush intelBlobBrush = new SolidColorBrush(MapConf.ActiveColourScheme.IntelOverlayColour);
            Brush intelClearBlobBrush = new SolidColorBrush(MapConf.ActiveColourScheme.IntelClearOverlayColour);

            foreach (EVEData.IntelData id in EM.IntelDataList)
            {
                foreach (string sysStr in id.Systems)
                {
                    if (Region.IsSystemOnMap(sysStr))
                    {
                        EVEData.MapSystem sys = Region.MapSystems[sysStr];

                        double radiusScale = (DateTime.Now - id.IntelTime).TotalSeconds / (double)MapConf.MaxIntelSeconds;

                        if (radiusScale < 0.0 || radiusScale >= 1.0)
                        {
                            continue;
                        }

                        // add circle to the map
                        double radius = 24 + (100 * (1.0 - radiusScale));
                        double circleOffset = radius / 2;

                        Shape intelShape = new Ellipse() { Height = radius, Width = radius };
                        if (id.ClearNotification)
                        {
                            intelShape.Fill = intelClearBlobBrush;
                        }
                        else
                        {
                            intelShape.Fill = intelBlobBrush;
                        }

                        Canvas.SetLeft(intelShape, sys.LayoutX - circleOffset);
                        Canvas.SetTop(intelShape, sys.LayoutY - circleOffset);
                        Canvas.SetZIndex(intelShape, 15);
                        MainCanvas.Children.Add(intelShape);

                        DynamicMapElements.Add(intelShape);
                    }
                }
            }
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

            Brush SysInRegionDarkBrush = new SolidColorBrush(DarkenColour(MapConf.ActiveColourScheme.InRegionSystemColour));
            Brush SysOutRegionDarkBrush = new SolidColorBrush(DarkenColour(MapConf.ActiveColourScheme.OutRegionSystemColour));

            Brush HasIceBrush = new SolidColorBrush(Colors.LightBlue);

            Brush SysInRegionTextBrush = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);
            Brush SysOutRegionTextBrush = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);

            Brush FriendlyJumpBridgeBrush = new SolidColorBrush(MapConf.ActiveColourScheme.FriendlyJumpBridgeColour);
            Brush DisabledJumpBridgeBrush = new SolidColorBrush(MapConf.ActiveColourScheme.DisabledJumpBridgeColour);

            Brush JumpInRange = new SolidColorBrush(MapConf.ActiveColourScheme.JumpRangeInColour);
            Brush JumpInRangeMulti = new SolidColorBrush(Colors.Black);

            Brush Incursion = new SolidColorBrush(MapConf.ActiveColourScheme.ActiveIncursionColour);

            Brush ConstellationHighlight = new SolidColorBrush(MapConf.ActiveColourScheme.ConstellationHighlightColour);

            Brush DarkTextColourBrush = new SolidColorBrush(Colors.Black);

            Color bgtc = MapConf.ActiveColourScheme.MapBackgroundColour;
            bgtc.A = 192;
            Brush SysTextBackgroundBrush = new SolidColorBrush(bgtc);

            Color bgd = MapConf.ActiveColourScheme.MapBackgroundColour;

            float darkenFactor = 0.9f;

            bgd.R = (byte)(darkenFactor * bgd.R);
            bgd.G = (byte)(darkenFactor * bgd.G);
            bgd.B = (byte)(darkenFactor * bgd.B);

            Brush MapBackgroundBrushDarkend = new SolidColorBrush(bgd);

            List<long> AlliancesKeyList = new List<long>();

            Brush NormalGateBrush = new SolidColorBrush(MapConf.ActiveColourScheme.NormalGateColour);
            Brush ConstellationGateBrush = new SolidColorBrush(MapConf.ActiveColourScheme.ConstellationGateColour);

            // cache all system links
            List<GateHelper> systemLinks = new List<GateHelper>();

            Random rnd = new Random(4);


  

            foreach (KeyValuePair<string, EVEData.MapSystem> kvp in Region.MapSystems)
            {
                EVEData.MapSystem system = kvp.Value;

                Coalition SystemCoalition = null;


                double trueSecVal = system.ActualSystem.TrueSec;
                bool gradeTruesec = MapConf.ShowTrueSec;
                if (MapConf.ShowSimpleSecurityView)
                {
                    // gradeTruesec = false;
                    if (system.ActualSystem.TrueSec >= 0.45)
                    {
                        trueSecVal = 1.0;
                    }
                    else if (system.ActualSystem.TrueSec > 0.0)
                    {
                        trueSecVal = 0.4;
                    }
                    else
                    {
                        trueSecVal = 0.0;
                    }
                }

                Brush securityColorFill = new SolidColorBrush(MapColours.GetSecStatusColour(trueSecVal, gradeTruesec));

                if (MapConf.SOVBasedITCU)
                {
                    if (system.ActualSystem.SOVAllianceTCU != 0)
                    {
                        foreach (Coalition c in EM.Coalitions)
                        {
                            foreach (long l in c.MemberAlliances)
                            {
                                if (l == system.ActualSystem.SOVAllianceTCU)
                                {
                                    SystemCoalition = c;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (system.ActualSystem.SOVAllianceIHUB != 0)
                    {
                        foreach (Coalition c in EM.Coalitions)
                        {
                            foreach (long l in c.MemberAlliances)
                            {
                                if (l == system.ActualSystem.SOVAllianceIHUB)
                                {
                                    SystemCoalition = c;
                                    break;
                                }
                            }
                        }
                    }
                }

                string SystemSubText = string.Empty;

                // add circle for system
                Polygon systemShape = new Polygon();
                systemShape.StrokeThickness = 1.5;

                bool needsOutline = true;
                bool drawKeep = false;
                bool drawFort = false;
                bool drawAstra = false;
                bool drawNPCStation = system.ActualSystem.HasNPCStation;

                foreach (StructureHunter.Structures sh in system.ActualSystem.SHStructures)
                {
                    switch (sh.TypeId)
                    {
                        case 35834: // Keepstar
                            drawKeep = true;
                            break;

                        case 35833: // fortizar
                        case 47512: // faction fortizar
                        case 47513: // faction fortizar
                        case 47514: // faction fortizar
                        case 47515: // faction fortizar
                        case 47516: // faction fortizar
                        case 35827: // Sotiyo
                            drawFort = true;
                            break;

                        default:
                            drawAstra = true;
                            break;
                    }
                }

                if (ActiveCharacter != null && ActiveCharacter.ESILinked && ActiveCharacter.DockableStructures != null && ActiveCharacter.DockableStructures.Keys.Contains(system.Name))
                {
                    foreach (StructureIDs.StructureIdData sid in ActiveCharacter.DockableStructures[system.Name])
                    {
                        switch (sid.TypeId)
                        {
                            case 35834: // Keepstar
                                drawKeep = true;
                                break;

                            case 35833: // fortizar
                            case 47512: // faction fortizar
                            case 47513: // faction fortizar
                            case 47514: // faction fortizar
                            case 47515: // faction fortizar
                            case 47516: // faction fortizar
                            case 35827: // Sotiyo
                                drawFort = true;
                                break;

                            default:
                                drawAstra = true;
                                break;
                        }
                    }
                }

                if (drawKeep)
                {
                    drawFort = false;
                    drawAstra = false;
                    drawNPCStation = false;

                    needsOutline = false;
                }

                if (drawFort)
                {
                    drawAstra = false;
                    drawNPCStation = false;
                }

                if (drawNPCStation)
                {
                    drawAstra = false;
                }

                List<Point> shapePoints = null;

                if (drawKeep)
                {
                    shapePoints = SystemIcon_Keepstar;
                    systemShape.StrokeThickness = 1.5;
                }

                if (drawFort)
                {
                    shapePoints = SystemIcon_Fortizar;
                    systemShape.StrokeThickness = 1;
                }
                if (drawAstra)
                {
                    shapePoints = SystemIcon_Astrahaus;
                    systemShape.StrokeThickness = 1;
                }

                if (drawNPCStation)
                {
                    shapePoints = SystemIcon_NPCStation;
                    systemShape.StrokeThickness = 1.5;
                    needsOutline = false;
                }

                // override
                if (ShowSystemADM)
                {
                    shapePoints = null;
                    needsOutline = true;
                }

                if (shapePoints != null)
                {
                    foreach (Point p in shapePoints)
                    {
                        systemShape.Points.Add(p);
                    }

                    systemShape.Stroke = SysOutlineBrush;
                    systemShape.StrokeLineJoin = PenLineJoin.Round;

                    if (system.OutOfRegion)
                    {
                        systemShape.Fill = SysOutRegionDarkBrush;
                    }
                    else
                    {
                        systemShape.Fill = SysInRegionDarkBrush;
                    }

                    if (system.ActualSystem.HasIceBelt)
                    {
                        systemShape.Fill = HasIceBrush;
                    }

                    // override with sec status colours
                    if (ShowSystemSecurity)
                    {
                        systemShape.Fill = securityColorFill;
                    }

                    if (!needsOutline)
                    {
                        // add the hover over and click handlers
                        systemShape.DataContext = system;
                        systemShape.MouseDown += ShapeMouseDownHandler;
                        systemShape.MouseEnter += ShapeMouseOverHandler;
                        systemShape.MouseLeave += ShapeMouseOverHandler;
                    }
                    else
                    {
                        systemShape.IsHitTestVisible = false;
                    }

                    Canvas.SetLeft(systemShape, system.LayoutX - SYSTEM_SHAPE_OFFSET + 1);
                    Canvas.SetTop(systemShape, system.LayoutY - SYSTEM_SHAPE_OFFSET + 1);
                    Canvas.SetZIndex(systemShape, SYSTEM_Z_INDEX);
                    MainCanvas.Children.Add(systemShape);
                }

                if (needsOutline)
                {
                    Shape SystemOutline = new Ellipse { Width = SYSTEM_SHAPE_SIZE, Height = SYSTEM_SHAPE_SIZE };
                    SystemOutline.Stroke = SysOutlineBrush;
                    SystemOutline.StrokeThickness = 1.5;
                    SystemOutline.StrokeLineJoin = PenLineJoin.Round;

                    if (system.OutOfRegion)
                    {
                        SystemOutline.Fill = SysOutRegionBrush;
                    }
                    else
                    {
                        SystemOutline.Fill = SysInRegionBrush;
                    }

                    if (system.ActualSystem.HasIceBelt)
                    {
                        SystemOutline.Fill = HasIceBrush;
                    }

                    // override with sec status colours
                    if (ShowSystemSecurity)
                    {
                        SystemOutline.Fill = securityColorFill;
                    }

                    if (ShowSystemADM && system.ActualSystem.IHubOccupancyLevel != 0.0f)
                    {
                        float SovVal = system.ActualSystem.IHubOccupancyLevel;

                        float Blend = 1.0f - ((SovVal - 1.0f) / 5.0f);
                        byte r, g;

                        if (Blend < 0.5)
                        {
                            r = 255;
                            g = (byte)(255 * Blend / 0.5);
                        }
                        else
                        {
                            g = 255;
                            r = (byte)(255 - (255 * (Blend - 0.5) / 0.5));
                        }

                        SystemOutline.Fill = new SolidColorBrush(Color.FromRgb(r, g, 0));
                    }

                    SystemOutline.DataContext = system;
                    SystemOutline.MouseDown += ShapeMouseDownHandler;
                    SystemOutline.MouseEnter += ShapeMouseOverHandler;
                    SystemOutline.MouseLeave += ShapeMouseOverHandler;

                    Canvas.SetLeft(SystemOutline, system.LayoutX - SYSTEM_SHAPE_OFFSET);
                    Canvas.SetTop(SystemOutline, system.LayoutY - SYSTEM_SHAPE_OFFSET);
                    Canvas.SetZIndex(SystemOutline, SYSTEM_Z_INDEX - 1);
                    MainCanvas.Children.Add(SystemOutline);
                }

                if (ShowSystemADM && system.ActualSystem.IHubOccupancyLevel != 0.0 && !ShowSystemTimers)
                {
                    Label sovADM = new Label();
                    sovADM.Content = "1.0";
                    sovADM.FontSize = 9;
                    sovADM.IsHitTestVisible = false;
                    sovADM.Content = $"{system.ActualSystem.IHubOccupancyLevel:f1}";
                    sovADM.HorizontalContentAlignment = HorizontalAlignment.Center;
                    sovADM.VerticalContentAlignment = VerticalAlignment.Center;
                    sovADM.Width = SYSTEM_SHAPE_SIZE+2;
                    sovADM.Height = SYSTEM_SHAPE_SIZE+2;
                    sovADM.Foreground = DarkTextColourBrush;


                    Canvas.SetLeft(sovADM, system.LayoutX - (SYSTEM_SHAPE_OFFSET+1));
                    Canvas.SetTop(sovADM, system.LayoutY - (SYSTEM_SHAPE_OFFSET+1));
                    Canvas.SetZIndex(sovADM, SYSTEM_Z_INDEX - 1);
                    MainCanvas.Children.Add(sovADM);
                }

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
                sysText.IsHitTestVisible = false;

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

                double regionMarkerOffset = SYSTEM_REGION_TEXT_Y_OFFSET;

                if (MapConf.ShowActiveIncursions && system.ActualSystem.ActiveIncursion)
                {
                    {
                        Polygon poly = new Polygon();

                        foreach (Point p in system.CellPoints)
                        {
                            poly.Points.Add(p);
                        }

                        //poly.Fill
                        poly.Fill = Incursion;
                        poly.SnapsToDevicePixels = true;
                        poly.Stroke = poly.Fill;
                        poly.StrokeThickness = 3;
                        poly.StrokeDashCap = PenLineCap.Round;
                        poly.StrokeLineJoin = PenLineJoin.Round;
                        MainCanvas.Children.Add(poly);
                    }
                }

                if (MapConf.ShowCynoBeacons && system.ActualSystem.HasJumpBeacon)
                {
                    Shape CynoBeaconLogo = new Ellipse { Width = 8, Height = 8 };
                    CynoBeaconLogo.Stroke = SysOutlineBrush;
                    CynoBeaconLogo.StrokeThickness = 1.0;
                    CynoBeaconLogo.StrokeLineJoin = PenLineJoin.Round;
                    CynoBeaconLogo.Fill = new SolidColorBrush(Colors.OrangeRed);

                    Canvas.SetLeft(CynoBeaconLogo, system.LayoutX + 7);
                    Canvas.SetTop(CynoBeaconLogo, system.LayoutY - 12);
                    Canvas.SetZIndex(CynoBeaconLogo, SYSTEM_Z_INDEX + 5);
                    MainCanvas.Children.Add(CynoBeaconLogo);
                }

                if (MapConf.ShowJoveObservatories && system.ActualSystem.HasJoveObservatory && !ShowSystemADM && !ShowSystemTimers)
                {
                    Image JoveLogo = new Image
                    {
                        Width = 10,
                        Height = 9,
                        Name = "JoveLogo",
                        Source = ResourceLoader.LoadBitmapFromResource("Images/Jove_logo.png"),
                        Stretch = Stretch.Uniform,
                        IsHitTestVisible = false,
                    };

                    RenderOptions.SetBitmapScalingMode(JoveLogo, BitmapScalingMode.NearestNeighbor);

                    Canvas.SetLeft(JoveLogo, system.LayoutX - SYSTEM_SHAPE_OFFSET + 5);
                    Canvas.SetTop(JoveLogo, system.LayoutY - SYSTEM_SHAPE_OFFSET + 6);
                    Canvas.SetZIndex(JoveLogo, SYSTEM_Z_INDEX + 5);
                    MainCanvas.Children.Add(JoveLogo);
                }

                EVEData.System es = EM.GetEveSystem(SelectedSystem);

                if (es != null && (ShowSystemTimers && (MapConf.ShowIhubVunerabilities || MapConf.ShowTCUVunerabilities)) && system.ActualSystem.ConstellationID == es.ConstellationID)
                {
                    {
                        Polygon poly = new Polygon();

                        foreach (Point p in system.CellPoints)
                        {
                            poly.Points.Add(p);
                        }

                        //poly.Fill
                        poly.Fill = ConstellationHighlight;
                        poly.SnapsToDevicePixels = true;
                        poly.Stroke = poly.Fill;
                        poly.StrokeThickness = 3;
                        poly.StrokeDashCap = PenLineCap.Round;
                        poly.StrokeLineJoin = PenLineJoin.Round;
                        MainCanvas.Children.Add(poly);
                    }
                }

                long SystemAlliance = 0;

                if (MapConf.SOVBasedITCU)
                {
                    SystemAlliance = system.ActualSystem.SOVAllianceTCU;
                }
                else
                {
                    SystemAlliance = system.ActualSystem.SOVAllianceIHUB;
                }

                if (ShowSovOwner && SelectedAlliance != 0 && SystemAlliance == SelectedAlliance)
                {
                    Polygon poly = new Polygon();

                    foreach (Point p in system.CellPoints)
                    {
                        poly.Points.Add(p);
                    }

                    poly.Fill = SelectedAllianceBrush;
                    poly.SnapsToDevicePixels = true;
                    poly.Stroke = poly.Fill;
                    poly.StrokeThickness = 1;
                    poly.StrokeDashCap = PenLineCap.Round;
                    poly.StrokeLineJoin = PenLineJoin.Round;
                    Canvas.SetZIndex(poly, SYSTEM_Z_INDEX - 2);
                    MainCanvas.Children.Add(poly);
                }

                if ((ShowSovOwner) && SystemAlliance != 0 && EM.AllianceIDToName.Keys.Contains(SystemAlliance))
                {
                    Label sysRegionText = new Label();

                    string allianceName = EM.GetAllianceName(SystemAlliance);
                    string allianceTicker = EM.GetAllianceTicker(SystemAlliance);
                    string coalitionName = string.Empty;

                    string content = allianceTicker;

                    if (MapConf.ShowCoalition && SystemCoalition != null)
                    {
                        content = SystemCoalition.Name + " (" + allianceTicker + ")";
                    }

                    if (SystemSubText != string.Empty)
                    {
                        SystemSubText += "\n";
                    }
                    SystemSubText += content;

                    if (!AlliancesKeyList.Contains(SystemAlliance))
                    {
                        AlliancesKeyList.Add(SystemAlliance);
                    }

                    /*

                    sysRegionText.Content = content;
                    sysRegionText.FontSize = SYSTEM_TEXT_TEXT_SIZE;
                    sysText.FontSize = MapConf.ActiveColourScheme.SystemTextSize;

                    Canvas.SetLeft(sysRegionText, system.LayoutX + SYSTEM_REGION_TEXT_X_OFFSET);
                    Canvas.SetTop (sysRegionText, system.LayoutY + SYSTEM_REGION_TEXT_Y_OFFSET + 9);
                    Canvas.SetZIndex(sysRegionText, SYSTEM_Z_INDEX);

                    MainCanvas.Children.Add(sysRegionText);

                    regionMarkerOffset += SYSTEM_TEXT_TEXT_SIZE;

                    if(!AlliancesKeyList.Contains(system.ActualSystem.SOVAlliance))
                    {
                        AlliancesKeyList.Add(system.ActualSystem.SOVAlliance);
                    }
                    */
                }
 
                if (system.OutOfRegion)
                {
                    /*
                    Label sysRegionText = new Label();
                    sysRegionText.Content = "(" + system.Region + ")";
                    sysRegionText.FontSize = SYSTEM_TEXT_TEXT_SIZE;
                    sysRegionText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.OutRegionSystemTextColour);

                    Canvas.SetLeft(sysRegionText, system.LayoutX + SYSTEM_REGION_TEXT_X_OFFSET);
                    Canvas.SetTop(sysRegionText, system.LayoutY + regionMarkerOffset);
                    Canvas.SetZIndex(sysRegionText, SYSTEM_Z_INDEX);

                    MainCanvas.Children.Add(sysRegionText);

                    */

                    if (SystemSubText != string.Empty)
                    {
                        SystemSubText += "\n";
                    }
                    SystemSubText += "(" + system.Region + ")";

                    Polygon poly = new Polygon();

                    foreach (Point p in system.CellPoints)
                    {
                        poly.Points.Add(p);
                    }

                    //poly.Fill
                    poly.Fill = MapBackgroundBrushDarkend;
                    poly.SnapsToDevicePixels = true;
                    poly.Stroke = MapBackgroundBrushDarkend;
                    poly.StrokeThickness = 3;
                    poly.StrokeDashCap = PenLineCap.Round;
                    poly.StrokeLineJoin = PenLineJoin.Round;
                    MainCanvas.Children.Add(poly);
                }

                if (SystemSubText != string.Empty)
                {
                    Label sysSubText = new Label();
                    sysSubText.Content = SystemSubText;
                    sysSubText.FontSize = SYSTEM_TEXT_TEXT_SIZE;
                    sysSubText.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemTextColour);
                    sysSubText.IsHitTestVisible = false;

                    Canvas.SetLeft(sysSubText, system.LayoutX + SYSTEM_REGION_TEXT_X_OFFSET);
                    Canvas.SetTop(sysSubText, system.LayoutY + regionMarkerOffset);
                    Canvas.SetZIndex(sysSubText, SYSTEM_Z_INDEX);

                    MainCanvas.Children.Add(sysSubText);
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

            if (ShowJumpBridges && EM.JumpBridges != null)
            {
                foreach (EVEData.JumpBridge jb in EM.JumpBridges)
                {
                    if (Region.IsSystemOnMap(jb.From) || Region.IsSystemOnMap(jb.To))
                    {
                        EVEData.MapSystem from;

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

                        if (!Region.IsSystemOnMap(jb.To) || !Region.IsSystemOnMap(jb.From))
                        {
                            endPoint = new Point(from.LayoutX - 20, from.LayoutY - 40);

                            Shape jbOutofSystemBlob = new Ellipse() { Height = 6, Width = 6 };
                            Canvas.SetLeft(jbOutofSystemBlob, endPoint.X - 3);
                            Canvas.SetTop(jbOutofSystemBlob, endPoint.Y - 3);
                            Canvas.SetZIndex(jbOutofSystemBlob, 19);

                            MainCanvas.Children.Add(jbOutofSystemBlob);

                            if (jb.Disabled)
                            {
                                jbOutofSystemBlob.Stroke = DisabledJumpBridgeBrush;
                            }
                            else
                            {
                                jbOutofSystemBlob.Stroke = FriendlyJumpBridgeBrush;
                            }
                            jbOutofSystemBlob.Fill = jbOutofSystemBlob.Stroke;
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

                        path.StrokeThickness = 2;

                        DoubleCollection dashes = new DoubleCollection();

                        if (!jb.Disabled)
                        {
                            dashes.Add(1.0);
                            dashes.Add(1.0);
                            path.Stroke = FriendlyJumpBridgeBrush;

                        }
                        else
                        {
                            dashes.Add(1.0);
                            dashes.Add(3.0);
                            path.Stroke = DisabledJumpBridgeBrush;
                        }

                        path.StrokeDashArray = dashes;

                        // animate the jump bridges
                        DoubleAnimation da = new DoubleAnimation();
                        da.From = 0;
                        da.To = 200;
                        da.By = 2;
                        da.Duration = new Duration(TimeSpan.FromSeconds(100));
                        da.RepeatBehavior = RepeatBehavior.Forever;
                        Timeline.SetDesiredFrameRate(da, 20);
                        // Storyboard.SetTargetProperty(path, new PropertyPath(Shape.StrokeDashOffsetProperty));
                        // Storyboard.SetTargetName()
                        // Storyboard sb = new Storyboard();
                        // sb.Children.Add(da);

                        path.StrokeDashArray = dashes;

                        if (!MapConf.DisableJumpBridgesPathAnimation)
                        {
                            path.BeginAnimation(Shape.StrokeDashOffsetProperty, da);
                        }
                        
                        // path.BeginStoryboard(sb);

                        Canvas.SetZIndex(path, 19);

                        MainCanvas.Children.Add(path);
                    }
                }
            }

            if (AlliancesKeyList.Count > 0)
            {
                AllianceNameList.Visibility = Visibility.Visible;
                AllianceNameListStackPanel.Children.Clear();

                Brush fontColour = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF767576"));
                Brush SelectedFont = new SolidColorBrush(Colors.White);

                /*
                List<string> allianceKeyItems = new List<string>

                foreach (long allianceID in AlliancesKeyList)
                {
                    string allianceName = EM.GetAllianceName(allianceID);
                    string allianceTicker = EM.GetAllianceTicker(allianceID);
                    allianceKeyItems.Add($"{allianceTicker}\t{allianceName}")
                }

                allianceKeyItems.Sort();

                */

                List<Label> AllianceNameListLabels = new List<Label>();

                Thickness p = new Thickness(3);


                foreach (long allianceID in AlliancesKeyList)
                {
                    string allianceName = EM.GetAllianceName(allianceID);
                    string allianceTicker = EM.GetAllianceTicker(allianceID);

                    Label akl = new Label();
                    akl.MouseDown += AllianceKeyList_MouseDown;
                    akl.DataContext = allianceID.ToString();
                    akl.Content = $"{allianceTicker}\t{allianceName}";
                    akl.Foreground = fontColour;
                    akl.Margin = p;

                    if (allianceID == SelectedAlliance)
                    {
                        akl.Foreground = SelectedFont;
                    }

                    AllianceNameListLabels.Add(akl);
                }

                List<Label> SortedAlliance = AllianceNameListLabels.OrderBy(an => an.Content).ToList();

                foreach (Label l in SortedAlliance)
                {
                    AllianceNameListStackPanel.Children.Add(l);
                }
            }
            else
            {
                AllianceNameList.Visibility = Visibility.Hidden;
            }
        }

        private void AllianceKeyList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label obj = sender as Label;
            string AllianceIDStr = obj.DataContext as string;
            long AllianceID = long.Parse(AllianceIDStr);

            if (e.ClickCount == 2)
            {
                string AURL = $"https://zkillboard.com/region/{Region.ID}/alliance/{AllianceID}";
                System.Diagnostics.Process.Start(AURL);
            }
            else
            {
                if (SelectedAlliance == AllianceID)
                {
                    SelectedAlliance = 0;
                }
                else
                {
                    SelectedAlliance = AllianceID;
                }
                ReDrawMap(true);
            }
        }

        private Color DarkenColour(Color inCol)
        {
            Color Dark = inCol;
            Dark.R = (Byte)(0.8 * Dark.R);
            Dark.G = (Byte)(0.8 * Dark.G);
            Dark.B = (Byte)(0.8 * Dark.B);
            return Dark;
        }

        private void FollowCharacterChk_Checked(object sender, RoutedEventArgs e)
        {
            UpdateActiveCharacter();
        }

        private void GlobalSystemDropDownAC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FollowCharacter = false;

            EVEData.System sd = GlobalSystemDropDownAC.SelectedItem as EVEData.System;

            if (sd != null)
            {
                bool ChangeRegion = sd.Region != Region.Name;
                SelectSystem(sd.Name, ChangeRegion);
                ReDrawMap(ChangeRegion);
            }
        }

        public void UpdateActiveCharacter(EVEData.LocalCharacter c = null )
        {
            if (ActiveCharacter != c && c !=null)
            {
                ActiveCharacter = c;
            }

            if (ActiveCharacter != null && FollowCharacter)
            {
                EVEData.System s = EM.GetEveSystem(ActiveCharacter.Location);
                if (s != null)
                {
                    if (s.Region != Region.Name)
                    {
                        // change region
                        SelectRegion(s.Region);
                    }

                    SelectSystem(ActiveCharacter.Location);

                    // force the follow as this will be reset by the region change
                    FollowCharacter = true;
                }
            }
        }

        private void MapObjectChanged(object sender, PropertyChangedEventArgs e)
        {
            ReDrawMap(true);
        }

        /// <summary>
        /// Region Selection Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegionSelectCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FollowCharacter = false;

            EVEData.MapRegion rd = RegionSelectCB.SelectedItem as EVEData.MapRegion;
            if (rd == null)
            {
                return;
            }

            SelectRegion(rd.Name);
        }

        private void SetJumpRange_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                EveManager.JumpShip js = EveManager.JumpShip.Super;


                if (mi.DataContext as string == "6")
                {
                    js = EveManager.JumpShip.Super;
                }
                if (mi.DataContext as string == "7")
                {
                    js = EveManager.JumpShip.Carrier;
                }

                if (mi.DataContext as string == "8")
                {
                    js = EveManager.JumpShip.Blops;
                }

                if (mi.DataContext as string == "10")
                {
                    js = EveManager.JumpShip.JF;
                }

                activeJumpSpheres[eveSys.Name] = js;


                if (mi.DataContext as string == "0")
                {
                    if(activeJumpSpheres.Keys.Contains(eveSys.Name))
                    {
                        activeJumpSpheres.Remove(eveSys.Name);
                    }
                }

                if (mi.DataContext as string == "-1")
                {
                    activeJumpSpheres.Clear();
                    currentJumpCharacter = "";
                    currentCharacterJumpSystem = "";
                }

                if(!string.IsNullOrEmpty(currentJumpCharacter))
                {
                    showJumpDistance = true;
                }
                else
                {
                    showJumpDistance = activeJumpSpheres.Count > 0;
                }

                ReDrawMap(true);
            }
        }

        /// <summary>
        /// Shape (ie System) MouseDown handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShapeMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            Shape obj = sender as Shape;

            EVEData.MapSystem selectedSys = obj.DataContext as EVEData.MapSystem;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    bool redraw = false;
                    if (showJumpDistance || (ShowSystemTimers && (MapConf.ShowIhubVunerabilities || MapConf.ShowTCUVunerabilities)))
                    {
                        redraw = true;
                    }
                    FollowCharacter = false;
                    SelectSystem(selectedSys.Name);
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

                MenuItem characters = cm.Items[4] as MenuItem;
                characters.Items.Clear();

                setDesto.IsEnabled = false;
                addWaypoint.IsEnabled = false;

                characters.IsEnabled = false;
                characters.Visibility = Visibility.Collapsed;

                if (ActiveCharacter != null && ActiveCharacter.ESILinked)
                {
                    setDesto.IsEnabled = true;
                    addWaypoint.IsEnabled = true;
                }

                // get a list of characters in this system
                List<Character> charactersInSystem = new List<Character>();
                foreach (LocalCharacter lc in EM.LocalCharacters)
                {
                    if (lc.Location == selectedSys.Name)
                    {
                        charactersInSystem.Add(lc);
                    }
                }

                if (charactersInSystem.Count > 0)
                {
                    characters.IsEnabled = true;
                    characters.Visibility = Visibility.Visible;

                    foreach (Character lc in charactersInSystem)
                    {
                        MenuItem miChar = new MenuItem();
                        miChar.Header = lc.Name;
                        characters.Items.Add(miChar);


                        // now create the child menu's
                        MenuItem miAutoRange = new MenuItem();
                        miAutoRange.Header = "Auto Jump Range";
                        miAutoRange.DataContext = lc;
                        miChar.Items.Add(miAutoRange);


                        MenuItem miARNone = new MenuItem();
                        miARNone.Header = "None";
                        miARNone.DataContext = "0";
                        miARNone.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARNone);


                        MenuItem miARSuper = new MenuItem();
                        miARSuper.Header = "Super/Titan  (6.0LY)";
                        miARSuper.DataContext = "6";
                        miARSuper.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARSuper);

                        MenuItem miARCF = new MenuItem();
                        miARCF.Header = "Carriers/Fax (7.0LY)";
                        miARCF.DataContext = "7";
                        miARCF.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARCF);

                        MenuItem miARBlops = new MenuItem();
                        miARBlops.Header = "Black Ops    (8.0LY)";
                        miARBlops.DataContext = "8";
                        miARBlops.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARBlops);

                        MenuItem miARJFR = new MenuItem();
                        miARJFR.Header = "JF/Rorq     (10.0LY)";
                        miARJFR.DataContext = "10";
                        miARJFR.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARJFR);









                    }

                }

                cm.IsOpen = true;
            }
        }

        private void characterRightClickAutoRange_Clicked(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                EveManager.JumpShip js = EveManager.JumpShip.Super;

                LocalCharacter lc = ((MenuItem)mi.Parent).DataContext as LocalCharacter;



                if (mi.DataContext as string == "6")
                {
                    js = EveManager.JumpShip.Super;
                }
                if (mi.DataContext as string == "7")
                {
                    js = EveManager.JumpShip.Carrier;
                }

                if (mi.DataContext as string == "8")
                {
                    js = EveManager.JumpShip.Blops;
                }

                if (mi.DataContext as string == "10")
                {
                    js = EveManager.JumpShip.JF;
                }


                if (mi.DataContext as string == "0")
                {
                    showJumpDistance = false;
                    currentJumpCharacter = "";
                    currentCharacterJumpSystem = "";
                }
                else
                {
                    showJumpDistance = true;
                    currentJumpCharacter = lc.Name;
                    currentCharacterJumpSystem = lc.Location;
                    jumpShipType = js;
                }
            }

            ReDrawMap(false);
        }

        /// <summary>
        /// Shape (ie System) Mouse over handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShapeMouseOverHandler(object sender, MouseEventArgs e)
        {
            Shape obj = sender as Shape;

            EVEData.MapSystem selectedSys = obj.DataContext as EVEData.MapSystem;

            Thickness one = new Thickness(1);

            if (obj.IsMouseOver && MapConf.ShowSystemPopup)
            {
                SystemInfoPopup.PlacementTarget = obj;
                SystemInfoPopup.VerticalOffset = 5;
                SystemInfoPopup.HorizontalOffset = 15;
                SystemInfoPopup.DataContext = selectedSys.ActualSystem;

                SystemInfoPopupSP.Background = new SolidColorBrush(MapConf.ActiveColourScheme.PopupBackground);

                SystemInfoPopupSP.Children.Clear();

                Label header = new Label();
                header.Content = selectedSys.Name;
                header.FontWeight = FontWeights.Bold;
                header.FontSize = 13;
                header.Padding = one;
                header.Margin = one;
                header.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);

                SystemInfoPopupSP.Children.Add(header);

                SystemInfoPopupSP.Children.Add(new Separator());

                if (selectedSys.ActualSystem.ShipKillsLastHour != 0)
                {
                    Label data = new Label();
                    data.Padding = one;
                    data.Margin = one;
                    data.Content = $"Ship Kills\t:  {selectedSys.ActualSystem.ShipKillsLastHour}";
                    data.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(data);
                }

                if (selectedSys.ActualSystem.PodKillsLastHour != 0)
                {
                    Label data = new Label();
                    data.Padding = one;
                    data.Margin = one;
                    data.Content = $"Pod Kills\t:  {selectedSys.ActualSystem.PodKillsLastHour}";
                    data.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(data);
                }

                if (selectedSys.ActualSystem.NPCKillsLastHour != 0)
                {
                    Label data = new Label();
                    data.Padding = one;
                    data.Margin = one;
                    data.Content = $"NPC Kills\t:  {selectedSys.ActualSystem.NPCKillsLastHour}, Delta ({selectedSys.ActualSystem.NPCKillsDeltaLastHour})";
                    data.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(data);
                }

                if (selectedSys.ActualSystem.JumpsLastHour != 0)
                {
                    Label data = new Label();
                    data.Padding = one;
                    data.Margin = one;

                    data.Content = $"Jumps\t:  {selectedSys.ActualSystem.JumpsLastHour}";
                    data.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(data);
                }

                if (ShowJumpBridges)
                {
                    foreach (EVEData.JumpBridge jb in EM.JumpBridges)
                    {
                        if (selectedSys.Name == jb.From)
                        {
                            Label jbl = new Label();
                            jbl.Padding = one;
                            jbl.Margin = one;
                            jbl.Content = $"JB\t: {jb.To}";
                            jbl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                            SystemInfoPopupSP.Children.Add(jbl);
                        }

                        if (selectedSys.Name == jb.To)
                        {
                            Label jbl = new Label();
                            jbl.Padding = one;
                            jbl.Margin = one;
                            jbl.Content = $"JB\t: {jb.From}";
                            jbl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                            SystemInfoPopupSP.Children.Add(jbl);
                        }
                    }
                }

                if (selectedSys.ActualSystem.IHubOccupancyLevel != 0.0f || selectedSys.ActualSystem.TCUOccupancyLevel != 0.0f)
                {
                    SystemInfoPopupSP.Children.Add(new Separator());
                }

                // update IHubInfo
                if (selectedSys.ActualSystem.IHubOccupancyLevel != 0.0f)
                {
                    Label sov = new Label();
                    sov.Padding = one;
                    sov.Margin = one;
                    sov.Content = $"IHUB\t:  {selectedSys.ActualSystem.IHubVunerabliltyStart.Hour:00}:{selectedSys.ActualSystem.IHubVunerabliltyStart.Minute:00} to {selectedSys.ActualSystem.IHubVunerabliltyEnd.Hour:00}:{selectedSys.ActualSystem.IHubVunerabliltyEnd.Minute:00}, ADM : {selectedSys.ActualSystem.IHubOccupancyLevel}";
                    sov.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(sov);
                }

                // update TCUInfo
                if (selectedSys.ActualSystem.TCUOccupancyLevel != 0.0f)
                {
                    Label sov = new Label();
                    sov.Padding = one;
                    sov.Margin = one;
                    sov.Content = $"TCU\t:  {selectedSys.ActualSystem.TCUVunerabliltyStart.Hour:00}:{selectedSys.ActualSystem.TCUVunerabliltyStart.Minute:00} to {selectedSys.ActualSystem.TCUVunerabliltyEnd.Hour:00}:{selectedSys.ActualSystem.TCUVunerabliltyEnd.Minute:00}, ADM : {selectedSys.ActualSystem.TCUOccupancyLevel}";
                    sov.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(sov);
                }

                // update Thera Info
                foreach (EVEData.TheraConnection tc in EM.TheraConnections)
                {
                    if (selectedSys.Name == tc.System)
                    {
                        SystemInfoPopupSP.Children.Add(new Separator());

                        Label tl = new Label();
                        tl.Padding = one;
                        tl.Margin = one;
                        tl.Content = $"Thera \t:  {tc.InSignatureID}";
                        tl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                        SystemInfoPopupSP.Children.Add(tl);
                    }
                }

                SystemInfoPopup.IsOpen = true;
            }
            else
            {
                SystemInfoPopup.IsOpen = false;
            }
        }

        /// <summary>
        /// Add Waypoint Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemAddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            if (ActiveCharacter != null)
            {
                ActiveCharacter.AddDestination(eveSys.ActualSystem.ID, false);
            }
        }

        /// <summary>
        /// Copy Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            try
            {
                if (eveSys != null)
                {
                    bool CopyWithMeta = true;
                    if (CopyWithMeta)
                    {
                        Clipboard.SetText($"<url=showinfo:5//{eveSys.ActualSystem.ID}>{eveSys.Name}</url>");
                    }
                    else
                    {
                        Clipboard.SetText(eveSys.Name);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Dotlan Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemDotlan_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("http://evemaps.dotlan.net/map/{0}/{1}", rd.DotLanRef, eveSys.Name);
            System.Diagnostics.Process.Start(uRL);
        }

        /// <summary>
        /// Set Destination Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemSetDestination_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            if (ActiveCharacter != null)
            {
                ActiveCharacter.AddDestination(eveSys.ActualSystem.ID, true);
            }
        }

        private void SysContexMenuItemShowInUniverse_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            RoutedEventArgs newEventArgs = new RoutedEventArgs(UniverseSystemSelectEvent, eveSys.Name);
            RaiseEvent(newEventArgs);
        }

        /// <summary>
        /// ZKillboard Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemZKB_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("https://zkillboard.com/system/{0}", eveSys.ActualSystem.ID);
            System.Diagnostics.Process.Start(uRL);
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

        /// <summary>
        /// UI Refresh Timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (currentJumpCharacter != "")
            {
                foreach (LocalCharacter c in EM.LocalCharacters)
                {
                    if (c.Name == currentJumpCharacter)
                    {
                        currentCharacterJumpSystem = c.Location;
                    }
                }
            }

            ReDrawMap(false);
        }

        private struct GateHelper
        {
            public EVEData.MapSystem from { get; set; }
            public EVEData.MapSystem to { get; set; }
        }
    }
}