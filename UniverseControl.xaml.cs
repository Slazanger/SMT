using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SMT
{
    /// <summary>
    /// Interaction logic for UniverseControl.xaml
    /// </summary>
    public partial class UniverseControl : UserControl
    {

        private DrawingGroup drawingGroup;

        private GeometryGroup systemsGeometryGroup = new GeometryGroup();
        private GeometryGroup linksGeometryGroup = new GeometryGroup();
        private GeometryGroup textGeometryGroup = new GeometryGroup();

        private GeometryDrawing systemsGeometryDrawing;
        private GeometryDrawing linksGeometryDrawing;
        private GeometryDrawing textGeometryDrawing;

        public UniverseControl()
        {
            InitializeComponent();

            systemsGeometryDrawing = new GeometryDrawing(new SolidColorBrush(Colors.Black), new Pen(Brushes.Black, 2), systemsGeometryGroup );
            linksGeometryDrawing = new GeometryDrawing(new SolidColorBrush(Colors.DarkGray), new Pen(Brushes.DarkGray, 2), linksGeometryGroup);
            textGeometryDrawing = new GeometryDrawing(new SolidColorBrush(Colors.Gray), new Pen(Brushes.Gray, 1), textGeometryGroup);


            drawingGroup = new DrawingGroup();

            drawingGroup.Children.Add(linksGeometryDrawing);
            drawingGroup.Children.Add(systemsGeometryDrawing);
            drawingGroup.Children.Add(textGeometryDrawing);

            DrawingBrush dBrush = new DrawingBrush();
            dBrush.Drawing = drawingGroup;

            
            //UniverseMainCanvas.Background = dBrush;
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
            double textXOffset = 0.5;
            double textYOffset = 0.5;

            double XScale = (8000) / universeWidth;
            double ZScale = (8000) / universeDepth;
            double scale = Math.Min(XScale, ZScale);

            Brush SysCol = new SolidColorBrush(Colors.Black);
            Brush ConstGateCol = new SolidColorBrush(Colors.Gray);
            Brush TextCol = new SolidColorBrush(Colors.DarkGray);
            Brush GateCol = new SolidColorBrush(Colors.LightGray);
            Brush JBCol = new SolidColorBrush(Colors.Blue);

            SysCol.Freeze();
            ConstGateCol.Freeze();
            TextCol.Freeze();
            GateCol.Freeze();
            JBCol.Freeze();


            System.Windows.FontStyle fontStyle = FontStyles.Normal;
            FontWeight fontWeight = FontWeights.Medium;

            if(FullRedraw)
            {
                systemsGeometryGroup.Children.Clear();
                linksGeometryGroup.Children.Clear();

                foreach (EVEData.System sys in EM.Systems)
                {
                    double size = 8.0;
                    double halfSize = size / 2.0;

                    double X = (sys.ActualX - universeXMin) * scale;

                    // need to invert Z
                    double Z = (universeDepth - (sys.ActualZ - universeZMin)) * scale;


                    Shape systemShape = new Rectangle() { Height = 6, Width = 6 };
                    systemShape.Fill = SysCol;
                    Canvas.SetLeft(systemShape, Padding + X);
                    Canvas.SetTop(systemShape, Padding + Z);
                    Canvas.SetZIndex(systemShape, 20);

                    UniverseMainCanvas.Children.Add(systemShape);

                    /*
                    RectangleGeometry rg = new RectangleGeometry(new Rect(X - 3, Z - 3, 6, 6));
                    rg.Freeze();

                    systemsGeometryGroup.Children.Add(rg);

                    FormattedText formattedText = new FormattedText(
                        sys.Name,
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Courier"), 
                        12,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );

                    Geometry textGeometry = formattedText.BuildGeometry(new Point(X + textXOffset, Z + textYOffset));
                    textGeometry.Freeze();
                    textGeometryGroup.Children.Add(textGeometry);
                    */


                    

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

                systemsGeometryGroup.Freeze();
                textGeometryGroup.Freeze();

                foreach (GateHelper gh in universeSysLinksCache)
                {
                    double X1 = (gh.from.ActualX - universeXMin) * scale;
                    double Y1 = (universeDepth - (gh.from.ActualZ - universeZMin)) * scale;

                    double X2 = (gh.to.ActualX - universeXMin) * scale;
                    double Y2 = (universeDepth - (gh.to.ActualZ - universeZMin)) * scale;

                    /*
                    LineGeometry lg = new LineGeometry(new Point(X1, Y1), new Point(X2, Y2));
                    lg.Freeze();

                    linksGeometryGroup.Children.Add(lg);
                    */

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
                        sysLink.Stroke = GateCol;
                    }
                    sysLink.StrokeThickness = 1;
                    Canvas.SetZIndex(sysLink, 19);
                    UniverseMainCanvas.Children.Add(sysLink);

                }

                linksGeometryGroup.Freeze();


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
           
        }


        /*
        
        private void OnPaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            // the the canvas and properties
            var canvas = e.Surface.Canvas;

            double XScale = (15000) / universeWidth;
            double ZScale = (15000) / universeDepth;
            double scale = Math.Min(XScale, ZScale);

            float textXOffset = 10;
            float textYOffset = 10;

            float padding = 250.0f;

            canvas.Clear(SKColors.LightGray);


            Brush SysCol = new SolidColorBrush(Colors.Black);
            Brush ConstGateCol = new SolidColorBrush(Colors.Gray);
            Brush TextCol = new SolidColorBrush(Colors.DarkGray);
            Brush JBCol = new SolidColorBrush(Colors.Blue);


            var SysPaint = new SKPaint
            {
                TextSize = 12.0f,
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
            };

            var ConstGatePaint = new SKPaint
            {
                TextSize = 12.0f,
                IsAntialias = true,
                Color = SKColors.Gray,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
            };

            var TextPaint = new SKPaint
            {
                TextSize = 12.0f,
                IsAntialias = true,
                Color = SKColors.DarkGray,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
            };

            var JBPaint = new SKPaint
            {
                TextSize = 12.0f,
                IsAntialias = true,
                Color = SKColors.Blue,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
            };

            var ZKBPaint = new SKPaint
            {
                TextSize = 12.0f,
                IsAntialias = true,
                Color = SKColors.Purple,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
            };


            // paint in back to front order



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


                foreach (EVEData.System sys in EM.Systems)
                {
                    if (ZKBBaseFeed.Keys.Contains(sys.Name))
                    {
                        float ZKBValue = 10 + (ZKBBaseFeed[sys.Name] * 2);

                        float X = (float)((sys.ActualX - universeXMin) * scale);

                        // need to invert Z
                        float Y = (float)((universeDepth - (sys.ActualZ - universeZMin)) * scale);

                        X += padding;
                        Y += padding;

                        canvas.DrawCircle(X, Y, ZKBValue, ZKBPaint);


                        canvas.DrawText(sys.Name, X + textXOffset, Y + textYOffset, SysPaint);

                    }
                }
            }





            foreach (EVEData.System sys in EM.Systems)
            {
                double size = 8.0;
                double halfSize = size / 2.0;

                float X = (float)( (sys.ActualX - universeXMin) * scale);

                // need to invert Z
                float Y = (float)((universeDepth - (sys.ActualZ - universeZMin)) * scale);

                X += padding;
                Y += padding;

                canvas.DrawCircle(X, Y, 9, SysPaint);


                canvas.DrawText(sys.Name, X + textXOffset, Y + textYOffset, SysPaint);
            }

            foreach (GateHelper gh in universeSysLinksCache)
            {

                float X1 = (float)((gh.from.ActualX - universeXMin) * scale);
                float Y1 = (float)((universeDepth - (gh.from.ActualZ - universeZMin)) * scale);

                float X2 = (float)((gh.to.ActualX - universeXMin) * scale);
                float Y2 = (float)((universeDepth - (gh.to.ActualZ - universeZMin)) * scale);

                X1 += padding;
                Y1 += padding;
                X2 += padding;
                Y2 += padding;

                
                


                if (gh.from.Region != gh.to.Region || gh.from.ConstellationID != gh.to.ConstellationID)
                {
                    canvas.DrawLine(X1, Y1, X2, Y2, ConstGatePaint); 
                }
                else
                {
                   canvas.DrawLine(X1, Y1, X2, Y2, SysPaint);
                }
            }
        } */
    }
}
