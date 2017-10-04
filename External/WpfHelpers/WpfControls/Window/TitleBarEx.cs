using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfHelpers.WpfControls.Window
{
    public class TitleBarEx
    {
        public static readonly DependencyProperty ButtonsInTitleBarProperty = DependencyProperty.RegisterAttached(
            "TitleAdditionalContent", typeof(UIElement), typeof(TitleBarEx), new PropertyMetadata(null));

        public static void SetTitleAdditionalContent(DependencyObject element, UIElement value)
        {
            element.SetValue(ButtonsInTitleBarProperty, value);
        }

        public static UIElement GetTitleAdditionalContent(DependencyObject element)
        {
            return (UIElement)element.GetValue(ButtonsInTitleBarProperty);
        }

        public static readonly DependencyProperty LeftTitleBarProperty = DependencyProperty.RegisterAttached(
            "LeftTitleBar", typeof (UIElement), typeof (TitleBarEx), new PropertyMetadata(default(UIElement)));

        public static void SetLeftTitleBar(DependencyObject element, UIElement value)
        {
            element.SetValue(LeftTitleBarProperty, value);
        }

        public static UIElement GetLeftTitleBar(DependencyObject element)
        {
            return (UIElement)element.GetValue(LeftTitleBarProperty);
        }
    }
}
