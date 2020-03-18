using System.Windows;

namespace WpfHelpers.WpfDataManipulation.XamlConverters
{
    public class BoolToVisibilityConverter : BoolToAnySwitchConverter<Visibility>
    {
        public BoolToVisibilityConverter()
        {
            IfTrue = Visibility.Visible;
            IfFalse = Visibility.Collapsed;
        }
    }
}