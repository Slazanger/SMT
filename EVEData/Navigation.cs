using EVEDataUtils;

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

            MapNode startSys = null;

            foreach (MapNode sys in MapNodes.Values)
            {
                if (sys.Name == start)
                {
                    startSys = sys;
                    break;
                }
            }

            foreach (MapNode sys in MapNodes.Values)
            {
                if (sys == startSys)
                {
                    continue;
                }

                decimal x = startSys.X - sys.X;
                decimal y = startSys.Y - sys.Y;
                decimal z = startSys.Z - sys.Z;

                decimal length = DecimalMath.DecimalEx.Sqrt((x * x) + (y * y) + (z * z));

                bool shouldAdd = false;

                if (length < (decimal)LY)
                {
                    shouldAdd = true;
                }

                if (sys.HighSec & !includeHighSecSystems)
                {
                    shouldAdd = false;
                }

                if (sys.Pochven & !includePochvenSystems)
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

        public static List<string> GetSystemsXJumpsFrom(List<string> sysList, string start, int X)
        {
            if (MapNodes == null || !MapNodes.ContainsKey(start))
            {
                return sysList;
            }

            if (X != 0)
            {
                if (!sysList.Contains(start))
                {
                    sysList.Add(start);
                }

                MapNode mn = MapNodes[start];

                foreach (string mm in mn.Connections)
                {
                    if (!sysList.Contains(mm))
                    {
                        sysList.Add(mm);
                    }

                    List<string> connected = GetSystemsXJumpsFrom(sysList, mm, X - 1);
                    foreach (string s in connected)
                    {
                        if (!sysList.Contains(s))
                        {
                            sysList.Add(s);
                        }
                    }
                }
            }
            return sysList;
        }

        public static SerializableDictionary<string, List<string>> CreateStaticNavigationCache(List<System> eveSystems)
        {
            SerializableDictionary<string, List<string>> rangeCache = new SerializableDictionary<string, List<string>>();

            decimal maxRange = 10;

            // now create the jumpable system links
            foreach (System sysa in eveSystems)
            {
                foreach (System sysb in eveSystems)
                {
                    if (sysa == sysb)
                    {
                        continue;
                    }
                    // cant jump into highsec systems
                    if (sysb.TrueSec > 0.45)
                    {
                        continue;
                    }

                    // cant jump into Pochven systems
                    if (sysb.Region == "Pochven")
                    {
                        continue;
                    }

                    // cant jump into Zarzakh
                    if (sysb.Name == "Zarzakh")
                    {
                        continue;
                    }

                    decimal Distance = EveManager.Instance.GetRangeBetweenSystems(sysa.Name, sysb.Name);
                    if (Distance < maxRange && Distance > 0)
                    {
                        if (!rangeCache.ContainsKey(sysa.Name))
                        {
                            rangeCache[sysa.Name] = new List<string>();
                        }

                        rangeCache[sysa.Name].Add(sysb.Name);
                    }
                }
            }

            return rangeCache;
        }

        public static void InitNavigation(List<System> eveSystems, List<JumpBridge> jumpBridges, SerializableDictionary<string, List<string>> jumpRangeCache)
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

            decimal MaxRange = 10;

            foreach (string s in jumpRangeCache.Keys)
            {
                MapNode sysMN = MapNodes[s];
                foreach (string t in jumpRangeCache[s])
                {
                    decimal Distance = EveManager.Instance.GetRangeBetweenSystems(sysMN.Name, t);
                    if (Distance < MaxRange && Distance > 0)
                    {
                        JumpLink jl = new JumpLink();
                        jl.System = t;
                        jl.RangeLY = Distance;
                        sysMN.JumpableSystems.Add(jl);
                    }
                }
            }
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

            double ExtraJumpFactor = 5.0;
            double AvoidFactor = 0.0;

            // clear the scores, values and parents from the list
            foreach (MapNode mapNode in MapNodes.Values)
            {
                mapNode.NearestToStart = null;
                mapNode.MinCostToStart = 0;
                mapNode.Visited = false;
            }

            MapNode Start = MapNodes[From];
            MapNode End = MapNodes[To];

            List<MapNode> OpenList = new List<MapNode>();
            List<MapNode> ClosedList = new List<MapNode>();

            MapNode CurrentNode = null;

            // add the start to the open list
            OpenList.Add(Start);

            while (OpenList.Count > 0)
            {
                // get the MapNode with the lowest F score
                double lowest = OpenList.Min(mn => mn.MinCostToStart);
                CurrentNode = OpenList.First(mn => mn.MinCostToStart == lowest);

                // add the list to the closed list
                ClosedList.Add(CurrentNode);

                // remove it from the open list
                OpenList.Remove(CurrentNode);

                // walk the connections
                foreach (JumpLink connection in CurrentNode.JumpableSystems)
                {
                    if (connection.RangeLY > (decimal)MaxLY)
                    {
                        continue;
                    }

                    MapNode CMN = MapNodes[connection.System];

                    if (CMN.Visited)
                        continue;

                    if (systemsToAvoid.Contains(connection.System))
                    {
                        AvoidFactor = 10000;
                    }
                    else
                    {
                        AvoidFactor = 0.0;
                    }

                    if (CMN.MinCostToStart == 0 || CurrentNode.MinCostToStart + (double)connection.RangeLY + ExtraJumpFactor + AvoidFactor < CMN.MinCostToStart)
                    {
                        CMN.MinCostToStart = CurrentNode.MinCostToStart + (double)connection.RangeLY + ExtraJumpFactor + AvoidFactor;
                        CMN.NearestToStart = CurrentNode;
                        if (!OpenList.Contains(CMN))
                        {
                            OpenList.Add(CMN);
                        }
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
            public decimal RangeLY;
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
            public List<string> Connections { get; set; }
            public bool HighSec { get; set; }
            public bool Pochven { get; set; }
            public List<JumpLink> JumpableSystems { get; set; }
            public string Name { get; set; }
            public System ActualSystem { get; set; }
        }
    }
}