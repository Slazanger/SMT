using System;
using System.Globalization;
using System.Windows.Data;

namespace SMT.Converters
{
    /// <summary>
    /// Convert time elapsed to simple human format.
    /// </summary>
    public class TimeSinceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not DateTime timeFound)
                return "Unknown";

            var now = DateTime.Now;
            var elapsed = now - timeFound;

            if(elapsed.TotalDays >= 1.0 && elapsed.TotalDays < 2.0)
                return $"{(int)elapsed.TotalDays} day";
            else if(elapsed.TotalDays >= 2.0)
                return $"{(int)elapsed.TotalDays} days";

            if(elapsed.TotalHours >= 1.0 && elapsed.TotalHours < 2.0)
                return $"{(int)elapsed.TotalHours} hour";
            else if(elapsed.TotalHours >= 2.0)
                return $"{(int)elapsed.TotalHours} hours";

            if(elapsed.TotalMinutes < 1.0)
                return "Now";
            else if(elapsed.TotalMinutes < 2.0)
                return $"{(int)elapsed.TotalMinutes} minute";
            else
                return $"{(int)elapsed.TotalMinutes} minutes";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
