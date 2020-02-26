using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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

        private int m_MaxIntelSeconds;
        private string m_DefaultRegion;

        [Browsable(false)]
        public string DefaultRegion
        {
            get
            {
                return m_DefaultRegion;
            }
            set
            {
                m_DefaultRegion = value;
                OnPropertyChanged("DefaultRegion");
            }
        }

        [Browsable(false)]
        public string DefaultColourSchemeName { get; set; }

        [Browsable(false)]
        public List<MapColours> MapColours { get; set; }

        [Browsable(false)]
        public MapColours ActiveColourScheme;

        private bool m_ShowZKillData;
        [Category("General")]
        [DisplayName("Show ZKillData")]
        public bool ShowZKillData
        {
            get
            {
                return m_ShowZKillData;
            }
            set
            {
                m_ShowZKillData = value;
                OnPropertyChanged("ShowZKillData");
            }
        }

        [Category("General")]
        [DisplayName("System Popup")]
        public bool ShowSystemPopup { get; set; }


        [Category("Incursions")]
        [DisplayName("Show Active Incursions")]
        public bool ShowActiveIncursions { get; set; }


        [Category("Jove")]
        [DisplayName("Show Observatories")]
        public bool ShowJoveObservatories { get; set; }


        [Category("Navigation")]
        [DisplayName("Show Cyno Beacons")]
        public bool ShowCynoBeacons { get; set; }




        [Category("Navigation")]
        [DisplayName("Show Jump Distance")]
        public bool ShowJumpDistance { get; set; }


        private string m_CurrentJumpSystem;
        [Category("Navigation")]
        [DisplayName("Current Jump System"), ReadOnly(true)]
        public string CurrentJumpSystem
        {
            get
            {
                return m_CurrentJumpSystem;
            }
            set
            {
                m_CurrentJumpSystem = value;
                OnPropertyChanged("CurrentJumpSystem");
            }
        }



        private string m_CurrentJumpCharacter;
        [Category("Navigation")]
        [DisplayName("Current Jump Character")]
        [ItemsSource(typeof(JumpCharacterItemsSource))]

        public string CurrentJumpCharacter
        {
            get
            {
                return m_CurrentJumpCharacter;
            }
            set
            {
                m_CurrentJumpCharacter = value;
                OnPropertyChanged("CurrentJumpCharacter");
            }
        }


        [Category("Navigation")]
        [DisplayName("Ship Type")]
        public EVEData.EveManager.JumpShip JumpShipType { get; set; }

        private bool m_JumpRangeInAsOutline;
        [Category("Navigation")]
        [DisplayName("Jump Range as Outline")]
        public bool JumpRangeInAsOutline
        {
            get
            {
                return m_JumpRangeInAsOutline;
            }
            set
            {
                m_JumpRangeInAsOutline = value;
                OnPropertyChanged("JumpRangeInAsOutline");
            }
        }



        [Category("Intel")]
        [DisplayName("Warning Sound")]
        public bool PlayIntelSound { get; set; }

        [Category("Intel")]
        [DisplayName("Max Intel Time (s)")]
        public int MaxIntelSeconds
        {
            get
            {
                return m_MaxIntelSeconds;
            }
            set
            {
                // clamp to 30s miniumum
                if (value > 30)
                {
                    m_MaxIntelSeconds = value;
                }
                else
                {
                    m_MaxIntelSeconds = 30;
                }
            }
        }

        private bool m_AlwaysOnTop;
        [Category("General")]
        [DisplayName("Always on top")]
        public bool AlwaysOnTop
        {
            get
            {
                return m_AlwaysOnTop;
            }
            set
            {
                m_AlwaysOnTop = value;
                OnPropertyChanged("AlwaysOnTop");
            }
        }


        private bool m_ShowTrueSec;
        [Category("General")]
        [DisplayName("Show TrueSec")]
        public bool ShowTrueSec
        {
            get
            {
                return m_ShowTrueSec;
            }
            set
            {
                m_ShowTrueSec = value;
                OnPropertyChanged("ShowTrueSec");
            }
        }


        private bool m_ShowToolBox = true;
        [Category("General")]
        [DisplayName("Show Toolbox")]
        public bool ShowToolBox
        {
            get
            {
                return m_ShowToolBox;
            }
            set
            {
                m_ShowToolBox = value;
                OnPropertyChanged("ShowToolBox");
            }
        }

        private bool m_ShowRegionStandings;

        [Category("Regions")]
        [DisplayName("Show RegionStandings")]
        public bool ShowRegionStandings
        {
            get
            {
                return m_ShowRegionStandings;
            }

            set
            {
                m_ShowRegionStandings = value;

                if (m_ShowRegionStandings)
                {
                    ShowUniverseRats = false;
                    ShowUniversePods = false;
                    ShowUniverseKills = false;
                }

                OnPropertyChanged("ShowRegionStandings");
            }
        }

        private bool m_ShowUniverseRats;
        [Category("Regions")]
        [DisplayName("Show Ratting Stats")]
        public bool ShowUniverseRats
        {
            get
            {
                return m_ShowUniverseRats;
            }

            set
            {
                m_ShowUniverseRats = value;
                if (m_ShowUniverseRats)
                {
                    ShowRegionStandings = false;
                    ShowUniversePods = false;
                    ShowUniverseKills = false;
                }


                OnPropertyChanged("ShowUniverseRats");
            }
        }

        private bool m_ShowUniversePods;
        [Category("Regions")]
        [DisplayName("Show Pod kill Stats")]
        public bool ShowUniversePods
        {
            get
            {
                return m_ShowUniversePods;
            }

            set
            {
                m_ShowUniversePods = value;
                if (ShowUniversePods)
                {
                    ShowRegionStandings = false;
                    ShowUniverseRats = false;
                    ShowUniverseKills = false;
                }


                OnPropertyChanged("ShowUniversePods");
            }
        }

        private bool m_ShowUniverseKills;
        [Category("Regions")]
        [DisplayName("Show Ship kill Stats")]
        public bool ShowUniverseKills
        {
            get
            {
                return m_ShowUniverseKills;
            }

            set
            {
                m_ShowUniverseKills = value;

                if (m_ShowUniverseKills)
                {
                    ShowRegionStandings = false;
                    ShowUniverseRats = false;
                    ShowUniversePods = false;
                }


                OnPropertyChanged("ShowUniverseKills");
            }
        }

        private double m_UniverseDataScale = 1.0f;
        [Category("Regions")]
        [DisplayName("Universe Data Scale")]
        public double UniverseDataScale
        {
            get
            {
                return m_UniverseDataScale;
            }

            set
            {
                m_UniverseDataScale = value;

                if (m_UniverseDataScale < 0.01)
                {
                    m_UniverseDataScale = 0.01;
                }


                OnPropertyChanged("UniverseDataScale");
            }
        }


        private bool m_ShowRattingDataAsDelta;
        [Category("Misc")]
        [DisplayName("Show Ratting Data as Delta")]
        public bool ShowRattingDataAsDelta
        {
            get
            {
                return m_ShowRattingDataAsDelta;
            }
            set
            {
                m_ShowRattingDataAsDelta = value;
                OnPropertyChanged("ShowRattingDataAsDelta");
            }
        }

        private bool m_ShowNegativeRattingDelta;
        [Category("Misc")]
        [DisplayName("Show Negative Ratting Delta")]
        public bool ShowNegativeRattingDelta
        {
            get
            {
                return m_ShowNegativeRattingDelta;
            }
            set
            {
                m_ShowNegativeRattingDelta = value;
                OnPropertyChanged("ShowNegativeRattingDelta");
            }
        }



        private bool m_SOVShowConflicts;
        [Category("SOV")]
        [DisplayName("Show Sov Conflicts")]
        public bool SOVShowConflicts
        {
            get
            {
                return m_SOVShowConflicts;
            }
            set
            {
                m_SOVShowConflicts = value;
                OnPropertyChanged("SOVShowConflicts");
            }
        }


        private bool m_SOVBasedonTCU;
        [Category("SOV")]
        [DisplayName("Show Sov Based on TCU")]
        public bool SOVBasedITCU
        {
            get
            {
                return m_SOVBasedonTCU;
            }
            set
            {
                m_SOVBasedonTCU = value;
                OnPropertyChanged("SOVBasedITCU");
            }
        }



        private bool m_ShowTCUVunerabilities;
        [Category("SOV")]
        [DisplayName("Show TCU Timers")]
        public bool ShowTCUVunerabilities
        {
            get
            {
                return m_ShowTCUVunerabilities;
            }

            set
            {
                m_ShowTCUVunerabilities = value;

                if (m_ShowTCUVunerabilities)
                {
                    ShowIhubVunerabilities = false;
                }

                OnPropertyChanged("ShowTCUVunerabilities");
            }
        }

        private bool m_ShowIhubVunerabilities;
        [Category("SOV")]
        [DisplayName("Show IHUB Timers")]
        public bool ShowIhubVunerabilities
        {
            get
            {
                return m_ShowIhubVunerabilities;
            }

            set
            {
                m_ShowIhubVunerabilities = value;
                if (m_ShowIhubVunerabilities)
                {
                    ShowTCUVunerabilities = false;
                }

                OnPropertyChanged("ShowIhubVunerabilities");
            }
        }

        private int m_UpcomingSovMinutes;

        [Category("SOV")]
        [DisplayName("Upcoming Period (Mins)")]
        public int UpcomingSovMinutes
        {
            get
            {
                return m_UpcomingSovMinutes;
            }

            set
            {
                m_UpcomingSovMinutes = value;
                if (m_UpcomingSovMinutes < 5)
                {
                    m_UpcomingSovMinutes = 5;
                }

                OnPropertyChanged("UpcomingSovMinutes");
            }
        }


        private bool m_ShowCoalition;
        [Category("SOV")]
        [DisplayName("Show Coalition")]
        public bool ShowCoalition
        {
            get
            {
                return m_ShowCoalition;
            }

            set
            {
                m_ShowCoalition = value;
                OnPropertyChanged("ShowCoalition");
            }
        }


        private bool m_ShowADM;
        [Category("SOV")]
        [DisplayName("Show ADM")]
        public bool ShowADM
        {
            get
            {
                return m_ShowADM;
            }

            set
            {
                m_ShowADM = value;
                OnPropertyChanged("ShowADM");
            }
        }


        private float m_UniverseMaxZoomDisplaySystems;
        [Category("Universe View")]
        [DisplayName("Systems Max Zoom")]
        public float UniverseMaxZoomDisplaySystems
        {
            get
            {
                return m_UniverseMaxZoomDisplaySystems;
            }

            set
            {
                m_UniverseMaxZoomDisplaySystems = Math.Min(Math.Max(value, 0.5f), 10.0f);
                OnPropertyChanged("UniverseMaxZoomDisplaySystems");
            }
        }


        private float m_UniverseMaxZoomDisplaySystemsText;
        [Category("Universe View")]
        [DisplayName("Systems Text Max Zoom")]
        public float UniverseMaxZoomDisplaySystemsText
        {
            get
            {
                return m_UniverseMaxZoomDisplaySystemsText;
            }

            set
            {
                m_UniverseMaxZoomDisplaySystemsText = Math.Min(Math.Max(value, 0.5f), 10.0f);
                OnPropertyChanged("UniverseMaxZoomDisplaySystemsText");
            }
        }



        [Category("Navigation")]
        public ObservableCollection<StaticJumpOverlay> StaticJumpPoints;






        public void SetDefaults()
        {
            DefaultRegion = "Molden Heath";
            ShowSystemPopup = true;
            MaxIntelSeconds = 120;
            UpcomingSovMinutes = 30;
            AlwaysOnTop = false;
            ShowToolBox = true;
            ShowZKillData = true;
            ShowTrueSec = true;
            JumpRangeInAsOutline = true;
            MapColours = new List<MapColours>();
            ShowActiveIncursions = true;
            CurrentJumpCharacter = "";
            StaticJumpPoints = new ObservableCollection<StaticJumpOverlay>();
            SOVShowConflicts = true;
            SOVBasedITCU = true;

            UniverseMaxZoomDisplaySystems = 1.3f;
            UniverseMaxZoomDisplaySystemsText = 2.0f;


        }

        public void SetDefaultColours()
        {
            MapColours defaultColours = new MapColours();
            defaultColours.Name = "Default";
            defaultColours.UserEditable = false;
            defaultColours.FriendlyJumpBridgeColour = Color.FromRgb(102, 205, 170);
            defaultColours.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            defaultColours.InRegionSystemColour = Color.FromRgb(255, 239, 213);
            defaultColours.InRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            defaultColours.OutRegionSystemColour = Color.FromRgb(218, 165, 32);
            defaultColours.OutRegionSystemTextColour = Color.FromRgb(0, 0, 0);

            defaultColours.PopupText = Color.FromRgb(0, 0, 0);
            defaultColours.PopupBackground = (Color)ColorConverter.ConvertFromString("#FF959595");


            defaultColours.MapBackgroundColour = (Color)ColorConverter.ConvertFromString("#5E615E");
            defaultColours.RegionMarkerTextColour = (Color)ColorConverter.ConvertFromString("#6E716E");
            defaultColours.RegionMarkerTextColourFull = Color.FromRgb(0, 0, 0);
            defaultColours.ESIOverlayColour = Color.FromRgb(188, 143, 143);
            defaultColours.IntelOverlayColour = Color.FromRgb(178, 34, 34);
            defaultColours.IntelClearOverlayColour = Colors.Orange;

            defaultColours.NormalGateColour = Color.FromRgb(255, 248, 220);
            defaultColours.ConstellationGateColour = Color.FromRgb(128, 128, 128);
            defaultColours.SelectedSystemColour = Color.FromRgb(255, 255, 255);
            defaultColours.CharacterHighlightColour = Color.FromRgb(170, 130, 180);
            defaultColours.CharacterTextColour = Color.FromRgb(240, 190, 10);
            defaultColours.CharacterTextSize = 11;
            defaultColours.SystemTextSize = 12;

            defaultColours.JumpRangeInColour = Color.FromRgb(255, 165, 0);
            defaultColours.JumpRangeInColourHighlight = Color.FromArgb(156, 82, 135, 155);
            defaultColours.JumpRangeOverlapHighlight = Colors.DarkBlue;


            defaultColours.ActiveIncursionColour = Color.FromRgb(110, 82, 77);

            defaultColours.SOVStructureVunerableColour = Color.FromRgb(64, 64, 64);
            defaultColours.SOVStructureVunerableSoonColour = Color.FromRgb(178, 178, 178);

            defaultColours.ConstellationHighlightColour = Color.FromRgb(148, 200, 180);

            defaultColours.TheraEntranceRegion = Colors.YellowGreen;
            defaultColours.TheraEntranceSystem = Colors.YellowGreen;

            defaultColours.ZKillDataOverlay = Colors.Purple;





            MapColours.Add(defaultColours);

            MapColours blueColours = new MapColours();
            blueColours.Name = "Blue";
            blueColours.UserEditable = false;
            blueColours.FriendlyJumpBridgeColour = Color.FromRgb(154, 205, 50);
            blueColours.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            blueColours.InRegionSystemColour = Color.FromRgb(134, 206, 235);
            blueColours.InRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            blueColours.OutRegionSystemColour = Color.FromRgb(112, 108, 124);
            blueColours.OutRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            blueColours.MapBackgroundColour = Color.FromRgb(245, 245, 245);
            blueColours.RegionMarkerTextColour = (Color)ColorConverter.ConvertFromString("#6E716E");
            blueColours.RegionMarkerTextColourFull = Color.FromRgb(0, 0, 0);
            blueColours.ESIOverlayColour = Color.FromRgb(192, 192, 192);
            blueColours.IntelOverlayColour = Color.FromRgb(216, 191, 216);
            blueColours.IntelClearOverlayColour = Colors.Orange;
            blueColours.NormalGateColour = Color.FromRgb(90, 90, 90);
            blueColours.ConstellationGateColour = Color.FromRgb(120, 120, 120);
            blueColours.SelectedSystemColour = Color.FromRgb(0, 0, 0);
            blueColours.CharacterHighlightColour = Color.FromRgb(0, 0, 0);
            blueColours.CharacterTextColour = Color.FromRgb(0, 0, 0);
            blueColours.CharacterTextSize = 8;
            blueColours.SystemTextSize = 12;
            blueColours.JumpRangeInColour = Color.FromRgb(0, 255, 0);
            blueColours.JumpRangeInColourHighlight = Color.FromArgb(156, 82, 135, 155);
            blueColours.JumpRangeOverlapHighlight = Colors.DarkBlue;
            blueColours.ActiveIncursionColour = Color.FromRgb(110, 82, 77);

            blueColours.SOVStructureVunerableColour = Colors.Red;
            blueColours.SOVStructureVunerableSoonColour = Colors.Yellow;
            blueColours.ConstellationHighlightColour = Color.FromRgb(148, 200, 180);

            blueColours.TheraEntranceRegion = Colors.YellowGreen;
            blueColours.TheraEntranceSystem = Colors.YellowGreen;

            blueColours.ZKillDataOverlay = Colors.Purple;

            blueColours.PopupText = Color.FromRgb(0, 0, 0);
            blueColours.PopupBackground = (Color)ColorConverter.ConvertFromString("#FF959595");



            MapColours.Add(blueColours);

            MapColours greyAndRed = new MapColours();
            greyAndRed.Name = "Grey & Red";
            greyAndRed.UserEditable = false;
            greyAndRed.FriendlyJumpBridgeColour = Color.FromRgb(128, 128, 128);
            greyAndRed.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            greyAndRed.InRegionSystemColour = Color.FromRgb(240, 248, 255);
            greyAndRed.InRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            greyAndRed.OutRegionSystemColour = Color.FromRgb(128, 34, 34);
            greyAndRed.OutRegionSystemTextColour = Color.FromRgb(0, 0, 0);
            greyAndRed.MapBackgroundColour = Color.FromRgb(245, 245, 245);
            greyAndRed.RegionMarkerTextColour = (Color)ColorConverter.ConvertFromString("#6E716E");
            greyAndRed.RegionMarkerTextColourFull = Color.FromRgb(0, 0, 0);
            greyAndRed.ESIOverlayColour = Color.FromRgb(192, 192, 192);
            greyAndRed.IntelOverlayColour = Color.FromRgb(80, 34, 34);
            greyAndRed.IntelClearOverlayColour = Colors.Orange;
            greyAndRed.NormalGateColour = Color.FromRgb(80, 80, 80);
            greyAndRed.ConstellationGateColour = Color.FromRgb(120, 120, 120);
            greyAndRed.SelectedSystemColour = Color.FromRgb(0, 0, 0);
            greyAndRed.CharacterHighlightColour = Color.FromRgb(0, 0, 0);
            greyAndRed.CharacterTextColour = Color.FromRgb(0, 0, 0);
            greyAndRed.CharacterTextSize = 8;
            greyAndRed.SystemTextSize = 12;
            greyAndRed.JumpRangeInColour = Color.FromRgb(0, 255, 0);
            greyAndRed.JumpRangeInColourHighlight = Color.FromArgb(156, 82, 135, 155);
            greyAndRed.JumpRangeOverlapHighlight = Colors.DarkBlue;
            greyAndRed.ActiveIncursionColour = Color.FromRgb(110, 82, 77);

            greyAndRed.SOVStructureVunerableColour = Colors.Red;
            greyAndRed.SOVStructureVunerableSoonColour = Colors.Yellow;
            greyAndRed.ConstellationHighlightColour = Color.FromRgb(148, 200, 180);

            greyAndRed.TheraEntranceRegion = Colors.YellowGreen;
            greyAndRed.TheraEntranceSystem = Colors.YellowGreen;

            greyAndRed.PopupText = Color.FromRgb(0, 0, 0);
            greyAndRed.PopupBackground = (Color)ColorConverter.ConvertFromString("#FF959595");


            greyAndRed.ZKillDataOverlay = Colors.Purple;


            MapColours.Add(greyAndRed);

            MapColours dark = new MapColours();
            dark.Name = "Dark";
            dark.UserEditable = false;
            dark.FriendlyJumpBridgeColour = Color.FromRgb(46, 139, 87);
            dark.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            dark.InRegionSystemColour = Color.FromRgb(112, 128, 144);
            dark.InRegionSystemTextColour = Color.FromRgb(128, 128, 128);
            dark.OutRegionSystemColour = Color.FromRgb(224, 255, 255);
            dark.OutRegionSystemTextColour = Color.FromRgb(128, 128, 128);
            dark.MapBackgroundColour = Color.FromRgb(20, 20, 20);
            dark.RegionMarkerTextColour = (Color)ColorConverter.ConvertFromString("#6E716E");
            dark.RegionMarkerTextColourFull = Color.FromRgb(0, 0, 0);
            dark.ESIOverlayColour = Color.FromRgb(209, 201, 202);
            dark.IntelOverlayColour = Color.FromRgb(205, 92, 92);
            dark.IntelClearOverlayColour = Colors.Orange;
            dark.NormalGateColour = Color.FromRgb(192, 192, 192);
            dark.ConstellationGateColour = Color.FromRgb(150, 150, 150);
            dark.SelectedSystemColour = Color.FromRgb(255, 255, 255);
            dark.CharacterHighlightColour = Color.FromRgb(255, 255, 255);
            dark.CharacterTextColour = Color.FromRgb(255, 255, 255);
            dark.CharacterTextSize = 8;
            dark.SystemTextSize = 12;
            dark.JumpRangeInColour = Color.FromRgb(0, 255, 0);
            dark.JumpRangeInColourHighlight = Color.FromArgb(156, 82, 135, 155);
            dark.JumpRangeOverlapHighlight = Colors.DarkBlue;
            dark.ActiveIncursionColour = Color.FromRgb(110, 82, 77);

            dark.SOVStructureVunerableColour = Colors.Red;
            dark.SOVStructureVunerableSoonColour = Colors.Yellow;
            dark.ConstellationHighlightColour = Color.FromRgb(148, 200, 180);

            dark.TheraEntranceRegion = Colors.YellowGreen;
            dark.TheraEntranceSystem = Colors.YellowGreen;

            defaultColours.PopupText = Color.FromRgb(0, 0, 0);
            defaultColours.PopupBackground = (Color)ColorConverter.ConvertFromString("#FF959595");


            dark.ZKillDataOverlay = Colors.Purple;

            MapColours.Add(dark);

            MapColours lateNight = new MapColours();
            lateNight.Name = "Modern Dark";
            lateNight.UserEditable = false;
            lateNight.FriendlyJumpBridgeColour = Color.FromRgb(102, 205, 170);
            lateNight.SystemOutlineColour = Color.FromRgb(0, 0, 0);
            lateNight.InRegionSystemColour = Color.FromRgb(255, 239, 213);
            lateNight.InRegionSystemTextColour = Color.FromRgb(245, 245, 245);
            lateNight.OutRegionSystemColour = Color.FromRgb(218, 165, 32);
            lateNight.OutRegionSystemTextColour = Color.FromRgb(218, 165, 32);
            lateNight.MapBackgroundColour = Color.FromRgb(32, 32, 32);
            lateNight.RegionMarkerTextColour = (Color)ColorConverter.ConvertFromString("#6E716E");
            lateNight.RegionMarkerTextColourFull = Color.FromRgb(0, 0, 0);
            lateNight.ESIOverlayColour = Color.FromRgb(81, 81, 81);
            lateNight.IntelOverlayColour = Color.FromRgb(178, 34, 34);
            lateNight.IntelClearOverlayColour = Colors.Orange;
            lateNight.NormalGateColour = Color.FromRgb(255, 248, 220);
            lateNight.ConstellationGateColour = Color.FromRgb(128, 128, 128);
            lateNight.SelectedSystemColour = Color.FromRgb(173, 255, 47);
            lateNight.CharacterHighlightColour = Color.FromRgb(173, 255, 47);
            lateNight.CharacterTextColour = Color.FromRgb(127, 255, 0);
            lateNight.CharacterTextSize = 8;
            lateNight.SystemTextSize = 13;
            lateNight.JumpRangeInColour = Color.FromRgb(0, 255, 0);
            lateNight.JumpRangeInColourHighlight = Color.FromArgb(156, 82, 135, 155);
            lateNight.JumpRangeOverlapHighlight = Colors.DarkBlue;
            lateNight.ActiveIncursionColour = Color.FromRgb(110, 82, 77);

            lateNight.SOVStructureVunerableColour = Colors.Red;
            lateNight.SOVStructureVunerableSoonColour = Colors.Yellow;
            lateNight.ConstellationHighlightColour = Color.FromRgb(148, 200, 180);

            lateNight.TheraEntranceRegion = Colors.YellowGreen;
            lateNight.TheraEntranceSystem = Colors.YellowGreen;

            lateNight.PopupText = Color.FromRgb(200, 200, 200);
            lateNight.PopupBackground = Colors.DarkGray;


            lateNight.ZKillDataOverlay = Colors.Purple;

            MapColours.Add(lateNight);

            ActiveColourScheme = defaultColours;
        }

        public MapConfig()
        {
            SetDefaults();
        }



    }

    public class JumpCharacterItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            ItemCollection sizes = new ItemCollection();
            sizes.Add("");

            foreach (EVEData.LocalCharacter c in EVEData.EveManager.Instance.LocalCharacters)
            {
                sizes.Add(c.Name);
            }
            return sizes;
        }
    }
}