//-----------------------------------------------------------------------
// MainWindow — dock panels: regions / region map / universe / preferences / version check
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

        #region RegionsView Control

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

        #endregion RegionsView Control

        #region Region Control

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

        #endregion Region Control

        #region Universe Control

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

        #endregion Universe Control

        #region Preferences & Options

        private void ForceESIUpdate_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            EVEManager.UpdateESIUniverseData();
        }

        private void ClearOldEVELogs_Click(object sender, RoutedEventArgs e)
        {
            string EVEGameLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Gamelogs";
            {
                DirectoryInfo di = new DirectoryInfo(EVEGameLogFolder);
                FileInfo[] files = di.GetFiles("*.txt");
                foreach(FileInfo file in files)
                {
                    // keep only recent files
                    if(file.CreationTime < DateTime.Now.AddDays(-1))
                    {
                        try
                        {
                            File.Delete(file.FullName);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            string EVEChatLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Chatlogs";
            {
                DirectoryInfo di = new DirectoryInfo(EVEChatLogFolder);
                FileInfo[] files = di.GetFiles("*.txt");
                foreach(FileInfo file in files)
                {
                    // keep only recent files
                    if(file.CreationTime < DateTime.Now.AddDays(-1))
                    {
                        try
                        {
                            File.Delete(file.FullName);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private void ManageInfrastructureUpgrades_Click(object sender, RoutedEventArgs e)
        {
            InfrastructureUpgradeWindow upgradeWindow = new InfrastructureUpgradeWindow();
            upgradeWindow.EM = EVEManager;
            upgradeWindow.Owner = this;
            upgradeWindow.ShowDialog();

            // Refresh the current region view after closing the window
            if(RegionUC != null)
            {
                RegionUC.ReDrawMap(false);
            }
        }

        private void LoadInfrastructureUpgrades_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            dlg.Title = "Load Infrastructure Upgrades";

            // Default to the auto-load file location
            string defaultPath = System.IO.Path.Combine(EVEData.EveAppConfig.StorageRoot, "InfrastructureUpgrades.txt");
            if(System.IO.File.Exists(defaultPath))
            {
                dlg.FileName = defaultPath;
            }
            else
            {
                dlg.InitialDirectory = EVEData.EveAppConfig.StorageRoot;
            }

            bool? result = dlg.ShowDialog();

            if(result == true)
            {
                string filename = dlg.FileName;
                EVEManager.LoadInfrastructureUpgrades(filename);

                // Refresh the current region view to show the loaded upgrades
                if(RegionUC != null)
                {
                    RegionUC.ReDrawMap(false);
                }
            }
        }

        private void SaveInfrastructureUpgrades_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            dlg.Title = "Save Infrastructure Upgrades";

            // Default to the auto-load file location
            string defaultPath = System.IO.Path.Combine(EVEData.EveAppConfig.StorageRoot, "InfrastructureUpgrades.txt");
            dlg.FileName = defaultPath;

            bool? result = dlg.ShowDialog();

            if(result == true)
            {
                string filename = dlg.FileName;
                EVEManager.SaveInfrastructureUpgrades(filename);
            }
        }

        private void FullScreenToggle_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(miFullScreenToggle.IsChecked)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
            }
        }

        private void Preferences_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(preferencesWindow != null)
            {
                preferencesWindow.Close();
            }

            preferencesWindow = new PreferencesWindow();
            preferencesWindow.Closed += PreferencesWindow_Closed;
            preferencesWindow.Owner = this;
            preferencesWindow.DataContext = MapConf;
            preferencesWindow.MapConf = MapConf;
            preferencesWindow.EM = EVEManager;
            preferencesWindow.Init();
            preferencesWindow.ShowDialog();
        }

        private void PreferencesWindow_Closed(object sender, EventArgs e)
        {
            RegionUC.ReDrawMap(true);
            UniverseUC.ReDrawMap(true, true, false);

            // recalculate the route if required
            if(ActiveCharacter != null && ActiveCharacter.Waypoints.Count > 0)
            {
                ActiveCharacter.RecalcRoute();
            }
        }

        #endregion Preferences & Options

        #region NewVersion

        private async void CheckGitHubVersion()
        {
            string url = @"https://api.github.com/repos/slazanger/smt/releases/latest";
            string strContent = string.Empty;

            try
            {
                HttpClient hc = new HttpClient();
                hc.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SMT", EveAppConfig.SMT_VERSION));
                var response = await hc.GetAsync(url);
                response.EnsureSuccessStatusCode();
                strContent = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return;
            }

            GitHubRelease.Release releaseInfo = GitHubRelease.Release.FromJson(strContent);

            if(releaseInfo != null)
            {
                if(releaseInfo.TagName != EveAppConfig.SMT_VERSION)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        NewVersionWindow nw = new NewVersionWindow();
                        nw.ReleaseInfo = releaseInfo.Body;
                        nw.CurrentVersion = EveAppConfig.SMT_VERSION;
                        nw.NewVersion = releaseInfo.TagName;
                        nw.ReleaseURL = releaseInfo.HtmlUrl.ToString();
                        nw.Owner = this;
                        nw.ShowDialog();
                    }), DispatcherPriority.Normal);
                }
            }
        }

        #endregion NewVersion
    }
}

