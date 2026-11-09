namespace SMT.EVEData
{
    public enum RoutingMode
    {
        Shortest,
        Safest,
        PreferLow,
    }

    public class Navigation
    {
        /// <summary>
        /// Longest jump range, in light years, that capital routing considers.
        /// </summary>
        private const double MaxJumpRangeLY = 10.0;

        public enum GateType
        {
            StarGate,
            Ansiblex,
            JumpTo,
            Thera,
            Zarzakh,
            Turnur,
        }

        private static Dictionary<string, MapNode> MapNodes { get; set; }
        private static List<string> TheraLinks { get; set; }
        private static List<string> ZarzakhLinks { get; set; }
        private static List<string> TurnurLinks { get; set; }
        
        // Track nodes that were touched (visited OR added to open list) in the last navigation run for lazy reset
        private static HashSet<MapNode> LastTouchedNodes { get; set; } = new HashSet<MapNode>();

        public static void ClearJumpBridges()
        {
            foreach (MapNode mn in MapNodes.Values)
            {
                mn.JBConnection = null;
            }
        }

        public static void ClearTheraConnections()
        {
            foreach (MapNode mn in MapNodes.Values)
            {
                mn.TheraConnections = null;
            }
        }

        public static void ClearZarzakhConnections()
        {
            foreach (MapNode mn in MapNodes.Values)
            {
                mn.ZarzakhConnections = null;
            }
        }

        public static void ClearTurnurConnections()
        {
            foreach (MapNode mn in MapNodes.Values)
            {
                mn.TurnurConnections = null;
            }
        }

        public static void UpdateTheraConnections(List<string> theraSystems)
        {
            ClearTheraConnections();

            foreach (string ts in theraSystems)
            {
                MapNodes[ts].TheraConnections = theraSystems;
            }
        }

        public static void UpdateZarzakhConnections(List<string> zazahkSystems)
        {
            ClearZarzakhConnections();

            foreach (string ts in zazahkSystems)
            {
                MapNodes[ts].ZarzakhConnections = zazahkSystems;
            }
        }

        public static void UpdateTurnurConnections(List<string> turnurSystems)
        {
            ClearTurnurConnections();

            MapNodes["Turnur"].TurnurConnections = turnurSystems;

            List<string> toTurnurConnections = new List<string>();
            toTurnurConnections.Add("Turnur");

            foreach (string ts in turnurSystems)
            {
                MapNodes[ts].TurnurConnections = toTurnurConnections;
            }
        }

        public static List<string> GetSystemsWithinXLYFrom(string start, double LY, bool includeHighSecSystems, bool includePochvenSystems)
        {
            List<string> inRange = new List<string>();

            if(MapNodes == null || LY <= 0 || !MapNodes.TryGetValue(start, out MapNode startSys))
            {
                return inRange;
            }

            decimal maxDistance = (decimal)LY;
            decimal maxDistanceSquared = maxDistance * maxDistance;

            foreach (MapNode sys in MapNodes.Values)
            {
                if (sys == startSys)
                {
                    continue;
                }

                decimal x = startSys.X - sys.X;
                decimal y = startSys.Y - sys.Y;
                decimal z = startSys.Z - sys.Z;

                decimal distanceSquared = (x * x) + (y * y) + (z * z);
                bool shouldAdd = distanceSquared < maxDistanceSquared;

                if (sys.HighSec && !includeHighSecSystems)
                {
                    shouldAdd = false;
                }

                if (sys.Pochven && !includePochvenSystems)
                {
                    shouldAdd = false;
                }

                if (shouldAdd)
                {
                    inRange.Add(sys.Name);
                }
            }

            return inRange;
        }

        /// <summary>
        /// Adds every system within X gate jumps of start, start included, to sysList and returns it. Nothing is added
        /// for X &lt;= 0 or an unknown start. Breadth-first, so each system is expanded once.
        /// </summary>
        public static List<string> GetSystemsXJumpsFrom(List<string> sysList, string start, int X)
        {
            if (MapNodes == null || !MapNodes.ContainsKey(start) || X <= 0)
            {
                return sysList;
            }

            HashSet<string> listed = new HashSet<string>(sysList);
            if (listed.Add(start))
            {
                sysList.Add(start);
            }

            HashSet<string> reached = new HashSet<string> { start };
            List<string> frontier = new List<string> { start };
            for (int jumps = 0; jumps < X && frontier.Count > 0; jumps++)
            {
                List<string> next = new List<string>();
                foreach (string name in frontier)
                {
                    if (!MapNodes.TryGetValue(name, out MapNode node))
                    {
                        continue;
                    }

                    foreach (string connection in node.Connections)
                    {
                        if (listed.Add(connection))
                        {
                            sysList.Add(connection);
                        }

                        if (reached.Add(connection))
                        {
                            next.Add(connection);
                        }
                    }
                }

                frontier = next;
            }

            return sysList;
        }

        public static void InitNavigation(List<System> eveSystems, List<JumpBridge> jumpBridges)
        {
            MapNodes = new Dictionary<string, MapNode>();

            TheraLinks = new List<string>();
            ZarzakhLinks = new List<string>();
            TurnurLinks = new List<string>();

            // build up the nav structures
            foreach (System sys in eveSystems)
            {
                MapNode mn = new MapNode
                {
                    Name = sys.Name,
                    HighSec = sys.TrueSec > 0.45,
                    Pochven = sys.Region == "Pochven",
                    Connections = new List<string>(),
                    JumpableSystems = new List<JumpLink>(),
                    Cost = 1,
                    MinCostToStart = 0,
                    X = sys.ActualX,
                    Y = sys.ActualY,
                    Z = sys.ActualZ,
                    PositionX = (double)sys.ActualX,
                    PositionY = (double)sys.ActualY,
                    PositionZ = (double)sys.ActualZ,
                    F = 0,
                    ActualSystem = sys
                };

                foreach (string s in sys.Jumps)
                {
                    mn.Connections.Add(s);
                }

                MapNodes[mn.Name] = mn;
            }

            UpdateJumpBridges(jumpBridges);

            BuildJumpableSystems();
        }

        /// <summary>
        /// Links every system to each system within MaxJumpRangeLY that a capital can jump into
        /// (not highsec, Pochven or Zarzakh). Destinations are bucketed into cubes MaxJumpRangeLY
        /// on a side, so each system is only measured against the 27 cubes around it. Building
        /// the table this way at startup is quicker than loading it from a data file.
        /// </summary>
        private static void BuildJumpableSystems()
        {
            Dictionary<(long, long, long), List<MapNode>> cells = new Dictionary<(long, long, long), List<MapNode>>();

            foreach (MapNode node in MapNodes.Values)
            {
                if (node.HighSec || node.Pochven || node.Name == "Zarzakh")
                {
                    continue;
                }

                (long, long, long) cell = GetJumpRangeCell(node);
                if (!cells.TryGetValue(cell, out List<MapNode> members))
                {
                    members = new List<MapNode>();
                    cells[cell] = members;
                }

                members.Add(node);
            }

            const double maxRangeSquared = MaxJumpRangeLY * MaxJumpRangeLY;

            foreach (MapNode node in MapNodes.Values)
            {
                (long cellX, long cellY, long cellZ) = GetJumpRangeCell(node);

                for (long dx = -1; dx <= 1; dx++)
                {
                    for (long dy = -1; dy <= 1; dy++)
                    {
                        for (long dz = -1; dz <= 1; dz++)
                        {
                            if (!cells.TryGetValue((cellX + dx, cellY + dy, cellZ + dz), out List<MapNode> members))
                            {
                                continue;
                            }

                            foreach (MapNode target in members)
                            {
                                double x = node.PositionX - target.PositionX;
                                double y = node.PositionY - target.PositionY;
                                double z = node.PositionZ - target.PositionZ;
                                double distanceSquared = (x * x) + (y * y) + (z * z);

                                // zero distance is the system itself
                                if (distanceSquared > 0 && distanceSquared < maxRangeSquared)
                                {
                                    node.JumpableSystems.Add(new JumpLink { System = target.Name, RangeLY = Math.Sqrt(distanceSquared) });
                                }
                            }
                        }
                    }
                }
            }
        }

        private static (long, long, long) GetJumpRangeCell(MapNode node)
        {
            return ((long)Math.Floor(node.PositionX / MaxJumpRangeLY),
                    (long)Math.Floor(node.PositionY / MaxJumpRangeLY),
                    (long)Math.Floor(node.PositionZ / MaxJumpRangeLY));
        }

        public static List<RoutePoint> Navigate(string From, string To, bool UseJumpGates, bool UseThera, bool UseZarzakh, bool UseTurnur, RoutingMode routingMode)
        {
            if (!(MapNodes.ContainsKey(From)) || !(MapNodes.ContainsKey(To)) || From == "" || To == "")

            {
                return null;
            }

            // Lazy reset: only clear nodes that were touched (visited OR in open list) in the previous run
            foreach (MapNode mapNode in LastTouchedNodes)
            {
                mapNode.NearestToStart = null;
                mapNode.MinCostToStart = 0;
                mapNode.Visited = false;
            }
            LastTouchedNodes.Clear();

            // Routing costs will be calculated dynamically during pathfinding - no need to process all nodes

            MapNode Start = MapNodes[From];
            MapNode End = MapNodes[To];

            // Use local collections for thread safety and reliability
            SortedSet<MapNode> OpenList = new SortedSet<MapNode>(new MapNodeComparer());
            HashSet<MapNode> OpenSet = new HashSet<MapNode>(); // For fast Contains operations
            HashSet<MapNode> ClosedSet = new HashSet<MapNode>();

            MapNode CurrentNode = null;

            // add the start to the open list
            OpenList.Add(Start);
            OpenSet.Add(Start);
            LastTouchedNodes.Add(Start);

            while (OpenList.Count > 0)
            {
                // get the MapNode with the lowest cost - O(1) operation with SortedSet
                CurrentNode = OpenList.Min;

                // add to the closed set
                ClosedSet.Add(CurrentNode);

                // remove from the open list and set - O(log n) operation
                OpenList.Remove(CurrentNode);
                OpenSet.Remove(CurrentNode);

                // Early termination: stop when we reach the destination
                if (CurrentNode == End)
                {
                    break;
                }

                // walk the connections
                foreach (string connectionName in CurrentNode.Connections)
                {
                    MapNode connectionNode = MapNodes[connectionName];

                    if (connectionNode.Visited)
                        continue;

                    double connectionCost = GetRoutingCost(connectionNode, routingMode);
                    double newCostToStart = CurrentNode.MinCostToStart + connectionCost;
                    
                    if (connectionNode.MinCostToStart == 0 || newCostToStart < connectionNode.MinCostToStart)
                    {
                        // If node is already in open set, remove it first to update its position
                        if (OpenSet.Contains(connectionNode))
                        {
                            OpenList.Remove(connectionNode);
                            OpenSet.Remove(connectionNode);
                        }
                        
                        connectionNode.MinCostToStart = newCostToStart;
                        connectionNode.NearestToStart = CurrentNode;
                        LastTouchedNodes.Add(connectionNode);
                        
                        if (!ClosedSet.Contains(connectionNode))
                        {
                            OpenList.Add(connectionNode);
                            OpenSet.Add(connectionNode);
                        }
                    }
                }

                if (UseJumpGates && CurrentNode.JBConnection != null)
                {
                    MapNode jumpBridgeNode = MapNodes[CurrentNode.JBConnection];
                    if (!jumpBridgeNode.Visited)
                    {
                        double connectionCost = GetRoutingCost(jumpBridgeNode, routingMode);
                        double newCostToStart = CurrentNode.MinCostToStart + connectionCost;
                        
                        if (jumpBridgeNode.MinCostToStart == 0 || newCostToStart < jumpBridgeNode.MinCostToStart)
                        {
                            // If node is already in open set, remove it first to update its position
                            if (OpenSet.Contains(jumpBridgeNode))
                            {
                                OpenList.Remove(jumpBridgeNode);
                                OpenSet.Remove(jumpBridgeNode);
                            }
                            
                            jumpBridgeNode.MinCostToStart = newCostToStart;
                            jumpBridgeNode.NearestToStart = CurrentNode;
                            LastTouchedNodes.Add(jumpBridgeNode);
                            
                            if (!ClosedSet.Contains(jumpBridgeNode))
                            {
                                OpenList.Add(jumpBridgeNode);
                                OpenSet.Add(jumpBridgeNode);
                            }
                        }
                    }
                }

                if (UseThera && CurrentNode.TheraConnections != null)
                {
                    foreach (string theraConnection in CurrentNode.TheraConnections)
                    {
                        MapNode theraNode = MapNodes[theraConnection];

                        if (theraNode.Visited)
                            continue;

                        // dont jump back to the system we came from
                        if (CurrentNode.Name == theraConnection)
                            continue;

                        double connectionCost = GetRoutingCost(theraNode, routingMode);
                        double newCostToStart = CurrentNode.MinCostToStart + connectionCost;
                        
                        if (theraNode.MinCostToStart == 0 || newCostToStart < theraNode.MinCostToStart)
                        {
                            // If node is already in open set, remove it first to update its position
                            if (OpenSet.Contains(theraNode))
                            {
                                OpenList.Remove(theraNode);
                                OpenSet.Remove(theraNode);
                            }
                            
                            theraNode.MinCostToStart = newCostToStart;
                            theraNode.NearestToStart = CurrentNode;
                            LastTouchedNodes.Add(theraNode);
                            
                            if (!ClosedSet.Contains(theraNode))
                            {
                                OpenList.Add(theraNode);
                                OpenSet.Add(theraNode);
                            }
                        }
                    }
                }

                if (UseTurnur && CurrentNode.TurnurConnections != null)
                {
                    foreach (string turnurConnection in CurrentNode.TurnurConnections)
                    {
                        MapNode turnurNode = MapNodes[turnurConnection];

                        if (turnurNode.Visited)
                            continue;

                        // dont jump back to the system we came from
                        if (CurrentNode.Name == turnurConnection)
                            continue;

                        double connectionCost = GetRoutingCost(turnurNode, routingMode);
                        double newCostToStart = CurrentNode.MinCostToStart + connectionCost;
                        
                        if (turnurNode.MinCostToStart == 0 || newCostToStart < turnurNode.MinCostToStart)
                        {
                            // If node is already in open set, remove it first to update its position
                            if (OpenSet.Contains(turnurNode))
                            {
                                OpenList.Remove(turnurNode);
                                OpenSet.Remove(turnurNode);
                            }
                            
                            turnurNode.MinCostToStart = newCostToStart;
                            turnurNode.NearestToStart = CurrentNode;
                            LastTouchedNodes.Add(turnurNode);
                            
                            if (!ClosedSet.Contains(turnurNode))
                            {
                                OpenList.Add(turnurNode);
                                OpenSet.Add(turnurNode);
                            }
                        }
                    }
                }

                if (UseZarzakh && CurrentNode.ZarzakhConnections != null)
                {
                    foreach (string ZarzakhConnection in CurrentNode.ZarzakhConnections)
                    {
                        MapNode zarzakhNode = MapNodes[ZarzakhConnection];

                        if (zarzakhNode.Visited)
                            continue;

                        // don't jump back to the system we just came from
                        if (CurrentNode.Name == ZarzakhConnection)
                            continue;

                        double connectionCost = GetRoutingCost(zarzakhNode, routingMode);
                        double newCostToStart = CurrentNode.MinCostToStart + connectionCost;
                        
                        if (zarzakhNode.MinCostToStart == 0 || newCostToStart < zarzakhNode.MinCostToStart)
                        {
                            // If node is already in open set, remove it first to update its position
                            if (OpenSet.Contains(zarzakhNode))
                            {
                                OpenList.Remove(zarzakhNode);
                                OpenSet.Remove(zarzakhNode);
                            }
                            
                            zarzakhNode.MinCostToStart = newCostToStart;
                            zarzakhNode.NearestToStart = CurrentNode;
                            LastTouchedNodes.Add(zarzakhNode);
                            
                            if (!ClosedSet.Contains(zarzakhNode))
                            {
                                OpenList.Add(zarzakhNode);
                                OpenSet.Add(zarzakhNode);
                            }
                        }
                    }
                }

                /* Todo :  Additional error checking
                if (UseThera && !string.IsNullOrEmptyCurrent(Node.TheraInSig))
                {
                    //SJS HERE ERROR
                }
                */

                CurrentNode.Visited = true;
                LastTouchedNodes.Add(CurrentNode);
            }

            // build the path

            List<string> Route = new List<string>();

            bool rootError = false;

            CurrentNode = End;
            if (End.NearestToStart != null)
            {
                while (CurrentNode != null)
                {
                    Route.Add(CurrentNode.Name);
                    CurrentNode = CurrentNode.NearestToStart;
                    if (Route.Count > 2000)
                    {
                        rootError = true;
                        break;
                    }
                }
                Route.Reverse();
            }

            List<RoutePoint> ActualRoute = new List<RoutePoint>();

            if (!rootError)
            {
                for (int i = 0; i < Route.Count; i++)
                {
                    RoutePoint RP = new RoutePoint();
                    RP.SystemName = Route[i];
                    RP.ActualSystem = EveManager.Instance.GetEveSystem(Route[i]);
                    RP.GateToTake = GateType.StarGate;
                    RP.LY = 0.0m;

                    if (i < Route.Count - 1)
                    {
                        MapNode mn = MapNodes[RP.SystemName];
                        if (mn.JBConnection != null && mn.JBConnection == Route[i + 1])
                        {
                            RP.GateToTake = GateType.Ansiblex;
                        }

                        if (UseThera && mn.TheraConnections != null && mn.TheraConnections.Contains(Route[i + 1]))
                        {
                            RP.GateToTake = GateType.Thera;
                        }

                        if (UseZarzakh && mn.ZarzakhConnections != null && mn.ZarzakhConnections.Contains(Route[i + 1]))
                        {
                            RP.GateToTake = GateType.Zarzakh;
                        }

                        if(UseTurnur && mn.TurnurConnections != null && mn.TurnurConnections.Contains(Route[i + 1]) && (mn.Name == "Turnur" || Route[i + 1] == "Turnur") )
                        {
                            RP.GateToTake = GateType.Turnur;
                        }
                    }
                    ActualRoute.Add(RP);
                }
            }

            return ActualRoute;
        }

        public static List<RoutePoint> NavigateCapitals(string From, string To, double MaxLY, LocalCharacter lc, List<string> systemsToAvoid)
        {
            if (!(MapNodes.ContainsKey(From)) || !(MapNodes.ContainsKey(To)) || From == "" || To == "")
            {
                return null;
            }

            const double ExtraJumpFactor = 5.0;
            HashSet<string> avoidSystems = systemsToAvoid == null ? new HashSet<string>() : new HashSet<string>(systemsToAvoid);

            // Reset only nodes touched by the previous route calculation.
            foreach (MapNode mapNode in LastTouchedNodes)
            {
                mapNode.NearestToStart = null;
                mapNode.MinCostToStart = 0;
                mapNode.Visited = false;
            }
            LastTouchedNodes.Clear();

            MapNode Start = MapNodes[From];
            MapNode End = MapNodes[To];

            SortedSet<MapNode> OpenList = new SortedSet<MapNode>(new MapNodeComparer());
            HashSet<MapNode> OpenSet = new HashSet<MapNode>();

            MapNode CurrentNode = null;

            // add the start to the open list
            OpenList.Add(Start);
            OpenSet.Add(Start);
            LastTouchedNodes.Add(Start);

            while (OpenList.Count > 0)
            {
                CurrentNode = OpenList.Min;
                OpenList.Remove(CurrentNode);
                OpenSet.Remove(CurrentNode);

                // The lowest-cost destination has been found; no remaining nodes need processing.
                if(CurrentNode == End)
                {
                    break;
                }

                // walk the connections
                foreach (JumpLink connection in CurrentNode.JumpableSystems)
                {
                    if (connection.RangeLY > MaxLY)
                    {
                        continue;
                    }

                    MapNode CMN = MapNodes[connection.System];

                    if (CMN.Visited)
                        continue;

                    double avoidFactor = avoidSystems.Contains(connection.System) ? 10000.0 : 0.0;
                    double newCostToStart = CurrentNode.MinCostToStart + connection.RangeLY + ExtraJumpFactor + avoidFactor;

                    if (CMN.MinCostToStart == 0 || newCostToStart < CMN.MinCostToStart)
                    {
                        if(OpenSet.Contains(CMN))
                        {
                            OpenList.Remove(CMN);
                            OpenSet.Remove(CMN);
                        }

                        CMN.MinCostToStart = newCostToStart;
                        CMN.NearestToStart = CurrentNode;
                        LastTouchedNodes.Add(CMN);
                        OpenList.Add(CMN);
                        OpenSet.Add(CMN);
                    }
                }

                CurrentNode.Visited = true;
                LastTouchedNodes.Add(CurrentNode);
            }

            // build the path

            List<string> Route = new List<string>();

            CurrentNode = End;
            if (End.NearestToStart != null)
            {
                while (CurrentNode != null)
                {
                    Route.Add(CurrentNode.Name);
                    CurrentNode = CurrentNode.NearestToStart;
                }
            }

            List<RoutePoint> ActualRoute = new List<RoutePoint>();

            for (int i = 0; i < Route.Count; i++)
            {
                RoutePoint RP = new RoutePoint();
                RP.GateToTake = GateType.JumpTo;
                RP.LY = 0.0m;
                RP.SystemName = Route[i];

                if (i > 0)
                {
                    RP.LY = EveManager.Instance.GetRangeBetweenSystems(Route[i], Route[i - 1]);
                }
                ActualRoute.Add(RP);
            }

            ActualRoute.Reverse();

            return ActualRoute;
        }

        public static void UpdateJumpBridges(List<JumpBridge> jumpBridges)
        {
            foreach (JumpBridge jb in jumpBridges)
            {
                if (jb.Disabled)
                {
                    continue;
                }

                MapNodes[jb.From].JBConnection = jb.To;
                MapNodes[jb.To].JBConnection = jb.From;
            }
        }

        private static double GetRoutingCost(MapNode node, RoutingMode routingMode)
        {
            switch (routingMode)
            {
                case RoutingMode.PreferLow:
                    return node.HighSec ? 1000 : 1;
                    
                case RoutingMode.Safest:
                    return !node.HighSec ? 1000 : 1;
                    
                case RoutingMode.Shortest:
                default:
                    return 1;
            }
        }

        private struct JumpLink
        {
            public double RangeLY;
            public string System;
        }

        public class RoutePoint
        {
            public GateType GateToTake { get; set; }
            public decimal LY { get; set; }
            public string SystemName { get; set; }

            public System ActualSystem { get; set; }

            public override string ToString()
            {
                string s = SystemName;
                if (GateToTake == GateType.Ansiblex)
                {
                    s += " (Ansiblex)";
                }

                if (GateToTake == GateType.Thera)
                {
                    s += " (Thera)";
                }

                if (GateToTake == GateType.Zarzakh)
                {
                    s += " (Zarzakh)";
                }

                if (GateToTake == GateType.JumpTo && LY > 0.0m)
                {
                    s += " (Jump To, Range " + LY.ToString("0.##") + " )";
                }

                return s;
            }
        }

        private class MapNodeComparer : IComparer<MapNode>
        {
            public int Compare(MapNode x, MapNode y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                
                // Primary comparison: MinCostToStart
                int costComparison = x.MinCostToStart.CompareTo(y.MinCostToStart);
                if (costComparison != 0)
                    return costComparison;
                
                // Reliable tie-breaker: use system name for stable, deterministic sorting
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }
        }

        private class MapNode
        {
            public double Cost;
            public double F;
            public string JBConnection;
            public List<string> TheraConnections;
            public List<string> ZarzakhConnections;
            public List<string> TurnurConnections;

            public double MinCostToStart;
            public MapNode NearestToStart;
            public string TheraInSig;
            public string TheraOutSig;
            public bool Visited;
            public decimal X;
            public decimal Y;
            public decimal Z;

            // the same position in light years as double, for the jump range build
            public double PositionX;
            public double PositionY;
            public double PositionZ;
            public List<string> Connections { get; set; }
            public bool HighSec { get; set; }
            public bool Pochven { get; set; }
            public List<JumpLink> JumpableSystems { get; set; }
            public string Name { get; set; }
            public System ActualSystem { get; set; }
        }
    }
}
