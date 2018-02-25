using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace SMT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Main Region Manager
        /// </summary>
        public EVEData.EveManager EVEManager { get; set; }


        public EVEData.AnomManager ANOMManager { get; set; }


        private static NLog.Logger OutputLog = NLog.LogManager.GetCurrentClassLogger();

        private MapConfig MapConf;

        public MainWindow()
        {
            OutputLog.Info("Starting App..");

            InitializeComponent();

            // load any custom map settings off disk
            string mapConfigFileName = AppDomain.CurrentDomain.BaseDirectory + @"\MapConfig.dat";
            OutputLog.Info("Loading Map config from {0}", mapConfigFileName);


            if (File.Exists(mapConfigFileName))
            {
                try
                {
                    XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
                    FileStream fs = new FileStream(mapConfigFileName, FileMode.Open);
                    XmlReader xmlr = XmlReader.Create(fs);

                    MapConf = (MapConfig)xms.Deserialize(xmlr);
                    fs.Close();
                }
                catch
                {
                    MapConf = new MapConfig();
                    MapConf.MapColours = new List<MapColours>();
                    MapConf.SetDefaultColours();
                }
            }
            else
            {
                MapConf = new MapConfig();
                MapConf.MapColours = new List<MapColours>();
                MapConf.SetDefaultColours();
            }

            EVEManager = new EVEData.EveManager();
            EVEData.EveManager.SetInstance(EVEManager);

            // if we want to re-build the data as we've changed the format, recreate it all from scratch
            bool initFromScratch = false;
            if (initFromScratch)
            {
                EVEManager.CreateFromScratch();
            }
            else
            {
                EVEManager.LoadFromDisk();
            }

            EVEManager.SetupIntelWatcher();
            RawIntelBox.ItemsSource = EVEManager.IntelDataList;

            
            // load jump bridge data
            EVEManager.LoadJumpBridgeData();
            EVEManager.StartUpdateKillsFromESI();
            EVEManager.StartUpdateJumpsFromESI();
            EVEManager.StartUpdateSOVFromESI();

            MapControlsPropertyGrid.SelectedObject = MapConf;

            ColourListDropdown.ItemsSource = MapConf.MapColours;
            CharacterList.ItemsSource = EVEManager.LocalCharacters;

            MapColours selectedColours = MapConf.MapColours[0];

            // find the matching active colour scheme
            foreach (MapColours mc in MapConf.MapColours)
            {
                if (MapConf.DefaultColourSchemeName == mc.Name)
                {
                    selectedColours = mc;
                }
            }

            // load the dockmanager layout
            string dockManagerLayoutName = AppDomain.CurrentDomain.BaseDirectory + @"\Layout.dat";
            if (File.Exists(dockManagerLayoutName))
            {
                try
                {
                    Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                    using (var sr = new StreamReader(dockManagerLayoutName))
                    {
                        ls.Deserialize(sr);
                    }
                }
                catch
                {
                }
            }

            RegionRC.MapConf = MapConf;
            RegionRC.Init();
            RegionRC.SelectRegion(MapConf.DefaultRegion);

            // load the anom data
            string anomDataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Anoms.dat";
            if (File.Exists(anomDataFilename))
            {
                try
                {
                    XmlSerializer xms = new XmlSerializer(typeof(EVEData.AnomManager));

                    FileStream fs = new FileStream(anomDataFilename, FileMode.Open);
                    XmlReader xmlr = XmlReader.Create(fs);

                    ANOMManager = (EVEData.AnomManager)xms.Deserialize(xmlr);
                    fs.Close();
                }
                catch
                {
                    ANOMManager = new EVEData.AnomManager();
                }
            }
            else
            {
                ANOMManager = new EVEData.AnomManager();
            }

            MainAnomGrid.DataContext = ANOMManager;
            RegionRC.ANOMManager = ANOMManager;

            MainRouteGrid.DataContext = RegionRC;


            // ColourListDropdown.SelectedItem = selectedColours;
            ColoursPropertyGrid.SelectedObject = selectedColours;
            MapConf.ActiveColourScheme = selectedColours;
            ColoursPropertyGrid.PropertyChanged += ColoursPropertyGrid_PropertyChanged;
            MapConf.PropertyChanged += MapConf_PropertyChanged;

            Closed += MainWindow_Closed;

            EVEManager.IntelAddedEvent += OnIntelAdded;
            //MapConf.PropertyChanged += RegionRC.MapObjectChanged;
        }

        private void MapConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "AlwaysOnTop")
            {
                if(MapConf.AlwaysOnTop)
                {
                    this.Topmost = true;
                }
                else
                {
                    this.Topmost = false;
                }
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // save off the dockmanager layout
            string dockManagerLayoutName = AppDomain.CurrentDomain.BaseDirectory + @"\Layout.dat";
            try
            {
                Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                using (var sw = new StreamWriter(dockManagerLayoutName))
                {
                    ls.Serialize(sw);
                }
            }
            catch
            {
            }


            try
            {
                // Save the Map Colours
                string mapConfigFileName = AppDomain.CurrentDomain.BaseDirectory + @"\MapConfig.dat";

                // now serialise the class to disk
                XmlSerializer xms = new XmlSerializer(typeof(MapConfig));
                using (TextWriter tw = new StreamWriter(mapConfigFileName))
                {
                    xms.Serialize(tw, MapConf);
                }
            }
            catch
            {
            }


            try
            {
                // save the Anom Data
                // now serialise the class to disk
                XmlSerializer anomxms = new XmlSerializer(typeof(EVEData.AnomManager));
                string anomDataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Anoms.dat";

                using (TextWriter tw = new StreamWriter(anomDataFilename))
                {
                    anomxms.Serialize(tw, ANOMManager);
                }
            }
            catch
            { }


            // save the character data
            EVEManager.SaveData();
        }


        private void ColoursPropertyGrid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RegionRC.ReDrawMap();
        }


        private void OnIntelAdded()
        {
            if(MapConf.PlayIntelSound)
            {
                Uri uri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                var player = new MediaPlayer();
                player.Open(uri);
                player.Play();
            }
        }

        private void refreshData_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.StartUpdateKillsFromESI();
            EVEManager.StartUpdateJumpsFromESI();
        }
         
        private void ResetColourData_Click(object sender, RoutedEventArgs e)
        {
            MapConf.MapColours = new List<MapColours>();
            MapConf.SetDefaultColours();
            ColourListDropdown.ItemsSource = MapConf.MapColours;
            ColourListDropdown.SelectedItem = MapConf.MapColours[0];

            RegionRC.ReDrawMap();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            About popup = new About();
            popup.ShowDialog();
        }

        private void ColourListDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MapColours newSelection = ColourListDropdown.SelectedItem as MapColours;
            if (newSelection == null)
            {
                return;
            }

            MapConf.ActiveColourScheme = newSelection;
            ColoursPropertyGrid.SelectedObject = newSelection;
            ColoursPropertyGrid.Update();

            MapConf.DefaultColourSchemeName = newSelection.Name;
        }

        private void btnUpdateAnomList_Click(object sender, RoutedEventArgs e)
        {
            string pasteData = Clipboard.GetText();

            if (pasteData != null || pasteData != string.Empty)
            {
                EVEData.AnomData ad = ANOMManager.ActiveSystem;

                if (ad != null)
                {
                    ad.UpdateFromPaste(pasteData);
                    AnomSigList.Items.Refresh();
                    AnomSigList.UpdateLayout();
                    CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
                }
            }
        }

        private void btnClearAnomList_Click(object sender, RoutedEventArgs e)
        {
            EVEData.AnomData ad = ANOMManager.ActiveSystem;
            if (ad != null)
            {
                ad.Anoms.Clear();
                AnomSigList.Items.Refresh();
                AnomSigList.UpdateLayout();
                CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
            }
        }

        private void btn_AddCharacter_Click(object sender, RoutedEventArgs e)
        {
            string eSILogonURL = EVEManager.GetESILogonURL();

            LogonWindow logonBrowserWindow = new LogonWindow();
            logonBrowserWindow.logonBrowser.Navigate(eSILogonURL);

            logonBrowserWindow.URLName.Text = eSILogonURL;
            logonBrowserWindow.ShowDialog();
        }

        public Color stringToColour(string str)
        {
            int hash = 0;

            foreach (char c in str.ToCharArray())
            {
                hash = c + ((hash << 5) - hash);
            }

            double R = (((byte) (hash & 0xff) / 255.0) * 80.0 ) + 127.0 ;
            double G = (((byte) ((hash >> 8) & 0xff) / 255.0 ) * 80.0 ) + 127.0;
            double B = (((byte) ((hash >> 16) & 0xff) / 255.0)* 80.0) + 127.0;
           
            return Color.FromArgb(100,(byte)R, (byte)G, (byte)B);
        }

        private void RawIntelBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(RawIntelBox.SelectedItem == null)
            {
                return;
            }
           
            EVEData.IntelData intel = RawIntelBox.SelectedItem as EVEData.IntelData;

            foreach(string s in intel.IntelString.Split(' '))
            {
                if(s=="")
                {
                    continue;
                }
                if (EVEManager.Systems.Any(x => (x.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)))
                {
                    EVEData.System sys = EVEManager.GetEveSystem(s);
                    if (sys == null)
                    {
                        return;
                    }

                    if(RegionRC.Region.Name != sys.Region)
                    {
                        RegionRC.SelectRegion(sys.Region);
                    }


                    RegionRC.SelectSystem(s);
                    return;
                }
            }

        }

        private void ClearWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.Character c = RegionRC.ActiveCharacter as EVEData.Character;
            if(c!=null)
            {
                c.ActiveRoute.Clear();
                c.Waypoints.Clear();
            }
        }
    }

}