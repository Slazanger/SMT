using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

namespace SMT
{
    public partial class ZKBMonitor : Window
    {
        private MainWindow mainWindow;
        private bool filterByRegion = true;
        private int maxKills = 50;

        public ZKBMonitor(MainWindow mw)
        {
            InitializeComponent();

            mainWindow = mw;
            if (mainWindow == null) return;

            ZKBKillList.ItemsSource = mainWindow.EVEManager.ZKillFeed.KillStream;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ZKBKillList.ItemsSource);
            view.Filter = item => ZKillFilter(item);

            filterByRegion = mw.MapConf.ZKBFloatFilterByRegion;
            maxKills = mw.MapConf.ZKBFloatMaxKills;

            this.Background.Opacity = mw.MapConf.ZKBFloatBackgroundOpacity;
            ZKBKillList.Opacity = mw.MapConf.ZKBFloatContentOpacity;

            mainWindow.EVEManager.ZKillFeed.KillsAddedEvent += OnKillsAdded;
            mw.MapConf.PropertyChanged += MapConf_PropertyChanged;

            Closing += ZKBMonitor_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            LoadWindowPosition();
        }

        private void LoadWindowPosition()
        {
            string placement = Properties.Settings.Default.ZKBMonitorWindow_placement;
            if (!string.IsNullOrEmpty(placement))
            {
                WindowPlacement.SetPlacement(new WindowInteropHelper(this).Handle, placement);
            }
        }

        private void StoreWindowPosition()
        {
            Properties.Settings.Default.ZKBMonitorWindow_placement =
                WindowPlacement.GetPlacement(new WindowInteropHelper(this).Handle);
            Properties.Settings.Default.Save();
        }

        private void ZKBMonitor_Closing(object sender, CancelEventArgs e)
        {
            StoreWindowPosition();
            mainWindow.EVEManager.ZKillFeed.KillsAddedEvent -= OnKillsAdded;
            mainWindow.MapConf.PropertyChanged -= MapConf_PropertyChanged;
            mainWindow.MapConf.ZKBFloatWindowOpen = false;
        }

        private void ZKBMonitor_Window_Move(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.ResizeMode = ResizeMode.NoResize;
                this.DragMove();
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            e.Handled = true;
        }

        private void ZKBMonitor_Window_Close(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void ContextMenu_ShowSystem_Click(object sender, RoutedEventArgs e)
        {
            dynamic zs = ZKBKillList.SelectedItem;
            if (zs != null)
            {
                mainWindow.RegionUC.SelectSystem(zs.SystemName, true);
            }
        }

        private void ContextMenu_OpenZKB_Click(object sender, RoutedEventArgs e)
        {
            dynamic zs = ZKBKillList.SelectedItem;
            if (zs != null)
            {
                string url = "https://zkillboard.com/kill/" + zs.KillID + "/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private void ZKBKillList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic zs = ZKBKillList.SelectedItem;
            if (zs != null)
            {
                string url = "https://zkillboard.com/kill/" + zs.KillID + "/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private void OnKillsAdded()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CollectionViewSource.GetDefaultView(ZKBKillList.ItemsSource)?.Refresh();
            });
        }

        private bool ZKillFilter(object item)
        {
            if (!filterByRegion) return true;
            dynamic zs = item;
            if (zs != null)
            {
                return mainWindow.RegionUC?.Region?.IsSystemOnMap(zs.SystemName) ?? false;
            }
            return false;
        }

        private void MapConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZKBFloatBackgroundOpacity")
                this.Background.Opacity = mainWindow.MapConf.ZKBFloatBackgroundOpacity;
            if (e.PropertyName == "ZKBFloatContentOpacity")
                ZKBKillList.Opacity = mainWindow.MapConf.ZKBFloatContentOpacity;
            if (e.PropertyName == "ZKBFloatFilterByRegion")
            {
                filterByRegion = mainWindow.MapConf.ZKBFloatFilterByRegion;
                CollectionViewSource.GetDefaultView(ZKBKillList.ItemsSource)?.Refresh();
            }
            if (e.PropertyName == "ZKBFloatMaxKills")
                maxKills = mainWindow.MapConf.ZKBFloatMaxKills;
        }
    }
}
