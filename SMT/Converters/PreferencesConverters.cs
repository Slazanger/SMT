using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace SMT.Converters
{
    public class JoinStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var lines = value as IEnumerable<string>;
            return lines is null ? null : string.Join(Environment.NewLine, lines);
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            string inputstr = (string)value;
            string[] lines = inputstr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> oc = new List<string>(lines);
            return oc;
        }
    }

    public class NegateBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is bool boolValue)
            {
                if(boolValue)
                {
                    return "True";
                }
                else
                {
                    return "False";
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
