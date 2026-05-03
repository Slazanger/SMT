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
using static SMT.EVEData.Navigation;

namespace SMT
{
    public partial class MainWindow
    {
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

        /// <summary>
        /// The anomalies grid is clicked
        /// </summary>
        private void AnomSigList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // set focus to enable the grid to receive the PreviewKeyDown events
            AnomSigList.Focus();
        }

        /// <summary>
        /// Keys pressed while the anomalies grid is in focus
        /// </summary>
        private void AnomSigList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // handle paste operation
            if(Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                updateAnomListFromClipboard();
                e.Handled = true;
            }
            // handle copy operation
            else if(Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                copyAnomListToClipboard();
                e.Handled = true;
            }
            // handle delete
            else if(Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Delete)
            {
                deleteSelectedAnoms();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Clear system Anoms button clicked
        /// </summary>
        private void btnClearAnomList_Click(object sender, RoutedEventArgs e)
        {
            EVEData.AnomData ad = ANOMManager.ActiveSystem;
            if(ad != null)
            {
                ad.Anoms.Clear();
                AnomSigList.Items.Refresh();
                AnomSigList.UpdateLayout();
                CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
            }
        }

        /// <summary>
        /// Delete selected Anoms button clicked
        /// </summary>
        private void btnDeleteSelectedAnoms_Click(object sender, RoutedEventArgs e)
        {
            deleteSelectedAnoms();
        }

        /// <summary>
        /// Update Anoms clicked
        /// </summary>
        private void btnUpdateAnomList_Click(object sender, RoutedEventArgs e)
        {
            updateAnomListFromClipboard();
        }

        private void updateAnomListFromClipboard()
        {
            EVEData.AnomData ad = ANOMManager.ActiveSystem;
            string pasteData = Clipboard.GetText();

            if(ad == null || string.IsNullOrEmpty(pasteData))
                return;

            HashSet<string> missingSignatures = ad.UpdateFromPaste(pasteData);

            // Clear current selection
            AnomSigList.SelectedItems.Clear();

            foreach(var item in AnomSigList.Items)
            {
                var anom = item as Anom;
                if(anom != null && missingSignatures.Contains(anom.Signature))
                    AnomSigList.SelectedItems.Add(item);
            }

            if(AnomSigList.SelectedItems.Count > 0)
                AnomSigList.ScrollIntoView(AnomSigList.SelectedItems[0]);

            AnomSigList.Items.Refresh();
            AnomSigList.UpdateLayout();
            CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
        }

        private void copyAnomListToClipboard()
        {
            EVEData.AnomData ad = ANOMManager.ActiveSystem;
            if(ad == null)
                return;

            var str = string.Empty;
            if(AnomSigList.SelectedItems.Count > 0)
            {
                // copy the selected items to clipboard
                foreach(var entry in AnomSigList.SelectedItems)
                {
                    var anom = entry as Anom;
                    str += anom.ToString() + "\n";
                }
            }
            else
            {
                // copy the entire list
                foreach(var entry in ad.Anoms)
                {
                    var anom = entry.Value;
                    str += anom.ToString() + "\n";
                }
            }

            Clipboard.SetText(str);
        }

        private void deleteSelectedAnoms()
        {
            EVEData.AnomData ad = ANOMManager.ActiveSystem;
            if(ad == null || AnomSigList.SelectedItems.Count == 0)
                return;

            var selectedSignatures = AnomSigList.SelectedItems.Cast<Anom>()
                .Select(anom => anom.Signature)
                .ToHashSet();

            var keysToRemove = ad.Anoms
                .Where(kvp => selectedSignatures.Contains(kvp.Value.Signature))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach(var key in keysToRemove)
                ad.Anoms.Remove(key);

            AnomSigList.Items.Refresh();
            AnomSigList.UpdateLayout();
            CollectionViewSource.GetDefaultView(AnomSigList.ItemsSource).Refresh();
        }
        private void SovCampaignFilterViewChk_Checked(object sender, RoutedEventArgs e)
        {
            sovCampaignFilterByRegion = (bool)SovCampaignFilterViewChk.IsChecked;

            if(SovCampaignList != null && SovCampaignList.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(SovCampaignList.ItemsSource).Refresh();
            }
        }

        private bool SovCampaignFilter(object item)
        {
            if(!sovCampaignFilterByRegion)
                return true;

            if(item is not EVEData.SOVCampaign sc)
                return false;

            string currentRegion = RegionUC?.Region?.Name;
            if(string.IsNullOrEmpty(currentRegion))
                return true; // no region selected => don't filter

            return string.Equals(sc.Region, currentRegion, StringComparison.OrdinalIgnoreCase);
        }
        private void SovCampaignList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if(grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;

                    EVEData.SOVCampaign sc = dgr.Item as EVEData.SOVCampaign;

                    if(sc != null)
                    {
                        RegionUC.SelectSystem(sc.System, true);
                    }

                    if(RegionLayoutDoc != null)
                    {
                        RegionLayoutDoc.IsSelected = true;
                    }
                }
            }
        }
        private void JumpPlannerShipType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CapitalRoute != null)
            {
                ComboBox cb = sender as ComboBox;
                ComboBoxItem cbi = cb.SelectedItem as ComboBoxItem;
                CapitalRoute.MaxLY = double.Parse(cbi.DataContext as string);
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

        private void JumpPlannerJDC_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CapitalRoute != null)
            {
                ComboBox cb = sender as ComboBox;
                ComboBoxItem cbi = cb.SelectedItem as ComboBoxItem;
                CapitalRoute.JDC = int.Parse(cbi.DataContext as string);
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

        private void CurrentCapitalRouteItem_Selected(object sender, RoutedEventArgs e)
        {
            if(dgCapitalRouteCurrentRoute.SelectedItem != null)
            {
                Navigation.RoutePoint rp = dgCapitalRouteCurrentRoute.SelectedItem as Navigation.RoutePoint;
                string sel = rp.SystemName;

                UniverseUC.ShowSystem(sel);

                lblAlternateMids.Content = sel;
                if(CapitalRoute.AlternateMids.ContainsKey(sel))
                {
                    lbAlternateMids.ItemsSource = CapitalRoute.AlternateMids[sel];
                }
                else
                {
                    lbAlternateMids.ItemsSource = null;
                }
            }
        }

        private void CapitalWaypointsContextMenuMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if(capitalRouteWaypointsLB.SelectedItem != null && capitalRouteWaypointsLB.SelectedIndex != 0)
            {
                string sys = CapitalRoute.WayPoints[capitalRouteWaypointsLB.SelectedIndex];
                CapitalRoute.WayPoints.RemoveAt(capitalRouteWaypointsLB.SelectedIndex);
                CapitalRoute.WayPoints.Insert(capitalRouteWaypointsLB.SelectedIndex - 1, sys);

                CapitalRoute.Recalculate();
                refreshJumpRouteUI();
            }
        }

        private void CapitalWaypointsContextMenuMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if(capitalRouteWaypointsLB.SelectedItem != null && capitalRouteWaypointsLB.SelectedIndex != CapitalRoute.WayPoints.Count - 1)
            {
                string sys = CapitalRoute.WayPoints[capitalRouteWaypointsLB.SelectedIndex];
                CapitalRoute.WayPoints.RemoveAt(capitalRouteWaypointsLB.SelectedIndex);
                CapitalRoute.WayPoints.Insert(capitalRouteWaypointsLB.SelectedIndex + 1, sys);

                CapitalRoute.Recalculate();
                refreshJumpRouteUI();
            }
        }

        private void CapitalWaypointsContextMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if(capitalRouteWaypointsLB.SelectedItem != null)
            {
                CapitalRoute.WayPoints.RemoveAt(capitalRouteWaypointsLB.SelectedIndex);
                CapitalRoute.Recalculate();
                refreshJumpRouteUI();
            }
        }

        private void CapitalRouteContextMenuUseAlt_Click(object sender, RoutedEventArgs e)
        {
            if(lbAlternateMids.SelectedItem != null)
            {
                string selectedAlt = lbAlternateMids.SelectedItem as string;

                // need to find where to insert the new waypoint
                int waypointIndex = -1;
                foreach(Navigation.RoutePoint rp in CapitalRoute.CurrentRoute)
                {
                    if(rp.SystemName == CapitalRoute.WayPoints[waypointIndex + 1])
                    {
                        waypointIndex++;
                    }
                    if(CapitalRoute.AlternateMids.ContainsKey(rp.SystemName))
                    {
                        foreach(string alt in CapitalRoute.AlternateMids[rp.SystemName])
                        {
                            if(alt == selectedAlt)
                            {
                                CapitalRoute.WayPoints.Insert(waypointIndex + 1, selectedAlt);
                                break;
                            }
                        }
                    }
                }

                CapitalRoute.Recalculate();
                refreshJumpRouteUI();
            }
        }
        private void btnUnseenFits_Click(object sender, RoutedEventArgs e)
        {
            string KillURL = "https://zkillboard.com/character/93280351/losses/";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(KillURL) { UseShellExecute = true });
        }
        private void MetaliminalStormList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(MetaliminalStormList.SelectedIndex != -1)
            {
                EVEData.Storm selectedStorm = MetaliminalStormList.SelectedItem as EVEData.Storm;
                if(selectedStorm != null)
                {
                    // Select the system in the map
                    RegionUC.SelectSystem(selectedStorm.System, true);
                    // If the region doc is not selected, select it
                    if(RegionLayoutDoc != null && !RegionLayoutDoc.IsSelected)
                    {
                        RegionLayoutDoc.IsSelected = true;
                    }
                }
            }
        }
    }
}
