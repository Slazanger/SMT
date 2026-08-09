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
using System.Threading.Tasks;
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
using EVEDataUtils;

namespace SMT
{
    public partial class MainWindow
    {
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowPlacement.SetPlacement(new WindowInteropHelper(this).Handle, Properties.Settings.Default.MainWindow_placement);
        }
        private void SaveDefaultLayout()
        {
            string defaultLayoutFile = AppDomain.CurrentDomain.BaseDirectory + @"\DefaultWindowLayout.dat";

            try
            {
                if(OperatingSystem.IsWindows())
                {
                    AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                    AtomicFile.WriteText(defaultLayoutFile, writer => ls.Serialize(writer));
                }
            }
            catch(Exception exception)
            {
                AppLog.Error("Save default layout", exception);
            }
        }

        private void LoadWindowLayoutWithBackup(string layoutFile)
        {
            if(!OperatingSystem.IsWindows())
            {
                return;
            }

            string backupFile = AtomicFile.GetBackupPath(layoutFile);
            string candidate = File.Exists(layoutFile) ? layoutFile : File.Exists(backupFile) ? backupFile : null;
            if(candidate == null)
            {
                return;
            }

            try
            {
                DeserializeWindowLayout(candidate);
                if(candidate == backupFile)
                {
                    AppLog.Warning("Load window layout", "The backup layout was loaded because the primary file was missing.");
                }
            }
            catch(Exception primaryException)
            {
                if(candidate == layoutFile && File.Exists(backupFile))
                {
                    try
                    {
                        DeserializeWindowLayout(backupFile);
                        AppLog.Warning("Load window layout", $"Recovered the window layout from backup after: {primaryException.Message}");
                        return;
                    }
                    catch(Exception backupException)
                    {
                        AppLog.Error("Load window layout", new AggregateException(primaryException, backupException));
                        return;
                    }
                }

                AppLog.Error("Load window layout", primaryException);
            }
        }

        private void DeserializeWindowLayout(string layoutFile)
        {
            AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
            using StreamReader reader = new StreamReader(layoutFile);
            serializer.Deserialize(reader);
        }

        private void Exit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
        private AvalonDock.Layout.LayoutDocument FindDocWithContentID(AvalonDock.Layout.ILayoutElement root, string contentID)
        {
            AvalonDock.Layout.LayoutDocument content = null;

            if(root is AvalonDock.Layout.ILayoutContainer)
            {
                AvalonDock.Layout.ILayoutContainer ic = root as AvalonDock.Layout.ILayoutContainer;
                foreach(AvalonDock.Layout.ILayoutElement ie in ic.Children)
                {
                    AvalonDock.Layout.LayoutDocument f = FindDocWithContentID(ie, contentID);
                    if(f != null)
                    {
                        content = f;
                        break;
                    }
                }
            }
            else
            {
                if(root is AvalonDock.Layout.LayoutDocument)
                {
                    AvalonDock.Layout.LayoutDocument i = root as AvalonDock.Layout.LayoutDocument;
                    if(i.ContentId == contentID)
                    {
                        content = i;
                    }
                }
            }
            return content;
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if(MapConf.CloseToTray && MapConf.MinimizeToTray && AppWindow.ShowInTaskbar != false)
            {
                e.Cancel = true;
                AppWindow.Hide();
                AppWindow.ShowInTaskbar = false;
                nIcon.Visible = true;
                return;
            }

            // Store the main window position and size

            Properties.Settings.Default.MainWindow_placement = WindowPlacement.GetPlacement(new WindowInteropHelper(AppWindow).Handle);
            Properties.Settings.Default.Save();

            EVEManager.ZKillFeed.KillsAddedEvent -= OnZKillsAdded;
            AppLog.StatusChanged -= AppLog_StatusChanged;
            EVEManager.BeginShutdown();
        }

        private void AppLog_StatusChanged(string message, bool isError)
        {
            if(Dispatcher.HasShutdownStarted)
            {
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                EVEManager.ServerInfo.StatusMessage = message;
                EVEManager.ServerInfo.StatusIsError = isError;
            });
        }
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // save off the dockmanager layout
            string dockManagerLayoutName = Path.Combine(EveAppConfig.StorageRoot, "Layout_" + WindowLayoutVersion + ".dat");

            try
            {
                AvalonDock.Layout.Serialization.XmlLayoutSerializer ls = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                AtomicFile.WriteText(dockManagerLayoutName, writer => ls.Serialize(writer));
            }
            catch(Exception exception)
            {
                AppLog.Error("Save window layout", exception);
            }

            try
            {
                // Save off any explicit items
                MapConf.UseESIForCharacterPositions = EVEManager.UseESIForCharacterPositions;

                // Save the Map Colours
                string mapConfigFileName = Path.Combine(EveAppConfig.StorageRoot, "MapConfig_" + MapConfig.SaveVersion + ".dat");

                // save off the toolbar setup
                MapConf.ToolBox_ShowJumpBridges = RegionUC.ShowJumpBridges;
                MapConf.ToolBox_ShowNPCKills = RegionUC.ShowNPCKills;
                MapConf.ToolBox_ShowPodKills = RegionUC.ShowPodKills;
                MapConf.ToolBox_ShowShipJumps = RegionUC.ShowShipJumps;
                MapConf.ToolBox_ShowShipKills = RegionUC.ShowShipKills;
                MapConf.ToolBox_ShowSovOwner = RegionUC.ShowSovOwner;
                MapConf.ToolBox_ShowStandings = RegionUC.ShowStandings;
                MapConf.ToolBox_ShowSystemADM = RegionUC.ShowSystemADM;
                MapConf.ToolBox_ShowSystemSecurity = RegionUC.ShowSystemSecurity;
                MapConf.ToolBox_ShowSystemTimers = RegionUC.ShowSystemTimers;
                MapConf.ToolBox_ESIOverlayScale = RegionUC.ESIOverlayScale;

                // now serialise the class to disk
                Serialization.SerializeToDisk(MapConf, mapConfigFileName);

                // Save any custom map Layout
                string customLayoutFile = Path.Combine(EveAppConfig.VersionStorage, "CustomUniverseLayout.txt");

                AtomicFile.WriteText(customLayoutFile, tw =>
                {
                    foreach(EVEData.System s in EVEManager.Systems)
                    {
                        if(s.CustomUniverseLayout)
                        {
                            tw.WriteLine($"{s.Region},{s.Name},{s.UniverseX.ToString(CultureInfo.InvariantCulture)},{s.UniverseY.ToString(CultureInfo.InvariantCulture)}");
                        }
                    }
                });
            }
            catch(Exception exception)
            {
                AppLog.Error("Save map configuration", exception);
            }

            try
            {
                // save the Anom Data
                // now serialise the class to disk
                string anomDataFilename = EVEManager.SaveDataVersionFolder + @"\Anoms.dat";
                Serialization.SerializeToDisk(ANOMManager, anomDataFilename);
            }
            catch(Exception exception)
            {
                AppLog.Error("Save anomaly data", exception);
            }

            // save the character data
            try
            {
                EVEManager.SaveData();
            }
            catch(Exception exception)
            {
                AppLog.Error("Save application data", exception);
            }
            EVEManager.ShutDown();

            waveOutEvent?.Stop();
            waveOutEvent?.Dispose();
            audioFileReader?.Dispose();
            nIcon.Visible = false;
            nIcon.Dispose();
        }
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if(this.WindowState == WindowState.Minimized)
            {
                if(MapConf.MinimizeToTray)
                {
                    AppWindow.Hide();
                    AppWindow.ShowInTaskbar = false;
                    nIcon.Visible = true;
                }
            }
        }
        private void NIcon_DClick(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.Visibility = Visibility.Visible;
            this.Show();
            this.WindowState = WindowState.Normal;
            nIcon.Visible = false;
            this.Activate();
        }

        private void NIcon_Exit(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
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
        private async Task CheckGitHubVersionAsync()
        {
            string url = @"https://api.github.com/repos/slazanger/smt/releases/latest";
            string strContent = string.Empty;

            try
            {
                using HttpClient hc = new HttpClient();
                hc.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SMT", EveAppConfig.SMT_VERSION));
                using HttpResponseMessage response = await hc.GetAsync(url);
                response.EnsureSuccessStatusCode();
                strContent = await response.Content.ReadAsStringAsync();
            }
            catch(Exception exception)
            {
                AppLog.Error("Check for SMT updates", exception);
                return;
            }

            try
            {
                GitHubRelease.Release releaseInfo = GitHubRelease.Release.FromJson(strContent);

                if(releaseInfo != null && releaseInfo.TagName != EveAppConfig.SMT_VERSION)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        NewVersionWindow nw = new NewVersionWindow();
                        nw.ReleaseInfo = releaseInfo.Body;
                        nw.CurrentVersion = EveAppConfig.SMT_VERSION;
                        nw.NewVersion = releaseInfo.TagName;
                        nw.ReleaseURL = releaseInfo.HtmlUrl?.ToString() ?? string.Empty;
                        nw.Owner = this;
                        nw.ShowDialog();
                    }, DispatcherPriority.Normal);
                }
            }
            catch(Exception exception)
            {
                AppLog.Error("Process SMT update information", exception);
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
                catch(Exception exception)
                {
                    AppLog.Error("Reset window layout", exception);
                }
            }

            // Due to bugs in the Dock manager patch up the content id's for the 2 main views
            RegionLayoutDoc = FindDocWithContentID(dockManager.Layout, "MapRegionContentID");
            UniverseLayoutDoc = FindDocWithContentID(dockManager.Layout, "FullUniverseViewID");

            dockManager.UpdateLayout();
        }
    }
}
