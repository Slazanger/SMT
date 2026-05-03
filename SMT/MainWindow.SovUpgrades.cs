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
    }
}
