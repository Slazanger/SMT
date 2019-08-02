using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SMT
{
    /// <summary>
    /// Interaction logic for UniverseControl.xaml
    /// </summary>
    public partial class UniverseControl : UserControl
    {
        public UniverseControl()
        {
            InitializeComponent();

            //InitMapData();
        }


        private struct GateHelper
        {
            public EVEData.System from { get; set; }
            public EVEData.System to { get; set; }
        }


        private List<GateHelper> universeSysLinksCache;
        private double universeWidth;
        private double universeDepth;
        private double universeXMin;
        private double universeXMax;

        private double universeZMin;
        private double universeZMax;

        private EVEData.EveManager EM;



        public void Init()
        {
            EM = EVEData.EveManager.Instance;

            universeSysLinksCache = new List<GateHelper>();


            universeXMin = 0.0;
            universeXMax = 336522971264518000.0;

            universeZMin = -484452845697854000;
            universeZMax = 472860102256057000.0;

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

            ReDrawMap(true);

        }


        /// <summary>
        /// Redraw the map
        /// </summary>
        /// <param name="FullRedraw">Clear all the static items or not</param>
        public void ReDrawMap(bool FullRedraw = false)
        {
            double Padding = -3;
            double textXOffset = 1;
            double textYOffset = 1;

            double XScale = (8000) / universeWidth;
            double ZScale = (8000) / universeDepth;
            double scale = Math.Min(XScale, ZScale);

            Brush SysCol = new SolidColorBrush(Colors.Black);
            Brush ConstGateCol = new SolidColorBrush(Colors.Gray);
            Brush TextCol = new SolidColorBrush(Colors.DarkGray);
            Brush JBCol = new SolidColorBrush(Colors.Blue);

            UniverseMainCanvas.Visibility = Visibility.Hidden;
            


            foreach (EVEData.System sys in EM.Systems)
            {
                double size = 8.0;
                double halfSize = size / 2.0;

                Shape systemShape = new Rectangle() { Height = 6, Width = 6 };
                systemShape.Fill = SysCol;

                double X = (sys.ActualX - universeXMin) * scale;

                // need to invert Z
                double Z = (universeDepth - (sys.ActualZ - universeZMin)) * scale;

                Canvas.SetLeft(systemShape, Padding + X);
                Canvas.SetTop(systemShape, Padding + Z);
                Canvas.SetZIndex(systemShape, 20);

                UniverseMainCanvas.Children.Add(systemShape);

                // add text
                Label sysText = new Label();
                sysText.Content = sys.Name;
                sysText.FontSize = 6;
                sysText.Foreground = TextCol;

                Canvas.SetLeft(sysText, X + textXOffset);
                Canvas.SetTop(sysText, Z + textYOffset);
                Canvas.SetZIndex(sysText, 20);

                UniverseMainCanvas.Children.Add(sysText);
            }

            foreach (GateHelper gh in universeSysLinksCache)
            {
                Line sysLink = new Line();

                sysLink.X1 = (gh.from.ActualX - universeXMin) * scale;
                sysLink.Y1 = (universeDepth - (gh.from.ActualZ - universeZMin)) * scale;

                sysLink.X2 = (gh.to.ActualX - universeXMin) * scale;
                sysLink.Y2 = (universeDepth - (gh.to.ActualZ - universeZMin)) * scale;

                if (gh.from.Region != gh.to.Region || gh.from.ConstellationID != gh.to.ConstellationID)
                {
                    sysLink.Stroke = ConstGateCol;
                }
                else
                {
                    sysLink.Stroke = SysCol;
                }

                sysLink.StrokeThickness = 1;

                Canvas.SetZIndex(sysLink, 19);
                UniverseMainCanvas.Children.Add(sysLink);
            }


            foreach( EVEData.JumpBridge jb in EM.JumpBridges)
            {
                Line jbLink = new Line();

                EVEData.System from = EM.GetEveSystem(jb.From);
                EVEData.System to = EM.GetEveSystem(jb.To);


                jbLink.X1 = (from.ActualX - universeXMin) * scale;
                jbLink.Y1 = (universeDepth - (from.ActualZ - universeZMin)) * scale;

                jbLink.X2 = (to.ActualX - universeXMin) * scale;
                jbLink.Y2 = (universeDepth - (to.ActualZ - universeZMin)) * scale;

                jbLink.StrokeThickness = 1;
                jbLink.Visibility = Visibility.Visible;

                jbLink.Stroke = JBCol;


                DoubleCollection dashes = new DoubleCollection();
                dashes.Add(1.0);
                dashes.Add(1.0);

                jbLink.StrokeDashArray = dashes;

                Canvas.SetZIndex(jbLink, 19);
                UniverseMainCanvas.Children.Add(jbLink);
            }



            UniverseMainCanvas.Visibility = Visibility.Visible;
        }
    }
}
