namespace SMT.EVEData
{
    public class JumpRoute
    {
        public List<Navigation.RoutePoint> CurrentRoute { get; set; }
        public List<string> WayPoints { get; set; }
        public double MaxLY { get; set; }
        public int JDC { get; set; }

        public List<string> AvoidSystems { get; set; }

        public Dictionary<string, List<string>> AlternateMids { get; set; }

        public JumpRoute()
        {
            MaxLY = 7.0;
            JDC = 5;

            CurrentRoute = new List<Navigation.RoutePoint>();
            WayPoints = new List<string>();
            AvoidSystems = new List<string>();
            AlternateMids = new Dictionary<string, List<string>>();
        }

        public void Recalculate()
        {
            CurrentRoute.Clear();

            if (WayPoints.Count < 2)
            {
                return;
            }

            double actualMaxLY = MaxLY;
            if (JDC != 5)
            {
                actualMaxLY *= .9;
            }

            // new routing
            string start = string.Empty;
            string end = WayPoints[0];

            List<string> avoidSystems = AvoidSystems;

            AlternateMids.Clear();

            // loop through all the waypoints
            for (int i = 1; i < WayPoints.Count; i++)
            {
                start = end;
                end = WayPoints[i];

                List<Navigation.RoutePoint> sysList = Navigation.NavigateCapitals(start, end, actualMaxLY, null, avoidSystems);

                if (sysList != null)
                {
                    foreach (Navigation.RoutePoint s in sysList)
                    {
                        // for multiple waypoint routes, the first in the new and last item in the list will be the same system, so remove
                        if (CurrentRoute.Count > 0 && CurrentRoute.Last().SystemName == s.SystemName)
                        {
                            CurrentRoute.Last().LY = s.LY;
                        }
                        else
                        {
                            CurrentRoute.Add(s);
                        }
                    }

                    if (sysList.Count > 2)
                    {
                        for (int j = 2; j < sysList.Count; j++)
                        {
                            List<string> a = Navigation.GetSystemsWithinXLYFrom(sysList[j - 2].SystemName, actualMaxLY, false, false);
                            List<string> b = Navigation.GetSystemsWithinXLYFrom(sysList[j].SystemName, actualMaxLY, false, false);

                            IEnumerable<string> alternatives = a.Intersect(b);
                            string currentMid = sysList[j - 1].SystemName;
                            List<string> alternateMids = new List<string>();

                            foreach (string mid in alternatives)
                            {
                                if (mid != currentMid)
                                {
                                    alternateMids.Add(mid);
                                }
                            }

                            AlternateMids[currentMid] = alternateMids;
                        }
                    }
                }
            }
        }
    }
}
