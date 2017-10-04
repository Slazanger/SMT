using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfHelpers.WpfDataManipulation.XamlConverters
{
    public class BoolToAnySwitchConverter<T> : IValueConverter
    {
        public T IfTrue { get; set; }

        public T IfFalse { get; set; }

        #region Implementation of IValueConverter

        /// <summary>
        ///     Converts a value.
        /// </summary>
        /// <returns>
        ///     A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">if 'r', result will be reverter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                var result = (bool) value;

                if (parameter != null && parameter.Equals("r"))
                    result = !result;

                return result ? IfTrue : IfFalse;
            }

            return IfFalse;
        }

        /// <summary>
        ///     Converts a value.
        /// </summary>
        /// <returns>
        ///     A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is T))
                return false;

            if (value.Equals(IfTrue))
                return true;

            return false;
        }

        #endregion
    }

    /// <summary>
    ///     Converts bool to any kind of type, just fill <see cref="BoolToObjectConverter.IfTrue" /> and
    ///     <see cref="BoolToObjectConverter.IfFalse" /> props
    /// </summary>
    public class BoolToObjectConverter : BoolToAnySwitchConverter<object>
    {
    }
}