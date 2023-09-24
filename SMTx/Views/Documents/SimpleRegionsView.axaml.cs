using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Newtonsoft.Json.Serialization;

namespace SMTx.Views.Documents
{
    public partial class SimpleRegionsView : UserControl
    {
        const int RegionShapeWidth = 100;
        const int RegionShapeHeight = 60;
        const int RegionShapeWidthOffset = RegionShapeWidth / 2;
        const int RegionShapeHeightOffset = RegionShapeHeight / 2;


        const int RegionLinksZIndex = 19;
        const int RegionShapeZIndex = 20;
        const int RegionShapeTextZIndex = 21;
        

        private List<object> staticRegionItems;
        private List<object> dynamicRegionItems;


        public SimpleRegionsView()
        {
            InitializeComponent();

            staticRegionItems = new List<object>();
            dynamicRegionItems = new List<object>();
            SetupBrushes();
            AddRegions();
        }

        private Brush RegionFill;
        private Brush RegionStroke;
        private Brush RegionTextFill;
        private Brush BackgroundFill;
        private Brush AmarrFill;
        private Brush CaldariFill;
        private Brush GallenteFill;
        private Brush MinmatarFill;
        private Brush RegionLinksStroke;


        private void SetupBrushes()
        {
            RegionFill = new SolidColorBrush(Colors.DarkGray);
            RegionStroke = new SolidColorBrush(Colors.Black);
            BackgroundFill = new SolidColorBrush(Colors.LightGray);
            RegionLinksStroke = new SolidColorBrush(Colors.Black);
            RegionTextFill = new SolidColorBrush(Colors.Black);


            // Faction specific colours
            AmarrFill = new SolidColorBrush(Color.FromArgb(255, 126, 110, 95));
            CaldariFill = new SolidColorBrush(Color.FromArgb(255, 149, 159, 171));
            GallenteFill = new SolidColorBrush(Color.FromArgb(255, 127, 139, 137));
            MinmatarFill = new SolidColorBrush(Color.FromArgb(255, 143, 120, 120));

        }


        private void AddRegions()
        {
            MainRegionsViewGrid.Background = BackgroundFill;
            Background = BackgroundFill;

            Thickness RegionShapePadding = new Thickness(3);

            foreach (SMT.EVEData.MapRegion mr in SMT.EVEData.EveManager.Instance.Regions)
            {
                // Add the base Region

                Rectangle regionShape = new Rectangle
                {
                    Width = RegionShapeWidth,
                    Height = RegionShapeHeight,
                    Stroke = RegionStroke,
                    Fill = RegionFill,
                    RadiusX = 10,
                    RadiusY = 10,
                    StrokeThickness = 3,
                    StrokeJoin = PenLineJoin.Round,
                    ZIndex = RegionShapeZIndex
                };


                if (mr.Faction == "Amarr")
                {
                    regionShape.Fill = AmarrFill;
                }

                if (mr.Faction == "Caldari")
                {
                    regionShape.Fill = CaldariFill;
                }

                if (mr.Faction == "Gallente")
                {
                    regionShape.Fill = GallenteFill;
                }

                if (mr.Faction == "Minmatar")
                {
                    regionShape.Fill = MinmatarFill;
                }

                if (mr.HasHighSecSystems)
                {
                    regionShape.StrokeThickness = 2;
                }

                Canvas.SetLeft(regionShape, mr.UniverseViewX);
                Canvas.SetTop(regionShape, mr.UniverseViewY);
                MainRegionsViewGrid.Children.Add(regionShape);

                // Add the label

                TextBlock regionName = new TextBlock
                {
                    Text = mr.Name,

                    Width = RegionShapeWidth,
                    Height = RegionShapeHeight,
                    ZIndex = RegionShapeTextZIndex,

                    FontSize = 15,
                    Foreground = RegionTextFill,
                    Padding = RegionShapePadding,

                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Canvas.SetLeft(regionName, mr.UniverseViewX);
                Canvas.SetTop(regionName, mr.UniverseViewY);
                MainRegionsViewGrid.Children.Add(regionName);

                
                // region links : TODO :  this will end up adding 2 lines, region a -> b and b -> a
                Point regionOrigin = new Point(mr.UniverseViewX + RegionShapeWidthOffset, mr.UniverseViewY + RegionShapeHeightOffset);

                foreach (string s in mr.RegionLinks)
                {
                    SMT.EVEData.MapRegion or = SMT.EVEData.EveManager.Instance.GetRegion(s);
                    Line regionLink = new Line();
                    regionLink.ZIndex = RegionLinksZIndex;
                    regionLink.Stroke = RegionLinksStroke;
                    regionLink.StartPoint = regionOrigin;
                    regionLink.EndPoint = new Point(or.UniverseViewX + RegionShapeWidthOffset, or.UniverseViewY + RegionShapeHeightOffset);

                    MainRegionsViewGrid.Children.Add(regionLink);
                }
            }
        }
    }
}
