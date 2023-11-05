using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using csDelaunay;
using StructureHunter;
using SMT.EVEData;

namespace SMTx.Views.Documents
{
    public partial class RegionView : UserControl
    {

        private const int SystemShapeSize = 16;
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
            public SMT.EVEData.MapSystem from { get; set; }
            public SMT.EVEData.MapSystem to { get; set; }
        }



        public RegionView()
        {
            InitializeComponent();

            staticRegionItems = new List<object>();
            dynamicRegionItems = new List<object>();

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
            RegionViewGrid.Background = BackgroundFill;
            Background = BackgroundFill;

            Thickness SystemShapePadding = new Thickness(3);


            // cache all system links
            List<GateHelper> systemLinks = new List<GateHelper>();



            SMT.EVEData.MapRegion mr = SMT.EVEData.EveManager.Instance.GetRegion("Delve");

            // Add all of the systems
            foreach (SMT.EVEData.MapSystem ms in mr.MapSystems.Values)
            {
                Shape sys;

                if (ms.ActualSystem.HasNPCStation)
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

                Canvas.SetLeft(sys, ms.Layout.X - SystemShapeOffset);
                Canvas.SetTop(sys, ms.Layout.Y - SystemShapeOffset);
                RegionViewGrid.Children.Add(sys);

                // System Name
                TextBlock systemName = new TextBlock
                {
                    Text = ms.Name,

                    Width = SystemTextWidth,
                    Height = SystemTextWidth,
                    ZIndex = SystemTextZIndex,

                    FontSize = 10,
                    Foreground = SystemTextFill,

                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                Canvas.SetLeft(systemName, ms.Layout.X - SystemTextWidthOffset);
                Canvas.SetTop(systemName, ms.Layout.Y + SystemShapeTextYOffset);
                RegionViewGrid.Children.Add(systemName);



                // generate the list of links
                foreach (string jumpTo in ms.ActualSystem.Jumps)
                {
                    if (mr.IsSystemOnMap(jumpTo))
                    {
                        SMT.EVEData.MapSystem to = mr.MapSystems[jumpTo];

                        bool NeedsAdd = true;
                        foreach (GateHelper gh in systemLinks)
                        {
                            if (((gh.from == ms) || (gh.to == ms)) && ((gh.from == to) || (gh.to == to)))
                            {
                                NeedsAdd = false;
                                break;
                            }
                        }

                        if (NeedsAdd)
                        {
                            GateHelper g = new GateHelper();
                            g.from = ms;
                            g.to = to;
                            systemLinks.Add(g);
                        }
                    }
                }
            }

            // now add all of the links
            foreach (GateHelper gh in systemLinks)
            {
                Line sysLink = new Line();
                sysLink.StartPoint = new Point(gh.from.Layout.X, gh.from.Layout.Y);
                sysLink.EndPoint = new Point(gh.to.Layout.X, gh.to.Layout.Y);
                sysLink.Stroke = LinkStroke;

                if (gh.from.ActualSystem.ConstellationID != gh.to.ActualSystem.ConstellationID)
                {
                    sysLink.Stroke = ConstellationLinkStroke;
                }

                if (gh.from.ActualSystem.Region != gh.to.ActualSystem.Region)
                {
                    sysLink.Stroke = RegionLinkStroke;
                }

                sysLink.StrokeThickness = 1.2;
                sysLink.ZIndex = SystemLinkZIndex;
                RegionViewGrid.Children.Add(sysLink);

            }

        }
    }
}
