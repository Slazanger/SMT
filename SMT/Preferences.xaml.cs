using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
//using System.Windows.Forms;
using System.Windows.Media;
using NAudio.Utils;
using NAudio.Wave;
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

        private WaveOutEvent waveOutEvent;
        private AudioFileReader audioFileReader;

        private bool isInitialLoad = true; // Flag to track initial load of preferences window

        public PreferencesWindow()
        {
            InitializeComponent();

            syncESIPositionChk.IsChecked = EveManager.Instance.UseESIForCharacterPositions;


            waveOutEvent = new WaveOutEvent();
            audioFileReader = new AudioFileReader(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
            waveOutEvent.Init(audioFileReader);


            JumpBridgeList.ItemsSource = EveManager.Instance.JumpBridges;
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
            ColoursPropertyGrid.PropertyValueChanged += ColoursPropertyGrid_PropertyValueChanged;

            intelVolumeSlider.ValueChanged += IntelVolumeChanged_ValueChanged;
        }

        private void ColoursPropertyGrid_PropertyValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            MainWindow.AppWindow.RegionUC.ReDrawMap(true);
            MainWindow.AppWindow.UniverseUC.ReDrawMap(true, true, true);
        }

        private void IntelVolumeChanged_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isInitialLoad)
            {
                isInitialLoad = false;
                return; // Skip sound playback on initial load
            }

            waveOutEvent.Volume = MapConf.IntelSoundVolume;

            if (waveOutEvent.PlaybackState != PlaybackState.Playing )
            {
                audioFileReader.Position = 0; // Reset position to the beginning
                waveOutEvent.Play(); // Play the sound  
            }

        }

        private void SetLogLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
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

        private void ClearJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            EveManager.Instance.JumpBridges.Clear();
            EVEData.Navigation.ClearJumpBridges();

            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();
        }

        private void DeleteJumpGateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (JumpBridgeList.SelectedIndex == -1)
            {
                return;
            }

            EVEData.JumpBridge jb = JumpBridgeList.SelectedItem as EVEData.JumpBridge;

            EveManager.Instance.JumpBridges.Remove(jb);

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EveManager.Instance.JumpBridges);

            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();
            UpdateJumpBridgeSummary();
        }

        private void EnableDisableJumpGateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (JumpBridgeList.SelectedIndex == -1)
            {
                return;
            }

            EVEData.JumpBridge jb = JumpBridgeList.SelectedItem as EVEData.JumpBridge;

            jb.Disabled = !jb.Disabled;

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EveManager.Instance.JumpBridges);

            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();
            UpdateJumpBridgeSummary();
        }

        private void ExportJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            string ExportText = "";

            foreach (EVEData.MapRegion mr in EveManager.Instance.Regions)
            {
                ExportText += "# " + mr.Name + "\n";

                foreach (EVEData.JumpBridge jb in EveManager.Instance.JumpBridges)
                {
                    EVEData.System es = EveManager.Instance.GetEveSystem(jb.From);
                    if (es.Region == mr.Name)
                    {
                        ExportText += $"{jb.FromID} {jb.From} --> {jb.To}\n";
                    }

                    es = EveManager.Instance.GetEveSystem(jb.To);
                    if (es.Region == mr.Name)
                    {
                        ExportText += $"{jb.ToID} {jb.To} --> {jb.From}\n";
                    }
                }

                ExportText += "\n";
            }

            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, ExportText);
                }
                catch { }
            }
        }

        private async void FindJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            ImportJumpGatesBtn.IsEnabled = false;
            ClearJumpGatesBtn.IsEnabled = false;
            JumpBridgeList.IsEnabled = false;
            ImportPasteJumpGatesBtn.IsEnabled = false;
            ExportJumpGatesBtn.IsEnabled = false;

            foreach (EVEData.LocalCharacter c in EveManager.Instance.LocalCharacters)
            {
                if (c.ESILinked)
                {
                    // This should never be set due to https://developers.eveonline.com/blog/article/the-esi-api-is-a-shared-resource-do-not-abuse-it
                    if (c.DeepSearchEnabled && GateSearchFilter.Text == " » ")
                    {
                        string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                        string basesearch = " » ";

                        foreach (char cc in chars)
                        {
                            string search = basesearch + cc;
                            List<EVEData.JumpBridge> jbl = await c.FindJumpGates(search);

                            foreach (EVEData.JumpBridge jb in jbl)
                            {
                                bool found = false;

                                foreach (EVEData.JumpBridge jbr in EveManager.Instance.JumpBridges)
                                {
                                    if ((jb.From == jbr.From && jb.To == jbr.To) || (jb.From == jbr.To && jb.To == jbr.From))
                                    {
                                        found = true;
                                    }
                                }

                                if (!found)
                                {
                                    EveManager.Instance.JumpBridges.Add(jb);
                                }
                            }

                            Thread.Sleep(100);
                        }

                        foreach (char cc in chars)
                        {
                            string search = cc + basesearch;
                            List<EVEData.JumpBridge> jbl = await c.FindJumpGates(search);

                            foreach (EVEData.JumpBridge jb in jbl)
                            {
                                bool found = false;

                                foreach (EVEData.JumpBridge jbr in EveManager.Instance.JumpBridges)
                                {
                                    if ((jb.From == jbr.From && jb.To == jbr.To) || (jb.From == jbr.To && jb.To == jbr.From))
                                    {
                                        found = true;
                                    }
                                }

                                if (!found)
                                {
                                    EveManager.Instance.JumpBridges.Add(jb);
                                }
                            }

                            Thread.Sleep(100);
                        }
                    }
                    else
                    {
                        List<EVEData.JumpBridge> jbl = await c.FindJumpGates(GateSearchFilter.Text);

                        foreach (EVEData.JumpBridge jb in jbl)
                        {
                            bool found = false;

                            foreach (EVEData.JumpBridge jbr in EveManager.Instance.JumpBridges)
                            {
                                if ((jb.From == jbr.From && jb.To == jbr.To) || (jb.From == jbr.To && jb.To == jbr.From))
                                {
                                    found = true;
                                }
                            }

                            if (!found)
                            {
                                EveManager.Instance.JumpBridges.Add(jb);
                            }
                        }
                    }
                }
            }

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EveManager.Instance.JumpBridges);
            UpdateJumpBridgeSummary();

            ImportJumpGatesBtn.IsEnabled = true;
            ClearJumpGatesBtn.IsEnabled = true;
            JumpBridgeList.IsEnabled = true;
            ImportPasteJumpGatesBtn.IsEnabled = true;
            ExportJumpGatesBtn.IsEnabled = true;
            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();
        }

        private void ImportPasteJumpGatesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Text))
            {
                return;
            }
            String jbText = Clipboard.GetText(TextDataFormat.UnicodeText);

            Regex rx = new Regex(
                @"<url=showinfo:35841//([0-9]+)>(.*?) » (.*?) - .*?</url>|^[\t ]*([0-9]+) (.*) --> (.*)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
            MatchCollection matches = rx.Matches(jbText);

            foreach (Match match in matches)
            {
                // from eve chat
                // 1 = id
                // 2 = from
                // 3 = to

                // from export
                // 4 = id
                // 5 = from
                // 6 = to
                GroupCollection groups = match.Groups;
                long IDFrom = 0;
                if (groups[1].Value != "" && groups[2].Value != "" && groups[3].Value != "")
                {
                    long.TryParse(groups[1].Value, out IDFrom);
                    string from = groups[2].Value;
                    string to = groups[3].Value;
                    EveManager.Instance.AddUpdateJumpBridge(from, to, IDFrom);
                }
                else if (groups[4].Value != "" && groups[5].Value != "" && groups[6].Value != "")
                {
                    long.TryParse(groups[4].Value, out IDFrom);
                    string from = groups[5].Value.Trim();
                    string to = groups[6].Value.Trim();
                    EveManager.Instance.AddUpdateJumpBridge(from, to, IDFrom);
                }
            }

            EVEData.Navigation.ClearJumpBridges();
            EVEData.Navigation.UpdateJumpBridges(EveManager.Instance.JumpBridges);
            UpdateJumpBridgeSummary();
            CollectionViewSource.GetDefaultView(JumpBridgeList.ItemsSource).Refresh();
        }

        private void UpdateJumpBridgeSummary()
        {
            int JBCount = 0;
            int MissingInfo = 0;
            int Disabled = 0;

            foreach (EVEData.JumpBridge jb in EveManager.Instance.JumpBridges)
            {
                JBCount++;

                if (jb.FromID == 0 || jb.ToID == 0)
                {
                    MissingInfo++;
                }
                if (jb.Disabled)
                {
                    Disabled++;
                }
            }

            string Label = $"{JBCount} gates, {MissingInfo} Incomplete, {Disabled} Disabled ";

            AnsiblexSummaryLbl.Content = Label;
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

    public class NegateBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (boolValue)
                {
                    return "True";
                }
                else
                {
                    return "False";
                }
            }

            return System.Windows.Data.Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}