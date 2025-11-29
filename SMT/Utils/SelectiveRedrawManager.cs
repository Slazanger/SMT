using System;
using System.Collections.Generic;

namespace SMT.Utils
{
    /// <summary>
    /// Defines the scope and type of redraw operations to minimize unnecessary rendering
    /// </summary>
    [Flags]
    public enum RedrawType
    {
        None = 0,
        
        // Individual layer updates
        SystemShapes = 1 << 0,        // System circles/rectangles
        SystemText = 1 << 1,          // System names and labels  
        SystemData = 1 << 2,          // Data overlays (kills, jumps, etc.)
        Characters = 1 << 3,          // Character positions and names
        ZKillData = 1 << 4,           // ZKillboard data overlays
        Connections = 1 << 5,         // Gates, jump bridges, wormholes
        Routes = 1 << 6,              // Navigation routes
        Intel = 1 << 7,               // Intel overlays
        Sovereignty = 1 << 8,         // SOV data and conflicts
        FactionWarfare = 1 << 9,      // Faction warfare systems
        Background = 1 << 10,         // Background colors and gradients
        
        // Compound update types
        DataLayers = SystemData | ZKillData | Intel | Sovereignty,
        DynamicElements = Characters | Routes | Intel,
        StaticElements = SystemShapes | SystemText | Connections | Background,
        ColorScheme = SystemShapes | SystemText | Background | Connections,
        
        // Special cases
        ViewportChange = SystemShapes | SystemText | SystemData | Characters | ZKillData | Connections,
        FullRedraw = ~0  // All flags set
    }

    /// <summary>
    /// Manages selective redrawing to optimize rendering performance by only updating changed elements
    /// </summary>
    public class SelectiveRedrawManager
    {
        private readonly HashSet<RedrawType> _pendingRedraws = new HashSet<RedrawType>();
        private readonly Dictionary<string, RedrawType> _propertyToRedrawMap = new Dictionary<string, RedrawType>();
        private readonly object _lock = new object();
        
        public SelectiveRedrawManager()
        {
            InitializePropertyMappings();
        }

        /// <summary>
        /// Maps property names to their corresponding redraw types
        /// </summary>
        private void InitializePropertyMappings()
        {
            // Color scheme changes
            _propertyToRedrawMap["ActiveColourScheme"] = RedrawType.ColorScheme;
            _propertyToRedrawMap["ShowSimpleSecurityView"] = RedrawType.SystemShapes;
            _propertyToRedrawMap["ShowTrueSec"] = RedrawType.SystemShapes;
            
            // System display options
            _propertyToRedrawMap["ShowSystemSecurity"] = RedrawType.SystemShapes;
            _propertyToRedrawMap["ShowSystemADM"] = RedrawType.SystemShapes;
            _propertyToRedrawMap["ShowSystemNames"] = RedrawType.SystemText;
            _propertyToRedrawMap["ShowSystemTimers"] = RedrawType.SystemText;
            
            // Data overlay toggles
            _propertyToRedrawMap["ShowNPCKills"] = RedrawType.SystemData;
            _propertyToRedrawMap["ShowPodKills"] = RedrawType.SystemData;
            _propertyToRedrawMap["ShowShipKills"] = RedrawType.SystemData;
            _propertyToRedrawMap["ShowShipJumps"] = RedrawType.SystemData;
            _propertyToRedrawMap["ShowRattingDataAsDelta"] = RedrawType.SystemData;
            _propertyToRedrawMap["ShowNegativeRattingDelta"] = RedrawType.SystemData;
            _propertyToRedrawMap["ESIOverlayScale"] = RedrawType.SystemData;
            
            // Character and intel
            _propertyToRedrawMap["ShowCharacterNamesOnMap"] = RedrawType.Characters;
            _propertyToRedrawMap["ShowCharacterLocationsOnMap"] = RedrawType.Characters;
            _propertyToRedrawMap["ShowIntelOnMap"] = RedrawType.Intel;
            
            // ZKillboard data
            _propertyToRedrawMap["ShowZKillData"] = RedrawType.ZKillData;
            
            // Routes and navigation
            _propertyToRedrawMap["DrawRoute"] = RedrawType.Routes;
            _propertyToRedrawMap["ShowJumpDistance"] = RedrawType.Routes;
            
            // Sovereignty and faction warfare
            _propertyToRedrawMap["ShowSovOwner"] = RedrawType.Sovereignty;
            _propertyToRedrawMap["ShowSOVConflicts"] = RedrawType.Sovereignty;
            _propertyToRedrawMap["ShowFactionWarfare"] = RedrawType.FactionWarfare;
            
            // Connection display
            _propertyToRedrawMap["ShowJumpBridges"] = RedrawType.Connections;
            _propertyToRedrawMap["ShowTheraConnections"] = RedrawType.Connections;
            _propertyToRedrawMap["ShowTurnurConnections"] = RedrawType.Connections;
        }

        /// <summary>
        /// Queues a redraw operation for the specified type
        /// </summary>
        public void RequestRedraw(RedrawType redrawType)
        {
            if (redrawType == RedrawType.None) return;
            
            lock (_lock)
            {
                _pendingRedraws.Add(redrawType);
            }
        }

        /// <summary>
        /// Queues a redraw based on a property name change
        /// </summary>
        public void RequestRedrawForProperty(string propertyName)
        {
            if (_propertyToRedrawMap.TryGetValue(propertyName, out var redrawType))
            {
                RequestRedraw(redrawType);
            }
            else
            {
                // Unknown property - safer to do full redraw
                RequestRedraw(RedrawType.FullRedraw);
            }
        }

        /// <summary>
        /// Gets all pending redraw types and clears the queue
        /// </summary>
        public RedrawType GetPendingRedraws()
        {
            lock (_lock)
            {
                var combined = RedrawType.None;
                foreach (var redraw in _pendingRedraws)
                {
                    combined |= redraw;
                }
                _pendingRedraws.Clear();
                return combined;
            }
        }

        /// <summary>
        /// Checks if a specific redraw type is pending
        /// </summary>
        public bool HasPendingRedraw(RedrawType redrawType)
        {
            lock (_lock)
            {
                return _pendingRedraws.Contains(redrawType);
            }
        }

        /// <summary>
        /// Optimizes the redraw type by removing redundant operations
        /// </summary>
        public static RedrawType OptimizeRedrawType(RedrawType redrawType)
        {
            // If full redraw is requested, return it as-is
            if ((redrawType & RedrawType.FullRedraw) == RedrawType.FullRedraw)
            {
                return RedrawType.FullRedraw;
            }

            // If color scheme change is requested, it affects multiple systems
            if ((redrawType & RedrawType.ColorScheme) != 0)
            {
                // Color scheme changes affect these specific areas
                return RedrawType.ColorScheme;
            }

            return redrawType;
        }

        /// <summary>
        /// Determines if the redraw requires clearing visual hosts
        /// </summary>
        public static bool RequiresClearing(RedrawType redrawType)
        {
            return (redrawType & (
                RedrawType.SystemShapes | 
                RedrawType.SystemData | 
                RedrawType.Characters | 
                RedrawType.ZKillData | 
                RedrawType.Connections | 
                RedrawType.Routes |
                RedrawType.Intel |
                RedrawType.Sovereignty |
                RedrawType.FactionWarfare)) != 0;
        }

        /// <summary>
        /// Gets a human-readable description of the redraw operations
        /// </summary>
        public static string GetRedrawDescription(RedrawType redrawType)
        {
            if (redrawType == RedrawType.None) return "No redraw needed";
            if (redrawType == RedrawType.FullRedraw) return "Full redraw";

            var parts = new List<string>();
            
            if ((redrawType & RedrawType.SystemShapes) != 0) parts.Add("System shapes");
            if ((redrawType & RedrawType.SystemText) != 0) parts.Add("System text");
            if ((redrawType & RedrawType.SystemData) != 0) parts.Add("System data");
            if ((redrawType & RedrawType.Characters) != 0) parts.Add("Characters");
            if ((redrawType & RedrawType.ZKillData) != 0) parts.Add("ZKill data");
            if ((redrawType & RedrawType.Connections) != 0) parts.Add("Connections");
            if ((redrawType & RedrawType.Routes) != 0) parts.Add("Routes");
            if ((redrawType & RedrawType.Intel) != 0) parts.Add("Intel");
            if ((redrawType & RedrawType.Sovereignty) != 0) parts.Add("Sovereignty");
            if ((redrawType & RedrawType.FactionWarfare) != 0) parts.Add("Faction warfare");
            if ((redrawType & RedrawType.Background) != 0) parts.Add("Background");

            return $"Selective redraw: {string.Join(", ", parts)}";
        }
    }
}
