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
using System.Windows.Threading;
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

        private MapConfig MapConf { get; }

        private Xceed.Wpf.AvalonDock.Layout.LayoutDocument RegionLayoutDoc { get; }


        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;


        public MainWindow()
        {
            OutputLog.Info("Starting App..");

            InitializeComponent();


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

            RegionLayoutDoc = null;
            
            // now update the RegionLayoutDoc because the layout loading breaks the binding
            foreach (Xceed.Wpf.AvalonDock.Layout.LayoutPanel ldc in dockManager.Layout.Children.OfType<Xceed.Wpf.AvalonDock.Layout.LayoutPanel>())
            {
                foreach (Xceed.Wpf.AvalonDock.Layout.LayoutDocumentPane ldp in ldc.Children.OfType<Xceed.Wpf.AvalonDock.Layout.LayoutDocumentPane>())
                {
                    foreach (Xceed.Wpf.AvalonDock.Layout.LayoutDocument ld in ldp.Children.OfType<Xceed.Wpf.AvalonDock.Layout.LayoutDocument>())
                    {
                        if (ld.ContentId == "MapRegionContentID")
                        {
                            RegionLayoutDoc = ld;
                        }
                    }
                }
            }

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
            EVEData.EveManager.Instance = EVEManager;

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
            EVEManager.UpdateESIUniverseData();

            MapControlsPropertyGrid.SelectedObject = MapConf;

            ColourListDropdown.ItemsSource = MapConf.MapColours;
            CharacterList.ItemsSource = EVEManager.LocalCharacters;

            TheraConnectionsList.ItemsSource = EVEManager.TheraConnections;

            MapColours selectedColours = MapConf.MapColours[0];

            // find the matching active colour scheme
            foreach (MapColours mc in MapConf.MapColours)
            {
                if (MapConf.DefaultColourSchemeName == mc.Name)
                {
                    selectedColours = mc;
                }
            }




            RegionRC.MapConf = MapConf;
            RegionRC.Init();
            RegionRC.SelectRegion(MapConf.DefaultRegion);


            RegionRC.RegionChanged += RegionRC_RegionChanged;


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

            AddRegionsToUniverse();


            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 4);
            uiRefreshTimer.Start();



            ZKBFeed.ItemsSource = EVEManager.ZKillFeed.KillStream;

        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            RedrawUniverse();
        }

        private void RegionRC_RegionChanged(object sender, PropertyChangedEventArgs e)
        {
            RegionLayoutDoc.Title = "Region : " + RegionRC.Region.Name;
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

            if(e.PropertyName == "ShowZKillData")
            {
                EVEManager.ZKillFeed.PauseUpdate = !MapConf.ShowZKillData; 
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

        private void RedrawUniverse()
        {
            MainUniverseCanvas.Children.Clear();
            AddRegionsToUniverse();
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
            EVEManager.UpdateESIUniverseData();
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

        private void TheraConnectionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    EVEData.TheraConnection tc = dgr.Item as EVEData.TheraConnection;

                    if(tc != null)
                    {
                        RegionRC.SelectSystem(tc.System, true);
                    }
                }
            }
        }

        private void btn_UpdateThera_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.UpdateTheraConnections();
        }


        private void MenuItem_ViewIntelClick(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
 
        }

        private void AddRegionsToUniverse()
        {
            Brush SysOutlineBrush = new SolidColorBrush(MapConf.ActiveColourScheme.SystemOutlineColour);
            Brush SysInRegionBrush = new SolidColorBrush(MapConf.ActiveColourScheme.InRegionSystemColour);
            Brush BackgroundColourBrush = new SolidColorBrush(MapConf.ActiveColourScheme.MapBackgroundColour);

            Brush AmarrBg = new SolidColorBrush(Color.FromArgb(255, 126, 110, 95));
            Brush MinmatarBg = new SolidColorBrush(Color.FromArgb(255, 143, 120, 120));
            Brush GallenteBg = new SolidColorBrush(Color.FromArgb(255, 127, 139, 137));
            Brush CaldariBg = new SolidColorBrush(Color.FromArgb(255, 149, 159, 171));

            Brush TheraBrush = new SolidColorBrush(Colors.YellowGreen);

            MainUniverseCanvas.Background = BackgroundColourBrush;
            MainUniverseGrid.Background = BackgroundColourBrush;

            foreach (EVEData.MapRegion mr in EVEManager.Regions)
            {
                // add circle for system
                Rectangle RegionShape = new Rectangle() { Height = 30, Width = 80 };
                RegionShape.Stroke = SysOutlineBrush;
                RegionShape.StrokeThickness = 1.5;
                RegionShape.StrokeLineJoin = PenLineJoin.Round;
                RegionShape.RadiusX = 5;
                RegionShape.RadiusY = 5;
                RegionShape.Fill = SysInRegionBrush;
                RegionShape.MouseDown += RegionShape_MouseDown;
                RegionShape.DataContext = mr;

                if (mr.Faction == "Amarr")
                {
                    RegionShape.Fill = AmarrBg;
                }
                if (mr.Faction == "Gallente")
                {
                    RegionShape.Fill = GallenteBg;
                }
                if (mr.Faction == "Minmatar")
                {
                    RegionShape.Fill = MinmatarBg;
                }
                if (mr.Faction == "Caldari")
                {
                    RegionShape.Fill = CaldariBg;
                }


                Canvas.SetLeft(RegionShape, mr.RegionX - 40);
                Canvas.SetTop(RegionShape, mr.RegionY - 15);
                Canvas.SetZIndex(RegionShape, 22);
                MainUniverseCanvas.Children.Add(RegionShape);

                Label RegionText = new Label();
                RegionText.Width = 80;
                RegionText.Height = 27;
                RegionText.Content = mr.Name;
                RegionText.Foreground = SysOutlineBrush;
                RegionText.FontSize = 10;
                RegionText.HorizontalAlignment = HorizontalAlignment.Center;
                RegionText.VerticalAlignment = VerticalAlignment.Center;
                RegionText.IsHitTestVisible = false;

                RegionText.HorizontalContentAlignment = HorizontalAlignment.Center;
                RegionText.VerticalContentAlignment = VerticalAlignment.Center;


                Canvas.SetLeft(RegionText, mr.RegionX - 40);
                Canvas.SetTop(RegionText, mr.RegionY - 15);
                Canvas.SetZIndex(RegionText, 23);
                MainUniverseCanvas.Children.Add(RegionText);


                if(mr.Faction != "")
                {

                    Label FactionText = new Label();
                    FactionText.Width = 80;
                    FactionText.Height = 30;
                    FactionText.Content = mr.Faction;
                    FactionText.Foreground = SysOutlineBrush;
                    FactionText.FontSize = 5;
                    FactionText.HorizontalAlignment = HorizontalAlignment.Center;
                    FactionText.VerticalAlignment = VerticalAlignment.Center;
                    FactionText.IsHitTestVisible = false;

                    FactionText.HorizontalContentAlignment = HorizontalAlignment.Center;
                    FactionText.VerticalContentAlignment = VerticalAlignment.Bottom;

                    Canvas.SetLeft(FactionText, mr.RegionX - 40);
                    Canvas.SetTop(FactionText, mr.RegionY - 15);
                    Canvas.SetZIndex(FactionText, 23);
                    MainUniverseCanvas.Children.Add(FactionText);


                }


                // now add all the region links
                foreach (string s in mr.RegionLinks)
                {
                    EVEData.MapRegion or = EVEManager.GetRegion(s);
                    Line regionLink = new Line();

                    regionLink.X1 = mr.RegionX;
                    regionLink.Y1 = mr.RegionY;

                    regionLink.X2 = or.RegionX;
                    regionLink.Y2 = or.RegionY;

                    regionLink.Stroke = SysOutlineBrush;
                    regionLink.StrokeThickness = 1.2;
                    regionLink.Visibility = Visibility.Visible;

                    Canvas.SetZIndex(regionLink, 21);
                    MainUniverseCanvas.Children.Add(regionLink);
                }

                bool AddTheraConnection = false;
                foreach(EVEData.TheraConnection tc in EVEManager.TheraConnections)
                {
                    if(string.Compare(tc.Region, mr.Name, true) == 0)
                    {
                        AddTheraConnection = true;
                        break;
                    }
                }

                if(AddTheraConnection)
                {
                    Rectangle TheraShape = new Rectangle() { Width = 8, Height = 8 };

                    TheraShape.Stroke = SysOutlineBrush;
                    TheraShape.StrokeThickness = 1;
                    TheraShape.StrokeLineJoin = PenLineJoin.Round;
                    TheraShape.RadiusX = 2;
                    TheraShape.RadiusY = 2;
                    TheraShape.Fill = TheraBrush;
                    TheraShape.IsHitTestVisible = false;

                    Canvas.SetLeft(TheraShape, mr.RegionX + 28);
                    Canvas.SetTop(TheraShape, mr.RegionY + 3);
                    Canvas.SetZIndex(TheraShape, 22);
                    MainUniverseCanvas.Children.Add(TheraShape);


                }

            }
        }

        private void RegionShape_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Shape obj = sender as Shape;
            EVEData.MapRegion mr = obj.DataContext as EVEData.MapRegion;
            if(mr == null)
            {
                return;
            }

            if(e.ClickCount == 2)
            {
                RegionRC.SelectRegion(mr.Name);
                RegionLayoutDoc.IsSelected = true;
            }

        }

        private void ZKBFeed_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if (zkbs != null)
            {
                string KillURL = "https://zkillboard.com/kill/" + zkbs.KillID + "/";
                System.Diagnostics.Process.Start(KillURL);
            }
        }

        private void ZKBContexMenu_ShowSystem_Click(object sender, RoutedEventArgs e)
        {
            if (ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if (zkbs != null)
            {
                string systemName = EVEManager.GetSystemNameFromSystemID(zkbs.SystemID);
                if (systemName != "")
                {
                    RegionRC.SelectSystem(systemName, true);
                }
            }
        }

        private void ZKBContexMenu_ShowZKB_Click(object sender, RoutedEventArgs e)
        {
            if (ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if(zkbs != null)
            {
                string KillURL = "https://zkillboard.com/kill/" + zkbs.KillID + "/";
                System.Diagnostics.Process.Start(KillURL);
            }
        }


    }

}