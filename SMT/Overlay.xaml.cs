using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Windows.Services;
using Microsoft.IdentityModel.Tokens;
using NHotkey;
using SMT.EVEData;
using static SMT.EVEData.Navigation;

public static class WindowsServices
{
  const int WS_EX_TRANSPARENT = 0x00000020;
  const int GWL_EXSTYLE = (-20);

  [DllImport("user32.dll")]
  static extern int GetWindowLong(IntPtr hwnd, int index);

  [DllImport("user32.dll")]
  static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

  public static void SetWindowExTransparent(IntPtr hwnd)
  {
      var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
    SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
  }
  
  public static void SetWindowExNotTransparent(IntPtr hwnd)
  {
      var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
      SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
  }
}

namespace SMT
{
    /// <summary>
    /// This class holds the data which is used on the overview for a system
    /// that is currently shown. The purpose of this class is to reduce the
    /// need for recalculating or refetching certain data points.
    /// </summary>
    public class OverlaySystemData
    {
        public EVEData.System system;

        /// <summary>
        /// The offset coordinate is in relation to the system the user
        /// is currently in.
        /// </summary>
        public Vector2 offsetCoordinate;

        public Vector2 mapSystemCoordinate;
        public Vector2 canvasCoordinate;

        public int rattingDelta;

        public Shape systemCanvasElement;
        public TextBlock systemNameElement;
        public Shape npcKillCanvasElement;
        public Shape npcKillDeltaCanvasElement;
        public Path jumpBridgePath;
        public string jumpBridgeTarget = "";

        public EVEData.IntelData intelData;

        public int overlayTier = 0;

        public OverlaySystemData(EVEData.System sys, Vector2 coord, Vector2 mapSysCoord, Vector2 canvasCoord, int ratDelta, Shape shape, int ovrTier)
        {
            system = sys;
            offsetCoordinate = coord;
            mapSystemCoordinate = mapSysCoord;
            canvasCoordinate = canvasCoord;
            rattingDelta = ratDelta;
            systemCanvasElement = shape;
            overlayTier = ovrTier;
            intelData = null;
        }

        public OverlaySystemData(EVEData.System sys) : this(sys, Vector2.Zero, Vector2.Zero, Vector2.Zero, 0, null, 0)
        {
        }

        public OverlaySystemData(EVEData.System sys, Vector2 coord) : this(sys, coord, Vector2.Zero, Vector2.Zero, 0, null, 0)
        {
        }

        public void CleanUpCanvas(Canvas canvas, bool keepSystem = false)
        {
            if (!keepSystem && systemCanvasElement != null && canvas.Children.Contains(systemCanvasElement)) canvas.Children.Remove(systemCanvasElement);
            if (!keepSystem && systemNameElement != null && canvas.Children.Contains(systemNameElement)) canvas.Children.Remove(systemNameElement);
            if (npcKillCanvasElement != null && canvas.Children.Contains(npcKillCanvasElement)) canvas.Children.Remove(npcKillCanvasElement);
            if (npcKillDeltaCanvasElement != null && canvas.Children.Contains(npcKillDeltaCanvasElement)) canvas.Children.Remove(npcKillDeltaCanvasElement);
            CleanUpJumpBridges(canvas);
        }

        public void CleanUpJumpBridges(Canvas canvas)
        {
            if (jumpBridgePath != null && canvas.Children.Contains(jumpBridgePath)) canvas.Children.Remove(jumpBridgePath);
            jumpBridgePath = null;
        }
    }

    /// <summary>
    /// A class to hold all the information necessary to draw systems to the canvas.
    /// Does the data collection and computation to go from layout coordinates
    /// to canvas coordinates.
    /// </summary>
    public class OverlayCanvasData
    {
        public Vector2 dimensions;

        public Vector2 borderedDimensions
        { get { return dimensions - (Vector2.One * (2f * mapBorderMargin)); } }

        public Vector2 unscaledMapExtendsMax;
        public Vector2 unscaledMapExtendsMin;

        public Vector2 unscaledMapDimensions
        { get { return new Vector2(unscaledMapExtendsMax.X - unscaledMapExtendsMin.X, unscaledMapExtendsMax.Y - unscaledMapExtendsMin.Y); } }

        public Vector2 currentOriginCoordinates;

        public float mapBorderMargin;
        public float mapBorderBottomAdd;
        public float mapScalingX;
        public float mapScalingY;
        public bool mapBorderBottomExtension = false;

        public float mapScalingMin
        { get { return Math.Min(mapScalingY, mapScalingX); } }

        public OverlayCanvasData()
        {
            dimensions = Vector2.One * 100f;

            unscaledMapExtendsMax = new Vector2(float.MinValue, float.MinValue);
            unscaledMapExtendsMin = new Vector2(float.MaxValue, float.MaxValue);

            currentOriginCoordinates = Vector2.Zero;

            mapBorderMargin = 20f;
            mapBorderBottomAdd = 10f;
            mapScalingX = 1f;
            mapScalingY = 1f;
        }

        /// <summary>
        /// Sets the current dimensions of the canvas element.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetDimensions(double width, double height)
        {
            dimensions.X = Math.Max((float)width, 100f);
            dimensions.Y = Math.Max((float)height, 100f);
        }

        /// <summary>
        /// Resets the stored minimal and mixmum extends in layout coordinates.
        /// </summary>
        public void ResetExtends()
        {
            unscaledMapExtendsMax = new Vector2(float.MinValue, float.MinValue);
            unscaledMapExtendsMin = new Vector2(float.MaxValue, float.MaxValue);
        }

        /// <summary>
        /// Updates the min and max extends in layout coordinates.
        /// </summary>
        /// <param name="offsetCoordinates">Coordinates of a system in relation to the origin system (current player location).</param>
        public void UpdateUnscaledExtends(Vector2 offsetCoordinates)
        {
            unscaledMapExtendsMax.X = Math.Max(unscaledMapExtendsMax.X, offsetCoordinates.X);
            unscaledMapExtendsMax.Y = Math.Max(unscaledMapExtendsMax.Y, offsetCoordinates.Y);
            unscaledMapExtendsMin.X = Math.Min(unscaledMapExtendsMin.X, offsetCoordinates.X);
            unscaledMapExtendsMin.Y = Math.Min(unscaledMapExtendsMin.Y, offsetCoordinates.Y);
            
            if (unscaledMapExtendsMax.X - unscaledMapExtendsMin.X < mapBorderMargin * 3f)
            {
                unscaledMapExtendsMax.X += mapBorderMargin * 1.5f;
                unscaledMapExtendsMin.X -= mapBorderMargin * 1.5f;
            }
            
            if (unscaledMapExtendsMax.Y - unscaledMapExtendsMin.Y < mapBorderMargin * 3f)
            {
                unscaledMapExtendsMax.Y += mapBorderMargin * 1.5f;
                unscaledMapExtendsMin.Y -= mapBorderMargin * 1.5f;
            }
            
            ComputeScaling();
        }

        /// <summary>
        /// Computes the scaling needed to draw all systems onto the canvas.
        /// </summary>
        private void ComputeScaling()
        {
            mapScalingX = (dimensions.X - (mapBorderMargin * 2f)) / unscaledMapDimensions.X;
            mapScalingY = (dimensions.Y - (mapBorderMargin * 2f) - (mapBorderBottomExtension ? mapBorderBottomAdd : 0)) / unscaledMapDimensions.Y;
        }

        /// <summary>
        /// Takes layout coordinates and converts them to canvas coordinates.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public Vector2 CoordinateToCanvas(Vector2 coordinate)
        {
            Vector2 scaledMapDimensions = unscaledMapDimensions * mapScalingMin;
            float emptySpaceOffsetX = (borderedDimensions.X - scaledMapDimensions.X) * 0.5f;
            float emptySpaceOffsetY = (borderedDimensions.Y - scaledMapDimensions.Y) * 0.5f;
            float canvasX = emptySpaceOffsetX + mapBorderMargin + (((coordinate.X - unscaledMapExtendsMin.X) * mapScalingMin) / borderedDimensions.X) * borderedDimensions.X;
            float canvasY = emptySpaceOffsetY + mapBorderMargin - (mapBorderBottomExtension ? (mapBorderBottomAdd * 0.5f) : 0 ) + (((coordinate.Y - unscaledMapExtendsMin.Y) * mapScalingMin) / borderedDimensions.Y) * borderedDimensions.Y;
            return new Vector2(canvasX, canvasY);
        }
    }

    /// <summary>
    /// The overlay window is a separate window that is set to always-on-top
    /// and shows systems up to a certain range from the player and highlights
    /// the systems that have intel reported.
    /// </summary>
    /// TODO: Consolidate different methods into single UpdateMap / RedrawMap methods.
    public partial class Overlay : Window
    {
        private MainWindow mainWindow;
        private Dictionary<string, OverlaySystemData> systemData = new Dictionary<string, OverlaySystemData>();
        private List<(EVEData.IntelData data, List<Ellipse> ellipse)> intelData = new List<(EVEData.IntelData, List<Ellipse>)>();
        private List<Line> jumpLines = new List<Line>();
        private List<OverlaySystemData> bridgeSystems = new List<OverlaySystemData>();
        private List<Shape> routeLines = new List<Shape>();
        private Ellipse jumpBridgeTargetHighlight = null;

        private Brush sysOutlineBrush;
        private Brush sysTheraOutlineBrush;
        private Brush sysLocationOutlineBrush;
        private Brush npcKillDataBrush;
        private Brush npcKillDeltaDataBrush;
        private Brush outOfRegionSysOutlineBrush;
        private Brush intelUrgentOutlineBrush;
        private Brush intelClearedOutlineBrush;
        private Brush intelStaleOutlineBrush;
        private Brush intelHistoryOutlineBrush;
        private Brush sysFillBrush;
        private Brush outOfRegionSysFillBrush;
        private Brush intelFillBrush;
        private Brush jumpLineBrush;
        private Brush bridgeLineBrush;
        private Brush routeLineBrush;
        private Brush transparentBrush;

        private Brush toolTipBackgroundBrush;
        private Brush toolTipForegroundBrush;

        private int overlayDepth = 8;
        private Dictionary<LocalCharacter, OverlaySystemData> currentPlayersSystemData = new ();
        private OverlaySystemData currentPlayerSystemData;
        private OverlayCanvasData canvasData = new OverlayCanvasData();

        private float intelUrgentPeriod = 300;
        private float intelStalePeriod = 300;
        private float intelHistoryPeriod = 600;

        private float overlaySystemSizeGatherer = 20f;
        private float overlaySystemSizeHunter = 5f;
        private float overlayCurrentSystemSizeHunterModifier = 3f;
        private float overlayAdditionalCharacterSystemSizeHunterModifier = 2f;

        private float overlayIntelOversize = 10f;
        private float CalculatedOverlayIntelOversize { get => overlayIntelOversize; }

        private bool gathererMode = true;
        private bool gathererModeIncludesAdjacentRegions = false;
        private bool hunterModeShowFullRegion = false;

        private bool showNPCKillData = true;
        private float npcKillDeltaMaxSize = 40f;
        private float npcKillDeltaMaxEqualsKills = 500f;

        private bool showNPCKillDeltaData = true;
        private bool showCharName = true;
        private bool showCharLocation = true;
        private bool showJumpBridges = true;
        private bool showSystemNames = false;
        private bool showAllCharacterNames = false;
        private bool individualCharacterWindows = false;

        private DispatcherTimer locationUpdateTimer = new DispatcherTimer();
        private DispatcherTimer dataUpdateTimer = new DispatcherTimer();

        private DoubleCollection dashStroke = new DoubleCollection(new List<double> { 2, 2 });

        private DoubleAnimation dashAnimation;

        private Dictionary<string, bool> regionMirrorVectors = new Dictionary<string, bool>();

        public LocalCharacter OverlayCharacter = null;

        public Overlay(MainWindow mw)
        {
            InitializeComponent();

            this.Background.Opacity = mw.MapConf.OverlayBackgroundOpacity;
            overlay_Canvas.Opacity = mw.MapConf.OverlayOpacity;
            gathererMode = mw.MapConf.OverlayGathererMode;
            hunterModeShowFullRegion = mw.MapConf.OverlayHunterModeShowFullRegion;
            showCharName = mw.MapConf.OverlayShowCharName;
            showCharLocation = mw.MapConf.OverlayShowCharLocation;

            // Set up all the brushes
            sysOutlineBrush = new SolidColorBrush(Colors.DarkGray);
            sysOutlineBrush.Opacity = 1f;

            sysTheraOutlineBrush = new SolidColorBrush(mw.MapConf.ActiveColourScheme.TheraEntranceSystem);
            sysTheraOutlineBrush.Opacity = 1f;

            sysOutlineBrush = new SolidColorBrush(Colors.DarkGray);
            sysOutlineBrush.Opacity = 1f;

            npcKillDataBrush = new SolidColorBrush(mw.MapConf.ActiveColourScheme.ESIOverlayColour);
            npcKillDataBrush.Opacity = 1f;

            Color npcDeltaColor = mw.MapConf.ActiveColourScheme.ESIOverlayColour;
            npcDeltaColor.R = (byte)(npcDeltaColor.R * 0.4);
            npcDeltaColor.G = (byte)(npcDeltaColor.G * 0.4);
            npcDeltaColor.B = (byte)(npcDeltaColor.B * 0.4);

            npcKillDeltaDataBrush = new SolidColorBrush(npcDeltaColor);
            npcKillDeltaDataBrush.Opacity = 1f;

            outOfRegionSysFillBrush = new SolidColorBrush(Colors.Black);
            outOfRegionSysFillBrush.Opacity = 1f;

            Color outOfRegionOutline = Colors.Red;
            outOfRegionOutline.R = (byte)(outOfRegionOutline.R * 0.4);
            outOfRegionOutline.G = (byte)(outOfRegionOutline.G * 0.4);
            outOfRegionOutline.B = (byte)(outOfRegionOutline.B * 0.4);

            outOfRegionSysOutlineBrush = new SolidColorBrush(outOfRegionOutline);
            outOfRegionSysOutlineBrush.Opacity = 1f;

            sysLocationOutlineBrush = new SolidColorBrush(Colors.Orange);
            sysLocationOutlineBrush.Opacity = 1f;

            intelUrgentOutlineBrush = new SolidColorBrush(Colors.Red);
            intelUrgentOutlineBrush.Opacity = 1f;

            intelClearedOutlineBrush = new SolidColorBrush(mw.MapConf.ActiveColourScheme.IntelClearOverlayColour);
            intelClearedOutlineBrush.Opacity = 1f;

            intelStaleOutlineBrush = new SolidColorBrush(Colors.Yellow);
            intelStaleOutlineBrush.Opacity = 1f;

            intelHistoryOutlineBrush = new SolidColorBrush(Colors.LightGray);
            intelHistoryOutlineBrush.Opacity = 1f;

            sysFillBrush = new SolidColorBrush(Colors.Gray);
            sysFillBrush.Opacity = 1f;

            intelFillBrush = new SolidColorBrush(Colors.Red);
            intelFillBrush.Opacity = 0.25f;

            jumpLineBrush = new SolidColorBrush(Colors.White);
            jumpLineBrush.Opacity = 0.75f;

            bridgeLineBrush = new SolidColorBrush(mw.MapConf.ActiveColourScheme.FriendlyJumpBridgeColour);
            bridgeLineBrush.Opacity = 0.75f;

            routeLineBrush = new SolidColorBrush(Colors.Yellow);
            routeLineBrush.Opacity = 0.75f;

            transparentBrush = new SolidColorBrush(Colors.White);
            transparentBrush.Opacity = 0f;

            toolTipBackgroundBrush = new SolidColorBrush(Colors.Black);
            toolTipForegroundBrush = new SolidColorBrush(Colors.DarkGray);

            mainWindow = mw;
            if (mainWindow == null) return;

            if (mainWindow.ActiveCharacter != null)
            {
                OverlayCharacter = mw.ActiveCharacter;
            }

            // Set up some events
            mainWindow.OnSelectedCharChangedEventHandler += SelectedCharChanged;
            mainWindow.MapConf.PropertyChanged += OverlayConf_PropertyChanged;
            SizeChanged += OnSizeChanged;
            Closing += Overlay_Closing;
            // We can only redraw stuff when the canvas is actually resized, otherwise dimensions will be wrong!
            overlay_Canvas.SizeChanged += OnCanvasSizeChanged;
            
            // Set up hotkeys
            HotkeyManager.Current.AddOrReplace("Toggle click trough overlay windows.", Key.T, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift, OnClickTroughToggle);

            // Update settings
            intelUrgentPeriod = mainWindow.MapConf.IntelFreshTime;
            intelStalePeriod = mainWindow.MapConf.IntelStaleTime;
            intelHistoryPeriod = mainWindow.MapConf.IntelHistoricTime;
            overlayDepth = mainWindow.MapConf.OverlayRange + 1;
            showJumpBridges = mainWindow.MapConf.OverlayShowJumpBridges;
            showSystemNames = mainWindow.MapConf.OverlayShowSystemNames;
            showAllCharacterNames = mainWindow.MapConf.OverlayShowAllCharacterNames;
            individualCharacterWindows = mainWindow.MapConf.OverlayIndividualCharacterWindows;

            // Initialize value animation to be used by dashed lines
            dashAnimation = new DoubleAnimation();
            dashAnimation.From = 20;
            dashAnimation.To = 0;
            dashAnimation.By = 2;
            dashAnimation.Duration = new Duration(TimeSpan.FromSeconds(10));
            dashAnimation.RepeatBehavior = RepeatBehavior.Forever;
            Timeline.SetDesiredFrameRate(dashAnimation, 20);

            RefreshButtonStates();

            // TODO: Add better handling for new intel events.
            // mw.EVEManager.IntelAddedEvent += OnIntelAdded;

            // Start the magic
            ToggleClickTrough(mainWindow.OverlayWindowsAreClickTrough);
            RefreshCurrentView();

            locationUpdateTimer.Interval = TimeSpan.FromMilliseconds(250);
            locationUpdateTimer.Tick += UpdatePlayerLocations;
            
            dataUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            dataUpdateTimer.Tick += UpdateDataOverlay;
            
            locationUpdateTimer.Start();
            dataUpdateTimer.Start();
        }

        private void OnClickTroughToggle(object sender, HotkeyEventArgs e)
        {
            mainWindow.OverlayWindow_ToggleClickTrough();
        }

        public void ToggleClickTrough(bool isClickTrough)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            
            if (isClickTrough)
            {
                WindowsServices.SetWindowExTransparent(hwnd);
                overlay_ButtonRow.Height = new GridLength(0);
                this.ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                WindowsServices.SetWindowExNotTransparent(hwnd);
                overlay_ButtonRow.Height = new GridLength(20);
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            LoadOverlayWindowPosition();
        }

        /// <summary>
        /// Calculates the actual size for the canvas element that represents a system.
        /// Takes into account if it is the current system the player is in.
        /// </summary>
        /// <param name="systemName">The name of the system for which the size should be calculated.</param>
        /// <returns>The size for the canvas element for the system.</returns>
        private float CalculatedOverlaySystemSize(string systemName)
        {
            if (gathererMode)
                return overlaySystemSizeGatherer;

            if (systemName == currentPlayersSystemData[OverlayCharacter]?.system?.Name)
                return overlaySystemSizeHunter * overlayCurrentSystemSizeHunterModifier;

            if (currentPlayersSystemData.Any(s => s.Value.system.Name == systemName))
            {
                return overlaySystemSizeHunter * overlayAdditionalCharacterSystemSizeHunterModifier;
            }

            return overlaySystemSizeHunter;
        }

        /// <summary>
        /// The UI buttons that toggle different states will be exchanged based
        /// on the value they are representing. This saves a few lines of xaml code
        /// over doing it properly.
        /// </summary>
        private void RefreshButtonStates()
        {
            overlay_HunterButton.Visibility = gathererMode ? Visibility.Collapsed : Visibility.Visible;
            overlay_GathererButton.Visibility = gathererMode ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Cleans up and then redraws the current overlay map.
        /// This is called whenever significant data changes.
        /// </summary>
        private void RefreshCurrentView()
        {
            canvasData.SetDimensions(overlay_Canvas.RenderSize.Width, overlay_Canvas.RenderSize.Height);
            canvasData.mapBorderBottomExtension = showSystemNames;
            UpdatePlayerInformationText();
            UpdateSystemList();
            UpdateIntelDataCoordinates();
            UpdateRouteData();
        }

        /// <summary>
        /// If the player is in a system without informations about the surroundings
        /// like wormholes or abyssal, this will be called to wipe the overlay canvas
        /// clean. Otherwise the last drawn map would persist.
        /// </summary>
        private void ClearView()
        {
            UpdatePlayerInformationText();
            foreach (var sD in systemData)
            {
                sD.Value.CleanUpCanvas(overlay_Canvas);
            }
            systemData.Clear();

            foreach (var line in jumpLines)
            {
                overlay_Canvas.Children.Remove(line);
            }
            jumpLines.Clear();

            foreach (var line in routeLines)
            {
                overlay_Canvas.Children.Remove(line);
            }
            routeLines.Clear();
        }

        /// <summary>
        /// Fetches the position and size values from the preferences.
        /// </summary>
        private void LoadOverlayWindowPosition()
        {
            string currentPlacementString = Properties.Settings.Default.OverlayWindow_placement;
            string currentCharacterName = OverlayCharacter == null ? "_" : OverlayCharacter.Name;
            string currentPlacement = "";
            if (currentPlacementString.Contains("|ID|"))
            {
                string[] currentPlacementStringParts = currentPlacementString.Split("|CHAR|");
                foreach (string currentCharacterPlacementString in currentPlacementStringParts)
                {
                    string[] currentCharacterPlacementStringParts = currentCharacterPlacementString.Split("|ID|");
                    if (currentCharacterPlacementStringParts[0] == currentCharacterName)
                    {
                        currentPlacement = currentCharacterPlacementStringParts[1];
                        break;
                    }
                    
                }
            }
            else
            {
                currentPlacement = currentPlacementString;
            }

            if (currentPlacement != "")
            {
                WindowPlacement.SetPlacement(new WindowInteropHelper(this).Handle, currentPlacement);   
            }
        }

        private void CleanupAndUpdateOverlayWindowPosition(string characterName)
        {
            string currentPlacementString = Properties.Settings.Default.OverlayWindow_placement;
            string[] currentCharacterPlacements = currentPlacementString.Split("|CHAR|");
            List<string> validCharacterPlacements = new List<string>();

            string actualWindowPlacement = (OverlayCharacter == null ? "_" : OverlayCharacter.Name) + "|ID|" + WindowPlacement.GetPlacement(new WindowInteropHelper(this).Handle);
            bool overlayCharInlist = false;
            
            foreach (string placementString in currentCharacterPlacements)
            {
                string[] placementStringParts = placementString.Split("|ID|");
                if (mainWindow.EVEManager.LocalCharacters.Any(c => c.Name == placementStringParts[0]) || placementStringParts[0] == "_")
                {
                    if (placementStringParts[0] == characterName)
                    {
                        validCharacterPlacements.Add(actualWindowPlacement);
                        overlayCharInlist = true;
                    }
                    else
                    {
                        validCharacterPlacements.Add(placementString);   
                    }
                }
            }

            if (!overlayCharInlist)
            {
                validCharacterPlacements.Add(actualWindowPlacement);
            }
            
            Properties.Settings.Default.OverlayWindow_placement = String.Join("|CHAR|", validCharacterPlacements);
            Properties.Settings.Default.Save();
        }
        
        /// <summary>
        /// Stores the position and size values to the preferences.
        /// </summary>
        private void StoreOverlayWindowPosition()
        {
            CleanupAndUpdateOverlayWindowPosition(OverlayCharacter == null ? "_" : OverlayCharacter.Name);
        }

        private void Overlay_Closing(object sender, CancelEventArgs e)
        {
            StoreOverlayWindowPosition();
        }

        private void UpdateCharacterData()
        {
            if (OverlayCharacter != null)
            {
                if (mainWindow.EVEManager.LocalCharacters.Any(c => c == OverlayCharacter))
                {
                    OverlayCharacter = mainWindow.EVEManager.LocalCharacters.First(c => c == OverlayCharacter);
                }
            }
        }

        private void UpdatePlayerLocations(object sender, EventArgs e)
        {
            if (OverlayCharacter != null)
            {
                if (currentPlayersSystemData[OverlayCharacter].system == null)
                {
                    RefreshCurrentView();
                }
                else if (OverlayCharacter.Location != currentPlayersSystemData[OverlayCharacter].system.Name || routeLines.Count > 0 && (routeLines.Count != OverlayCharacter.ActiveRoute.Count - 1))
                {
                    RefreshCurrentView();
                }
                else
                {
                    foreach (LocalCharacter additionalCharacter in mainWindow.EVEManager.LocalCharacters)
                    {
                        if (additionalCharacter != OverlayCharacter)
                        {
                            if (additionalCharacter.Location != currentPlayersSystemData[additionalCharacter].system.Name)
                            {
                                RefreshCurrentView();
                                break;
                            }
                        }
                    }   
                }
            }
        }

        private void UpdateDataOverlay(object sender, EventArgs e)
        {
            try
            {
                UpdateIntelData();
                if (!gathererMode && (showNPCKillData || showNPCKillDeltaData)) UpdateNPCKillData();
                UpdateRouteData();
            }
            catch (Exception)
            {
            }
        }

        private void UpdateNPCKillData()
        {
            foreach (var sysData in systemData)
            {
                DrawNPCKillsToOverlay(sysData.Value);
            }
        }

        /// <summary>
        /// Fetches the data from the manager and updates existing entries or
        /// creates new ones which hold the intel data.
        /// </summary>
        private void UpdateIntelData()
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
                if (intelData.Any(d => d.data.RawIntelString == intelDataset.RawIntelString))
                    continue;

                // Check if it is intel for an already existing system, throw out older entries.
                List<(EVEData.IntelData data, List<Ellipse> ellipse)> deleteList = new List<(EVEData.IntelData, List<Ellipse>)>();
                bool skipIntel = false;

                foreach (string intelSystem in intelDataset.Systems)
                {
                    foreach (var existingIntelDataset in intelData)
                    {
                        if (existingIntelDataset.data.Systems.Any(s => s == intelSystem))
                        {
                            if (existingIntelDataset.data.IntelTime < intelDataset.IntelTime)
                            {
                                deleteList.Add(existingIntelDataset);
                            }
                            else
                            {
                                skipIntel = true;
                            }
                        }
                    }
                }
                if (skipIntel) continue;

                foreach (var deleteEntry in deleteList)
                {
                    foreach (Ellipse intelShape in deleteEntry.ellipse)
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
                if ((DateTime.Now - intelDataEntry.data.IntelTime).TotalSeconds > intelLifetime)
                {
                    // If the intel data is past its lifetime, delete the shapes from the canvas.
                    foreach (Ellipse intelShape in intelDataEntry.ellipse)
                    {
                        overlay_Canvas.Children.Remove(intelShape);
                    }
                }
            }

            // Remove all expired entries.
            intelData.RemoveAll(d => (DateTime.Now - d.data.IntelTime).TotalSeconds > intelLifetime);

            // Loop over all remaining, therfore current, entries.
            foreach (var intelDataEntry in intelData)
            {
                // If there are no shapes, add them.
                if (intelDataEntry.Item2.Count == 0)
                {
                    foreach (string systemName in intelDataEntry.data.Systems)
                    {
                        intelDataEntry.ellipse.Add(DrawIntelToOverlay(systemName));
                    }
                }

                // Fill or update ToolTips
                foreach (string systemName in intelDataEntry.data.Systems)
                {
                    if (!systemData.ContainsKey(systemName)) continue;
                    if (systemData[systemName].intelData == null || systemData[systemName].intelData != intelDataEntry.data)
                    {
                        systemData[systemName].intelData = intelDataEntry.data;
                    }
                }

                float intelAgeInSeconds = (float)(DateTime.Now - intelDataEntry.data.IntelTime).TotalSeconds;

                // Update the style.
                foreach (Ellipse intelShape in intelDataEntry.ellipse)
                {
                    switch (intelAgeInSeconds)
                    {
                        case float age when age < intelUrgentPeriod:
                            intelShape.Stroke = intelDataEntry.data.ClearNotification ? intelClearedOutlineBrush : intelUrgentOutlineBrush;
                            intelShape.Fill = intelFillBrush;
                            break;

                        case float age when age < intelUrgentPeriod + intelStalePeriod:
                            intelShape.Stroke = intelDataEntry.data.ClearNotification ? intelClearedOutlineBrush : intelStaleOutlineBrush;
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

        /// <summary>
        /// Displays the current route in the hunter mode overlay.
        /// </summary>
        private void UpdateRouteData()
        {
            UpdateCharacterData();
            
            // clear all
            while (routeLines.Count > 0)
            {
                overlay_Canvas.Children.Remove(routeLines[^1]);
                routeLines.Remove(routeLines[^1]);
            }

            if (!gathererMode && mainWindow.MapConf.OverlayShowRoute && OverlayCharacter != null && OverlayCharacter.Waypoints.Count > 0)
            {
                List<RoutePoint> routePoints = OverlayCharacter.ActiveRoute.ToList();

                if (routePoints.Count < 2) return;

                for (int i = 1; i < routePoints.Count; i++)
                {
                    RoutePoint segmentStart = routePoints[i - 1];
                    RoutePoint segmentEnd = routePoints[i];

                    if ( systemData.ContainsKey(segmentStart.SystemName) && segmentStart.GateToTake == GateType.Ansiblex )
                    {
                        Path jumpBridgePath = new Path();
                        jumpBridgePath.Data = GenerateJumpBridgePathGeometry(segmentStart.SystemName, segmentEnd.SystemName);

                        routeLines.Add(jumpBridgePath);

                        routeLines[i - 1].Stroke = jumpLineBrush;
                    }
                    else
                    {
                        routeLines.Add(new Line());

                        if (!systemData.ContainsKey(segmentStart.SystemName) || !systemData.ContainsKey(segmentEnd.SystemName)) continue;

                        Vector2 segmentStartCanvasCoordinate = canvasData.CoordinateToCanvas(systemData[segmentStart.SystemName].mapSystemCoordinate);
                        Vector2 segmentEndCanvasCoordinate = canvasData.CoordinateToCanvas(systemData[segmentEnd.SystemName].mapSystemCoordinate);

                        ((Line)routeLines[i - 1]).X1 = segmentStartCanvasCoordinate.X;
                        ((Line)routeLines[i - 1]).Y1 = segmentStartCanvasCoordinate.Y;
                        ((Line)routeLines[i - 1]).X2 = segmentEndCanvasCoordinate.X;
                        ((Line)routeLines[i - 1]).Y2 = segmentEndCanvasCoordinate.Y;

                        routeLines[i - 1].Stroke = routeLineBrush;
                    }                    
                    
                    routeLines[i - 1].StrokeThickness = 4;
                    routeLines[i - 1].StrokeDashArray = new DoubleCollection(new List<double> { 1.0, 1.0 });
                    routeLines[i - 1].BeginAnimation(Shape.StrokeDashOffsetProperty, dashAnimation);

                    Canvas.SetZIndex(routeLines[i - 1], 95);
                    overlay_Canvas.Children.Add(routeLines[i - 1]);
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
                    if (systemData.ContainsKey(intelSystem))
                    {
                        Vector2 systemCoordinate = systemData[intelSystem].canvasCoordinate;
                        intelSystemCoordinateVector = new Vector2f(systemCoordinate.X - (CalculatedOverlayIntelOversize / 2f), systemCoordinate.Y - (CalculatedOverlayIntelOversize / 2f));
                        visible = true;
                    }
                    else
                    {
                        intelSystemCoordinateVector = new Vector2f(0, 0);
                    }

                    // Update the shape associated with the current intel entry.
                    if (intelDataEntry.ellipse.Count > i)
                    {
                        intelDataEntry.ellipse[i].Width = CalculatedOverlaySystemSize(intelSystem) + CalculatedOverlayIntelOversize;
                        intelDataEntry.ellipse[i].Height = CalculatedOverlaySystemSize(intelSystem) + CalculatedOverlayIntelOversize;
                        Canvas.SetLeft(intelDataEntry.ellipse[i], intelSystemCoordinateVector.x);
                        Canvas.SetTop(intelDataEntry.ellipse[i], intelSystemCoordinateVector.y);
                        // If a system is not on the current map, hide it. Can be made visible later.
                        intelDataEntry.ellipse[i].Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                    }

                    // Since all children are deleted when resizing the canvas, we readd the intel shapes.
                    if (intelDataEntry.ellipse.Count > 0)
                    {
                        if (!overlay_Canvas.Children.Contains(intelDataEntry.ellipse[i])) overlay_Canvas.Children.Add(intelDataEntry.ellipse[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Handles refreshing the tooltip data only when the tooltip is opening
        /// to avoid unnecessary overhead.
        /// </summary>
        /// <param name="sender">The element for which the tooltip is opening.</param>
        /// <param name="e"></param>
        public void UpdateSystemTooltipOnOpeningTooltip(object sender, RoutedEventArgs e)
        {
            OverlaySystemData tooltipSystemData = systemData.FirstOrDefault(x => x.Value.systemCanvasElement == (Shape)sender).Value;
            if (tooltipSystemData != null)
            {
                UpdateSystemTooltip(tooltipSystemData);
            }            
        }

        /// <summary>
        /// Updates the tooltip content of a specific system.
        /// </summary>
        /// <param name="systemData"></param>
        public void UpdateSystemTooltip(OverlaySystemData systemData)
        {
            if (systemData.systemCanvasElement.ToolTip == null)
            {
                systemData.systemCanvasElement.ToolTip = new ToolTip();
                ToolTipService.SetInitialShowDelay(systemData.systemCanvasElement, 0);
            }

            // Todo: NPC Kills == 0, delta == 0, jumps, hunter/gatherer
            string toolTipText = $"{systemData.system.Name} ({systemData.system.TrueSec.ToString("n2")})";
            if (!gathererMode) toolTipText += $"\nNPC Kills: {systemData.system.NPCKillsLastHour}\nDelta: {systemData.system.NPCKillsDeltaLastHour}";
            if (systemData.intelData != null) toolTipText += $"\nReported: {systemData.intelData.RawIntelString}";
            var jumpBridges = mainWindow.EVEManager.JumpBridges.Where(t => t.From == systemData.system.Name || t.To == systemData.system.Name);
            if (jumpBridges.Any()) {
                foreach ( var jumpBridge in jumpBridges )
                {
                    toolTipText += $"\nBridge: {jumpBridge.From} - {jumpBridge.To}";
                }
            }
            // Todo: Add Thera

            ((ToolTip)systemData.systemCanvasElement.ToolTip).Content = toolTipText;
            ((ToolTip)systemData.systemCanvasElement.ToolTip).Opacity = 0.75;
            ((ToolTip)systemData.systemCanvasElement.ToolTip).Background = toolTipBackgroundBrush;
            ((ToolTip)systemData.systemCanvasElement.ToolTip).Foreground = toolTipForegroundBrush;            
        }

        /// <summary>
        /// Updates and draws all systems that are in a certain range from the current
        /// system the player is in.
        /// </summary>
        /// TODO: Make range a global setting.
        private void UpdateSystemList()
        {
            // Cleanup
            canvasData.ResetExtends();

            List<string> systemsInList = new List<string>();

            // If there is no main window or no selected character, abort.
            if (mainWindow == null || OverlayCharacter == null) return;
            UpdateCharacterData();

            // Gather data
            currentPlayersSystemData.Clear();
            string currentLocation = OverlayCharacter.Location;
            EVEData.System currentSystem = mainWindow.EVEManager.GetEveSystem(currentLocation);
            currentPlayersSystemData.Add(OverlayCharacter, new OverlaySystemData(currentSystem));
            foreach (LocalCharacter additionalChar in mainWindow.EVEManager.LocalCharacters)
            {
                if (!currentPlayersSystemData.ContainsKey(additionalChar))
                    currentPlayersSystemData.Add(additionalChar, new OverlaySystemData(mainWindow.EVEManager.GetEveSystem(additionalChar.Location)));
            }

            // Bail out if the system does not exist. I.e. wormhole systems.
            if (currentSystem == null)
            {
                //on your way out, mop up everything thats left
                ClearView();
                return;
            }

            List<List<OverlaySystemData>> hierarchie = new List<List<OverlaySystemData>>();

            // If gatherer mode or not set to full region, collect systems in range.
            if (gathererMode || (!gathererMode && !hunterModeShowFullRegion))
            {
                // Add the players location to the hierarchie.
                hierarchie.Add(new List<OverlaySystemData>() { currentPlayersSystemData[OverlayCharacter] });
                if (!systemData.ContainsKey(currentPlayersSystemData[OverlayCharacter].system.Name))
                {
                    systemData.Add(currentPlayersSystemData[OverlayCharacter].system.Name, currentPlayersSystemData[OverlayCharacter]);
                }
                canvasData.UpdateUnscaledExtends(mainWindow.EVEManager.GetRegion(currentPlayersSystemData[OverlayCharacter].system.Region).MapSystems[currentPlayersSystemData[OverlayCharacter].system.Name].Layout);

                // Track which systems are already in the list to avoid doubles.
                systemsInList.Add(currentSystem.Name);

                currentPlayersSystemData[OverlayCharacter].mapSystemCoordinate = mainWindow.EVEManager.GetRegion(currentSystem.Region).MapSystems[currentSystem.Name].Layout;
                canvasData.currentOriginCoordinates = currentPlayersSystemData[OverlayCharacter].mapSystemCoordinate;
                systemData[currentPlayersSystemData[OverlayCharacter].system.Name].mapSystemCoordinate = currentPlayersSystemData[OverlayCharacter].mapSystemCoordinate;

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

                            if (gathererMode == true || targetRegion == currentPlayersSystemData[OverlayCharacter].system.Region || sourceRegion == currentPlayersSystemData[OverlayCharacter].system.Region || (regionMirrorVectors.ContainsKey(targetRegion) && gathererModeIncludesAdjacentRegions))
                            {
                                // Only add the system if it was not yet added.
                                if (!systemsInList.Contains(jump))
                                {
                                    Vector2 originSystemCoord = mainWindow.EVEManager.GetRegion(previousDepthSystem.system.Region).MapSystems[previousDepthSystem.system.Name].Layout;
                                    Vector2 jumpSystemCoord = mainWindow.EVEManager.GetRegion(previousDepthSystem.system.Region).MapSystems[jump].Layout;

                                    Vector2 systemConnection = (jumpSystemCoord - originSystemCoord);

                                    Vector2 originOffset = previousDepthSystem.offsetCoordinate + systemConnection;

                                    canvasData.UpdateUnscaledExtends(jumpSystemCoord);

                                    currentDepth.Add(new OverlaySystemData(mainWindow.EVEManager.GetEveSystem(jump), originOffset));
                                    systemsInList.Add(jump);
                                    if (!systemData.ContainsKey(jump))
                                    {
                                        systemData.Add(jump, new OverlaySystemData(jumpSystem));
                                    }

                                    systemData[jump].mapSystemCoordinate = jumpSystemCoord;
                                }
                            }
                        }
                    }
                    hierarchie.Add(currentDepth);
                }
            }
            else
            {
                List<OverlaySystemData> hunterSystems = new List<OverlaySystemData>();

                MapRegion mr = mainWindow.EVEManager.GetRegion(currentSystem.Region);
                foreach (MapSystem ms in mr.MapSystems.Values)
                {
                    Vector2 newCoords = ms.Layout;
                    if (!systemData.ContainsKey(ms.Name))
                    {
                        systemData.Add(ms.Name, new OverlaySystemData(ms.ActualSystem));
                    }
                    canvasData.UpdateUnscaledExtends(ms.Layout);
                    systemsInList.Add(ms.Name);
                    hunterSystems.Add(new OverlaySystemData(ms.ActualSystem, newCoords));
                    systemData[ms.Name].mapSystemCoordinate = newCoords;
                }

                hierarchie.Add(hunterSystems);
            }

            List<string> deleteSystems = new List<string>();
            foreach (var overlaySystemData in systemData)
            {
                if (!systemsInList.Contains(overlaySystemData.Key))
                {
                    deleteSystems.Add(overlaySystemData.Key);
                }
            }

            foreach (string sysName in deleteSystems)
            {
                // Clean up the canvas
                systemData[sysName].CleanUpCanvas(overlay_Canvas);
                systemData.Remove(sysName);
            }

            // Draw the systems.
            for (int i = 0; i < hierarchie.Count; i++)
            {
                DrawSystemsToOverlay(i, hierarchie[i], hierarchie.Count);
            }

            // Add the jump bridges, if enabled
            if (showJumpBridges) DrawJumpBridgesToOverlay(systemsInList);

            // Add the system connections.
            DrawJumpsToOverlay(systemsInList);
        }


        /// <summary>
        /// If systems at the end of jump bridges are not on the map or not in the region
        /// this method gives an offscreen coordinate to draw a line anyway.
        /// </summary>
        /// <param name="originSystem"></param>
        /// <param name="targetSystem"></param>
        /// <returns></returns>
        private Vector2 OffMapConnection (string originSystem, string targetSystem)
        {
            Vector2 originSystemPosition;
            Vector2 targetSystemPosition;

            var originRegion = mainWindow.EVEManager.GetRegion(mainWindow.EVEManager.GetEveSystem(originSystem).Region);
            var targetRegion = mainWindow.EVEManager.GetRegion(mainWindow.EVEManager.GetEveSystem(targetSystem).Region);

            if ( originRegion == targetRegion )
            {
                originSystemPosition = mainWindow.EVEManager.GetRegion(mainWindow.EVEManager.GetEveSystem(originSystem).Region).MapSystems[originSystem].Layout;
                targetSystemPosition = mainWindow.EVEManager.GetRegion(mainWindow.EVEManager.GetEveSystem(targetSystem).Region).MapSystems[targetSystem].Layout;
            } 
            else
            {
                originSystemPosition = new Vector2((float)originRegion.UniverseViewX, (float)originRegion.UniverseViewY);
                targetSystemPosition = new Vector2((float)targetRegion.UniverseViewX, (float)targetRegion.UniverseViewY);
            }               

            Vector2 offMapDirection = Vector2.Normalize(targetSystemPosition - originSystemPosition);

            Vector2 result = systemData[originSystem].canvasCoordinate + (offMapDirection * 2f * Math.Max(canvasData.dimensions.X, canvasData.dimensions.Y) );

            return result;
        }

        /// <summary>
        /// This draws the jump bridge connections to the overlay. Out of region connections go off screen.
        /// </summary>
        /// <param name="systemsInList"></param>
        private void DrawJumpBridgesToOverlay(List<string> systemsInList)
        {
            List<string> alreadyConnected = new List<string>();

            foreach (OverlaySystemData overlaySystemData in bridgeSystems)
            {
                overlaySystemData.CleanUpJumpBridges(overlay_Canvas);
            }
            bridgeSystems.Clear();

            foreach (var jumpBridge in mainWindow.EVEManager.JumpBridges)
            {
                // Only draw a line if neither directions of the bridge have been drawn before.
                if ( !alreadyConnected.Contains($"{jumpBridge.To} {jumpBridge.From}") && !alreadyConnected.Contains($"{jumpBridge.From} {jumpBridge.To}" )) {

                    // Only draw a line if at least one system is visible.
                    if ( systemData.ContainsKey(jumpBridge.From) || systemData.ContainsKey(jumpBridge.To))
                    {
                        OverlaySystemData visibleSystemData = null;
                        string otherEndSystemName = "";

                        if (systemData.ContainsKey(jumpBridge.From))
                        {
                            visibleSystemData = systemData[jumpBridge.From];
                            otherEndSystemName = jumpBridge.To;
                        }                     
                        
                        if (systemData.ContainsKey(jumpBridge.To))
                        {
                            visibleSystemData = systemData[jumpBridge.To];
                            otherEndSystemName = jumpBridge.From;
                        }                 

                        if (visibleSystemData == null) continue;

                        PathGeometry pathGeometry = GenerateJumpBridgePathGeometry(jumpBridge.From, jumpBridge.To);

                        Path path;

                        if (visibleSystemData.jumpBridgePath != null) path = visibleSystemData.jumpBridgePath;
                        else path = new Path();

                        path.Data = pathGeometry;
                        path.Fill = Brushes.Transparent;
                        path.Stroke = bridgeLineBrush;
                        path.StrokeDashArray = dashStroke;
                        path.StrokeThickness = gathererMode ? 2 : 1;

                        visibleSystemData.jumpBridgePath = path;
                        visibleSystemData.jumpBridgeTarget = otherEndSystemName;
                        bridgeSystems.Add(visibleSystemData);                        

                        if ( systemData.ContainsKey(otherEndSystemName))
                        {
                            systemData[otherEndSystemName].jumpBridgePath = path;
                            systemData[otherEndSystemName].jumpBridgeTarget = visibleSystemData.system.Name;
                            bridgeSystems.Add(systemData[otherEndSystemName]);
                        }

                        if (!overlay_Canvas.Children.Contains(path))
                        {
                            overlay_Canvas.Children.Add(path);
                        }
                        Canvas.SetZIndex(path, 25);
                    }

                    alreadyConnected.Add($"{jumpBridge.From} {jumpBridge.To}");
                }
            }
        }

        private PathGeometry GenerateJumpBridgePathGeometry(string fromSystemName, string toSystemName)
        {
            ArcSegment connectionArc = new ArcSegment();
            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();

            Vector2 fromCoordinate;
            Vector2 toCoordinate;
            string otherEndSystemName = "";

            if (systemData.ContainsKey(fromSystemName))
            {
                fromCoordinate = systemData[fromSystemName].canvasCoordinate;
                otherEndSystemName = toSystemName;
            }
            else
            {
                fromCoordinate = OffMapConnection(toSystemName, fromSystemName);
            }

            if (systemData.ContainsKey(toSystemName))
            {
                toCoordinate = systemData[toSystemName].canvasCoordinate;
                otherEndSystemName = fromSystemName;
            }
            else
            {
                toCoordinate = OffMapConnection(fromSystemName, toSystemName);
            }

            Vector2f fromVector = new Vector2f(fromCoordinate.X + (CalculatedOverlaySystemSize(fromSystemName) / 2f),
                fromCoordinate.Y + (CalculatedOverlaySystemSize(fromSystemName) / 2f));
            Vector2f toVector = new Vector2f(toCoordinate.X + (CalculatedOverlaySystemSize(toSystemName) / 2f),
                toCoordinate.Y + (CalculatedOverlaySystemSize(toSystemName) / 2f));

            Vector2f bridgeConnection = toVector == fromVector ? new Vector2f(0, 0) : Vector2f.Normalize(toVector - fromVector);

            Vector2f fromCorrected = fromVector + (bridgeConnection);// * (int)(CalculatedOverlaySystemSize(jumpBridge.From) / 2f));
            Vector2f toCorrected = toVector - (bridgeConnection);// * (int)(CalculatedOverlaySystemSize(jumpBridge.To) / 2f));

            pathFigure.StartPoint = new Point(fromCorrected.x, fromCorrected.y);
            pathFigure.Segments.Add(
                    new ArcSegment(
                        new Point(toCorrected.x, toCorrected.y),
                        new Size(20, 100),
                        MathF.Atan2(bridgeConnection.x, bridgeConnection.y) * 57.295779513f * -1f,
                        false,
                        SweepDirection.Clockwise,
                        true
                    )
                );

            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
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
            int currentJumpIndex = 0;

            foreach (Line jLine in jumpLines)
            {
                overlay_Canvas.Children.Remove(jLine);
            }
            jumpLines.Clear();

            foreach (string systemName in systemsInList)
            {
                // (string, double, double) currentCoordinate = systemCoordinates.Where(s => s.Item1 == systemName).First();
                Vector2 currentCoordinate = systemData[systemName].canvasCoordinate;
                EVEData.System currentSystem = mainWindow.EVEManager.GetEveSystem(systemName);

                // Iterate over all the connections.
                foreach (string connectedSystem in currentSystem.Jumps)
                {
                    // Only draw a connection if the target system is visible and if it was not yet connected.
                    if (systemData.ContainsKey(connectedSystem) && !alreadyConnected.Contains(connectedSystem))
                    {
                        // (string, double, double) connectedCoordinate = systemCoordinates.Where(s => s.Item1 == connectedSystem).First();
                        Vector2 connectedCoordinate = systemData[connectedSystem].canvasCoordinate;

                        Line connectionLine;

                        if (currentJumpIndex + 1 > jumpLines.Count)
                        {
                            connectionLine = new Line();
                            jumpLines.Add(connectionLine);
                            currentJumpIndex++;
                        }

                        Vector2f current = new Vector2f(currentCoordinate.X + (CalculatedOverlaySystemSize(currentSystem.Name) / 2f),
                            currentCoordinate.Y + (CalculatedOverlaySystemSize(currentSystem.Name) / 2f));
                        Vector2f connected = new Vector2f(connectedCoordinate.X + (CalculatedOverlaySystemSize(connectedSystem) / 2f),
                            connectedCoordinate.Y + (CalculatedOverlaySystemSize(connectedSystem) / 2f));

                        Vector2f currentToConnected = connected == current ? new Vector2f(0, 0) : Vector2f.Normalize(connected - current);

                        Vector2f currentCorrected = current + (currentToConnected * (int)(CalculatedOverlaySystemSize(currentSystem.Name) / 2f));
                        Vector2f connectedCorrected = connected - (currentToConnected * (int)(CalculatedOverlaySystemSize(connectedSystem) / 2f));

                        jumpLines[^1].X1 = currentCorrected.x;
                        jumpLines[^1].Y1 = currentCorrected.y;
                        jumpLines[^1].X2 = connectedCorrected.x;
                        jumpLines[^1].Y2 = connectedCorrected.y;
                        jumpLines[^1].Stroke = jumpLineBrush;
                        jumpLines[^1].StrokeThickness = gathererMode ? 2 : 1;

                        if (!overlay_Canvas.Children.Contains(jumpLines[^1]))
                        {
                            overlay_Canvas.Children.Add(jumpLines[^1]);
                        }
                        Canvas.SetZIndex(jumpLines[^1], 25);
                    }
                }

                alreadyConnected.Add(systemName);
            }

            // Remove unused lines from canvas and then delete
            if (currentJumpIndex > jumpLines.Count)
            {
                foreach (Line jLine in jumpLines.GetRange(currentJumpIndex, jumpLines.Count - currentJumpIndex))
                {
                    overlay_Canvas.Children.Remove(jLine);
                }
                jumpLines.RemoveRange(currentJumpIndex, jumpLines.Count - currentJumpIndex);
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
            double rowHeight = (canvasData.dimensions.Y - (canvasData.mapBorderBottomExtension ? canvasData.mapBorderBottomAdd : 0)) / maxDepth;
            double columnWidth = canvasData.dimensions.X / systems.Count;

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
                    Vector2 canvasCoordinate = canvasData.CoordinateToCanvas(systemData[systems[i].system.Name].mapSystemCoordinate);
                    left = canvasCoordinate.X;
                    top = canvasCoordinate.Y;
                }
                DrawSystemToOverlay(systems[i], left, top);
            }
        }

        /// <summary>
        /// Draws a single system to the canvas.
        /// </summary>
        /// <param name="sysData">The system to be drawn.</param>
        /// <param name="left">The position from the left edge.</param>
        /// <param name="top">The position from the top edge.</param>
        /// TODO: Make shape settings a global setting.
        /// TODO: Add more info to ToolTip or replace it with something else.
        private void DrawSystemToOverlay(OverlaySystemData sysData, double left, double top)
        {
            if (!systemData.ContainsKey(sysData.system.Name)) return;

            if (systemData[sysData.system.Name].systemCanvasElement == null)
            {
                if (mainWindow.EVEManager.JumpBridges.Any(t => t.From == sysData.system.Name))
                {
                    systemData[sysData.system.Name].systemCanvasElement = new Rectangle();
                }
                else
                {
                    systemData[sysData.system.Name].systemCanvasElement = new Ellipse();
                }
            } else
            {
                if (mainWindow.EVEManager.JumpBridges.Any(t => t.From == sysData.system.Name || t.To == sysData.system.Name)) {
                    if (systemData[sysData.system.Name].systemCanvasElement.GetType() == typeof(Ellipse))
                    {
                        systemData[sysData.system.Name].CleanUpCanvas(overlay_Canvas);
                        systemData[sysData.system.Name].systemCanvasElement = new Rectangle();
                    }
                } 
                else
                {
                    if (systemData[sysData.system.Name].systemCanvasElement.GetType() == typeof(Rectangle))
                    {
                        systemData[sysData.system.Name].CleanUpCanvas(overlay_Canvas);
                        systemData[sysData.system.Name].systemCanvasElement = new Ellipse();
                    }
                }                
            }
           
            systemData[sysData.system.Name].systemCanvasElement.Width = CalculatedOverlaySystemSize(sysData.system.Name);
            systemData[sysData.system.Name].systemCanvasElement.Height = CalculatedOverlaySystemSize(sysData.system.Name);
            systemData[sysData.system.Name].systemCanvasElement.StrokeThickness = gathererMode ? 2 : 1;

            if (sysData.system.Name == currentPlayersSystemData[OverlayCharacter].system.Name)
            {
                systemData[sysData.system.Name].systemCanvasElement.Stroke = sysLocationOutlineBrush;
            }
            else
            {
                systemData[sysData.system.Name].systemCanvasElement.Stroke = sysOutlineBrush;
            }
            systemData[sysData.system.Name].systemCanvasElement.Fill = sysFillBrush;

            if (!gathererMode)
            {
                double trueSecVal = sysData.system.TrueSec;
                if (trueSecVal >= 0.45)
                {
                    trueSecVal = 1.0;
                }
                else if (trueSecVal > 0.0)
                {
                    trueSecVal = 0.4;
                }

                Brush securityColorFill = new SolidColorBrush(MapColours.GetSecStatusColour(trueSecVal, false));
                systemData[sysData.system.Name].systemCanvasElement.Fill = securityColorFill;
            }

            if (!gathererMode && sysData.system.Region != currentPlayersSystemData[OverlayCharacter].system.Region)
            {
                systemData[sysData.system.Name].systemCanvasElement.Stroke = outOfRegionSysOutlineBrush;
                systemData[sysData.system.Name].systemCanvasElement.Fill = outOfRegionSysFillBrush;
            }

            if (mainWindow.EVEManager.TheraConnections.Any(t => t.System == sysData.system.Name))
            {
                systemData[sysData.system.Name].systemCanvasElement.Stroke = sysTheraOutlineBrush;
                systemData[sysData.system.Name].systemCanvasElement.StrokeThickness = gathererMode ? 3 : 5;
            }

            UpdateSystemTooltip(systemData[sysData.system.Name]); // Tooltip needs to be populated for ToolTipOpening event to occur.
            systemData[sysData.system.Name].systemCanvasElement.ToolTipOpening += UpdateSystemTooltipOnOpeningTooltip;

            systemData[sysData.system.Name].systemCanvasElement.ToolTipOpening += StartSystemHighlight;
            systemData[sysData.system.Name].systemCanvasElement.ToolTipClosing += StopSystemHighlight;

            if ( !gathererMode )
            {
                ContextMenu systemContextMenu = new ContextMenu();

                if (OverlayCharacter.ESILinked)
                {
                    MenuItem setRouteTarget = new MenuItem();
                    setRouteTarget.Header = "Set Destination";
                    setRouteTarget.Click += SetDestination_Click;
                    setRouteTarget.DataContext = sysData.system.Name;
                    systemContextMenu.Items.Add(setRouteTarget);

                    MenuItem addRouteWaypoint = new MenuItem();
                    addRouteWaypoint.Header = "Add Waypoint";
                    addRouteWaypoint.DataContext = sysData.system.Name;
                    addRouteWaypoint.Click += AddWaypoint_Click;
                    systemContextMenu.Items.Add(addRouteWaypoint);
                }
                else
                {
                    MenuItem esiNote = new MenuItem();
                    esiNote.Header = "ESI auth required to set route.";
                    esiNote.IsEnabled = false;
                    systemContextMenu.Items.Add(esiNote);
                }

                systemData[sysData.system.Name].systemCanvasElement.ContextMenu = systemContextMenu;
            }            

            double leftCoord = left - (systemData[sysData.system.Name].systemCanvasElement.Width * 0.5);
            double topCoord = top - (systemData[sysData.system.Name].systemCanvasElement.Height * 0.5);

            if (showSystemNames || showAllCharacterNames )
            {
                if (systemData[sysData.system.Name].systemNameElement == null)
                {
                    systemData[sysData.system.Name].systemNameElement = new TextBlock();
                }
                systemData[sysData.system.Name].systemNameElement.Inlines.Clear();
                systemData[sysData.system.Name].systemNameElement.Width = 80;

                bool firstEntry = false;
                if (showSystemNames)
                {
                    systemData[sysData.system.Name].systemNameElement.Inlines.Add( new Run(sysData.system.Name));
                    firstEntry = true;
                }

                foreach (KeyValuePair<LocalCharacter, OverlaySystemData> localCharacterEntry in currentPlayersSystemData)
                {
                    if (localCharacterEntry.Value.system != null && sysData.system.Name == localCharacterEntry.Value.system.Name)
                    {
                        if (firstEntry)
                        {
                            systemData[sysData.system.Name].systemNameElement.Inlines.Add(new LineBreak());   
                        }
                        systemData[sysData.system.Name].systemNameElement.Inlines.Add(new Run($"{localCharacterEntry.Key.Name}")) ;
                        firstEntry = true;
                    }
                }
                
                systemData[sysData.system.Name].systemNameElement.Foreground = Brushes.White;
                systemData[sysData.system.Name].systemNameElement.FontSize = 10;
                systemData[sysData.system.Name].systemNameElement.TextAlignment = TextAlignment.Center;
                systemData[sysData.system.Name].systemNameElement.IsHitTestVisible = false;

                Canvas.SetLeft(systemData[sysData.system.Name].systemNameElement, leftCoord - (systemData[sysData.system.Name].systemNameElement.Width * 0.5f) + (systemData[sysData.system.Name].systemCanvasElement.Width * 0.5f));
                Canvas.SetTop(systemData[sysData.system.Name].systemNameElement, topCoord + systemData[sysData.system.Name].systemCanvasElement.Height + 2);
                Canvas.SetZIndex(systemData[sysData.system.Name].systemNameElement, 125);

                if (!overlay_Canvas.Children.Contains(systemData[sysData.system.Name].systemNameElement))
                {
                    overlay_Canvas.Children.Add(systemData[sysData.system.Name].systemNameElement);
                }
            }

            systemData[sysData.system.Name].canvasCoordinate = new Vector2((float)leftCoord, (float)topCoord);

            Canvas.SetLeft(systemData[sysData.system.Name].systemCanvasElement, leftCoord);
            Canvas.SetTop(systemData[sysData.system.Name].systemCanvasElement, topCoord);
            Canvas.SetZIndex(systemData[sysData.system.Name].systemCanvasElement, 100);
            if (!overlay_Canvas.Children.Contains(systemData[sysData.system.Name].systemCanvasElement))
            {
                systemData[sysData.system.Name].systemCanvasElement.Name = "system";
                overlay_Canvas.Children.Add(systemData[sysData.system.Name].systemCanvasElement);
            }            
        }

        private void AddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            string senderDataContext = senderElement.DataContext as string;

            EVEData.System system = mainWindow.EVEManager.GetEveSystem(senderDataContext);
            mainWindow.EVEManager.LocalCharacters.First(c => c == OverlayCharacter).AddDestination(system.ID, false);
        }

        private void SetDestination_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            string senderDataContext = senderElement.DataContext as string;

            EVEData.System system = mainWindow.EVEManager.GetEveSystem(senderDataContext);
            mainWindow.EVEManager.LocalCharacters.First(c => c == OverlayCharacter).AddDestination(system.ID, true);
        }

        /// <summary>
        /// When highlighting a system is starting this is called.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void StartSystemHighlight(object sender, RoutedEventArgs eventArgs)
        {
            if (jumpBridgeTargetHighlight != null)
            {
                overlay_Canvas.Children.Remove(jumpBridgeTargetHighlight);
                jumpBridgeTargetHighlight = null;
            }

            OverlaySystemData highlightSystemData = systemData.FirstOrDefault(x => x.Value.systemCanvasElement == (Shape)sender).Value;
            if (highlightSystemData != null)
            {
                if (highlightSystemData.jumpBridgePath != null)
                {
                    highlightSystemData.jumpBridgePath.StrokeThickness = gathererMode ? 6 : 3;
                    highlightSystemData.jumpBridgePath.Stroke = Brushes.Yellow;

                    if ( systemData.ContainsKey(highlightSystemData.jumpBridgeTarget) )
                    {
                        double systemWidth = systemData[highlightSystemData.jumpBridgeTarget].systemCanvasElement.Width;
                        double systemHeight = systemData[highlightSystemData.jumpBridgeTarget].systemCanvasElement.Height;
                        double highlightMargin = gathererMode ? 6 : 10;
                        double highlightMarginPositionOffset = ((systemWidth + highlightMargin) / 2) - (systemWidth / 2);

                        jumpBridgeTargetHighlight = new Ellipse();
                        jumpBridgeTargetHighlight.Width = systemWidth + highlightMargin;
                        jumpBridgeTargetHighlight.Height = systemHeight + highlightMargin;
                        jumpBridgeTargetHighlight.Fill = Brushes.Transparent;
                        jumpBridgeTargetHighlight.Stroke = Brushes.Yellow;
                        jumpBridgeTargetHighlight.StrokeThickness = 4;

                        Canvas.SetLeft(jumpBridgeTargetHighlight, systemData[highlightSystemData.jumpBridgeTarget].canvasCoordinate.X - highlightMarginPositionOffset );
                        Canvas.SetTop(jumpBridgeTargetHighlight, systemData[highlightSystemData.jumpBridgeTarget].canvasCoordinate.Y - highlightMarginPositionOffset );
                        Canvas.SetZIndex(jumpBridgeTargetHighlight, 110);

                        overlay_Canvas.Children.Add(jumpBridgeTargetHighlight);
                    }                    
                }               
            }
        }

        /// <summary>
        /// When highlighting a system ends, this is called.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void StopSystemHighlight(object sender, RoutedEventArgs eventArgs)
        {
            OverlaySystemData highlightSystemData = systemData.FirstOrDefault(x => x.Value.systemCanvasElement == (Shape)sender).Value;
            if (highlightSystemData != null)
            {
                if (highlightSystemData.jumpBridgePath != null)
                {
                    highlightSystemData.jumpBridgePath.StrokeThickness = gathererMode ? 2 : 1;
                    highlightSystemData.jumpBridgePath.Stroke = bridgeLineBrush;                    
                }                    
            }

            if (jumpBridgeTargetHighlight != null)
            {
                overlay_Canvas.Children.Remove(jumpBridgeTargetHighlight);
                jumpBridgeTargetHighlight = null;
            }
        }

        /// <summary>
        /// Add a circle to represent the amount of npc kills to the canvas.
        /// </summary>
        /// <param name="sysData"></param>
        public void DrawNPCKillsToOverlay(OverlaySystemData sysData)
        {
            if (!systemData.ContainsKey(sysData.system.Name)) return;
            int npcKillData = sysData.system.NPCKillsLastHour;

            Vector2 canvasCoordinate = canvasData.CoordinateToCanvas(systemData[sysData.system.Name].mapSystemCoordinate);
            float left = canvasCoordinate.X;
            float top = canvasCoordinate.Y;

            // Set to show NPC kills?
            if (mainWindow.MapConf.OverlayShowNPCKills)
            {
                if (systemData[sysData.system.Name].npcKillCanvasElement == null)
                {
                    systemData[sysData.system.Name].npcKillCanvasElement = new Ellipse();
                }

                float killDataCalculatedSize = Math.Clamp((Math.Clamp(npcKillData, 0f, Math.Abs(npcKillData)) / npcKillDeltaMaxEqualsKills), 0f, 1f) * npcKillDeltaMaxSize;

                systemData[sysData.system.Name].npcKillCanvasElement.Width = CalculatedOverlaySystemSize(sysData.system.Name) + killDataCalculatedSize;
                systemData[sysData.system.Name].npcKillCanvasElement.Height = CalculatedOverlaySystemSize(sysData.system.Name) + killDataCalculatedSize;
                systemData[sysData.system.Name].npcKillCanvasElement.StrokeThickness = 0f;

                systemData[sysData.system.Name].npcKillCanvasElement.Fill = npcKillDataBrush;

                double leftCoord = left - (systemData[sysData.system.Name].npcKillCanvasElement.Width * 0.5);
                double topCoord = top - (systemData[sysData.system.Name].npcKillCanvasElement.Height * 0.5);

                Canvas.SetLeft(systemData[sysData.system.Name].npcKillCanvasElement, leftCoord);
                Canvas.SetTop(systemData[sysData.system.Name].npcKillCanvasElement, topCoord);
                Canvas.SetZIndex(systemData[sysData.system.Name].npcKillCanvasElement, 3);
                if (!overlay_Canvas.Children.Contains(systemData[sysData.system.Name].npcKillCanvasElement))
                {
                    overlay_Canvas.Children.Add(systemData[sysData.system.Name].npcKillCanvasElement);
                }
            }
            else
            {
                if (systemData[sysData.system.Name].npcKillCanvasElement != null)
                {
                    if (overlay_Canvas.Children.Contains(systemData[sysData.system.Name].npcKillCanvasElement))
                    {
                        overlay_Canvas.Children.Remove(systemData[sysData.system.Name].npcKillCanvasElement);
                    }
                    systemData[sysData.system.Name].npcKillCanvasElement = null;
                }
            }

            // Show delta?
            if (mainWindow.MapConf.OverlayShowNPCKillDelta)
            {
                int npcKillDelta = sysData.system.NPCKillsDeltaLastHour;

                if (systemData[sysData.system.Name].npcKillDeltaCanvasElement == null)
                {
                    systemData[sysData.system.Name].npcKillDeltaCanvasElement = new Ellipse();
                }

                float killDeltaDataCalculatedSize = Math.Clamp((Math.Clamp(npcKillDelta, 0f, Math.Abs(npcKillDelta)) / npcKillDeltaMaxEqualsKills), 0f, 1f) * npcKillDeltaMaxSize;

                systemData[sysData.system.Name].npcKillDeltaCanvasElement.Width = CalculatedOverlaySystemSize(sysData.system.Name) + killDeltaDataCalculatedSize;
                systemData[sysData.system.Name].npcKillDeltaCanvasElement.Height = CalculatedOverlaySystemSize(sysData.system.Name) + killDeltaDataCalculatedSize;
                systemData[sysData.system.Name].npcKillDeltaCanvasElement.StrokeThickness = 0f;

                systemData[sysData.system.Name].npcKillDeltaCanvasElement.Fill = npcKillDeltaDataBrush;

                double leftCoord = left - (systemData[sysData.system.Name].npcKillDeltaCanvasElement.Width * 0.5);
                double topCoord = top - (systemData[sysData.system.Name].npcKillDeltaCanvasElement.Height * 0.5);

                Canvas.SetLeft(systemData[sysData.system.Name].npcKillDeltaCanvasElement, leftCoord);
                Canvas.SetTop(systemData[sysData.system.Name].npcKillDeltaCanvasElement, topCoord);
                Canvas.SetZIndex(systemData[sysData.system.Name].npcKillDeltaCanvasElement, 4);
                if (!overlay_Canvas.Children.Contains(systemData[sysData.system.Name].npcKillDeltaCanvasElement))
                {
                    overlay_Canvas.Children.Add(systemData[sysData.system.Name].npcKillDeltaCanvasElement);
                }
            }
            else
            {
                if (systemData[sysData.system.Name].npcKillDeltaCanvasElement != null)
                {
                    if (overlay_Canvas.Children.Contains(systemData[sysData.system.Name].npcKillDeltaCanvasElement))
                    {
                        overlay_Canvas.Children.Remove(systemData[sysData.system.Name].npcKillDeltaCanvasElement);
                    }
                    systemData[sysData.system.Name].npcKillDeltaCanvasElement = null;
                }
            }
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

            if (e.PropertyName == "OverlayOpacity")
            {
                overlay_Canvas.Opacity = mainWindow.MapConf.OverlayOpacity;
            }

            if (e.PropertyName == "OverlayBackgroundOpacity")
            {
                this.Background.Opacity = mainWindow.MapConf.OverlayBackgroundOpacity;
            }

            if (e.PropertyName == "OverlayShowCharName")
            {
                showCharName = mainWindow.MapConf.OverlayShowCharName;
                UpdatePlayerInformationText();
            }

            if (e.PropertyName == "OverlayShowCharLocation")
            {
                showCharLocation = mainWindow.MapConf.OverlayShowCharLocation;
                UpdatePlayerInformationText();
            }

            if (e.PropertyName == "OverlayHunterModeShowFullRegion")
            {
                hunterModeShowFullRegion = mainWindow.MapConf.OverlayHunterModeShowFullRegion;
                RefreshCurrentView();
            }

            if (e.PropertyName == "OverlayShowJumpBridges")
            {
                showJumpBridges = mainWindow.MapConf.OverlayShowJumpBridges;
                ClearView();
                RefreshCurrentView();
            }

            if (e.PropertyName == "OverlayShowSystemNames")
            {
                showSystemNames = mainWindow.MapConf.OverlayShowSystemNames;
                ClearView();
                RefreshCurrentView();
            }
            
            if (e.PropertyName == "OverlayShowAllCharacterNames")
            {
                showAllCharacterNames = mainWindow.MapConf.OverlayShowAllCharacterNames;
                ClearView();
                RefreshCurrentView();
            }
            
            if (e.PropertyName == "OverlayIndividualCharacterWindows")
            {
                individualCharacterWindows = mainWindow.MapConf.OverlayIndividualCharacterWindows;
                Close();
            }
        }

        /// <summary>
        /// When the character is changed, the map has to be redrawn with new data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectedCharChanged(object sender, EventArgs e)
        {
            if (!individualCharacterWindows) OverlayCharacter = mainWindow.ActiveCharacter;
            RefreshCurrentView();
        }

        /// <summary>
        /// Fetches the currently selected characters name and location and
        /// displays it in the overlay window.
        /// </summary>
        public void UpdatePlayerInformationText()
        {
            
            LocalCharacter currentCharacter = mainWindow.EVEManager.LocalCharacters.FirstOrDefault(c => c == OverlayCharacter);
            if (currentCharacter != null)
            {
                string displayText = "";
                if (showCharName)
                {
                    displayText = OverlayCharacter.Name;
                }

                if (showCharLocation)
                {
                    displayText += (displayText.IsNullOrEmpty() ? "" : "\n") + currentCharacter.Location;
                }

                overlay_CharNameTextblock.Text = displayText;
            }
            else
            {
                overlay_CharNameTextblock.Text = "No char selected";
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
            RefreshCurrentView();
            canvasData.SetDimensions(overlay_Canvas.RenderSize.Width, overlay_Canvas.RenderSize.Height);
        }

        /// <summary>
        /// Handles moving the window and storing the new position data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Overlay_Window_Move(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // This is a quick way to avoid snapping by just disallowing the resizing during drag.
                this.ResizeMode = ResizeMode.NoResize;

                this.DragMove();

                // Since this not asynchronous, we can just restore the resizing here.
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
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

        /// <summary>
        /// Toggles the hunter mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Overlay_ToggleHunterMode(object sender, MouseButtonEventArgs e)
        {
            gathererMode = false;
            mainWindow.MapConf.OverlayGathererMode = gathererMode;

            if (gathererMode)
            {
                foreach (KeyValuePair<string, OverlaySystemData> sysData in systemData)
                {
                    sysData.Value.CleanUpCanvas(overlay_Canvas, true);
                }
            }

            RefreshButtonStates();
            RefreshCurrentView();
        }

        private void Overlay_ToggleGathererMode(object sender, MouseButtonEventArgs e)
        {
            gathererMode = true;
            mainWindow.MapConf.OverlayGathererMode = gathererMode;

            foreach (KeyValuePair<string, OverlaySystemData> sysData in systemData)
            {
                sysData.Value.CleanUpCanvas(overlay_Canvas, true);
            }

            RefreshButtonStates();
            RefreshCurrentView();
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
            if (systemData.ContainsKey(intelSystem))
            {
                // (string name, double x, double y) intelSystemCoordinate = systemCoordinates.Where(s => s.name == intelSystem).First();
                Vector2 intelSystemCoordinate = systemData[intelSystem].canvasCoordinate;
                intelSystemCoordinateVector = new Vector2f(intelSystemCoordinate.X - (CalculatedOverlayIntelOversize / 2f), intelSystemCoordinate.Y - (CalculatedOverlayIntelOversize / 2f));
            }
            else
            {
                intelSystemCoordinateVector = new Vector2f(0, 0);
                intelShape.Visibility = Visibility.Hidden;
            }

            intelShape.Width = CalculatedOverlaySystemSize(intelSystem) + CalculatedOverlayIntelOversize;
            intelShape.Height = CalculatedOverlaySystemSize(intelSystem) + CalculatedOverlayIntelOversize;
            intelShape.StrokeThickness = CalculatedOverlayIntelOversize / 2f;
            intelShape.Stroke = intelUrgentOutlineBrush;
            intelShape.IsHitTestVisible = false;

            Canvas.SetLeft(intelShape, intelSystemCoordinateVector.x);
            Canvas.SetTop(intelShape, intelSystemCoordinateVector.y);
            Canvas.SetZIndex(intelShape, 90);
            if (!overlay_Canvas.Children.Contains(intelShape)) overlay_Canvas.Children.Add(intelShape);

            return intelShape;
        }
    }
}