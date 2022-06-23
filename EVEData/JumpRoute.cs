using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace SMT.EVEData
{
    public class JumpRoute
    {
        public ObservableCollection<Navigation.RoutePoint> CurrentRoute { get; set; }
        public ObservableCollection<string> WayPoints { get; set; }
        public double MaxLY { get; set; }
        public int JDC { get; set; }

        public ObservableCollection<string> AvoidSystems { get; set; }
        public ObservableCollection<string> AvoidRegions { get; set; }

        public Dictionary<string, ObservableCollection<string>> AlternateMids { get; set; }


        public JumpRoute()
        {
            MaxLY = 7.0;
            JDC = 5;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CurrentRoute = new ObservableCollection<Navigation.RoutePoint>();
                WayPoints = new ObservableCollection<string>();
                AvoidRegions = new ObservableCollection<string>();
                AvoidSystems = new ObservableCollection<string>();
                AlternateMids = new Dictionary<string, ObservableCollection<string>>();

            }), DispatcherPriority.Normal, null);
        }

        public void Recalculate()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CurrentRoute.Clear();
            }), DispatcherPriority.Normal, null);


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

            List<string> avoidSystems = AvoidSystems.ToList();


            AlternateMids.Clear();


            // loop through all the waypoints
            for (int i = 1; i < WayPoints.Count; i++)
            {
                start = end;
                end = WayPoints[i];



                List<Navigation.RoutePoint> sysList = Navigation.NavigateCapitals(start, end, actualMaxLY, null, avoidSystems);

                if (sysList != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {

                        foreach (Navigation.RoutePoint s in sysList)
                        {
                            // for multiple waypoint routes, the first in the new and last item in the list will be the same system, so remove
                            if(CurrentRoute.Count > 0 && CurrentRoute.Last().SystemName == s.SystemName)
                            {
                                CurrentRoute.Last().LY = s.LY;
                            }
                            else
                            {
                                CurrentRoute.Add(s);
                            }
                        }

                        if(sysList.Count > 2 )
                        {
                            for (int j = 2; j < sysList.Count; j++)
                            {
                                List<string> a = Navigation.GetSystemsWithinXLYFrom(CurrentRoute[j - 2].SystemName, MaxLY);
                                List<string> b = Navigation.GetSystemsWithinXLYFrom(CurrentRoute[j].SystemName, MaxLY);

                                IEnumerable<string> alternatives = a.AsQueryable().Intersect(b);

                                AlternateMids[CurrentRoute[j - 1].SystemName] = new ObservableCollection<string>();
                                foreach (string mid in alternatives)
                                {
                                    if (mid != CurrentRoute[j - 1].SystemName)
                                    {
                                        AlternateMids[CurrentRoute[j - 1].SystemName].Add(mid);
                                    }
                                }
                            }

                        }
                    }), DispatcherPriority.Normal, null);
                }
            }
        }
    }
}
