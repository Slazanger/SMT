
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
        private bool m_ShowShipJumps;

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


        [Category("General")]
        [DisplayName("Selected System")]
        public Color SelectedSystemColour { get; set; }

        [Category("General")]
        [DisplayName("System Popup")]
        public bool ShowSystemPopup { get; set; }



        [Category("Character")]
        [DisplayName("Highlight")]
        public Color CharacterHighlightColour { get; set; }

        [Category("Character")]
        [DisplayName("Text")]
        public Color CharacterTextColour { get; set; }

        [Category("Character")]
        [DisplayName("Text Size")]
        public int CharacterTextSize { get; set; }



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
                    ShowShipJumps = false;


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
                    ShowShipJumps = false;

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
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowShipKills");
            }
        }

        [Category("ESI Data")]
        [DisplayName("Jumps")]
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

            SelectedSystemColour = Color.FromRgb(255, 255, 255);
            ShowSystemPopup = true;

            CharacterHighlightColour = Color.FromRgb(70, 130, 180);
            CharacterTextColour = Color.FromRgb(0, 0, 0);
            CharacterTextSize = 8;


            DefaultRegion = "Molden Heath";
        }

        public MapConfig()
        {
            SetDefaults();
        }
    }



}
