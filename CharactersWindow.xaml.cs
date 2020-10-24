using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SMT
{
    /// <summary>
    /// Interaction logic for Characters.xaml
    /// </summary>
    public partial class CharactersWindow : Window
    {
        public CharactersWindow()
        {
            InitializeComponent();
        }

        private void characterLV_Selected(object sender, RoutedEventArgs e)
        {
             characterInfoGrid.DataContext = characterLV.SelectedItem;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (EVEData.LocalCharacter lc in EVEData.EveManager.Instance.LocalCharacters)
            {
                lc.warningSystemsNeedsUpdate = true;
            }
        }

        private void AddCharacter_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = Owner as MainWindow;
            mw.AddCharacter();
        }

        private void dangerzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            EVEData.LocalCharacter lc = characterInfoGrid.DataContext as EVEData.LocalCharacter;
            if (lc != null)
            {
                lc.warningSystemsNeedsUpdate = true;
            }
        }

        private void dangerZoneEnabled_Checked(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter lc = characterInfoGrid.DataContext as EVEData.LocalCharacter;
            if (lc != null)
            {
                lc.warningSystemsNeedsUpdate = true;
            }
        }
    }
}
