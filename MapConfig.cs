
using System.ComponentModel;
using System.Windows.Media;


namespace SMT
{

    public class MapConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private bool m_ShowNPCKills;
        private bool m_ShowPodKills;
        private bool m_ShowShipKills;

        [Browsable(false)]
        public string DefaultRegion;


        [Category("Jump Bridges")]
        [DisplayName("Friendly Bridges")]
        public bool ShowJumpBridges { get; set; }

        [Category("Jump Bridges")]
        [DisplayName("Hostile Bridges")]
        public bool ShowHostileJumpBridges { get; set; }

        [Category("Jump Bridges")]
        [DisplayName("Friendly")]
        public Color FriendlyJumpBridgeColour { get; set; }


        [Category("Jump Bridges")]
        [DisplayName("Hostile ")]
        public Color HostileJumpBridgeColour { get; set; }

        [Category("Systems")]
        [DisplayName("Name Size")]
        public int SystemTextSize { get; set; }

        [Category("Systems")]
        [DisplayName("Outline")]
        public Color SystemOutlineColour { get; set; }


        [Category("Systems")]
        [DisplayName("In Region")]
        public Color InRegionSystemColour { get; set; }

        [Category("Systems")]
        [DisplayName("In Region Text")]
        public Color InRegionSystemTextColour { get; set; }

        [Category("Systems")]
        [DisplayName("Out of Region")]
        public Color OutRegionSystemColour { get; set; }

        [Category("Systems")]
        [DisplayName("Out of Region Text")]
        public Color OutRegionSystemTextColour { get; set; }

        [Category("Gates")]
        [DisplayName("Normal")]
        public Color NormalGateColour { get; set; }

        [Category("Gates")]
        [DisplayName("Constellation")]
        public Color ConstellationGateColour { get; set; }



        [Category("General")]
        [DisplayName("Map Background")]
        public Color MapBackgroundColour { get; set; }



        [Category("ESI Data")]
        [DisplayName("ESI Scale")]
        public double ESIOverlayScale { get; set; }

        [Category("ESI Data")]
        [DisplayName("Overlay")]
        public Color ESIOverlayColour { get; set; }

        [Category("Intel")]
        [DisplayName("Overlay")]
        public Color IntelOverlayColour { get; set; }

        [Category("Intel")]
        [DisplayName("Show Intel")]
        public bool ShowIntel { get; set; }




        [Category("ESI Data")]
        [DisplayName("Rat Kills")]
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
                }

                OnPropertyChanged("ShowNPCKills");
            }
        }

        [Category("ESI Data")]
        [DisplayName("Pod Kills")]
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
                }

                OnPropertyChanged("ShowPodKills");
            }
        }

        [Category("ESI Data")]
        [DisplayName("Ship Kills")]
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
                }

                OnPropertyChanged("ShowShipKills");
            }
        }





        public void SetDefaults()
        {
            ShowJumpBridges = true;
            ShowHostileJumpBridges = true;
            ESIOverlayScale = 1.0f;
            FriendlyJumpBridgeColour = Color.FromRgb(102, 205, 170);
            HostileJumpBridgeColour = Color.FromRgb(250, 128, 114);

            SystemOutlineColour = Color.FromRgb(0, 0, 0);
            InRegionSystemColour = Color.FromRgb(255, 239, 213);
            InRegionSystemTextColour = Color.FromRgb(0, 0, 0);

            OutRegionSystemColour = Color.FromRgb(218, 165, 32);
            OutRegionSystemTextColour = Color.FromRgb(0, 0, 0);

            MapBackgroundColour = Color.FromRgb(105, 105, 105);

            ESIOverlayColour = Color.FromRgb(188, 143, 143);

            IntelOverlayColour = Color.FromRgb(178, 34, 34);

            ShowPodKills = true;
            ShowIntel = true;
            SystemTextSize = 12;

            NormalGateColour = Color.FromRgb(255, 248, 220);
            ConstellationGateColour = Color.FromRgb(128, 128, 128);

            DefaultRegion = "Heimatar";
        }

        public MapConfig()
        {
            SetDefaults();
        }
    }



}
