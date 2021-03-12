using SMT.EVEData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SMT
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        public SMT.MapConfig MapConf;
        public SMT.EVEData.EveManager EM { get; set; }

        public PreferencesWindow()
        {
            InitializeComponent();

            syncESIPositionChk.IsChecked = EveManager.Instance.UseESIForCharacterPositions;
        }

        private void Prefs_OK_Click(object sender, RoutedEventArgs e)
        {
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

        }

        private void ColoursPropertyGrid_PropertyValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
        {
            MainWindow.AppWindow.RegionUC.ReDrawMap(true);
            MainWindow.AppWindow.UniverseUC.ReDrawMap(true, true, true);
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