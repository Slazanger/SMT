using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfHelpers.WpfDataManipulation.XamlConverters
{
    /// <summary>
    ///     Compares Converter Parameter.ToString() and value.Tostring() And returns result
    /// </summary>
    [ValueConversion(typeof(ResizeMode?), typeof(Visibility))]
    public class ResizeModeToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Checks value for ResizeMode value />
        /// </summary>
        /// <param name="value">incoming value</param>
        /// <param name="targetType">not used</param>
        /// <param name="parameter">not used</param>
        /// <param name="culture">not used</param>
        /// <returns>if value is <see cref="ResizeMode.NoResize"/>, returns <see cref="Visibility.Collapsed"/>, else <see cref="Visibility.Visible"/>  </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (ResizeMode)value;

            if (val == ResizeMode.NoResize)
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        /// <summary>
        /// Implemets IValueConverter.ConvertBack
        /// </summary>
        /// <param name="value">incoming value</param>
        /// <param name="targetType">not used</param>
        /// <param name="parameter">not used</param>
        /// <param name="culture">not used</param>
        /// <returns>returns value back</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}