using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace WpfHelpers.WpfDataManipulation.XamlConverters
{
    internal class StringToIpAddressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var input = value?.ToString() ?? string.Empty;
            var ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            var result = ip.Matches(input);

            if (result.Count > 0)
                return result[0].Value;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}