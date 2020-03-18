using System.Windows;

namespace WpfHelpers.WpfControls.Window
{
    public class TitleBarEx
    {
        public static readonly DependencyProperty ButtonsInTitleBarProperty = DependencyProperty.RegisterAttached(
            "TitleAdditionalContent", typeof(UIElement), typeof(TitleBarEx), new PropertyMetadata(null));

        public static readonly DependencyProperty LeftTitleBarProperty = DependencyProperty.RegisterAttached(
            "LeftTitleBar", typeof(UIElement), typeof(TitleBarEx), new PropertyMetadata(default(UIElement)));

        public static UIElement GetLeftTitleBar(DependencyObject element)
        {
            return (UIElement)element.GetValue(LeftTitleBarProperty);
        }

        public static UIElement GetTitleAdditionalContent(DependencyObject element)
        {
            return (UIElement)element.GetValue(ButtonsInTitleBarProperty);
        }

        public static void SetLeftTitleBar(DependencyObject element, UIElement value)
        {
            element.SetValue(LeftTitleBarProperty, value);
        }

        public static void SetTitleAdditionalContent(DependencyObject element, UIElement value)
        {
            element.SetValue(ButtonsInTitleBarProperty, value);
        }
    }
}