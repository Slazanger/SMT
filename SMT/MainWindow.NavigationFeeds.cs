//-----------------------------------------------------------------------
// MainWindow — Thera/Turnur, route, ZKill feed
//-----------------------------------------------------------------------
using AvalonDock.Layout;
using Microsoft.Toolkit.Uwp.Notifications;
using NAudio.Wave;
using NHotkey;
using NHotkey.Wpf;
using SMT.EVEData;
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
    public partial class MainWindow : Window
    {
        #region Thera / Turnur

        /// <summary>
        /// Update Thera Button Clicked
        /// </summary>
        private void btn_UpdateThera_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.UpdateTheraConnections();
        }

        private void TheraConnectionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if(grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    EVEData.TheraConnection tc = dgr.Item as EVEData.TheraConnection;

                    if(tc != null)
                    {
                        RegionUC.SelectSystem(tc.System, true);
                    }
                }
            }
        }

        private void btn_UpdateTurnur_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.UpdateTurnurConnections();
        }

        private void TurnurConnectionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if(grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    EVEData.TurnurConnection tc = dgr.Item as EVEData.TurnurConnection;

                    if(tc != null)
                    {
                        RegionUC.SelectSystem(tc.System, true);
                    }
                }
            }
        }

        #endregion Thera / Turnur

        #region Route

        private void refreshJumpRouteUI()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if(capitalRouteWaypointsLB.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(capitalRouteWaypointsLB.ItemsSource).Refresh();
                }

                if(capitalRouteAvoidLB.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(capitalRouteAvoidLB.ItemsSource).Refresh();
                }

                if(dgCapitalRouteCurrentRoute.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(dgCapitalRouteCurrentRoute.ItemsSource).Refresh();
                }

                // lbAlternateMids could be null, need to check..

                if(lbAlternateMids.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(lbAlternateMids.ItemsSource).Refresh();
                }
            }), DispatcherPriority.Normal, null);
        }

        private void AddWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            if(RegionUC.ActiveCharacter == null)
            {
                return;
            }

            if(RouteSystemDropDownAC.SelectedItem == null)
            {
                return;
            }
            EVEData.System s = RouteSystemDropDownAC.SelectedItem as EVEData.System;

            if(s != null)
            {
                RegionUC.ActiveCharacter.AddDestination(s.ID, false);
            }
        }

        private void AddJumpWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            if(JumpRouteSystemDropDownAC.SelectedItem == null)
            {
                return;
            }
            EVEData.System s = JumpRouteSystemDropDownAC.SelectedItem as EVEData.System;

            if(s != null)
            {
                CapitalRoute.WayPoints.Add(s.Name);
                CapitalRoute.Recalculate();

                if(CapitalRoute.CurrentRoute.Count == 0)
                {
                    lblCapitalRouteSummary.Content = "No Valid Route Found";
                }
                else
                {
                    lblCapitalRouteSummary.Content = $"{CapitalRoute.CurrentRoute.Count - 2} Mids";
                }

                refreshJumpRouteUI();
            }
        }

        private void AddJumpAvoidSystemsBtn_Click(object sender, RoutedEventArgs e)
        {
            if(JumpRouteAvoidSystemDropDownAC.SelectedItem == null)
            {
                return;
            }
            EVEData.System s = JumpRouteAvoidSystemDropDownAC.SelectedItem as EVEData.System;

            if(s != null)
            {
                CapitalRoute.AvoidSystems.Add(s.Name);
                CapitalRoute.Recalculate();

                if(CapitalRoute.CurrentRoute.Count == 0)
                {
                    lblCapitalRouteSummary.Content = "No Valid Route Found";
                }
                else
                {
                    lblCapitalRouteSummary.Content = $"{CapitalRoute.CurrentRoute.Count - 2} Mids";
                }

                refreshJumpRouteUI();
            }
        }

        private void ClearWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if(c != null)
            {
                c.ClearAllWaypoints();
            }
        }

        private void ClearJumpWaypointsBtn_Click(object sender, RoutedEventArgs e)
        {
            CapitalRoute.WayPoints.Clear();
            CapitalRoute.CurrentRoute.Clear();
            lbAlternateMids.ItemsSource = null;
            lblAlternateMids.Content = "";

            refreshJumpRouteUI();
        }

        private void ClearJumpAvoidSystemsBtn_Click(object sender, RoutedEventArgs e)
        {
            CapitalRoute.AvoidSystems.Clear();
            refreshJumpRouteUI();
        }

        private void CopyRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if(c != null)
            {
                string WPT = c.GetWayPointText();

                try
                {
                    Clipboard.SetText(WPT);
                }
                catch { }
            }
        }

        private void ReCalculateRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEData.LocalCharacter c = RegionUC.ActiveCharacter as EVEData.LocalCharacter;
            if(c != null && c.Waypoints.Count > 0)
            {
                c.RecalcRoute();
            }
        }

        #endregion Route

        #region ZKillBoard

        private bool zkbFilterByRegion = true;

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

        private void ZKBFeedFilterViewChk_Checked(object sender, RoutedEventArgs e)
        {
            zkbFilterByRegion = (bool)ZKBFeedFilterViewChk.IsChecked;

            if(ZKBFeed != null)
            {
                manualZKillFilterRefreshRequired = true;
            }
        }


        #endregion ZKillBoard
    }
}

