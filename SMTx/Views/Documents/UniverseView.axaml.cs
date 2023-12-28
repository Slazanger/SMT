using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;

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

                Canvas.SetLeft(sys, s.UniverseX - SystemShapeOffset);
                Canvas.SetTop(sys, s.UniverseY - SystemShapeOffset);
                UniverseViewGrid.Children.Add(sys);

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

                Canvas.SetLeft(systemName, s.UniverseX - SystemTextWidthOffset);
                Canvas.SetTop(systemName, s.UniverseY + SystemShapeTextYOffset);
                UniverseViewGrid.Children.Add(systemName);



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
                sysLink.StartPoint = new Point(gh.from.UniverseX, gh.from.UniverseY);
                sysLink.EndPoint = new Point(gh.to.UniverseX, gh.to.UniverseY);
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
            }
        }



        #region Zoom Controls

        private void SldZoom_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            double zoomTo = SldZoom.Value;
            double zoomCentreX = ZoomBorder.DesiredSize.Width / 2;
            double zoomCentreY = ZoomBorder.DesiredSize.Height / 2;


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
            ZoomBorder.Uniform(true);
        }

        #endregion

    }
}
