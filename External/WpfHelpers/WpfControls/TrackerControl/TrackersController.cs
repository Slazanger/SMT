using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfHelpers.WpfControls.TrackerControl
{
    /// <summary>
    ///     Class that contains and controls Trackers
    /// </summary>
    public class TrackersController : List<Tracker>
    {
        /// <summary>
        /// Triggers when track change selection
        /// </summary>
        public event EventHandler CurrentSelectionChanged;

        public bool CanMoveTrackers { get; set; }

        private Brush _selectedObject = new SolidColorBrush(Color.FromArgb(40, 200, 50, 50));

        private Brush _fillObjectColor = new SolidColorBrush(Color.FromArgb(92, 20, 160, 20));
        //Window m_Window = null;
        /// <summary>
        ///     Window that is associated with a TrackerController
        /// </summary>
        /// <summary>
        ///     Canvas that is associated with a TrackerController
        /// </summary>
        private readonly Canvas m_Canvas;

        private Tracker.SelectionType RemoveType;

        /// <summary>
        ///     Indicates whether a new Tracker adding is occuring
        /// </summary>
        private bool m_Add;

        /// <summary>
        ///     Currently dragged Tracker
        /// </summary>
        private Tracker m_CurrentTracker;

        /// <summary>
        ///     Current drag state
        /// </summary>
        private int m_Drag = -2;

        /// <summary>
        ///     Last click point
        /// </summary>
        private Point m_LastClick;

        /// <summary>
        ///     Indicates whether a Tracker removal is occuring
        /// </summary>
        private bool m_Remove;

        /// <summary>
        /// Window, tracker attached to
        /// </summary>
        private System.Windows.Window m_Window;

        /// <summary>
        ///     Create new instance and associate it with a given Window and Canvas
        /// </summary>
        /// <param name="pWnd">Window to associate TrackersController with</param>
        /// <param name="pCanvas">Canvas to associate TrackersController with</param>
        public TrackersController(System.Windows.Window pWnd, Canvas pCanvas)
        {
            m_Window = pWnd;
            m_Canvas = pCanvas;
            CanMoveTrackers = true;
        }

        public void Init()
        {
            m_Canvas.MouseUp -= OnMouseUp;
            m_Canvas.MouseUp += OnMouseUp;

            m_Canvas.MouseDown -= OnMouseDown;
            m_Canvas.MouseDown += OnMouseDown;

            m_Canvas.MouseMove -= OnMouseMove;
            m_Canvas.MouseMove += OnMouseMove;
        }

        /// <summary>
        ///     MouseDown event handler
        /// </summary>
        public void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (m_Add)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    m_CurrentTracker.AddPoint(e.GetPosition(m_Canvas));

                    if (m_CurrentTracker.ModeType == Tracker.SelectionType.Line && m_CurrentTracker.PointCount == 3)
                    {
                        m_CurrentTracker.RemovePointAt(m_CurrentTracker.PointCount - 1);
                        m_Add = false;
                    }
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    //DecipheredObjectsDicpatcher.Instance.RemoveArea(m_CurrentTracker);
                    if (m_CurrentTracker.PointCount == 1)
                    {
                        m_CurrentTracker.Dispose();
                    }
                    else
                        m_CurrentTracker.RemovePointAt(m_CurrentTracker.PointCount - 1);
                    m_Add = false;
                }
                e.Handled = true;
            }
            else if (m_Remove)
            {
                if (e.ChangedButton == MouseButton.Right)
                {
                    foreach (Tracker track in this)
                    {
                        track.Selected = false;
                    }
                    m_Remove = false;
                    m_Window.Cursor = Cursors.Arrow;
                }
                else if (e.ChangedButton == MouseButton.Left)
                {
                    m_CurrentTracker = null;
                    foreach (Tracker track in this.Where(o => o.ModeType == RemoveType))
                    {
                        int res = track.HitTest(e);
                        if (res != -2)
                        {
                            m_CurrentTracker = track;
                            break;
                        }
                    }
                    if (m_CurrentTracker != null)
                    {
                        Remove(m_CurrentTracker);
                        m_CurrentTracker.Dispose();
                        m_Remove = false;
                        m_Window.Cursor = Cursors.Arrow;
                    }
                }
                e.Handled = true;
            }
            else
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    //Сначала проверяем все объекты, начиная с последних добавленных
                    foreach (Tracker track in this.Where(o => o.ModeType == Tracker.SelectionType.Object).Reverse())
                    {
                        m_Drag = track.HitTest(e);
                        if (m_Drag != -2)
                        {
                            m_LastClick = e.GetPosition(track.Polygon);
                            m_CurrentTracker = track;
                            // логика выделения нового объекта
                            foreach (
                                Tracker tr in this.Where(o => o.ModeType == Tracker.SelectionType.Object && o.Selected && o != m_CurrentTracker))
                            {
                                tr.Selected = false;
                                tr.FillBrush = _fillObjectColor;
                            }

                            m_CurrentTracker.Selected = !m_CurrentTracker.Selected;

                            //::Event Trigger
                            CurrentSelectionChanged?.Invoke(this, EventArgs.Empty);


                            if (m_CurrentTracker.Selected)
                                m_CurrentTracker.FillBrush = _selectedObject;
                            else
                                m_CurrentTracker.FillBrush = _fillObjectColor;
                            e.Handled = true;
                            return;
                        }
                    }
                    // потом области выделения, в обратном порядке
                    foreach (Tracker track in this.Where(o => o.ModeType != Tracker.SelectionType.Object).Reverse())
                    {
                        m_Drag = track.HitTest(e);
                        if (m_Drag != -2)
                        {
                            m_LastClick = e.GetPosition(track.Polygon);
                            m_CurrentTracker = track;
                            e.Handled = true;
                            return;
                        }
                    }
                }

                // удаляем линии-рулетки
                if (e.ChangedButton == MouseButton.Right)
                {
                    foreach (Tracker track in this.Where(o => o.ModeType == Tracker.SelectionType.Line).Reverse())
                    {
                        m_Drag = track.HitTest(e);
                        if (m_Drag != -2)
                        {
                            m_CurrentTracker = null;
                            Remove(track);
                            track.Dispose();
                            m_Remove = false;
                            m_Window.Cursor = Cursors.Arrow;
                            return;
                        }
                    }
                }
            }
        }

        public new void Add(Tracker tracker)
        {
            base.Add(tracker);
        }

        public new void Remove(Tracker track)
        {
            track.Dispose();
            base.Remove(track);
        }

        /// <summary>
        ///     MouseUp event handler
        /// </summary>
        public void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            m_Drag = -2;
        }

        /// <summary>
        ///     MouseMove event handler
        /// </summary>
        public void OnMouseMove(object sender, MouseEventArgs e)
        {

            if (m_Add)
            {
                m_CurrentTracker[m_CurrentTracker.PointCount - 1] = e.GetPosition(m_Canvas);
                e.Handled = true;
            }
            else if (m_Remove)
            {
                bool isSelection = false;
                foreach (Tracker track in this.Where(o => o.ModeType == RemoveType))
                {
                    int res = track.HitTest(e);
                    if ((res != -2) && (!isSelection))
                    {
                        track.Selected = true;
                        isSelection = true;
                    }
                    else
                        track.Selected = false;
                }

                e.Handled = true;
            }
            // MOVE
            else
            {
                if (!CanMoveTrackers )
                    return;

                if (m_CurrentTracker?.AttachedObject != null)
                    return;

                //e.Handled = true;

                if (m_Drag == -2)
                {
                    int res = -2;

                    foreach (Tracker track in this)
                    {
                        res = track.HitTest(e);
                        if (res != -2)
                            break;
                    }

                    if (res >= 0)
                        m_Window.Cursor = Cursors.Arrow;
                    else if (res == -1)
                        m_Window.Cursor = Cursors.Arrow;
                    else
                        m_Window.Cursor = Cursors.Arrow;
                }
                else if (m_Drag == -1)
                {
                    m_CurrentTracker.Move(e.GetPosition(m_Canvas) - m_LastClick);
                    m_LastClick = e.GetPosition(m_CurrentTracker.Polygon);
                }
                else
                {
                    Point pt = m_CurrentTracker[m_Drag];
                    pt.X += e.GetPosition(m_Canvas).X - m_LastClick.X;
                    pt.Y += e.GetPosition(m_Canvas).Y - m_LastClick.Y;
                    m_CurrentTracker[m_Drag] = pt;

                    m_CurrentTracker.RaiseOnPointMoved();

                    m_LastClick = e.GetPosition(m_CurrentTracker.Polygon);
                }
            }
        }

        /// <summary>
        ///     Starts adding a new default Tracker
        /// </summary>
        public Tracker AddTracker(Tracker.SelectionType type)
        {
            if ((m_Add) || (m_Remove))
                return null;
            m_Remove = false;
            m_Add = true;
            m_CurrentTracker = new Tracker(m_Canvas, type);
            Add(m_CurrentTracker);
            m_CurrentTracker.AddPoint(Mouse.GetPosition(m_Canvas));

            return m_CurrentTracker;
        }

        /// <summary>
        ///     Starts adding a new given Tracker. It should be empty (no points) but may be configured
        /// </summary>
        public void AddTracker(Tracker pTracker)
        {
            if ((m_Add) || (m_Remove))
                return;

            m_Remove = false;
            m_Add = true;
            m_CurrentTracker = pTracker;
            pTracker.Canvas = m_Canvas;
            Add(pTracker);
            m_CurrentTracker.AddPoint(Mouse.GetPosition(m_Canvas));
        }

        /// <summary>
        ///     Starts Tracker removal
        /// </summary>
        public void RemoveTracker(Tracker.SelectionType type)
        {
            if ((m_Add) || (m_Remove))
                return;
            m_Add = false;
            RemoveType = type;
            m_Remove = true;
            m_Window.Cursor = Cursors.Arrow;
        }

        public void Cancel()
        {
            m_CurrentTracker.Dispose();
            m_CurrentTracker = null;
            m_Add = false;
            m_Drag = -2;
            m_Remove = false;
        }
    }
}