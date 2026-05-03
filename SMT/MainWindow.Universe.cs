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
        private void Storms_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(MetaliminalStormList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void TheraConnections_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(TheraConnectionsList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void TurnurConnections_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(TurnurConnectionsList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }
        private void ActiveSovCampaigns_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(SovCampaignList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void LocalCharacters_CollectionChanged()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(CharactersList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }
        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            uiRefreshCounter++;
            if(uiRefreshCounter == 5)
            {
                uiRefreshCounter = 0;

                if(FleetMembersList.ItemsSource != null)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
                    }), DispatcherPriority.Normal);
                }
            }
            if(MapConf.SyncActiveCharacterBasedOnActiveEVEClient)
            {
                UpdateCharacterSelectionBasedOnActiveWindow();
            }

            if(manualZKillFilterRefreshRequired)
            {
                try
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        CollectionViewSource.GetDefaultView(ZKBFeed.ItemsSource).Refresh();
                    }), DispatcherPriority.Normal);
                }
                catch
                {
                    // ignore the error
                }

                manualZKillFilterRefreshRequired = false;
            }

            // refresh the anomalies datagrid every 60 seconds to update the "since" column
            anomRefreshCounter++;
            if(anomRefreshCounter == 60)
            {
                anomRefreshCounter = 0;
                AnomSigList.Items.Refresh();
            }
        }
        private void RegionsViewUC_RequestRegion(object sender, RoutedEventArgs e)
        {
            string regionName = e.OriginalSource as string;
            RegionUC.FollowCharacter = false;
            RegionUC.SelectRegion(regionName);

            if(RegionLayoutDoc != null)
            {
                RegionLayoutDoc.IsSelected = true;
            }
        }
        private void MapConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "AlwaysOnTop")
            {
                if(MapConf.AlwaysOnTop)
                {
                    this.Topmost = true;
                }
                else
                {
                    this.Topmost = false;
                }
            }

            if(e.PropertyName == "ShowZKillData")
            {
                EVEManager.ZKillFeed.PauseUpdate = !MapConf.ShowZKillData;
            }

            RegionUC.ReDrawMap(true);

            if(e.PropertyName == "ShowRegionStandings")
            {
                RegionsViewUC.Redraw(true);
            }

            if(e.PropertyName == "ShowUniverseRats")
            {
                RegionsViewUC.Redraw(true);
            }

            if(e.PropertyName == "ShowUniversePods")
            {
                RegionsViewUC.Redraw(true);
            }

            if(e.PropertyName == "ShowUniverseKills")
            {
                RegionsViewUC.Redraw(true);
            }

            if(e.PropertyName == "UniverseDataScale")
            {
                RegionsViewUC.Redraw(true);
            }
        }

        public void OnCharacterSelectionChanged()
        {
            manualZKillFilterRefreshRequired = true;
        }

        private void RegionUC_RegionChanged(object sender, PropertyChangedEventArgs e)
        {
            if(RegionLayoutDoc != null)
            {
                RegionLayoutDoc.Title = RegionUC.Region.LocalizedName;
            }

            manualZKillFilterRefreshRequired = true;

            if(SovCampaignList != null && SovCampaignList.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(SovCampaignList.ItemsSource).Refresh();
            }
        }

        private void RegionUC_UniverseSystemSelect(object sender, RoutedEventArgs e)
        {
            string sysName = e.OriginalSource as string;
            UniverseUC.ShowSystem(sysName);

            if(UniverseLayoutDoc != null)
            {
                UniverseLayoutDoc.IsSelected = true;
            }
        }
        private void UniverseUC_RequestRegionSystem(object sender, RoutedEventArgs e)
        {
            string sysName = e.OriginalSource as string;
            RegionUC.FollowCharacter = false;
            RegionUC.SelectSystem(sysName, true);

            if(RegionLayoutDoc != null)
            {
                RegionLayoutDoc.IsSelected = true;
            }
        }
    }
}
