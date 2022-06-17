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
                            CurrentRoute.Add(s);
                        }
                    }), DispatcherPriority.Normal, null);
                }
            }


        }

    }
}
