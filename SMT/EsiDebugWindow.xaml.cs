using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using EVEData;

namespace SMT
{
    /// <summary>
    /// Debug panel showing ESI rate-limit token buckets (per group): Remain and Reset at.
    /// </summary>
    public partial class EsiDebugWindow : Window
    {
        private readonly ObservableCollection<EsiRateLimitBucketState> _buckets = new ObservableCollection<EsiRateLimitBucketState>();
        private DispatcherTimer _refreshTimer;

        public EsiDebugWindow()
        {
            InitializeComponent();
            BucketsDataGrid.ItemsSource = _buckets;
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            RefreshBuckets();
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _refreshTimer.Tick += (s, args) => RefreshBuckets();
            _refreshTimer.Start();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _refreshTimer?.Stop();
        }

        private void RefreshBuckets()
        {
            var snapshot = EveManager.Instance.GetEsiRateLimitBuckets();
            _buckets.Clear();
            foreach(var b in snapshot)
                _buckets.Add(b);
            EmptyMessage.Visibility = _buckets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RefreshLabel.Text = _buckets.Count == 0 ? "Refreshing every 3 s." : $"Last refresh: {DateTime.Now:HH:mm:ss} (every 3 s)";
        }
    }
}