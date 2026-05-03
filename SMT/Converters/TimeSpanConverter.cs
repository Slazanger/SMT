using System;
using System.Globalization;
using System.Windows.Data;

namespace SMT.Converters
{
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan ts = (TimeSpan)value;

            string Output = "";

            if(ts.Ticks < 0)
            {
                Output += "-";
            }

            if(ts.Days != 0)
            {
                Output += Math.Abs(ts.Days) + "d ";
            }

            if(ts.Hours != 0)
            {
                Output += Math.Abs(ts.Hours) + "h ";
            }

            Output += Math.Abs(ts.Minutes) + "m ";

            return Output;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
