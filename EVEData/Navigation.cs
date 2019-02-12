using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{


    public class Navigation
    {
        class MapNode
        {
            public bool HighSec { get; set; }
            public string Name { get; set; }
            public int Cost;

            public double X;
            public double Y;
            public double Z;

            public int F;
            public int G;
            public int H;

            public MapNode Parent;

            public List<string> Connections { get; set; }

            public string JBConnection;
        }


        public static void InitNavigation(List<System> eveSystems, List<JumpBridge> jumpBridges)
        {
            MapNodes = new Dictionary<string, MapNode>();


            // build up the nav structures
            foreach(System sys in eveSystems)
            {
                MapNode mn = new MapNode
                {
                    Name = sys.Name,
                    HighSec = sys.TrueSec >= 0.5,
                    Connections = new List<string>(),
                    Cost = 1,
                    X = sys.ActualX,
                    Y = sys.ActualY,
                    Z = sys.ActualZ,
                    F = 0
                };

                foreach(string s in sys.Jumps)
                {
                    mn.Connections.Add(s);
                }

                MapNodes[mn.Name] = mn;

            }

            foreach(JumpBridge jb in jumpBridges)
            {
                if(jb.Friendly)
                {
                    MapNodes[jb.From].JBConnection = jb.To;
                    MapNodes[jb.To].JBConnection = jb.From;
                }
            }
        }


        static int CalculateHScore(MapNode from, MapNode to)
        {
            double x = from.X - to.X;
            double z = from.Z - to.Z;
            double y = from.Y - to.Y;

            double length = Math.Sqrt((x * x) + (y * y) + (z * z));

            length = length / 9460730472580800.0;

            return (int)length;
        }


        public static List<string> Navigate(string From, string To, bool UseJumpGates)
        {
            if( !(MapNodes.Keys.Contains(From)||MapNodes.Keys.Contains(To)) )
            {
                return null;
            }

            // clear the scores, values and parents from the list
            foreach(MapNode mapNode in MapNodes.Values)
            {
                mapNode.F = 0;
                mapNode.G = 0;
                mapNode.H = 0;
                mapNode.Parent = null;
            }


            MapNode Start = MapNodes[From];
            MapNode End = MapNodes[To];

            List<MapNode> OpenList = new List<MapNode>();
            List<MapNode> ClosedList = new List<MapNode>();

            int g = 0;
            MapNode CurrentNode = null;

            // add the start to the open list
            OpenList.Add(Start);


            while (OpenList.Count > 0)
            {
                // get the MapNode with the lowest F score
                int lowest = OpenList.Min(mn => mn.F);
                CurrentNode = OpenList.First(mn => mn.F == lowest);

                // add the list to the closed list
                ClosedList.Add(CurrentNode);

                // remove it from the open list
                OpenList.Remove(CurrentNode);


                // if we added the destination to the closed list, we've found a path
                if (ClosedList.FirstOrDefault(l => l.Name == To) != null)
                    break;

                g++;

                // walk the connections
                foreach(string connectionName in CurrentNode.Connections)
                {

                    // if this connection is in the closed list ignore it
                    if( ClosedList.FirstOrDefault(mn => mn.Name == connectionName) != null)
                    {
                        continue;
                    }

                    MapNode CMN = MapNodes[connectionName];

                    // if its not in the Open List
                    if (OpenList.FirstOrDefault(mn => mn.Name == connectionName) == null)
                    {
                        // compute its score, set the parent
                        CMN.G = g;
                        CMN.H = CalculateHScore(CMN, End);
                        CMN.F = CMN.G + CMN.H;
                        CMN.Parent = CurrentNode;

                        // add it to the open list
                        OpenList.Insert(0, CMN);

                    }
                    else
                    {
                        // test if using the current G score makes the route better, if so update it
                        if(g + CMN.H < CMN.F)
                        {
                            CMN.G = g;
                            CMN.F = CMN.G + CMN.H;
                            CMN.Parent = CurrentNode;
                        }
                    }
                }

                if(UseJumpGates && CurrentNode.JBConnection != null)
                {
                    MapNode JMN = MapNodes[CurrentNode.JBConnection];

                    // if this connection is in the closed list ignore it
                    if (ClosedList.FirstOrDefault(mn => mn.Name == CurrentNode.JBConnection) != null)
                    {
                        continue;
                    }



                    // if its not in the Open List
                    if (OpenList.FirstOrDefault(mn => mn.Name == CurrentNode.JBConnection) == null)
                    {
                        // compute its score, set the parent
                        JMN.G = g;
                        JMN.H = CalculateHScore(JMN, End);
                        JMN.F = JMN.G + JMN.H;
                        JMN.Parent = CurrentNode;

                        // add it to the open list
                        OpenList.Insert(0, JMN);

                    }
                    else
                    {
                        // test if using the current G score makes the route better, if so update it
                        if (g + JMN.H < JMN.F)
                        {
                            JMN.G = g;
                            JMN.F = JMN.G + JMN.H;
                            JMN.Parent = CurrentNode;
                        }
                    }
                }
            }

            // build the path

            List<string> Route = new List<string>();

            while(CurrentNode != null)
            {
                Route.Add(CurrentNode.Name);
                CurrentNode = CurrentNode.Parent;
            }
            Route.Reverse();
            return Route;
        }


        static Dictionary<string, MapNode> MapNodes { get; set; }


    }
}
