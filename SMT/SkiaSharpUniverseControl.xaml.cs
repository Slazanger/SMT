using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using SMT.EVEData;
using Point = System.Windows.Point;

namespace SMT
{
    /// <summary>
    /// Interaction logic for SkiaSharpUniverseControl.xaml
    /// </summary>
    public partial class SkiaSharpUniverseControl : UserControl, INotifyPropertyChanged
    {
        // Rendering layer data structures
        private class RenderableSystem
        {
            public EVEData.System System { get; set; }
            public SKRect Bounds { get; set; }
        }

        private class RenderableItem
        {
            public SKRect Bounds { get; set; }
            public object DataContext { get; set; }
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

        public SkiaSharpUniverseControl()
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

        public static readonly RoutedEvent RequestRegionSystemSelectEvent = EventManager.RegisterRoutedEvent("RequestRegionSystem", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SkiaSharpUniverseControl));

        public event RoutedEventHandler RequestRegionSystem
        {
            add { AddHandler(RequestRegionSystemSelectEvent, value); }
            remove { RemoveHandler(RequestRegionSystemSelectEvent, value); }
        }

        private List<GateHelper> universeSysLinksCache;
        private List<KeyValuePair<string, decimal>> activeJumpSpheres;
        private EVEData.EveManager EM;

        // SkiaSharp rendering data
        private List<RenderableSystem> renderableSystems;
        private List<RenderableItem> clickableItems;
        private bool needsFullRedraw = true;
        private bool needsDataRedraw = true;
        private bool needsFastUpdate = true;

        // SkiaSharp paints and fonts
        private SKPaint SystemPaint;
        private SKPaint SystemHiSecPaint;
        private SKPaint SystemLowSecPaint;
        private SKPaint SystemNullSecPaint;
        private SKPaint SystemOutlinePaint;
        private SKPaint ConstellationPaint;
        private SKPaint SystemTextPaint;
        private SKPaint RegionTextPaint;
        private SKPaint RegionTextZoomedOutPaint;
        private SKPaint GatePaint;
        private SKPaint RegionGatePaint;
        private SKPaint PochvenGatePaint;
        private SKPaint JumpBridgePaint;
        private SKPaint DataPaint;
        private SKPaint BackgroundPaint;
        private SKPaint RegionShapePaint;
        private SKPaint WHTheraPaint;
        private SKPaint WHTurnurPaint;
        private SKFont MainFont;

        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;
        private int uiRefreshTimer_interval = 0;

        public void Init()
        {
            EM = EVEData.EveManager.Instance;

            universeSysLinksCache = new List<GateHelper>();
            activeJumpSpheres = new List<KeyValuePair<string, decimal>>();
            renderableSystems = new List<RenderableSystem>();
            clickableItems = new List<RenderableItem>();

            InitializeSkiaSharpPaints();

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();

            PropertyChanged += SkiaSharpUniverseControl_PropertyChanged;

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

        private void InitializeSkiaSharpPaints()
        {
            // Initialize all SkiaSharp paints and fonts - matching original WPF sizes
            SystemPaint = new SKPaint { Color = SKColors.Gray, Style = SKPaintStyle.Fill };
            SystemHiSecPaint = new SKPaint { Color = SKColors.Green, Style = SKPaintStyle.Fill };
            SystemLowSecPaint = new SKPaint { Color = SKColors.Yellow, Style = SKPaintStyle.Fill };
            SystemNullSecPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill };
            SystemOutlinePaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 0.3f };

            ConstellationPaint = new SKPaint { Color = SKColors.DarkGray, Style = SKPaintStyle.Stroke, StrokeWidth = 0.6f };
            SystemTextPaint = new SKPaint { Color = SKColors.White, TextSize = 5, IsAntialias = true };
            RegionTextPaint = new SKPaint { Color = SKColors.LightBlue, TextSize = 50, IsAntialias = true };
            RegionTextZoomedOutPaint = new SKPaint { Color = SKColors.Blue, TextSize = 50, IsAntialias = true };
            GatePaint = new SKPaint { Color = SKColors.Gray, Style = SKPaintStyle.Stroke, StrokeWidth = 0.6f };
            RegionGatePaint = new SKPaint { Color = SKColors.Orange, Style = SKPaintStyle.Stroke, StrokeWidth = 0.8f };
            PochvenGatePaint = new SKPaint { Color = SKColors.DimGray, Style = SKPaintStyle.Stroke, StrokeWidth = 0.3f };
            JumpBridgePaint = new SKPaint { Color = SKColors.Cyan, Style = SKPaintStyle.Stroke, StrokeWidth = 0.6f };
            DataPaint = new SKPaint { Color = SKColors.Purple, Style = SKPaintStyle.Fill };
            BackgroundPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };
            RegionShapePaint = new SKPaint { Color = SKColors.DarkBlue, Style = SKPaintStyle.Fill };
            WHTheraPaint = new SKPaint { Color = SKColors.Magenta, Style = SKPaintStyle.Fill };
            WHTurnurPaint = new SKPaint { Color = SKColors.Orange, Style = SKPaintStyle.Fill };

            // Create font - matching original WPF SystemTextSize
            MainFont = new SKFont(SKTypeface.FromFamilyName("Atkinson Hyperlegible") ?? SKTypeface.Default, 5);
        }

        private void UpdatePaintsFromColorScheme()
        {
            if (MapConf?.ActiveColourScheme == null) return;

            SystemPaint.Color = ToSKColor(MapConf.ActiveColourScheme.UniverseSystemColour);
            SystemHiSecPaint.Color = ToSKColor(MapColours.GetSecStatusColour(1.0, false));
            SystemLowSecPaint.Color = ToSKColor(MapColours.GetSecStatusColour(0.4, false));
            SystemNullSecPaint.Color = ToSKColor(MapColours.GetSecStatusColour(-0.5, true));

            ConstellationPaint.Color = ToSKColor(MapConf.ActiveColourScheme.UniverseConstellationGateColour);
            SystemTextPaint.Color = ToSKColor(MapConf.ActiveColourScheme.UniverseSystemTextColour);
            RegionTextPaint.Color = ToSKColor(MapConf.ActiveColourScheme.RegionMarkerTextColour);
            RegionTextZoomedOutPaint.Color = ToSKColor(MapConf.ActiveColourScheme.RegionMarkerTextColourFull);
            GatePaint.Color = ToSKColor(MapConf.ActiveColourScheme.UniverseGateColour);
            JumpBridgePaint.Color = ToSKColor(MapConf.ActiveColourScheme.FriendlyJumpBridgeColour);
            DataPaint.Color = ToSKColor(MapConf.ActiveColourScheme.ESIOverlayColour);
            BackgroundPaint.Color = ToSKColor(MapConf.ActiveColourScheme.UniverseMapBackgroundColour);
            RegionGatePaint.Color = ToSKColor(MapConf.ActiveColourScheme.UniverseRegionGateColour);

            var regionShapeFillCol = MapConf.ActiveColourScheme.UniverseMapBackgroundColour;
            regionShapeFillCol.R = (byte)(regionShapeFillCol.R * 0.8);
            regionShapeFillCol.G = (byte)(regionShapeFillCol.G * 0.8);
            regionShapeFillCol.B = (byte)(regionShapeFillCol.B * 0.8);
            RegionShapePaint.Color = ToSKColor(regionShapeFillCol);

            WHTheraPaint.Color = ToSKColor(MapConf.ActiveColourScheme.TheraEntranceSystem);
            WHTurnurPaint.Color = ToSKColor(MapConf.ActiveColourScheme.ThurnurEntranceSystem);
        }

        private SKColor ToSKColor(System.Windows.Media.Color wpfColor)
        {
            return new SKColor(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);
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

            MenuItem mi = sender as MenuItem;
            double LY = double.Parse(mi.DataContext as string);

            if (LY == -1.0)
            {
                activeJumpSpheres.Clear();
                UniverseCanvas.InvalidateVisual();
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

            UniverseCanvas.InvalidateVisual();
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

        private void SkiaSharpUniverseControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReDrawMap(false, true, true);
        }

        /// <summary>
        /// Redraw the map
        /// </summary>
        /// <param name="FullRedraw">Clear all the static items or not</param>
        public void ReDrawMap(bool FullRedraw = false, bool DataRedraw = false, bool FastUpdate = false)
        {
            needsFullRedraw = needsFullRedraw || FullRedraw;
            needsDataRedraw = needsDataRedraw || DataRedraw;
            needsFastUpdate = needsFastUpdate || FastUpdate;

            // Invalidate the SkiaSharp canvas to trigger a redraw
            UniverseCanvas.InvalidateVisual();
        }

        private void UniverseCanvas_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            var info = e.Info;

            // Clear the canvas
            canvas.Clear(BackgroundPaint.Color);

            if (EM == null) return;

            // Scale the canvas to match the 5000x5000 coordinate space used in WPF Canvas
            // The SKElement size is set to 5000x5000 in XAML, but we need to ensure proper scaling
            var scaleX = info.Width / 5000f;
            var scaleY = info.Height / 5000f;
            canvas.Scale(scaleX, scaleY);

            // Update paints from color scheme if needed
            if (needsFullRedraw)
            {
                UpdatePaintsFromColorScheme();
                renderableSystems.Clear();
                clickableItems.Clear();
            }

            if (needsFullRedraw)
            {
                DrawStaticElements(canvas);
                needsFullRedraw = false;
            }

            if (needsDataRedraw)
            {
                DrawDataElements(canvas);
                needsDataRedraw = false;
            }

            if (needsFastUpdate)
            {
                DrawDynamicElements(canvas);
                needsFastUpdate = false;
            }
        }

        private void DrawStaticElements(SKCanvas canvas)
        {
            // Use original WPF sizes directly - SkiaSharp handles scaling internally
            double SystemTextSize = 5;
            double textXOffset = 0;
            double textYOffset = 3;

            // Draw region shapes (background blobs)
            if (MainZoomControl.Zoom < MapConf.UniverseMaxZoomDisplaySystems)
            {
                DrawRegionShapes(canvas);
            }

            // Draw gates/links between systems
            DrawSystemLinks(canvas);

            // Draw jump bridges
            if (ShowJumpBridges)
            {
                DrawJumpBridges(canvas);
            }

            // Draw systems
            foreach (EVEData.System sys in EM.Systems)
            {
                double X = sys.UniverseX;
                double Z = sys.UniverseY;

                // Determine system color based on security status
                SKPaint sysPaint = SystemNullSecPaint;
                if (sys.TrueSec >= 0.45)
                {
                    sysPaint = SystemHiSecPaint;
                }
                else if (sys.TrueSec > 0.0)
                {
                    sysPaint = SystemLowSecPaint;
                }

                // Draw system shape using original WPF sizes
                if (sys.HasNPCStation)
                {
                    canvas.DrawRect((float)(X - 2), (float)(Z - 2), 4, 4, sysPaint);
                    canvas.DrawRect((float)(X - 2), (float)(Z - 2), 4, 4, SystemOutlinePaint);
                }
                else
                {
                    canvas.DrawCircle((float)X, (float)Z, 2, sysPaint);
                    canvas.DrawCircle((float)X, (float)Z, 2, SystemOutlinePaint);
                }

                // Add to renderable systems for hit testing
                renderableSystems.Add(new RenderableSystem
                {
                    System = sys,
                    Bounds = new SKRect((float)(X - 3), (float)(Z - 3), (float)(X + 3), (float)(Z + 3))
                });

                // Draw system name if zoomed in enough
                if (MainZoomControl.Zoom >= MapConf.UniverseMaxZoomDisplaySystemsText)
                {
                    // Update the text size to match the local SystemTextSize variable
                    SystemTextPaint.TextSize = (float)SystemTextSize;
                    var textBounds = new SKRect();
                    SystemTextPaint.MeasureText(sys.Name, ref textBounds);
                    canvas.DrawText(sys.Name, (float)(X + textXOffset - textBounds.Width / 2), (float)(Z + textYOffset), SystemTextPaint);
                }
            }

            // Draw region names
            DrawRegionNames(canvas, MainZoomControl.Zoom < MapConf.UniverseMaxZoomDisplaySystems);

            // Draw jump ranges
            DrawJumpRanges(canvas);
        }

        private void DrawRegionShapes(SKCanvas canvas)
        {
            foreach (EVEData.System sys in EM.Systems)
            {
                double X = sys.UniverseX;
                double Z = sys.UniverseY;
                double blobSize = 55;

                canvas.DrawCircle((float)X, (float)Z, (float)blobSize, RegionShapePaint);
            }
        }

        private void DrawSystemLinks(SKCanvas canvas)
        {
            foreach (GateHelper gh in universeSysLinksCache)
            {
                double X1 = gh.from.UniverseX;
                double Y1 = gh.from.UniverseY;

                double X2 = gh.to.UniverseX;
                double Y2 = gh.to.UniverseY;

                SKPaint p = GatePaint;

                if (gh.from.ConstellationID != gh.to.ConstellationID)
                {
                    p = ConstellationPaint;
                }

                if (gh.from.Region != gh.to.Region)
                {
                    p = RegionGatePaint;
                }

                if (gh.from.Region == "Pochven")
                {
                    p = PochvenGatePaint;
                }

                canvas.DrawLine((float)X1, (float)Y1, (float)X2, (float)Y2, p);
            }
        }

        private void DrawJumpBridges(SKCanvas canvas)
        {
            var dashedPaint = new SKPaint
            {
                Color = JumpBridgePaint.Color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 0.6f,
                PathEffect = SKPathEffect.CreateDash(new float[] { 2, 2 }, 0)
            };

            foreach (EVEData.JumpBridge jb in EM.JumpBridges)
            {
                EVEData.System from = EM.GetEveSystem(jb.From);
                EVEData.System to = EM.GetEveSystem(jb.To);

                if (from != null && to != null)
                {
                    double X1 = from.UniverseX;
                    double Y1 = from.UniverseY;
                    double X2 = to.UniverseX;
                    double Y2 = to.UniverseY;

                    canvas.DrawLine((float)X1, (float)Y1, (float)X2, (float)Y2, dashedPaint);
                }
            }

            dashedPaint.Dispose();
        }

        private void DrawJumpRanges(SKCanvas canvas)
        {
            var rangeCol = new SKPaint { Color = ToSKColor(MapConf.ActiveColourScheme.JumpRangeInColourHighlight), Style = SKPaintStyle.Fill };
            var sysCentreCol = new SKPaint { Color = ToSKColor(MapConf.ActiveColourScheme.SelectedSystemColour), Style = SKPaintStyle.Fill };
            var sysRangeCol = new SKPaint { Color = ToSKColor(MapConf.ActiveColourScheme.JumpRangeInColour), Style = SKPaintStyle.Fill };
            var rangeOverlapCol = new SKPaint { Color = ToSKColor(MapConf.ActiveColourScheme.JumpRangeOverlapHighlight), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

            foreach (KeyValuePair<string, decimal> kvp in activeJumpSpheres)
            {
                EVEData.System ssys = EM.GetEveSystem(kvp.Key);
                if (ssys == null) continue;

                double Radius = (double)(kvp.Value * (decimal)universeScale);
                double X = ssys.UniverseX;
                double Z = ssys.UniverseY;

                canvas.DrawCircle((float)X, (float)Z, (float)Radius, rangeCol);
                canvas.DrawRect((float)(X - 5), (float)(Z - 5), 10, 10, sysCentreCol);
            }

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

                    if (overlap)
                    {
                        canvas.DrawRect((float)(irX - 3), (float)(irZ - 3), 6, 6, rangeOverlapCol);
                    }
                    else
                    {
                        canvas.DrawRect((float)(irX - 3), (float)(irZ - 3), 6, 6, sysRangeCol);
                    }
                }
            }

            rangeCol.Dispose();
            sysCentreCol.Dispose();
            sysRangeCol.Dispose();
            rangeOverlapCol.Dispose();
        }

        private void DrawRegionNames(SKCanvas canvas, bool ZoomedOut)
        {
            double RegionTextSize = 50;
            SKPaint rtb = ZoomedOut ? RegionTextZoomedOutPaint : RegionTextPaint;

            // Update the text size to match the local RegionTextSize variable
            rtb.TextSize = (float)RegionTextSize;

            foreach (EVEData.MapRegion mr in EM.Regions)
            {
                if (mr.MetaRegion || mr.Name == "Pochven")
                {
                    continue;
                }

                double X = mr.RegionX;
                double Z = mr.RegionY;

                var textBounds = new SKRect();
                rtb.MeasureText(mr.Name, ref textBounds);
                canvas.DrawText(mr.Name, (float)(X - textBounds.Width / 2), (float)Z, rtb);
            }
        }

        private void DrawDataElements(SKCanvas canvas)
        {
            var positiveDeltaColor = new SKPaint { Color = SKColors.Green, Style = SKPaintStyle.Fill };
            var negativeDeltaColor = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill };

            foreach (EVEData.System sys in EM.Systems)
            {
                double X = sys.UniverseX;
                double Z = sys.UniverseY;
                double DataScale = 0;
                SKPaint dataBrush = DataPaint;

                if (ShowNPCKills)
                {
                    DataScale = sys.NPCKillsLastHour * ESIOverlayScale * 0.05f;

                    if (MapConf.ShowRattingDataAsDelta)
                    {
                        if (!MapConf.ShowNegativeRattingDelta)
                        {
                            DataScale = Math.Max(0, sys.NPCKillsDeltaLastHour) * ESIOverlayScale * 0.05f;
                        }
                        else
                        {
                            DataScale = Math.Abs(sys.NPCKillsDeltaLastHour) * ESIOverlayScale * 0.05f;

                            if (sys.NPCKillsDeltaLastHour > 0)
                            {
                                dataBrush = positiveDeltaColor;
                            }
                            else
                            {
                                dataBrush = negativeDeltaColor;
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
                    canvas.DrawCircle((float)X, (float)Z, (float)DataScale, dataBrush);
                }
            }

            positiveDeltaColor.Dispose();
            negativeDeltaColor.Dispose();
        }

        private void DrawDynamicElements(SKCanvas canvas)
        {
            DrawCharacters(canvas);
            DrawZKillData(canvas);
            DrawRoutes(canvas);
            DrawWormholeConnections(canvas);
        }

        private void DrawCharacters(SKCanvas canvas)
        {
            if (!MapConf.ShowCharacterNamesOnMap) return;

            float characterNametextXOffset = 3;
            float characterNametextYOffset = -16;
            double CharacterTextSize = 6; // Match original WPF version
            var characterNameBrush = new SKPaint { Color = ToSKColor(MapConf.ActiveColourScheme.CharacterTextColour), TextSize = (float)CharacterTextSize, IsAntialias = true };
            var characterNameSysHighlightBrush = new SKPaint { Color = ToSKColor(MapConf.ActiveColourScheme.CharacterHighlightColour), Style = SKPaintStyle.Stroke, StrokeWidth = 1.0f };

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
                if (sys == null) continue;

                double X = sys.UniverseX;
                double Z = sys.UniverseY;
                double charTextOffset = 0;

                // draw a circle around the system
                canvas.DrawCircle((float)X, (float)Z, 6, characterNameSysHighlightBrush);

                foreach (string name in kvp.Value)
                {
                    canvas.DrawText(name, (float)(X + characterNametextXOffset), (float)(Z + characterNametextYOffset + charTextOffset), characterNameBrush);
                    charTextOffset -= (CharacterTextSize + 2);
                }
            }

            characterNameBrush.Dispose();
            characterNameSysHighlightBrush.Dispose();
        }

        private void DrawZKillData(SKCanvas canvas)
        {
            var zkbBrush = new SKPaint { Color = ToSKColor(MapConf.ActiveColourScheme.ZKillDataOverlay), Style = SKPaintStyle.Fill };

            Dictionary<string, int> ZKBBaseFeed = new Dictionary<string, int>();
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

            foreach (KeyValuePair<string, int> kvp in ZKBBaseFeed)
            {
                double zkbVal = 5 + ((double)kvp.Value * ESIOverlayScale * 2);

                EVEData.System sys = EM.GetEveSystem(kvp.Key);
                if (sys == null) continue;

                double X = sys.UniverseX;
                double Z = sys.UniverseY;

                canvas.DrawCircle((float)X, (float)Z, (float)zkbVal, zkbBrush);
            }

            zkbBrush.Dispose();
        }

        private void DrawRoutes(SKCanvas canvas)
        {
            if (CapitalRoute?.CurrentRoute?.Count > 1)
            {
                var jumpRouteColour = new SKPaint { Color = SKColors.Orange, Style = SKPaintStyle.Stroke, StrokeWidth = 2, PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0) };
                var routeBrush = new SKPaint { Color = SKColors.Orange, Style = SKPaintStyle.Fill };
                var routeAltBrush = new SKPaint { Color = SKColors.DarkRed, Style = SKPaintStyle.Fill };

                // add the lines
                for (int i = 1; i < CapitalRoute.CurrentRoute.Count; i++)
                {
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
                            canvas.DrawCircle((float)X1, (float)Y1, 6, routeBrush);
                        }
                        canvas.DrawCircle((float)X2, (float)Y2, 6, routeBrush);
                        canvas.DrawLine((float)X1, (float)Y1, (float)X2, (float)Y2, jumpRouteColour);
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
                    if (sys != null)
                    {
                        double X = sys.UniverseX;
                        double Y = sys.UniverseY;

                        canvas.DrawCircle((float)X, (float)Y, 3, routeAltBrush);
                    }
                }

                jumpRouteColour.Dispose();
                routeBrush.Dispose();
                routeAltBrush.Dispose();
            }
        }

        private void DrawWormholeConnections(SKCanvas canvas)
        {
            // thera connections
            List<TheraConnection> currentTheraConnections = EM.TheraConnections.ToList();
            foreach (TheraConnection tc in currentTheraConnections)
            {
                EVEData.System sys = EM.GetEveSystem(tc.System);
                if (sys != null)
                {
                    double X = sys.UniverseX;
                    double Z = sys.UniverseY;
                    canvas.DrawCircle((float)X, (float)Z, 5, WHTheraPaint);
                }
            }

            // turnur connections
            List<TurnurConnection> currentTurnurConnections = EM.TurnurConnections.ToList();
            foreach (TurnurConnection tc in currentTurnurConnections)
            {
                EVEData.System sys = EM.GetEveSystem(tc.System);
                if (sys != null)
                {
                    double X = sys.UniverseX;
                    double Z = sys.UniverseY;
                    canvas.DrawCircle((float)X, (float)Z, 5, WHTurnurPaint);
                }
            }

            // add turnur itself
            EVEData.System turnurSys = EM.GetEveSystem("Turnur");
            if (turnurSys != null)
            {
                double X = turnurSys.UniverseX;
                double Z = turnurSys.UniverseY;
                canvas.DrawCircle((float)X, (float)Z, 5, WHTurnurPaint);
            }
        }

        private void MainZoomControl_ZoomChanged(object sender, RoutedEventArgs e)
        {
            needsFullRedraw = true;
            UniverseCanvas.InvalidateVisual();
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

        private void UniverseCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MapConf.Debug_EnableMapEdit == false)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                Point p = e.GetPosition(UniverseCanvas);

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

        private void UniverseCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(UniverseCanvas);
            
            // Find the system under the mouse cursor
            EVEData.System clickedSystem = null;
            
            foreach (var renderSys in renderableSystems)
            {
                if (renderSys.Bounds.Contains((float)mousePos.X, (float)mousePos.Y))
                {
                    clickedSystem = renderSys.System;
                    break;
                }
            }

            if (clickedSystem != null)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    currentDebugSystem = clickedSystem;
                }
                else
                {
                    currentDebugSystem = null;
                }

                ContextMenu cm = this.FindResource("SysRightClickContextMenu") as ContextMenu;

                cm.DataContext = clickedSystem;
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

                if (clickedSystem.SOVAllianceID != 0)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "IHUB: " + EM.GetAllianceTicker(clickedSystem.SOVAllianceID);
                    mi.DataContext = clickedSystem.SOVAllianceID;
                    mi.Click += VHSystems_SOV_Clicked;
                    SovHeader.IsEnabled = true;
                    SovHeader.Items.Add(mi);
                }

                // update stats
                MenuItem StatsHeader = cm.Items[10] as MenuItem;
                StatsHeader.Items.Clear();
                StatsHeader.IsEnabled = false;

                if (clickedSystem.HasNPCStation)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "NPC Station(s)";
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                if (clickedSystem.HasIceBelt)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Ice Belts";
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                if (clickedSystem.HasJoveObservatory)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Jove Observatory";
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                if (clickedSystem.JumpsLastHour > 0)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Jumps : " + clickedSystem.JumpsLastHour;
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                if (clickedSystem.ShipKillsLastHour > 0)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Ship Kills : " + clickedSystem.ShipKillsLastHour;
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                if (clickedSystem.PodKillsLastHour > 0)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Pod Kills : " + clickedSystem.PodKillsLastHour;
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                if (clickedSystem.NPCKillsLastHour > 0)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "NPC Kills : " + clickedSystem.NPCKillsLastHour + " (Delta: " + clickedSystem.NPCKillsDeltaLastHour + ")";
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                if (clickedSystem.RadiusAU > 0)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Radius : " + clickedSystem.RadiusAU.ToString("#.##") + " (AU)";
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                TheraConnection tc = EM.TheraConnections.Where(theraSystem => theraSystem.System == clickedSystem.Name).FirstOrDefault();
                if (tc != null)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Thera : In Sig : " + tc.InSignatureID;
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);

                    mi = new MenuItem();
                    mi.Header = "Thera : Out Sig : " + tc.OutSignatureID;
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }

                TurnurConnection tuc = EM.TurnurConnections.Where(turnerSystem => turnerSystem.System == clickedSystem.Name).FirstOrDefault();
                if (tuc != null)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Turnur : In Sig : " + tuc.InSignatureID;
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);

                    mi = new MenuItem();
                    mi.Header = "Turnur : Out Sig : " + tuc.OutSignatureID;
                    StatsHeader.IsEnabled = true;
                    StatsHeader.Items.Add(mi);
                }
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
    }
}
