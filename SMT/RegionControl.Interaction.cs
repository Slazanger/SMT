//-----------------------------------------------------------------------
// Region control input, context menus, and timers (split from RegionControl.xaml.cs)
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SMT.EVEData;
using SMT.ResourceUsage;

namespace SMT
{
    public partial class RegionControl : UserControl, INotifyPropertyChanged
    {
        private void AllianceKeyList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label obj = sender as Label;
            string AllianceIDStr = obj.DataContext as string;
            long AllianceID = long.Parse(AllianceIDStr);

            if(e.ClickCount == 2)
            {
                string AURL = $"https://zkillboard.com/region/{Region.ID}/alliance/{AllianceID}/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AURL) { UseShellExecute = true });
            }
            else
            {
                if(SelectedAlliance == AllianceID)
                {
                    SelectedAlliance = 0;
                }
                else
                {
                    SelectedAlliance = AllianceID;
                }
                ReDrawMap(true);
            }
        }

        private void characterRightClickAutoRange_Clicked(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if(mi != null)
            {
                EveManager.JumpShip js = EveManager.JumpShip.Super;

                LocalCharacter lc = ((MenuItem)mi.Parent).DataContext as LocalCharacter;

                if(mi.DataContext as string == "6")
                {
                    js = EveManager.JumpShip.Super;
                }
                if(mi.DataContext as string == "7")
                {
                    js = EveManager.JumpShip.Carrier;
                }

                if(mi.DataContext as string == "8")
                {
                    js = EveManager.JumpShip.Blops;
                }

                if(mi.DataContext as string == "10")
                {
                    js = EveManager.JumpShip.JF;
                }

                if(mi.DataContext as string == "0")
                {
                    showJumpDistance = false;
                    currentJumpCharacter = "";
                    currentCharacterJumpSystem = "";
                }
                else
                {
                    showJumpDistance = true;
                    currentJumpCharacter = lc.Name;
                    currentCharacterJumpSystem = lc.Location;
                    jumpShipType = js;
                }
            }

            ReDrawMap(false);
        }

        private static Color DarkenColour(Color inCol)
        {
            Color Dark = inCol;
            Dark.R = (Byte)(0.8 * Dark.R);
            Dark.G = (Byte)(0.8 * Dark.G);
            Dark.B = (Byte)(0.8 * Dark.B);
            return Dark;
        }

        private void FollowCharacterChk_Checked(object sender, RoutedEventArgs e)
        {
            UpdateActiveCharacter();
        }

        private void GlobalSystemDropDownAC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FollowCharacter = false;

            EVEData.System sd = GlobalSystemDropDownAC.SelectedItem as EVEData.System;

            if(sd != null && Region != null)
            {
                bool ChangeRegion = sd.Region != Region.Name;
                SelectSystem(sd.Name, ChangeRegion);
                ReDrawMap(ChangeRegion);
            }
        }

        private void HelpIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(HelpList.Visibility == Visibility.Hidden)
            {
                HelpList.Visibility = Visibility.Visible;
                helpIcon.Fill = new SolidColorBrush(Colors.Yellow);
                HelpQM.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                HelpList.Visibility = Visibility.Hidden;
                helpIcon.Fill = new SolidColorBrush(Colors.Black);
                HelpQM.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void MapObjectChanged(object sender, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                ReDrawMap(true);
            }), DispatcherPriority.Normal);
        }

        /// <summary>
        /// Region Selection Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegionSelectCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FollowCharacter = false;

            EVEData.MapRegion rd = RegionSelectCB.SelectedItem as EVEData.MapRegion;
            if(rd == null)
            {
                return;
            }

            SelectRegion(rd.Name);
        }

        private void SetJumpRange_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            MenuItem mi = sender as MenuItem;
            if(mi != null)
            {
                EveManager.JumpShip js = EveManager.JumpShip.Super;

                if(mi.DataContext as string == "6")
                {
                    js = EveManager.JumpShip.Super;
                }
                if(mi.DataContext as string == "7")
                {
                    js = EveManager.JumpShip.Carrier;
                }

                if(mi.DataContext as string == "8")
                {
                    js = EveManager.JumpShip.Blops;
                }

                if(mi.DataContext as string == "10")
                {
                    js = EveManager.JumpShip.JF;
                }

                activeJumpSpheres[eveSys.Name] = js;

                if(mi.DataContext as string == "0")
                {
                    if(activeJumpSpheres.Keys.Contains(eveSys.Name))
                    {
                        activeJumpSpheres.Remove(eveSys.Name);
                    }
                }

                if(mi.DataContext as string == "-1")
                {
                    activeJumpSpheres.Clear();
                    currentJumpCharacter = "";
                    currentCharacterJumpSystem = "";
                }

                if(!string.IsNullOrEmpty(currentJumpCharacter))
                {
                    showJumpDistance = true;
                }
                else
                {
                    showJumpDistance = activeJumpSpheres.Count > 0;
                }

                ReDrawMap(true);
            }
        }

        /// <summary>
        /// Shape (ie System) MouseDown handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShapeMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            Shape obj = sender as Shape;

            EVEData.MapSystem selectedSys = obj.DataContext as EVEData.MapSystem;

            if(e.ChangedButton == MouseButton.Left)
            {
                if(e.ClickCount == 1)
                {
                    bool redraw = false;
                    if(showJumpDistance || (ShowSystemTimers && MapConf.ShowIhubVunerabilities))
                    {
                        redraw = true;
                    }
                    FollowCharacter = false;
                    SelectSystem(selectedSys.Name);

                    ReDrawMap(redraw);
                }

                if(e.ClickCount == 2 && selectedSys.Region != Region.Name)
                {
                    foreach(EVEData.MapRegion rd in EM.Regions)
                    {
                        if(rd.Name == selectedSys.Region)
                        {
                            RegionSelectCB.SelectedItem = rd;

                            ReDrawMap();
                            SelectSystem(selectedSys.Name);
                            break;
                        }
                    }
                }
            }

            if(e.ChangedButton == MouseButton.Right)
            {
                ContextMenu cm = this.FindResource("SysRightClickContextMenu") as ContextMenu;
                cm.PlacementTarget = obj;
                cm.DataContext = selectedSys;

                MenuItem setDesto = cm.Items[2] as MenuItem;
                MenuItem addWaypoint = cm.Items[4] as MenuItem;
                MenuItem clearRoute = cm.Items[6] as MenuItem;

                MenuItem characters = cm.Items[7] as MenuItem;
                characters.Items.Clear();

                setDesto.IsEnabled = false;
                addWaypoint.IsEnabled = false;
                clearRoute.IsEnabled = false;

                characters.IsEnabled = false;
                characters.Visibility = Visibility.Collapsed;

                if(ActiveCharacter != null && ActiveCharacter.ESILinked)
                {
                    setDesto.IsEnabled = true;
                    addWaypoint.IsEnabled = true;
                    clearRoute.IsEnabled = true;
                }

                // get a list of characters in this system
                List<LocalCharacter> charactersInSystem = new List<LocalCharacter>();
                foreach(LocalCharacter lc in EM.LocalCharacters)
                {
                    if(lc.Location == selectedSys.Name)
                    {
                        charactersInSystem.Add(lc);
                    }
                }

                if(charactersInSystem.Count > 0)
                {
                    characters.IsEnabled = true;
                    characters.Visibility = Visibility.Visible;

                    foreach(LocalCharacter lc in charactersInSystem)
                    {
                        MenuItem miChar = new MenuItem();
                        miChar.Header = lc.Name;
                        characters.Items.Add(miChar);

                        // Use zh-CN menu/popup strings when that UI language is active
                        bool isZH = SMT.EVEData.EveManager.CurrentLanguage == "zh-CN";

                        // now create the child menu's
                        MenuItem miAutoRange = new MenuItem();
                        miAutoRange.Header = isZH ? "è‡ªåŠ¨è·³è·ƒèŒƒå›´" : "Auto Jump Range";
                        miAutoRange.DataContext = lc;
                        miChar.Items.Add(miAutoRange);

                        MenuItem miARNone = new MenuItem();
                        miARNone.Header = isZH ? "æ— " : "None";
                        miARNone.DataContext = "0";
                        miARNone.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARNone);

                        MenuItem miARSuper = new MenuItem();
                        miARSuper.Header = isZH ? "è¶…çº§èˆªæ¯/æ³°å¦ (6.0LY)" : "Super/Titan  (6.0LY)";
                        miARSuper.DataContext = "6";
                        miARSuper.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARSuper);

                        MenuItem miARCF = new MenuItem();
                        miARCF.Header = isZH ? "èˆªæ¯/æ— ç•/ä¼ æ¯ (7.0LY)" : "Carriers/Fax (7.0LY)";
                        miARCF.DataContext = "7";
                        miARCF.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARCF);

                        MenuItem miARBlops = new MenuItem();
                        miARBlops.Header = isZH ? "é»‘éšç‰¹å‹¤èˆ° (8.0LY)" : "Black Ops    (8.0LY)";
                        miARBlops.DataContext = "8";
                        miARBlops.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARBlops);

                        MenuItem miARJFR = new MenuItem();
                        miARJFR.Header = isZH ? "è·³è´§/å¤§é²¸é±¼ (10.0LY)" : "JF/Rorq     (10.0LY)";
                        miARJFR.DataContext = "10";
                        miARJFR.Click += characterRightClickAutoRange_Clicked;
                        miAutoRange.Items.Add(miARJFR);

                        if (!string.IsNullOrEmpty(lc.GameLogWarningText))
                        {
                            MenuItem miRemoveWarning = new MenuItem();
                            miRemoveWarning.Header = isZH ? "æ¸…é™¤è­¦å‘Š" : "Clear Warning";
                            miRemoveWarning.DataContext = lc;
                            miRemoveWarning.Click += characterRightClickClearWarning;
                            miChar.Items.Add(miRemoveWarning);
                        }
                    }
                }

                cm.IsOpen = true;
            }
        }

        private void characterRightClickClearWarning(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;

            LocalCharacter lc = mi.DataContext as LocalCharacter;
            if(lc != null)
            {
                lc.GameLogWarningText = "";
            }
        }

        /// <summary>
        /// Shape (ie System) Mouse over handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShapeMouseOverHandler(object sender, MouseEventArgs e)
        {
            Shape obj = sender as Shape;

            EVEData.MapSystem selectedSys = obj.DataContext as EVEData.MapSystem;

            Thickness one = new Thickness(1);

            if(obj.IsMouseOver && MapConf.ShowSystemPopup)
            {
                SystemInfoPopup.PlacementTarget = obj;
                SystemInfoPopup.VerticalOffset = 5;
                SystemInfoPopup.HorizontalOffset = 15;
                SystemInfoPopup.DataContext = selectedSys.ActualSystem;

                SystemInfoPopupSP.Background = new SolidColorBrush(MapConf.ActiveColourScheme.PopupBackground);

                SystemInfoPopupSP.Children.Clear();

                Label header = new Label();
                header.Content = selectedSys.LocalizedName;
                header.FontWeight = FontWeights.Bold;
                header.FontSize = 14;
                header.Padding = one;
                header.Margin = one;
                header.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);

                SystemInfoPopupSP.Children.Add(header);
                SystemInfoPopupSP.Children.Add(new Separator());

                bool needSeperator = false;
                List<string> charNames = new List<string>();
                foreach(LocalCharacter c in EM.LocalCharacters)
                {
                    if(c.Location == selectedSys.Name)
                    {
                        needSeperator = true;
                        Label characterlabel = new Label();
                        string cname = c.Name;
                        if(!c.IsOnline)
                        {
                            cname += " (Offline)";
                        }
                        charNames.Add(cname);
                    }
                }

                charNames.Sort();

                foreach(string s in charNames)
                {
                    Label characterlabel = new Label();
                    characterlabel.Padding = one;
                    characterlabel.Margin = one;
                    characterlabel.Content = s;

                    characterlabel.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(characterlabel);
                }

                if(needSeperator)
                {
                    SystemInfoPopupSP.Children.Add(new Separator());
                }

                // Use zh-CN popup labels when that UI language is active
                bool isZH = SMT.EVEData.EveManager.CurrentLanguage == "zh-CN";

                Label constellation = new Label();
                constellation.Padding = one;
                constellation.Margin = one;
                constellation.Content = (isZH ? "æ˜Ÿåº§\t:  " : "Const\t:  ") + selectedSys.ActualSystem.ConstellationName;
                constellation.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                SystemInfoPopupSP.Children.Add(constellation);

                Label secstatus = new Label();
                secstatus.Padding = one;
                secstatus.Margin = one;
                secstatus.Content = (isZH ? "å®‰å…¨ç­‰çº§\t:  " : "Security\t:  ") + string.Format("{0:0.00}", selectedSys.ActualSystem.TrueSec) + " (" + selectedSys.ActualSystem.SecType + ")";
                secstatus.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                SystemInfoPopupSP.Children.Add(secstatus);

                SystemInfoPopupSP.Children.Add(new Separator());

                if (selectedSys.ActualSystem.ShipKillsLastHour != 0)
                {
                    Label data = new Label();
                    data.Padding = one;
                    data.Margin = one;
                    data.Content = isZH ? $"èˆ°èˆ¹å‡»æ€\t:  {selectedSys.ActualSystem.ShipKillsLastHour}" : $"Ship Kills\t:  {selectedSys.ActualSystem.ShipKillsLastHour}";
                    data.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(data);
                }

                if (selectedSys.ActualSystem.PodKillsLastHour != 0)
                {
                    Label data = new Label();
                    data.Padding = one;
                    data.Margin = one;
                    data.Content = isZH ? $"å¤ªç©ºèˆ±å‡»æ€\t:  {selectedSys.ActualSystem.PodKillsLastHour}" : $"Pod Kills\t:  {selectedSys.ActualSystem.PodKillsLastHour}";
                    data.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(data);
                }

                if (selectedSys.ActualSystem.NPCKillsLastHour != 0)
                {
                    Label data = new Label();
                    data.Padding = one;
                    data.Margin = one;
                    data.Content = isZH ? $"NPC å‡»æ€\t:  {selectedSys.ActualSystem.NPCKillsLastHour}, å˜åŒ– ({selectedSys.ActualSystem.NPCKillsDeltaLastHour})" : $"NPC Kills\t:  {selectedSys.ActualSystem.NPCKillsLastHour}, Delta ({selectedSys.ActualSystem.NPCKillsDeltaLastHour})";
                    data.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(data);
                }

                if (selectedSys.ActualSystem.JumpsLastHour != 0)
                {
                    Label data = new Label();
                    data.Padding = one;
                    data.Margin = one;

                    data.Content = isZH ? $"è·³è·ƒæ•°\t:  {selectedSys.ActualSystem.JumpsLastHour}" : $"Jumps\t:  {selectedSys.ActualSystem.JumpsLastHour}";
                    data.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(data);
                }

                if (ShowJumpBridges)
                {
                    Point from = new Point();
                    Point to = new Point(); ;
                    bool AddJBHighlight = false;

                    foreach(EVEData.JumpBridge jb in EM.JumpBridges)
                    {
                        if(selectedSys.Name == jb.From)
                        {
                            Label jbl = new Label();
                            jbl.Padding = one;
                            jbl.Margin = one;
                            jbl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);

                            jbl.Content = $"JB\t: {jb.To}";

                            if(!Region.IsSystemOnMap(jb.To))
                            {
                                EVEData.System sys = EM.GetEveSystem(jb.To);
                                jbl.Content += $" ({sys.Region})";
                            }

                            SystemInfoPopupSP.Children.Add(jbl);

                            from.X = selectedSys.Layout.X;
                            from.Y = selectedSys.Layout.Y;

                            if(Region.IsSystemOnMap(jb.To) && !jb.Disabled)
                            {
                                MapSystem ms = Region.MapSystems[jb.To];
                                to.X = ms.Layout.X;
                                to.Y = ms.Layout.Y;
                                AddJBHighlight = true;
                            }
                        }

                        if(selectedSys.Name == jb.To)
                        {
                            Label jbl = new Label();
                            jbl.Padding = one;
                            jbl.Margin = one;
                            jbl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);

                            jbl.Content = $"JB\t: {jb.From}";

                            if(!Region.IsSystemOnMap(jb.From))
                            {
                                EVEData.System sys = EM.GetEveSystem(jb.From);
                                jbl.Content += $" ({sys.Region})";
                            }

                            SystemInfoPopupSP.Children.Add(jbl);

                            from.X = selectedSys.Layout.X;
                            from.Y = selectedSys.Layout.Y;

                            if(Region.IsSystemOnMap(jb.From) && !jb.Disabled)
                            {
                                MapSystem ms = Region.MapSystems[jb.From];
                                to.X = ms.Layout.X;
                                to.Y = ms.Layout.Y;
                                AddJBHighlight = true;
                            }
                        }
                    }




                    if(AddJBHighlight)
                    {
                        Line jbHighlight = new Line();

                        Brush highlightBrush = new SolidColorBrush(Colors.Yellow);

                        jbHighlight.X1 = from.X;
                        jbHighlight.Y1 = from.Y;

                        jbHighlight.X2 = to.X;
                        jbHighlight.Y2 = to.Y;

                        jbHighlight.StrokeThickness = 5;
                        jbHighlight.Visibility = Visibility.Visible;
                        jbHighlight.IsHitTestVisible = false;
                        jbHighlight.Stroke = highlightBrush;
                        jbHighlight.StrokeThickness = 5;

                        DoubleCollection dashes = new DoubleCollection();
                        dashes.Add(1.0);
                        dashes.Add(1.0);
                        jbHighlight.StrokeDashArray = dashes;

                        DynamicMapElementsJBHighlight.Add(jbHighlight);

                        Canvas.SetZIndex(jbHighlight, 19);

                        MainCanvas.Children.Add(jbHighlight);

                        double circleSize = 30;
                        double circleOffset = circleSize / 2;

                        Shape jbhighlightEndPointCircle = new Ellipse() { Height = circleSize, Width = circleSize };

                        jbhighlightEndPointCircle.Stroke = highlightBrush;
                        jbhighlightEndPointCircle.StrokeThickness = 1.5;
                        jbhighlightEndPointCircle.StrokeLineJoin = PenLineJoin.Round;

                        Canvas.SetLeft(jbhighlightEndPointCircle, to.X - circleOffset);
                        Canvas.SetTop(jbhighlightEndPointCircle, to.Y - circleOffset);

                        DynamicMapElementsJBHighlight.Add(jbhighlightEndPointCircle);

                        Canvas.SetZIndex(jbhighlightEndPointCircle, 19);

                        MainCanvas.Children.Add(jbhighlightEndPointCircle);
                    }
                }

                bool addAdditionalHighlights = true;
                if(addAdditionalHighlights)
                {
                    Brush NormalGateBrush = new SolidColorBrush(MapConf.ActiveColourScheme.NormalGateColour);
                    Brush ConstellationGateBrush = new SolidColorBrush(MapConf.ActiveColourScheme.ConstellationGateColour);
                    Brush RegionGateBrush = new SolidColorBrush(MapConf.ActiveColourScheme.RegionGateColour);

                    foreach(string connection in selectedSys.ActualSystem.Jumps)
                    {


                        if(Region.MapSystems.ContainsKey(connection))
                        {
                            MapSystem s1 = Region.MapSystems[connection];

                            Line sysLink = new Line();
                            sysLink.Stroke = NormalGateBrush;

                            if(selectedSys.ActualSystem.ConstellationID != s1.ActualSystem.ConstellationID)
                            {
                                sysLink.Stroke = ConstellationGateBrush;
                            }

                            if(selectedSys.ActualSystem.Region != s1.ActualSystem.Region)
                            {
                                sysLink.Stroke = RegionGateBrush;
                            }



                            sysLink.X1 = selectedSys.Layout.X;
                            sysLink.Y1 = selectedSys.Layout.Y;

                            sysLink.X2 = s1.Layout.X;
                            sysLink.Y2 = s1.Layout.Y;


                            sysLink.StrokeThickness = 4;

                            DynamicMapElementsSysLinkHighlight.Add(sysLink);
                            Canvas.SetZIndex(sysLink, 19);
                            MainCanvas.Children.Add(sysLink);
                        }


                    }
                }

                if(selectedSys.ActualSystem.IHubOccupancyLevel != 0.0f || selectedSys.ActualSystem.TCUOccupancyLevel != 0.0f)
                {
                    SystemInfoPopupSP.Children.Add(new Separator());
                }

                // update IHubInfo
                if(selectedSys.ActualSystem.IHubOccupancyLevel != 0.0f)
                {
                    Label sov = new Label();
                    sov.Padding = one;
                    sov.Margin = one;
                    sov.Content = $"IHUB\t:  {selectedSys.ActualSystem.IHubVunerabliltyStart.Hour:00}:{selectedSys.ActualSystem.IHubVunerabliltyStart.Minute:00} to {selectedSys.ActualSystem.IHubVunerabliltyEnd.Hour:00}:{selectedSys.ActualSystem.IHubVunerabliltyEnd.Minute:00}, ADM : {selectedSys.ActualSystem.IHubOccupancyLevel}";
                    sov.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(sov);
                }

                // update TCUInfo
                if(selectedSys.ActualSystem.TCUOccupancyLevel != 0.0f)
                {
                    Label sov = new Label();
                    sov.Padding = one;
                    sov.Margin = one;
                    sov.Content = $"TCU\t:  {selectedSys.ActualSystem.TCUVunerabliltyStart.Hour:00}:{selectedSys.ActualSystem.TCUVunerabliltyStart.Minute:00} to {selectedSys.ActualSystem.TCUVunerabliltyEnd.Hour:00}:{selectedSys.ActualSystem.TCUVunerabliltyEnd.Minute:00}, ADM : {selectedSys.ActualSystem.TCUOccupancyLevel}";
                    sov.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(sov);
                }

                // update Infrastructure Upgrades
                if(selectedSys.ActualSystem.InfrastructureUpgrades.Count > 0)
                {
                    Label upgradeHeader = new Label();
                    upgradeHeader.Padding = one;
                    upgradeHeader.Margin = one;
                    upgradeHeader.Content = "Infrastructure Upgrades:";
                    upgradeHeader.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    upgradeHeader.FontWeight = FontWeights.Bold;
                    SystemInfoPopupSP.Children.Add(upgradeHeader);

                    foreach(EVEData.InfrastructureUpgrade upgrade in selectedSys.ActualSystem.InfrastructureUpgrades.OrderBy(u => u.SlotNumber))
                    {
                        Label upgradeLabel = new Label();
                        upgradeLabel.Padding = new Thickness(15, 1, 1, 1);
                        upgradeLabel.Margin = one;
                        upgradeLabel.Content = $"{upgrade.SlotNumber}. {upgrade.DisplayName} - {upgrade.Status}";
                        upgradeLabel.Foreground = new SolidColorBrush(upgrade.IsOnline ? Colors.LightGreen : Colors.Gray);
                        SystemInfoPopupSP.Children.Add(upgradeLabel);
                    }
                }

                List<TheraConnection> currentTheraConnections = EM.TheraConnections.ToList();
                // update Thera Info
                foreach(EVEData.TheraConnection tc in currentTheraConnections)
                {
                    if(selectedSys.Name == tc.System)
                    {
                        SystemInfoPopupSP.Children.Add(new Separator());

                        Label tl = new Label();
                        tl.Padding = one;
                        tl.Margin = one;
                        tl.Content = $"Thera\t: in {tc.InSignatureID}";
                        tl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                        SystemInfoPopupSP.Children.Add(tl);

                        tl = new Label();
                        tl.Padding = one;
                        tl.Margin = one;
                        tl.Content = $"Thera\t: out {tc.OutSignatureID}";
                        tl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                        SystemInfoPopupSP.Children.Add(tl);
                    }
                }
                List<TurnurConnection> currentTurnurConnections = EM.TurnurConnections.ToList();

                // update Turnur Info
                foreach(EVEData.TurnurConnection tc in currentTurnurConnections)
                {
                    if(selectedSys.Name == tc.System)
                    {
                        SystemInfoPopupSP.Children.Add(new Separator());

                        Label tl = new Label();
                        tl.Padding = one;
                        tl.Margin = one;
                        tl.Content = $"Turnur\t: in {tc.InSignatureID}";
                        tl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                        SystemInfoPopupSP.Children.Add(tl);

                        tl = new Label();
                        tl.Padding = one;
                        tl.Margin = one;
                        tl.Content = $"Turnur\t: out {tc.OutSignatureID}";
                        tl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                        SystemInfoPopupSP.Children.Add(tl);
                    }
                }

                // storms
                foreach(EVEData.Storm s in EM.MetaliminalStorms)
                {
                    if(selectedSys.Name == s.System)
                    {
                        SystemInfoPopupSP.Children.Add(new Separator());

                        Label tl = new Label();
                        tl.Padding = one;
                        tl.Margin = one;
                        tl.Content = $"Storm\t: {s.Type}";
                        tl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                        SystemInfoPopupSP.Children.Add(tl);
                    }
                }

                SystemInfoPopupSP.Children.Add(new Separator());

                // Points of interest
                foreach(POI p in EM.PointsOfInterest)
                {
                    if(selectedSys.Name == p.System)
                    {
                        Label tl = new Label();
                        tl.Padding = one;
                        tl.Margin = one;
                        tl.Content = $"{p.Type} : {p.ShortDesc}";
                        tl.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                        SystemInfoPopupSP.Children.Add(tl);
                    }
                }

                if(MapConf.ShowTrigInvasions && selectedSys.ActualSystem.TrigInvasionStatus != EVEData.System.EdenComTrigStatus.None)
                {
                    Label trigInfo = new Label();
                    trigInfo.Padding = one;
                    trigInfo.Margin = one;
                    trigInfo.Content = $"Invasion : {selectedSys.ActualSystem.TrigInvasionStatus}";
                    trigInfo.Foreground = new SolidColorBrush(MapConf.ActiveColourScheme.PopupText);
                    SystemInfoPopupSP.Children.Add(trigInfo);
                }

                // trigger the hover event

                if(SystemHoverEvent != null)
                {
                    SystemHoverEvent(selectedSys.Name);
                }

                SystemInfoPopup.IsOpen = true;
            }
            else
            {
                SystemInfoPopup.IsOpen = false;

                foreach(UIElement uie in DynamicMapElementsSysLinkHighlight)
                {
                    MainCanvas.Children.Remove(uie);
                }

                foreach(UIElement uie in DynamicMapElementsJBHighlight)
                {
                    MainCanvas.Children.Remove(uie);
                }

                // trigger the hover event

                if(SystemHoverEvent != null)
                {
                    SystemHoverEvent(string.Empty);
                }

                DynamicMapElementsJBHighlight.Clear();
                DynamicMapElementsSysLinkHighlight.Clear();
            }
        }

        /// <summary>
        /// Add Waypoint Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemAddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            if(ActiveCharacter != null)
            {
                ActiveCharacter.AddDestination(eveSys.ActualSystem.ID, false);
            }
        }

        private void SysContexMenuItemAddWaypointAll_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            foreach(LocalCharacter lc in EM.LocalCharacters)
            {
                if(lc.IsOnline && lc.ESILinked)
                {
                    lc.AddDestination(eveSys.ActualSystem.ID, false);
                }
            }
        }

        /// <summary>
        /// Ckear Route  Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemClearRoute_Click(object sender, RoutedEventArgs e)
        {
            if(ActiveCharacter != null)
            {
                ActiveCharacter.ClearAllWaypoints();
            }
        }

        /// <summary>
        /// Copy Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            try
            {
                if(eveSys != null)
                {
                    Clipboard.SetText(eveSys.Name);
                }
            }
            catch { }
        }

        private void SysContexMenuItemCopyEncoded_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            try
            {
                if(eveSys != null)
                {
                    Clipboard.SetText($"<url=showinfo:5//{eveSys.ActualSystem.ID}>{eveSys.Name}</url>");
                }
            }
            catch { }
        }



        /// <summary>
        /// Dotlan Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemDotlan_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("http://evemaps.dotlan.net/map/{0}/{1}", rd.DotLanRef, eveSys.Name);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uRL) { UseShellExecute = true });
        }

        /// <summary>
        /// Set Destination Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemSetDestination_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            if(ActiveCharacter != null)
            {
                ActiveCharacter.AddDestination(eveSys.ActualSystem.ID, true);
            }
        }

        private void SysContexMenuItemSetDestinationAll_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            foreach(LocalCharacter lc in EM.LocalCharacters)
            {
                if(lc.IsOnline && lc.ESILinked)
                {
                    lc.AddDestination(eveSys.ActualSystem.ID, true);
                }
            }
        }

        private void SysContexMenuItemShowInUniverse_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;

            RoutedEventArgs newEventArgs = new RoutedEventArgs(UniverseSystemSelectEvent, eveSys.Name);
            RaiseEvent(newEventArgs);
        }

        /// <summary>
        /// ZKillboard Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysContexMenuItemZKB_Click(object sender, RoutedEventArgs e)
        {
            EVEData.MapSystem eveSys = ((System.Windows.FrameworkElement)((System.Windows.FrameworkElement)sender).Parent).DataContext as EVEData.MapSystem;
            EVEData.MapRegion rd = EM.GetRegion(eveSys.Region);

            string uRL = string.Format("https://zkillboard.com/system/{0}/", eveSys.ActualSystem.ID);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uRL) { UseShellExecute = true });
        }

        private void SystemDropDownAC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EVEData.MapSystem sd = SystemDropDownAC.SelectedItem as EVEData.MapSystem;

            if(sd != null)
            {
                SelectSystem(sd.Name);
                ReDrawMap(false);
            }
        }

        /// <summary>
        /// UI Refresh Timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            if(currentJumpCharacter != "")
            {
                foreach(LocalCharacter c in EM.LocalCharacters)
                {
                    if(c.Name == currentJumpCharacter)
                    {
                        currentCharacterJumpSystem = c.Location;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                ReDrawMap(false);
            }), DispatcherPriority.Normal);
        }
    }
}

