using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
