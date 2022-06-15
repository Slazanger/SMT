using System;
using System.Windows;

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
            characterInfoGrid.Visibility = Visibility.Visible;
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

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter lc = characterInfoGrid.DataContext as EVEData.LocalCharacter;
            if (lc != null)
            {
                MessageBoxResult result = MessageBox.Show("Would you like to Delete \"" + lc.Name + " ?", "Delete Character?", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    MainWindow mw = Owner as MainWindow;

                    mw.ActiveCharacter = null;
                    mw.FleetMembersList.ItemsSource = null;

                    mw.CurrentActiveCharacterCombo.SelectedIndex = -1;
                    mw.RegionsViewUC.ActiveCharacter = null;
                    mw.RegionUC.ActiveCharacter = null;
                    mw.RegionUC.UpdateActiveCharacter();
                    mw.UniverseUC.ActiveCharacter = null;
                    mw.OnCharacterSelectionChanged();

                    mw.EVEManager.LocalCharacters.Remove(lc);

                    characterLV.Items.Refresh();

                    characterInfoGrid.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}