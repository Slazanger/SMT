#region

using System;
using System.Globalization;
using System.Windows.Data;

#endregion

namespace SMT.ZoomControl
{
    public class EqualityToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return parameter;

            return null;
            //it's false, so don't bind it back
            throw new Exception("EqualityToBooleanConverter: It's false, I won't bind back.");
        }
    }
}