using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using Avalonia.Controls.PanAndZoom;
using System.Diagnostics;

namespace SMTx.Views.Documents
{
    public partial class UniverseView : UserControl
    {


        private const int SystemShapeSize = 8;
        private const int SystemShapeOffset = SystemShapeSize / 2;
        private const int SystemShapeTextYOffset = SystemShapeOffset + 1;
        private const int SystemTextWidth = 50;
        private const int SystemTextWidthOffset = SystemTextWidth / 2;



        private const int SystemShapeZIndex = 20;
        private const int SystemTextZIndex = 20;
        private const int SystemLinkZIndex = 19;


        private List<object> staticRegionItems;
        private List<object> dynamicRegionItems;


        private Brush SystemFill;
        private Brush SystemStroke;
        private Brush SystemTextFill;
        private Brush LinkStroke;
        private Brush RegionLinkStroke;
        private Brush ConstellationLinkStroke;
        private Brush BackgroundFill;

        private List<Shape> canvasObjects = new List<Shape>();

        private struct GateHelper
        {
            public SMT.EVEData.System from { get; set; }
            public SMT.EVEData.System to { get; set; }
        }





        public UniverseView()
        {
            InitializeComponent();
            SetupBrushes();
            AddSystems();
        }


        private void SetupBrushes()
        {
            SystemFill = new SolidColorBrush(Colors.DeepPink);
            SystemStroke = new SolidColorBrush(Colors.Black);
            SystemTextFill = new SolidColorBrush(Colors.Black);
            LinkStroke = new SolidColorBrush(Colors.DarkGray);
            RegionLinkStroke = new SolidColorBrush(Colors.DarkRed);
            ConstellationLinkStroke = new SolidColorBrush(Colors.DarkBlue);

            BackgroundFill = new SolidColorBrush(Colors.LightGray);
        }


        private void AddSystems()
        {
            UniverseViewGrid.Background = BackgroundFill;
            Background = BackgroundFill;

            Thickness SystemShapePadding = new Thickness(3);


            // cache all system links
            List<GateHelper> systemLinks = new List<GateHelper>();

            SMT.EVEData.MapRegion mr = SMT.EVEData.EveManager.Instance.GetRegion("Delve");

            // absolute coodinate variables
            double minX, maxX, minY, maxY, absWidth, absHeight;
            minX = maxX = minY = maxY = absWidth = absHeight = 0;

            // offsets
            double xOffset, yOffset;

            // Iterate all systems (points) in the region data to find the min and max coordinate offsets
            foreach (SMT.EVEData.System s in SMT.EVEData.EveManager.Instance.Systems)
            {
                minX = Math.Min(minX, s.UniverseX);
                maxX = Math.Max(maxX, s.UniverseX);
                minY = Math.Min(minY, s.UniverseY);
                maxY = Math.Max(maxY, s.UniverseY);
            }
            // Calculate absolute width and height
            absWidth = maxX - minX;
            absHeight = maxY - minY;
            // Calculate an offset for each object on the canvas to normalize
            xOffset = (SystemTextWidth / 2d) - minX;
            yOffset = (SystemShapeSize / 2d) - minY;

            UniverseViewGrid.Width = absWidth + SystemTextWidth;
            UniverseViewGrid.Height = absHeight + SystemShapeSize + SystemShapeTextYOffset;

            // Add all of the systems
            foreach (SMT.EVEData.System s in SMT.EVEData.EveManager.Instance.Systems)
            {
                Shape sys;

                if (s.HasNPCStation)
                {
                    sys = new Rectangle
                    {
                        Width = SystemShapeSize,
                        Height = SystemShapeSize,
                        Stroke = SystemStroke,
                        Fill = SystemFill,
                        StrokeThickness = 2,
                        StrokeJoin = PenLineJoin.Round,
                        ZIndex = SystemShapeZIndex
                    };

                }
                else
                {
                    sys = new Ellipse()
                    {
                        Width = SystemShapeSize,
                        Height = SystemShapeSize,
                        Stroke = SystemStroke,
                        Fill = SystemFill,
                        StrokeThickness = 2,
                        StrokeJoin = PenLineJoin.Round,
                        ZIndex = SystemShapeZIndex
                    };

                }

                Canvas.SetLeft(sys, s.UniverseX - SystemShapeOffset + xOffset);
                Canvas.SetTop(sys, s.UniverseY - SystemShapeOffset + yOffset);
                UniverseViewGrid.Children.Add(sys);
                canvasObjects.Add(sys);

                // System Name
                TextBlock systemName = new TextBlock
                {
                    Text = s.Name,

                    Width = SystemTextWidth,
                    Height = SystemTextWidth,
                    ZIndex = SystemTextZIndex,

                    FontSize = 6,
                    Foreground = SystemTextFill,

                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                Canvas.SetLeft(systemName, s.UniverseX - SystemTextWidthOffset + xOffset);
                Canvas.SetTop(systemName, s.UniverseY + SystemShapeTextYOffset + yOffset);
                UniverseViewGrid.Children.Add(systemName);

                // TODO put the text block into a collection along with the canvas objects.
                // canvasObjects.Add(systemName);


                // generate the list of links
                foreach (string jumpTo in s.Jumps)
                {
                    SMT.EVEData.System to = SMT.EVEData.EveManager.Instance.GetEveSystem(jumpTo);

                    bool NeedsAdd = true;
                    foreach (GateHelper gh in systemLinks)
                    {
                        if (((gh.from == s) || (gh.to == s)) && ((gh.from == to) || (gh.to == to)))
                        {
                            NeedsAdd = false;
                            break;
                        }
                    }

                    if (NeedsAdd)
                    {
                        GateHelper g = new GateHelper();
                        g.from = s;
                        g.to = to;
                        systemLinks.Add(g);
                    }
                }
            }

            // now add all of the links
            foreach (GateHelper gh in systemLinks)
            {
                Line sysLink = new Line();
                sysLink.StartPoint = new Point(gh.from.UniverseX + xOffset, gh.from.UniverseY + yOffset);
                sysLink.EndPoint = new Point(gh.to.UniverseX + xOffset, gh.to.UniverseY + yOffset);
                sysLink.Stroke = LinkStroke;

                if (gh.from.ConstellationID != gh.to.ConstellationID)
                {
                    sysLink.Stroke = ConstellationLinkStroke;
                }

                if (gh.from.Region != gh.to.Region)
                {
                    sysLink.Stroke = RegionLinkStroke;
                }

                sysLink.StrokeThickness = 1.2;
                sysLink.ZIndex = SystemLinkZIndex;
                UniverseViewGrid.Children.Add(sysLink);
                canvasObjects.Add(sysLink);
            }
        }



        #region Zoom Controls

        private void SldZoom_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            double zoomTo = SldZoom.Value;

            var desiredSize = ZoomBorder.DesiredSize;

            double zoomCentreX = ZoomBorder.Bounds.Width / 2.0;
            double zoomCentreY = ZoomBorder.Bounds.Height / 2.0;

            ZoomBorder.Zoom(zoomTo, zoomCentreX, zoomCentreY, true);
        }

        // Resize back to 1:1 scale
        private void BtnOrigSize_OnClick(object sender, RoutedEventArgs e)
        {
            ZoomBorder.None(true);
        }

        // resize to fill the space
        private void BtnFill_OnClick(object sender, RoutedEventArgs e)
        {
            ZoomBorder.Uniform(true);
        }

        private void ZoomBorder_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // ignore focus changes / tab changes
            if (e.PreviousSize.Width != 0 && e.PreviousSize.Height != 0)
            {
                ZoomBorder.Uniform(true);
            }
        }

        private void ZoomBorder_ZoomChanged(object sender, ZoomChangedEventArgs e)
        {
            Debug.WriteLine($"[ZoomChanged] {e.ZoomX} {e.ZoomY} {e.OffsetX} {e.OffsetY}");
            double normalizedX = e.OffsetX / e.ZoomX;
            double normalizedY = e.OffsetY / e.ZoomY;
            Debug.WriteLine($"[Normalized X] {normalizedX} [Normalized Y] {normalizedY}");
            if (sender is ZoomBorder zoomBorder)
            {
                double maxBaseOffsetX = zoomBorder.Bounds.Width;
                double maxBaseOffsetY = zoomBorder.Bounds.Height;

                Point originVector = new Point(UniverseViewGrid.Bounds.Left, UniverseViewGrid.Bounds.Top);
                Point boundsVector = new Point(UniverseViewGrid.Bounds.Right, UniverseViewGrid.Bounds.Bottom);

                Avalonia.Matrix zoomMatrix = Avalonia.Matrix.CreateScale(e.ZoomX, e.ZoomY);

                /*
                double newFromX = zoomBorder.Matrix.Transform(originVector).X;
                double newFromY = zoomBorder.Matrix.Transform(originVector).Y;
                double newToX = zoomBorder.Matrix.Transform(boundsVector).X;
                double newToY = zoomBorder.Matrix.Transform(boundsVector).Y;
                */

                double newFromX = zoomMatrix.Transform(originVector).X;
                double newFromY = zoomMatrix.Transform(originVector).Y;
                double newToX = zoomMatrix.Transform(boundsVector).X;
                double newToY = zoomMatrix.Transform(boundsVector).Y;

                if (Math.Round(zoomBorder.MinOffsetX) != Math.Round(newFromX))
                    zoomBorder.MinOffsetX = newFromX;
                if (Math.Round(zoomBorder.MinOffsetY) != Math.Round(newFromY))
                    zoomBorder.MinOffsetY = newFromY;
                if (Math.Round(zoomBorder.MaxOffsetX) != Math.Round(newToX))
                    zoomBorder.MaxOffsetX = newToX;
                if (Math.Round(zoomBorder.MaxOffsetY) != Math.Round(newToY))
                    zoomBorder.MaxOffsetY = newToY;

                /*
                foreach (Shape sys in canvasObjects)
                {
                    // TODO iterate and clip objects based on their bounds
                }
                */
            }
        }

        #endregion

    }
}
