using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SMT
{

    /// <summary>
    /// The overlay window is a separate window that is set to always-on-top 
    /// and shows systems up to a certain range from the player and highlights 
    /// the systems that have intel reported.
    /// </summary>
    /// TODO: Consolidate different methods into single UpdateMap / RedrawMap methods.
    public partial class Overlay: Window
    {

        private MainWindow mainWindow;
        private List<(string, double, double)> systemCoordinates = new List<(string, double, double)>();
        private List<(EVEData.IntelData, List<Ellipse>)> intelData = new List<(EVEData.IntelData, List<Ellipse>)>();

        private Brush sysOutlineBrush;
        private Brush intelUrgentOutlineBrush;
        private Brush intelStaleOutlineBrush;
        private Brush intelHistoryOutlineBrush;
        private Brush sysFillBrush;
        private Brush intelFillBrush;
        private Brush jumpLineBrush;
        private Brush transparentBrush;

        private PeriodicTimer intelTimer, characterUpdateTimer;

        private int overlayDepth = 8;
        private string currentPlayerSystem = "";

        private float intelUrgentPeriod = 300;
        private float intelStalePeriod = 300;
        private float intelHistoryPeriod = 600;

        public Overlay(MainWindow mw)
        {
            InitializeComponent();

            // Restore the last window size and position
            LoadOverlayWindowPosition();

            // Set up all the brushes
            sysOutlineBrush = new SolidColorBrush(Colors.White);
            sysOutlineBrush.Opacity = 0.5;

            intelUrgentOutlineBrush = new SolidColorBrush(Colors.Red);
            intelUrgentOutlineBrush.Opacity = 0.75;

            intelStaleOutlineBrush = new SolidColorBrush(Colors.Yellow);
            intelStaleOutlineBrush.Opacity = 0.5;

            intelHistoryOutlineBrush = new SolidColorBrush(Colors.White);
            intelHistoryOutlineBrush.Opacity = 0.25;

            sysFillBrush = new SolidColorBrush(Colors.White);
            sysFillBrush.Opacity = 0.2;

            intelFillBrush = new SolidColorBrush(Colors.Red);
            intelFillBrush.Opacity = 0.2;

            jumpLineBrush = new SolidColorBrush(Colors.White);
            jumpLineBrush.Opacity = 0.5;

            transparentBrush = new SolidColorBrush(Colors.White);
            transparentBrush.Opacity = 0.0;

            mainWindow = mw;
            if (mainWindow == null) return;

            // Set up some events
            mainWindow.OnSelectedCharChangedEventHandler += SelectedCharChanged;
            SizeChanged += OnSizeChanged;
            // TODO: Add better handling for new intel events.
            // mw.EVEManager.IntelAddedEvent += OnIntelAdded;

            // Start the magic
            UpdateSystemList();
            CharacterLocationUpdateLoop();
            IntelOverlayUpdateLoop();
        }

        /// <summary>
        /// Fetches the position and size values from the preferences.
        /// </summary>
        private void LoadOverlayWindowPosition()
        {
            this.Top = Properties.Settings.Default.overlay_window_top;
            this.Left = Properties.Settings.Default.overlay_window_left;
            this.Width = Properties.Settings.Default.overlay_window_width;
            this.Height = Properties.Settings.Default.overlay_window_height;
        }

        /// <summary>
        /// Stores the position and size values to the preferences.
        /// </summary>
        private void StoreOverlayWindowPosition()
        {
            Properties.Settings.Default.overlay_window_top = this.Top;
            Properties.Settings.Default.overlay_window_left = this.Left;
            Properties.Settings.Default.overlay_window_width = this.Width;
            Properties.Settings.Default.overlay_window_height = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Starts a timer that will periodically check for changes in the
        /// players location to update the map.
        /// </summary>
        /// <returns></returns>
        /// TODO: Make intervall a global setting.
        private async Task CharacterLocationUpdateLoop()
        {
            characterUpdateTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

            while (await characterUpdateTimer.WaitForNextTickAsync())
            {
                // If the location differs from the last known location, trigger a change.
                if (mainWindow.ActiveCharacter != null && mainWindow.ActiveCharacter.Location != currentPlayerSystem)
                {
                    UpdateSystemList();
                    UpdateIntelDataCoordinates();
                }
            }
        }

        /// <summary>
        /// Starts a timer that will periodically update the intel information.
        /// </summary>
        /// <returns></returns>
        /// TODO: Make intervall a global setting.
        private async Task IntelOverlayUpdateLoop()
        {
            intelTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (await intelTimer.WaitForNextTickAsync())
            {

                float intelLifetime = intelUrgentPeriod + intelStalePeriod + intelHistoryPeriod;

                // Gather all intel data from EVEManager.
                foreach (EVEData.IntelData intelDataset in mainWindow.EVEManager.IntelDataList)
                {
                    if (intelDataset.Systems.Count == 0)
                        continue;

                    // If it is older than the maximum lifetime, skip it.
                    if ((DateTime.Now - intelDataset.IntelTime).TotalSeconds > intelLifetime)
                        continue;

                    // If we already have the data in the list, skip it.
                    if (intelData.Any(d => d.Item1.RawIntelString == intelDataset.RawIntelString))
                        continue;

                    // Otherwise, add it.
                    intelData.Add((intelDataset, new List<Ellipse>()));

                }



                // Check all intel data in the list if it has expired.
                foreach (var intelDataEntry in intelData)
                {
                    if ((DateTime.Now - intelDataEntry.Item1.IntelTime).TotalSeconds > intelLifetime)
                    {

                        // If the intel data is past its lifetime, delete the shapes from the canvas.
                        foreach (Ellipse intelShape in intelDataEntry.Item2)
                        {
                            overlay_Canvas.Children.Remove(intelShape);
                        }
                    }
                }


                // Remove all expired entries.
                intelData.RemoveAll(d => (DateTime.Now - d.Item1.IntelTime).TotalSeconds > intelLifetime);

                // Loop over all remaining, therfore current, entries.
                foreach (var intelDataEntry in intelData)
                {

                    // If there are no shapes, add them.
                    if (intelDataEntry.Item2.Count == 0)
                    {
                        foreach (string systemName in intelDataEntry.Item1.Systems)
                        {
                            intelDataEntry.Item2.Add(DrawIntelToOverlay(systemName));
                        }
                    }

                    float intelAgeInSeconds = (float)(DateTime.Now - intelDataEntry.Item1.IntelTime).TotalSeconds;

                    // Update the style.
                    foreach (Ellipse intelShape in intelDataEntry.Item2)
                    {

                        switch (intelAgeInSeconds)
                        {
                            case float age when age < intelUrgentPeriod:
                                intelShape.Stroke = intelUrgentOutlineBrush;
                                intelShape.Fill = intelFillBrush;
                                break;
                            case float age when age < intelUrgentPeriod + intelStalePeriod:
                                intelShape.Stroke = intelStaleOutlineBrush;
                                intelShape.Fill = transparentBrush;
                                break;
                            case float age when age >= intelUrgentPeriod + intelStalePeriod:
                            default:
                                intelShape.Stroke = intelHistoryOutlineBrush;
                                intelShape.Fill = transparentBrush;
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Intel display data is stored separately from the other map data and
        /// therefore can and has to be updated when the content or size of the map changes.
        /// This method handles updating the existing intel data on the map.
        /// </summary>
        private void UpdateIntelDataCoordinates()
        {
            // Loop over all intel data that is currently active.
            foreach (var intelDataEntry in intelData)
            {
                // Each intel entry can have multiple systems associated. Some entries may not be valid systems.
                for (int i = 0; i < intelDataEntry.Item1.Systems.Count; i++)
                {
                    string intelSystem = intelDataEntry.Item1.Systems[i];

                    Vector2f intelSystemCoordinateVector;
                    bool visible = false;

                    // Check if the system exists in the list of currently shown systems.
                    if (systemCoordinates.Any(s => s.Item1 == intelSystem))
                    {
                        (string, double, double) intelSystemCoordinate = systemCoordinates.Where(s => s.Item1 == intelSystem).First();
                        intelSystemCoordinateVector = new Vector2f(intelSystemCoordinate.Item2 - 5, intelSystemCoordinate.Item3 - 5);
                        visible = true;
                    }
                    else
                    {
                        intelSystemCoordinateVector = new Vector2f(0, 0);
                    }

                    // Update the shape associated with the current intel entry.
                    if (intelDataEntry.Item2.Count > i)
                    {
                        Canvas.SetLeft(intelDataEntry.Item2[i], intelSystemCoordinateVector.x);
                        Canvas.SetTop(intelDataEntry.Item2[i], intelSystemCoordinateVector.y);
                        // If a system is not on the current map, hide it. Can be made visible later.
                        intelDataEntry.Item2[i].Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                    }

                    // Since all children are deleted when resizing the canvas, we readd the intel shapes.
                    overlay_Canvas.Children.Add(intelDataEntry.Item2[i]);
                }
            }
        }

        /// <summary>
        /// Updates and draws all systems that are in a certain range from the current 
        /// system the player is in.
        /// </summary>
        /// TODO: Make range a global setting.
        private void UpdateSystemList()
        {
            // Cleanup
            overlay_Canvas.Children.Clear();
            List<string> systemsInList = new List<string>();
            systemCoordinates.Clear();

            // If there is no main window or no selected character, abort.
            if (mainWindow == null || mainWindow.ActiveCharacter == null) return;

            // Gather data
            string currentLocation = mainWindow.ActiveCharacter.Location;
            currentPlayerSystem = currentLocation;
            EVEData.System currentSystem = mainWindow.EVEManager.GetEveSystem(currentLocation);

            // Bail out if the system does not exist. I.e. wormhole systems.
            if ( currentSystem == null ) return;

            List<List<EVEData.System>> hierarchie = new List<List<EVEData.System>>();

            // Add the players location to the hierarchie.
            hierarchie.Add(new List<EVEData.System>() { currentSystem });
            // Track which systems are already in the list to avoid doubles.
            systemsInList.Add(currentSystem.Name);

            for (int i = 1; i < overlayDepth; i++)
            {
                // Each depth level is represented by a list.
                List<EVEData.System> currentDepth = new List<EVEData.System>();

                // For each depth the jumps in all systems in the previous depth will be collected.
                foreach (EVEData.System previousDepthSystem in hierarchie[i - 1])
                {
                    foreach (string jump in previousDepthSystem.Jumps)
                    {
                        // Only add the system if it was not yet added.
                        if (!systemsInList.Contains(jump))
                        {
                            currentDepth.Add(mainWindow.EVEManager.GetEveSystem(jump));
                            systemsInList.Add(jump);
                        }
                    }
                }
                hierarchie.Add(currentDepth);
            }

            // Draw the systems.
            for (int i = 0; i < hierarchie.Count; i++)
            {
                DrawSystemsToOverlay(i, hierarchie[i], hierarchie.Count);
            }

            // Add the system connections.
            DrawJumpsToOverlay(systemsInList);
        }

        /// <summary>
        /// This method draws all the connections between the systems to the
        /// map.
        /// </summary>
        /// <param name="systemsInList">A list of the systems that are currently visible on the map.</param>
        /// TODO: Make size and distance settings global settings.
        private void DrawJumpsToOverlay(List<string> systemsInList)
        {
            // Keep track of the systems for which connections are already drawn.
            List<string> alreadyConnected = new List<string>();

            foreach (string systemName in systemsInList)
            {
                (string, double, double) currentCoordinate = systemCoordinates.Where(s => s.Item1 == systemName).First();
                EVEData.System currentSystem = mainWindow.EVEManager.GetEveSystem(systemName);

                // Iterate over all the connections.
                foreach (string connectedSystem in currentSystem.Jumps)
                {
                    // Only draw a connection if the target system is visible and if it was not yet connected.
                    if (systemCoordinates.Any(s => s.Item1 == connectedSystem) && !alreadyConnected.Contains(connectedSystem))
                    {
                        (string, double, double) connectedCoordinate = systemCoordinates.Where(s => s.Item1 == connectedSystem).First();
                        Line connectionLine = new Line();

                        Vector2f current = new Vector2f(currentCoordinate.Item2 + 10, currentCoordinate.Item3 + 10);
                        Vector2f connected = new Vector2f(connectedCoordinate.Item2 + 10, connectedCoordinate.Item3 + 10);

                        Vector2f currentToConnected = connected == current ? new Vector2f(0, 0) : Vector2f.Normalize(connected - current);

                        Vector2f currentCorrected = current + (currentToConnected * 10);
                        Vector2f connectedCorrected = connected - (currentToConnected * 10);

                        connectionLine.X1 = currentCorrected.x;
                        connectionLine.Y1 = currentCorrected.y;
                        connectionLine.X2 = connectedCorrected.x;
                        connectionLine.Y2 = connectedCorrected.y;
                        connectionLine.Stroke = jumpLineBrush;
                        connectionLine.StrokeThickness = 2;

                        overlay_Canvas.Children.Add(connectionLine);
                    }
                }

                alreadyConnected.Add(systemName);
            }
        }

        /// <summary>
        /// Draw icons for the systems onto the canvas. This is done for each depth separately.
        /// </summary>
        /// <param name="depth">The current depth layer.</param>
        /// <param name="systems">The systems on the current depth layer.</param>
        /// <param name="maxDepth">The maxmium depth of the current map.</param>
        private void DrawSystemsToOverlay(int depth, List<EVEData.System> systems, int maxDepth)
        {
            // Fetch data and determine the sizes for rows and columns.
            double canvasWidth = overlay_Canvas.RenderSize.Width;
            double canvasHeight = overlay_Canvas.RenderSize.Height;
            double rowHeight = canvasHeight / maxDepth;
            double columnWidth = canvasWidth / systems.Count;

            // In each depth the width of the columns is divided equally by the number of systems.
            for (int i = 0; i < systems.Count; i++)
            {
                double left = (columnWidth / 2d) + (columnWidth * i);
                double top = (rowHeight / 2d) + (rowHeight * depth);
                DrawSystemToOverlay(systems[i], left, top);
            }
        }

        /// <summary>
        /// Draws a single system to the canvas.
        /// </summary>
        /// <param name="system">The system to be drawn.</param>
        /// <param name="left">The position from the left edge.</param>
        /// <param name="top">The position from the top edge.</param>
        /// TODO: Make shape settings a global setting.
        /// TODO: Add more info to ToolTip or replace it with something else.
        private void DrawSystemToOverlay(EVEData.System system, double left, double top)
        {
            Ellipse systemShape = new Ellipse();

            systemShape.Width = 20;
            systemShape.Height = 20;
            systemShape.StrokeThickness = 1.5;
            systemShape.Stroke = sysOutlineBrush;
            systemShape.Fill = sysFillBrush;

            ToolTip systemTooltip = new ToolTip();
            systemTooltip.Content = system.Name;

            systemShape.ToolTip = systemTooltip;

            double leftCoord = left - (systemShape.Width * 0.5);
            double topCoord = top - (systemShape.Height * 0.5);

            if (!systemCoordinates.Any(s => s.Item1 == system.Name))
            {
                systemCoordinates.Add((system.Name, leftCoord, topCoord));
            }

            Canvas.SetLeft(systemShape, leftCoord);
            Canvas.SetTop(systemShape, topCoord);
            Canvas.SetZIndex(systemShape, 5);
            overlay_Canvas.Children.Add(systemShape);
        }

        /// <summary>
        /// When the character is changed, the map has to be redrawn with new data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectedCharChanged(object sender, EventArgs e)
        {
            UpdateSystemList();
            UpdateIntelDataCoordinates();
        }

        /// <summary>
        /// Handles resizing the window. On resizing the content of the map has
        /// to be refreshed. Also the new size and position of the window 
        /// is stored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSizeChanged(object sender, EventArgs e)
        {
            overlay_Canvas.Height = Math.Max(this.Height - 30, 0);
            StoreOverlayWindowPosition();
            UpdateSystemList();
            UpdateIntelDataCoordinates();
        }

        /// <summary>
        /// Handles moving the window and storing the new position data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Overlay_Window_Move(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            StoreOverlayWindowPosition();
            e.Handled = true;
        }

        /// <summary>
        /// Handles closing the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Overlay_Window_Close(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Draws the intel data to the overlay.
        /// </summary>
        /// <param name="intelSystem"></param>
        /// <returns>The shape created.</returns>
        /// TODO: Make shape settings global parameters.
        private Ellipse DrawIntelToOverlay(string intelSystem)
        {
            Ellipse intelShape = new Ellipse();
            Vector2f intelSystemCoordinateVector;

            // Only draw a visible element if the system is currently visible on the map.
            if (systemCoordinates.Any(s => s.Item1 == intelSystem))
            {
                (string, double, double) intelSystemCoordinate = systemCoordinates.Where(s => s.Item1 == intelSystem).First();
                intelSystemCoordinateVector = new Vector2f(intelSystemCoordinate.Item2 - 5, intelSystemCoordinate.Item3 - 5);
            }
            else
            {
                intelSystemCoordinateVector = new Vector2f(0, 0);
                intelShape.Visibility = Visibility.Hidden;
            }

            intelShape.Width = 30;
            intelShape.Height = 30;
            intelShape.StrokeThickness = 6;
            intelShape.Stroke = intelUrgentOutlineBrush;

            Canvas.SetLeft(intelShape, intelSystemCoordinateVector.x);
            Canvas.SetTop(intelShape, intelSystemCoordinateVector.y);
            Canvas.SetZIndex(intelShape, 1);
            overlay_Canvas.Children.Add(intelShape);

            return intelShape;
        }
    }
}
