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
    }
}
