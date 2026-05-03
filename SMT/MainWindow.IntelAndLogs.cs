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
        private void RegionUC_SystemHoverEvent(string system)
        {
            RawIntelBox.SelectedItem = null;

            // iterate over all of the RawIntelBox ui items
            for(int i = 0; i < RawIntelBox.Items.Count; i++)
            {
                IntelData id = RawIntelBox.Items[i] as IntelData;
                if(id != null)
                {
                    if(system != string.Empty && id.Systems.Contains(system))
                    {
                        // highlight the item
                        ListViewItem lvi = RawIntelBox.ItemContainerGenerator.ContainerFromItem(id) as ListViewItem;
                        if(lvi != null)
                        {
                            lvi.Background = Brushes.DarkRed;
                        }
                    }
                    else
                    {
                        // remove the highlight
                        ListViewItem lvi = RawIntelBox.ItemContainerGenerator.ContainerFromItem(id) as ListViewItem;
                        if(lvi != null)
                        {
                            lvi.Background = Brushes.Transparent;
                        }
                    }
                }
            }
        }
        private void OnGamelogUpdated(List<EVEData.GameLogData> gll)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                List<GameLogData> removeList = new List<GameLogData>();
                List<GameLogData> addList = new List<GameLogData>();

                // remove old

                if(GameLogCache.Count > 50)
                {
                    foreach(GameLogData gl in GameLogCache)
                    {
                        if(!gll.Contains(gl))
                        {
                            removeList.Add(gl);
                        }
                    }

                    foreach(GameLogData gl in removeList)
                    {
                        GameLogCache.Remove(gl);
                    }
                }

                // add new
                foreach(GameLogData gl in gll)
                {
                    if(!GameLogCache.Contains(gl))
                    {
                        GameLogCache.Insert(0, gl);
                    }
                }
            }), DispatcherPriority.Normal);
        }
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
    }
}
