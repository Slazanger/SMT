//-----------------------------------------------------------------------
// MainWindow — anomalies, SOV filter, overlays, converters host tail
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
        #region Anoms

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

        #endregion Anoms


        private bool sovCampaignFilterByRegion = false;

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



        private void Characters_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CharactersWindow charactersWindow = new CharactersWindow();
            charactersWindow.characterLV.ItemsSource = EVEManager.LocalCharacters;

            charactersWindow.Owner = this;

            charactersWindow.ShowDialog();
        }

        private void LoadInfoObjects()
        {
            InfoLayer = new List<InfoItem>();

            // now add the beacons
            string infoObjectsFile = Path.Combine(EveAppConfig.StorageRoot, "InfoObjects.txt");
            if(File.Exists(infoObjectsFile))
            {
                StreamReader file = new StreamReader(infoObjectsFile);

                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(line.StartsWith("#"))
                    {
                        continue;
                    }

                    string[] parts = line.Split(',');

                    if(parts.Length == 0)
                    {
                        continue;
                    }

                    string region = parts[0];

                    EVEData.MapRegion mr = EVEManager.GetRegion(region);
                    if(mr == null)
                    {
                        continue;
                    }

                    if(parts[1] == "SYSLINK" || parts[1] == "SYSLINKARC")
                    {
                        if(parts.Length != 7)
                        {
                            continue;
                        }
                        // REGION SYSLINK FROM TO SOLID/DASHED size #FFFFFF
                        string from = parts[2];
                        string to = parts[3];
                        string lineStyle = parts[4];
                        string size = parts[5];
                        string colour = parts[6];

                        if(!mr.MapSystems.ContainsKey(from))
                        {
                            continue;
                        }

                        if(!mr.MapSystems.ContainsKey(to))
                        {
                            continue;
                        }

                        EVEData.MapSystem fromMS = mr.MapSystems[from];
                        EVEData.MapSystem toMS = mr.MapSystems[to];

                        InfoItem.LineType lt = InfoItem.LineType.Solid;
                        if(lineStyle == "DASHED")
                        {
                            lt = InfoItem.LineType.Dashed;
                        }

                        if(lineStyle == "LIGHTDASHED")
                        {
                            lt = InfoItem.LineType.LightDashed;
                        }

                        Color c = (Color)ColorConverter.ConvertFromString(colour);

                        int lineThickness = int.Parse(size);

                        InfoItem ii = new InfoItem();
                        ii.DrawType = InfoItem.ShapeType.Line;

                        if(parts[1] == "SYSLINKARC")
                        {
                            ii.DrawType = InfoItem.ShapeType.ArcLine;
                        }

                        ii.X1 = (int)fromMS.Layout.X;
                        ii.Y1 = (int)fromMS.Layout.Y;
                        ii.X2 = (int)toMS.Layout.X;
                        ii.Y2 = (int)toMS.Layout.Y;
                        ii.Size = lineThickness;
                        ii.Region = region;
                        ii.Fill = c;
                        ii.LineStyle = lt;
                        InfoLayer.Add(ii);
                    }

                    if(parts[1] == "SYSMARKER")
                    {
                        if(parts.Length != 5)
                        {
                            continue;
                        }
                        // REGION SYSMARKER FROM SIZE #FFFFFF
                        string from = parts[2];
                        string size = parts[3];
                        string colour = parts[4];

                        if(!mr.MapSystems.ContainsKey(from))
                        {
                            continue;
                        }

                        EVEData.MapSystem fromMS = mr.MapSystems[from];

                        Color c = (Color)ColorConverter.ConvertFromString(colour);

                        int radius = int.Parse(size);

                        InfoItem ii = new InfoItem();
                        ii.DrawType = InfoItem.ShapeType.Circle;
                        ii.X1 = (int)fromMS.Layout.X;
                        ii.Y1 = (int)fromMS.Layout.Y;
                        ii.Size = radius;
                        ii.Region = region;
                        ii.Fill = c;
                        InfoLayer.Add(ii);
                    }
                }
            }

            RegionUC.InfoLayer = InfoLayer;
        }

        private void TestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string characterName = ActiveCharacter.Name;

            LocalCharacter lc = ActiveCharacter;

            string line = "Your cloak deactivates due to a pulse from a Mobile Observatory deployed by Slazanger.";

            lc.GameLogWarningText = line;

            if(OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
            {
                ToastContentBuilder tb = new ToastContentBuilder();
                tb.AddText("SMT Alert");
                tb.AddText("Character : " + characterName + "(" + lc.Location + ")");

                // add the character portrait if we have one
                if(lc.PortraitLocation != null)
                {
                    tb.AddInlineImage(lc.PortraitLocation);
                }

                tb.AddText(line);
                tb.AddArgument("character", characterName);
                tb.SetToastScenario(ToastScenario.Alarm);
                tb.SetToastDuration(ToastDuration.Long);
                Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                tb.AddAudio(woopUri);
                tb.Show();
            }
        }

        private void miResetLayout_Click(object sender, RoutedEventArgs e)
        {
            string defaultLayoutFile = AppDomain.CurrentDomain.BaseDirectory + @"\DefaultWindowLayout.dat";
            if(File.Exists(defaultLayoutFile))
            {
                try
                {
                    AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                    using(var sr = new StreamReader(defaultLayoutFile))
                    {
                        ls.Deserialize(sr);
                    }
                }
                catch
                {
                }
            }

            // Due to bugs in the Dock manager patch up the content id's for the 2 main views
            RegionLayoutDoc = FindDocWithContentID(dockManager.Layout, "MapRegionContentID");
            UniverseLayoutDoc = FindDocWithContentID(dockManager.Layout, "FullUniverseViewID");

            dockManager.UpdateLayout();
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

        private void OverlayWindow_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(overlayWindows == null) overlayWindows = new List<Overlay>();

            if(MapConf.OverlayIndividualCharacterWindows)
            {
                if(activeCharacter == null || overlayWindows.Any(w => w.OverlayCharacter == activeCharacter))
                {
                    return;
                }
            }
            else
            {
                if(overlayWindows.Count > 0)
                {
                    return;
                }
            }

            // Set up hotkeys
            try
            {
                HotkeyManager.Current.AddOrReplace("Toggle click trough overlay windows.", Key.T, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift, OverlayWindows_ToggleClicktrough_HotkeyTrigger);
            }
            catch(NHotkey.HotkeyAlreadyRegisteredException exception)
            {
            }

            Overlay newOverlayWindow = new Overlay(this);
            newOverlayWindow.Closing += OnOverlayWindowClosing;
            newOverlayWindow.Show();
            overlayWindows.Add(newOverlayWindow);
        }

        private void OverlayClickTroughToggle_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OverlayWindow_ToggleClickTrough();
        }

        private void OverlayWindows_ToggleClicktrough_HotkeyTrigger(object sender, HotkeyEventArgs eventArgs)
        {
            OverlayWindow_ToggleClickTrough();
        }

        public void OverlayWindow_ToggleClickTrough()
        {
            overlayWindowsAreClickTrough = !overlayWindowsAreClickTrough;
            foreach(Overlay overlayWindow in overlayWindows)
            {
                overlayWindow.ToggleClickTrough(overlayWindowsAreClickTrough);
            }
        }

        public void OnOverlayWindowClosing(object sender, CancelEventArgs e)
        {
            overlayWindows.Remove((Overlay)sender);

            if(overlayWindows.Count < 1)
            {
                try
                {
                    HotkeyManager.Current.Remove("Toggle click trough overlay windows.");
                }
                catch
                {
                }
            }
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

