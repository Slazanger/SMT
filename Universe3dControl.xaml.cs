using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SMT
{
    /// <summary>
    /// Interaction logic for Universe3DControl.xaml
    /// </summary>
    public partial class Universe3DControl : UserControl, INotifyPropertyChanged
    {
  
        private double m_ESIOverlayScale = 1.0f;
        private bool m_ShowNPCKills = false;
        private bool m_ShowPodKills = false;
        private bool m_ShowShipKills = false;
        private bool m_ShowShipJumps = false;
        private bool m_ShowJumpBridges = true;

        public MapConfig MapConf { get; set; }

        public List<EVEData.Navigation.RoutePoint> ActiveRoute { get; set; }

        public bool FollowCharacter
        {
            get
            {
                return FollowCharacterChk.IsChecked.Value;
            }
            set
            {
                FollowCharacterChk.IsChecked = value;
            }
        }

        public Universe3DControl() 
        {
            InitializeComponent();
            DataContext = this;
        }

        private struct GateHelper
        {
            public EVEData.System from { get; set; }
            public EVEData.System to { get; set; }

            public bool RegionalGate { get; set; }
        }

        public bool ShowJumpBridges
        {
            get
            {
                return m_ShowJumpBridges;
            }
            set
            {
                m_ShowJumpBridges = value;
                OnPropertyChanged("ShowJumpBridges");
            }
        }

        public double ESIOverlayScale
        {
            get
            {
                return m_ESIOverlayScale;
            }
            set
            {
                m_ESIOverlayScale = value;
                OnPropertyChanged("ESIOverlayScale");
            }
        }

        public bool ShowNPCKills
        {
            get
            {
                return m_ShowNPCKills;
            }

            set
            {
                m_ShowNPCKills = value;

                if (m_ShowNPCKills)
                {
                    ShowPodKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowNPCKills");
            }
        }

        public bool ShowPodKills
        {
            get
            {
                return m_ShowPodKills;
            }

            set
            {
                m_ShowPodKills = value;
                if (m_ShowPodKills)
                {
                    ShowNPCKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowPodKills");
            }
        }

        public bool ShowShipKills
        {
            get
            {
                return m_ShowShipKills;
            }

            set
            {
                m_ShowShipKills = value;
                if (m_ShowShipKills)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowShipKills");
            }
        }

        public bool ShowShipJumps
        {
            get
            {
                return m_ShowShipJumps;
            }

            set
            {
                m_ShowShipJumps = value;
                if (m_ShowShipJumps)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipKills = false;
                }

                OnPropertyChanged("ShowShipJumps");
            }
        }

        public EVEData.LocalCharacter ActiveCharacter { get; set; }

        public void UpdateActiveCharacter(EVEData.LocalCharacter lc)
        {
            ActiveCharacter = lc;

            if (FollowCharacterChk.IsChecked.HasValue && (bool)FollowCharacterChk.IsChecked)
            {
                CentreMapOnActiveCharacter();
            }
        }

        public static readonly RoutedEvent RequestRegionSystemSelectEvent = EventManager.RegisterRoutedEvent("RequestRegionSystem", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Universe3DControl));

        public event RoutedEventHandler RequestRegionSystem
        {
            add { AddHandler(RequestRegionSystemSelectEvent, value); }
            remove { RemoveHandler(RequestRegionSystemSelectEvent, value); }
        }

        private List<GateHelper> universeSysLinksCache;

        private List<KeyValuePair<string, double>> activeJumpSpheres;

        private double universeWidth;
        private double universeDepth;
        private double universeHeight;
        private double universeXMin;
        private double universeXMax;
        private double universeScale;

        private double universeZMin;
        private double universeZMax;

        private double universeYMin;
        private double universeYMax;




        private EVEData.EveManager EM;



        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;

        private int uiRefreshTimer_interval = 0;

        public void Init()
        {
            EM = EVEData.EveManager.Instance;

            universeSysLinksCache = new List<GateHelper>();
            activeJumpSpheres = new List<KeyValuePair<string, double>>();

            universeXMin = 0;
            universeXMax = 0;

            universeZMin = 0;
            universeZMax = 0;


            universeYMin = 0;
            universeYMax = 0;


            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 5);
            uiRefreshTimer.Start();

            PropertyChanged += Universe3DControl_PropertyChanged;

            DataContext = this;

            foreach (EVEData.System sys in EM.Systems)
            {
                foreach (string jumpTo in sys.Jumps)
                {
                    EVEData.System to = EM.GetEveSystem(jumpTo);

                    bool NeedsAdd = true;
                    foreach (GateHelper gh in universeSysLinksCache)
                    {
                        if (((gh.from == sys) || (gh.to == sys)) && ((gh.from == to) || (gh.to == to)))
                        {
                            NeedsAdd = false;
                            break;
                        }
                    }

                    if (NeedsAdd)
                    {
                        GateHelper g = new GateHelper();
                        g.from = sys;
                        g.to = to;
                        g.RegionalGate = sys.Region != to.Region;
                        universeSysLinksCache.Add(g);
                    }
                }

                if (sys.ActualX < universeXMin)
                {
                    universeXMin = sys.ActualX;
                }

                if (sys.ActualX > universeXMax)
                {
                    universeXMax = sys.ActualX;
                }

                if (sys.ActualZ < universeZMin)
                {
                    universeZMin = sys.ActualZ;
                }

                if (sys.ActualZ > universeZMax)
                {
                    universeZMax = sys.ActualZ;
                }

                if (sys.ActualY < universeYMin)
                {
                    universeYMin = sys.ActualY;
                }

                if (sys.ActualY > universeYMax)
                {
                    universeYMax = sys.ActualY;
                }


            }

            universeWidth = universeXMax - universeXMin;
            universeDepth = universeZMax - universeZMin;
            universeHeight = universeYMax - universeYMin;

            List<EVEData.System> globalSystemList = new List<EVEData.System>(EM.Systems);
            globalSystemList.Sort((a, b) => string.Compare(a.Name, b.Name));
            GlobalSystemDropDownAC.ItemsSource = globalSystemList;


            UniverseMain3DViewPort.EffectsManager = new DefaultEffectsManager();



            HelixToolkit.Wpf.SharpDX.PerspectiveCamera pc = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera();
            pc.NearPlaneDistance = 0.1;
            pc.FarPlaneDistance = 100000;

            pc.Position = new Point3D(4000, 4000, 10000);

            pc.UpDirection = new Vector3D(0,0,1);
            pc.LookDirection = new Vector3D(-1,0, 0);

            pc.LookAt(new Point3D(4000, 4000, 4000), 1);


            UniverseMain3DViewPort.Camera = pc;


            ReDrawMap(true);
        }


        private void SetJumpRange_Click(object sender, RoutedEventArgs e)
        {
       }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            uiRefreshTimer_interval++;

            bool FullRedraw = false;
            bool FastUpdate = true;
            bool DataRedraw = false;

            if (uiRefreshTimer_interval == 4)
            {
                uiRefreshTimer_interval = 0;
                DataRedraw = false;
            }

            if (FollowCharacterChk.IsChecked.HasValue && (bool)FollowCharacterChk.IsChecked)
            {
                CentreMapOnActiveCharacter();
            }
           // ReDrawMap(FullRedraw, DataRedraw, FastUpdate);
        }

        private void VHSystems_MouseClicked(object sender, RoutedEventArgs e)
        {
            EVEData.System sys = (EVEData.System)e.OriginalSource;

            ContextMenu cm = this.FindResource("SysRightClickContextMenu") as ContextMenu;

            cm.DataContext = sys;
            cm.IsOpen = true;

            MenuItem setDesto = cm.Items[2] as MenuItem;
            MenuItem addWaypoint = cm.Items[3] as MenuItem;

            if (ActiveCharacter != null && ActiveCharacter.ESILinked)
            {
                setDesto.IsEnabled = true;
                addWaypoint.IsEnabled = true;
            }

            // update SOV
            MenuItem SovHeader = cm.Items[6] as MenuItem;
            SovHeader.Items.Clear();
            SovHeader.IsEnabled = false;

            if (sys.SOVAllianceIHUB != 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "IHUB: " + EM.GetAllianceTicker(sys.SOVAllianceIHUB);
                mi.DataContext = sys.SOVAllianceIHUB;
                mi.Click += VHSystems_SOV_Clicked;
                SovHeader.IsEnabled = true;
                SovHeader.Items.Add(mi);
            }

            if (sys.SOVAllianceTCU != 0)
            {
                MenuItem mi = new MenuItem();
                mi.DataContext = sys.SOVAllianceTCU;
                mi.Header = "TCU : " + EM.GetAllianceTicker(sys.SOVAllianceTCU);
                mi.Click += VHSystems_SOV_Clicked;
                SovHeader.IsEnabled = true;
                SovHeader.Items.Add(mi);
            }

            // update stats
            MenuItem StatsHeader = cm.Items[7] as MenuItem;
            StatsHeader.Items.Clear();
            StatsHeader.IsEnabled = false;

            if (sys.HasNPCStation)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "NPC Station(s)";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.HasIceBelt)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Ice Belts";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.HasJoveObservatory)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Jove Observatory";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.JumpsLastHour > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Jumps : " + sys.JumpsLastHour;
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.ShipKillsLastHour > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Ship Kills : " + sys.ShipKillsLastHour;
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.PodKillsLastHour > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Pod Kills : " + sys.PodKillsLastHour;
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.NPCKillsLastHour > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "NPC Kills : " + sys.NPCKillsLastHour + " (Delta: " + sys.NPCKillsDeltaLastHour + ")";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }

            if (sys.RadiusAU > 0)
            {
                MenuItem mi = new MenuItem();
                mi.Header = "Radius : " + sys.RadiusAU.ToString("#.##") + " (AU)";
                StatsHeader.IsEnabled = true;
                StatsHeader.Items.Add(mi);
            }
        }

        private void VHSystems_SOV_Clicked(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            long ID = (long)mi.DataContext;

            if (ID != 0)
            {
                string uRL = string.Format("https://evewho.com/alliance/{0}", ID);
                System.Diagnostics.Process.Start(uRL);
            }
        }

        private void Universe3DControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
           // ReDrawMap(false, true, true);
        }

 
        /// <summary>
        /// Redraw the map
        /// </summary>
        /// <param name="FullRedraw">Clear all the static items or not</param>
        public void ReDrawMap(bool FullRedraw = false, bool DataRedraw = false, bool FastUpdate = false)
        {

            
            double Size = 8000;
            double XScale = Size / universeWidth;
            double ZScale = Size / universeDepth;
            universeScale = Math.Min(XScale, ZScale);

            var mb = new MeshBuilder();
            var mbRegion = new MeshBuilder();

            foreach (GateHelper gh in universeSysLinksCache)
            {

                double X1 = (gh.from.ActualX - universeXMin) * universeScale;
                double Y1 = (gh.from.ActualZ - universeZMin) * universeScale;
                double Z1 = (gh.from.ActualY - universeYMin) * universeScale;


                double X2 = (gh.to.ActualX - universeXMin) * universeScale;
                double Y2 = (gh.to.ActualZ - universeZMin) * universeScale;
                double Z2 = (gh.to.ActualY - universeYMin) * universeScale;


                if(gh.RegionalGate)
                {
                    mbRegion.AddCylinder(new Vector3((float)X1, (float)Y1, (float)Z1), new Vector3((float)X2, (float)Y2, (float)Z2), 1, 4);
                }
                else
                {
                    mb.AddCylinder(new Vector3((float)X1, (float)Y1, (float)Z1), new Vector3((float)X2, (float)Y2, (float)Z2), 1, 4);
                }
            }

            PhongMaterial em = PhongMaterials.Gray;
            PhongMaterial emrg = PhongMaterials.Red;


            MeshGeometryModel3D geomModelGates = new MeshGeometryModel3D { Geometry = mb.ToMeshGeometry3D(), Material = em };
            UniverseMain3DViewPort.Items.Add(geomModelGates);

            MeshGeometryModel3D geomModelRegionalGates = new MeshGeometryModel3D { Geometry = mbRegion.ToMesh(), Material = emrg };
            UniverseMain3DViewPort.Items.Add(geomModelRegionalGates);


            var mbSystems = new MeshBuilder();

            foreach (EVEData.System sys in EM.Systems)
            {
                double X1 = (sys.ActualX - universeXMin) * universeScale;
                double Y1 = (sys.ActualZ - universeZMin) * universeScale;
                double Z1 = (sys.ActualY - universeYMin) * universeScale;

                Vector3 sysCentre = new Vector3((float)X1, (float)Y1, (float)Z1);

                mbSystems.AddBox(sysCentre, 10, 10, 10);



//                BillboardTextVisual3D sysTextBB = new BillboardTextVisual3D();
//                
//                sysTextBB.Position = sysCentre;
//                sysTextBB.Text = "    " + sys.Name;
//                UniverseMain3DViewPort.Children.Add(sysTextBB);

            }




            PhongMaterial emsys = PhongMaterials.White;
            MeshGeometryModel3D geomModelSystems = new MeshGeometryModel3D { Geometry = mbSystems.ToMesh(), Material = emsys};

            UniverseMain3DViewPort.Items.Add(geomModelSystems);

//            UniverseMain3DViewPort.Camera.Position = new Point3D(4000, 4000, 1000);
//            UniverseMain3DViewPort.Camera.LookAt(new Point3D(0, 0, 0), 1);

            //            UniverseMain3DViewPort.Camera.ZoomExtents();



        }

        public void ShowSystem(string SystemName)
        {
            EVEData.System sd = EM.GetEveSystem(SystemName);

            if (sd != null)
            {
                // actual
                double X1 = (sd.ActualX - universeXMin) * universeScale;
                double Y1 = (universeDepth - (sd.ActualZ - universeZMin)) * universeScale;

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void GlobalSystemDropDownAC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EVEData.System sd = GlobalSystemDropDownAC.SelectedItem as EVEData.System;

            if (sd != null)
            {
                FollowCharacterChk.IsChecked = false;
                ShowSystem(sd.Name);
            }
        }

        /// <summary>
        /// Dotlan Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemDotlan_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("http://evemaps.dotlan.net/map/{0}/{1}", rd.DotLanRef, eveSys.Name);
            System.Diagnostics.Process.Start(uRL);
        }

        /// <summary>
        /// ZKillboard Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemZKB_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("https://zkillboard.com/system/{0}", eveSys.ID);
            System.Diagnostics.Process.Start(uRL);
        }

        private void SysContexMenuShowInRegion_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System s = ((System.Windows.Controls.MenuItem)e.OriginalSource).DataContext as EVEData.System;

            RoutedEventArgs newEventArgs = new RoutedEventArgs(RequestRegionSystemSelectEvent, s.Name);
            RaiseEvent(newEventArgs);
        }

        private void FollowCharacterChk_Checked(object sender, RoutedEventArgs e)
        {
            CentreMapOnActiveCharacter();
        }

        private void CentreMapOnActiveCharacter()
        {
 
        }

  
        private void RecentreBtn_Click(object sender, RoutedEventArgs e)
        {
            CentreMapOnActiveCharacter();
        }

        private void SysContexMenuItemSetDestination_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            if (ActiveCharacter != null)
            {
                ActiveCharacter.AddDestination(eveSys.ID, true);
            }
        }

        private void SysContexMenuItemAddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            EVEData.System eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.System;
            if (ActiveCharacter != null)
            {
                ActiveCharacter.AddDestination(eveSys.ID, false);
            }
        }
    }
}