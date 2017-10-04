using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfHelpers.WpfControls.TrackerControl
{
    /// <summary>
    ///     Class that is associated with a Polygon on a Canvas
    /// </summary>
    public class Tracker
    {
        public enum SelectionType
        {
            /// <summary>
            /// Fill and borders
            /// </summary>
            Object = 2,

            /// <summary>
            /// Только границы
            /// </summary>
            Graphic = 3,

            /// <summary>
            /// Точка
            /// </summary>
            Point = 4,

            /// <summary>
            ///     Линия - рулетка
            /// </summary>
            Line = 5
        }

        /// <summary>
        ///     List of Corner points
        /// </summary>
        private readonly List<Ellipse> m_Corners = new List<Ellipse>();

        /// <summary>
        ///     Polygon shape of this Tracker
        /// </summary>
        private readonly Polygon m_Polygon = new Polygon();

        /// <summary>
        ///     Canvas associated with a current Tracker
        /// </summary>
        private Canvas m_Canvas;

        ///// <summary>
        /////     Radius of Corner points
        ///// </summary>
        //private int m_CornerRadius = 5;

        /// <summary>
        ///     Brush used to paint Tracker interior
        /// </summary>
        private Brush m_FillBrush;

        /// <summary>
        ///     Brush used to paint interior when Tracker is selected
        /// </summary>
        private Brush m_FillBrushSelection;

        /// <summary>
        ///     Maximum distance to a Corner from which it is considered that HitTest returns that point
        /// </summary>
        private int m_PointTolerance = 10;

        /// <summary>
        ///     Same as m_PointTolerance but squared for faster calculations
        /// </summary>
        private int m_PointToleranceSq = 100;

        /// <summary>
        ///     Indicates whether the current Tracker is selected
        /// </summary>
        private bool m_Selected;

        /// <summary>
        ///     Brush used to paint Tracker outline
        /// </summary>
        private Brush m_StrokeBrush;

        private Brush m_StrokeBrushPoint;

        /// <summary>
        ///     Brush used to paint outline when Tracker is selected
        /// </summary>
        private Brush m_StrokeBrushSelection;

        private ToolTip popup;

        private int _cornerRadius = 1;


        /// <summary>
        ///     Create new instance and associates it with a Canvas
        /// </summary>
        /// <param name="pCanvas">Canvas object to associate Tracker with</param>
        public Tracker(Canvas pCanvas, SelectionType type)
        {
            ModeType = type;
            m_Canvas = pCanvas;
            m_Canvas.Children.Add(m_Polygon);

            DefaultInitialization();
        }

        /// <summary>
        ///     Deciphered object track attached to, can be null if was not deciphered
        /// </summary>
        public object AttachedObject { get; set; }

        /// <summary>
        ///     Some object, assosiated with <see cref="Tracker" />
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        ///     Brush to save default value in compare mode
        /// </summary>
        public Brush DefaultBrush { get; set; }

        public SelectionType ModeType { get; }

        /// <summary>
        ///     Gets or sets Canvas to associate Tracker with
        /// </summary>
        public Canvas Canvas
        {
            get { return m_Canvas; }
            set
            {
                if (m_Canvas != null && m_Canvas != value)
                {
                    // If there was Canvas before - delete everything from it
                    m_Canvas.Children.Remove(m_Polygon);
                    foreach (var el in m_Corners)
                        m_Canvas.Children.Remove(el);
                }

                m_Canvas = value;
                if (m_Canvas != null)
                {
                    var img = m_Canvas.Children.OfType<Image>().First();
                    var rad = Math.Max(img.ActualWidth, img.ActualHeight) / 300;
                    m_Canvas.Children.Add(m_Polygon);

                    foreach (var el in m_Corners)
                    {
                        el.Width = rad;
                        el.Height = rad;
                        m_Canvas.Children.Add(el);
                    }
                }
            }
        }


        /// <summary>
        ///     Gets polygon shape of this Tracker
        /// </summary>
        public Polygon Polygon
        {
            get { return m_Polygon; }
        }

        /// <summary>
        ///     Gets or sets Brush that specifies how the shape's interior is painted
        /// </summary>
        public Brush FillBrush
        {
            get { return m_Polygon.Fill; }
            set
            {
                m_Polygon.Fill = value;
                m_FillBrush = value;
            }
        }

        /// <summary>
        ///     Gets or sets Brush that specifies how the shape's outline is painted
        /// </summary>
        public Brush StrokeBrush
        {
            get { return m_Polygon.Stroke; }
            set
            {
                m_Polygon.Stroke = value;
                foreach (var el in m_Corners)
                    el.Stroke = value;
                m_StrokeBrush = value;
            }
        }

        public Brush StrokeBrushPoint
        {
            get { return m_StrokeBrushPoint; }
            set
            {
                foreach (var el in m_Corners)
                    el.Stroke = value;
                m_StrokeBrushPoint = value;
            }
        }

        /// <summary>
        ///     Gets or sets Brush that specifies how the shape's interior is painted
        /// </summary>
        public Brush FillBrushSelection
        {
            get { return m_FillBrushSelection; }
            set { m_FillBrushSelection = value; }
        }

        /// <summary>
        ///     Gets or sets Brush that specifies how the shape's outline is painted
        /// </summary>
        public Brush StrokeBrushSelection
        {
            get { return m_StrokeBrushSelection; }
            set { m_StrokeBrushSelection = value; }
        }

        /// <summary>
        ///     Gets or sets the width of the outline
        /// </summary>
        public double StrokeThickness
        {
            get { return m_Polygon.StrokeThickness; }
            set
            {
                m_Polygon.StrokeThickness = value;
                foreach (var el in m_Corners)
                    el.StrokeThickness = value;
            }
        }

        /// <summary>
        ///     Gets or sets the type of join for Tracker Corners
        /// </summary>
        public PenLineJoin StrokeLineJoin
        {
            get { return m_Polygon.StrokeLineJoin; }
            set { m_Polygon.StrokeLineJoin = value; }
        }

        /// <summary>
        ///     Gets or sets a pattern of dashes and gaps that is used to outline Tracker
        /// </summary>
        public DoubleCollection StrokeDashArray
        {
            get { return m_Polygon.StrokeDashArray; }
            set { m_Polygon.StrokeDashArray = value; }
        }

        /// <summary>
        ///     Gets or sets a radius of Corner points
        /// </summary>
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set
            {
                _cornerRadius = value;

                for (int i = 0; i < m_Corners.Count; i++)
                {
                    m_Corners[i].Width = value*2;
                    m_Corners[i].Height = value*2;

                    Canvas.SetLeft(m_Corners[i], m_Polygon.Points[i].X - CornerRadius);
                    Canvas.SetTop(m_Corners[i], m_Polygon.Points[i].Y - CornerRadius);
                }
            }
        }

        /// <summary>
        ///     Maximum distance to a Corner from which it is considered that HitTest returns that Corner
        /// </summary>
        public int PointTolerance
        {
            get { return m_PointTolerance; }
            set
            {
                m_PointTolerance = value;
                m_PointToleranceSq = value * value;
            }
        }

        /// <summary>
        ///     Indicates whether the current Tracker is selected
        /// </summary>
        public bool Selected
        {
            get { return m_Selected; }
            set
            {
                if (m_Selected = value)
                {
                    //enable selection
                    m_Polygon.Fill = m_FillBrushSelection;
                    m_Polygon.Stroke = m_StrokeBrushSelection;
                    foreach (var el in m_Corners)
                        el.Stroke = m_StrokeBrushSelection;
                }
                else
                {
                    // disable selection
                    m_Polygon.Fill = m_FillBrush;
                    m_Polygon.Stroke = m_StrokeBrush;
                    foreach (var el in m_Corners)
                        el.Stroke = m_StrokeBrush;
                }
            }
        }

        /// <summary>
        ///     Returns a number of Tracker points
        /// </summary>
        public int PointCount
        {
            get { return m_Polygon.Points.Count; }
        }

        /// <summary>
        ///     Gets or sets a Tracker point
        /// </summary>
        /// <param name="index">Point index</param>
        public Point this[int index]
        {
            get { return m_Polygon.Points[index]; }
            set
            {
                m_Polygon.Points[index] = value;
                Canvas.SetLeft(m_Corners[index], value.X - CornerRadius);
                Canvas.SetTop(m_Corners[index], value.Y - CornerRadius);
            }
        }

        /// <summary>
        ///     Triggers when user is moving point
        /// </summary>
        public event EventHandler PointMoved; // { get; set; }

        public event EventHandler Disposed;

        /// <summary>
        ///     Triggers when user is moving whole tracker
        /// </summary>
        public event EventHandler TrackerMoved; // { get; set; }

        /// <summary>
        ///     Triggers when tracker finished
        /// </summary>
        public event EventHandler NewPointAdded; // { get; set; }

        public void UpdateGraphics()
        {
            if (ModeType == SelectionType.Graphic)
                DefaultInitialization();
        }

        /// <summary>
        ///     Initializes Tracker with default configuration
        /// </summary>
        private void DefaultInitialization()
        {
            switch (ModeType)
            {
                case SelectionType.Object:
                {
                    FillBrush = new SolidColorBrush(Color.FromArgb(60, 20, 160, 20));

                    StrokeThickness = 2;
                    StrokeBrush = new SolidColorBrush(Color.FromArgb(100, 20, 160, 20));
                    StrokeBrushPoint = StrokeBrush;
                    StrokeLineJoin = PenLineJoin.Round;
                    StrokeDashArray = new DoubleCollection(new double[] {4, 2});

                    FillBrushSelection = new SolidColorBrush(Color.FromArgb(60, 20, 20, 160));
                    StrokeBrushSelection = new SolidColorBrush(Color.FromArgb(100, 20, 20, 160));
                    m_Selected = false;
                }
                    break;

                case SelectionType.Graphic:
                {
                    FillBrush = new SolidColorBrush(Colors.Transparent); //Color.FromArgb(60, 20, 20, 160));

                    StrokeThickness = 2;
                    StrokeBrush = new SolidColorBrush(Color.FromArgb(100, 20, 20, 160));
                    StrokeBrushPoint = StrokeBrush;
                    StrokeLineJoin = PenLineJoin.Round;

                    m_Polygon.IsMouseDirectlyOverChanged += M_Polygon_IsMouseDirectlyOverChanged;

                    FillBrushSelection = new SolidColorBrush(Colors.Transparent); //Color.FromArgb(60, 120, 20, 160));
                    StrokeBrushSelection = new SolidColorBrush(Color.FromArgb(100, 120, 20, 160));

                    m_Selected = false;
                }
                    break;

                case SelectionType.Point:
                {
                    FillBrush = new SolidColorBrush(Colors.Transparent);

                    StrokeThickness = 2;
                    StrokeBrush = new SolidColorBrush(Colors.Transparent);
                    StrokeBrushPoint = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    StrokeLineJoin = PenLineJoin.Round;
                    StrokeDashArray = new DoubleCollection(new double[] {4, 2});

                    FillBrushSelection = new SolidColorBrush(Colors.Transparent);
                    StrokeBrushSelection = new SolidColorBrush(Colors.Transparent);

                    m_Selected = false;
                    break;
                    //m_labelVisible = true;
                }
                case SelectionType.Line:
                {
                    FillBrush = new SolidColorBrush(Colors.Transparent);
                    StrokeThickness = 1;
                    StrokeBrush = new SolidColorBrush(Colors.Black);
                    StrokeBrushPoint = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

                    StrokeLineJoin = PenLineJoin.Round;
                    //StrokeDashArray = new DoubleCollection(new double[] {2, 2});

                    FillBrushSelection = new SolidColorBrush(Colors.Transparent);
                    StrokeBrushSelection = new SolidColorBrush(Colors.Transparent);

                    m_Selected = false;
                    break;
                }
            }
        }

        private void M_Polygon_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (var ellipse in m_Corners)
                if ((bool) e.NewValue)
                    ellipse.Visibility = Visibility.Visible;
                else
                    ellipse.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        ///     Unlinks Tracker from the Canvas and releases used memory
        /// </summary>
        public void Dispose()
        {
            Canvas = null;

            OnDisposed();
        }

        /// <summary>
        ///     Adds a new point to the Tracker
        /// </summary>
        /// <param name="pPoint">New point to add</param>
        public void AddPoint(Point pPoint)
        {
            m_Polygon.Points.Add(pPoint);

            var el = new Ellipse
            {
                Width = CornerRadius * 2,
                Height = CornerRadius * 2,
                Fill = StrokeBrushPoint,
                Stroke = StrokeBrushPoint,
                StrokeThickness = m_Polygon.StrokeThickness
            };
            m_Corners.Add(el);

            if (m_Canvas != null)
            {
                m_Canvas.Children.Add(el);
                Canvas.SetLeft(el, pPoint.X + CornerRadius);
                Canvas.SetTop(el, pPoint.Y + CornerRadius);
            }

            OnNewPointAdded();
        }

        /// <summary>
        ///     Removes point from the Tracker
        /// </summary>
        /// <param name="pPoint">Point to remove from the Tracker</param>
        public void RemovePoint(Point pPoint)
        {
            var ind = m_Polygon.Points.IndexOf(pPoint);
            m_Polygon.Points.Remove(pPoint);
            Canvas.Children.Remove(m_Corners[ind]);
            m_Corners.RemoveAt(ind);
        }

        /// <summary>
        ///     Removes point at a given index from the Tracker
        /// </summary>
        /// <param name="pIndex">Point index to remove from the Tracker</param>
        public void RemovePointAt(int pIndex)
        {
            m_Polygon.Points.RemoveAt(pIndex);
            Canvas.Children.Remove(m_Corners[pIndex]);
            m_Corners.RemoveAt(pIndex);
        }

        /// <summary>
        ///     Moves the entire Tracker by a specified Vector
        /// </summary>
        public void Move(Vector pVector)
        {
            for (var i = 0; i < m_Polygon.Points.Count; i++)
            {
                var pt = m_Polygon.Points[i];
                pt.X += pVector.X;
                pt.Y += pVector.Y;
                m_Polygon.Points[i] = pt;

                Canvas.SetLeft(m_Corners[i], m_Polygon.Points[i].X + CornerRadius);
                Canvas.SetTop(m_Corners[i], m_Polygon.Points[i].Y + CornerRadius);
            }

            OnTrackerMoved();
        }

        /// <summary>
        ///     Returns element that is hit at the given point
        /// </summary>
        /// <returns>
        ///     Returns -2 if nothing was hit. Returns -1 if the entire Tracker was hit. Otherwise returns index of Point that
        ///     was hit
        /// </returns>
        public int HitTest(Point pPoint)
        {
            for (var i = 0; i < m_Polygon.Points.Count; i++)
                if ((pPoint - m_Polygon.Points[i]).LengthSquared < m_PointToleranceSq)
                    return i;

            var res = VisualTreeHelper.HitTest(m_Polygon, pPoint);
            if (res != null)
                return -1;

            return -2;
        }

        /// <summary>
        ///     Returns element that is hit at the given point
        /// </summary>
        /// <returns>
        ///     Returns -2 if nothing was hit. Returns -1 if the entire Tracker was hit. Otherwise returns index of Point that
        ///     was hit
        /// </returns>
        public int HitTest(MouseEventArgs e)
        {
            for (var i = 0; i < m_Polygon.Points.Count; i++)
                if ((e.GetPosition(m_Polygon) - m_Polygon.Points[i]).LengthSquared < m_PointToleranceSq)
                    return i;

            var res = VisualTreeHelper.HitTest(m_Polygon, e.GetPosition(m_Polygon));
            if (res != null)
                return -1;

            return -2;
        }

        public virtual void RaiseOnPointMoved()
        {
            PointMoved?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnNewPointAdded()
        {
            NewPointAdded?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTrackerMoved()
        {
            TrackerMoved?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisposed()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }
    }
}