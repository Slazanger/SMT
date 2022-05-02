using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using SMT.EVEData;
using MessageBox = System.Windows.MessageBox;

namespace SMT
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        public SMT.MapConfig MapConf;
        public SMT.EVEData.EveManager EM { get; set; }

        public List<string> CynoBeaconSystems { get; set; }

        private MediaPlayer mediaPlayer;

        public PreferencesWindow()
        {
            InitializeComponent();

            syncESIPositionChk.IsChecked = EveManager.Instance.UseESIForCharacterPositions;

            mediaPlayer = new MediaPlayer();
            Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
            mediaPlayer.Open(woopUri);
        }

        public void Init()
        {
            CynoBeaconSystems = new List<string>();
            foreach (EVEData.System s in EM.Systems)
            {
                if (s.HasJumpBeacon)
                {
                    CynoBeaconSystems.Add(s.Name);
                }
            }
        }

        private void Prefs_OK_Click(object sender, RoutedEventArgs e)
        {
            foreach (EVEData.System s in EM.Systems)
            {
                s.HasJumpBeacon = false;
            }

            foreach (string sys in CynoBeaconSystems)
            {
                EVEData.System es = EM.GetEveSystem(sys);
                if (es != null)
                {
                    es.HasJumpBeacon = true;
                }
            }

            Close();
        }

        private void Prefs_Default_Click(object sender, RoutedEventArgs e)
        {
            if (MapConf != null)
            {
                MapConf.SetDefaults();
            }
        }

        private void syncESIPositionChk_Checked(object sender, RoutedEventArgs e)
        {
            EveManager.Instance.UseESIForCharacterPositions = (bool)syncESIPositionChk.IsChecked;
        }

        private void zkilltime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            EveManager.Instance.ZKillFeed.KillExpireTimeMinutes = MapConf.ZkillExpireTimeMinutes;
        }

        private void ResetColourData_Click(object sender, RoutedEventArgs e)
        {
            MapConf.SetDefaultColours();
            ColoursPropertyGrid.SelectedObject = MapConf.ActiveColourScheme;
            MainWindow.AppWindow.RegionUC.ReDrawMap(true);
            MainWindow.AppWindow.UniverseUC.ReDrawMap(true, true, true);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ColoursPropertyGrid.SelectedObject = MapConf.ActiveColourScheme;
            ColoursPropertyGrid.CollapseAllProperties();
            ColoursPropertyGrid.Update();
            ColoursPropertyGrid.PropertyValueChanged += ColoursPropertyGrid_PropertyValueChanged; ;

            intelVolumeSlider.ValueChanged += IntelVolumeChanged_ValueChanged;
        }

        private void ColoursPropertyGrid_PropertyValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            MainWindow.AppWindow.RegionUC.ReDrawMap(true);
            MainWindow.AppWindow.UniverseUC.ReDrawMap(true, true, true);
        }

        private void IntelVolumeChanged_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Stop();
            mediaPlayer.Volume = MapConf.IntelSoundVolume;
            mediaPlayer.Position = new TimeSpan(0, 0, 0);
            mediaPlayer.Play();
        }

        private void SetLogLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MapConf.CustomEveLogFolderLocation = dialog.SelectedPath;
            }
            MessageBoxResult result = MessageBox.Show("Restart SMT for the log folder location to take effect", "Please Restart SMT", MessageBoxButton.OK);
        }

        private void DefaultLogLocation_Click(object sender, RoutedEventArgs e)
        {
            MapConf.CustomEveLogFolderLocation = string.Empty;
            MessageBoxResult result = MessageBox.Show("Restart SMT for the log folder location to take effect", "Please Restart SMT", MessageBoxButton.OK);
        }
    }

    public class JoinStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var lines = value as IEnumerable<string>;
            return lines is null ? null : string.Join(Environment.NewLine, lines);
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            string inputstr = (string)value;
            string[] lines = inputstr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> oc = new List<string>(lines);
            return oc;
        }
    }
}