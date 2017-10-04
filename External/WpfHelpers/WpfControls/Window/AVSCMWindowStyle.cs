#region

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

#endregion

namespace WpfHelpers.WpfControls.Window
{
    internal static class LocalExtensions
    {
        public static void ForWindowFromChild(this object childDependencyObject, Action<System.Windows.Window> action)
        {
            var element = childDependencyObject as DependencyObject;
            while (element != null)
            {
                element = VisualTreeHelper.GetParent(element);
                if (element is System.Windows.Window)
                {
                    action(element as System.Windows.Window);
                    break;
                }
            }
        }

        public static void ForWindowFromTemplate(this object templateFrameworkElement, Action<System.Windows.Window> action)
        {
            var window = ((FrameworkElement) templateFrameworkElement).TemplatedParent as System.Windows.Window;
            if (window != null) action(window);
        }

        public static IntPtr GetWindowHandle(this System.Windows.Window window)
        {
            var helper = new WindowInteropHelper(window);
            return helper.Handle;
        }
    }

    /// <summary>
    ///     Converts <see cref="ResizeMode" /> type to <see cref="Cursors" /> class
    /// </summary>
    public class MouseCursorConverter : IValueConverter
    {
        /// <summary>
        ///     Converts value and converter param to Cursors type
        /// </summary>
        /// <param name="value">if <see cref="ResizeMode.NoResize" />, return <see cref="Cursors.Arrow" />, elese checks param</param>
        /// <param name="targetType"> not used </param>
        /// <param name="parameter"> SizeWE or SizeNS </param>
        /// <param name="culture">not used</param>
        /// <returns>default: <see cref="Cursors.Arrow" />, else depends on value name or param</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((ResizeMode) value == ResizeMode.NoResize)
                return Cursors.Arrow;


            if (parameter.ToString() == "SizeWE")
                return Cursors.SizeWE;

            if (parameter.ToString() == "SizeNS")
                return Cursors.SizeNS;


            return Cursors.Arrow;
        }

        /// <summary>
        ///     Empty IValueConverter.ConvertBack, throws <exception cref="NotImplementedException"></exception>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    /// <summary>
    ///     App window style template
    /// </summary>
    [TemplatePart(Name = "PART_grid", Type = typeof (Grid))]
    public partial class AVSCMWindowStyle
    {
        /// <summary>
        ///     TitleBar_MouseDown - Drag if single-click, resize if double-click
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                if (e.ClickCount == 2)
                {
                    AdjustWindowSize(sender);
                }
                else
                {
                    sender.ForWindowFromTemplate(w => w.DragMove());
                }
        }

        /// <summary>
        ///     CloseButton_Clicked
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(w => w.Close());
        }

        /// <summary>
        ///     MaximizedButton_Clicked
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            AdjustWindowSize(sender);
        }

        /// <summary>
        ///     Minimized Button_Clicked
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(w => w.WindowState = WindowState.Minimized);
        }

        /// <summary>
        ///     Adjusts the WindowSize to correct parameters when Maximize button is clicked
        /// </summary>
        private void AdjustWindowSize(object sender)
        {
            sender.ForWindowFromTemplate(w =>
            {
                if (w.ResizeMode == ResizeMode.CanResize)
                {
                    if (w.WindowState == WindowState.Maximized)
                    {
                        //this.AllowsTransparency=true;
                        w.WindowState = WindowState.Normal;
                    }
                    else
                    {
                        //this.AllowsTransparency=false;
                        w.WindowState = WindowState.Maximized;
                    }
                }
            });
        }

        #region sizing event handlers

        private void OnSizeSouth(object sender, MouseButtonEventArgs e)
        {
            OnSize(sender, SizingAction.South);
        }

        private void OnSizeNorth(object sender, MouseButtonEventArgs e)
        {
            OnSize(sender, SizingAction.North);
        }

        private void OnSizeEast(object sender, MouseButtonEventArgs e)
        {
            OnSize(sender, SizingAction.East);
        }

        private void OnSizeWest(object sender, MouseButtonEventArgs e)
        {
            OnSize(sender, SizingAction.West);
        }

        private void OnSizeNorthWest(object sender, MouseButtonEventArgs e)
        {
            OnSize(sender, SizingAction.NorthWest);
        }

        private void OnSizeNorthEast(object sender, MouseButtonEventArgs e)
        {
            OnSize(sender, SizingAction.NorthEast);
        }

        private void OnSizeSouthEast(object sender, MouseButtonEventArgs e)
        {
            OnSize(sender, SizingAction.SouthEast);
        }

        private void OnSizeSouthWest(object sender, MouseButtonEventArgs e)
        {
            OnSize(sender, SizingAction.SouthWest);
        }

        private void OnSize(object sender, SizingAction action)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                sender.ForWindowFromTemplate(w =>
                {
                    if (w.ResizeMode == ResizeMode.NoResize) return;

                    if (w.WindowState == WindowState.Normal)
                        DragSize(w.GetWindowHandle(), action);
                });
            }
        }

        private void IconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                sender.ForWindowFromTemplate(w => w.Close());
            }
            else
            {
                sender.ForWindowFromTemplate(w =>
                    SendMessage(w.GetWindowHandle(), WM_SYSCOMMAND, (IntPtr) SC_KEYMENU, (IntPtr) ' '));
            }
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(w => w.Close());
        }

        private void MinButtonClick(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(w => w.WindowState = WindowState.Minimized);
        }

        private void MaxButtonClick(object sender, RoutedEventArgs e)
        {
            sender.ForWindowFromTemplate(
                w =>
                    w.WindowState =
                        w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
        }

        private void TitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                MaxButtonClick(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                sender.ForWindowFromTemplate(w => w.DragMove());
            }
        }

        private void TitleBarMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                sender.ForWindowFromTemplate(w =>
                {
                    if (w.WindowState == WindowState.Maximized)
                    {
                        w.BeginInit();
                        const double adjustment = 40.0;
                        var mouse1 = e.MouseDevice.GetPosition(w);
                        var width1 = Math.Max(w.ActualWidth - 2*adjustment, adjustment);
                        w.WindowState = WindowState.Normal;
                        // ISSUE: fix multiple monitors 
                        var width2 = Math.Max(w.ActualWidth - 2*adjustment, adjustment);
                        w.Left = (mouse1.X - adjustment)*(1 - width2/width1);
                        w.Top = -7;
                        w.EndInit();
                        w.DragMove();
                    }
                });
            }
        }

        #region P/Invoke

        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_SIZE = 0xF000;
        private const int SC_KEYMENU = 0xF100;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


        private void DragSize(IntPtr handle, SizingAction sizingAction)
        {
            SendMessage(handle, WM_SYSCOMMAND, (IntPtr) (SC_SIZE + sizingAction), IntPtr.Zero);
            SendMessage(handle, 514, IntPtr.Zero, IntPtr.Zero);
        }

        private enum SizingAction
        {
            North = 3,
            South = 6,
            East = 2,
            West = 1,
            NorthEast = 5,
            NorthWest = 4,
            SouthEast = 8,
            SouthWest = 7
        }

        #endregion

        #endregion
    }
}