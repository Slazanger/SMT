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
    }
}
