using SMT.EVEData;
using System.Windows;

namespace SMT
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        public SMT.MapConfig MapConf;

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
    }
}