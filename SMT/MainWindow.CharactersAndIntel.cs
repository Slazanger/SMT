//-----------------------------------------------------------------------
// MainWindow — characters and intel
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
        #region Characters

        // Property now automatically fires an event when the active character changes.
        private EVEData.LocalCharacter activeCharacter;

        public EVEData.LocalCharacter ActiveCharacter
        { get => activeCharacter; set { activeCharacter = value; OnSelectedCharChangedEventHandler?.Invoke(this, EventArgs.Empty); } }

        /// <summary>
        ///  Add Character Button Clicked
        /// </summary>
        private void btn_AddCharacter_Click(object sender, RoutedEventArgs e)
        {
            AddCharacter();
        }

        public void AddCharacter()
        {
            if(logonBrowserWindow != null)
            {
                logonBrowserWindow.Close();
            }

            logonBrowserWindow = new LogonWindow();
            logonBrowserWindow.Owner = this;
            logonBrowserWindow.ShowDialog();
        }

        private void CharactersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if(grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    EVEData.LocalCharacter lc = dgr.Item as EVEData.LocalCharacter;

                    if(lc != null)
                    {
                        ActiveCharacter = lc;
                        CurrentActiveCharacterCombo.SelectedItem = lc;

                        lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
                        lc.FleetUpdatedEvent += OnFleetMemebersUpdate;

                        lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;
                        lc.RouteUpdatedEvent += OnCharacterRouteUpdate;

                        FleetMembersList.ItemsSource = lc.FleetInfo.Members;
                        CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();

                        RegionUC.FollowCharacter = true;
                        RegionUC.SelectSystem(lc.Location, true);

                        UniverseUC.FollowCharacter = true;
                        UniverseUC.UpdateActiveCharacter(lc);
                    }
                }
            }
        }

        private void OnCharacterRouteUpdate()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(dgActiveCharacterRoute.ItemsSource).Refresh();
                CollectionViewSource.GetDefaultView(lbActiveCharacterWaypoints.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        private void CharactersListMenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if(CharactersList.SelectedIndex == -1)
            {
                return;
            }

            EVEData.LocalCharacter lc = CharactersList.SelectedItem as EVEData.LocalCharacter;

            lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
            lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;

            ActiveCharacter = null;
            FleetMembersList.ItemsSource = null;

            CurrentActiveCharacterCombo.SelectedIndex = -1;
            RegionsViewUC.ActiveCharacter = null;
            RegionUC.ActiveCharacter = null;
            RegionUC.UpdateActiveCharacter();
            UniverseUC.ActiveCharacter = null;
            OnCharacterSelectionChanged();

            EVEManager.RemoveCharacter(lc);
        }

        private void CurrentActiveCharacterCombo_Selected(object sender, SelectionChangedEventArgs e)
        {
            if(CurrentActiveCharacterCombo.SelectedIndex == -1)
            {
                RegionsViewUC.ActiveCharacter = null;
                RegionUC.ActiveCharacter = null;
                FleetMembersList.ItemsSource = null;
                RegionUC.UpdateActiveCharacter();
                UniverseUC.UpdateActiveCharacter(null);
            }
            else
            {
                EVEData.LocalCharacter lc = CurrentActiveCharacterCombo.SelectedItem as EVEData.LocalCharacter;
                ActiveCharacter = lc;

                FleetMembersList.ItemsSource = lc.FleetInfo.Members;
                CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
                lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
                lc.FleetUpdatedEvent += OnFleetMemebersUpdate;

                lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;
                lc.RouteUpdatedEvent += OnCharacterRouteUpdate;

                RegionsViewUC.ActiveCharacter = lc;
                RegionUC.UpdateActiveCharacter(lc);
                UniverseUC.UpdateActiveCharacter(lc);
            }

            OnCharacterSelectionChanged();
        }

        private void UpdateCharacterSelectionBasedOnActiveWindow()
        {
            string ActiveWindowText = Utils.Misc.GetCaptionOfActiveWindow();

            if(ActiveWindowText.Contains("EVE - "))
            {
                string characterName = ActiveWindowText.Substring(6);
                foreach(EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                {
                    if(lc.Name == characterName)
                    {
                        ActiveCharacter = lc;
                        CurrentActiveCharacterCombo.SelectedItem = lc;
                        FleetMembersList.ItemsSource = lc.FleetInfo.Members;
                        CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
                        lc.FleetUpdatedEvent -= OnFleetMemebersUpdate;
                        lc.FleetUpdatedEvent += OnFleetMemebersUpdate;

                        lc.RouteUpdatedEvent -= OnCharacterRouteUpdate;
                        lc.RouteUpdatedEvent += OnCharacterRouteUpdate;

                        RegionUC.UpdateActiveCharacter(lc);
                        UniverseUC.UpdateActiveCharacter(lc);

                        break;
                    }
                }
            }
        }

        public void OnFleetMemebersUpdate(LocalCharacter c)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                CollectionViewSource.GetDefaultView(FleetMembersList.ItemsSource).Refresh();
            }), DispatcherPriority.Normal);
        }

        #endregion Characters

        #region intel

        private ObservableCollection<EVEData.IntelData> IntelCache;

        private ObservableCollection<EVEData.GameLogData> GameLogCache;

        private void ClearIntelBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.IntelDataList.ClearAll();
            IntelCache.Clear();
        }

        private void ClearGameLogBtn_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.GameLogList.ClearAll();
            GameLogCache.Clear();
        }

        private void OnIntelUpdated(List<IntelData> idl)
        {
            bool playSound = false;
            bool flashWindow = false;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                List<IntelData> removeList = new List<IntelData>();
                List<IntelData> addList = new List<IntelData>();

                // remove old

                if(IntelCache.Count >= 250)
                {
                    foreach(IntelData id in IntelCache)
                    {
                        if(!idl.Contains(id))
                        {
                            removeList.Add(id);
                        }
                    }

                    foreach(IntelData id in removeList)
                    {
                        IntelCache.Remove(id);
                    }
                }

                // add new
                foreach(IntelData id in idl)
                {
                    if(!IntelCache.Contains(id))
                    {
                        IntelCache.Insert(0, id);
                    }
                }
            }), DispatcherPriority.Normal);

            IntelData id = IntelCache[0];

            if(id.ClearNotification)
            {
                // do nothing for now
                return;
            }

            if(MapConf.PlayIntelSoundOnUnknown && id.Systems.Count == 0)
            {
                playSound = true;
                flashWindow = true;
            }


            if(MapConf.PlayIntelSound || MapConf.FlashWindow || MapConf.PlayIntelSoundOnAlert)
            {
                if(MapConf.PlaySoundOnlyInDangerZone || MapConf.FlashWindowOnlyInDangerZone)
                {
                    foreach(string s in id.Systems)
                    {
                        foreach(EVEData.LocalCharacter lc in EVEManager.LocalCharacters)
                        {
                            if(lc.WarningSystems != null && lc.DangerZoneActive)
                            {
                                foreach(string ls in lc.WarningSystems)
                                {
                                    if(ls == s)
                                    {
                                        playSound = playSound || MapConf.PlaySoundOnlyInDangerZone;
                                        flashWindow = flashWindow || MapConf.FlashWindowOnlyInDangerZone;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if(MapConf.PlayIntelSoundOnAlert)
                {
                    // Check if the intel contains a text we should explicitly alert on
                    foreach(string alertName in EVEManager.IntelAlertFilters)
                    {
                        if(id.RawIntelString.Contains(alertName))
                        {
                            playSound = playSound || MapConf.PlayIntelSoundOnAlert;
                            break;
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if(playSound || (!MapConf.PlaySoundOnlyInDangerZone && MapConf.PlayIntelSound))
                {
                    try
                    {
                        waveOutEvent.Stop();
                        waveOutEvent.Volume = MapConf.IntelSoundVolume;
                        audioFileReader.Position = 0;
                        waveOutEvent.Play();
                    }
                    catch(Exception ex)
                    {
                        // if the sound fails to play
                        // seen this after wake up from sleep
                        Debug.WriteLine("Failed to play intel sound: " + ex.Message);
                    }
                }
                if(flashWindow || (!MapConf.FlashWindowOnlyInDangerZone && MapConf.FlashWindow))
                {
                    FlashWindow.Flash(AppWindow, 5);
                }
            }), DispatcherPriority.Normal);
        }

        private void OnShipDecloaked(string character, string text)
        {
            foreach(LocalCharacter lc in EVEManager.LocalCharacters)
            {
                if(lc.Name == character)
                {
                    bool triggerAlert = lc.DecloakWarningEnabled;

                    if(text.Contains("Mobile Observatory"))
                    {
                        triggerAlert = lc.ObservatoryDecloakWarningEnabled;
                    }

                    if(text.Contains("Stargate"))
                    {
                        triggerAlert = lc.GateDecloakWarningEnabled;
                    }

                    if(triggerAlert)
                    {
                        if(OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                try
                                {
                                    // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                                    ToastContentBuilder tb = new ToastContentBuilder();
                                    tb.AddText("SMT Alert");
                                    tb.AddText("Character : " + character + "(" + lc.Location + ")");

                                    // add the character portrait if we have one
                                    if(lc.PortraitLocation != null)
                                    {
                                        tb.AddInlineImage(lc.PortraitLocation);
                                    }

                                    tb.AddText(text);
                                    tb.AddArgument("character", character);
                                    tb.SetToastScenario(ToastScenario.Alarm);
                                    tb.SetToastDuration(ToastDuration.Long);
                                    Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                                    tb.AddAudio(woopUri);
                                    tb.Show();
                                }
                                catch
                                {
                                    // sometimes caused by this :
                                    // https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/4858
                                }
                            }), DispatcherPriority.Normal, null);
                        }
                    }

                    break;
                }
            }
        }

        private void OnCombatEvent(string character, string text)
        {
            foreach(LocalCharacter lc in EVEManager.LocalCharacters)
            {
                if(lc.Name == character)
                {
                    if(lc.CombatWarningEnabled)
                    {
                        if(OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                try
                                {
                                    // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                                    ToastContentBuilder tb = new ToastContentBuilder();
                                    tb.AddText("SMT Alert");
                                    tb.AddText("Character : " + character + "(" + lc.Location + ")");

                                    // add the character portrait if we have one
                                    if(lc.PortraitLocation != null)
                                    {
                                        tb.AddInlineImage(lc.PortraitLocation);
                                    }

                                    tb.AddText(text);
                                    tb.AddArgument("character", character);
                                    tb.SetToastScenario(ToastScenario.Alarm);
                                    tb.SetToastDuration(ToastDuration.Long);
                                    Uri woopUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Sounds\woop.mp3");
                                    tb.AddAudio(woopUri);
                                    tb.Show();
                                }
                                catch
                                {
                                    // sometimes caused by this :
                                    // https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/4858
                                }
                            }), DispatcherPriority.Normal, null);
                        }
                    }

                    break;
                }
            }
        }

        private void OnZKillsAdded()
        {
            manualZKillFilterRefreshRequired = true;
        }

        private void RawIntelBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(RawIntelBox.SelectedItem == null)
            {
                return;
            }

            EVEData.IntelData chat = RawIntelBox.SelectedItem as EVEData.IntelData;

            bool selectedSystem = false;

            foreach(string s in chat.IntelString.Split(' '))
            {
                if(s == "")
                {
                    continue;
                }
                var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                foreach(Match m in linkParser.Matches(s))
                {
                    string url = m.Value;
                    if(!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    {
                        url = "http://" + url;
                    }
                    if(Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
                // only select the first system
                if(!selectedSystem)
                {
                    foreach(EVEData.System sys in EVEManager.Systems)
                    {
                        if(s.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            if(RegionUC.Region.Name != sys.Region)
                            {
                                RegionUC.SelectRegion(sys.Region);
                            }

                            RegionUC.SelectSystem(s, true);
                            selectedSystem = true;
                        }
                    }
                }
            }
        }

        #endregion intel
    }
}

