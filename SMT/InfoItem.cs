using System.Windows.Media;
using System.Windows.Shapes;

namespace SMT
{
    public struct InfoItem
    {
        public enum ShapeType
        {
            Line,
            ArcLine,
            Circle,
            Text,
            Rect,
        };

        public enum LineType
        {
            Solid,
            LightDashed,
            Dashed,
        };

        public ShapeType DrawType;

        public int X1;
        public int Y1;
        public int X2;
        public int Y2;

        public int Size;

        public Color Fill;

        public string Region;
        public string Content;

        private Brush FillBrush;

        public LineType LineStyle;

        public Shape Draw()
        {
            if (FillBrush == null)
            {
                FillBrush = new SolidColorBrush(Fill);
            }

            Shape infoObject = null;

            switch (DrawType)
            {
                case ShapeType.Line:
                    {
                        Line infoLine = new Line();

                        infoLine.X1 = X1;
                        infoLine.Y1 = Y1;

                        infoLine.X2 = X2;
                        infoLine.Y2 = Y2;

                        infoLine.StrokeThickness = Size;
                        infoLine.Visibility = System.Windows.Visibility.Visible;
                        infoLine.Stroke = FillBrush;

                        if (LineStyle == LineType.Dashed)
                        {
                            DoubleCollection dashes = new DoubleCollection();
                            dashes.Add(1.0);
                            dashes.Add(1.0);

                            infoLine.StrokeDashArray = dashes;
                        }

                        if (LineStyle == LineType.LightDashed)
                        {
                            DoubleCollection dashes = new DoubleCollection();
                            dashes.Add(1.0);
                            dashes.Add(3.0);

                            infoLine.StrokeDashArray = dashes;
                        }

                        System.Windows.Controls.Canvas.SetZIndex(infoLine, 19);

                        infoObject = infoLine;
                    }
                    break;

                case ShapeType.ArcLine:
                    {
                        Line infoLine = new Line();

                        infoLine.X1 = X1;
                        infoLine.Y1 = Y1;

                        infoLine.X2 = X2;
                        infoLine.Y2 = Y2;

                        infoLine.StrokeThickness = Size;
                        infoLine.Visibility = System.Windows.Visibility.Visible;
                        infoLine.Stroke = FillBrush;

                        if (LineStyle == LineType.Dashed)
                        {
                            DoubleCollection dashes = new DoubleCollection();
                            dashes.Add(1.0);
                            dashes.Add(1.0);

                            infoLine.StrokeDashArray = dashes;
                        }

                        if (LineStyle == LineType.LightDashed)
                        {
                            DoubleCollection dashes = new DoubleCollection();
                            dashes.Add(1.0);
                            dashes.Add(3.0);

                            infoLine.StrokeDashArray = dashes;
                        }

                        System.Windows.Controls.Canvas.SetZIndex(infoLine, 19);

                        infoObject = infoLine;
                    }
                    break;

                case ShapeType.Circle:
                    {
                        Ellipse infoCircle = new Ellipse();
                        infoCircle.Height = Size;
                        infoCircle.Width = Size;

                        System.Windows.Controls.Canvas.SetZIndex(infoCircle, 19);

                        infoCircle.Stroke = FillBrush;
                        infoCircle.StrokeThickness = 1.5;
                        infoCircle.StrokeLineJoin = PenLineJoin.Round;
                        infoCircle.Fill = FillBrush;

                        double halfSize = Size / 2;

                        System.Windows.Controls.Canvas.SetLeft(infoCircle, X1 - halfSize);
                        System.Windows.Controls.Canvas.SetTop(infoCircle, Y1 - halfSize);

                        infoObject = infoCircle;
                    }

                    break;
            }

            return infoObject;
        }
    }
}