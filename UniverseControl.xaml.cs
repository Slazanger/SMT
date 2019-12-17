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


        private class VisualHost : FrameworkElement
        {
            // Create a collection of child visual objects.
            public VisualCollection Children;

            public VisualHost()
            {
                Children = new VisualCollection(this);
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

        }


        public UniverseControl()
        {
            InitializeComponent();
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


        private VisualHost VH;



        public void Init()
        {
            EM = EVEData.EveManager.Instance;
            


            universeSysLinksCache = new List<GateHelper>();


            universeXMin = 0.0;
            universeXMax = 336522971264518000.0;

            universeZMin = -484452845697854000;
            universeZMax = 472860102256057000.0;

            VH = new VisualHost();
            UniverseMainCanvas.Children.Add(VH);


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
            double textXOffset = 5;
            double textYOffset = 5;

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
 

                foreach (GateHelper gh in universeSysLinksCache)
                {
                    double X1 = (gh.from.ActualX - universeXMin) * scale;
                    double Y1 = (universeDepth - (gh.from.ActualZ - universeZMin)) * scale;

                    double X2 = (gh.to.ActualX - universeXMin) * scale;
                    double Y2 = (universeDepth - (gh.to.ActualZ - universeZMin)) * scale;
/*
                    Line sysLink = new Line();

                    sysLink.X1 = (gh.from.ActualX - universeXMin) * scale;
                    sysLink.Y1 = (universeDepth - (gh.from.ActualZ - universeZMin)) * scale;
                    sysLink.X2 = (gh.to.ActualX - universeXMin) * scale;
                    sysLink.Y2 = (universeDepth - (gh.to.ActualZ - universeZMin)) * scale;

                */
                    Brush Col = GateCol;

                    if (gh.from.Region != gh.to.Region || gh.from.ConstellationID != gh.to.ConstellationID)
                    {
                        Col = ConstGateCol;
                    }


                    System.Windows.Media.DrawingVisual sysLinkVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = sysLinkVisual.RenderOpen();

                    // Create a rectangle and draw it in the DrawingContext.
                    drawingContext.DrawLine(new Pen(Col, 1), new Point(X1, Y1), new Point(X2, Y2));

                    drawingContext.Close();

                    VH.Children.Add(sysLinkVisual);



                    /*
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
                   // UniverseMainCanvas.Children.Add(sysLink);

                */

                }

            }




            foreach (EVEData.JumpBridge jb in EM.JumpBridges)
            {
                Line jbLink = new Line();

                EVEData.System from = EM.GetEveSystem(jb.From);
                EVEData.System to = EM.GetEveSystem(jb.To);


                double X1 = (from.ActualX - universeXMin) * scale; ;
                double Y1 = (universeDepth - (from.ActualZ - universeZMin)) * scale;

                double X2 = (to.ActualX - universeXMin) * scale;
                double Y2 = (universeDepth - (to.ActualZ - universeZMin)) * scale;


                System.Windows.Media.DrawingVisual sysLinkVisual = new System.Windows.Media.DrawingVisual();

                // Retrieve the DrawingContext in order to create new drawing content.
                DrawingContext drawingContext = sysLinkVisual.RenderOpen();

                Pen p = new Pen(JBCol, 1);
                p.DashStyle = DashStyles.Dot;

                // Create a rectangle and draw it in the DrawingContext.
                drawingContext.DrawLine(p, new Point(X1, Y1), new Point(X2, Y2));

                drawingContext.Close();

                VH.Children.Add(sysLinkVisual);



                /*

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
                              //  UniverseMainCanvas.Children.Add(jbLink);
                            
                            */
            }





            foreach (EVEData.System sys in EM.Systems)
            {
                double size = 8.0;
                double halfSize = size / 2.0;

                double X = (sys.ActualX - universeXMin) * scale;

                // need to invert Z
                double Z = (universeDepth - (sys.ActualZ - universeZMin)) * scale;


                System.Windows.Media.DrawingVisual systemShapeVisual = new System.Windows.Media.DrawingVisual();

                // Retrieve the DrawingContext in order to create new drawing content.
                DrawingContext drawingContext = systemShapeVisual.RenderOpen();

                // Create a rectangle and draw it in the DrawingContext.
                Rect rect = new Rect(new Point(X - 3, Z - 3), new Size(6, 6));
                drawingContext.DrawRectangle(SysCol, null, rect);

                // Persist the drawing content.
                drawingContext.Close();
                VH.Children.Add(systemShapeVisual);

                /*
                Shape systemShape = new Rectangle() { Height = 6, Width = 6 };
                systemShape.Fill = SysCol;
                Canvas.SetLeft(systemShape, Padding + X);
                Canvas.SetTop(systemShape, Padding + Z);
                Canvas.SetZIndex(systemShape, 20);
                UniverseMainCanvas.Children.Add(systemShape);
                */


                // add text


                // Create an instance of a DrawingVisual.
                System.Windows.Media.DrawingVisual SystemTextVisual = new System.Windows.Media.DrawingVisual();

                // Retrieve the DrawingContext from the DrawingVisual.
                drawingContext = SystemTextVisual.RenderOpen();

#pragma warning disable CS0618 // 'FormattedText.FormattedText(string, CultureInfo, FlowDirection, Typeface, double, Brush)' is obsolete: 'Use the PixelsPerDip override'
                // Draw a formatted text string into the DrawingContext.
                drawingContext.DrawText(
                    new FormattedText(sys.Name,
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        6, TextCol),
                    new Point(X + textXOffset, Z + textYOffset));
#pragma warning enable CS0618 // 'FormattedText.FormattedText(string, CultureInfo, FlowDirection, Typeface, double, Brush)' is obsolete: 'Use the PixelsPerDip override'

                // Close the DrawingContext to persist changes to the DrawingVisual.
                drawingContext.Close();

                VH.Children.Add(SystemTextVisual);

                /*
                Label sysText = new Label();
                sysText.Content = sys.Name;
                sysText.FontSize = 6;
                sysText.Foreground = TextCol;



                Canvas.SetLeft(sysText, X + textXOffset);
                Canvas.SetTop(sysText, Z + textYOffset);
                Canvas.SetZIndex(sysText, 20);

                UniverseMainCanvas.Children.Add(sysText);
                */


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
