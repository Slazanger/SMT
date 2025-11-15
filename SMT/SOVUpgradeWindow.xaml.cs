using System.Windows;
using System.Windows.Controls;
using SMT.EVEData;

namespace SMT
{
    public partial class SOVUpgradeWindow : Window
    {
        private EVEData.System _system;

        public SOVUpgradeWindow(EVEData.System system)
        {
            InitializeComponent();
            _system = system;

            SystemNameLabel.Content = $"SOV Upgrades for {system.Name}";
            SystemInfoLabel.Text = $"Region: {system.Region}";

            InitializeAvailableUpgrades();
            UpdateInstalledUpgrades();
        }

        private void InitializeAvailableUpgrades()
        {
            // Create upgrade categories
            CreateUpgradeCategory("Strategic", new[]
            {
                (SOVUpgradeType.CynosuralNavigation, "Cynosural Navigation", "Enables Pharolux Cyno Beacon"),
                (SOVUpgradeType.CynosuralSuppression, "Cynosural Suppression", "Enables Tenebrex Cyno Jammer"),
                (SOVUpgradeType.AdvancedLogisticsNetwork, "Advanced Logistics Network", "Enables Ansiblex Jump Gate"),
                (SOVUpgradeType.SupercapitalConstructionFacilities, "Supercapital Construction", "Enables Supercapital Shipyards")
            });

            CreateUpgradeCategory("Industrial", new[]
            {
                (SOVUpgradeType.OreProspecting1, "Ore Prospecting I", "Increases ore resources"),
                (SOVUpgradeType.OreProspecting2, "Ore Prospecting II", "Increases ore resources"),
                (SOVUpgradeType.OreProspecting3, "Ore Prospecting III", "Increases ore resources"),
                (SOVUpgradeType.OreProspecting4, "Ore Prospecting IV", "Increases ore resources"),
                (SOVUpgradeType.OreProspecting5, "Ore Prospecting V", "Increases ore resources"),
                (SOVUpgradeType.MiniProfession1, "Mini-Profession I", "Increases mini-profession sites"),
                (SOVUpgradeType.MiniProfession2, "Mini-Profession II", "Increases mini-profession sites"),
                (SOVUpgradeType.MiniProfession3, "Mini-Profession III", "Increases mini-profession sites"),
                (SOVUpgradeType.MiniProfession4, "Mini-Profession IV", "Increases mini-profession sites"),
                (SOVUpgradeType.MiniProfession5, "Mini-Profession V", "Increases mini-profession sites")
            });

            CreateUpgradeCategory("Military", new[]
            {
                (SOVUpgradeType.CombatSites1, "Combat Sites I", "Increases combat sites"),
                (SOVUpgradeType.CombatSites2, "Combat Sites II", "Increases combat sites"),
                (SOVUpgradeType.CombatSites3, "Combat Sites III", "Increases combat sites"),
                (SOVUpgradeType.CombatSites4, "Combat Sites IV", "Increases combat sites"),
                (SOVUpgradeType.CombatSites5, "Combat Sites V", "Increases combat sites"),
                (SOVUpgradeType.Wormhole1, "Wormhole I", "Increases wormhole chance"),
                (SOVUpgradeType.Wormhole2, "Wormhole II", "Increases wormhole chance"),
                (SOVUpgradeType.Wormhole3, "Wormhole III", "Increases wormhole chance"),
                (SOVUpgradeType.Wormhole4, "Wormhole IV", "Increases wormhole chance"),
                (SOVUpgradeType.Wormhole5, "Wormhole V", "Increases wormhole chance"),
                (SOVUpgradeType.Entrapment1, "Entrapment I", "Increases complex chance"),
                (SOVUpgradeType.Entrapment2, "Entrapment II", "Increases complex chance"),
                (SOVUpgradeType.Entrapment3, "Entrapment III", "Increases complex chance"),
                (SOVUpgradeType.Entrapment4, "Entrapment IV", "Increases complex chance"),
                (SOVUpgradeType.Entrapment5, "Entrapment V", "Increases complex chance")
            });
        }

        private void CreateUpgradeCategory(string categoryName, (SOVUpgradeType type, string name, string description)[] upgrades)
        {
            // Add category header
            var categoryLabel = new Label
            {
                Content = categoryName,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 10, 0, 0)
            };
            AvailableUpgradesPanel.Children.Add(categoryLabel);

            // Add upgrades
            foreach (var (type, name, description) in upgrades)
            {
                var button = new Button
                {
                    Content = name,
                    Margin = new Thickness(5, 2, 5, 2),
                    Padding = new Thickness(5),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Tag = type,
                    ToolTip = description
                };
                button.Click += AddUpgradeButton_Click;
                AvailableUpgradesPanel.Children.Add(button);
            }
        }

        private void AddUpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SOVUpgradeType upgradeType)
            {
                // Check if upgrade already installed
                if (_system.SOVUpgrades.Exists(u => u.Type == upgradeType))
                {
                    MessageBox.Show($"This upgrade is already installed in {_system.Name}.", "Already Installed", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Add the upgrade
                var upgrade = new SOVUpgrade(upgradeType, button.Content.ToString(), button.ToolTip?.ToString() ?? "", 0);
                _system.SOVUpgrades.Add(upgrade);

                UpdateInstalledUpgrades();
            }
        }

        private void UpdateInstalledUpgrades()
        {
            InstalledUpgradesListBox.ItemsSource = null;
            InstalledUpgradesListBox.ItemsSource = _system.SOVUpgrades;
        }

        private void InstalledUpgradesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveSelectedButton.IsEnabled = InstalledUpgradesListBox.SelectedItem != null;
        }

        private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (InstalledUpgradesListBox.SelectedItem is SOVUpgrade upgrade)
            {
                _system.SOVUpgrades.Remove(upgrade);
                UpdateInstalledUpgrades();
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Remove all SOV upgrades from {_system.Name}?", "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _system.SOVUpgrades.Clear();
                UpdateInstalledUpgrades();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
