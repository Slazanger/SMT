using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SMT
{
    /// <summary>
    /// Interaction logic for RegionsViewControl.xaml
    /// </summary>
    public partial class RegionsViewControl : UserControl
    {
        private List<UIElement> dynamicRegionsViewElements = new List<UIElement>();
        private List<UIElement> dynamicRegionsViewHighlightElements = new List<UIElement>();

        public MapConfig MapConf { get; set; }
        public EVEData.LocalCharacter ActiveCharacter { get; set; }

        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        public static readonly RoutedEvent RequestRegionSelectEvent = EventManager.RegisterRoutedEvent("RequestRegion", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UniverseControl));

        public event RoutedEventHandler RequestRegion
        {
            add { AddHandler(RequestRegionSelectEvent, value); }
            remove { RemoveHandler(RequestRegionSelectEvent, value); }
        }

        public RegionsViewControl()
        {
            InitializeComponent();
        }

        public void Init()
        {
            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();

            AddRegions();
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            Redraw(false);
        }

        public void Redraw(bool redraw)
        {
            if (redraw)
            {
                MainUniverseCanvas.Children.Clear();
                AddRegions();
            }
            else
            {
                foreach (UIElement uie in dynamicRegionsViewElements)
                {
                    MainUniverseCanvas.Children.Remove(uie);
                }
                dynamicRegionsViewElements.Clear();

                foreach (UIElement uie in dynamicRegionsViewHighlightElements)
                {
                    MainUniverseCanvas.Children.Remove(uie);
                }
                dynamicRegionsViewHighlightElements.Clear();
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

                foreach (EVEData.LocalCharacter lc in EVEData.EveManager.Instance.LocalCharacters)
                {
                    EVEData.System s = EVEData.EveManager.Instance.GetEveSystem(lc.Location);
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

        private void RegionShape_MouseOverHandler(object sender, MouseEventArgs e)
        {
            Shape obj = sender as Shape;
            EVEData.MapRegion selectedRegion = obj.DataContext as EVEData.MapRegion;

            if (obj.IsMouseOver)
            {
                Brush HighlightOutlineBrush = new SolidColorBrush(Colors.Black);
                Brush ConnectedHighlightOutlineBrush = new SolidColorBrush(Colors.SlateGray);

                // add circle for system
                Rectangle regionShape = new Rectangle() { Height = 60, Width = 180 };
                regionShape.Stroke = HighlightOutlineBrush;
                regionShape.StrokeThickness = 6;
                regionShape.StrokeLineJoin = PenLineJoin.Round;
                regionShape.RadiusX = 10;
                regionShape.RadiusY = 10;
                regionShape.IsHitTestVisible = false;

                Canvas.SetLeft(regionShape, selectedRegion.UniverseViewX - 90);
                Canvas.SetTop(regionShape, selectedRegion.UniverseViewY - 30);
                Canvas.SetZIndex(regionShape, 23);
                MainUniverseCanvas.Children.Add(regionShape);
                dynamicRegionsViewHighlightElements.Add(regionShape);

                // now add connected regions
                foreach (string s in selectedRegion.RegionLinks)
                {
                    EVEData.MapRegion mr = EVEData.EveManager.Instance.GetRegion(s);

                    // add circle for system
                    Rectangle connectedRegion = new Rectangle() { Height = 60, Width = 180 };
                    connectedRegion.Stroke = ConnectedHighlightOutlineBrush;
                    connectedRegion.StrokeThickness = 6;
                    connectedRegion.StrokeLineJoin = PenLineJoin.Round;
                    connectedRegion.RadiusX = 10;
                    connectedRegion.RadiusY = 10;
                    connectedRegion.IsHitTestVisible = false;

                    Canvas.SetLeft(connectedRegion, mr.UniverseViewX - 90);
                    Canvas.SetTop(connectedRegion, mr.UniverseViewY - 30);
                    Canvas.SetZIndex(connectedRegion, 23);
                    MainUniverseCanvas.Children.Add(connectedRegion);
                    dynamicRegionsViewHighlightElements.Add(connectedRegion);

                    Line regionLink = new Line();

                    regionLink.X1 = mr.UniverseViewX;
                    regionLink.Y1 = mr.UniverseViewY;

                    regionLink.X2 = selectedRegion.UniverseViewX;
                    regionLink.Y2 = selectedRegion.UniverseViewY;

                    regionLink.Stroke = ConnectedHighlightOutlineBrush;
                    regionLink.StrokeThickness = 3;
                    regionLink.Visibility = Visibility.Visible;

                    Canvas.SetZIndex(regionLink, 21);
                    MainUniverseCanvas.Children.Add(regionLink);
                    dynamicRegionsViewHighlightElements.Add(regionLink);
                }
            }
            else
            {
                foreach (UIElement uie in dynamicRegionsViewHighlightElements)
                {
                    MainUniverseCanvas.Children.Remove(uie);
                }
                dynamicRegionsViewHighlightElements.Clear();
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
                RoutedEventArgs newEventArgs = new RoutedEventArgs(RequestRegionSelectEvent, mr.Name);
                RaiseEvent(newEventArgs);
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

                foreach (EVEData.TheraConnection tc in EVEData.EveManager.Instance.TheraConnections)
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

        /// <summary>
        /// Add Data to the Universe (Thera, Characters etc)
        /// </summary>
        private void AddDataToUniverse()
        {
            Brush sysOutlineBrush = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);
            Brush theraBrush = new SolidColorBrush(MapConf.ActiveColourScheme.TheraEntranceRegion);
            Brush characterBrush = new SolidColorBrush(MapConf.ActiveColourScheme.CharacterHighlightColour);

            foreach (EVEData.MapRegion mr in EVEData.EveManager.Instance.Regions)
            {
                bool addTheraConnection = false;
                foreach (EVEData.TheraConnection tc in EVEData.EveManager.Instance.TheraConnections)
                {
                    if (string.Compare(tc.Region, mr.Name, true) == 0)
                    {
                        addTheraConnection = true;
                        break;
                    }
                }

                if (addTheraConnection)
                {
                    Rectangle theraShape = new Rectangle() { Width = 16, Height = 16 };

                    theraShape.Stroke = sysOutlineBrush;
                    theraShape.StrokeThickness = 2;
                    theraShape.StrokeLineJoin = PenLineJoin.Round;
                    theraShape.RadiusX = 4;
                    theraShape.RadiusY = 4;
                    theraShape.Fill = theraBrush;

                    theraShape.DataContext = mr;
                    theraShape.MouseEnter += RegionThera_ShapeMouseOverHandler;
                    theraShape.MouseLeave += RegionThera_ShapeMouseOverHandler;

                    Canvas.SetLeft(theraShape, mr.UniverseViewX + 56);
                    Canvas.SetTop(theraShape, mr.UniverseViewY + 6);
                    Canvas.SetZIndex(theraShape, 22);
                    MainUniverseCanvas.Children.Add(theraShape);
                    dynamicRegionsViewElements.Add(theraShape);
                }

                bool addCharacter = false;

                foreach (EVEData.LocalCharacter lc in EVEData.EveManager.Instance.LocalCharacters)
                {
                    EVEData.System s = EVEData.EveManager.Instance.GetEveSystem(lc.Location);
                    if (s != null && s.Region == mr.Name)
                    {
                        addCharacter = true;
                    }
                }

                if (addCharacter && MapConf.ShowCharacterNamesOnMap)
                {
                    Rectangle characterShape = new Rectangle() { Width = 16, Height = 16 };

                    characterShape.Stroke = sysOutlineBrush;
                    characterShape.StrokeThickness = 2;
                    characterShape.StrokeLineJoin = PenLineJoin.Round;
                    characterShape.RadiusX = 4;
                    characterShape.RadiusY = 4;
                    characterShape.Fill = characterBrush;

                    characterShape.DataContext = mr;
                    characterShape.MouseEnter += RegionCharacter_ShapeMouseOverHandler;
                    characterShape.MouseLeave += RegionCharacter_ShapeMouseOverHandler;

                    Canvas.SetLeft(characterShape, mr.UniverseViewX + 56);
                    Canvas.SetTop(characterShape, mr.UniverseViewY - 22);
                    Canvas.SetZIndex(characterShape, 23);
                    MainUniverseCanvas.Children.Add(characterShape);
                    dynamicRegionsViewElements.Add(characterShape);
                }
            }
        }

        /// <summary>
        /// Add the regions to the universe view
        /// </summary>
        private void AddRegions()
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

            foreach (EVEData.MapRegion mr in EVEData.EveManager.Instance.Regions)
            {
                // add circle for system
                Rectangle regionShape = new Rectangle() { Height = 60, Width = 180 };
                regionShape.Stroke = sysOutlineBrush;
                regionShape.StrokeThickness = 3;
                regionShape.StrokeLineJoin = PenLineJoin.Round;
                regionShape.RadiusX = 10;
                regionShape.RadiusY = 10;
                regionShape.Fill = sysInRegionBrush;
                regionShape.MouseDown += RegionShape_MouseDown;
                regionShape.DataContext = mr;
                regionShape.MouseEnter += RegionShape_MouseOverHandler;
                regionShape.MouseLeave += RegionShape_MouseOverHandler;

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

                if (ActiveCharacter != null && ActiveCharacter.ESILinked && MapConf.ShowRegionStandings)
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
                            if (ActiveCharacter.AllianceID != 0 && ActiveCharacter.AllianceID == s.ActualSystem.SOVAllianceTCU)
                            {
                                averageStanding += 10.0f;
                            }

                            if (s.ActualSystem.SOVAllianceTCU != 0 && ActiveCharacter.Standings.Keys.Contains(s.ActualSystem.SOVAllianceTCU))
                            {
                                averageStanding += ActiveCharacter.Standings[s.ActualSystem.SOVAllianceTCU];
                            }
                        }
                        else
                        {
                            if (ActiveCharacter.AllianceID != 0 && ActiveCharacter.AllianceID == s.ActualSystem.SOVAllianceIHUB)
                            {
                                averageStanding += 10.0f;
                            }

                            if (s.ActualSystem.SOVAllianceTCU != 0 && ActiveCharacter.Standings.Keys.Contains(s.ActualSystem.SOVAllianceIHUB))
                            {
                                averageStanding += ActiveCharacter.Standings[s.ActualSystem.SOVAllianceIHUB];
                            }
                        }

                        if (s.ActualSystem.SOVCorp != 0 && ActiveCharacter.Standings.Keys.Contains(s.ActualSystem.SOVCorp))
                        {
                            averageStanding += ActiveCharacter.Standings[s.ActualSystem.SOVCorp];
                        }
                    }

                    averageStanding = averageStanding / numSystems;

                    if (averageStanding > 0.55)
                    {
                        Color blueIsh = Colors.Black;
                        blueIsh.B = (byte)(255 * (averageStanding / 10.0f));
                        regionShape.Fill = new SolidColorBrush(blueIsh);
                    }
                    else if (averageStanding < -0.55)
                    {
                        averageStanding *= -1;
                        Color redIsh = Colors.Black;
                        redIsh.R = (byte)(255 * (averageStanding / 10.0f));
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

                Canvas.SetLeft(regionShape, mr.UniverseViewX - 90);
                Canvas.SetTop(regionShape, mr.UniverseViewY - 30);
                Canvas.SetZIndex(regionShape, 22);
                MainUniverseCanvas.Children.Add(regionShape);

                Label regionText = new Label();
                regionText.Width = 180;
                regionText.Height = 54;
                regionText.Content = mr.Name;
                regionText.Foreground = sysOutlineBrush;
                regionText.FontSize = 24;
                regionText.FontWeight = FontWeights.Bold;
                regionText.HorizontalAlignment = HorizontalAlignment.Center;
                regionText.VerticalAlignment = VerticalAlignment.Center;
                regionText.IsHitTestVisible = false;

                regionText.HorizontalContentAlignment = HorizontalAlignment.Center;
                regionText.VerticalContentAlignment = VerticalAlignment.Center;

                Canvas.SetLeft(regionText, mr.UniverseViewX - 90);
                Canvas.SetTop(regionText, mr.UniverseViewY - 30);
                Canvas.SetZIndex(regionText, 23);
                MainUniverseCanvas.Children.Add(regionText);

                if (!string.IsNullOrEmpty(mr.Faction))
                {
                    Label factionText = new Label();
                    factionText.Width = 180;
                    factionText.Height = 60;
                    factionText.Content = mr.Faction;
                    factionText.Foreground = sysOutlineBrush;
                    factionText.FontSize = 12;
                    factionText.HorizontalAlignment = HorizontalAlignment.Center;
                    factionText.VerticalAlignment = VerticalAlignment.Center;
                    factionText.IsHitTestVisible = false;

                    factionText.HorizontalContentAlignment = HorizontalAlignment.Center;
                    factionText.VerticalContentAlignment = VerticalAlignment.Bottom;

                    Canvas.SetLeft(factionText, mr.UniverseViewX - 90);
                    Canvas.SetTop(factionText, mr.UniverseViewY - 30);
                    Canvas.SetZIndex(factionText, 23);
                    MainUniverseCanvas.Children.Add(factionText);
                }

                // now add all the region links : TODO :  this will end up adding 2 lines, region a -> b and b -> a
                foreach (string s in mr.RegionLinks)
                {
                    EVEData.MapRegion or = EVEData.EveManager.Instance.GetRegion(s);
                    Line regionLink = new Line();

                    regionLink.X1 = mr.UniverseViewX;
                    regionLink.Y1 = mr.UniverseViewY;

                    regionLink.X2 = or.UniverseViewX;
                    regionLink.Y2 = or.UniverseViewY;

                    regionLink.Stroke = sysOutlineBrush;
                    regionLink.StrokeThickness = 2;
                    regionLink.Visibility = Visibility.Visible;

                    Canvas.SetZIndex(regionLink, 21);
                    MainUniverseCanvas.Children.Add(regionLink);
                }
            }
        }
    }
}