using AvalonDock.Layout;
using Microsoft.Toolkit.Uwp.Notifications;
using NAudio.Wave;
using NHotkey;
using NHotkey.Wpf;
using SMT.EVEData;
using SMT.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using static SMT.EVEData.ZKillRedisQ;

namespace SMT
{
    public partial class MainWindow
    {
        private void OnZKillsAdded()
        {
            manualZKillFilterRefreshRequired = true;
        }
        private void ZKBContexMenu_ShowSystem_Click(object sender, RoutedEventArgs e)
        {
            if(ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if(zkbs != null)
            {
                RegionUC.SelectSystem(zkbs.SystemName, true);

                if(RegionLayoutDoc != null)
                {
                    RegionLayoutDoc.IsSelected = true;
                }
            }
        }

        private void ZKBContexMenu_ShowZKB_Click(object sender, RoutedEventArgs e)
        {
            if(ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if(zkbs != null)
            {
                string KillURL = "https://zkillboard.com/kill/" + zkbs.KillID + "/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(KillURL) { UseShellExecute = true });
            }
        }

        private void ZKBFeed_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if(zkbs != null)
            {
                string KillURL = "https://zkillboard.com/kill/" + zkbs.KillID + "/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(KillURL) { UseShellExecute = true });
            }
        }

        private void ZKBFeed_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            if(ZKBFeed.SelectedIndex == -1)
            {
                return;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zkbs = ZKBFeed.SelectedItem as EVEData.ZKillRedisQ.ZKBDataSimple;

            if(zkbs != null)
            {
                string KillURL = "https://zkillboard.com/kill/" + zkbs.KillID + "/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(KillURL) { UseShellExecute = true });
            }
        }

        private bool ZKBFeedFilter(object item)
        {
            // Define the new filter logic to show only the last 50 items Predicate<object> newFilter = item => allItems.IndexOf((YourItemType)item) >= Math.Max(0, allItems.Count - 50);

            if(zkbFilterByRegion == false)
            {
                return true;
            }

            EVEData.ZKillRedisQ.ZKBDataSimple zs = item as EVEData.ZKillRedisQ.ZKBDataSimple;
            if(zs == null)
            {
                return false;
            }

            if(RegionUC.Region.IsSystemOnMap(zs.SystemName))
            {
                return true;
            }

            return false;
        }

        // The feed shows the newest ZKBFeedMaxKills kills that pass ZKBFeedFilter. The view calls its filter
        // once per item, so the visible set is worked out once and reused until the kill stream changes or
        // InvalidateZKBVisibleKills is called. Working it out for every item would make one refresh
        // quadratic in the number of kills.
        private const int ZKBFeedMaxKills = 30;
        private HashSet<ZKBDataSimple> zkbVisibleKills = new HashSet<ZKBDataSimple>();
        private int zkbVisibleKillsCount = -1;
        private ZKBDataSimple zkbVisibleKillsFirst;
        private ZKBDataSimple zkbVisibleKillsLast;

        private bool ZKBFeedViewFilter(object item)
        {
            ObservableCollection<ZKBDataSimple> killStream = EVEManager.ZKillFeed.KillStream;
            int count = killStream.Count;
            ZKBDataSimple first = count > 0 ? killStream[0] : null;
            ZKBDataSimple last = count > 0 ? killStream[count - 1] : null;

            // kills are inserted at the front and expire from the back, so any change to the stream
            // changes its count, its first kill or its last kill
            if(count != zkbVisibleKillsCount || first != zkbVisibleKillsFirst || last != zkbVisibleKillsLast)
            {
                zkbVisibleKills = new HashSet<ZKBDataSimple>(killStream.Where(kill => ZKBFeedFilter(kill)).Take(ZKBFeedMaxKills));
                zkbVisibleKillsCount = count;
                zkbVisibleKillsFirst = first;
                zkbVisibleKillsLast = last;
            }

            return item is ZKBDataSimple zkbItem && zkbVisibleKills.Contains(zkbItem);
        }

        /// <summary>
        /// Forces the next filter call to rebuild the visible kills, e.g. after the region or the region filter changed.
        /// </summary>
        private void InvalidateZKBVisibleKills()
        {
            zkbVisibleKillsCount = -1;
        }

        private void ZKBFeedFilterViewChk_Checked(object sender, RoutedEventArgs e)
        {
            zkbFilterByRegion = (bool)ZKBFeedFilterViewChk.IsChecked;
            InvalidateZKBVisibleKills();

            if(ZKBFeed != null)
            {
                manualZKillFilterRefreshRequired = true;
            }
        }
    }
}
