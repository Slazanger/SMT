using System;
using System.Windows.Controls;

namespace WpfHelpers.WpfDataManipulation.Rules
{
    public class GenericRangeValidationRule<T> : ValidationRule 
        where T : IComparable
    {
        public T MinValue { get; set; }
        public T MaxValue { get; set; }

        public override ValidationResult Validate(
            object value, System.Globalization.CultureInfo cultureInfo)
        {
            T tValue = default(T);

            try
            {
                tValue = (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception)
            {
                return new ValidationResult(false, "Not valid");
            }
            
            string text = $"Must be between {MinValue} and {MaxValue}";
            
            if (tValue.CompareTo(MinValue) < 0)
                return new ValidationResult(false, "To small. " + text);
            if (tValue.CompareTo(MaxValue) > 0)
                return new ValidationResult(false, "To large. " + text);

            return ValidationResult.ValidResult;
        }
    }

    public class IntRangeValidationRule : GenericRangeValidationRule<int> { }
    public class DoubleRangeValidationRule : GenericRangeValidationRule<double> { }
    public class LongRangeValidationRule : GenericRangeValidationRule<long> { }
    public class UIntRangeValidationRule : GenericRangeValidationRule<uint> { }
    public class DecimalRangeValidationRule : GenericRangeValidationRule<decimal> { }
}