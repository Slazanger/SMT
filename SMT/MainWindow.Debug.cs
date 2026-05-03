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
        private void EsiDebug_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var esiDebugWindow = new EsiDebugWindow();
            esiDebugWindow.Owner = this;
            esiDebugWindow.Show();
        }
    }
}
