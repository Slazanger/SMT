using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
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
    /// This struct holds the data which is used on the overview for a system
    /// that is currently shown. The purpose of this struct is to reduce the
    /// need for recalculating or refetching certain data points.
    /// </summary>
    public struct OverlaySystemData
    {
        public EVEData.System system;
        /// <summary>
        /// The offset coordinate is in relation to the system the user
        /// is currently in.
        /// </summary>
        public Vector2 offsetCoordinate;

        public OverlaySystemData(EVEData.System sys)
        {
            system = sys;
            offsetCoordinate = new Vector2(0f,0f);
        }

        public OverlaySystemData( EVEData.System sys, Vector2 coord )
        {
            system = sys;
            offsetCoordinate = coord;
        }
    }

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
        private Brush sysLocationBrush;
        private Brush outOfRegionSysOutlineBrush;
        private Brush intelUrgentOutlineBrush;
        private Brush intelStaleOutlineBrush;
        private Brush intelHistoryOutlineBrush;
        private Brush sysFillBrush;
        private Brush outOfRegionSysFillBrush;
        private Brush intelFillBrush;
        private Brush jumpLineBrush;
        private Brush transparentBrush;

        private PeriodicTimer intelTimer, characterUpdateTimer;

        private int overlayDepth = 8;
        private OverlaySystemData currentPlayerSystemData;

        private float intelUrgentPeriod = 300;
        private float intelStalePeriod = 300;
        private float intelHistoryPeriod = 600;

        private float overlaySystemSize = 20f;

        private bool gathererMode = true;
        private bool gathererModeIncludesAdjacentRegions = false;

        private float gathererMapScalingX = 1.0f;
        private float gathererMapScalingY = 1.0f;
        private float gathererMapScalingMin = 1.0f;
        private float gathererMapLeftOffset = 0f;
        private float gathererMapTopOffset = 0f;

        private Dictionary<string, bool> regionMirrorVectors = new Dictionary<string, bool>();

        public Overlay(MainWindow mw)
        {
            InitializeComponent();

            // Restore the last window size and position
            LoadOverlayWindowPosition();

            // Set up all the brushes
            sysOutlineBrush = new SolidColorBrush(Colors.White);
            sysOutlineBrush.Opacity = 0.5;

            outOfRegionSysOutlineBrush = new SolidColorBrush(Colors.White);
            outOfRegionSysOutlineBrush.Opacity = 0.25f;

            outOfRegionSysFillBrush = new SolidColorBrush(Colors.White);
            outOfRegionSysFillBrush.Opacity = 0.1f;

            sysLocationBrush = new SolidColorBrush(Colors.Green);
            sysLocationBrush.Opacity = 0.5;

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
            mainWindow.MapConf.PropertyChanged += OverlayConf_PropertyChanged;
            SizeChanged += OnSizeChanged;
            // We can only redraw stuff when the canvas is actually resized, otherwise dimensions will be wrong!
            overlay_Canvas.SizeChanged += OnCanvasSizeChanged;

            // Update settings
            intelUrgentPeriod = mainWindow.MapConf.IntelFreshTime;
            intelStalePeriod = mainWindow.MapConf.IntelStaleTime;
            intelHistoryPeriod = mainWindow.MapConf.IntelHistoricTime;
            overlayDepth = mainWindow.MapConf.OverlayRange + 1;

            // TODO: Add better handling for new intel events.
            // mw.EVEManager.IntelAddedEvent += OnIntelAdded;

            // Start the magic
            RefreshCurrentView();
            _ = CharacterLocationUpdateLoop();
            _ = IntelOverlayUpdateLoop();
        }

        /// <summary>
        /// Cleans up and then redraws the current overlay map.
        /// This is called whenever significant data changes.
        /// </summary>
        private void RefreshCurrentView()
        {
            UpdatePlayerInformationText();
            UpdateSystemList();
            UpdateIntelDataCoordinates();
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
                if (mainWindow.ActiveCharacter != null && mainWindow.ActiveCharacter.Location != currentPlayerSystemData.system.Name)
                {
                    RefreshCurrentView();
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
                    // Clean the system list to remove everything that is not a valid system.
                    List<string> cleanedSystemList = new List<string>();
                    foreach (string intelSystem in intelDataset.Systems)
                    {
                        if (mainWindow.EVEManager.GetEveSystem(intelSystem) != null)
                        {
                            cleanedSystemList.Add(intelSystem);
                        }
                    }
                    intelDataset.Systems = cleanedSystemList;

                    if (intelDataset.Systems.Count == 0)
                        continue;

                    // If it is older than the maximum lifetime, skip it.
                    if ((DateTime.Now - intelDataset.IntelTime).TotalSeconds > intelLifetime)
                        continue;

                    // If we already have the data in the list, skip it.
                    if (intelData.Any(d => d.Item1.RawIntelString == intelDataset.RawIntelString))
                        continue;

                    // Check if it is intel for an already existing system, throw out older entries.
                    List<(EVEData.IntelData, List<Ellipse>)> deleteList = new List<(EVEData.IntelData, List<Ellipse>)>();
                    foreach (string intelSystem in intelDataset.Systems)
                    {
                        foreach (var existingIntelDataset in intelData)
                        {
                            if (existingIntelDataset.Item1.Systems.Any(s => s == intelSystem))
                            {
                                deleteList.Add(existingIntelDataset);
                            }
                        }
                    }
                    foreach (var deleteEntry in deleteList)
                    {
                        foreach (Ellipse intelShape in deleteEntry.Item2)
                        {
                            overlay_Canvas.Children.Remove(intelShape);
                        }
                        intelData.Remove(deleteEntry);
                    }

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
            EVEData.System currentSystem = mainWindow.EVEManager.GetEveSystem(currentLocation);
            currentPlayerSystemData = new OverlaySystemData(currentSystem, new Vector2(0f, 0f));

            // Bail out if the system does not exist. I.e. wormhole systems.
            if (currentSystem == null) return;

            List<List<OverlaySystemData>> hierarchie = new List<List<OverlaySystemData>>();

            // Add the players location to the hierarchie.
            hierarchie.Add(new List<OverlaySystemData>() { new OverlaySystemData(currentSystem, new Vector2(0f,0f)) });

            // Track which systems are already in the list to avoid doubles.
            systemsInList.Add(currentSystem.Name);

            Vector2 maxCoord = new Vector2(float.MinValue, float.MinValue);
            Vector2 minCoord = new Vector2(float.MaxValue, float.MaxValue);

            for (int i = 1; i < overlayDepth; i++)
            {
                // Each depth level is represented by a list.
                List<OverlaySystemData> currentDepth = new List<OverlaySystemData>();

                // For each depth the jumps in all systems in the previous depth will be collected.
                foreach (OverlaySystemData previousDepthSystem in hierarchie[i - 1])
                {
                    foreach (string jump in previousDepthSystem.system.Jumps)
                    {
                        EVEData.System jumpSystem = mainWindow.EVEManager.GetEveSystem(jump);

                        string sourceRegion = previousDepthSystem.system.Region;
                        string targetRegion = jumpSystem.Region;

                        if ( gathererMode == true || targetRegion == currentPlayerSystemData.system.Region || sourceRegion == currentPlayerSystemData.system.Region || (regionMirrorVectors.ContainsKey(targetRegion) && gathererModeIncludesAdjacentRegions) )
                        {

                            // Only add the system if it was not yet added.
                            if (!systemsInList.Contains(jump))
                            {
                                // if source and target have different regions it is a border system.
                                if (sourceRegion != targetRegion)
                                {

                                    if (!regionMirrorVectors.ContainsKey(targetRegion))
                                    {
                                        Vector2 prevSystemInSourceRegionLayout = mainWindow.EVEManager.GetRegion(sourceRegion).MapSystems[previousDepthSystem.system.Name].Layout;
                                        Vector2 jumpSystemInSourceRegionLayout = mainWindow.EVEManager.GetRegion(sourceRegion).MapSystems[jump].Layout;

                                        Vector2 prevSystemInTargetRegionLayout = mainWindow.EVEManager.GetRegion(jumpSystem.Region).MapSystems[previousDepthSystem.system.Name].Layout;
                                        Vector2 jumpSystemInTargetRegionLayout = mainWindow.EVEManager.GetRegion(jumpSystem.Region).MapSystems[jump].Layout;

                                        Vector2 sourceRegionDirection = jumpSystemInSourceRegionLayout - prevSystemInSourceRegionLayout;
                                        Vector2 targetRegionDirection = jumpSystemInTargetRegionLayout - prevSystemInTargetRegionLayout;

                                        /*
                                         * If the connecting vector between the systems has opposing directions
                                         * in each region, we note that we need to mirror the systems in the target
                                         * region to avoid overlap. This is messy and should be avoided.
                                         */
                                        if (Vector2.Dot(sourceRegionDirection, targetRegionDirection) < 0)
                                        {
                                            regionMirrorVectors.Add(targetRegion, true);
                                        }
                                        else
                                        {
                                            regionMirrorVectors.Add(targetRegion, false);
                                        }
                                    }
                                }

                                Vector2 originSystemCoord = mainWindow.EVEManager.GetRegion(jumpSystem.Region).MapSystems[previousDepthSystem.system.Name].Layout;
                                Vector2 jumpSystemCoord = mainWindow.EVEManager.GetRegion(jumpSystem.Region).MapSystems[jump].Layout;

                                Vector2 systemConnection = (jumpSystemCoord - originSystemCoord);

                                // If we need to mirror it, rotate it by 180 degrees or PI radians.
                                if (regionMirrorVectors.ContainsKey(targetRegion) && regionMirrorVectors[targetRegion])
                                {
                                    systemConnection = Vector2.Transform(systemConnection, Matrix3x2.CreateRotation((float)Math.PI));
                                }

                                Vector2 originOffset = previousDepthSystem.offsetCoordinate + systemConnection;

                                maxCoord.X = Math.Max(maxCoord.X, originOffset.X);
                                maxCoord.Y = Math.Max(maxCoord.Y, originOffset.Y);
                                minCoord.X = Math.Min(minCoord.X, originOffset.X);
                                minCoord.Y = Math.Min(minCoord.Y, originOffset.Y);

                                currentDepth.Add(new OverlaySystemData(mainWindow.EVEManager.GetEveSystem(jump), originOffset));
                                systemsInList.Add(jump);
                            }
                        }
                    }
                }
                hierarchie.Add(currentDepth);
            }

            double canvasWidth = overlay_Canvas.RenderSize.Width;
            double canvasHeight = overlay_Canvas.RenderSize.Height;

            // The dimension of the area covered by all systems on the overlay.
            float coordDimensionsX = maxCoord.X - minCoord.X;
            float coordDimensionsY = maxCoord.Y - minCoord.Y;

            // The scaling to bring the coordinates to the canvas area.
            gathererMapScalingX = ((float)canvasWidth - (overlaySystemSize * 2f)) / coordDimensionsX;
            gathererMapScalingY = ((float)canvasHeight - (overlaySystemSize * 2f)) / coordDimensionsY;
            gathererMapScalingMin = Math.Min(gathererMapScalingX, gathererMapScalingY);

            float gathererMapDimensionX = (maxCoord.X - minCoord.X) * gathererMapScalingMin;
            float gathererMapDimensionY = (maxCoord.Y - minCoord.Y) * gathererMapScalingMin;

            float emptySpaceX = (float)canvasWidth - gathererMapDimensionX;
            float emptySpaceY = (float)canvasWidth - gathererMapDimensionY;

            // How the systems need to be positioned to be displayed correctly.
            gathererMapLeftOffset = (minCoord.X * gathererMapScalingX) - overlaySystemSize;
            gathererMapTopOffset = (minCoord.Y * gathererMapScalingY) - overlaySystemSize;

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
        private void DrawSystemsToOverlay(int depth, List<OverlaySystemData> systems, int maxDepth)
        {
            // Fetch data and determine the sizes for rows and columns.
            double canvasWidth = overlay_Canvas.RenderSize.Width;
            double canvasHeight = overlay_Canvas.RenderSize.Height;
            double rowHeight = canvasHeight / maxDepth;
            double columnWidth = canvasWidth / systems.Count;

            // In each depth the width of the columns is divided equally by the number of systems.
            for (int i = 0; i < systems.Count; i++)
            {
                double left, top;
                if (gathererMode)
                {
                    left = (columnWidth / 2d) + (columnWidth * i);
                    top = (rowHeight / 2d) + (rowHeight * depth);
                }
                else
                {
                    left = -gathererMapLeftOffset  + (systems[i].offsetCoordinate.X * gathererMapScalingMin);
                    top = -gathererMapTopOffset + (systems[i].offsetCoordinate.Y * gathererMapScalingMin);
                }
                DrawSystemToOverlay(systems[i], left, top);
            }
        }

        /// <summary>
        /// Draws a single system to the canvas.
        /// </summary>
        /// <param name="systemData">The system to be drawn.</param>
        /// <param name="left">The position from the left edge.</param>
        /// <param name="top">The position from the top edge.</param>
        /// TODO: Make shape settings a global setting.
        /// TODO: Add more info to ToolTip or replace it with something else.
        private void DrawSystemToOverlay(OverlaySystemData systemData, double left, double top)
        {
            Ellipse systemShape = new Ellipse();

            systemShape.Width = overlaySystemSize;
            systemShape.Height = overlaySystemSize;
            systemShape.StrokeThickness = 1.5;
            if ( systemData.system.Name == currentPlayerSystemData.system.Name )
            {
                systemShape.Fill = sysLocationBrush;
            }
            else
            {
                systemShape.Fill = sysFillBrush;
            }
            systemShape.Stroke = sysOutlineBrush;
            
            if ( systemData.system.Region != currentPlayerSystemData.system.Region )
            {
                systemShape.Stroke = outOfRegionSysOutlineBrush;
                systemShape.Fill = outOfRegionSysFillBrush;
            }

            ToolTip systemTooltip = new ToolTip();
            systemTooltip.Content = systemData.system.Name;

            systemShape.ToolTip = systemTooltip;

            double leftCoord = left - (systemShape.Width * 0.5);
            double topCoord = top - (systemShape.Height * 0.5);

            if (!systemCoordinates.Any(s => s.Item1 == systemData.system.Name))
            {
                systemCoordinates.Add((systemData.system.Name, leftCoord, topCoord));
            }

            Canvas.SetLeft(systemShape, leftCoord);
            Canvas.SetTop(systemShape, topCoord);
            Canvas.SetZIndex(systemShape, 5);
            overlay_Canvas.Children.Add(systemShape);
        }

        /// <summary>
        /// This is a callback that will be executed whenever a setting is changed.
        /// It is set to filter only those settings that will affect the overlay window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OverlayConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IntelFreshTime")
            {
                intelUrgentPeriod = mainWindow.MapConf.IntelFreshTime;
            }

            if (e.PropertyName == "IntelStaleTime")
            {
                intelStalePeriod = mainWindow.MapConf.IntelStaleTime;
            }

            if (e.PropertyName == "IntelHistoricTime")
            {
                intelHistoryPeriod = mainWindow.MapConf.IntelHistoricTime;
            }

            if (e.PropertyName == "OverlayRange")
            {
                overlayDepth = mainWindow.MapConf.OverlayRange + 1;
                RefreshCurrentView();
            }
        }

        /// <summary>
        /// When the character is changed, the map has to be redrawn with new data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectedCharChanged(object sender, EventArgs e)
        {
            RefreshCurrentView();
        }

        /// <summary>
        /// Fetches the currently selected characters name and location and
        /// displays it in the overlay window.
        /// </summary>
        public void UpdatePlayerInformationText ()
        {
            if (mainWindow.ActiveCharacter != null)
            {
                overlay_CharNameTextblock.Text = mainWindow.ActiveCharacter.Name + "\n" + mainWindow.ActiveCharacter.Location;
            }
            else
            {
                overlay_CharNameTextblock.Text = "";
            }
        }

        /// <summary>
        /// Handles resizing the window. On resizing the content the new size and 
        /// position of the window is stored. Also the size of the canvas is updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            overlay_Canvas.Height = Math.Max(e.NewSize.Height - 30, 1);
        }

        /// <summary>
        /// When the canvas is resized we need to redraw the map.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshCurrentView ();
        }

        /// <summary>
        /// Handles moving the window and storing the new position data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Overlay_Window_Move(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
                StoreOverlayWindowPosition();
            }
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

        private void Overlay_ToggleHunterMode(object sender, MouseButtonEventArgs e)
        {
            gathererMode = !gathererMode;
            RefreshCurrentView(); ;
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
